using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ── Creature entry ────────────────────────────────────────────────────────────

public enum CreatureType { BigCreature, RangedCreature, AmbushCreature, SwarmCreature, SmallCreature }

[System.Serializable]
public class CreatureSpawnEntry
{
    public string       label      = "Creature";
    public GameObject   prefab;              // assign for animated prefab
    public CreatureType fallbackType = CreatureType.BigCreature; // used when prefab is null
    [Range(0f, 1f)]
    public float        weight     = 1f;
    public int          minThreat  = 1;
    public bool         isSwarm    = false;
    public int          swarmSize  = 4;
}

// ── Pickup entry ──────────────────────────────────────────────────────────────

[System.Serializable]
public class PickupPool
{
    public int   targetCount  = 3;    // how many should exist in the world at once
    public float respawnDelay = 40f;  // seconds after collection before a new one appears
    public int   amount       = 1;    // heal amount (health) or ammo amount (ammo)
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Single manager that procedurally populates the world with creatures and
/// health / ammo pickups. Pickups need no prefabs — built in code.
/// Creatures use prefabs when assigned; fall back to runtime component if not.
/// </summary>
public class WorldSpawner : MonoBehaviour
{
    // ── Spawn area ────────────────────────────────────────────────────────────

    [Header("Spawn Area")]
    [Tooltip("Leave empty to use this GameObject's position as center.")]
    [SerializeField] private Transform areaCenter;
    [SerializeField] private float     minSpawnRadius = 20f;
    [SerializeField] private float     maxSpawnRadius = 80f;
    [SerializeField] private float     groundRaycastHeight = 200f;
    [SerializeField] private LayerMask groundMask = ~0;

    // ── Creatures ─────────────────────────────────────────────────────────────

    [Header("Creatures")]
    [SerializeField] private bool               spawnCreatures   = true;
    [SerializeField] private CreatureSpawnEntry[] creatureEntries;
    [SerializeField] private float              baseInterval     = 10f;  // seconds at threat 1
    [SerializeField] private float              minInterval      = 2f;   // floor at threat 5
    [SerializeField] private int                maxAlive         = 20;

    // ── Pickups ───────────────────────────────────────────────────────────────

    [Header("Health Pickups")]
    [SerializeField] private bool       spawnHealth = true;
    [SerializeField] private PickupPool healthPool  = new PickupPool { targetCount = 3, respawnDelay = 45f, amount = 1 };

    [Header("Ammo Pickups")]
    [SerializeField] private bool       spawnAmmo = true;
    [SerializeField] private PickupPool ammoPool  = new PickupPool { targetCount = 4, respawnDelay = 35f, amount = 12 };

    [Header("Obstacle Avoidance")]
    [Tooltip("Radius checked around each candidate position — rejects spots inside tree colliders.")]
    [SerializeField] private float overlapCheckRadius = 0.6f;
    [SerializeField] private LayerMask obstaclesMask  = ~0; // everything except ground by default

    // ── Runtime ───────────────────────────────────────────────────────────────

    private readonly List<GameObject>     aliveCreatures = new();
    private readonly List<TrackedPickup>  healthPickups  = new();
    private readonly List<TrackedPickup>  ammoPickups    = new();
    private PickupSpawnPoint[]            spawnPoints;

    private struct TrackedPickup
    {
        public GameObject go;
        public float      deathTime; // Time.time when it was collected (go == null)
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        spawnPoints = FindObjectsByType<PickupSpawnPoint>(FindObjectsSortMode.None);

        if (spawnPoints.Length > 0)
            Debug.Log($"[WorldSpawner] Using {spawnPoints.Length} PickupSpawnPoints for pickup placement.");
        else
            Debug.Log("[WorldSpawner] No PickupSpawnPoints found — falling back to random raycast placement.");

        if (spawnCreatures && creatureEntries != null && creatureEntries.Length > 0)
            StartCoroutine(CreatureLoop());

        if (spawnHealth)
            StartCoroutine(PickupLoop(healthPickups, healthPool, isHealth: true));

        if (spawnAmmo)
            StartCoroutine(PickupLoop(ammoPickups, ammoPool, isHealth: false));
    }

    // ── Creature loop ─────────────────────────────────────────────────────────

    IEnumerator CreatureLoop()
    {
        while (true)
        {
            CleanDead();

            int   threat   = DifficultyManager.Instance != null ? DifficultyManager.Instance.ThreatLevel : 1;
            float t        = Mathf.InverseLerp(1f, 5f, threat);
            float interval = Mathf.Lerp(baseInterval, minInterval, t);

            yield return new WaitForSeconds(interval);

            if (aliveCreatures.Count < maxAlive)
                SpawnCreature(threat);
        }
    }

    void SpawnCreature(int threat)
    {
        var entry = PickCreatureEntry(threat);
        if (entry == null) return;

        int count = entry.isSwarm ? entry.swarmSize : 1;
        for (int i = 0; i < count; i++)
        {
            if (!TryGetSpawnPoint(out Vector3 pos)) continue;

            GameObject go = entry.prefab != null
                ? Instantiate(entry.prefab, pos, Quaternion.identity)
                : CreateCreatureRuntime(entry.fallbackType, pos);

            if (go != null) aliveCreatures.Add(go);
        }
    }

    // Creates a creature in code (no prefab) — no animations but fully functional
    static GameObject CreateCreatureRuntime(CreatureType type, Vector3 pos)
    {
        var go = new GameObject(type.ToString());
        go.transform.position = pos;

        switch (type)
        {
            case CreatureType.BigCreature:     go.AddComponent<BigCreature>();     break;
            case CreatureType.RangedCreature:  go.AddComponent<RangedCreature>();  break;
            case CreatureType.AmbushCreature:  go.AddComponent<AmbushCreature>();  break;
            case CreatureType.SwarmCreature:   go.AddComponent<SwarmCreature>();   break;
            case CreatureType.SmallCreature:   go.AddComponent<SmallCreature>();   break;
        }
        return go;
    }

    CreatureSpawnEntry PickCreatureEntry(int threat)
    {
        float total = 0f;
        var eligible = new List<CreatureSpawnEntry>();
        foreach (var e in creatureEntries)
        {
            if (threat >= e.minThreat) { eligible.Add(e); total += e.weight; }
        }
        if (eligible.Count == 0 || total <= 0f) return null;

        float roll = Random.Range(0f, total), acc = 0f;
        foreach (var e in eligible)
        {
            acc += e.weight;
            if (roll <= acc) return e;
        }
        return eligible[^1];
    }

    void CleanDead() => aliveCreatures.RemoveAll(g => g == null);

    // ── Pickup loop ───────────────────────────────────────────────────────────

    IEnumerator PickupLoop(List<TrackedPickup> pool, PickupPool settings, bool isHealth)
    {
        // Initial fill
        while (pool.Count < settings.targetCount)
        {
            SpawnPickup(pool, settings, isHealth);
            yield return new WaitForSeconds(0.1f);
        }

        while (true)
        {
            yield return new WaitForSeconds(5f);

            // Check for collected pickups and schedule respawns
            for (int i = 0; i < pool.Count; i++)
            {
                var p = pool[i];
                if (p.go == null && p.deathTime == 0f)
                {
                    p.deathTime = Time.time;
                    pool[i]     = p;
                }
            }

            // Respawn any that have waited long enough, and fill missing slots
            int alive = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                var p = pool[i];
                if (p.go != null) { alive++; continue; }
                if (Time.time - p.deathTime >= settings.respawnDelay)
                {
                    pool.RemoveAt(i--);
                }
            }

            while (pool.Count < settings.targetCount)
                SpawnPickup(pool, settings, isHealth);
        }
    }

    void SpawnPickup(List<TrackedPickup> pool, PickupPool settings, bool isHealth)
    {
        if (!TryGetPickupPoint(out Vector3 pos, isHealth)) return;

        GameObject go;
        if (isHealth)
        {
            go = new GameObject("HealthPickup");
            go.transform.position = pos;
            go.AddComponent<HealthPickup>().Init(settings.amount);
        }
        else
        {
            go = new GameObject("AmmoPickup");
            go.transform.position = pos;
            go.AddComponent<AmmoPickup>().Init(settings.amount);
        }
        pool.Add(new TrackedPickup { go = go, deathTime = 0f });
    }

    // ── Position helpers ──────────────────────────────────────────────────────

    // Pickup-specific: prefers PickupSpawnPoints, falls back to random raycast
    bool TryGetPickupPoint(out Vector3 result, bool isHealth)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return TryGetSpawnPointMarker(out result, isHealth);

        return TryGetSpawnPoint(out result);
    }

    bool TryGetSpawnPointMarker(out Vector3 result, bool isHealth)
    {
        // Shuffle a few candidates and pick the first clear one
        var candidates = new List<PickupSpawnPoint>(spawnPoints);
        for (int i = 0; i < Mathf.Min(15, candidates.Count); i++)
        {
            int   idx = Random.Range(i, candidates.Count);
            (candidates[i], candidates[idx]) = (candidates[idx], candidates[i]);

            var pt = candidates[i];
            if (isHealth  && !pt.HealthAllowed) continue;
            if (!isHealth && !pt.AmmoAllowed)   continue;

            Vector3 pos = pt.transform.position;
            if (!IsObstructed(pos))
            {
                result = pos;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }

    // Creature / fallback: random position in radius via downward raycast
    bool TryGetSpawnPoint(out Vector3 result)
    {
        Vector3 center = areaCenter != null ? areaCenter.position : transform.position;

        for (int attempt = 0; attempt < 25; attempt++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float r     = Mathf.Lerp(minSpawnRadius, maxSpawnRadius, Random.value);
            float x     = center.x + Mathf.Cos(angle) * r;
            float z     = center.z + Mathf.Sin(angle) * r;

            var ray = new Ray(new Vector3(x, groundRaycastHeight, z), Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, groundRaycastHeight * 2f, groundMask,
                                 QueryTriggerInteraction.Ignore))
            {
                Vector3 pos = hit.point + Vector3.up * 0.5f;
                if (!IsObstructed(pos)) { result = pos; return true; }
            }
        }

        result = Vector3.zero;
        return false;
    }

    // Returns true if a tree (or other obstacle) blocks this position
    bool IsObstructed(Vector3 pos)
    {
        var hits = Physics.OverlapSphere(pos, overlapCheckRadius, obstaclesMask,
                                         QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            // Allow ground-layer colliders, reject everything else (trees, rocks, etc.)
            if (((1 << h.gameObject.layer) & groundMask) == 0)
                return true;
        }
        return false;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Vector3 center = areaCenter != null ? areaCenter.position : transform.position;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(center, minSpawnRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.1f);
        Gizmos.DrawWireSphere(center, maxSpawnRadius);
    }
}

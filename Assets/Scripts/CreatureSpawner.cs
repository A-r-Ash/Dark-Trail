using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnEntry
{
    public GameObject prefab;
    [Range(0f, 1f)]
    public float      weight    = 1f;
    public int        minThreat = 1;
    public bool       isSwarm   = false;
    public int        swarmSize = 5;
}

public class CreatureSpawner : MonoBehaviour
{
    [Header("Spawn Entries")]
    [SerializeField] private SpawnEntry[] entries;

    [Header("Spawn Area")]
    [Tooltip("Drag an empty GameObject here to use as the spawn area center. Leave empty to use this object's position.")]
    [SerializeField] private Transform areaCenterTransform;
    [SerializeField] private float     minRadius = 30f;
    [SerializeField] private float     maxRadius = 200f;

    [Header("Player")]
    [Tooltip("Auto-found by tag if left empty.")]
    [SerializeField] private Transform player;

    [Header("Timing")]
    [SerializeField] private float baseInterval = 12f;
    [SerializeField] private float minInterval  = 2f;
    [SerializeField] private int   maxAlive     = 30;

    private readonly List<GameObject> aliveCreatures = new List<GameObject>();

    void Start()
    {
        if (player == null)
        {
            var obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) player = obj.transform;
        }

        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            CleanDead();

            int   threat   = DifficultyManager.Instance != null ? DifficultyManager.Instance.ThreatLevel : 1;
            float t        = Mathf.InverseLerp(1f, 5f, threat);
            float interval = Mathf.Lerp(baseInterval, minInterval, t);

            yield return new WaitForSeconds(interval);

            // Only spawn when player is inside the area
            if (!IsPlayerInArea()) continue;

            if (aliveCreatures.Count < maxAlive)
                Spawn(threat);
        }
    }

    bool IsPlayerInArea()
    {
        if (player == null) return false;
        Vector3 center = Center();
        float   dist   = Vector3.Distance(
            new Vector3(player.position.x, 0f, player.position.z),
            new Vector3(center.x,          0f, center.z));
        return dist >= minRadius && dist <= maxRadius;
    }

    Vector3 Center() =>
        areaCenterTransform != null ? areaCenterTransform.position : transform.position;

    void Spawn(int threat)
    {
        SpawnEntry entry = PickEntry(threat);
        if (entry == null || entry.prefab == null) return;

        int count = entry.isSwarm ? entry.swarmSize : 1;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = RandomPosition();
            var go = Instantiate(entry.prefab, pos, Quaternion.identity);
            go.name = $"{entry.prefab.name}_{Random.Range(1000, 9999)}";
            aliveCreatures.Add(go);
        }
    }

    SpawnEntry PickEntry(int threat)
    {
        float totalWeight = 0f;
        var eligible = new List<SpawnEntry>();
        foreach (var e in entries)
        {
            if (e.prefab != null && threat >= e.minThreat)
            {
                eligible.Add(e);
                totalWeight += e.weight;
            }
        }
        if (eligible.Count == 0 || totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight), acc = 0f;
        foreach (var e in eligible)
        {
            acc += e.weight;
            if (roll <= acc) return e;
        }
        return eligible[eligible.Count - 1];
    }

    void CleanDead() => aliveCreatures.RemoveAll(g => g == null);

    Vector3 RandomPosition()
    {
        Vector3 center = Center();
        for (int i = 0; i < 20; i++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float r     = Mathf.Lerp(minRadius, maxRadius, Random.value);
            float x     = center.x + Mathf.Cos(angle) * r;
            float z     = center.z + Mathf.Sin(angle) * r;

            if (Physics.Raycast(new Vector3(x, 200f, z), Vector3.down, out RaycastHit hit, 400f))
                return hit.point + Vector3.up;
        }
        return center + Vector3.up;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = Center();
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.35f);
        Gizmos.DrawWireSphere(center, minRadius);
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.15f);
        Gizmos.DrawWireSphere(center, maxRadius);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnEntry
{
    public GameObject prefab;
    [Range(0f, 1f)]
    public float      weight       = 1f;
    public int        minThreat    = 1;   // only spawns at this threat level or above
    public bool       isSwarm      = false;
    public int        swarmSize    = 5;
}

// Continuously spawns creatures based on current threat level (1-5).
// Higher threat → more frequent spawns + heavier creatures.
// Reads threat level from DifficultyManager.Instance (or falls back to 1).
public class CreatureSpawner : MonoBehaviour
{
    [Header("Spawn Entries")]
    [SerializeField] private SpawnEntry[] entries;

    [Header("Spawn Area (X / Z)")]
    [SerializeField] private Vector2 areaCenter = new Vector2(-92f, -233f);
    [SerializeField] private float   minRadius  = 30f;
    [SerializeField] private float   maxRadius  = 200f;

    [Header("Timing")]
    [SerializeField] private float baseInterval    = 12f;   // seconds between spawns at threat 1
    [SerializeField] private float minInterval     = 2f;    // floor at threat 5
    [SerializeField] private int   maxAlive        = 30;    // hard cap

    private readonly List<GameObject> aliveCreatures = new List<GameObject>();

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            CleanDead();

            int threat = DifficultyManager.Instance != null ? DifficultyManager.Instance.ThreatLevel : 1;
            float t = Mathf.InverseLerp(1f, 5f, threat);
            float interval = Mathf.Lerp(baseInterval, minInterval, t);

            yield return new WaitForSeconds(interval);

            if (aliveCreatures.Count < maxAlive)
                Spawn(threat);
        }
    }

    void Spawn(int threat)
    {
        SpawnEntry entry = PickEntry(threat);
        if (entry == null || entry.prefab == null) return;

        if (entry.isSwarm)
        {
            for (int i = 0; i < entry.swarmSize; i++)
            {
                Vector3 pos = RandomPosition();
                var go = Instantiate(entry.prefab, pos, Quaternion.identity);
                go.name = $"{entry.prefab.name}_Swarm_{i}";
                aliveCreatures.Add(go);
            }
        }
        else
        {
            Vector3 pos = RandomPosition();
            var go = Instantiate(entry.prefab, pos, Quaternion.identity);
            go.name = $"{entry.prefab.name}_{Random.Range(1000, 9999)}";
            aliveCreatures.Add(go);
        }
    }

    SpawnEntry PickEntry(int threat)
    {
        // Collect eligible entries
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

        float roll = Random.Range(0f, totalWeight);
        float acc  = 0f;
        foreach (var e in eligible)
        {
            acc += e.weight;
            if (roll <= acc) return e;
        }
        return eligible[eligible.Count - 1];
    }

    void CleanDead()
    {
        aliveCreatures.RemoveAll(g => g == null);
    }

    Vector3 RandomPosition()
    {
        for (int i = 0; i < 20; i++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float r     = Mathf.Lerp(minRadius, maxRadius, Random.value);
            float x     = areaCenter.x + Mathf.Cos(angle) * r;
            float z     = areaCenter.y + Mathf.Sin(angle) * r;

            if (Physics.Raycast(new Vector3(x, 200f, z), Vector3.down, out RaycastHit hit, 400f))
                return hit.point + Vector3.up;
        }

        float fa = Random.value * Mathf.PI * 2f;
        float fr = Mathf.Lerp(minRadius, maxRadius, Random.value);
        return new Vector3(areaCenter.x + Mathf.Cos(fa) * fr, 1f, areaCenter.y + Mathf.Sin(fa) * fr);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 centre = new Vector3(areaCenter.x, 0f, areaCenter.y);
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.3f);
        Gizmos.DrawWireSphere(centre, minRadius);
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.15f);
        Gizmos.DrawWireSphere(centre, maxRadius);
    }
}

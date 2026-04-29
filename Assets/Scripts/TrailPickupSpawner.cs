using System.Collections;
using UnityEngine;

/// <summary>
/// Place empty GameObjects on your trails and drag them into Spawn Points.
/// Each point randomly spawns a health or ammo pickup and respawns it after collection.
/// </summary>
public class TrailPickupSpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("Drag your empty trail GameObjects here — one pickup spawns at each point.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Pickup Settings")]
    [SerializeField] private float respawnDelay  = 40f;
    [SerializeField] private int   healAmount    = 1;
    [SerializeField] private int   ammoAmount    = 12;
    [Range(0f, 1f)]
    [SerializeField] private float healthChance  = 0.5f; // 0 = always ammo, 1 = always health

    private GameObject[] activePickups;
    private float[]      timers;
    private bool[]       waiting;

    void Start()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        activePickups = new GameObject[spawnPoints.Length];
        timers        = new float[spawnPoints.Length];
        waiting       = new bool[spawnPoints.Length];

        for (int i = 0; i < spawnPoints.Length; i++)
            SpawnAt(i);
    }

    void Update()
    {
        if (spawnPoints == null) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;

            // Pickup was collected
            if (activePickups[i] == null && !waiting[i])
            {
                waiting[i] = true;
                timers[i]  = 0f;
            }

            // Count down and respawn
            if (waiting[i])
            {
                timers[i] += Time.deltaTime;
                if (timers[i] >= respawnDelay)
                {
                    waiting[i] = false;
                    SpawnAt(i);
                }
            }
        }
    }

    void SpawnAt(int index)
    {
        if (spawnPoints[index] == null) return;

        Vector3 pos  = spawnPoints[index].position;
        bool isHealth = Random.value <= healthChance;

        GameObject go;
        if (isHealth)
        {
            go = new GameObject("HealthPickup");
            go.transform.position = pos;
            go.AddComponent<HealthPickup>().Init(healAmount);
        }
        else
        {
            go = new GameObject("AmmoPickup");
            go.transform.position = pos;
            go.AddComponent<AmmoPickup>().Init(ammoAmount);
        }

        activePickups[index] = go;
    }

    void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;
        foreach (var pt in spawnPoints)
        {
            if (pt == null) continue;
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.8f);
            Gizmos.DrawWireSphere(pt.position, 0.4f);
            Gizmos.DrawLine(pt.position, pt.position + Vector3.up * 0.6f);
        }
    }
}

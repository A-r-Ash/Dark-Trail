using UnityEngine;

// Respawns a pickup after a delay when the player collects it.
// Attach to any GameObject; assign the pickup prefab.
public class PickupSpawner : MonoBehaviour
{
    [SerializeField] private PickupBase pickupPrefab;
    [SerializeField] private float      respawnDelay = 30f;
    [SerializeField] private bool       spawnOnStart = true;

    private PickupBase activePickup;
    private float      respawnTimer;
    private bool       waiting;

    void Start()
    {
        if (spawnOnStart)
            SpawnPickup();
    }

    void Update()
    {
        if (!waiting) return;

        // Check if the active pickup was collected (destroyed)
        if (activePickup == null)
        {
            respawnTimer += Time.deltaTime;
            if (respawnTimer >= respawnDelay)
            {
                waiting       = false;
                respawnTimer  = 0f;
                SpawnPickup();
            }
        }
        else
        {
            // Pickup still alive — reset timer
            respawnTimer = 0f;
        }
    }

    void SpawnPickup()
    {
        if (pickupPrefab == null) return;
        activePickup = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
        waiting      = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}

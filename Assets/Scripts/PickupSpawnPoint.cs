using UnityEngine;

// Place this on empty GameObjects along your trails.
// WorldSpawner will pick from these instead of random raycasts.
public class PickupSpawnPoint : MonoBehaviour
{
    [SerializeField] private bool healthAllowed = true;
    [SerializeField] private bool ammoAllowed   = true;

    public bool HealthAllowed => healthAllowed;
    public bool AmmoAllowed   => ammoAllowed;

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.5f);
        Gizmos.DrawSphere(transform.position, 0.3f);
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
    }
}

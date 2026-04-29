using UnityEngine;

// Fired by RangedCreature. Travels toward a fixed world target, damages player on contact.
[RequireComponent(typeof(Rigidbody))]
public class CreatureProjectile : MonoBehaviour
{
    [SerializeField] private float damage    = 20f;
    [SerializeField] private float speed     = 8f;
    [SerializeField] private float lifetime  = 4f;

    private Vector3    targetPos;
    private Rigidbody  rb;
    //private bool       hit;

    void Awake()
    {
        rb           = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
    }

    public void Launch(Vector3 worldTarget)
    {
        targetPos = worldTarget;
        targetPos.y = transform.position.y;

        Vector3 dir = (targetPos - transform.position).normalized;
        rb.linearVelocity = dir * speed;
        transform.rotation = Quaternion.LookRotation(dir);

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        //if (hit) return;

        if (other.CompareTag("Player"))
        {
            //hit = true;
            var health = other.GetComponent<PlayerHealth>();
            if (health == null) health = other.GetComponentInParent<PlayerHealth>();
            health?.TakeDamage(Mathf.RoundToInt(damage));
            Destroy(gameObject);
            return;
        }

        // Destroy on any solid (non-trigger) hit that isn't a creature
        if (!other.isTrigger && other.GetComponentInParent<CreatureBase>() == null)
        {
            //hit = true;
            Destroy(gameObject);
        }
    }
}

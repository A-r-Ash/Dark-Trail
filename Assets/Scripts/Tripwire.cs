using System.Collections;
using UnityEngine;

// Place two child Transforms named "AnchorA" and "AnchorB" to define the wire endpoints.
// The script draws a LineRenderer laser between them and checks for player intersection each frame.
public class Tripwire : MonoBehaviour
{
    [Header("Wire")]
    [SerializeField] private Transform anchorA;
    [SerializeField] private Transform anchorB;
    [SerializeField] private Color     laserColor  = new Color(0.9f, 0.1f, 0.1f, 0.85f);
    [SerializeField] private float     laserWidth  = 0.025f;

    [Header("Explosion")]
    [SerializeField] private float explosionDelay   = 1f;
    [SerializeField] private float explosionRadius  = 4f;
    [SerializeField] private float explosionDamage  = 40f;
    [SerializeField] private float rearmDelay       = 8f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem explosionParticles;

    [Header("Audio")]
    [SerializeField] private AudioClip triggerSound;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private AudioClip rearmSound;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    private LineRenderer lr;
    private AudioSource  audioSource;
    private bool         isArmed = true;

    void Awake()
    {
        EnsureAnchors();
        BuildLineRenderer();

        audioSource            = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    void Update()
    {
        if (!isArmed) return;
        UpdateLaser();
        CheckIntersection();
    }

    void EnsureAnchors()
    {
        if (anchorA == null)
        {
            var ga = new GameObject("AnchorA");
            ga.transform.SetParent(transform, false);
            ga.transform.localPosition = new Vector3(-2f, 0.3f, 0f);
            anchorA = ga.transform;
        }
        if (anchorB == null)
        {
            var gb = new GameObject("AnchorB");
            gb.transform.SetParent(transform, false);
            gb.transform.localPosition = new Vector3(2f, 0.3f, 0f);
            anchorB = gb.transform;
        }
    }

    void BuildLineRenderer()
    {
        lr = gameObject.AddComponent<LineRenderer>();
        lr.positionCount    = 2;
        lr.startWidth       = laserWidth;
        lr.endWidth         = laserWidth;
        lr.useWorldSpace    = true;

        var mat = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Legacy Shaders/Diffuse"));
        mat.color = laserColor;
        lr.material = mat;

        UpdateLaser();
    }

    void UpdateLaser()
    {
        if (anchorA == null || anchorB == null) return;
        lr.SetPosition(0, anchorA.position);
        lr.SetPosition(1, anchorB.position);
    }

    void CheckIntersection()
    {
        if (anchorA == null || anchorB == null) return;

        Collider[] hits = Physics.OverlapCapsule(anchorA.position, anchorB.position, 0.15f);
        foreach (var col in hits)
        {
            if (col.CompareTag("Player"))
            {
                StartCoroutine(TriggerExplosion(col.gameObject));
                return;
            }
        }
    }

    IEnumerator TriggerExplosion(GameObject triggerGo)
    {
        isArmed    = false;
        lr.enabled = false;

        if (triggerSound != null)
            audioSource.PlayOneShot(triggerSound, volume);

        yield return new WaitForSeconds(explosionDelay);

        if (explosionSound != null)
            audioSource.PlayOneShot(explosionSound, volume);

        Vector3 centre = (anchorA.position + anchorB.position) * 0.5f;

        if (explosionParticles != null)
        {
            explosionParticles.transform.position = centre;
            explosionParticles.Play();
        }

        Collider[] hits = Physics.OverlapSphere(centre, explosionRadius);
        foreach (var col in hits)
        {
            if (col.CompareTag("Player"))
            {
                var health = col.GetComponent<PlayerHealth>() ?? col.GetComponentInParent<PlayerHealth>();
                health?.TakeDamage(Mathf.RoundToInt(explosionDamage));
            }
            else
            {
                var creature = col.GetComponentInParent<CreatureBase>();
                creature?.TakeDamage(explosionDamage);
            }
        }

        yield return new WaitForSeconds(rearmDelay);

        isArmed    = true;
        lr.enabled = true;

        if (rearmSound != null)
            audioSource.PlayOneShot(rearmSound, volume);
    }

    void OnDrawGizmosSelected()
    {
        if (anchorA == null || anchorB == null) return;
        Gizmos.color = isArmed ? new Color(1f, 0.1f, 0.1f, 0.8f) : new Color(0.5f, 0.5f, 0.5f, 0.4f);
        Gizmos.DrawLine(anchorA.position, anchorB.position);
        Gizmos.DrawWireSphere((anchorA.position + anchorB.position) * 0.5f, explosionRadius);
    }
}

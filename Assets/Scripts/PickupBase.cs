using System.Collections;
using UnityEngine;

// Glowing, spinning gem pickup. Subclasses implement OnPickup().
[RequireComponent(typeof(SphereCollider))]
public abstract class PickupBase : MonoBehaviour
{
    [Header("Appearance")]
    protected Color gemColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private   float  spinSpeed   = 90f;
    [SerializeField] private   float  bobHeight   = 0.18f;
    [SerializeField] private   float  bobSpeed    = 1.4f;
    [SerializeField] private   float  glowIntensity = 1.2f;

    [Header("Collection")]
    [SerializeField] private float collectRadius = 0.6f;
    [SerializeField] private AudioClip pickupSound;

    private Light     glow;
    private Transform gem;
    private float     startY;
    private bool      collected;
    private AudioSource audioSrc;

    protected virtual void Awake()
    {
        var col       = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = collectRadius;

        BuildGem();

        audioSrc            = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;
    }

    void Start()
    {
        startY = transform.position.y;
    }

    void Update()
    {
        if (collected) return;

        float y = startY + Mathf.Sin(Time.time * bobSpeed * Mathf.PI * 2f) * bobHeight;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        gem.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        GameObject playerRoot = GetPlayerRoot(other);
        if (playerRoot == null) return;
        if (!CanPickup(playerRoot))
        {
            var msg = CannotPickupMessage;
            if (msg != null) PickupNotification.Show(msg);
            return;
        }
        collected = true;
        OnPickup(playerRoot);
        StartCoroutine(CollectRoutine());
    }

    static GameObject GetPlayerRoot(Collider other)
    {
        if (other.CompareTag("Player")) return other.gameObject;
        var ctrl = other.GetComponentInParent<PlayerController>();
        return ctrl != null ? ctrl.gameObject : null;
    }

    protected virtual bool   CanPickup(GameObject player)  => true;
    protected virtual string CannotPickupMessage            => null;
    protected abstract void  OnPickup(GameObject player);

    IEnumerator CollectRoutine()
    {
        if (pickupSound != null && audioSrc != null)
            audioSrc.PlayOneShot(pickupSound);

        // Pop + fade out over 0.25s
        float t = 0f;
        Vector3 baseScale = gem.localScale;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float s = 1f + t * 3f;
            gem.localScale = baseScale * s;
            if (glow != null) glow.intensity = Mathf.Lerp(glowIntensity, 0f, t / 0.25f);
            foreach (var r in gem.GetComponentsInChildren<Renderer>())
            {
                var c = r.material.color;
                r.material.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, t / 0.25f));
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    void BuildGem()
    {
        gem = new GameObject("Gem").transform;
        gem.SetParent(transform, false);
        gem.localPosition = Vector3.zero;

        // Octahedron approximation: two pyramids sharing a base (6 primitives is too many, use scaled cubes)
        CreateGemFacet(gem, new Vector3(0f, 0.18f, 0f),  new Vector3(0.22f, 0.28f, 0.22f), Quaternion.identity);
        CreateGemFacet(gem, new Vector3(0f, -0.18f, 0f), new Vector3(0.22f, 0.28f, 0.22f), Quaternion.Euler(180f, 0f, 0f));

        // Point light glow
        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(gem, false);
        glowGo.transform.localPosition = Vector3.zero;
        glow              = glowGo.AddComponent<Light>();
        glow.type         = LightType.Point;
        glow.color        = gemColor;
        glow.intensity    = glowIntensity;
        glow.range        = 3f;
        glow.shadows      = LightShadows.None;
    }

    void CreateGemFacet(Transform parent, Vector3 localPos, Vector3 scale, Quaternion rot)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        go.transform.localRotation = rot;

        var mat = go.GetComponent<Renderer>().material;
        mat.color = gemColor;
    }
}

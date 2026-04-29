using System.Collections;
using UnityEngine;

// Bear trap: procedural jaw visuals, triggers on player contact, immobilizes 2s.
[RequireComponent(typeof(SphereCollider))]
public class BearTrap : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float immobilizeDuration = 2f;
    [SerializeField] private float rearmDelay         = 5f;
    [SerializeField] private float damage             = 10f;
    [SerializeField] private float triggerRadius      = 0.4f;

    [Header("Audio")]
    [SerializeField] private AudioClip triggerSound;
    [SerializeField] private AudioClip rearmSound;

    [Header("Visuals")]
    [SerializeField] private Color armedColor    = new Color(0.55f, 0.08f, 0.08f);
    [SerializeField] private Color triggeredColor = new Color(0.25f, 0.25f, 0.25f);

    private bool        isArmed = true;
    private bool        isSnapped;
    private AudioSource audioSource;

    private Transform jawTop;
    private Transform jawBottom;
    private Renderer  jawTopRenderer;
    private Renderer  jawBottomRenderer;

    void Awake()
    {
        var col       = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = triggerRadius;

        audioSource             = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        BuildVisuals();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isArmed || isSnapped) return;
        if (!other.CompareTag("Player")) return;

        StartCoroutine(SnapSequence(other.gameObject));
    }

    IEnumerator SnapSequence(GameObject playerGo)
    {
        isArmed  = false;
        isSnapped = true;
        SetColor(triggeredColor);
        ClosejJaws();
        if (triggerSound != null) audioSource.PlayOneShot(triggerSound);

        var health = playerGo.GetComponent<PlayerHealth>() ?? playerGo.GetComponentInParent<PlayerHealth>();
        health?.TakeDamage(Mathf.RoundToInt(damage));

        var controller = playerGo.GetComponent<PlayerController>() ?? playerGo.GetComponentInParent<PlayerController>();
        controller?.SetImmobilized(true, immobilizeDuration);

        yield return new WaitForSeconds(rearmDelay);

        isSnapped = false;
        isArmed   = true;
        SetColor(armedColor);
        OpenJaws();
        if (rearmSound != null) audioSource.PlayOneShot(rearmSound);
    }

    // ── Procedural visuals ────────────────────────────────────────────────────

    void BuildVisuals()
    {
        // Base plate
        var plate = CreateBox("Plate", transform, new Vector3(0f, -0.04f, 0f), new Vector3(0.55f, 0.04f, 0.4f));
        plate.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.3f);

        // Top jaw
        jawTop = CreateBox("JawTop", transform, new Vector3(0f, 0.06f, 0.1f), new Vector3(0.5f, 0.05f, 0.2f)).transform;
        jawTop.localEulerAngles = new Vector3(-20f, 0f, 0f);
        jawTopRenderer = jawTop.GetComponent<Renderer>();

        // Bottom jaw
        jawBottom = CreateBox("JawBottom", transform, new Vector3(0f, 0.06f, -0.1f), new Vector3(0.5f, 0.05f, 0.2f)).transform;
        jawBottom.localEulerAngles = new Vector3(20f, 0f, 0f);
        jawBottomRenderer = jawBottom.GetComponent<Renderer>();

        SetColor(armedColor);
    }

    GameObject CreateBox(string n, Transform parent, Vector3 localPos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.name = n;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = size;
        return go;
    }

    void SetColor(Color c)
    {
        if (jawTopRenderer    != null) jawTopRenderer.material.color    = c;
        if (jawBottomRenderer != null) jawBottomRenderer.material.color = c;
    }

    void ClosejJaws()
    {
        if (jawTop    != null) jawTop.localEulerAngles    = new Vector3(5f, 0f, 0f);
        if (jawBottom != null) jawBottom.localEulerAngles = new Vector3(-5f, 0f, 0f);
    }

    void OpenJaws()
    {
        if (jawTop    != null) jawTop.localEulerAngles    = new Vector3(-20f, 0f, 0f);
        if (jawBottom != null) jawBottom.localEulerAngles = new Vector3(20f, 0f, 0f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isArmed ? new Color(1f, 0.2f, 0.2f, 0.4f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}

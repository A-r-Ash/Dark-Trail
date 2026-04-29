using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CheckpointLight : MonoBehaviour
{
    [Header("Light Settings")]
    [SerializeField] private Light checkpointLight;
    [SerializeField] private float flickerIntensity = 100f;
    [SerializeField] private float steadyIntensity  = 150f;

    [Header("Flicker Timing")]
    [SerializeField] private float minOnTime  = 0.1f;
    [SerializeField] private float maxOnTime  = 0.5f;
    [SerializeField] private float minOffTime = 0.05f;
    [SerializeField] private float maxOffTime = 0.3f;

    [Header("Activation")]
    [SerializeField] private float activationRange = 3f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   activatedSound;

    private bool      isActivated = false;
    private bool      playerInRange = false;
    private Transform playerTransform;
    private Coroutine flickerCoroutine;

    void Start()
    {
        if (checkpointLight == null)
            checkpointLight = GetComponent<Light>();

        if (checkpointLight == null)
        {
            Debug.LogError("CheckpointLight: No Light component found!");
            enabled = false;
            return;
        }

        var trigger      = gameObject.AddComponent<SphereCollider>();
        trigger.radius   = activationRange;
        trigger.isTrigger = true;

        flickerCoroutine = StartCoroutine(FlickerLight());
    }

    void Update()
    {
        if (!playerInRange || isActivated) return;
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            ActivateCheckpoint();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other, out Transform root)) return;
        playerInRange   = true;
        playerTransform = root;
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other, out _)) return;
        playerInRange   = false;
        playerTransform = null;
    }

    static bool IsPlayer(Collider other, out Transform root)
    {
        if (other.CompareTag("Player")) { root = other.transform; return true; }
        var ctrl = other.GetComponentInParent<PlayerController>();
        if (ctrl != null) { root = ctrl.transform; return true; }
        root = null;
        return false;
    }

    void ActivateCheckpoint()
    {
        isActivated = true;

        if (flickerCoroutine != null)
            StopCoroutine(flickerCoroutine);

        checkpointLight.intensity = steadyIntensity;

        if (audioSource != null)
        {
            audioSource.Stop();
            if (activatedSound != null)
                audioSource.PlayOneShot(activatedSound);
        }

        Vector3 savePos = playerTransform != null ? playerTransform.position : transform.position;
        GameManager.Instance?.RegisterCheckpoint(savePos);
    }

    IEnumerator FlickerLight()
    {
        while (true)
        {
            checkpointLight.intensity = flickerIntensity;
            yield return new WaitForSeconds(Random.Range(minOnTime, maxOnTime));
            checkpointLight.intensity = 0f;
            yield return new WaitForSeconds(Random.Range(minOffTime, maxOffTime));
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRange);
    }
}

using UnityEngine;

public class SpeedyCreature : CreatureBase
{
    [Header("Speedy Settings")]
    [SerializeField] private float soundInterval = 1.5f; // How often it makes noise (seconds)
    [SerializeField] private AudioClip[] chaseSounds; // Footsteps, growls, breathing
    [SerializeField] private GameObject visualIndicator; // Pulse/glow effect
    [SerializeField] private float indicatorDuration = 0.3f; // How long indicator shows

    private AudioSource audioSource;
    private float soundTimer = 0f;
    private float indicatorTimer = 0f;

    protected override void Start()
    {
        base.Start();

        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.minDistance = 3f; // Full volume within 3 units (very close)
        audioSource.maxDistance = 50f; // Can hear from farther away
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // More realistic fade
        audioSource.volume = 0.8f;

        // Hide visual indicator initially
        if (visualIndicator != null)
        {
            visualIndicator.SetActive(false);
        }
    }

    public override void ReactToLight(Light flashlight, float playerDistance)
    {
        if (!afraidOfLight || flashlight == null) return;

        // Flee at HALF the flashlight range (retreat earlier than SmallCreature)
        float earlyFleeRange = flashlight.range * 0.5f;

        if (IsInLightCone(flashlight) && playerDistance <= earlyFleeRange)
        {
            currentState = CreatureState.Retreating;
        }
    }

    protected override void Update()
    {
        base.Update();

        // Make noise while chasing
        if (currentState == CreatureState.Chasing)
        {
            soundTimer += Time.deltaTime;

            if (soundTimer >= soundInterval)
            {
                MakeNoise();
                soundTimer = 0f;
            }
        }
        else
        {
            soundTimer = 0f; // Reset when not chasing
        }

        // Handle visual indicator
        if (indicatorTimer > 0f)
        {
            indicatorTimer -= Time.deltaTime;

            if (indicatorTimer <= 0f && visualIndicator != null)
            {
                visualIndicator.SetActive(false);
            }
        }
    }

    void MakeNoise()
    {
        // Play random chase sound
        if (chaseSounds != null && chaseSounds.Length > 0 && audioSource != null)
        {
            AudioClip randomSound = chaseSounds[Random.Range(0, chaseSounds.Length)];
            audioSource.PlayOneShot(randomSound);
        }

        // Show visual indicator
        if (visualIndicator != null)
        {
            visualIndicator.SetActive(true);
            indicatorTimer = indicatorDuration;
        }

        // Debug log for testing (remove later)
        Debug.Log($"{gameObject.name} made noise at {transform.position}");
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw the early flee range (half of flashlight range)
        if (player != null && playerFlashlight != null)
        {
            Gizmos.color = Color.magenta;
            float earlyFleeRange = playerFlashlight.range * 0.5f;
            Gizmos.DrawWireSphere(player.position, earlyFleeRange);
        }
    }
}
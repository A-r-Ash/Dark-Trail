using UnityEngine;
using UnityEngine.InputSystem;

// Handles boost input and lerps range/spotAngle.
// BatterySystem reads IsBoosting and BoostedIntensity to apply the final light values.
public class FlashlightBoost : MonoBehaviour
{
    [Header("Boost Settings")]
    [SerializeField] private float boostedIntensity = 2000f;
    [SerializeField] private float boostedRange     = 30f;
    [SerializeField] private float boostedSpotAngle = 60f;

    [Header("Normal Settings")]
    [SerializeField] private float normalRange      = 13f;
    [SerializeField] private float normalSpotAngle  = 0f;

    [Header("Transition")]
    [SerializeField] private float transitionSpeed  = 10f;

    private bool  isBoosting;
    private float currentRange;
    private float currentSpotAngle;

    public bool  IsBoosting       => isBoosting;
    public float BoostedIntensity => boostedIntensity;
    public float CurrentRange     => currentRange;
    public float CurrentSpotAngle => currentSpotAngle;

    void Start()
    {
        currentRange     = normalRange;
        currentSpotAngle = normalSpotAngle;
    }

    void Update()
    {
        if (Keyboard.current != null)
            isBoosting = Keyboard.current.fKey.isPressed;

        float tRange = isBoosting ? boostedRange     : normalRange;
        float tAngle = isBoosting ? boostedSpotAngle : normalSpotAngle;

        float dt = Time.deltaTime * transitionSpeed;
        currentRange      = Mathf.Lerp(currentRange,     tRange, dt);
        currentSpotAngle  = Mathf.Lerp(currentSpotAngle, tAngle, dt);
    }
}

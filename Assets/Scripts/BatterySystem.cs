using UnityEngine;
using UnityEngine.Events;

// Single owner of flashlight.intensity, range, and spotAngle.
// FlashlightBoost feeds multipliers; this script applies them scaled by battery %.
public class BatterySystem : MonoBehaviour
{
    [Header("Battery")]
    [SerializeField] private float maxBattery       = 100f;
    [SerializeField] private float batteryDrainRate = 0.5f;

    [Header("Flashlight — base values at full battery, no boost")]
    [SerializeField] private Light  flashlight;
    [SerializeField] private float  baseIntensity  = 800f;
    [SerializeField] private float  baseRange      = 13f;
    [SerializeField] private float  baseSpotAngle  = 0f;

    [Header("Events")]
    public UnityEvent OnBatteryDepleted = new UnityEvent();

    private float           currentBattery;
    private bool            depleted;
    private FlashlightBoost boost;

    public float CurrentBattery    => currentBattery;
    public float BatteryPercentage => maxBattery > 0f ? currentBattery / maxBattery : 0f;

    void Awake()
    {
        currentBattery = maxBattery;
        boost = GetComponentInChildren<FlashlightBoost>();
        if (boost == null) boost = FindFirstObjectByType<FlashlightBoost>();
    }

    void Update()
    {
        Drain();
        Apply();
    }

    void Drain()
    {
        if (depleted || currentBattery <= 0f) return;
        currentBattery = Mathf.Max(0f, currentBattery - batteryDrainRate * Time.deltaTime);
        if (currentBattery <= 0f)
        {
            depleted = true;
            OnBatteryDepleted?.Invoke();
        }
    }

    void Apply()
    {
        if (flashlight == null) return;

        bool boosting = boost != null && boost.IsBoosting;
        float targetIntensity = boosting
            ? boost.BoostedIntensity
            : baseIntensity * BatteryPercentage;

        flashlight.intensity = Mathf.Lerp(flashlight.intensity, targetIntensity, Time.deltaTime * 10f);
        flashlight.range     = boost != null ? boost.CurrentRange     : baseRange;
        flashlight.spotAngle = boost != null ? boost.CurrentSpotAngle : baseSpotAngle;
    }

    public void AddBattery(float amount)
    {
        currentBattery = Mathf.Min(currentBattery + amount, maxBattery);
        if (currentBattery > 0f) depleted = false;
    }

    public void SetBattery(float amount)
    {
        currentBattery = Mathf.Clamp(amount, 0f, maxBattery);
        if (currentBattery > 0f) depleted = false;
    }
}

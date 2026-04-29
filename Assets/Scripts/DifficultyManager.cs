using UnityEngine;
using UnityEngine.Events;

// Singleton that tracks threat level 1-5 and increments it over time.
// CreatureSpawner and DifficultyIndicator both read from here.
public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Difficulty")]
    [SerializeField] private float escalationInterval = 60f;   // seconds per threat tier
    [SerializeField] private int   maxThreat          = 5;

    public int   ThreatLevel  { get; private set; } = 1;
    public float LevelProgress => Mathf.Clamp01(timer / escalationInterval);  // 0-1 within current tier

    public UnityEvent<int> OnThreatChanged = new UnityEvent<int>();

    private float timer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (ThreatLevel >= maxThreat) return;

        timer += Time.deltaTime;
        if (timer >= escalationInterval)
        {
            timer = 0f;
            ThreatLevel = Mathf.Min(ThreatLevel + 1, maxThreat);
            OnThreatChanged?.Invoke(ThreatLevel);
        }
    }
}

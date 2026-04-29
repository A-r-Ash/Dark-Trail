using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform playerStartLocation;

    public UnityEvent OnGameOver    = new UnityEvent();
    public UnityEvent OnGameRestart = new UnityEvent();

    // Checkpoint state — memory only, never persisted to disk
    private Vector3 lastCheckpointPosition;
    private int     savedHealth;
    private int     savedAmmo;
    private int     savedReserve;
    private float   savedBattery;
    private bool    hasCheckpoint = false;
    private bool    isGameOver    = false;

    public bool    HasCheckpoint          => hasCheckpoint;
    public Vector3 LastCheckpointPosition => lastCheckpointPosition;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (playerTransform == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                var battery = playerObj.GetComponentInChildren<BatterySystem>();
                if (battery != null) battery.OnBatteryDepleted.AddListener(TriggerGameOver);
            }
        }

        // Always spawn at the designated start location — checkpoint is session-only
        if (playerTransform != null && playerStartLocation != null)
            playerTransform.position = playerStartLocation.position;
    }

    // ── Game over ─────────────────────────────────────────────────────────────

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        Time.timeScale = 0f;
        OnGameOver?.Invoke();
    }

    // ── Checkpoint ────────────────────────────────────────────────────────────

    // Called by CheckpointLight when player presses E — snapshots full player state
    public void RegisterCheckpoint(Vector3 position)
    {
        lastCheckpointPosition = position;
        hasCheckpoint = true;

        if (playerTransform == null) return;

        var health = playerTransform.GetComponent<PlayerHealth>()
                  ?? playerTransform.GetComponentInChildren<PlayerHealth>();
        if (health != null) savedHealth = health.CurrentHealth;

        var gun = playerTransform.GetComponentInChildren<GunBase>();
        if (gun != null) { savedAmmo = gun.CurrentAmmo; savedReserve = gun.ReserveAmmo; }

        var battery = playerTransform.GetComponentInChildren<BatterySystem>();
        if (battery != null) savedBattery = battery.CurrentBattery;

        Debug.Log("Checkpoint Hit, saved");
    }

    // In-place respawn — no scene reload, restores snapshotted state
    public void RespawnAtCheckpoint()
{
    if (!hasCheckpoint) return;

    Time.timeScale = 1f;
    isGameOver = false;
    
    OnGameRestart?.Invoke();  // ← Fire FIRST so canvas can hide itself
    
    // THEN restore player state
    if (playerTransform == null)
    {
        var obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null) playerTransform = obj.transform;
    }

    if (playerTransform != null)
    {
        var rb = playerTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Temporarily disable interpolation so the visual position snaps with
            // the physics position instantly — prevents the flashlight from appearing
            // to fly away toward the camera's old interpolated location.
            var prevInterp   = rb.interpolation;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.position      = lastCheckpointPosition;
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.interpolation = prevInterp;
        }
        else
        {
            playerTransform.position = lastCheckpointPosition;
        }

        var health = playerTransform.GetComponent<PlayerHealth>()
                  ?? playerTransform.GetComponentInChildren<PlayerHealth>();
        health?.Revive(savedHealth);

        var gun = playerTransform.GetComponentInChildren<GunBase>();
        gun?.SetAmmo(savedAmmo, savedReserve);

        var battery = playerTransform.GetComponentInChildren<BatterySystem>();
        battery?.SetBattery(savedBattery);
    }

    Debug.Log("Respawn successful");
}

    // Full restart — clears checkpoint, reloads scene, player starts fresh
    public void RestartGame()
    {
        ClearCheckpoint();
        Time.timeScale = 1f;
        isGameOver = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ClearCheckpoint()
    {
        hasCheckpoint          = false;
        lastCheckpointPosition = Vector3.zero;
        savedHealth = 0; savedAmmo = 0; savedReserve = 0; savedBattery = 0f;
    }
}

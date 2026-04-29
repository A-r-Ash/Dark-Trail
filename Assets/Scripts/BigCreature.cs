using UnityEngine;

public class BigCreature : CreatureBase
{
    [Header("Charge")]
    [SerializeField] private float chargeSpeed         = 18f;   // speed during charge
    [SerializeField] private float windupDuration      = 0.85f; // pause before charge (telegraph)
    [SerializeField] private float chargeMaxDistance   = 14f;   // charge ends after this distance
    [SerializeField] private float recoverDuration     = 1.2f;  // pause after charge ends
    [SerializeField] private float lightSlowMultiplier = 0.22f; // fraction of chargeSpeed when lit

    [Header("Sounds")]
    [SerializeField] private AudioClip roarClip;    // plays on windup
    [SerializeField] private AudioClip impactClip;  // plays on hitting player
    [SerializeField] private AudioClip stompClip;   // optional looping footstep during charge

    // ── Internal state ────────────────────────────────────────────────────────

    private enum BigState { Idle, Windup, Charging, Recovering }
    private BigState bigState   = BigState.Idle;
    private Vector3  chargeDir;
    private Vector3  chargeOrigin;
    private float    stateTimer;
    private bool     isLit;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void Start()
    {
        base.Start();
        afraidOfLight = false; // does NOT retreat — slows instead
    }

    protected override void Update()
    {
        if (isDead || player == null) return;

        isLit = playerFlashlight != null && IsInLightCone(playerFlashlight);

        float dist = Vector3.Distance(transform.position, player.position);

        switch (bigState)
        {
            case BigState.Idle:
                if (dist <= detectionRange)
                    EnterWindup();
                break;

            case BigState.Windup:
                stateTimer -= Time.deltaTime;
                FaceDir((player.position - transform.position));
                if (stateTimer <= 0f)
                    EnterCharge();
                break;

            case BigState.Charging:
                DoCharge(dist);
                break;

            case BigState.Recovering:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                    bigState = dist <= detectionRange ? BigState.Idle : BigState.Idle;
                break;
        }

        // Keep base animator in sync using existing bools
        currentState = (bigState == BigState.Charging || bigState == BigState.Windup)
            ? CreatureState.Chasing
            : CreatureState.Idle;
        SyncAnimator();
    }

    // ── Charge state machine ──────────────────────────────────────────────────

    void EnterWindup()
    {
        bigState   = BigState.Windup;
        stateTimer = windupDuration;
        if (roarClip != null) audioSource.PlayOneShot(roarClip);
    }

    void EnterCharge()
    {
        bigState     = BigState.Charging;
        chargeDir    = (player.position - transform.position);
        chargeDir.y  = 0f;
        chargeDir    = chargeDir.normalized;
        chargeOrigin = transform.position;
        if (stompClip != null) audioSource.PlayOneShot(stompClip);
    }

    void DoCharge(float distToPlayer)
    {
        // Light slows but does not stop the charge
        float speed = isLit ? chargeSpeed * lightSlowMultiplier : chargeSpeed;

        transform.position += chargeDir * speed * Time.deltaTime;
        FaceDir(chargeDir);

        // Hit player
        if (distToPlayer <= killRange)
        {
            OnHitPlayer();
            EnterRecover();
            return;
        }

        // Overshot — charge exhausted
        if (Vector3.Distance(transform.position, chargeOrigin) >= chargeMaxDistance)
            EnterRecover();
    }

    void EnterRecover()
    {
        bigState   = BigState.Recovering;
        stateTimer = recoverDuration;
        SetTrigger("Hurt"); // reuse stagger animation for recovery stumble
    }

    void OnHitPlayer()
    {
        var health = player.GetComponent<PlayerHealth>()
                  ?? player.GetComponentInParent<PlayerHealth>();
        health?.TakeDamage(1);
        if (impactClip != null) audioSource.PlayOneShot(impactClip);
    }

    // ── Overrides ─────────────────────────────────────────────────────────────

    // Light is handled via isLit flag in Update — no retreating
    public override void ReactToLight(Light flashlight, float playerDistance) { }

    // Immune to generic damage, only bullets work
    public override void TakeDamage(float damage) { }
    public override void TakeBulletDamage(float damage) => base.TakeDamage(damage);

    // ── Helpers ───────────────────────────────────────────────────────────────

    void FaceDir(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}

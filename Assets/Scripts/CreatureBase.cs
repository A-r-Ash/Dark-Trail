using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CreatureBase : MonoBehaviour, ICreature
{
    [Header("Movement")]
    [SerializeField] protected float chaseSpeed   = 3f;
    [SerializeField] protected float retreatSpeed = 6f;

    [Header("Detection")]
    [SerializeField] protected float detectionRange        = 60f;
    [SerializeField] protected float killRange             = 1f;
    [SerializeField] protected float flashlightRangeOffset = 2f;

    [Header("Combat")]
    [SerializeField] private float attackCooldown = 1f;
    private float lastAttackTime;
    

    [Header("Health")]
    [SerializeField] protected float maxHealth = 100f;

    [Header("Light Settings")]
    [SerializeField] protected bool afraidOfLight = true;

    [Header("Audio")]
    [SerializeField] protected AudioClip hurtAudioClip;
    [SerializeField] protected AudioClip deathAudioClip;
    [SerializeField] protected AudioClip footstepAudioClip;
    [SerializeField] protected float     footstepInterval = 0.5f;

    [Header("References")]
    [SerializeField] protected Transform player;
    [SerializeField] protected Light     playerFlashlight;

    protected AudioSource audioSource;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Animation Clips")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip chaseClip;
    [SerializeField] private AnimationClip retreatClip;
    [SerializeField] private AnimationClip attackClip;
    [SerializeField] private AnimationClip hurtClip;
    [SerializeField] private AnimationClip dieClip;

    // ── State ─────────────────────────────────────────────────────────────────

    protected enum CreatureState { Idle, Chasing, Retreating, Attacking }
    protected CreatureState currentState = CreatureState.Idle;
    protected Vector3 spawnPosition;
    protected float   currentHealth;
    protected bool    isDead;
    private   float   footstepTimer;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected virtual void Start()
    {
        spawnPosition = transform.position;
        currentHealth = maxHealth;

        audioSource             = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake  = false;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        SetupOverrides();

        if (player == null)
        {
            var obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) player = obj.transform;
        }

        if (playerFlashlight == null)
            playerFlashlight = FindFirstObjectByType<Light>();

        SyncAnimator();
    }

    protected virtual void Update()
    {   

        
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        ReactToLight(playerFlashlight, dist);
        DetectPlayer(player);
        ExecuteState();

        if (currentState == CreatureState.Chasing && CanKillPlayer())
            {
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    AttackPlayer();
                    lastAttackTime = Time.time;
                }
            }

        TickFootstep();
    }

    // ── ICreature ─────────────────────────────────────────────────────────────

    public virtual void DetectPlayer(Transform playerTransform)
    {
        if (currentState == CreatureState.Retreating) return;
        float d = Vector3.Distance(transform.position, playerTransform.position);
        currentState = d <= detectionRange ? CreatureState.Chasing : CreatureState.Idle;
    }

    public virtual void ChasePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        transform.position += dir * chaseSpeed * Time.deltaTime;
        if (dir.magnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
    }

    public virtual void StopChasing()
    {
        if (currentState == CreatureState.Chasing)
            currentState = CreatureState.Idle;
    }

    public virtual void ReactToLight(Light flashlight, float playerDistance)
    {
        if (!afraidOfLight || flashlight == null) return;
        float fleeRange = flashlight.range - flashlightRangeOffset;
        if (IsInLightCone(flashlight) && playerDistance <= fleeRange)
            currentState = CreatureState.Retreating;
    }

    public virtual bool IsAfraidOfLight() => afraidOfLight;

    public virtual void AttackPlayer()
    {
         if (animator != null)
        animator.SetTrigger("Attack");
    
    // Deal damage to player
    PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
    if (playerHealth != null)
        playerHealth.TakeDamage(1); // or whatever damage amount
    }

    public virtual bool CanKillPlayer()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= killRange;
    }

    void TickFootstep()
    {
        if (footstepAudioClip == null) return;
        if (currentState != CreatureState.Chasing && currentState != CreatureState.Retreating) return;
        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            audioSource.PlayOneShot(footstepAudioClip);
            footstepTimer = footstepInterval;
        }
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (currentHealth <= 0f) Die();
        else
        {
            SetTrigger("Hurt");
            if (hurtAudioClip != null) audioSource.PlayOneShot(hurtAudioClip);
        }
    }

    public virtual void TakeBulletDamage(float damage) => TakeDamage(damage);

    public virtual bool IsAlive() => !isDead && gameObject.activeSelf;

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        StopAllCoroutines();
        SetTrigger("Die");
        if (deathAudioClip != null) audioSource.PlayOneShot(deathAudioClip);
        StartCoroutine(DieAfterDelay(dieClip != null ? dieClip.length : 0f));
    }

    IEnumerator DieAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    // ── State execution ───────────────────────────────────────────────────────

    protected virtual void ExecuteState()
    {
        SyncAnimator();
        switch (currentState)
        {
            case CreatureState.Chasing:
                ChasePlayer();
                break;
            case CreatureState.Retreating:
                RetreatToSpawn();
                break;
        }
    }

    protected virtual void RetreatToSpawn()
    {
        Vector3 dir = (spawnPosition - transform.position).normalized;
        dir.y = 0;
        transform.position += dir * retreatSpeed * Time.deltaTime;
        if (dir.magnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
        if (Vector3.Distance(transform.position, spawnPosition) < 0.5f)
        {
            transform.position = spawnPosition;
            currentState = CreatureState.Idle;
        }
    }

    // ── Animator ──────────────────────────────────────────────────────────────

    protected void SyncAnimator()
    {
        if (animator == null) return;
        animator.SetBool("IsChasing",    currentState == CreatureState.Chasing);
        animator.SetBool("IsRetreating", currentState == CreatureState.Retreating);
    }

    protected void SetTrigger(string param)
    {
        animator?.SetTrigger(param);
    }

    void SetupOverrides()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        var oc        = new AnimatorOverrideController(animator.runtimeAnimatorController);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(oc.overridesCount);
        oc.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip replacement = overrides[i].Key.name switch
            {
                "Idle"    => idleClip,
                "Chase"   => chaseClip,
                "Retreat" => retreatClip,
                "Attack"  => attackClip,
                "Hurt"    => hurtClip,
                "Die"     => dieClip,
                _         => null
            };
            if (replacement != null)
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, replacement);
        }

        oc.ApplyOverrides(overrides);
        animator.runtimeAnimatorController = oc;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    protected virtual bool IsInLightCone(Light flashlight)
    {
        if (flashlight == null || flashlight.intensity <= 0) return false;
        if (Vector3.Distance(transform.position, flashlight.transform.position) > flashlight.range) return false;
        Vector3 dir   = (transform.position - flashlight.transform.position).normalized;
        float   angle = Vector3.Angle(flashlight.transform.forward, dir);
        return angle < flashlight.spotAngle / 2f;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, killRange);
        if (player != null && playerFlashlight != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, playerFlashlight.range);
        }
    }

    //______________ Coroutine __________________

    
}

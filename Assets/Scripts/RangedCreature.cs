using System.Collections;
using UnityEngine;

public class RangedCreature : CreatureBase
{
    [Header("Ranged")]
    [SerializeField] private float         strafeRadius   = 8f;
    [SerializeField] private float         strafeSpeed    = 2f;
    [SerializeField] private float         fireInterval   = 3f;
    [SerializeField] private CreatureProjectile projectilePrefab;

    private float  strafeAngle;
    private float  fireTimer;
    private bool   isFiring;

    protected override void Start()
    {
        afraidOfLight = true;
        base.Start();
        strafeAngle = Random.Range(0f, 360f);
    }

    protected override void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        ReactToLight(playerFlashlight, dist);

        if (currentState != CreatureState.Retreating)
            currentState = dist <= detectionRange ? CreatureState.Chasing : CreatureState.Idle;

        ExecuteState();

        if (currentState == CreatureState.Chasing && !isFiring)
        {
            fireTimer += Time.deltaTime;
            if (fireTimer >= fireInterval)
            {
                fireTimer = 0f;
                StartCoroutine(FireProjectile());
            }
        }
    }

    protected override void ExecuteState()
    {
        SyncAnimator();
        switch (currentState)
        {
            case CreatureState.Chasing:
                Strafe();
                break;
            case CreatureState.Retreating:
                RetreatToSpawn();
                break;
        }
    }

    void Strafe()
    {
        strafeAngle += strafeSpeed * Time.deltaTime * Mathf.Rad2Deg;

        float rad = strafeAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * strafeRadius;
        Vector3 target = player.position + offset;

        Vector3 dir = (target - transform.position);
        dir.y = 0f;
        float dist = dir.magnitude;

        if (dist > 0.1f)
        {
            transform.position += dir.normalized * chaseSpeed * Time.deltaTime;
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDir.normalized);
        }
    }

    IEnumerator FireProjectile()
    {
        isFiring = true;
        SetTrigger("Attack");

        if (projectilePrefab != null && player != null)
        {
            var proj = Instantiate(projectilePrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            proj.Launch(player.position);
        }

        yield return new WaitForSeconds(0.5f);
        isFiring = false;
    }

    // RangedCreature never closes to kill range — it attacks via projectile only
    public override bool CanKillPlayer() => false;
}

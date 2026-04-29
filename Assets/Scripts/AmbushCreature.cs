using System.Collections;
using UnityEngine;

// Stays burrowed (hidden) until the player steps within triggerRange.
// Then it erupts, lunges at speed, and kills on contact.
public class AmbushCreature : CreatureBase
{
    [Header("Ambush")]
    [SerializeField] private float triggerRange  = 4f;
    [SerializeField] private float lungeSpeed    = 14f;
    [SerializeField] private float emergeTime    = 0.4f;   // animation wind-up before lunge

    private bool hasEmerged;
    private bool isLunging;

    protected override void Start()
    {
        afraidOfLight = false;
        base.Start();
        SetBurrowed(true);
    }

    protected override void Update()
    {
        if (isDead || player == null) return;

        if (!hasEmerged)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= triggerRange)
                StartCoroutine(Emerge());
            return;
        }

        // After emerging, use normal base logic (chases and kills)
        base.Update();
    }

    IEnumerator Emerge()
    {
        hasEmerged = true;
        SetTrigger("Attack");          // play emerge animation via Attack trigger
        SetBurrowed(false);
        yield return new WaitForSeconds(emergeTime);

        isLunging    = true;
        currentState = CreatureState.Chasing;
    }

    protected override void ExecuteState()
    {
        SyncAnimator();
        if (isLunging)
        {
            LungeTowardPlayer();
            return;
        }
        switch (currentState)
        {
            case CreatureState.Chasing:    ChasePlayer(); break;
            case CreatureState.Retreating: RetreatToSpawn(); break;
        }
    }

    void LungeTowardPlayer()
    {
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.magnitude > 0.1f)
        {
            transform.position += dir.normalized * lungeSpeed * Time.deltaTime;
            transform.rotation  = Quaternion.LookRotation(dir.normalized);
        }

        if (Vector3.Distance(transform.position, player.position) <= killRange)
        {
            isLunging = false;
            AttackPlayer();
        }
    }

    void SetBurrowed(bool burrowed)
    {
        // Hide all renderers when burrowed — the creature is underground
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = !burrowed;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = !burrowed;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, triggerRange);
    }
}

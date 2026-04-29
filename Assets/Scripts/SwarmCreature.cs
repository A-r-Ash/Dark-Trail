using UnityEngine;

// Tiny, fearless, 1-hit kill. Never retreats from light.
// Scale the GameObject to ~0.5x in the inspector.
public class SwarmCreature : CreatureBase
{
    protected override void Start()
    {
        afraidOfLight = false;
        base.Start();
    }

    // Override so light never triggers a retreat
    public override void ReactToLight(Light flashlight, float playerDistance) { }

    public override void AttackPlayer()
    {
        SetTrigger("Attack");
        GameManager.Instance?.TriggerGameOver();
        Die();
    }
}

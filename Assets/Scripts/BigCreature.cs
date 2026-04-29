using UnityEngine;

public class BigCreature : CreatureBase
{
    

    // ── Overrides ─────────────────────────────────────────────────────────────

    

    // Immune to everything except bullets
    public override void TakeDamage(float damage) { }
    public override void TakeBulletDamage(float damage) => base.TakeDamage(damage);
}

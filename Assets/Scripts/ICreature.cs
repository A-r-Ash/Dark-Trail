using UnityEngine;

public interface ICreature
{
    // Detection and tracking
    void DetectPlayer(Transform player);
    void ChasePlayer();
    void StopChasing();

    // Light interaction
    void ReactToLight(Light flashlight, float playerDistance);
    bool IsAfraidOfLight();

    // Combat
    void AttackPlayer();
    bool CanKillPlayer();

    // State
    bool IsAlive();
    void Die();
}
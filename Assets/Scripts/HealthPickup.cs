using UnityEngine;

public class HealthPickup : PickupBase
{
    [SerializeField] private int healAmount = 1;

    protected override void Awake()
    {
        gemColor = new Color(0.1f, 0.85f, 0.2f);   // green
        base.Awake();
    }

    protected override bool CanPickup(GameObject player)
    {
        var health = player.GetComponent<PlayerHealth>()
                  ?? player.GetComponentInParent<PlayerHealth>()
                  ?? player.GetComponentInChildren<PlayerHealth>();
        return health != null && health.CurrentHealth < health.MaxHealth;
    }

    protected override string CannotPickupMessage => "Health is full";

    protected override void OnPickup(GameObject player)
    {
        var health = player.GetComponent<PlayerHealth>()
                  ?? player.GetComponentInParent<PlayerHealth>()
                  ?? player.GetComponentInChildren<PlayerHealth>();
        health?.Heal(healAmount);
    }
}

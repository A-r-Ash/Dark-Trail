using UnityEngine;

public class HealthPickup : PickupBase
{
    [SerializeField] private Color pickupColor = new Color(0.1f, 0.85f, 0.2f);
    [SerializeField] private int   healAmount  = 1;
    public void Init(int amount) => healAmount = amount;

    protected override void Awake()
    {
        gemColor = pickupColor;
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

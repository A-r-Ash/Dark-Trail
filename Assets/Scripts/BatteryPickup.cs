using UnityEngine;

public class BatteryPickup : PickupBase
{
    [SerializeField] private float batteryAmount = 30f;

    protected override void Awake()
    {
        gemColor = new Color(0.9f, 0.85f, 0.1f);   // yellow
        base.Awake();
    }

    protected override void OnPickup(GameObject player)
    {
        var battery = player.GetComponent<BatterySystem>() ?? player.GetComponentInParent<BatterySystem>();
        battery?.AddBattery(batteryAmount);
    }
}

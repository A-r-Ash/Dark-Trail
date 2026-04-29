using UnityEngine;

public class AmmoPickup : PickupBase
{
    [SerializeField] private int ammoAmount = 12;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override bool CanPickup(GameObject player)
    {
        var gun = GetGun(player);
        if (gun == null || gun.Data == null) return true;
        return gun.ReserveAmmo < gun.Data.maxReserveAmmo;
    }

    protected override string CannotPickupMessage => "Ammo is full";

    protected override void OnPickup(GameObject player) => GetGun(player)?.AddReserveAmmo(ammoAmount);

    static GunBase GetGun(GameObject player)
    {
        var shooter = player.GetComponent<PlayerShooter>()
                   ?? player.GetComponentInParent<PlayerShooter>()
                   ?? player.GetComponentInChildren<PlayerShooter>();
        return shooter?.EquippedGun
            ?? player.GetComponent<GunBase>()
            ?? player.GetComponentInChildren<GunBase>();
    }
}

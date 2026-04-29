using UnityEngine;

[CreateAssetMenu(fileName = "NewGunData", menuName = "Dark Trail/Gun Data")]
public class GunData : ScriptableObject
{
    [Header("Identity")]
    public string gunName = "Pistol";

    [Header("Firing")]
    public bool  isAutomatic  = false;
    public float fireRate     = 6f;     // rounds per second
    public float damage       = 30f;
    public float range        = 40f;

    [Header("Ammo")]
    public int magazineSize   = 12;
    public int maxReserveAmmo = 48;

    [Header("Handling")]
    public float reloadTime   = 1.8f;
    public float drawTime     = 0.3f;   // delay before first shot after equip

    [Header("Spread (degrees)")]
    public float hipSpread    = 4f;     // inaccuracy when hip-firing
    public float adsSpread    = 0.6f;   // inaccuracy when aiming

    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptyClickSound;
    public AudioClip drawSound;
}

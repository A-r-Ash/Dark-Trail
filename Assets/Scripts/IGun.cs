using System;
using UnityEngine;

public interface IGun
{
    // Actions
    void Fire(Vector3 worldDirection);
    void Reload();

    // State
    bool    CanFire     { get; }
    bool    IsReloading { get; }
    int     CurrentAmmo { get; }
    int     ReserveAmmo { get; }
    GunData Data        { get; }

    // Events — subscribe to drive UI, VFX, sound, etc.
    event Action<int, int> OnAmmoChanged;   // (current, reserve)
    event Action           OnFired;
    event Action           OnReloaded;
    event Action           OnEmpty;
}

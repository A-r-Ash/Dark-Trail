# Dark Trail

A top-down survival horror game built in Unity 6 (URP). The player explores a dark environment armed with a flashlight and a gun, managing a draining battery, fighting procedurally-spawned creatures, and surviving long enough to reach the next checkpoint.

> **Stack:** Unity 6 · Universal Render Pipeline · C# · New Input System

---

## Gameplay Overview

- Navigate darkness using a **directional flashlight** that physically rotates toward the mouse cursor
- **Battery drains in real time** — when it dies, the game ends
- Shoot, dodge, and avoid creature archetypes with distinct AI behaviours
- Find **pickups** to restore health, ammo, and battery charge
- Reach **checkpoint lights** and press E to save your current state
- On death, choose to **respawn at checkpoint** (full state restored in-place) or restart from the beginning

---

## Systems & Architecture

### Player

| Script | Responsibility |
|---|---|
| `PlayerController` | Rigidbody top-down movement, speed multiplier, `SetImmobilized(bool, float)`, `ScreenShake(float, float)` |
| `PlayerHealth` | 3-heart health pool, `TakeDamage`, `Heal`, `Revive`, `UnityEvent<int> OnHealthChanged` |
| `PlayerShooter` | Mouse-aimed raycast firing, right-click ADS, auto/semi-auto, reload on R |
| `FlashlightController` | Rotates flashlight toward mouse in ±135° arc with blind-spot avoidance logic |
| `BatterySystem` | Owns `Light` intensity, drains over time, fires `OnBatteryDepleted`, supports `AddBattery` / `SetBattery` |
| `FlashlightBoost` | F-key boost to absolute intensity value (not a multiplier), exposes read-only state to `BatterySystem` |

**Design note:** `BatterySystem` is the single writer to the flashlight's `Light` component. `FlashlightBoost` only exposes properties — this prevents the two systems fighting over the same value.

---

### Weapon System

Built on an abstract base class + `IGun` interface so new weapon types drop in without touching existing code.

```
IGun (interface)
└── GunBase (abstract MonoBehaviour)
    ├── Fire(Vector3 direction)   — raycast, ammo, cooldown, events
    ├── Reload()                  — coroutine-based
    ├── SetAmmo / AddReserveAmmo
    └── Shotgun                   — overrides PerformRaycast with 8-pellet spread cone
```

- **`GunData` (ScriptableObject):** fire rate, damage, range, magazine size, max reserve, reload time, spread angles, all audio clips — swap an asset reference to swap the whole weapon feel
- **Procedural VFX generated in code:** muzzle smoke (soft cone particle system) and brass shell ejection (spinning elongated oval, gravity arc) — no external assets needed
- **Events:** `OnAmmoChanged`, `OnFired`, `OnReloaded`, `OnEmpty` — UI and audio subscribe without coupling to the gun logic

---

### Creature System

```
CreatureBase (abstract)
├── Reacts to flashlight direction and intensity
├── Patrol → Hunt state machine
├── TakeBulletDamage(int)
├── SyncAnimator() (protected — subclasses drive their own animator)
│
├── SwarmCreature    — fearless, overrides ReactToLight to do nothing, 1-hit kill
├── AmbushCreature   — burrowed idle, lunges when player enters trigger range
└── RangedCreature   — circle-strafes at fixed radius, fires CreatureProjectile
```

**`CreatureProjectile`** — physics Rigidbody, launched toward a world target, destroys on solid hit.

**`CreatureSpawner`** — weighted spawn table (`SpawnEntry[]` with weight, min threat level, swarm flag), continuous coroutine, reads live threat level from `DifficultyManager`.

---

### Difficulty System

`DifficultyManager` (singleton) escalates `ThreatLevel` (1–5) every 60 seconds. `CreatureSpawner` filters its spawn table by minimum threat level so harder enemies only appear as the run progresses. `DifficultyIndicator` renders a top-left progress bar HUD built entirely in code.

---

### Checkpoint System

Fully in-memory — no `PlayerPrefs`, no file I/O, nothing persists across sessions.

```
Player presses E near CheckpointLight
  └── GameManager.RegisterCheckpoint(position)
        └── Snapshots: health, currentAmmo, reserveAmmo, battery

Player dies → Game Over screen
  └── "Respawn at Checkpoint" → GameManager.RespawnAtCheckpoint()
        ├── OnGameRestart fires first (hides canvas)
        ├── Rigidbody.interpolation disabled for one frame (prevents flashlight visual pop)
        ├── rb.position = lastCheckpointPosition
        ├── rb.linearVelocity = Vector3.zero
        └── Restores health / ammo / battery from snapshot
```

Teleportation uses `rb.position` (not `transform.position`) and momentarily sets `RigidbodyInterpolation.None` so the visual position snaps with the physics position instantly — otherwise the child flashlight appears to fly toward the camera's old interpolated location.

---

### Trap System

**`BearTrap`**
- Procedural jaw geometry built from primitives at runtime
- `SphereCollider` trigger, snaps on player contact
- `SnapSequence` coroutine: snap → deal damage → `SetImmobilized(true, duration)` → rearm after delay
- Serialized `triggerSound` and `rearmSound` (3D spatial audio)

**`Tripwire`**
- `LineRenderer` laser stretched between two auto-created anchor child transforms
- `Physics.OverlapCapsule` for presence detection (not `CapsuleCast` — a directional sweep would miss stationary players)
- Configurable `explosionDelay`, `explosionRadius`, `explosionDamage`, `rearmDelay`
- Slot for a serialized `ParticleSystem` (designer assigns their own explosion effect)
- Three audio stages: wire break → explosion → rearm

---

### Pickup System

```
PickupBase (abstract)  ←  SphereCollider trigger, spinning gem visual, collect animation
├── CanPickup(player)       virtual gate — item stays on ground if blocked
├── CannotPickupMessage     virtual string — shown via PickupNotification
├── OnPickup(player)        abstract — subclass applies the effect
│
├── HealthPickup   — blocked + notifies "Health is full" when at max HP
├── AmmoPickup     — blocked + notifies "Ammo is full" when reserve is capped
└── BatteryPickup  — always collectable
```

**Player detection** uses `CompareTag("Player")` with a `GetComponentInParent<PlayerController>()` fallback — works regardless of which child collider triggers the overlap.

**`PickupNotification`** — `DontDestroyOnLoad` singleton, auto-created on first use, builds its own Screen Space Overlay canvas. Shows a fading text notification (fade-in 0.12s → hold 1.4s → fade-out 0.35s) using `Time.unscaledDeltaTime` so it works even when `timeScale = 0`.

---

### HUD & UI

All UI is built procedurally in code — no prefabs, no scene canvas dependencies.

| Script | What it renders |
|---|---|
| `HealthHUD` | 3 hearts using a procedural texture generated from the algebraic heart curve `(x²+y²−1)³ − x²y³ ≤ 0` |
| `AmmoDisplay` | Bottom-right ammo counter + center-screen reload progress (6 bullet silhouettes that fill left-to-right) |
| `GameOverScreen` | Full game-over overlay with Respawn / Restart / Main Menu; auto-generates its own canvas or delegates to a designer canvas via `buildUIAutomatically` flag |
| `DifficultyIndicator` | Top-left threat level bar |
| `PickupNotification` | Transient fading text (health full, ammo full) |

**`AmmoDisplay`** and **`AmmoPickup`** both resolve the gun through `PlayerShooter.EquippedGun` — guaranteeing they reference the same `GunBase` instance so `OnAmmoChanged` events always reach the HUD.

---

### Hit Effects

`HitEffectManager` (singleton) manages surface-appropriate impact effects and a **blood decal pool**:

- 50 decals pre-instantiated in `Awake` (circular buffer, oldest overwritten)
- Decals placed on a `Vector3.up` plane at hit normal, 10-second alpha fade coroutine
- Surface type resolved via `SurfaceIdentifier` component on geometry — defaults to `SurfaceType.Default` if none found

---

## Technical Highlights

- **New Input System** throughout (`Keyboard.current`, `Mouse.current`) — no legacy `Input.GetKey`
- **ScriptableObject-driven weapon data** — designers tune weapons without touching code
- **Abstract base classes + interfaces** (`GunBase`/`IGun`, `CreatureBase`, `PickupBase`) — extensible without modifying existing behaviour
- **Singleton pattern** with lazy self-creation (`GameManager`, `HitEffectManager`, `DifficultyManager`, `PickupNotification`)
- **UnityEvent decoupling** — `OnGameOver`, `OnGameRestart`, `OnHealthChanged`, `OnBatteryDepleted` let systems react to each other without direct references
- **Procedural asset generation** — heart texture, bullet sprite, muzzle smoke, shell ejection, gem pickups all built in C# at runtime; no external art dependencies for core systems
- **Rigidbody teleportation** — disables interpolation for one frame on respawn to prevent visual/physics position divergence
- **Memory-only checkpoint state** — deliberately avoids `PlayerPrefs` so nothing persists across editor play sessions

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Player/         PlayerController, PlayerHealth, PlayerShooter
│   ├── Flashlight/     BatterySystem, FlashlightController, FlashlightBoost
│   ├── Weapons/        GunBase, GunData, IGun, Shotgun
│   ├── Creatures/      CreatureBase, SwarmCreature, AmbushCreature, RangedCreature, CreatureProjectile, CreatureSpawner
│   ├── Traps/          BearTrap, Tripwire
│   ├── Pickups/        PickupBase, HealthPickup, AmmoPickup, BatteryPickup, PickupSpawner, PickupNotification
│   ├── Checkpoints/    CheckpointLight
│   ├── UI/             GameOverScreen, HealthHUD, AmmoDisplay, DifficultyIndicator, HUDController
│   ├── Effects/        HitEffectManager
│   └── Core/           GameManager, DifficultyManager
└── ScriptableObjects/
    └── GunData assets
```

---

## Getting Started

1. Clone the repo
2. Open in **Unity 6** (6000.x) with **Universal Render Pipeline**
3. Open `Assets/Scenes/` and load the main scene
4. Press Play

> Requires Unity 6 LTS. URP package must be installed via Package Manager.

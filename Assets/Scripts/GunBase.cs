using System;
using System.Collections;
using UnityEngine;

public abstract class GunBase : MonoBehaviour, IGun
{
    [Header("Gun Data")]
    [SerializeField] protected GunData data;

    [Header("References")]
    [SerializeField] protected Transform muzzlePoint;
    [SerializeField] protected Transform ejectionPoint;   // right side of slide; auto-created if null
    [SerializeField] protected LayerMask hitMask = ~0;

    // ── IGun events ───────────────────────────────────────────────────────────
    public event Action<int, int> OnAmmoChanged;
    public event Action           OnFired;
    public event Action           OnReloaded;
    public event Action           OnEmpty;

    // ── State ─────────────────────────────────────────────────────────────────
    protected int            currentAmmo;
    protected int            reserveAmmo;
    protected bool           isReloading;
    protected float          nextFireTime;
    protected AudioSource    audioSource;
    protected ParticleSystem muzzleSmoke;
    protected ParticleSystem shellEjector;

    private static Material _smokeMat;
    private static Material _shellMat;

    // ── IGun properties ───────────────────────────────────────────────────────
    public bool    CanFire     => !isReloading && currentAmmo > 0 && Time.time >= nextFireTime;
    public bool    IsReloading => isReloading;
    public int     CurrentAmmo => currentAmmo;
    public int     ReserveAmmo => reserveAmmo;
    public GunData Data        => data;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    protected virtual void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"[GunBase] {name}: GunData not assigned.");
            enabled = false;
            return;
        }

        currentAmmo = data.magazineSize;
        reserveAmmo = data.maxReserveAmmo;

        audioSource              = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake  = false;
        audioSource.priority     = 64;

        muzzleSmoke  = BuildMuzzleSmoke();
        shellEjector = BuildShellEjector();
    }

    // ── IGun methods ──────────────────────────────────────────────────────────
    public void Fire(Vector3 worldDirection)
    {
        if (!CanFire)
        {
            if (currentAmmo <= 0 && !isReloading)
            {
                PlayClip(data.emptyClickSound);
                OnEmpty?.Invoke();
                if (reserveAmmo > 0)
                    StartCoroutine(ReloadRoutine());
            }
            return;
        }

        nextFireTime = Time.time + 1f / data.fireRate;
        currentAmmo--;

        PlayClip(data.fireSound);
        OnFired?.Invoke();
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);

        PerformRaycast(worldDirection);
        FireEffect();
        EmitMuzzleSmoke();
        EjectShell();

        if (currentAmmo <= 0 && reserveAmmo > 0)
            StartCoroutine(ReloadRoutine());
    }

    public void Reload()
    {
        if (isReloading || currentAmmo == data.magazineSize || reserveAmmo <= 0) return;
        StartCoroutine(ReloadRoutine());
    }

    public void AddReserveAmmo(int amount)
    {
        reserveAmmo = Mathf.Min(reserveAmmo + amount, data != null ? data.maxReserveAmmo : reserveAmmo + amount);
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }

    public void SetAmmo(int current, int reserve)
    {
        currentAmmo = Mathf.Clamp(current, 0, data != null ? data.magazineSize    : current);
        reserveAmmo = Mathf.Clamp(reserve, 0, data != null ? data.maxReserveAmmo  : reserve);
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }

    // ── Overrideable ──────────────────────────────────────────────────────────
    protected abstract void FireEffect();

    protected virtual void PerformRaycast(Vector3 direction)
    {
        Vector3 origin = muzzlePoint != null
            ? muzzlePoint.position
            : transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, data.range, hitMask, QueryTriggerInteraction.Ignore))
            OnHit(hit);
    }

    protected virtual void OnHit(RaycastHit hit)
    {
        SurfaceType surface  = SurfaceType.Default;
        var         surfaceId = hit.collider.GetComponentInParent<SurfaceIdentifier>();
        if (surfaceId != null) surface = surfaceId.surfaceType;

        CreatureBase creature = hit.collider.GetComponentInParent<CreatureBase>();
        if (creature != null)
        {
            surface = SurfaceType.Flesh;
            creature.TakeBulletDamage(data.damage);
        }

        HitEffectManager.Instance.SpawnHitEffect(surface, hit.point, hit.normal);
    }

    // ── Internals ─────────────────────────────────────────────────────────────
    protected IEnumerator ReloadRoutine()
    {
        isReloading = true;
        PlayClip(data.reloadSound);
        yield return new WaitForSeconds(data.reloadTime);
        int refill   = Mathf.Min(data.magazineSize - currentAmmo, reserveAmmo);
        currentAmmo += refill;
        reserveAmmo -= refill;
        isReloading  = false;
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
        OnReloaded?.Invoke();
    }

    protected void PlayClip(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    // ── Muzzle smoke ──────────────────────────────────────────────────────────

    void EmitMuzzleSmoke()
    {
        if (muzzleSmoke != null)
            muzzleSmoke.Emit(UnityEngine.Random.Range(4, 8));
    }

    ParticleSystem BuildMuzzleSmoke()
    {
        Transform anchor = muzzlePoint != null ? muzzlePoint : transform;

        var go = new GameObject("MuzzleSmoke");
        go.transform.SetParent(anchor);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main             = ps.main;
        main.playOnAwake     = false;
        main.loop            = false;
        main.duration        = 1f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.4f, 1.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.10f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(0.80f, 0.80f, 0.80f, 0.55f),
            new Color(0.50f, 0.50f, 0.50f, 0.25f));
        main.gravityModifier = -0.06f;   // drifts upward slightly
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 60;

        var emission          = ps.emission;
        emission.rateOverTime = 0;

        var shape         = ps.shape;
        shape.enabled     = true;
        shape.shapeType   = ParticleSystemShapeType.Cone;
        shape.angle       = 14f;
        shape.radius      = 0.003f;

        var col     = ps.colorOverLifetime;
        col.enabled = true;
        var g       = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var sol     = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size    = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(1f, 1f)));

        var noise       = ps.noise;
        noise.enabled   = true;
        noise.strength  = 0.25f;
        noise.frequency = 0.6f;

        SetParticleMaterial(ps, GetSmokeMat());
        return ps;
    }

    // ── Shell ejection ────────────────────────────────────────────────────────

    void EjectShell()
    {
        if (shellEjector == null) return;

        // Point the ejector in the correct world direction every shot so it adapts to gun rotation
        Transform gun = muzzlePoint != null ? muzzlePoint : transform;
        Vector3 ejectDir = (gun.right * 2f + Vector3.up).normalized;
        shellEjector.transform.rotation = Quaternion.LookRotation(ejectDir);

        shellEjector.Emit(1);
    }

    ParticleSystem BuildShellEjector()
    {
        // Anchor: use ejectionPoint if provided, otherwise a default offset right of muzzle
        Transform anchor;
        if (ejectionPoint != null)
        {
            anchor = ejectionPoint;
        }
        else
        {
            var ep = new GameObject("EjectionPoint");
            Transform muzzle = muzzlePoint != null ? muzzlePoint : transform;
            ep.transform.SetParent(muzzle);
            ep.transform.localPosition = new Vector3(0.04f, 0.01f, 0f); // right + slightly up
            ep.transform.localRotation = Quaternion.identity;
            anchor = ep.transform;
        }

        var go = new GameObject("ShellEjector");
        go.transform.SetParent(anchor);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main             = ps.main;
        main.playOnAwake     = false;
        main.loop            = false;
        main.duration        = 1f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.55f, 0.80f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1.8f, 3.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.018f, 0.024f);
        main.startRotation   = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(0.90f, 0.68f, 0.18f),
            new Color(0.75f, 0.52f, 0.10f));
        main.gravityModifier = 1.8f;     // falls to the floor
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 60;

        var emission          = ps.emission;
        emission.rateOverTime = 0;

        var shape         = ps.shape;
        shape.enabled     = true;
        shape.shapeType   = ParticleSystemShapeType.Cone;
        shape.angle       = 8f;    // tight cone — casings fly in one direction
        shape.radius      = 0.002f;

        // Stay fully visible during arc, quick fade at end (looks like it lands)
        var col     = ps.colorOverLifetime;
        col.enabled = true;
        var g       = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0f), new GradientColorKey(new Color(0.8f, 0.55f, 0.1f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.80f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        // Spin during flight
        var rot         = ps.rotationOverLifetime;
        rot.enabled     = true;
        rot.z           = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);

        SetParticleMaterial(ps, GetShellMat());
        return ps;
    }

    // ── Shared materials ──────────────────────────────────────────────────────

    static void SetParticleMaterial(ParticleSystem ps, Material mat)
    {
        var r        = ps.GetComponent<ParticleSystemRenderer>();
        r.material   = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
    }

    static Material GetSmokeMat()
    {
        if (_smokeMat != null) return _smokeMat;
        _smokeMat = BuildParticleMat(new Color(0.8f, 0.8f, 0.8f, 1f), BuildCircleTex(32));
        return _smokeMat;
    }

    static Material GetShellMat()
    {
        if (_shellMat != null) return _shellMat;
        _shellMat = BuildParticleMat(Color.white, BuildShellTex());
        return _shellMat;
    }

    static Material BuildParticleMat(Color baseColor, Texture2D tex)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        var mat = new Material(shader != null ? shader : Shader.Find("Legacy Shaders/Particles/Additive"));
        if (mat.HasProperty("_BaseMap"))   mat.SetTexture("_BaseMap",   tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",   baseColor);
        mat.mainTexture = tex;
        return mat;
    }

    // Soft circle for smoke
    static Texture2D BuildCircleTex(int size)
    {
        var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float h  = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(h, h)) / h);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(a, 0.55f)));
            }
        tex.Apply();
        return tex;
    }

    // Small elongated oval for brass shell casing (top-down silhouette)
    static Texture2D BuildShellTex()
    {
        const int w = 12, h = 20;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        float cx = w * 0.5f, cy = h * 0.5f;
        float rx = w * 0.44f, ry = h * 0.44f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f - cx) / rx;
                float dy = (y + 0.5f - cy) / ry;
                float dist  = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((1f - dist) / 0.15f);  // hard edge

                // Metallic shading: highlight left side
                float br = Mathf.Lerp(1.0f, 0.65f, (dx + 1f) * 0.5f);
                tex.SetPixel(x, y, new Color(br, br, br, alpha));
            }
        }
        tex.Apply();
        return tex;
    }
}

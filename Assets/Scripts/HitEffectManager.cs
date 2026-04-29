using System.Collections;
using UnityEngine;

public class HitEffectManager : MonoBehaviour
{
    // Self-initializing singleton — no manual scene setup needed
    public static HitEffectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("HitEffectManager");
                _instance = go.AddComponent<HitEffectManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    private static HitEffectManager _instance;

    [Header("Effect")]
    [SerializeField] [Range(0.1f, 5f)] private float effectScale = 1f;

    [Header("Hit Sounds")]
    [SerializeField] private AudioClip woodSound;
    [SerializeField] private AudioClip groundSound;
    [SerializeField] private AudioClip fleshSound;
    [SerializeField] private AudioClip metalSound;
    [SerializeField] private AudioClip stoneSound;
    [SerializeField] private AudioClip defaultSound;

    [Header("Audio")]
    [SerializeField] [Range(0f, 1f)] private float hitVolume = 0.75f;

    // Cached shared resources
    private Material  particleMat;
    private Texture2D circleTex;

    // ── Blood decal pool ──────────────────────────────────────────────────────
    private const int DecalPoolSize  = 50;
    private const float DecalFadeTime = 10f;

    private Renderer[]  decalPool;
    private int         decalIndex;
    private Material    bloodMat;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        circleTex   = BuildCircleTexture(32);
        particleMat = BuildParticleMaterial();

        bloodMat  = BuildBloodMaterial();
        decalPool = BuildDecalPool();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SpawnHitEffect(SurfaceType surface, Vector3 position, Vector3 normal)
    {
        switch (surface)
        {
            case SurfaceType.Wood:   SpawnWood(position, normal);   PlayHitSound(woodSound,    position); break;
            case SurfaceType.Ground: SpawnGround(position, normal); PlayHitSound(groundSound,  position); break;
            case SurfaceType.Flesh:  SpawnFlesh(position, normal);  PlayHitSound(fleshSound,   position); break;
            case SurfaceType.Metal:  SpawnMetal(position, normal);  PlayHitSound(metalSound,   position); break;
            case SurfaceType.Stone:  SpawnStone(position, normal);  PlayHitSound(stoneSound,   position); break;
            default:                 SpawnDefault(position, normal); PlayHitSound(defaultSound, position); break;
        }
    }

    void PlayHitSound(AudioClip clip, Vector3 position)
    {
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, position, hitVolume);
    }

    // ── Effect builders ───────────────────────────────────────────────────────

    void SpawnWood(Vector3 pos, Vector3 normal)
    {
        ParticleSystem ps = MakeSystem("HitFX_Wood", pos, normal);

        var main           = ps.main;
        main.startLifetime = Rng(0.35f, 0.7f);
        main.startSpeed    = Rng(1.5f, 6f);
        main.startSize     = Rng(0.04f, 0.10f);
        main.startColor    = Gradient2(
            new Color(0.45f, 0.25f, 0.08f),
            new Color(0.78f, 0.58f, 0.26f));
        main.gravityModifier = 2f;

        Burst(ps, 18, 28);

        var shape     = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle   = 38f;
        shape.radius  = 0.02f;

        FadeAlpha(ps);
        ShrinkSize(ps, 1f, 0f);
        SetRenderer(ps);
        ps.Play();
        Destroy(ps.gameObject, 2f);
    }

    void SpawnGround(Vector3 pos, Vector3 normal)
    {
        ParticleSystem ps = MakeSystem("HitFX_Ground", pos, normal);

        var main           = ps.main;
        main.startLifetime = Rng(0.45f, 1.0f);
        main.startSpeed    = Rng(0.8f, 3.5f);
        main.startSize     = Rng(0.07f, 0.20f);
        main.startColor    = Gradient2(
            new Color(0.38f, 0.28f, 0.17f),
            new Color(0.60f, 0.50f, 0.36f));
        main.gravityModifier = 0.3f;  // dust lingers

        Burst(ps, 24, 34);

        var shape       = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius    = 0.06f;

        FadeAlpha(ps, startAlpha: 0.85f);
        GrowThenFade(ps);  // dust puff expands
        SetRenderer(ps);
        ps.Play();
        Destroy(ps.gameObject, 2f);
    }

    void SpawnFlesh(Vector3 pos, Vector3 normal)
    {
        ParticleSystem ps = MakeSystem("HitFX_Flesh", pos, normal);

        var main           = ps.main;
        main.startLifetime = Rng(0.15f, 0.45f);
        main.startSpeed    = Rng(2f, 8f);
        main.startSize     = Rng(0.02f, 0.07f);
        main.startColor    = Gradient2(
            new Color(0.80f, 0.02f, 0.02f),
            new Color(1.00f, 0.18f, 0.06f));
        main.gravityModifier = 2.8f;

        Burst(ps, 28, 42);

        var shape       = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 22f;
        shape.radius    = 0.01f;

        ColorFade(ps,
            new Color(1f, 0.2f, 0.1f),
            new Color(0.35f, 0f, 0f));
        ShrinkSize(ps, 1f, 0.2f);
        SetRenderer(ps);
        ps.Play();
        Destroy(ps.gameObject, 2f);
        PlaceBloodDecal(pos, normal);
    }

    void SpawnMetal(Vector3 pos, Vector3 normal)
    {
        // Bright sparks + small dust
        ParticleSystem sparks = MakeSystem("HitFX_Metal_Sparks", pos, normal);

        var main           = sparks.main;
        main.startLifetime = Rng(0.10f, 0.35f);
        main.startSpeed    = Rng(4f, 12f);
        main.startSize     = Rng(0.01f, 0.035f);
        main.startColor    = Gradient2(
            new Color(1f, 0.92f, 0.4f),
            new Color(1f, 0.60f, 0.1f));
        main.gravityModifier = 1.5f;

        Burst(sparks, 20, 32);

        var shape       = sparks.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 45f;
        shape.radius    = 0.01f;

        FadeAlpha(sparks);
        SetRenderer(sparks);
        sparks.Play();
        Destroy(sparks.gameObject, 2f);
    }

    void SpawnStone(Vector3 pos, Vector3 normal)
    {
        ParticleSystem ps = MakeSystem("HitFX_Stone", pos, normal);

        var main           = ps.main;
        main.startLifetime = Rng(0.3f, 0.7f);
        main.startSpeed    = Rng(1f, 5f);
        main.startSize     = Rng(0.04f, 0.12f);
        main.startColor    = Gradient2(
            new Color(0.50f, 0.50f, 0.50f),
            new Color(0.75f, 0.72f, 0.68f));
        main.gravityModifier = 2f;

        Burst(ps, 16, 24);

        var shape       = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 35f;
        shape.radius    = 0.02f;

        FadeAlpha(ps);
        GrowThenFade(ps);
        SetRenderer(ps);
        ps.Play();
        Destroy(ps.gameObject, 2f);
    }

    void SpawnDefault(Vector3 pos, Vector3 normal)
    {
        ParticleSystem ps = MakeSystem("HitFX_Default", pos, normal);

        var main           = ps.main;
        main.startLifetime = Rng(0.08f, 0.28f);
        main.startSpeed    = Rng(3f, 10f);
        main.startSize     = Rng(0.01f, 0.04f);
        main.startColor    = Gradient2(
            new Color(1f,  0.95f, 0.6f),
            new Color(0.7f, 0.7f, 0.7f));
        main.gravityModifier = 1.2f;

        Burst(ps, 14, 22);

        var shape       = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 45f;
        shape.radius    = 0.01f;

        FadeAlpha(ps);
        SetRenderer(ps);
        ps.Play();
        Destroy(ps.gameObject, 2f);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    ParticleSystem MakeSystem(string goName, Vector3 pos, Vector3 normal)
    {
        var go = new GameObject(goName);
        go.transform.SetPositionAndRotation(pos + normal * 0.02f,
            normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity);

        var ps = go.AddComponent<ParticleSystem>();
        // Stop immediately — AddComponent starts playback (playOnAwake=true) before we can configure
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main             = ps.main;
        main.playOnAwake     = false;
        main.duration        = 0.15f;
        main.loop            = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 64;

        var emission          = ps.emission;
        emission.rateOverTime = 0;

        return ps;
    }

    static void Burst(ParticleSystem ps, int min, int max)
    {
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)min, (short)max, 1, 0f) });
    }

    static ParticleSystem.MinMaxCurve Rng(float a, float b) =>
        new ParticleSystem.MinMaxCurve(a, b);

    static ParticleSystem.MinMaxGradient Gradient2(Color a, Color b) =>
        new ParticleSystem.MinMaxGradient(a, b);

    static void FadeAlpha(ParticleSystem ps, float startAlpha = 1f)
    {
        var col     = ps.colorOverLifetime;
        col.enabled = true;
        var g       = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(startAlpha, 0f),  new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);
    }

    static void ColorFade(ParticleSystem ps, Color from, Color to)
    {
        var col     = ps.colorOverLifetime;
        col.enabled = true;
        var g       = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
            new[] { new GradientAlphaKey(1f, 0f),   new GradientAlphaKey(0f, 0.85f) });
        col.color = new ParticleSystem.MinMaxGradient(g);
    }

    static void ShrinkSize(ParticleSystem ps, float from, float to)
    {
        var sol     = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size    = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, from), new Keyframe(1f, to)));
    }

    static void GrowThenFade(ParticleSystem ps)
    {
        var sol     = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size    = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f,   0.15f),
                new Keyframe(0.35f, 1f),
                new Keyframe(1f,   1.5f)));
    }

    void SetRenderer(ParticleSystem ps)
    {
        var r        = ps.GetComponent<ParticleSystemRenderer>();
        r.material   = particleMat;
        r.renderMode = ParticleSystemRenderMode.Billboard;

        // Apply global scale to particle size — shared multiplier so no per-method changes needed
        var main = ps.main;
        main.startSizeMultiplier *= effectScale;
    }

    // ── Blood decals ──────────────────────────────────────────────────────────

    void PlaceBloodDecal(Vector3 pos, Vector3 normal)
    {
        Renderer r = decalPool[decalIndex];
        decalIndex = (decalIndex + 1) % DecalPoolSize;

        // Lay flat on surface: forward = normal, then rotate -90° around X so quad faces along normal
        Quaternion rot = Quaternion.LookRotation(normal) * Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
        r.transform.SetPositionAndRotation(pos + normal * 0.01f, rot);

        float scale = Random.Range(0.3f, 0.7f) * effectScale;
        r.transform.localScale = new Vector3(scale, scale, scale);

        var c = bloodMat.color;
        r.material.color = new Color(c.r, c.g, c.b, 0.85f);
        r.gameObject.SetActive(true);
        StartCoroutine(FadeDecal(r, DecalFadeTime));
    }

    IEnumerator FadeDecal(Renderer r, float duration)
    {
        float elapsed = 0f;
        Color start   = r.material.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(start.a, 0f, elapsed / duration);
            r.material.color = new Color(start.r, start.g, start.b, a);
            yield return null;
        }
        r.gameObject.SetActive(false);
    }

    Renderer[] BuildDecalPool()
    {
        var pool    = new Renderer[DecalPoolSize];
        var parent  = new GameObject("BloodDecalPool").transform;
        parent.SetParent(transform);

        for (int i = 0; i < DecalPoolSize; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<Collider>());
            go.name = $"Decal_{i}";
            go.transform.SetParent(parent, false);
            go.SetActive(false);

            var r = go.GetComponent<Renderer>();
            r.material        = new Material(bloodMat);   // instance per decal so alpha is independent
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows  = false;
            pool[i] = r;
        }
        return pool;
    }

    Material BuildBloodMaterial()
    {
        Texture2D tex = BuildBloodTexture(64);

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");

        var mat = new Material(shader);
        mat.color = new Color(0.35f, 0.01f, 0.01f, 0.85f);

        if (mat.HasProperty("_BaseMap"))   mat.SetTexture("_BaseMap",   tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",   mat.color);
        mat.mainTexture = tex;

        // Enable transparency
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);   // 0=Opaque 1=Transparent in URP
            mat.renderQueue = 3000;
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        return mat;
    }

    static Texture2D BuildBloodTexture(int size)
    {
        var tex   = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float h   = size * 0.5f;

        // Base circle splatter
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.clear);

        // Main blob
        DrawCircle(tex, h, h, h * 0.8f, 1f);

        // Irregular splatter arms
        var rng = new System.Random(42);
        for (int arm = 0; arm < 8; arm++)
        {
            double angle = arm * (2 * System.Math.PI / 8) + rng.NextDouble() * 0.5;
            float len    = (float)(rng.NextDouble() * h * 0.6f + h * 0.2f);
            float ex     = h + Mathf.Cos((float)angle) * len;
            float ey     = h + Mathf.Sin((float)angle) * len;
            float r      = (float)(rng.NextDouble() * h * 0.18f + h * 0.06f);
            DrawCircle(tex, ex, ey, r, (float)(rng.NextDouble() * 0.5f + 0.5f));
        }

        tex.Apply();
        return tex;
    }

    static void DrawCircle(Texture2D tex, float cx, float cy, float radius, float alpha)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
        int x1 = Mathf.Min(tex.width - 1,  Mathf.CeilToInt(cx + radius));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
        int y1 = Mathf.Min(tex.height - 1, Mathf.CeilToInt(cy + radius));

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                if (dist >= radius) continue;
                float a = Mathf.Clamp01(1f - dist / radius) * alpha;
                Color existing = tex.GetPixel(x, y);
                float newA = Mathf.Max(existing.a, a);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, newA));
            }
    }

    // ── Resource builders ─────────────────────────────────────────────────────

    static Texture2D BuildCircleTexture(int size)
    {
        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half));
                float alpha = Mathf.Clamp01(1f - dist / half);
                alpha       = Mathf.Pow(alpha, 0.6f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    Material BuildParticleMaterial()
    {
        // Try URP particles first, fall back to legacy
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");

        var mat         = new Material(shader);
        mat.mainTexture = circleTex;

        // URP property names
        if (mat.HasProperty("_BaseMap"))   mat.SetTexture("_BaseMap",   circleTex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",   Color.white);

        return mat;
    }
}

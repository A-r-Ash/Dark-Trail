using UnityEngine;

public class CarExhaust : MonoBehaviour
{
    void Awake()
    {
        BuildExhaust();
    }

    void BuildExhaust()
    {
        ParticleSystem ps = gameObject.AddComponent<ParticleSystem>();

        // ── Main ─────────────────────────────────────────────────────────────
        var main = ps.main;
        main.loop                = true;
        main.playOnAwake         = true;
        main.simulationSpace     = ParticleSystemSimulationSpace.World;
        main.maxParticles        = 800;
        main.startLifetime       = new ParticleSystem.MinMaxCurve(2.5f, 4f);
        main.startSpeed          = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
        main.startSize           = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
        main.startRotation       = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor          = new ParticleSystem.MinMaxGradient(
            new Color(0.06f, 0.06f, 0.06f, 1f),
            new Color(0.22f, 0.22f, 0.22f, 0.8f)
        );
        main.gravityModifier     = -0.08f;

        // ── Emission — intense bursts ─────────────────────────────────────────
        var em = ps.emission;
        em.enabled        = true;
        em.rateOverTime   = 55f;

        // random burst every ~0.4 s to mimic engine pulse
        em.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f,   8, 14, 999, 0.38f),
        });

        // ── Shape — tight cone (exhaust pipe) ────────────────────────────────
        var shape = ps.shape;
        shape.enabled    = true;
        shape.shapeType  = ParticleSystemShapeType.Cone;
        shape.angle      = 6f;
        shape.radius     = 0.04f;

        // ── Velocity over lifetime — rise & drift ────────────────────────────
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x       = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
        vel.y       = new ParticleSystem.MinMaxCurve(0.5f,   1.2f);
        vel.z       = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);

        // ── Size over lifetime — smoke balloons outward ───────────────────────
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve(
            new Keyframe(0f,  0.25f, 0f, 3f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f,  4.5f, 1f, 0f)
        );
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        // ── Color over lifetime — black → charcoal → invisible ───────────────
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g  = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.04f, 0.04f, 0.04f), 0.00f),
                new GradientColorKey(new Color(0.12f, 0.12f, 0.12f), 0.25f),
                new GradientColorKey(new Color(0.28f, 0.28f, 0.28f), 0.65f),
                new GradientColorKey(new Color(0.45f, 0.45f, 0.45f), 1.00f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.95f, 0.00f),
                new GradientAlphaKey(0.80f, 0.15f),
                new GradientAlphaKey(0.45f, 0.60f),
                new GradientAlphaKey(0.00f, 1.00f),
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        // ── Rotation over lifetime — organic tumble ───────────────────────────
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z       = new ParticleSystem.MinMaxCurve(-55f * Mathf.Deg2Rad, 55f * Mathf.Deg2Rad);

        // ── Noise — turbulence so smoke looks organic ─────────────────────────
        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        noise.frequency   = 0.25f;
        noise.scrollSpeed = 0.3f;
        noise.octaveCount = 2;
        noise.damping     = true;

        // ── Renderer ─────────────────────────────────────────────────────────
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode   = ParticleSystemRenderMode.Billboard;
        rend.sortingOrder = 1;

        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

        Material mat = new Material(sh);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend",   0f);
        mat.mainTexture = CreateSmokeTexture();
        rend.material   = mat;

        ps.Play();
    }

    Texture2D CreateSmokeTexture()
    {
        const int size = 128;
        Texture2D tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float     half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx   = (x - half) / half;
                float dy   = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy); // 0 = center, 1 = edge

                // Soft radial falloff — smooth smoke puff shape
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = Mathf.Pow(alpha, 1.8f); // sharpen the falloff slightly

                // Subtle lumpiness so it reads as smoke, not a perfect circle
                float bump = Mathf.PerlinNoise(x * 0.08f + 3.7f, y * 0.08f + 1.2f);
                alpha *= Mathf.Lerp(0.85f, 1f, bump);

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }
}

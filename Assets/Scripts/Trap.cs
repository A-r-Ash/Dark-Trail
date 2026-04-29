using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour
{
    [SerializeField] private int   damage         = 1;
    [SerializeField] private float damageCooldown = 2f;
    [SerializeField] private int   spikeCount     = 6;
    [SerializeField] private float spikeRadius    = 0.38f;
    [SerializeField] private float spikeMaxHeight = 0.7f;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Animation Clips")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip activationClip;
    [SerializeField] private AnimationClip explosionClip;

    private enum State { Armed, Warning, Triggered, Resetting }
    private State state = State.Armed;

    private Collider   trapCollider;
    private Transform[] spikes;
    private Light      glowLight;
    private Renderer   baseRenderer;

    static readonly Color ColIdle    = new Color(0.55f, 0.04f, 0.04f);
    static readonly Color ColWarning = new Color(1.00f, 0.30f, 0.00f);
    static readonly Color ColFire    = new Color(1.00f, 0.85f, 0.10f);
    static readonly Color ColCool    = new Color(0.15f, 0.02f, 0.02f);

    void Awake()
    {
        trapCollider = GetComponent<Collider>();
        if (trapCollider != null)
            trapCollider.isTrigger = true;

        baseRenderer = GetComponent<Renderer>();
        BuildVisuals();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        SetupOverrides();
    }

    void Start()
    {
        StartCoroutine(PulseIdle());
    }

    // ── Visual construction ───────────────────────────────────────────────────

    void BuildVisuals()
    {
        // Base plate: dark rust colour
        if (baseRenderer != null)
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (m.shader.name == "Hidden/InternalErrorShader")
                m = new Material(Shader.Find("Standard"));
            m.color = new Color(0.22f, 0.07f, 0.04f);
            baseRenderer.material = m;
        }

        // Point light — sits just above the plate
        GameObject lg = new GameObject("TrapGlow");
        lg.transform.SetParent(transform);
        lg.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        glowLight             = lg.AddComponent<Light>();
        glowLight.type        = LightType.Point;
        glowLight.color       = ColIdle;
        glowLight.intensity   = 1.8f;
        glowLight.range       = 3.5f;
        glowLight.shadows     = LightShadows.None;

        // Spikes (thin cylinders arranged in a ring)
        Material sm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (sm.shader.name == "Hidden/InternalErrorShader")
            sm = new Material(Shader.Find("Standard"));
        sm.color = new Color(0.55f, 0.50f, 0.44f);

        spikes = new Transform[spikeCount];
        for (int i = 0; i < spikeCount; i++)
        {
            float angle = i * (360f / spikeCount) * Mathf.Deg2Rad;
            float sx    = Mathf.Sin(angle) * spikeRadius;
            float sz    = Mathf.Cos(angle) * spikeRadius;

            GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spike.name = "Spike_" + i;
            spike.transform.SetParent(transform);
            spike.transform.localPosition = new Vector3(sx, 0f, sz);
            spike.transform.localScale    = new Vector3(0.07f, 0.001f, 0.07f); // flat/hidden
            spike.GetComponent<Renderer>().material = sm;
            Destroy(spike.GetComponent<Collider>());

            spikes[i] = spike.transform;
        }
    }

    // ── Trigger ───────────────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || state != State.Armed) return;
        StartCoroutine(TriggerSequence(other));
    }

    IEnumerator TriggerSequence(Collider player)
    {
        // --- Warning flash (0.25 s) ---
        state = State.Warning;
        animator?.SetTrigger("Activate");
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float p = Mathf.PingPong(t * 18f, 1f);
            glowLight.color     = Color.Lerp(ColIdle, ColWarning, p);
            glowLight.intensity = Mathf.Lerp(1.8f, 6f, p);
            yield return null;
        }

        // --- Spikes shoot up (0.12 s, ease-out cubic) ---
        state = State.Triggered;
        animator?.SetTrigger("Explode");
        glowLight.color     = ColFire;
        glowLight.intensity = 9f;

        t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.12f), 3f);
            SetSpikeHeight(ease * spikeMaxHeight);
            yield return null;
        }
        SetSpikeHeight(spikeMaxHeight);

        // Deal damage
        if (player != null)
        {
            PlayerHealth hp = player.GetComponent<PlayerHealth>();
            if (hp != null)
                hp.TakeDamage(damage);
            else
                GameManager.Instance?.TriggerGameOver();
        }

        // --- Hold ---
        yield return new WaitForSeconds(0.35f);

        // --- Retract (0.28 s, ease-in) ---
        state = State.Resetting;
        t = 0f;
        while (t < 0.28f)
        {
            t += Time.deltaTime;
            float ease = Mathf.Pow(Mathf.Clamp01(t / 0.28f), 2f);
            SetSpikeHeight((1f - ease) * spikeMaxHeight);
            glowLight.color     = Color.Lerp(ColFire, ColCool, t / 0.28f);
            glowLight.intensity = Mathf.Lerp(9f, 0.5f, t / 0.28f);
            yield return null;
        }
        SetSpikeHeight(0f);

        // --- Cooldown before re-arming ---
        float coolRemain = damageCooldown - 0.75f;
        if (coolRemain > 0f)
            yield return new WaitForSeconds(coolRemain);

        state = State.Armed;
        animator?.SetTrigger("Reset");
        StartCoroutine(PulseIdle());
    }

    // ── Idle pulse ────────────────────────────────────────────────────────────

    IEnumerator PulseIdle()
    {
        while (state == State.Armed)
        {
            float p = Mathf.PingPong(Time.time * 0.75f, 1f);
            glowLight.color     = Color.Lerp(ColCool, ColIdle, p);
            glowLight.intensity = Mathf.Lerp(0.8f, 2.2f, p);
            yield return null;
        }
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    void SetupOverrides()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        var oc        = new AnimatorOverrideController(animator.runtimeAnimatorController);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(oc.overridesCount);
        oc.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip clip = overrides[i].Key.name switch
            {
                "Idle"       => idleClip,
                "Activation" => activationClip,
                "Explosion"  => explosionClip,
                _            => null
            };
            if (clip != null)
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, clip);
        }

        oc.ApplyOverrides(overrides);
        animator.runtimeAnimatorController = oc;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void SetSpikeHeight(float h)
    {
        if (spikes == null) return;
        foreach (var s in spikes)
        {
            if (s == null) continue;
            s.localScale    = new Vector3(0.07f, Mathf.Max(0.001f, h * 0.5f), 0.07f);
            s.localPosition = new Vector3(s.localPosition.x, h * 0.5f, s.localPosition.z);
        }
    }
}

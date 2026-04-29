using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Clips — assign your animation clips here")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip walkClip;
    [SerializeField] private AnimationClip aimClip;
    [SerializeField] private AnimationClip reloadClip;
    [SerializeField] private AnimationClip dieClip;

    private PlayerController controller;
    private PlayerShooter    shooter;
    private PlayerHealth     health;
    private bool             isDead;

    void Awake()
    {
        controller = GetComponentInParent<PlayerController>();
        shooter    = GetComponentInParent<PlayerShooter>();
        health     = GetComponentInParent<PlayerHealth>();

        if (animator == null)
            animator = GetComponent<Animator>();

        SetupOverrides();
    }

    void Start()
    {
        if (health != null)
            health.OnDeath.AddListener(OnDeath);
    }

    void Update()
    {
        if (isDead || animator == null) return;

        bool moving   = controller != null && controller.IsMoving;
        bool aiming   = shooter   != null && shooter.IsAiming;
        bool reloading = shooter  != null && shooter.IsReloading;

        animator.SetBool("IsMoving",    moving);
        animator.SetBool("IsAiming",    aiming);
        animator.SetBool("IsReloading", reloading);
    }

    void OnDeath()
    {
        isDead = true;
        animator?.SetTrigger("Die");
    }

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
                "Idle"   => idleClip,
                "Walk"   => walkClip,
                "Aim"    => aimClip,
                "Reload" => reloadClip,
                "Die"    => dieClip,
                _      => null
            };
            if (clip != null)
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, clip);
        }

        oc.ApplyOverrides(overrides);
        animator.runtimeAnimatorController = oc;
    }
}

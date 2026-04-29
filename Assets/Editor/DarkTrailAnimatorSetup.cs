using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class DarkTrailAnimatorSetup
{
    const string OutPath = "Assets/Animations/Controllers";

    [MenuItem("Dark Trail/Generate Animator Controllers")]
    static void Generate()
    {
        if (!Directory.Exists(OutPath))
            Directory.CreateDirectory(OutPath);

        BuildCreatureController();
        BuildPlayerController();
        BuildTrapController();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Dark Trail] Animator controllers saved to {OutPath}");
    }

    // ── Creature ──────────────────────────────────────────────────────────────

    static void BuildCreatureController()
    {
        var ctrl = CreateController("Creature");
        var sm   = ctrl.layers[0].stateMachine;

        ctrl.AddParameter("IsChasing",    AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsRetreating", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Attack",       AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Hurt",         AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Die",          AnimatorControllerParameterType.Trigger);

        var idle    = MakeState(ctrl, "Idle",    loop: true);
        var chase   = MakeState(ctrl, "Chase",   loop: true);
        var retreat = MakeState(ctrl, "Retreat", loop: true);
        var attack  = MakeState(ctrl, "Attack",  loop: false);
        var hurt    = MakeState(ctrl, "Hurt",    loop: false);
        var die     = MakeState(ctrl, "Die",     loop: false);

        sm.defaultState = idle;

        // Idle ↔ Chase
        Bool(idle,    chase,   "IsChasing",    true);
        Bool2(chase,  idle,    "IsChasing",    false, "IsRetreating", false);

        // Idle ↔ Retreat
        Bool(idle,    retreat, "IsRetreating", true);
        Bool2(retreat,idle,    "IsRetreating", false, "IsChasing",    false);

        // Chase ↔ Retreat
        Bool(chase,   retreat, "IsRetreating", true);
        Bool(retreat, chase,   "IsChasing",    true);

        // Triggers from any state
        AnyTrigger(sm, attack, "Attack");
        AnyTrigger(sm, hurt,   "Hurt");
        AnyTrigger(sm, die,    "Die");

        // Return to Idle after one-shot clips finish
        ExitTime(attack, idle);
        ExitTime(hurt,   idle);

        EditorUtility.SetDirty(ctrl);
    }

    // ── Player ────────────────────────────────────────────────────────────────

    static void BuildPlayerController()
    {
        var ctrl = CreateController("Player");
        var sm   = ctrl.layers[0].stateMachine;

        ctrl.AddParameter("IsMoving",    AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsAiming",    AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsReloading", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Die",         AnimatorControllerParameterType.Trigger);

        var idle   = MakeState(ctrl, "Idle",   loop: true);
        var walk   = MakeState(ctrl, "Walk",   loop: true);
        var aim    = MakeState(ctrl, "Aim",    loop: true);
        var reload = MakeState(ctrl, "Reload", loop: false);
        var die    = MakeState(ctrl, "Die",    loop: false);

        sm.defaultState = idle;

        // Any → Reload (highest priority via AnyState)
        AnyBool(sm, reload, "IsReloading", true);

        // Reload exits (checked top-to-bottom, first match wins)
        Bool2(reload, walk, "IsReloading", false, "IsMoving", true);
        Bool2(reload, aim,  "IsReloading", false, "IsAiming", true);
        Bool(reload,  idle, "IsReloading", false);

        // Idle ↔ Walk
        Bool2(idle, walk, "IsMoving", true,  "IsAiming", false);
        Bool(walk,  idle, "IsMoving", false);

        // → Aim
        Bool(idle, aim, "IsAiming", true);
        Bool(walk, aim, "IsAiming", true);

        // Aim →
        Bool2(aim, idle, "IsAiming", false, "IsMoving", false);
        Bool2(aim, walk, "IsAiming", false, "IsMoving", true);

        // Die from any state
        AnyTrigger(sm, die, "Die");

        EditorUtility.SetDirty(ctrl);
    }

    // ── Trap ──────────────────────────────────────────────────────────────────

    static void BuildTrapController()
    {
        var ctrl = CreateController("Trap");
        var sm   = ctrl.layers[0].stateMachine;

        ctrl.AddParameter("Activate", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Explode",  AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Reset",    AnimatorControllerParameterType.Trigger);

        var idle       = MakeState(ctrl, "Idle",       loop: true);
        var activation = MakeState(ctrl, "Activation", loop: false);
        var explosion  = MakeState(ctrl, "Explosion",  loop: false);

        sm.defaultState = idle;

        Trigger(idle,       activation, "Activate");
        Trigger(activation, explosion,  "Explode");
        Trigger(explosion,  idle,       "Reset");

        EditorUtility.SetDirty(ctrl);
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    static AnimatorController CreateController(string name)
    {
        string path = $"{OutPath}/{name}.controller";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm   = ctrl.layers[0].stateMachine;
        foreach (var s in sm.states) sm.RemoveState(s.state);
        return ctrl;
    }

    static AnimatorState MakeState(AnimatorController ctrl, string name, bool loop)
    {
        var clip      = new AnimationClip { name = name };
        clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
        AssetDatabase.AddObjectToAsset(clip, ctrl);

        var state    = ctrl.layers[0].stateMachine.AddState(name);
        state.motion = clip;
        return state;
    }

    static void Bool(AnimatorState from, AnimatorState to, string p, bool v)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration    = 0.05f;
        t.AddCondition(v ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, p);
    }

    static void Bool2(AnimatorState from, AnimatorState to,
                      string p1, bool v1, string p2, bool v2)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration    = 0.05f;
        t.AddCondition(v1 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, p1);
        t.AddCondition(v2 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, p2);
    }

    static void Trigger(AnimatorState from, AnimatorState to, string param)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration    = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0f, param);
    }

    static void AnyTrigger(AnimatorStateMachine sm, AnimatorState to, string param)
    {
        var t = sm.AddAnyStateTransition(to);
        t.hasExitTime         = false;
        t.duration            = 0.05f;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0f, param);
    }

    static void AnyBool(AnimatorStateMachine sm, AnimatorState to, string param, bool value)
    {
        var t = sm.AddAnyStateTransition(to);
        t.hasExitTime         = false;
        t.duration            = 0.05f;
        t.canTransitionToSelf = false;
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, param);
    }

    static void ExitTime(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime    = 1f;
        t.duration    = 0.05f;
    }
}

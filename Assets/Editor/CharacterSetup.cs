using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Henry, the player character: Kenney "Mini Characters" male D (CC0). Sets up the model's import
// (generic rig, looping clips) and builds his Animator Controller:
//
//   Base layer:   Locomotion blend (idle → walk → sprint by Speed) ⇄ Jump → Fall → land,
//                 and a one-shot PickUp state (used for both picking up and putting down).
//   Arms layer:   "holding-both" on the arm bones only, weighted in while carrying.
static class CharacterSetup
{
    public const string ModelPath = "Assets/ThirdParty/KenneyCharacters/character-male-d.fbx";
    const string ControllerPath = "Assets/ThirdParty/KenneyCharacters/Henry.controller";
    const string MaskPath = "Assets/ThirdParty/KenneyCharacters/HenryArms.mask";

    static readonly string[] Looping = { "idle", "walk", "sprint", "fall", "holding-both" };

    public static void EnsureImport()
    {
        if (!(AssetImporter.GetAtPath(ModelPath) is ModelImporter importer)) return;
        bool dirty = false;
        if (importer.animationType != ModelImporterAnimationType.Generic)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            dirty = true;
        }
        if (!importer.isReadable) { importer.isReadable = true; dirty = true; }

        var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
        foreach (var c in clips)
        {
            bool loop = Looping.Contains(c.name) || Looping.Contains(c.takeName);
            if (c.loopTime != loop) { c.loopTime = loop; dirty = true; }
        }
        if (dirty)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }

    static AnimationClip Clip(string name) =>
        AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
            .FirstOrDefault(c => c.name == name && !c.name.StartsWith("__preview"));

    public static RuntimeAnimatorController BuildController(GameObject modelPrefab)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("PickUp", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;

        // Locomotion: Speed is 0 standing, 1 walking, 2 sprinting.
        var move = sm.AddState("Locomotion");
        var tree = new BlendTree { name = "Locomotion", blendParameter = "Speed", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(tree, ctrl);
        tree.AddChild(Clip("idle"), 0f);
        tree.AddChild(Clip("walk"), 1f);
        tree.AddChild(Clip("sprint"), 2f);
        move.motion = tree;
        sm.defaultState = move;

        var jump = sm.AddState("Jump");
        jump.motion = Clip("jump");
        var fall = sm.AddState("Fall");
        fall.motion = Clip("fall");
        var pick = sm.AddState("PickUp");
        pick.motion = Clip("pick-up");
        pick.speed = 1.6f;

        AnimatorStateTransition T(AnimatorState from, AnimatorState to, float duration, bool exit = false)
        {
            var t = from.AddTransition(to);
            t.duration = duration;
            t.hasExitTime = exit;
            if (exit) t.exitTime = 0.85f;
            return t;
        }

        T(move, jump, 0.08f).AddCondition(AnimatorConditionMode.If, 0, "Jump");
        T(move, fall, 0.2f).AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        T(jump, fall, 0.25f, exit: true);
        T(jump, move, 0.12f).AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        var land = T(fall, move, 0.12f);
        land.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        T(move, pick, 0.1f).AddCondition(AnimatorConditionMode.If, 0, "PickUp");
        T(pick, move, 0.2f, exit: true);

        // Arms layer for carrying.
        var mask = new AvatarMask();
        mask.AddTransformPath(modelPrefab.transform, true);
        for (int i = 0; i < mask.transformCount; i++)
        {
            string path = mask.GetTransformPath(i);
            mask.SetTransformActive(i, path.EndsWith("arm-left") || path.EndsWith("arm-right"));
        }
        AssetDatabase.DeleteAsset(MaskPath);
        AssetDatabase.CreateAsset(mask, MaskPath);

        var arms = new AnimatorControllerLayer
        {
            name = "Carry Arms",
            defaultWeight = 0f,
            avatarMask = mask,
            stateMachine = new AnimatorStateMachine { name = "Carry Arms" },
        };
        AssetDatabase.AddObjectToAsset(arms.stateMachine, ctrl);
        var hold = arms.stateMachine.AddState("Hold");
        hold.motion = Clip("holding-both");
        arms.stateMachine.defaultState = hold;
        ctrl.AddLayer(arms);

        AssetDatabase.SaveAssets();
        return ctrl;
    }
}

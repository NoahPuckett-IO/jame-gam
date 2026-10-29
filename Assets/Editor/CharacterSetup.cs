using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

static class CharacterSetup
{
    public const string ModelPath = "Assets/ThirdParty/KenneyAnimChar/characterMedium.fbx";
    public const string SkinPath = "Assets/ThirdParty/KenneyAnimChar/Skins/skaterMaleA.png";
    const string AnimPath = "Assets/ThirdParty/UAL/UAL1_Standard.fbx";
    const string ControllerPath = "Assets/ThirdParty/KenneyAnimChar/Henry.controller";
    const string MaskPath = "Assets/ThirdParty/KenneyAnimChar/HenryArms.mask";
    const string MaterialPath = "Assets/ThirdParty/KenneyAnimChar/Henry.mat";

    static readonly string[] Looping =
    {
        "Idle_Loop", "Walk_Loop", "Jog_Fwd_Loop", "Sprint_Loop", "Jump_Loop", "Crouch_Fwd_Loop", "Crouch_Idle_Loop",
        "Driving_Loop", "Swim_Fwd_Loop", "Swim_Idle_Loop", "Push_Loop", "Sitting_Idle_Loop", "Idle_Talking_Loop", "Dance_Loop",
    };

    public static void EnsureImport()
    {
        foreach (var path in new[] { ModelPath, AnimPath })
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;
            bool dirty = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }
            if (path == ModelPath && !importer.isReadable) { importer.isReadable = true; dirty = true; }
            if (path == AnimPath)
            {
                var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
                foreach (var c in clips)
                {
                    string n = Short(c.name);
                    bool loop = Looping.Contains(n);
                    bool walk = n == "Walk_Loop";
                    if (c.loopTime != loop || !c.lockRootRotation || !c.keepOriginalPositionY || !c.keepOriginalOrientation || c.rotationOffset != 180f
                        || c.lockRootPositionXZ == walk)
                    {
                        c.loopTime = loop;
                        c.lockRootRotation = true;
                        c.keepOriginalOrientation = true;
                        c.rotationOffset = 180f;
                        c.lockRootHeightY = true;
                        c.keepOriginalPositionY = true;
                        c.lockRootPositionXZ = !walk;
                        c.keepOriginalPositionXZ = !walk;
                        dirty = true;
                    }
                }
                if (dirty) importer.clipAnimations = clips;
            }
            if (dirty) importer.SaveAndReimport();
        }
    }

    static string Short(string clipName) => clipName.Contains("|") ? clipName.Substring(clipName.LastIndexOf('|') + 1) : clipName;

    public static AnimationClip Clip(string name) =>
        AssetDatabase.LoadAllAssetsAtPath(AnimPath).OfType<AnimationClip>()
            .FirstOrDefault(c => Short(c.name) == name && !c.name.StartsWith("__preview"));

    public static Material HenryMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        var skin = AssetDatabase.LoadAssetAtPath<Texture2D>(SkinPath);
        mat.SetTexture("_BaseMap", skin);
        mat.color = new Color(1.08f, 1.05f, 1.02f);
        mat.SetFloat("_Smoothness", 0.18f);
        mat.EnableKeyword("_EMISSION");
        mat.SetTexture("_EmissionMap", skin);
        // a good share of his own colours as glow, so shadows and dim corners never turn him into a silhouette
        mat.SetColor("_EmissionColor", new Color(0.42f, 0.42f, 0.42f));
        // without this flag URP treats the emission as black whenever it revalidates the material and drops the glow
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static RuntimeAnimatorController BuildController()
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("PickUp", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Dash", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;

        var move = sm.AddState("Locomotion");
        var tree = new BlendTree { name = "Locomotion", blendParameter = "Speed", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(tree, ctrl);
        tree.AddChild(Clip("Idle_Loop"), 0f);
        tree.AddChild(Clip("Walk_Loop"), 4f);
        tree.AddChild(Clip("Jog_Fwd_Loop"), 11f);
        tree.AddChild(Clip("Sprint_Loop"), 22f);
        move.motion = tree;
        sm.defaultState = move;

        AnimatorState State(string name, string clip, float speed = 1f)
        {
            var s = sm.AddState(name);
            s.motion = Clip(clip);
            s.speed = speed;
            return s;
        }
        var jumpStart = State("JumpStart", "Jump_Start", 1.6f);
        var jumpLoop = State("JumpLoop", "Jump_Loop");
        var jumpLand = State("JumpLand", "Jump_Land", 1.8f);
        var pick = State("PickUp", "PickUp_Table", 1.7f);
        var hit = State("Hit", "Hit_Chest", 1.3f);
        var dash = State("Dash", "Roll", 2.4f);
        State("Crawl", "Crouch_Fwd_Loop");
        State("CrouchIdle", "Crouch_Idle_Loop");
        State("WakeUp", "Sitting_Exit");
        State("Sit", "Sitting_Idle_Loop");
        State("Flail", "Swim_Fwd_Loop", 1.8f);
        State("Push", "Push_Loop");
        State("Death", "Death01");
        State("LookAround", "Idle_Talking_Loop");
        State("Hang", "Jump_Loop", 0.6f);
        State("Climb", "Idle_Loop", 0.5f);
        State("Lie", "Idle_Loop", 0.35f);

        AnimatorStateTransition T(AnimatorState from, AnimatorState to, float duration, bool exit = false, float exitTime = 0.9f)
        {
            var t = from.AddTransition(to);
            t.duration = duration;
            t.hasExitTime = exit;
            if (exit) t.exitTime = exitTime;
            return t;
        }

        T(move, jumpStart, 0.08f).AddCondition(AnimatorConditionMode.If, 0, "Jump");
        T(move, jumpLoop, 0.25f).AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        T(jumpStart, jumpLoop, 0.15f, exit: true, exitTime: 0.7f);
        T(jumpStart, jumpLand, 0.1f).AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        T(jumpLoop, jumpLand, 0.08f).AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        T(jumpLand, move, 0.15f, exit: true, exitTime: 0.5f);
        var landToMove = T(jumpLand, move, 0.1f);
        landToMove.AddCondition(AnimatorConditionMode.Greater, 3f, "Speed");
        T(move, pick, 0.1f).AddCondition(AnimatorConditionMode.If, 0, "PickUp");
        T(pick, move, 0.2f, exit: true, exitTime: 0.8f);
        var anyHit = sm.AddAnyStateTransition(hit);
        anyHit.duration = 0.05f;
        anyHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        T(hit, move, 0.2f, exit: true, exitTime: 0.85f);
        var anyDash = sm.AddAnyStateTransition(dash);
        anyDash.duration = 0.05f;
        anyDash.AddCondition(AnimatorConditionMode.If, 0, "Dash");
        T(dash, move, 0.15f, exit: true, exitTime: 0.8f);

        var mask = new AvatarMask();
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
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
        hold.motion = Clip("Driving_Loop");
        hold.speed = 0.2f;
        arms.stateMachine.defaultState = hold;
        ctrl.AddLayer(arms);

        var layers = ctrl.layers;
        layers[0].iKPass = true;
        layers[1].iKPass = true;
        ctrl.layers = layers;

        AssetDatabase.SaveAssets();
        return ctrl;
    }
}

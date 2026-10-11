using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Imports the CC0 Quaternius knight as a humanoid and prepares its runtime prefab.</summary>
[InitializeOnLoad]
public sealed class KnightCharacterImporter : AssetPostprocessor
{
    private const string ModelPath = "Assets/ShadowVeil/Art/Characters/KnightCharacter.fbx";
    private const string ControllerPath = "Assets/ShadowVeil/Art/Characters/Resources/KnightWarrior.controller";
    private const string PrefabPath = "Assets/ShadowVeil/Art/Characters/Resources/KnightWarrior.prefab";

    static KnightCharacterImporter()
    {
        EditorApplication.delayCall += EnsureModelImported;
    }

    private static void EnsureModelImported()
    {
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null) return;
        ModelImporterClipAnimation[] configuredAnimations = importer.clipAnimations;
        bool clipsNeedSetup = configuredAnimations == null || configuredAnimations.Length == 0 || configuredAnimations.Any(animation =>
        {
            string name = animation.name.ToLowerInvariant();
            bool shouldLoop = name.Contains("idle") || name.Contains("walking") || name.EndsWith("run");
            return animation.loopTime != shouldLoop || animation.loopPose != shouldLoop;
        });
        if (importer.animationType != ModelImporterAnimationType.Human || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel || !importer.importAnimation || clipsNeedSetup)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.optimizeGameObjects = false;
            ModelImporterClipAnimation[] animations = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation animation in animations)
            {
                string name = animation.name.ToLowerInvariant();
                animation.loopTime = name.Contains("idle") || name.Contains("walking") || name.EndsWith("run");
                animation.loopPose = animation.loopTime;
            }
            importer.clipAnimations = animations;
            importer.SaveAndReimport();
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) BuildRuntimeAssets();
    }

    private void OnPreprocessModel()
    {
        if (!string.Equals(assetPath, ModelPath, StringComparison.Ordinal)) return;
        ModelImporter importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.optimizeGameObjects = false;
    }

    private void OnPreprocessAnimation()
    {
        if (!string.Equals(assetPath, ModelPath, StringComparison.Ordinal)) return;
        ModelImporter importer = (ModelImporter)assetImporter;
        ModelImporterClipAnimation[] animations = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation animation in animations)
        {
            string name = animation.name.ToLowerInvariant();
            animation.loopTime = name.Contains("idle") || name.Contains("walking") || name.EndsWith("run");
            animation.loopPose = animation.loopTime;
        }
        importer.clipAnimations = animations;
    }

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (importedAssets.Any(path => string.Equals(path, ModelPath, StringComparison.Ordinal)))
            EditorApplication.delayCall += BuildRuntimeAssets;
    }

    private static void BuildRuntimeAssets()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) return;

        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<AnimationClip>()
            .Where(clip => clip != null && !clip.name.StartsWith("__", StringComparison.Ordinal))
            .ToArray();
        if (clips.Length == 0)
        {
            Debug.LogWarning("KnightCharacter.fbx imported without animation clips.");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null) AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        AnimationClip idle = FindClip(clips, "idle") ?? clips[0];
        AnimationClip walk = FindClip(clips, "walking") ?? FindClip(clips, "run") ?? idle;
        AnimationClip attack = FindClip(clips, "swordattack") ?? FindClip(clips, "attack") ?? idle;
        AnimationClip death = FindClip(clips, "death") ?? idle;
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idleState = machine.AddState("Idle"); idleState.motion = idle;
        AnimatorState walkState = machine.AddState("Walk"); walkState.motion = walk;
        AnimatorState attackState = machine.AddState("Attack"); attackState.motion = attack;
        AnimatorState deathState = machine.AddState("Death"); deathState.motion = death;
        machine.defaultState = idleState;

        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.hasExitTime = false; idleToWalk.duration = 0.12f;
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0f, "Moving");
        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.hasExitTime = false; walkToIdle.duration = 0.12f;
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");
        AnimatorStateTransition attackTransition = machine.AddAnyStateTransition(attackState);
        attackTransition.hasExitTime = false; attackTransition.duration = 0.08f;
        attackTransition.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true; attackToIdle.exitTime = 0.92f; attackToIdle.duration = 0.12f;
        AnimatorStateTransition deathTransition = machine.AddAnyStateTransition(deathState);
        deathTransition.hasExitTime = false; deathTransition.duration = 0.08f;
        deathTransition.AddCondition(AnimatorConditionMode.If, 0f, "Death");

        GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (instance == null) instance = UnityEngine.Object.Instantiate(model);
        instance.name = "KnightWarrior";
        Animator animator = instance.GetComponentInChildren<Animator>();
        if (animator == null) animator = instance.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        UnityEngine.Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        Debug.Log("Prepared KnightWarrior prefab with " + clips.Length + " imported animation clips.");
    }

    private static AnimationClip FindClip(AnimationClip[] clips, string token)
    {
        return clips
            .OrderBy(clip => clip.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) == 0 ? 0 : 1)
            .FirstOrDefault(clip => clip.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}

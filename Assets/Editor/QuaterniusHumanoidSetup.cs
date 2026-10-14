#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Imports the CC0 Quaternius humanoid clips and creates reusable guide/NPC prefabs.</summary>
[InitializeOnLoad]
internal static class QuaterniusHumanoidSetup
{
    private const string AnimationFbx = "Assets/ImportedAssets/Quaternius/Animations/UAL1_Standard.fbx";
    private const string MaleFbx = "Assets/ImportedAssets/Quaternius/BaseCharacters/Superhero_Male_FullBody.fbx";
    private const string FemaleFbx = "Assets/ImportedAssets/Quaternius/BaseCharacters/Superhero_Female_FullBody.fbx";
    private const string ResourcesFolder = "Assets/Resources/LumenPeople";
    private const string ControllerPath = ResourcesFolder + "/QuaterniusHumanoid.controller";

    static QuaterniusHumanoidSetup()
    {
        EditorApplication.delayCall += Prepare;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    internal static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!AssetDatabase.LoadMainAssetAtPath(AnimationFbx)) return;

        bool changed = ConfigureHumanoidImporter(AnimationFbx);
        changed |= ConfigureHumanoidImporter(MaleFbx);
        changed |= ConfigureHumanoidImporter(FemaleFbx);
        if (changed) return; // SaveAndReimport queues a second, clean pass.

        EnsureFolder("Assets/Resources", "LumenPeople");
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) controller = CreateController();
        if (controller == null) return;

        CreateCharacterPrefab(MaleFbx, ResourcesFolder + "/GuideMale.prefab", controller);
        CreateCharacterPrefab(FemaleFbx, ResourcesFolder + "/GuideFemale.prefab", controller);
        AssetDatabase.SaveAssets();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Prepare;
    }

    private static bool ConfigureHumanoidImporter(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return false;
        if (importer.animationType == ModelImporterAnimationType.Human && importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel && importer.importAnimation)
            return false;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.SaveAndReimport();
        Debug.Log("Configured the Quaternius character/animation rig for Unity Humanoid retargeting: " + path);
        return true;
    }

    private static AnimatorController CreateController()
    {
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(AnimationFbx)
            .OfType<AnimationClip>()
            .Where(clip => clip != null && clip.length > 0.02f && !clip.name.StartsWith("__preview__"))
            .GroupBy(clip => clip.name)
            .Select(group => group.First())
            .ToArray();
        if (clips.Length == 0)
        {
            Debug.LogWarning("Quaternius animation FBX imported but Unity did not expose any clips yet. Refresh the project after import to retry.");
            return null;
        }
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = null;
        AnimatorState fallbackIdle = null;
        foreach (AnimationClip clip in clips)
        {
            AnimatorState state = machine.AddState(clip.name);
            state.motion = clip;
            state.writeDefaultValues = true;
            string clipLower = clip.name.ToLowerInvariant();
            if (clipLower.EndsWith("idle_loop") || clipLower.EndsWith("|idle") || clipLower.Equals("idle"))
                idle = state;
            else if (fallbackIdle == null && clipLower.Contains("idle") && !clipLower.Contains("swim") && !clipLower.Contains("crouch"))
                fallbackIdle = state;
        }
        idle ??= fallbackIdle;
        if (idle == null) idle = machine.states[0].state;
        machine.defaultState = idle;
        EditorUtility.SetDirty(controller);
        Debug.Log("Created a retargetable Quaternius animation controller with " + clips.Length + " clips; default=" + idle.name + ".");
        return controller;
    }

    private static void CreateCharacterPrefab(string modelPath, string prefabPath, AnimatorController controller)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null) return;
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault(item => item != null && item.isHuman);
        if (avatar == null)
        {
            Debug.LogWarning("No valid humanoid Avatar was created for " + modelPath + ". The imported prefab was not generated.");
            return;
        }

        GameObject instance = Object.Instantiate(model);
        instance.name = model.name;
        Animator animator = instance.GetComponentInChildren<Animator>();
        if (animator == null) animator = instance.AddComponent<Animator>();
        animator.avatar = avatar;
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
        Debug.Log("Created the reusable Quaternius NPC prefab: " + prefabPath);
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent)) return;
        string full = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(full)) AssetDatabase.CreateFolder(parent, child);
    }
}

internal sealed class QuaterniusAssetImportWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (!importedAssets.Any(path => path.StartsWith("Assets/ImportedAssets/Quaternius/", System.StringComparison.OrdinalIgnoreCase))) return;
        EditorApplication.delayCall += QuaterniusHumanoidSetup.Prepare;
    }
}
#endif

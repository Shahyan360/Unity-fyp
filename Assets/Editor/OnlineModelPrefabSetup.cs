#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Turns the glTFast-imported online models into Resources prefabs used by the runtime exhibits.</summary>
[InitializeOnLoad]
internal static class OnlineModelPrefabSetup
{
    private const string ResourceFolder = "Assets/Resources/LumenExhibits";
    private static readonly (string source, string destination)[] Models =
    {
        ("Assets/ImportedAssets/OnlineModels/NASA/Earth.glb", ResourceFolder + "/NasaEarth.prefab"),
        ("Assets/ImportedAssets/OnlineModels/Smithsonian/Triceratops.glb", ResourceFolder + "/SmithsonianTriceratops.prefab"),
        ("Assets/ImportedAssets/OnlineModels/Smithsonian/Fern_Cyathea_furfuracea.glb", ResourceFolder + "/SmithsonianFern.prefab"),
        ("Assets/ImportedAssets/OnlineModels/Smithsonian/George_Washington_Bust.glb", ResourceFolder + "/SmithsonianGeorgeWashingtonBust.prefab"),
        ("Assets/ImportedAssets/OnlineModels/Smithsonian/Apollo_11_Command_Module.glb", ResourceFolder + "/SmithsonianApollo11CommandModule.prefab")
    };

    static OnlineModelPrefabSetup()
    {
        EditorApplication.delayCall += Prepare;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Prepare;
    }

    internal static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EnsureResourcesFolder();

        bool created = false;
        foreach ((string source, string destination) in Models)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(destination) != null) continue;
            GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(source);
            if (imported == null)
            {
                Debug.LogWarning("Online GLB is not imported yet; will retry after the model refreshes: " + source);
                continue;
            }

            GameObject instance = UnityEngine.Object.Instantiate(imported);
            instance.name = System.IO.Path.GetFileNameWithoutExtension(destination);
            PrefabUtility.SaveAsPrefabAsset(instance, destination);
            UnityEngine.Object.DestroyImmediate(instance);
            created = true;
            Debug.Log("Created a runtime-ready GLB model prefab: " + destination);
        }

        if (created) AssetDatabase.SaveAssets();
    }

    [MenuItem("Lumen/Prepare Imported Learning Models")]
    private static void PrepareFromMenu()
    {
        Prepare();
    }

    [MenuItem("Lumen/Reimport Downloaded Learning Models")]
    private static void ReimportDownloadedModels()
    {
        foreach (var model in Models)
        {
            string source = model.source;
            if (!File.Exists(source))
            {
                Debug.LogWarning("Downloaded model source file is missing: " + source);
                continue;
            }
            AssetDatabase.ImportAsset(source, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
        Prepare();
    }

    private static void EnsureResourcesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) return;
        if (!AssetDatabase.IsValidFolder(ResourceFolder)) AssetDatabase.CreateFolder("Assets/Resources", "LumenExhibits");
    }
}

internal sealed class OnlineModelPrefabImportWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (!importedAssets.Any(path => path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)
            && path.IndexOf("Assets/ImportedAssets/OnlineModels/", StringComparison.OrdinalIgnoreCase) >= 0)) return;
        EditorApplication.delayCall += OnlineModelPrefabSetup.Prepare;
    }
}
#endif

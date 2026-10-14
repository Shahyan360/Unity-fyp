#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Makes the licensed in-project robot guide available to standalone builds via Resources.</summary>
[InitializeOnLoad]
internal static class GuidePrefabResources
{
    private const string Source = "Assets/UnityTechnologies/SpaceRobotKyle/Prefabs/RobotKyle.prefab";
    private const string Folder = "Assets/Resources/LumenGuide";
    private const string Destination = Folder + "/RobotKyle.prefab";

    static GuidePrefabResources()
    {
        EditorApplication.delayCall += EnsureCopy;
    }

    private static void EnsureCopy()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Destination) != null) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Source) == null) return;
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources", "LumenGuide");
        if (AssetDatabase.CopyAsset(Source, Destination))
        {
            AssetDatabase.ImportAsset(Destination, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Lumen guide model is ready for play mode and standalone builds.");
        }
    }
}
#endif

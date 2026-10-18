using System.Collections.Generic;
using UnityEngine;

/// <summary>Loads and comfortably fits the downloaded, glTFast-imported models into world exhibits.</summary>
public static class ImportedModelUtility
{
    private static readonly HashSet<string> warnedResources = new HashSet<string>();

    public static GameObject AddFittedModel(string resourceName, Transform parent, float maxExtent, float baseHeight, float yaw = 0f,float pitch=0f,float roll=0f)
    {
        if (parent == null) return null;
        GameObject prefab = Resources.Load<GameObject>("LumenExhibits/" + resourceName);
        if (prefab == null)
        {
            if (warnedResources.Add(resourceName)) Debug.LogWarning("Downloaded model prefab is not available in Resources/LumenExhibits: " + resourceName);
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, parent, false);
        instance.name = resourceName.Replace("Smithsonian", "").Replace("Nasa", "") + " 3D model";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.AngleAxis(roll,Vector3.forward)*Quaternion.Euler(pitch,yaw,0f);
        instance.transform.localScale = Vector3.one;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Object.Destroy(instance);
            return null;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        float sourceExtent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (sourceExtent < 0.001f)
        {
            Object.Destroy(instance);
            return null;
        }

        instance.transform.localScale = Vector3.one * (Mathf.Max(0.1f, maxExtent) / sourceExtent);
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        Vector3 localCenter = parent.InverseTransformPoint(bounds.center);
        Vector3 localMinimum = parent.InverseTransformPoint(bounds.min);
        instance.transform.localPosition = new Vector3(-localCenter.x, baseHeight - localMinimum.y, -localCenter.z);
        return instance;
    }
}

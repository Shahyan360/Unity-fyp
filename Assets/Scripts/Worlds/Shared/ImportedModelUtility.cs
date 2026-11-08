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
        if (parent.GetComponentInParent<ObjectMetadata>() != null)
            AddSelectionVolume(instance, renderers);
        return instance;
    }

    private static void AddSelectionVolume(GameObject instance, Renderer[] renderers)
    {
        Transform model = instance.transform;
        Bounds selection = new Bounds(model.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
        foreach (Renderer renderer in renderers)
        {
            Bounds bounds = renderer.localBounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = new Vector3((corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                selection.Encapsulate(model.InverseTransformPoint(renderer.transform.TransformPoint(point)));
            }
        }
        var collider = instance.AddComponent<BoxCollider>();
        collider.center = selection.center;
        collider.size = selection.size;
        collider.isTrigger = true;
    }
}

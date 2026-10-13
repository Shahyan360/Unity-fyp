using System.Collections.Generic;
using UnityEngine;

/// <summary>Searchable context attached to meaningful classroom objects.</summary>
[DisallowMultipleComponent]
public sealed class ObjectMetadata : MonoBehaviour
{
    [Tooltip("Stable identifier used by classroom AI systems.")]
    public string objectId;
    public string displayName;
    public string category;
    [TextArea(2, 5)] public string descriptionSummary;
    [Tooltip("Comma-separated words such as teaching, board, front-wall.")]
    public string contextualTags;
    public bool isInteractable;
    public string interactionHint;
    [Header("Group Project Compatibility")]
    public string objectName;
    public string objectType;
    [TextArea(3, 6)] public string description;
    [Tooltip("Scenario identifier such as academic_tutor or historical_guide.")]
    public string scenario;

    public List<string> GetContextualTags()
    {
        var results = new List<string>();
        if (string.IsNullOrWhiteSpace(contextualTags)) return results;

        string[] tags = contextualTags.Split(new[] { ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string tag in tags)
        {
            string cleaned = tag.Trim();
            if (cleaned.Length > 0 && !results.Contains(cleaned)) results.Add(cleaned);
        }
        return results;
    }

    public bool HasTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return false;
        foreach (string item in GetContextualTags())
            if (string.Equals(item, tag.Trim(), System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(objectName)) return objectName.Trim();
        if (!string.IsNullOrWhiteSpace(displayName)) return displayName.Trim();
        return gameObject.name;
    }

    public string ToAIContext()
    {
        string label = GetDisplayName();
        string type = string.IsNullOrWhiteSpace(category) ? objectType : category;
        string context = string.IsNullOrWhiteSpace(descriptionSummary) ? description : descriptionSummary;
        if (string.IsNullOrWhiteSpace(context)) context = label;
        return string.Format("{0} (id: {1}, category: {2}). {3} Tags: {4}{5}", label, objectId, type,
            context, contextualTags, isInteractable ? " Interaction: " + interactionHint : string.Empty);
    }

    public static ObjectMetadata FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        ObjectMetadata[] all = FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ObjectMetadata item in all)
            if (item != null && string.Equals(item.objectId, id, System.StringComparison.OrdinalIgnoreCase)) return item;
        return null;
    }

    public static List<ObjectMetadata> FindByTag(string tag)
    {
        var results = new List<ObjectMetadata>();
        if (string.IsNullOrWhiteSpace(tag)) return results;
        ObjectMetadata[] all = FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ObjectMetadata item in all)
            if (item != null && item.HasTag(tag)) results.Add(item);
        return results;
    }
}



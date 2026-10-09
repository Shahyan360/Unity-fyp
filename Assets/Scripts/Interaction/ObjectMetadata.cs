using UnityEngine;

public class ObjectMetadata : MonoBehaviour
{
    [Header("Object Information")]
    public string objectName;
    public string objectType;

    [TextArea(3, 6)]
    public string description;

    [Header("Scenario")]
    [Tooltip("Use: academic_tutor, historical_guide, or social_coach")]
    public string scenario = "academic_tutor";

    public string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(objectName))
        {
            return objectName;
        }

        return gameObject.name;
    }
}


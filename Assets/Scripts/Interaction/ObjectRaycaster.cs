using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectRaycaster : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera raycastCamera;
    [SerializeField] private float raycastDistance = 5f;
    [SerializeField] private LayerMask raycastLayers = ~0;

    [Header("UI Label")]
    [SerializeField] private TMP_Text detectedObjectTMPLabel;
    [SerializeField] private Text detectedObjectLegacyLabel;

    public ObjectMetadata CurrentObject { get; private set; }
    private readonly RaycastHit[] hits = new RaycastHit[24];
    private static readonly Vector2[] AimOffsets = { Vector2.zero, new(.018f, 0), new(-.018f, 0), new(0, .018f), new(0, -.018f) };

    private void Awake()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }
    }

    private void LateUpdate() => UpdateCurrentObject();

    public ObjectMetadata RefreshTarget()
    {
        UpdateCurrentObject();
        return CurrentObject;
    }

    private void UpdateCurrentObject()
    {
        CurrentObject = null;
        if (raycastCamera == null || !raycastCamera.isActiveAndEnabled) raycastCamera = Camera.main;

        if (raycastCamera == null)
        {
            SetDetectedObjectLabel("No camera assigned");
            return;
        }

        if (LumenFrontPage.IsOpen) return;
        foreach (Vector2 offset in AimOffsets)
        {
            Ray ray = raycastCamera.ViewportPointToRay(new Vector3(.5f + offset.x, .5f + offset.y, 0f));
            CurrentObject = FindVisibleTarget(ray);
            if (CurrentObject != null) break;
        }

        if (CurrentObject != null)
        {
            CurrentObject.GetComponentInParent<LearningModelInspection>()?.Select();
            SetDetectedObjectLabel(CurrentObject.GetDisplayName());
        }
        else
        {
            SetDetectedObjectLabel("No object selected");
        }
    }

    private ObjectMetadata FindVisibleTarget(Ray ray)
    {
        int count = Physics.RaycastNonAlloc(ray, hits, Mathf.Max(8f, raycastDistance), raycastLayers, QueryTriggerInteraction.Collide);
        var visitor = raycastCamera.GetComponentInParent<CharacterController>();
        float nearest = float.PositiveInfinity;
        ObjectMetadata selected = null;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.distance >= nearest || hit.collider == visitor) continue;
            var metadata = hit.collider.GetComponentInParent<ObjectMetadata>();
            if (metadata == null && hit.collider.isTrigger) continue;
            nearest = hit.distance;
            selected = metadata;
        }
        return selected;
    }

    private void SetDetectedObjectLabel(string message)
    {
        if (detectedObjectTMPLabel != null)
        {
            detectedObjectTMPLabel.text = message;
        }

        if (detectedObjectLegacyLabel != null)
        {
            detectedObjectLegacyLabel.text = message;
        }
    }
}


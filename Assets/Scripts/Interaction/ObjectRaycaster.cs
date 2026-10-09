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

    private void Awake()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }
    }

    private void Update()
    {
        UpdateCurrentObject();
    }

    private void UpdateCurrentObject()
    {
        CurrentObject = null;

        if (raycastCamera == null)
        {
            SetDetectedObjectLabel("No camera assigned");
            return;
        }

        Ray ray = raycastCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, raycastLayers))
        {
            CurrentObject = hit.collider.GetComponentInParent<ObjectMetadata>();
        }

        if (CurrentObject != null)
        {
            SetDetectedObjectLabel(CurrentObject.GetDisplayName());
        }
        else
        {
            SetDetectedObjectLabel("No object selected");
        }
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


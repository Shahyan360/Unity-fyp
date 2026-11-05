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
    private ObjectMetadata lastViewed;
    private float lastViewedAt;
    private TextMeshPro talkPrompt;
    public ObjectMetadata ConversationTarget => CurrentObject != null ? CurrentObject
        : Time.unscaledTime - lastViewedAt < 6f ? lastViewed : null;

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
        if (talkPrompt != null) talkPrompt.gameObject.SetActive(false);
        if (CurrentObject != null && CurrentObject.category == "Person")
        {
            if (talkPrompt == null)
            {
                var prompt = new GameObject("Talk to this person", typeof(TextMeshPro));
                prompt.transform.SetParent(transform, false);
                talkPrompt = prompt.GetComponent<TextMeshPro>();
                talkPrompt.fontSize = 2.5f;
                talkPrompt.alignment = TextAlignmentOptions.Center;
                talkPrompt.rectTransform.sizeDelta = new Vector2(3, .65f);
            }
            talkPrompt.gameObject.SetActive(true);
            talkPrompt.text = CurrentObject.GetDisplayName() + "\n[E] Talk";
            talkPrompt.transform.position = CurrentObject.transform.position + Vector3.up * 2.15f;
            if (raycastCamera != null) talkPrompt.transform.rotation = raycastCamera.transform.rotation;
        }
    }

    public ObjectMetadata RefreshTarget()
    {
        UpdateCurrentObject();
        return ConversationTarget;
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

        Ray ray = raycastCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, raycastLayers, QueryTriggerInteraction.Collide))
        {
            CurrentObject = hit.collider.GetComponentInParent<ObjectMetadata>();
        }

        if (CurrentObject != null)
        {
            lastViewed=CurrentObject;lastViewedAt=Time.unscaledTime;
            CurrentObject.GetComponentInParent<LearningModelInspection>()?.Select();
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


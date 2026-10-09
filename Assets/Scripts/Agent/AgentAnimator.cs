using UnityEngine;

public class AgentAnimator : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private Renderer[] emotionRenderers;
    [SerializeField] private Light statusLight;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (emotionRenderers == null || emotionRenderers.Length == 0)
        {
            emotionRenderers = GetComponentsInChildren<Renderer>();
        }

        if (statusLight == null)
        {
            statusLight = GetComponentInChildren<Light>();
        }
    }

    public void ApplyResponseTags(string action, string emotion)
    {
        // For Phase 1, we only log emotion.
        // Later, we can connect emotions to facial expressions.
        ApplyEmotion(emotion);

        // Play simple action animation trigger.
        PlayAction(action);
    }

    public void ApplyEmotion(string emotion)
    {
        string normalizedEmotion = NormalizeTag(emotion, "neutral");

        Color colour = normalizedEmotion switch
        {
            "excited" => new Color(0.25f, 0.95f, 0.65f),
            "concerned" => new Color(1f, 0.58f, 0.32f),
            "thinking" => new Color(0.42f, 0.66f, 1f),
            "confident" => new Color(0.78f, 0.45f, 1f),
            _ => new Color(0.30f, 0.88f, 1f)
        };

        if (statusLight != null)
        {
            statusLight.color = colour;
        }
    }

    public void PlayAction(string action)
    {
        if (animator == null)
        {
            Debug.Log("No Animator found on assistant. Action received: " + action);
            return;
        }

        string normalizedAction = NormalizeTag(action, "idle");

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        switch (normalizedAction)
        {
            case "nod":
                animator.SetTrigger("nod");
                break;

            case "gesture":
                animator.SetTrigger("gesture");
                break;

            case "point":
                animator.SetTrigger("point");
                break;

            case "explain":
                animator.SetTrigger("explain");
                break;

            case "encourage":
                animator.SetTrigger("encourage");
                break;

            case "idle":
            default:
                animator.SetTrigger("idle");
                break;
        }
    }

    private string NormalizeTag(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Trim().ToLowerInvariant();
    }
}

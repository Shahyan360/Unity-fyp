using UnityEngine;

public class AgentResponseHandler : MonoBehaviour
{
    [Header("Agent")]
    [SerializeField] private AgentAnimator agentAnimator;
    [SerializeField] private AgentCompanionController companionController;
    [SerializeField] private SpeechOutputController speechOutput;

    private void Awake()
    {
        if (agentAnimator == null)
        {
            agentAnimator = FindObjectOfType<AgentAnimator>();
        }

        if (companionController == null)
        {
            companionController = FindFirstObjectByType<AgentCompanionController>();
        }

        if (speechOutput == null)
        {
            speechOutput = FindFirstObjectByType<SpeechOutputController>();
        }
    }

    public void HandleResponse(ChatResponse response)
    {
        if (response == null)
        {
            return;
        }

        if (agentAnimator != null)
        {
            agentAnimator.ApplyResponseTags(response.action, response.emotion);
        }

        if (companionController != null && !string.IsNullOrWhiteSpace(response.navigation_target))
        {
            companionController.TryNavigateToNamedTarget(response.navigation_target);
        }

        if (speechOutput != null)
        {
            speechOutput.Speak(response.response);
        }
    }
}

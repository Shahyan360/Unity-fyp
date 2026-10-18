using UnityEngine;

/// <summary>
/// Upgrades the original prototype scene at runtime without fragile manual scene edits.
/// It is invoked by AIClient, already present in both shipped Unity scenes.
/// </summary>
public static class EmbodiedExperienceBootstrap
{
    public static void Ensure(AIClient client)
    {
        // Leave GPU time for local inference instead of rendering hundreds of
        // unseen frames while the visitor waits for a conversational response.
        if(Application.targetFrameRate<0||Application.targetFrameRate>60)Application.targetFrameRate=60;
        Application.runInBackground=true;
        if (client == null || client.GetComponent<ScenarioRuntimeManager>() != null)
        {
            return;
        }

        client.gameObject.AddComponent<ScenarioRuntimeManager>();

        GameObject assistant = GameObject.Find("Assistant") ?? GameObject.Find("HomeAgent");
        if (assistant != null)
        {
            if (assistant.GetComponent<AgentCompanionController>() == null && assistant.GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
            {
                assistant.AddComponent<AgentCompanionController>();
            }

            if (assistant.GetComponent<SpeechOutputController>() == null)
            {
                assistant.AddComponent<SpeechOutputController>();
            }
        }

        if (Object.FindAnyObjectByType<SocialCoachingController>() == null)
        {
            client.gameObject.AddComponent<SocialCoachingController>();
        }

        if (client.GetComponent<SpeechInputController>() == null)
        {
            client.gameObject.AddComponent<SpeechInputController>();
        }
    }
}

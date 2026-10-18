using UnityEngine;
using UnityEngine.SceneManagement;

public class AgentResponseHandler : MonoBehaviour
{
    private const string PendingExhibitScenarioKey = "Lumen.PendingExhibitScenario";
    private const string PendingExhibitTargetKey = "Lumen.PendingExhibitTarget";
    [Header("Agent")]
    [SerializeField] private AgentAnimator agentAnimator;
    [SerializeField] private AgentCompanionController companionController;
    [SerializeField] private SpeechOutputController speechOutput;

    private void Awake()
    {
        if (agentAnimator == null)
        {
            agentAnimator = FindAnyObjectByType<AgentAnimator>();
        }

        if (companionController == null)
        {
            companionController = FindAnyObjectByType<AgentCompanionController>();
        }

        if (speechOutput == null)
        {
            speechOutput = FindAnyObjectByType<SpeechOutputController>();
        }
    }

    public void HandleResponse(ChatResponse response)
    {
        HandleResponse(response, null);
    }

    public void HandleResponse(ChatResponse response, ObjectMetadata responseSpeaker)
    {
        if (response == null)
        {
            return;
        }

        // Unity objects can be destroyed when switching scenes without their
        // managed references becoming CLR-null. Use Unity's overloaded null
        // check so scene-local guide components are reacquired after a load.
        if (agentAnimator == null) agentAnimator = FindAnyObjectByType<AgentAnimator>();
        AgentCompanionController sceneCompanion = FindSceneCompanion();
        if (sceneCompanion != null) companionController = sceneCompanion;
        else if (companionController == null) companionController = FindAnyObjectByType<AgentCompanionController>();
        if (speechOutput == null) speechOutput = FindAnyObjectByType<SpeechOutputController>();

        // Select the actual addressed person; otherwise the active world's host owns the response.
        AgentAnimator speakerAnimator = null;
        if (responseSpeaker != null)
            speakerAnimator = responseSpeaker.GetComponentInParent<AgentAnimator>() ?? responseSpeaker.GetComponentInChildren<AgentAnimator>();
        if (speakerAnimator == null && companionController != null)
            speakerAnimator = companionController.GetComponent<AgentAnimator>();
        if (speakerAnimator != null) agentAnimator = speakerAnimator;
        ScenarioCharacterIdentity identity = agentAnimator != null ? agentAnimator.GetComponent<ScenarioCharacterIdentity>() : null;
        if (identity != null && companionController != null)
            companionController.AttendToVisitor(Mathf.Clamp(response.response.Length * .07f + 2f, 3f, 30f));

        if (agentAnimator != null && !response.spoken_stream)
        {
            agentAnimator.ApplyResponseTags(response.action, response.emotion);
            if (!response.spoken_stream) agentAnimator.SpeakFor(Mathf.Clamp(response.response.Length * 0.055f, 1.8f, 14f));
        }

        if (responseSpeaker != null && !response.spoken_stream)
        {
            AmbientNpcRoutine speakerRoutine = responseSpeaker.GetComponent<AmbientNpcRoutine>() ?? responseSpeaker.GetComponentInParent<AmbientNpcRoutine>();
            if (speakerRoutine != null)
            {
                if (response.scenario == "cafe" && responseSpeaker.category == "Person" &&
                    (responseSpeaker.displayName.IndexOf("Barista", System.StringComparison.OrdinalIgnoreCase) >= 0 || response.response.IndexOf("order", System.StringComparison.OrdinalIgnoreCase) >= 0))
                    speakerRoutine.ReactToOrder();
                else speakerRoutine.ReactToConversation();
            }
        }

        bool changingScene = !string.IsNullOrWhiteSpace(response.next_scenario);
        if (changingScene && response.action == "show_model" && !string.IsNullOrWhiteSpace(response.navigation_target))
        {
            string destination = ScenarioRuntimeManager.NormalizeScenario(response.next_scenario);
            if (destination == "classroom" || destination == "museum")
            {
                PlayerPrefs.SetString(PendingExhibitScenarioKey, destination);
                PlayerPrefs.SetString(PendingExhibitTargetKey, response.navigation_target);
                PlayerPrefs.Save();
            }
        }

        if (response.action == "show_model")
        {
            if (!changingScene)
            {
                string selectedScenario = ScenarioRuntimeManager.NormalizeScenario(response.scenario);
                if (selectedScenario == "museum") MuseumGalleryExperience.ShowArtifact(response.navigation_target);
                else ClassroomLearningExhibit.ShowModel(response.navigation_target);
            }
        }

        if (response.action == "show_catalog" && ScenarioRuntimeManager.NormalizeScenario(response.scenario) == "classroom")
        {
            if (agentAnimator != null)
            {
                GuideExhibitCatalog catalog = agentAnimator.GetComponent<GuideExhibitCatalog>();
                if (catalog == null) catalog = agentAnimator.gameObject.AddComponent<GuideExhibitCatalog>();
                catalog.ShowCatalog();
            }
        }

        if (!changingScene && companionController != null && !string.IsNullOrWhiteSpace(response.navigation_target))
        {
            companionController.TryNavigateToNamedTarget(response.navigation_target);
        }

        if (speechOutput != null && !response.spoken_stream)
        {
            bool conversationPartner = responseSpeaker != null && responseSpeaker.category == "Person";
            string voice = identity != null ? identity.VoiceName : conversationPartner ? SelectPersonVoice(responseSpeaker) : string.Empty;
            string caption = conversationPartner ? responseSpeaker.GetDisplayName() : identity != null ? identity.DisplayName : "Lumen";
            speechOutput.SpeakAs(response.response, voice, caption, agentAnimator, identity != null ? identity.SpeechRate : 0, identity != null ? identity.VoicePitch : 1f);
        }

        if (changingScene) ScenarioTransitionService.LoadScenario(response.next_scenario);
    }

    public void SpeakIncremental(string text,ObjectMetadata person=null)
    {
        var guide=FindAnyObjectByType<ScenarioCharacterIdentity>();
        var actor=person!=null?person.GetComponentInParent<AgentAnimator>():guide!=null?guide.GetComponent<AgentAnimator>():null;
        if(speechOutput==null)speechOutput=FindAnyObjectByType<SpeechOutputController>();
        if(speechOutput==null)return;
        var identity=actor!=null?actor.GetComponent<ScenarioCharacterIdentity>():null;
        if(actor!=null){actor.ApplyEmotion("friendly");actor.GetComponent<AgentCompanionController>()?.AttendToVisitor(20);}
        speechOutput.EnqueueAs(text,identity!=null?identity.VoiceName:"",identity!=null?identity.DisplayName:person!=null?person.GetDisplayName():"Kai",actor,
            identity!=null?identity.SpeechRate:0,identity!=null?identity.VoicePitch:1);
    }

    private static string SelectPersonVoice(ObjectMetadata person)
    {
        var visual=person.GetComponentInChildren<ScenarioCharacterVisual>();
        if(visual!=null&&(visual.ResourceId=="Ada"||visual.ResourceId=="Maya"))return "Microsoft Zira Desktop";
        string name = (person.GetDisplayName() ?? string.Empty).ToLowerInvariant();
        return name.Contains("woman") || name.Contains("female") || name.Contains("barista")
            ? "Microsoft Zira Desktop"
            : "Microsoft David Desktop";
    }

    private static AgentCompanionController FindSceneCompanion()
    {
        string guideName = SceneManager.GetActiveScene().name switch
        {
            "Cafestore" => "Cafe_Follow_Agent",
            "HistoricalPlace" => "HistoricalPlace_Navigation_Demo_Agent",
            "Classroom" => "Classroom_Navigation_Demo_Agent",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(guideName)) return null;
        GameObject guide = GameObject.Find(guideName);
        return guide != null ? guide.GetComponent<AgentCompanionController>() : null;
    }
}

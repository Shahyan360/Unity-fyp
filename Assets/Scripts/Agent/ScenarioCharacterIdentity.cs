using PlayerPrefs = LumenPreferences;
using UnityEngine;

/// <summary>Scenario host identity, presentation, and offline voice preferences.</summary>
[DisallowMultipleComponent]
public sealed class ScenarioCharacterIdentity : MonoBehaviour
{
    [SerializeField] private string characterId = "lumen";
    [SerializeField] private string displayName = "Lumen";
    [SerializeField] private string role = "World guide";
    [SerializeField] private string voiceName = "Microsoft David Desktop";
    [SerializeField] private int speechRate;
    [SerializeField] private float voicePitch = 1f;

    public string CharacterId => characterId;
    public string ConversationId => displayName=="Kai"?"kai":displayName=="Orion"?"orion":displayName=="Professor Theo"?"theo":displayName=="Aurelius"?"aurelius":displayName=="Lucius"?"lucius":displayName=="Maya"?"maya":characterId;
    public string DisplayName => displayName;
    public string Role => role;
    public string VoiceName => voiceName;
    public int SpeechRate => speechRate;
    public float VoicePitch => voicePitch;
    public string NeuralSpeakerLabel => displayName switch
    {
        "Kai"=>"p226","Orion"=>"p237","Professor Theo"=>"p227","Lucius"=>"p243","Aurelius"=>"p245","Maya"=>"p229",
        _=>voiceName.Contains("Zira")?"p229":"p226"
    };

    public static ScenarioCharacterIdentity InstallOnGuide(GameObject guide, string scenario)
    {
        if (guide == null) return null;
        string key = ScenarioRuntimeManager.NormalizeScenario(scenario);
        if (scenario == "HistoricalPlace") key = "ancient_rome";
        else if (scenario == "Cafestore") key = "cafe";
        else if (scenario == "Classroom") key = "classroom";
        else if (scenario == "Museum") key = "museum";
        else if (scenario.StartsWith("Home")) key = "home";

        ScenarioCharacterIdentity identity = guide.GetComponent<ScenarioCharacterIdentity>();
        if (identity == null) identity = guide.AddComponent<ScenarioCharacterIdentity>();
        string resource;
        string description;
        switch (key)
        {
            case "ancient_rome":
                identity.characterId = "marcus"; identity.displayName = "Marcus";
                identity.role = "Historian & reconstruction guide";
                identity.voiceName = "Microsoft David Desktop"; identity.speechRate = -1; identity.voicePitch = 0.96f;
                resource = "Marcus";
                description = "Marcus is a contemporary historian hosting this reconstruction of Ancient Rome. He distinguishes evidence from reconstruction and explains daily life, engineering, and the forum. He is not an ancient Roman eyewitness.";
                break;
            case "cafe":
                identity.characterId = "maya"; identity.displayName = "Maya";
                identity.role = "Café host & conversation coach";
                identity.voiceName = "Microsoft Zira Desktop"; identity.speechRate = 1; identity.voicePitch = 1.04f;
                resource = "Maya";
                description = "Maya is the café host and ordering coach. Practice a friendly greeting, ask about the menu, place a simulated order, and build confidence speaking. She responds warmly and never processes real payments.";
                break;
            case "museum":
                identity.characterId = "imani"; identity.displayName = "Imani";
                identity.role = "Museum curator";
                identity.voiceName = "Microsoft Zira Desktop"; identity.speechRate = -1; identity.voicePitch = 0.98f;
                resource = "Marcus";
                description = "Imani is the museum curator. She connects artifacts to people and places, explains what is known and uncertain, and helps visitors compare objects without inventing provenance.";
                break;
            case "classroom":
                identity.characterId = "ada"; identity.displayName = "Ada";
                identity.role = "Science teacher & learning coach";
                identity.voiceName = "Microsoft Zira Desktop"; identity.speechRate = 0; identity.voicePitch = 1f;
                resource = "Marcus";
                description = "Ada is a patient science teacher. She introduces one exhibit at a time, uses understandable examples, checks understanding, and encourages questions without judging the learner.";
                break;
            case "home":
                identity.characterId="lumen";identity.displayName="Kai";identity.role="Your discovery companion";
                resource="Marcus";description="Kai is a friendly fictional host who welcomes visitors and helps them choose a world.";
                break;
            default:
                return identity;
        }

        string selected=PlayerPrefs.GetString("Lumen.Character."+scenario,CharacterWardrobe.Default(scenario));
        resource=CharacterWardrobe.Resource(selected);
        Transform current = guide.transform.Find("LumenGuideVisual");
        GameObject prefab = Resources.Load<GameObject>("ScenarioCharacters/" + resource);
        if (prefab != null && (current == null || current.GetComponent<ScenarioCharacterVisual>() == null||current.GetComponent<ScenarioCharacterVisual>().ResourceId!=resource))
        {
            if (current != null)
            {
                current.gameObject.SetActive(false);
                current.name = "LegacyGuideVisual";
                Destroy(current.gameObject);
            }
            GameObject visual = Instantiate(prefab, guide.transform, false);
            visual.name = "LumenGuideVisual";
            visual.GetComponent<ScenarioCharacterVisual>().ResourceId=resource;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>(true)) Destroy(body);
            CharacterGrounding grounding = guide.GetComponent<CharacterGrounding>();
            if (grounding == null) grounding = guide.AddComponent<CharacterGrounding>();
            grounding.Initialize(visual.transform);
        }

        ObjectMetadata metadata = guide.GetComponent<ObjectMetadata>();
        if (metadata == null) metadata = guide.AddComponent<ObjectMetadata>();
        metadata.objectId = identity.characterId;
        metadata.displayName = identity.displayName;
        metadata.objectName = identity.displayName;
        metadata.category = "AI companion";
        metadata.descriptionSummary = description;
        metadata.description = description;
        metadata.contextualTags = identity.characterId + ", guide, companion, " + identity.role;
        metadata.isInteractable = true;
        metadata.interactionHint = "Press E to talk with " + identity.displayName;
        metadata.scenario = key;

        Transform hostVisual=guide.transform.Find("LumenGuideVisual");
        if(hostVisual!=null)foreach(Animator legacy in guide.GetComponentsInChildren<Animator>(true))
            if(!legacy.transform.IsChildOf(hostVisual))legacy.enabled=false;
        AgentAnimator animation = guide.GetComponent<AgentAnimator>();
        if (animation == null) animation = guide.AddComponent<AgentAnimator>();
        animation.RefreshVisual();
        identity.ApplyAppearance(selected);
        if (key == "cafe" && guide.transform.Find("Host face fill") == null)
        {
            GameObject fillObject = new GameObject("Host face fill");
            fillObject.transform.SetParent(guide.transform, false);
            fillObject.transform.localPosition = new Vector3(0f, 2.1f, 1.1f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point; fill.range = 3f; fill.intensity = 1.5f;
            fill.color = new Color(1f, .89f, .76f); fill.shadows = LightShadows.None;
        }
        return identity;
    }

    public void ApplyAppearance(string style)
    {
        Transform visual=transform.Find("LumenGuideVisual");
        if(visual==null)return;
        string resource=CharacterWardrobe.Resource(style);
        var marker=visual.GetComponent<ScenarioCharacterVisual>();
        if(marker==null||marker.ResourceId!=resource)
        {
            GameObject source=Resources.Load<GameObject>("ScenarioCharacters/"+resource);
            if(source!=null)
            {
                visual.gameObject.SetActive(false);visual.name="ReplacedCharacterVisual";Destroy(visual.gameObject);
                visual=Instantiate(source,transform,false).transform;visual.name="LumenGuideVisual";
                visual.GetComponent<ScenarioCharacterVisual>().ResourceId=resource;
                GetComponent<CharacterGrounding>()?.Initialize(visual);
            }
        }
        if(style=="CafeHost"||CharacterWardrobe.Apply(visual,style))
        {
            string previousName=displayName;
            displayName=CharacterWardrobe.Label(style);
            ObjectMetadata data=GetComponent<ObjectMetadata>();
            if(data!=null)
            {
                data.objectName=displayName;data.displayName=displayName;data.interactionHint="Press E to talk with "+displayName;
                if(!string.IsNullOrEmpty(previousName))
                {
                    data.description=data.description?.Replace(previousName,displayName);
                    data.descriptionSummary=data.descriptionSummary?.Replace(previousName,displayName);
                }
            }
            voiceName=style=="CafeHost"?"Microsoft Zira Desktop":"Microsoft David Desktop";
        }
        GetComponent<AgentAnimator>()?.RefreshVisual();
        GetComponent<CharacterFacialAnimation>()?.SetRestExpression(style=="OpeningHero"?13:style=="MuseumMystic"?11:style=="Professor"?9:style=="CafeHost"?12:7,style=="MuseumMystic"?4:2);
    }
}

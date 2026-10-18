using System.Collections.Generic;
using TMPro;
using UnityEngine.AI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Creates the polished world hub and lightweight, shared in-world navigation HUD.</summary>
public sealed class WorldExperienceDirector : MonoBehaviour
{
    private static bool listening;
    private static Sprite roundedSprite;
    private static Sprite circleSprite;
    private static Sprite ringSprite;
    private static TMP_FontAsset uiFont;
    private string builtSceneName;
    private RenderTexture guidePreviewTexture;
    private static readonly Color Ink = new Color(0.035f, 0.055f, 0.09f, 0.97f);
    private static readonly Color Paper = new Color(0.96f, 0.93f, 0.86f, 1f);
    private static readonly Color Muted = new Color(0.72f, 0.76f, 0.80f, 1f);
    private Canvas canvas;
    private ObjectRaycaster raycaster;
    private TMP_Text targetText;
    private TMP_Text hubPortalPrompt;
    private ChatUIManager chatManager;
    private CafeOrderExperience cafeOrders;
    private HomeScenarioPanel scenarioPanel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        SceneManager.sceneLoaded -= InstallForScene;
        listening = false;
        // Local speech and scene transitions continue when another desktop app is focused.
        Application.runInBackground = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if (listening) return;
        listening = true;
        SceneManager.sceneLoaded += InstallForScene;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForFirstScene()
    {
        InstallForScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void InstallForScene(Scene scene, LoadSceneMode mode)
    {
        GameObject host = GameObject.Find("WorldExperienceDirector");
        if (host == null) host = new GameObject("WorldExperienceDirector");
        WorldExperienceDirector director = host.GetComponent<WorldExperienceDirector>();
        if (director == null) director = host.AddComponent<WorldExperienceDirector>();
        director.BuildForScene(scene);
    }

    private void BuildForScene(Scene scene)
    {
        // The initial scene is delivered both by sceneLoaded and AfterSceneLoad.
        // Install only once so the runtime hub and world HUD do not get duplicated.
        if (builtSceneName == scene.name) return;
        builtSceneName = scene.name;

        if (IsHomeScene(scene.name))
        {
            BuildHomeHub();
            WorldPresentationQuality.Ensure(scene.name);
            BlenderWorldCollection.Install(scene.name);
            return;
        }

        bool museumMode = scene.name == "Classroom" && PlayerPrefs.GetString(ScenarioRuntimeManager.SelectedScenarioKey, "classroom") == "museum";
        if (museumMode)
        {
            MuseumGalleryExperience.Ensure();
            PositionMuseumExplorer();
            EnsureGuideCharacter("Museum");
            GameObject guide = GameObject.Find("Classroom_Navigation_Demo_Agent");
            UnityEngine.AI.NavMeshAgent guideAgent = guide != null ? guide.GetComponent<UnityEngine.AI.NavMeshAgent>() : null;
            if (guideAgent != null && NavMesh.SamplePosition(MuseumGalleryExperience.GuidePosition, out NavMeshHit guideHit, 3f, NavMesh.AllAreas))
            {
                guideAgent.enabled = false;
                guide.transform.position = guideHit.position;
                guideAgent.enabled = true;
                guideAgent.Warp(guideHit.position);
            }
            BuildWorldHud("Museum");
            EnrichSceneObjects("Museum");
            EnsureWorldMovement("Museum");
            ConsumePendingExhibit("museum");
            WorldPresentationQuality.Ensure("Museum");
            BlenderWorldCollection.Install("Museum");
            return;
        }

        if (scene.name == "HistoricalPlace" || scene.name == "Classroom" || scene.name == "Cafestore")
        {
            if (scene.name == "Classroom") ConfigureClassroomLighting();
            EnsureGuideCharacter(scene.name);
            BuildWorldHud(scene.name);
            EnrichSceneObjects(scene.name);
            WorldCharacterCrowds.Ensure(scene.name);
            if(scene.name=="Cafestore")CafeVisualFinishing.Install();
            if (scene.name == "HistoricalPlace") RomanCityVisualUpgrade.Install();
            if (scene.name == "Classroom") ClassroomLearningExhibit.Ensure();
            if (scene.name == "HistoricalPlace" || scene.name == "Classroom") EnsureWorldMovement(scene.name);
            if (scene.name == "Classroom") ConsumePendingExhibit("classroom");
            WorldPresentationQuality.Ensure(scene.name);
            if (scene.name != "HistoricalPlace") BlenderWorldCollection.Install(scene.name);
        }
    }

    private static void ConfigureClassroomLighting()
    {
        // The authored classroom has very low ambient illumination. Lift its
        // base exposure so learners can read the room and see each model.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.60f, 0.69f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.61f, 0.59f, 0.53f);
        RenderSettings.ambientGroundColor = new Color(0.31f, 0.30f, 0.28f);
        GameObject dustObject = GameObject.Find("Dust");
        ParticleSystem dust = dustObject != null ? dustObject.GetComponent<ParticleSystem>() : null;
        if (dust != null)
        {
            // The authored effect reads as large square snowflakes at eye level.
            var main = dust.main;
            main.startSize = .014f;
            main.startColor = new Color(.9f, .86f, .75f, .12f);
            main.maxParticles = 45;
            var emission = dust.emission;
            emission.rateOverTime = 3f;
        }
        if (GameObject.Find("Classroom soft daylight") != null) return;
        GameObject lightObject = new GameObject("Classroom soft daylight");
        Light daylight = lightObject.AddComponent<Light>();
        daylight.type = LightType.Directional;
        daylight.color = new Color(1f, 0.93f, 0.82f);
        daylight.intensity = 0.9f;
        daylight.shadows = LightShadows.Soft;
        daylight.shadowStrength = 0.35f;
        lightObject.transform.rotation = Quaternion.Euler(42f, -26f, 0f);
    }

    private static void PositionMuseumExplorer()
    {
        Vector3 entry = MuseumGalleryExperience.EntryPosition;
        Quaternion facingGallery = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        CafeWalkController movement = FindAnyObjectByType<CafeWalkController>();
        if (movement != null) movement.transform.SetPositionAndRotation(entry, facingGallery);
        Camera view = Camera.main;
        if (view != null) view.transform.SetPositionAndRotation(entry + Vector3.up * 1.62f, facingGallery);
    }

    private static bool IsHomeScene(string sceneName)
    {
        return sceneName == "HomeScene" || sceneName == "HomeScene 1";
    }

    private static void ConsumePendingExhibit(string scenario)
    {
        const string scenarioKey = "Lumen.PendingExhibitScenario";
        const string targetKey = "Lumen.PendingExhibitTarget";
        string pendingScenario = PlayerPrefs.GetString(scenarioKey, string.Empty);
        string target = PlayerPrefs.GetString(targetKey, string.Empty);
        if (pendingScenario != scenario || string.IsNullOrWhiteSpace(target)) return;

        bool displayed = scenario == "museum"
            ? MuseumGalleryExperience.ShowArtifact(target)
            : ClassroomLearningExhibit.ShowModel(target);
        if (!displayed) Debug.LogWarning("The requested exhibit was not found in the destination scene: " + target);
        else
        {
            AgentCompanionController guide = FindAnyObjectByType<AgentCompanionController>();
            if (guide != null) guide.TryNavigateToNamedTarget(target);
        }
        PlayerPrefs.DeleteKey(scenarioKey);
        PlayerPrefs.DeleteKey(targetKey);
        PlayerPrefs.Save();
    }

    private void BuildHomeHub()
    {
        Canvas[] oldCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
        foreach (Canvas oldCanvas in oldCanvases)
        {
            if (oldCanvas != null && oldCanvas.gameObject.name == "HomeCanvas") oldCanvas.gameObject.SetActive(false);
        }

        WalkableHomeHub.Ensure();
        Material homeGalaxy = Resources.Load<Material>("LumenGalaxySky");
        if (homeGalaxy != null) RenderSettings.skybox = homeGalaxy;
        Camera homeCamera = Camera.main;
        if (homeCamera != null)
        {
            homeCamera.clearFlags = homeGalaxy != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            homeCamera.backgroundColor = new Color(0.012f, 0.022f, 0.065f);
        }
        EnsureHomeAssistantServices();
        EnsureEventSystem();
        canvas = CreateCanvas("LumenWalkableHubHUD", 250);
        Transform root = canvas.transform;

        // Pair the walkable atrium with large, tactile world cards so first-time
        // visitors can either wander to a portal or jump straight into a world.
        PositionHomeGuide();

        Image topBar = CreateImage(root, "AtriumTopBar", new Color(0.035f, 0.07f, 0.13f, 0.91f), true);
        Rect(topBar.rectTransform, new Vector2(0.025f, 0.925f), new Vector2(0.975f, 0.985f), Vector2.zero, Vector2.zero);
        CreateText(topBar.transform, "Brand", "LUMEN   /   A LITTLE WORLD OF BIG IDEAS", 19, new Color(0.74f, 0.94f, 0.91f), FontStyles.Bold,
            new Vector2(0.025f, 0.10f), new Vector2(0.48f, 0.90f), TextAlignmentOptions.Left);
        CreateText(topBar.transform, "LocalBadge", "YOUR GUIDE IS HERE  ·  HOLD V TO TALK", 13, new Color(0.47f, 0.78f, 0.75f), FontStyles.Bold,
            new Vector2(0.52f, 0.10f), new Vector2(0.975f, 0.90f), TextAlignmentOptions.Right);

        Image guidePanel = CreateImage(root, "GuidePromptPanel", new Color(0.035f, 0.07f, 0.13f, 0.94f), true);
        Rect(guidePanel.rectTransform, new Vector2(0.025f, 0.035f), new Vector2(0.455f, 0.265f), Vector2.zero, Vector2.zero);
        CreateText(guidePanel.transform, "GuidePromptTitle", "LUMEN  /  YOUR GUIDE", 18, new Color(0.99f, 0.56f, 0.40f), FontStyles.Bold,
            new Vector2(0.045f, 0.77f), new Vector2(0.95f, 0.97f), TextAlignmentOptions.Left);
        CreateText(guidePanel.transform, "GuidePromptIdeas", "Ask anything. I’ll help you find a place to begin.", 14, new Color(0.58f, 0.70f, 0.75f), FontStyles.Normal,
            new Vector2(0.045f, 0.59f), new Vector2(0.95f, 0.79f), TextAlignmentOptions.Left);
        targetText = CreateText(guidePanel.transform, "GuideConversation", "Hi! I’m Lumen. What are you curious about?", 15, new Color(0.84f, 0.91f, 0.90f), FontStyles.Normal,
            new Vector2(0.045f, 0.30f), new Vector2(0.95f, 0.60f), TextAlignmentOptions.Left);

        TMP_InputField input = CreateInput(guidePanel.transform,
            new Vector2(0.045f, 0.07f), new Vector2(0.72f, 0.28f), "Ask your guide...");
        Button send = CreateButton(guidePanel.transform, "SendMessage", "LET'S GO", new Color(0.99f, 0.65f, 0.36f),
            new Vector2(0.74f, 0.07f), new Vector2(0.955f, 0.28f), true);
        hubPortalPrompt = CreateText(root, "PortalInteractionPrompt", string.Empty,
            12, new Color(0.62f, 0.89f, 0.84f), FontStyles.Bold,
            new Vector2(0.025f, 0.875f), new Vector2(0.975f, 0.915f), TextAlignmentOptions.Center);
        WalkableHomeHub.BindPrompt(hubPortalPrompt);
        CreateText(root, "VoiceControls", "WASD MOVE  ·  RIGHT-CLICK LOOK  ·  M TOGGLE MIC  ·  V PUSH TO TALK", 12, new Color(0.55f, 0.73f, 0.76f), FontStyles.Bold,
            new Vector2(0.66f, 0.035f), new Vector2(0.975f, 0.075f), TextAlignmentOptions.Right);

        Button revealWorlds = CreateButton(root, "RevealWorlds", "WORLDS  /  CHOOSE YOUR NEXT DISCOVERY", new Color(0.36f, 0.75f, 0.79f, 0.96f),
            new Vector2(0.025f, 0.805f), new Vector2(0.665f, 0.865f), true);
        scenarioPanel = canvas.gameObject.AddComponent<HomeScenarioPanel>();
        GameObject panelObject = new GameObject("ScenarioRevealPanel", typeof(RectTransform), typeof(CanvasGroup));
        panelObject.transform.SetParent(root, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        Rect(panelRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CanvasGroup panelGroup = panelObject.GetComponent<CanvasGroup>();
        panelGroup.ignoreParentGroups = true;
        Image panelBacking = CreateImage(panelObject.transform, "ScenarioPanelBacking", new Color(0.025f, 0.05f, 0.10f, 0.93f), true);
        Rect(panelBacking.rectTransform, new Vector2(0.025f, 0.295f), new Vector2(0.665f, 0.865f), Vector2.zero, Vector2.zero);
        CreateText(panelObject.transform, "AdventureHeadline", "PICK A WORLD  /  FOLLOW YOUR CURIOSITY", 18, new Color(0.82f, 0.95f, 0.92f), FontStyles.Bold,
            new Vector2(0.055f, 0.805f), new Vector2(0.635f, 0.846f), TextAlignmentOptions.Center);
        scenarioPanel.Initialize(panelRect, revealWorlds);
        revealWorlds.transform.SetAsLastSibling();
        BuildWorldCard(panelObject.transform, 0, "01", "Ancient Rome", "Walk the Forum, meet its people, and uncover the city's stories.", "HISTORY", new Color(0.97f, 0.42f, 0.30f), "ancient_rome");
        BuildWorldCard(panelObject.transform, 1, "02", "Classroom", "Bring science, nature, and big questions to life in 3D.", "DISCOVER", new Color(0.27f, 0.72f, 0.95f), "classroom");
        BuildWorldCard(panelObject.transform, 2, "03", "Café", "Meet a barista, practise a chat, and place a pretend order.", "SOCIAL", new Color(0.28f, 0.81f, 0.57f), "cafe");
        BuildWorldCard(panelObject.transform, 3, "04", "Museum", "Meet real objects from history, science, and human ingenuity.", "ARTIFACTS", new Color(0.69f, 0.56f, 0.96f), "museum");

        HomeUIManager home = FindAnyObjectByType<HomeUIManager>();
        if (home == null)
        {
            GameObject manager = new GameObject("HomeManager");
            home = manager.AddComponent<HomeUIManager>();
        }
        if (home != null)
        {
            home.demoSceneName = "Classroom";
            home.homeChatScenario = "general_assistant";
            home.userInputField = input;
            home.sendButton = send;
            home.chatOutputText = targetText;
            home.autoManageChatLayout = false;
            home.maxStoredMessages = 5;
            targetText.fontSize = 14f;
            targetText.lineSpacing = 2f;
            send.onClick.RemoveListener(home.SendLocalMessage);
            send.onClick.AddListener(home.SendLocalMessage);
            send.onClick.AddListener(scenarioPanel.Reveal);
            input.onSubmit.AddListener(_ => SubmitHomeMessage(home));
            input.onSelect.AddListener(_ => scenarioPanel.Reveal());
            input.onValueChanged.AddListener(value => {
                if (!string.IsNullOrWhiteSpace(value)) scenarioPanel.Reveal();
            });
        }
        // The authored home canvas can leave one of its hidden input fields
        // selected in the EventSystem. Clear that stale selection so keyboard
        // navigation is live immediately; clicking the new chat field reselects it.
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        // Enter the existing walkable portal garden directly; no opaque opening overlay.
        CharacterSelectionControl.Install(canvas,"HomeScene");
    }

    private static void SubmitHomeMessage(HomeUIManager home)
    {
        home.SendLocalMessage();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private static void EnsureHomeAssistantServices()
    {
        GameObject services = GameObject.Find("HomeBackendClient");
        if (services == null) services = new GameObject("HomeBackendClient");
        if (services.GetComponent<AppConfig>() == null) services.AddComponent<AppConfig>();
        if (services.GetComponent<AIClient>() == null) services.AddComponent<AIClient>();
        if (services.GetComponent<SpeechOutputController>() == null) services.AddComponent<SpeechOutputController>();
        if (services.GetComponent<AgentResponseHandler>() == null) services.AddComponent<AgentResponseHandler>();
        if (services.GetComponent<SpeechInputController>() == null) services.AddComponent<SpeechInputController>();
    }

    private void EnsureGuideCharacter(string sceneName)
    {
        string objectName = sceneName == "Cafestore" ? "Cafe_Follow_Agent"
            : sceneName == "HistoricalPlace" ? "HistoricalPlace_Navigation_Demo_Agent"
            : "Classroom_Navigation_Demo_Agent";
        GameObject guide = GameObject.Find(objectName);
        if (guide == null) return;

        foreach (string placeholderName in new[] { "FollowerVisual", "Visual" })
        {
            Transform placeholder = guide.transform.Find(placeholderName);
            if (placeholder != null) placeholder.gameObject.SetActive(false);
        }
        CafeFollowPlayer oldCafeFollower = guide.GetComponent<CafeFollowPlayer>();
        if (oldCafeFollower != null) oldCafeFollower.enabled = false;

        // The authored classroom and Rome demo agents also carry an automatic
        // waypoint navigator. Once the runtime companion controller is added,
        // leaving that component active creates two independent destination
        // writers: the guide can oscillate between the authored route and the
        // learner, then appear to leave the scene. Companion following is the
        // single source of truth; explicit exhibit navigation still uses the
        // companion's own target request.
        ClassroomWaypointNavigator waypointNavigator = guide.GetComponent<ClassroomWaypointNavigator>();
        if (waypointNavigator != null)
        {
            waypointNavigator.StopNavigation();
            waypointNavigator.enabled = false;
        }

        UnityEngine.AI.NavMeshAgent navigation = guide.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navigation != null && guide.GetComponent<AgentCompanionController>() == null)
            guide.AddComponent<AgentCompanionController>();

        if (guide.transform.Find("LumenGuideVisual") == null)
        {
            GameObject prefab = Resources.Load<GameObject>("ScenarioCharacters/Marcus");
            GameObject visual = prefab != null ? Instantiate(prefab, guide.transform) : CreateFallbackGuide(guide.transform);
            visual.name = "LumenGuideVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.tag = "Untagged";
                child.gameObject.layer = 0;
            }
            foreach (MonoBehaviour script in visual.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            foreach (CharacterController controller in visual.GetComponentsInChildren<CharacterController>(true)) controller.enabled = false;
            foreach (Rigidbody rb in visual.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
            // Disabled controller dependencies remain until the old visual is replaced.
            foreach (Collider col in visual.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true)) animator.enabled = true;
            foreach (Renderer placeholderRenderer in guide.GetComponents<Renderer>()) placeholderRenderer.enabled = false;
        }
        if (guide.GetComponent<AgentAnimator>() == null) guide.AddComponent<AgentAnimator>();

        // The guide's feet rest on the real physics floor (independent of the
        // NavMesh height), so the companion never sinks through the floor or
        // floats above it while it follows the player.
        CharacterGrounding guideGrounding = guide.GetComponent<CharacterGrounding>();
        if (guideGrounding == null) guideGrounding = guide.AddComponent<CharacterGrounding>();
        guideGrounding.Initialize(guide.transform.Find("LumenGuideVisual"));

        ObjectMetadata metadata = guide.GetComponent<ObjectMetadata>();
        if (metadata == null) metadata = guide.AddComponent<ObjectMetadata>();
        metadata.displayName = "Lumen, your guide";
        metadata.objectName = "Lumen, your guide";
        metadata.category = "AI companion";
        metadata.descriptionSummary = "Your moving 3D guide. Ask for directions, explanations, or a friendly conversation.";
        metadata.description = metadata.descriptionSummary;
        metadata.contextualTags = "guide, companion, assistant, teacher, friend";
        metadata.isInteractable = true;
        metadata.interactionHint = "Press E to talk to your guide";
        metadata.scenario = sceneName == "Cafestore" ? "cafe" : sceneName == "HistoricalPlace" ? "ancient_rome" : sceneName == "Museum" ? "museum" : "classroom";
        ScenarioCharacterIdentity.InstallOnGuide(guide, sceneName);
    }

    private static GameObject CreateFallbackGuide(Transform parent)
    {
        GameObject root = new GameObject("LumenGuideVisual");
        root.transform.SetParent(parent, false);
        Material shell = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = new Color(0.22f, 0.36f, 0.48f) };
        Material accent = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = new Color(0.37f, 0.91f, 0.85f) };
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Lumen shell"; body.transform.SetParent(root.transform, false); body.transform.localPosition = new Vector3(0f, 0.82f, 0f); body.transform.localScale = new Vector3(0.55f, 0.63f, 0.42f); body.GetComponent<Renderer>().sharedMaterial = shell; Object.Destroy(body.GetComponent<Collider>());
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Lumen head"; head.transform.SetParent(root.transform, false); head.transform.localPosition = new Vector3(0f, 1.62f, 0f); head.transform.localScale = new Vector3(0.60f, 0.46f, 0.48f); head.GetComponent<Renderer>().sharedMaterial = shell; Object.Destroy(head.GetComponent<Collider>());
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Lumen eye"; eye.transform.SetParent(root.transform, false); eye.transform.localPosition = new Vector3(0f, 1.64f, 0.235f); eye.transform.localScale = new Vector3(0.34f, 0.12f, 0.05f); eye.GetComponent<Renderer>().sharedMaterial = accent; Object.Destroy(eye.GetComponent<Collider>());
        return root;
    }

    private void BuildGuidePreview(Transform root)
    {
        GameObject guide = GameObject.Find("HomeAgent") ?? GameObject.Find("Assistant");
        if (guide == null) return;
        if (guide.GetComponent<AgentAnimator>() == null) guide.AddComponent<AgentAnimator>();

        const int avatarLayer = 29;
        Renderer[] allRenderers = guide.GetComponentsInChildren<Renderer>(true);
        var modelRenderers = new List<Renderer>();
        foreach (Renderer renderer in allRenderers)
            if (renderer != null && renderer.transform != guide.transform) modelRenderers.Add(renderer);
        Renderer[] renderers = modelRenderers.Count > 0 ? modelRenderers.ToArray() : allRenderers;
        if (renderers.Length == 0) return;
        if (modelRenderers.Count > 0)
        {
            foreach (Renderer rootRenderer in guide.GetComponents<Renderer>()) rootRenderer.enabled = false;
        }
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            renderer.gameObject.layer = avatarLayer;
            bounds.Encapsulate(renderer.bounds);
        }
        foreach (Transform child in guide.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = avatarLayer;

        GameObject keyLightObject = new GameObject("LumenGuidePreviewKeyLight");
        keyLightObject.layer = avatarLayer;
        keyLightObject.transform.position = bounds.center + new Vector3(1.5f, 2.0f, 2.0f);
        Light keyLight = keyLightObject.AddComponent<Light>();
        keyLight.type = LightType.Point;
        keyLight.color = new Color(0.70f, 0.88f, 1f);
        keyLight.intensity = 5f;
        keyLight.range = Mathf.Max(4f, bounds.size.magnitude * 1.5f);
        keyLight.cullingMask = 1 << avatarLayer;

        GameObject rimLightObject = new GameObject("LumenGuidePreviewRimLight");
        rimLightObject.layer = avatarLayer;
        rimLightObject.transform.position = bounds.center + new Vector3(-1.8f, 1.0f, -1.5f);
        Light rimLight = rimLightObject.AddComponent<Light>();
        rimLight.type = LightType.Point;
        rimLight.color = new Color(1f, 0.63f, 0.36f);
        rimLight.intensity = 4f;
        rimLight.range = Mathf.Max(4f, bounds.size.magnitude * 1.4f);
        rimLight.cullingMask = 1 << avatarLayer;

        GameObject cameraObject = new GameObject("LumenGuidePreviewCamera");
        Camera previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.cullingMask = 1 << avatarLayer;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.04f, 0.07f, 0.105f, 0f);
        previewCamera.fieldOfView = 32f;
        previewCamera.nearClipPlane = 0.05f;
        previewCamera.farClipPlane = Mathf.Max(30f, bounds.size.magnitude * 8f);
        previewCamera.allowHDR = false;
        float distance = Mathf.Max(3.5f, bounds.size.y * 2.2f);
        Vector3 viewDirection = new Vector3(0.35f, 0.18f, 1f).normalized;
        cameraObject.transform.position = bounds.center + viewDirection * distance;
        cameraObject.transform.LookAt(bounds.center + Vector3.up * (bounds.size.y * 0.05f));
        guidePreviewTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { name = "LumenGuidePortrait" };
        guidePreviewTexture.Create();
        previewCamera.targetTexture = guidePreviewTexture;

        Image frame = CreateImage(root, "GuidePortraitFrame", new Color(0.08f, 0.12f, 0.16f, 1f), true);
        Rect(frame.rectTransform, new Vector2(0.705f, 0.615f), new Vector2(0.955f, 0.89f), Vector2.zero, Vector2.zero);
        GameObject portraitObject = new GameObject("GuidePortrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        portraitObject.transform.SetParent(frame.transform, false);
        RawImage portrait = portraitObject.GetComponent<RawImage>();
        portrait.texture = guidePreviewTexture;
        portrait.color = Color.white;
        portrait.raycastTarget = false;
        Rect(portrait.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
        TMP_Text caption = CreateText(frame.transform, "GuidePortraitLabel", "VOICE GUIDE  ·  HOLD V TO TALK", 11, new Color(0.68f, 0.91f, 0.84f), FontStyles.Bold,
            new Vector2(0.08f, 0.015f), new Vector2(0.92f, 0.10f), TextAlignmentOptions.Center);
        SpeechOutputController speech = FindAnyObjectByType<SpeechOutputController>();
        if (speech != null) speech.BindCaption(caption);
    }

    private void OnDestroy()
    {
        if (guidePreviewTexture == null) return;
        guidePreviewTexture.Release();
        Destroy(guidePreviewTexture);
        guidePreviewTexture = null;
    }

    private void BuildWorldCard(Transform parent, int index, string number, string title, string description, string category, Color accent, string scenario)
    {
        const float leftMargin = 0.025f;
        const float cardWidth = 0.305f;
        const float gapX = 0.018f;
        const float cardHeight = 0.225f;
        int column = index % 2;
        int row = index / 2;
        float left = leftMargin + column * (cardWidth + gapX);
        float bottom = row == 0 ? 0.568f : 0.331f;
        Image card = CreateImage(parent, "WorldCard_" + scenario, new Color(0.055f, 0.085f, 0.12f, 0.94f), true);
        card.raycastTarget = true;
        Rect(card.rectTransform, new Vector2(left, bottom - 0.02f), new Vector2(left + cardWidth, bottom + cardHeight - 0.02f), Vector2.zero, Vector2.zero);
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.78f);
        outline.effectDistance = new Vector2(2f, -2f);
        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = card;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock { normalColor = Color.white, highlightedColor = new Color(1.10f, 1.10f, 1.10f), pressedColor = new Color(0.87f, 0.90f, 0.92f), selectedColor = Color.white, disabledColor = Color.gray, colorMultiplier = 1f, fadeDuration = 0.12f };
        string selected = scenario;
        button.onClick.AddListener(() => {
            HomeUIManager manager = FindAnyObjectByType<HomeUIManager>();
            if (manager != null) manager.SelectScenarioAndLoad(selected);
            else ScenarioTransitionService.LoadScenario(selected);
        });
        button.gameObject.AddComponent<HubCardHover>();
        if (scenarioPanel != null) scenarioPanel.RegisterCard(card.rectTransform);

        Image illustration = CreateImage(card.transform, "IllustrationWash", new Color(accent.r, accent.g, accent.b, 0.18f), true);
        illustration.raycastTarget = false;
        Rect(illustration.rectTransform, new Vector2(0.035f, 0.43f), new Vector2(0.965f, 0.96f), Vector2.zero, Vector2.zero);
        BuildIcon(card.transform, index, accent);
        CreateText(card.transform, "CardNumber", number + "   /   " + category, 11, accent, FontStyles.Bold,
            new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.43f), TextAlignmentOptions.Left);
        CreateText(card.transform, "CardTitle", title, 21, Paper, FontStyles.Bold,
            new Vector2(0.06f, 0.19f), new Vector2(0.94f, 0.35f), TextAlignmentOptions.Left);
        CreateText(card.transform, "CardDescription", description, 12, Muted, FontStyles.Normal,
            new Vector2(0.06f, 0.035f), new Vector2(0.94f, 0.205f), TextAlignmentOptions.Left);
    }

    private static void PositionHomeGuide()
    {
        Camera viewCamera = Camera.main;
        GameObject guide = GameObject.Find("HomeAgent");
        if (viewCamera == null || guide == null) return;
        if (guide.GetComponent<AgentAnimator>() == null) guide.AddComponent<AgentAnimator>();

        Vector3 viewportPosition = viewCamera.WorldToViewportPoint(guide.transform.position);
        if (viewportPosition.z <= 0f) return;
        guide.transform.position = viewCamera.ViewportToWorldPoint(new Vector3(0.84f, 0.40f, viewportPosition.z));
    }

    private void BuildIcon(Transform card, int index, Color accent)
    {
        Transform art = card.Find("IllustrationWash");
        if (index == 0)
        {
            // A minimal basilica silhouette: bright lintel, three stone columns, and a sun disk.
            AddCircle(art, "Sun", new Vector2(0.76f, 0.68f), new Vector2(66, 66), new Color(accent.r, accent.g, accent.b, 0.78f));
            AddShape(art, "Entablature", new Vector2(0.5f, 0.44f), new Vector2(360, 22), accent);
            AddShape(art, "Pediment", new Vector2(0.5f, 0.55f), new Vector2(170, 56), new Color(accent.r, accent.g, accent.b, 0.85f));
            for (int i = -2; i <= 2; i++) AddShape(art, "Column" + i, new Vector2(0.5f + i * 0.105f, 0.25f), new Vector2(22, 88), new Color(0.95f, 0.85f, 0.70f, 0.9f));
        }
        else if (index == 1)
        {
            // Atom-like orbit rings and planets; the card is illustrative, the exhibit is a 3D model.
            AddCircle(art, "Nucleus", new Vector2(0.5f, 0.48f), new Vector2(54, 54), new Color(1f, 0.86f, 0.68f));
            AddOrbit(art, "OrbitA", new Vector2(0.5f, 0.48f), new Vector2(330, 100), accent, -24f);
            AddOrbit(art, "OrbitB", new Vector2(0.5f, 0.48f), new Vector2(330, 100), new Color(0.53f, 0.87f, 0.83f), 34f);
            AddCircle(art, "ElectronA", new Vector2(0.78f, 0.63f), new Vector2(20, 20), Paper);
            AddCircle(art, "ElectronB", new Vector2(0.22f, 0.34f), new Vector2(20, 20), Paper);
        }
        else if (index == 2)
        {
            AddShape(art, "CupBody", new Vector2(0.5f, 0.44f), new Vector2(92, 76), new Color(0.97f, 0.91f, 0.80f));
            AddShape(art, "CupTop", new Vector2(0.5f, 0.58f), new Vector2(102, 14), accent);
            AddShape(art, "Coffee", new Vector2(0.5f, 0.65f), new Vector2(80, 10), new Color(0.29f, 0.17f, 0.13f));
            AddOrbit(art, "CupHandle", new Vector2(0.62f, 0.46f), new Vector2(47, 40), new Color(0.97f, 0.91f, 0.80f), 0f);
            for (int i = -1; i <= 1; i++) AddShape(art, "Steam" + i, new Vector2(0.5f + i * 0.075f, 0.78f), new Vector2(5, 36), new Color(0.97f, 0.91f, 0.80f, 0.72f));
        }
        else
        {
            AddShape(art, "MuseumPedestal", new Vector2(0.5f, 0.24f), new Vector2(180, 36), accent);
            AddShape(art, "MuseumPlinth", new Vector2(0.5f, 0.34f), new Vector2(118, 34), new Color(0.96f, 0.91f, 0.80f));
            AddShape(art, "MuseumArtifact", new Vector2(0.5f, 0.58f), new Vector2(82, 102), new Color(0.96f, 0.91f, 0.80f));
            AddCircle(art, "MuseumArtifactTop", new Vector2(0.5f, 0.76f), new Vector2(62, 62), new Color(0.96f, 0.91f, 0.80f));
        }
        CreateText(art, "ExploreArrow", "OPEN", 12, accent, FontStyles.Bold, new Vector2(0.78f, 0.02f), new Vector2(0.98f, 0.18f), TextAlignmentOptions.Right);
    }

    private void BuildWorldHud(string sceneName)
    {
        EnsureEventSystem();
        raycaster = FindAnyObjectByType<ObjectRaycaster>();
        chatManager = FindAnyObjectByType<ChatUIManager>();
        canvas = CreateCanvas("WorldExperienceHUD", 190);
        Transform root = canvas.transform;
        if (chatManager != null) chatManager.HideLegacyPresentation();
        CreateRuntimeChat(root);
        Image bar = CreateImage(root, "WorldNavigationBar", new Color(0.035f, 0.055f, 0.09f, 0.88f), true);
        Rect(bar.rectTransform, new Vector2(0.79f, 0.91f), new Vector2(0.975f, 0.985f), Vector2.zero, Vector2.zero);
        Button back = CreateButton(bar.transform, "ReturnToHub", "‹  WORLDS", new Color(0.97f, 0.72f, 0.43f),
            new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), true);
        back.onClick.AddListener(() => ScenarioTransitionService.LoadHome());
        WorldVisitControls.Install(canvas, sceneName);
        CharacterSelectionControl.Install(canvas,sceneName);

        Image interaction = CreateImage(root, "InteractionPrompt", new Color(0.035f, 0.055f, 0.09f, 0.88f), true);
        Rect(interaction.rectTransform, new Vector2(0.025f, 0.035f), new Vector2(0.37f, 0.15f), Vector2.zero, Vector2.zero);
        CreateText(interaction.transform, "InteractionEyebrow", "LOOK AT SOMETHING TO EXPLORE IT", 11, new Color(0.97f, 0.72f, 0.43f), FontStyles.Bold,
            new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.95f), TextAlignmentOptions.Left);
        targetText = CreateText(interaction.transform, "InteractionObjectName", "Move your view over an object or person.", 15, Paper, FontStyles.Normal,
            new Vector2(0.06f, 0.37f), new Vector2(0.94f, 0.69f), TextAlignmentOptions.Left);
        CreateText(interaction.transform, "InteractionHint", "Press E to ask your guide about it", 12, Muted, FontStyles.Normal,
            new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.38f), TextAlignmentOptions.Left);

        if (sceneName == "Cafestore")
        {
            cafeOrders = canvas.gameObject.AddComponent<CafeOrderExperience>();
            cafeOrders.Initialize(canvas, chatManager);
            Button orderButton = CreateButton(root, "OpenCafeMenu", "ORDER  /  VIEW MENU", new Color(0.92f, 0.52f, 0.45f),
                new Vector2(0.78f, 0.035f), new Vector2(0.975f, 0.095f), true);
            orderButton.onClick.AddListener(cafeOrders.Show);
            CreateText(root, "CafeControls", "WASD MOVE  |  RIGHT-CLICK LOOK  |  ESC FREE CURSOR", 11, Muted, FontStyles.Bold,
                new Vector2(0.40f, 0.035f), new Vector2(0.76f, 0.095f), TextAlignmentOptions.Right);
        }
        else
        {
            CreateText(root, "WorldControls", "WASD MOVE  |  RIGHT-CLICK LOOK  |  ESC FREE CURSOR", 11, Muted, FontStyles.Bold,
                new Vector2(0.40f, 0.035f), new Vector2(0.975f, 0.095f), TextAlignmentOptions.Right);
        }
    }

    private void CreateRuntimeChat(Transform root)
    {
        GameObject services = GameObject.Find("WorldBackendServices");
        if (services == null) services = new GameObject("WorldBackendServices");
        AppConfig config = services.GetComponent<AppConfig>();
        if (config == null) config = services.AddComponent<AppConfig>();
        config.defaultScenario = ScenarioRuntimeManager.GetScenarioForScene(SceneManager.GetActiveScene().name);
        AIClient client = services.GetComponent<AIClient>();
        if (client == null) client = services.AddComponent<AIClient>();
        SpeechOutputController speechOutput = services.GetComponent<SpeechOutputController>();
        if (speechOutput == null) speechOutput = services.AddComponent<SpeechOutputController>();
        raycaster = services.GetComponent<ObjectRaycaster>();
        if (raycaster == null) raycaster = services.AddComponent<ObjectRaycaster>();

        foreach (UnityEngine.AI.NavMeshAgent navAgent in FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsInactive.Exclude))
        {
            if (navAgent == null || navAgent.GetComponent<AgentCompanionController>() != null) continue;
            if (navAgent.gameObject.name.IndexOf("agent", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            navAgent.gameObject.AddComponent<AgentCompanionController>();
            break;
        }
        AgentResponseHandler responseHandler = services.GetComponent<AgentResponseHandler>();
        if (responseHandler == null) responseHandler = services.AddComponent<AgentResponseHandler>();

        Image panel = CreateImage(root, "LocalGuideChat", new Color(0.045f, 0.067f, 0.095f, 0.94f), true);
        Rect(panel.rectTransform, new Vector2(0.68f, 0.145f), new Vector2(0.975f, 0.89f), Vector2.zero, Vector2.zero);
        CreateText(panel.transform, "ChatEyebrow", "YOUR COMPANION", 12, new Color(0.56f, 0.87f, 0.77f), FontStyles.Bold,
            new Vector2(0.06f, 0.925f), new Vector2(0.94f, 0.985f), TextAlignmentOptions.Left);
        string scene = SceneManager.GetActiveScene().name;
        bool isMuseum = string.Equals(config.defaultScenario, "museum", System.StringComparison.OrdinalIgnoreCase);
        string welcome = isMuseum ? "Walk through the Museum of Human Ideas"
            : scene == "HistoricalPlace" ? "Explore Rome together"
            : scene == "Cafestore" ? "Talk, order, and practise" : "Make a model make sense";
        TMP_Text chatTitle = CreateText(panel.transform, "ChatTitle", welcome, 19, Paper, FontStyles.Bold,
            new Vector2(0.06f, 0.865f), new Vector2(0.94f, 0.945f), TextAlignmentOptions.Left);
        chatTitle.enableAutoSizing = true;
        chatTitle.fontSizeMin = 14f;
        chatTitle.fontSizeMax = 19f;
        TMP_Text status = CreateText(panel.transform, "ChatStatus", "LOCAL GUIDE READY", 12, new Color(0.56f, 0.87f, 0.77f), FontStyles.Bold,
            new Vector2(0.06f, 0.815f), new Vector2(0.94f, 0.86f), TextAlignmentOptions.Left);
        speechOutput.BindCaption(status);

        GameObject viewportObject = new GameObject("ChatViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewportObject.transform.SetParent(panel.transform, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        Rect(viewportRect, new Vector2(0.045f, 0.20f), new Vector2(0.955f, 0.80f), Vector2.zero, Vector2.zero);
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0.035f, 0.055f, 0.08f, 0.2f);
        viewportImage.raycastTarget = true;
        ScrollRect scroll = viewportObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.scrollSensitivity = 24f;
        GameObject content = new GameObject("ChatTranscript", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        content.transform.SetParent(viewportObject.transform, false);
        TMP_Text transcript = content.GetComponent<TMP_Text>();
        transcript.font = TMP_Settings.defaultFontAsset;
        transcript.fontSize = 15f;
        transcript.color = Paper;
        transcript.lineSpacing = 3f;
        transcript.textWrappingMode = TextWrappingModes.Normal;
        transcript.overflowMode = TextOverflowModes.Overflow;
        transcript.raycastTarget = false;
        RectTransform contentRect = transcript.rectTransform;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = new Vector2(18f, -12f);
        scroll.content = contentRect;

        TMP_InputField input = CreateInput(panel.transform, new Vector2(0.045f, 0.055f), new Vector2(0.72f, 0.17f), "Ask your guide...");
        input.textComponent.fontSize = 14f;
        Button send = CreateButton(panel.transform, "SendGuideQuestion", "SEND", new Color(0.97f, 0.72f, 0.43f),
            new Vector2(0.75f, 0.055f), new Vector2(0.955f, 0.17f), true);
        if (chatManager == null) chatManager = services.GetComponent<ChatUIManager>();
        if (chatManager == null) chatManager = services.AddComponent<ChatUIManager>();
        chatManager.ConfigureRuntimeChat(client, config, raycaster, input, send, transcript, status, scroll);
        panel.gameObject.SetActive(false); // Let the environment fill the arrival view.
        Button collapse = CreateButton(root, "ToggleConversation", "CONVERSATION  /  OPEN", new Color(0.44f, 0.83f, 0.77f),
            new Vector2(0.68f, 0.105f), new Vector2(0.975f, 0.14f), true);
        collapse.onClick.AddListener(() =>
        {
            panel.gameObject.SetActive(!panel.gameObject.activeSelf);
            collapse.GetComponentInChildren<TMP_Text>().text = panel.gameObject.activeSelf ? "CONVERSATION  /  HIDE" : "CONVERSATION  /  OPEN";
        });

        string[] quickPrompts = isMuseum
            ? new[] { "Show Rosetta Stone", "Show Antikythera", "Show Triceratops", "Show Apollo 11" }
            : scene == "Classroom"
            ? new[] { "Show atom", "Show Solar System", "Show Earth", "Show a fern" }
            : scene == "Cafestore"
                ? new[] { "Help me order", "Start a conversation" }
                : new[] { "Explain the Forum", "Roman architecture" };
        for (int i = 0; i < quickPrompts.Length; i++)
        {
            int index = i;
            float chipWidth = quickPrompts.Length > 2 ? 0.215f : 0.435f;
            float gap = quickPrompts.Length > 2 ? 0.018f : 0.03f;
            float totalWidth = quickPrompts.Length * chipWidth + (quickPrompts.Length - 1) * gap;
            float minX = (1f - totalWidth) * 0.5f + i * (chipWidth + gap);
            float maxX = minX + chipWidth;
            Button chip = CreateButton(panel.transform, "QuickPrompt" + i, quickPrompts[i], new Color(0.14f, 0.19f, 0.22f),
                new Vector2(minX, 0.738f), new Vector2(maxX, 0.795f), true);
            TMP_Text chipText = chip.GetComponentInChildren<TMP_Text>();
            if (chipText != null)
            {
                chipText.color = Paper;
                chipText.fontSize = quickPrompts.Length > 2 ? 10.5f : 12f;
                chipText.enableAutoSizing = true;
                chipText.fontSizeMin = 8.5f;
                chipText.fontSizeMax = chipText.fontSize;
            }
            chip.onClick.AddListener(() => chatManager.SubmitWorldPrompt(quickPrompts[index]));
        }
        Rect(viewportRect, new Vector2(0.045f, 0.20f), new Vector2(0.955f, 0.725f), Vector2.zero, Vector2.zero);
    }

    private void Update()
    {
        if (targetText == null || canvas == null || IsHomeScene(SceneManager.GetActiveScene().name)) return;
        if (cafeOrders != null && cafeOrders.IsOpen) return;
        ObjectMetadata target = raycaster != null ? raycaster.CurrentObject : null;
        if (target != null)
        {
            string hint = string.IsNullOrWhiteSpace(target.interactionHint) ? "Press E to ask your guide about it" : "Press E  ·  " + target.interactionHint;
            targetText.text = target.GetDisplayName() + "\n" + hint;
        }
        else
        {
            targetText.text = "Move your view over an object or person.\nPress E to ask your guide about it";
        }

        bool interact = false;
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null) interact = keyboard.eKey.wasPressedThisFrame;
#else
        interact = Input.GetKeyDown(KeyCode.E);
#endif
        bool typingIntoChat = EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject != null
            && EventSystem.current.currentSelectedGameObject.GetComponentInParent<TMP_InputField>()?.isFocused == true;
        if (interact && !typingIntoChat && target != null && chatManager != null)
        {
            string prompt = target.category == "Person" || target.category == "AI companion" ? "Say hello and start a friendly conversation with " + target.GetDisplayName() + "."
                : "Explain " + target.GetDisplayName() + " using its context. What should I notice?";
            GeminiLiveConversation.Instance?.SelectPartner(target.category == "Person" ? target : null);
            chatManager.SubmitWorldPrompt(prompt, target);
        }
    }

    private void EnrichSceneObjects(string sceneName)
    {
        string scenario = ScenarioRuntimeManager.GetScenarioForScene(sceneName);
        ObjectMetadata[] objects = FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Exclude);
        foreach (ObjectMetadata metadata in objects)
        {
            if (!metadata.isActiveAndEnabled || metadata.category == "Interpretive architectural exhibit") continue;
            if (string.IsNullOrWhiteSpace(metadata.scenario)) metadata.scenario = scenario;
            string name = metadata.gameObject.name.ToLowerInvariant();
            if (sceneName == "HistoricalPlace")
            {
                if (name.Contains("colosseum")) SetKnowledge(metadata, "Landmark", "The Colosseum is Rome’s large Flavian amphitheatre, inaugurated in AD 80. It hosted public spectacles and could be reconfigured with awnings for shade.", "rome, colosseum, architecture, spectacle", "Ask to compare the arena’s architecture with a modern stadium.");
                else if (name.Contains("forum")) SetKnowledge(metadata, "Civic space", "The Roman Forum was a changing civic landscape of temples, law courts, basilicas, monuments, shops, and public meeting places—not a single-purpose plaza.", "rome, forum, civic-life, politics, trade", "Ask what different people might have come here to do.");
                else if (name.Contains("temple")) SetKnowledge(metadata, "Religious architecture", "Roman temples combined ritual, civic identity, and architectural display. Their surviving remains do not tell the whole story: paint, offerings, and ceremonies also mattered.", "rome, temple, religion, architecture", "Ask how a temple differs from a basilica or forum.");
                else if (name.Contains("fountain")) SetKnowledge(metadata, "Public infrastructure", "Fountains made water visible in the public landscape. Rome’s aqueduct system brought water into the city, while fountains and basins helped distribute it and served civic and decorative purposes.", "rome, water, engineering, forum", "Ask how water moved through an ancient city.");
                else if (name.Contains("tomb") || name.Contains("pharaoh") || name.Contains("camel")) SetKnowledge(metadata, "Historical comparison exhibit", "This model represents ancient Egypt rather than Rome. It is included as a comparison exhibit; the Roman and Egyptian worlds interacted, but they are distinct cultures and periods.", "rome, egypt, comparison, ancient-world", "Ask for a comparison between Rome and Egypt.");
                else if (name.Contains("shop")) SetKnowledge(metadata, "Commerce", "Small shops and workshops formed part of Roman street life. Archaeology can reveal counters and rooms, while the people and goods involved must be reconstructed carefully.", "rome, commerce, daily-life, forum", "Ask what evidence archaeologists use to identify a shop.");
            }
            else if (sceneName == "Classroom")
            {
                if (name.Contains("board")) SetKnowledge(metadata, "Teaching surface", "A classroom board supports diagrams, worked examples, and shared explanations. Ask the guide to unpack one idea step by step.", "classroom, teaching, diagram", "Ask the guide to draw out an idea in words.");
                else if (name.Contains("teacher_desk")) SetKnowledge(metadata, "Classroom station", "This is the teacher’s station. Nearby learning exhibits connect visible models with short explanations and check-for-understanding questions.", "classroom, learning, teacher", "Ask what classroom learning models are available.");
            }
            else if (sceneName == "Cafestore")
            {
                if (name.StartsWith("cafe_guest") || name == "cafe_chef")
                {
                    string personName = metadata.GetDisplayName();
                    SetKnowledge(metadata, "Person", "A café guest in this practice scene. Start a relaxed conversation, ask a polite question, or practise a small-talk opening.", "cafe, person, conversation, social-practice", "Press E to say hello and start a conversation.");
                }
                else if (name.Contains("counter_top") || name.Contains("counter_body")) SetKnowledge(metadata, "Café counter", "The café counter is the place to browse the menu and place a practice order. This is a simulation; no real payment is taken.", "cafe, menu, order, barista", "Use the Order button to open the menu.");
            }
        }

        if (sceneName == "Cafestore")
        {
            GameObject barista = GameObject.Find("Cafe_Chef");
            if (barista != null) AddPersonMetadata(barista, "Barista", "The café barista can greet you and receive your simulated order. No real payment is taken.", "cafe, person, barista, order");
            foreach (Transform guest in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if (guest == null || !guest.name.StartsWith("CafeGuest_", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (guest.parent != null && guest.parent.name.StartsWith("CafeGuest_", System.StringComparison.OrdinalIgnoreCase)) continue;
                string guestLabel = guest.name.Replace("CafeGuest_", "Guest ").Replace('_', ' ');
                AddPersonMetadata(guest.gameObject, guestLabel, "A café guest in this conversation-practice scene. Try a relaxed greeting, ask about their day, or share a little about yourself.", "cafe, person, conversation, guest, social-practice");
            }
            GameObject counter = GameObject.Find("Counter_Top");
            if (counter != null) AddPersonMetadata(counter, "Order Counter", "Browse the practice menu and place an order for pickup or table service. This experience does not charge money.", "cafe, order, menu, counter");
        }
    }

    private static void EnsureWorldMovement(string sceneName)
    {
        if (FindAnyObjectByType<CafeWalkController>() != null) return;
        Camera viewCamera = Camera.main;
        if (viewCamera == null) return;

        Vector3 cameraPosition = viewCamera.transform.position;
        Vector3 rayOrigin = new Vector3(cameraPosition.x, Mathf.Max(cameraPosition.y + 10f, 20f), cameraPosition.z);
        Vector3 floorPosition = new Vector3(cameraPosition.x, 0f, cameraPosition.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit floorHit, 250f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            floorPosition = floorHit.point;

        float yaw = viewCamera.transform.eulerAngles.y;
        float pitch = viewCamera.transform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        if (sceneName == "Museum")
        {
            floorPosition = MuseumGalleryExperience.EntryPosition;
            yaw = 0f;
            pitch = -6f;
        }
        else if (sceneName == "Classroom")
        {
            Transform spawn = GameObject.Find("Waypoint_entrance_inside")?.transform;
            Transform destination = GameObject.Find("Waypoint_front_aisle")?.transform;
            if (spawn != null) floorPosition = spawn.position;
            if (spawn != null && destination != null)
            {
                Vector3 direction = destination.position - spawn.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.01f) yaw = Quaternion.LookRotation(direction).eulerAngles.y;
            }
            pitch = 0f;
        }
        else if (sceneName == "HistoricalPlace")
        {
            Transform entry=GameObject.Find("Waypoint_forum_entry")?.transform;
            floorPosition=entry!=null?entry.position:new Vector3(0f,.1f,-28f);
            yaw = 0f;
            pitch = 0f;
        }
        GameObject player = new GameObject("WorldExplorer");
        player.transform.SetPositionAndRotation(floorPosition + Vector3.up * 0.12f, Quaternion.Euler(0f, yaw, 0f));
        CharacterController character = player.AddComponent<CharacterController>();
        character.height = 1.8f;
        character.radius = 0.32f;
        character.center = new Vector3(0f, 0.9f, 0f);
        character.skinWidth = 0.04f;
        character.slopeLimit = 50f;
        character.stepOffset = 0.35f;
        character.minMoveDistance = 0f;

        viewCamera.transform.SetParent(player.transform, true);
        viewCamera.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        CafeWalkController movement = player.AddComponent<CafeWalkController>();
        movement.InitializeView(viewCamera.transform, pitch);
    }

    private static void AddPersonMetadata(GameObject target, string displayName, string description, string tags)
    {
        ObjectMetadata metadata = target.GetComponent<ObjectMetadata>();
        if (metadata == null) metadata = target.AddComponent<ObjectMetadata>();
        metadata.objectId = "cafe_" + target.name.ToLowerInvariant();
        metadata.displayName = displayName;
        metadata.objectName = displayName;
        metadata.category = target.name.Contains("Counter") ? "Café counter" : "Person";
        metadata.descriptionSummary = description;
        metadata.description = description;
        metadata.contextualTags = tags;
        metadata.isInteractable = true;
        metadata.interactionHint = "Press E to start an interaction";
        metadata.scenario = "cafe";
        if (metadata.category == "Person" && target.GetComponent<Collider>() == null)
        {
            CapsuleCollider personCollider = target.AddComponent<CapsuleCollider>();
            personCollider.center = new Vector3(0f, 0.9f, 0f);
            personCollider.height = 1.8f;
            personCollider.radius = 0.36f;
            personCollider.isTrigger = true;
        }
    }

    private static void SetKnowledge(ObjectMetadata metadata, string category, string description, string tags, string hint)
    {
        metadata.category = category;
        metadata.descriptionSummary = description;
        metadata.description = description;
        metadata.contextualTags = tags;
        metadata.isInteractable = true;
        metadata.interactionHint = hint;
    }

    private static Canvas CreateCanvas(string name, int order)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static Image CreateImage(Transform parent, string name, Color color, bool rounded = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (rounded)
        {
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
        }
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float size, Color color, FontStyles style,
        Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.font = uiFont != null ? uiFont : TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
        Rect(text.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color fill, Vector2 anchorMin, Vector2 anchorMax, bool compact)
    {
        Image image = CreateImage(parent, name, fill, true);
        image.raycastTarget = true;
        Rect(image.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(fill, Color.white, 0.13f);
        colors.pressedColor = Color.Lerp(fill, Color.black, 0.12f);
        button.colors = colors;
        CreateText(image.transform, "ButtonLabel", label, compact ? 12 : 16, Ink, FontStyles.Bold,
            new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), TextAlignmentOptions.Center);
        return button;
    }

    private static TMP_InputField CreateInput(Transform parent, Vector2 anchorMin, Vector2 anchorMax, string placeholderText)
    {
        Image background = CreateImage(parent, "GuideQuestionInput", new Color(0.08f, 0.14f, 0.22f, 1f), true);
        background.raycastTarget = true;
        Rect(background.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        GameObject textGo = new GameObject("InputText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(background.transform, false);
        TMP_Text text = textGo.GetComponent<TMP_Text>();
        text.font = uiFont != null ? uiFont : TMP_Settings.defaultFontAsset;
        text.fontSize = 16;
        text.color = new Color(0.86f, 0.94f, 0.93f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(20, 6, 16, 6);
        // The text graphic sits above the input-field background in the
        // GraphicRaycaster. Let pointer events reach TMP_InputField itself so
        // a first click reliably focuses the box instead of the 3D controller.
        text.raycastTarget = false;
        Rect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        placeholderGo.transform.SetParent(background.transform, false);
        TMP_Text placeholder = placeholderGo.GetComponent<TMP_Text>();
        placeholder.font = text.font;
        placeholder.fontSize = 15;
        placeholder.color = new Color(0.48f, 0.62f, 0.66f);
        placeholder.text = placeholderText;
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        placeholder.margin = text.margin;
        placeholder.raycastTarget = false;
        Rect(placeholder.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TMP_InputField input = background.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = background;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 240;
        return input;
    }

    private static void AddShape(Transform parent, string name, Vector2 normalized, Vector2 size, Color color)
    {
        Image image = CreateImage(parent, name, color, true);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = normalized;
        rect.sizeDelta = size;
    }

    private static void AddCircle(Transform parent, string name, Vector2 normalized, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = GetCircleSprite();
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = normalized;
        rect.sizeDelta = size;
    }

    private static void AddOrbit(Transform parent, string name, Vector2 normalized, Vector2 size, Color color, float angle)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = GetRingSprite();
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = normalized;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private static Sprite GetRoundedSprite()
    {
        if (roundedSprite != null) return roundedSprite;
        Texture2D texture = CreateShapeTexture(64, false);
        roundedSprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64, 0, SpriteMeshType.FullRect, new Vector4(18, 18, 18, 18));
        return roundedSprite;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        Texture2D texture = CreateShapeTexture(64, true);
        circleSprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
        return circleSprite;
    }

    private static Sprite GetRingSprite()
    {
        if (ringSprite != null) return ringSprite;
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            float radius = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f));
            float alpha = Mathf.Clamp01(Mathf.Min(radius - 25f, 31f - radius) + 0.5f);
            pixels[y * 64 + x] = new Color(1, 1, 1, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        ringSprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
        return ringSprite;
    }

    private static Texture2D CreateShapeTexture(int size, bool circle)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = circle ? "RuntimeCircleUI" : "RuntimeRoundedUI";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        const float radius = 13f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = x + 0.5f;
            float py = y + 0.5f;
            float distance;
            if (circle) distance = Vector2.Distance(new Vector2(px, py), new Vector2(size * 0.5f, size * 0.5f)) - size * 0.5f;
            else
            {
                float qx = Mathf.Abs(px - size * 0.5f) - (size * 0.5f - radius);
                float qy = Mathf.Abs(py - size * 0.5f) - (size * 0.5f - radius);
                distance = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            }
            float alpha = Mathf.Clamp01(0.5f - distance);
            pixels[y * size + x] = new Color(1, 1, 1, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static void Rect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
    }

    private static void Fill(RectTransform rect) => Rect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        GameObject events = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        events.AddComponent<StandaloneInputModule>();
#endif
    }
}


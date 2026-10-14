using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChatUIManager : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private AIClient aiClient;
    [SerializeField] private AppConfig appConfig;
    [SerializeField] private ObjectRaycaster objectRaycaster;
    [SerializeField] private AgentResponseHandler agentResponseHandler;

    [Header("Input")]
    [SerializeField] private TMP_InputField tmpInputField;
    [SerializeField] private InputField legacyInputField;
    [SerializeField] private Button sendButton;

    [Header("Output")]
    [SerializeField] private TMP_Text tmpChatOutput;
    [SerializeField] private Text legacyChatOutput;
    [SerializeField] private TMP_Text tmpStatusText;
    [SerializeField] private Text legacyStatusText;
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private int maxStoredMessages = 80;

    [Header("Visual Polish")]
    [SerializeField] private bool applyDemoVisualPolishOnStart = true;
    [SerializeField] private bool buildSupervisorShowcaseOnStart = true;
    [SerializeField] private float chatPanelWidth = 500f;
    [SerializeField] private float chatPanelHeight = 420f;
    [SerializeField] private float chatPadding = 24f;

    [Header("Navigation")]
    [SerializeField] private Button backButton;
    [SerializeField] private bool createBackButtonOnStart = true;
    [SerializeField] private string homeSceneName = "HomeScene";

    private bool isWaitingForResponse;
    private readonly List<string> chatLines = new List<string>();
    private ObjectMetadata explicitResponseSpeaker;
    private ObjectMetadata pendingResponseSpeaker;

    private void Awake()
    {
        if (aiClient == null)
        {
            aiClient = FindAnyObjectByType<AIClient>();
        }

        if (appConfig == null)
        {
            appConfig = FindAnyObjectByType<AppConfig>();
        }

        if (objectRaycaster == null)
        {
            objectRaycaster = FindAnyObjectByType<ObjectRaycaster>();
        }

        if (agentResponseHandler == null)
        {
            agentResponseHandler = FindAnyObjectByType<AgentResponseHandler>();
        }

        if (chatScrollRect == null && tmpChatOutput != null)
        {
            chatScrollRect = tmpChatOutput.GetComponentInParent<ScrollRect>();
        }

        ConfigureChatText();
    }

    private void Start()
    {
        if (applyDemoVisualPolishOnStart)
        {
            ApplyDemoVisualPolish();
        }

        if (buildSupervisorShowcaseOnStart)
        {
            BuildSupervisorShowcase();
        }

        SetupBackButton();
        InitializeChatOutput();
    }

    private void OnEnable()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(SendCurrentMessage);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(LoadHomeScene);
        }
    }

    private void OnDisable()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(SendCurrentMessage);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(LoadHomeScene);
        }
    }

    public void LoadHomeScene()
    {
        if (string.IsNullOrWhiteSpace(homeSceneName))
        {
            SetStatus("Home scene name is missing.");
            return;
        }

        ScenarioTransitionService.LoadHome(homeSceneName);
    }

    /// <summary>Disables the old shared demo props and duplicate back button when authored worlds provide their own navigation.</summary>
    public void ConfigureUnifiedWorldExperience()
    {
        buildSupervisorShowcaseOnStart = false;
        createBackButtonOnStart = false;
    }

    /// <summary>Connects this scene's generated chat card to the local AI, gaze context, and UI controls.</summary>
    public void ConfigureRuntimeChat(AIClient client, AppConfig config, ObjectRaycaster gaze, TMP_InputField input,
        Button send, TMP_Text transcript, TMP_Text status, ScrollRect scroll)
    {
        aiClient = client;
        appConfig = config;
        objectRaycaster = gaze;
        tmpInputField = input;
        legacyInputField = null;
        sendButton = send;
        tmpChatOutput = transcript;
        legacyChatOutput = null;
        tmpStatusText = status;
        chatScrollRect = scroll;
        applyDemoVisualPolishOnStart = false;
        buildSupervisorShowcaseOnStart = false;
        createBackButtonOnStart = false;
        ConfigureChatText();
        SetupRuntimeChatScrollRect();
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(SendCurrentMessage);
            sendButton.onClick.AddListener(SendCurrentMessage);
        }
        if (tmpInputField != null) tmpInputField.onSubmit.AddListener(_ => SendCurrentMessage());
    }

    public void SendCurrentMessage()
    {
        if (isWaitingForResponse)
        {
            return;
        }

        if (aiClient == null)
        {
            SetStatus("AIClient is not assigned.");
            return;
        }

        string userMessage = GetInputText().Trim();
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            SetStatus("Type a message first.");
            return;
        }

        ObjectMetadata currentObject = objectRaycaster != null ? objectRaycaster.CurrentObject : null;
        pendingResponseSpeaker = explicitResponseSpeaker != null ? explicitResponseSpeaker : currentObject;
        explicitResponseSpeaker = null;
        string objectName = currentObject != null ? currentObject.GetDisplayName() : "";
        string objectDescription = currentObject != null ? currentObject.description : "";
        string scenario = GetScenario(currentObject);

        AppendChatLine($"You: {userMessage}");
        ClearInputText();
        SetLoadingState(true);

        StartCoroutine(aiClient.SendChatMessage(
            userMessage,
            scenario,
            objectName,
            objectDescription,
            HandleChatSuccess,
            HandleChatError));
    }

    /// <summary>Entry point for platform speech-to-text adapters.</summary>
    public void SubmitVoiceTranscription(string transcription)
    {
        if (string.IsNullOrWhiteSpace(transcription) || isWaitingForResponse)
        {
            return;
        }

        if (tmpInputField != null)
        {
            tmpInputField.text = transcription.Trim();
        }
        else if (legacyInputField != null)
        {
            legacyInputField.text = transcription.Trim();
        }

        SendCurrentMessage();
    }

    private string GetScenario(ObjectMetadata currentObject)
    {
        if (currentObject != null && !string.IsNullOrWhiteSpace(currentObject.scenario))
        {
            return currentObject.scenario;
        }

        if (appConfig != null && !string.IsNullOrWhiteSpace(appConfig.defaultScenario))
        {
            return appConfig.defaultScenario;
        }

        return ScenarioRuntimeManager.GetScenarioForScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Submits a contextual prompt from an in-world interaction control.</summary>
    public void SubmitWorldPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return;
        if (tmpInputField != null) tmpInputField.text = prompt;
        else if (legacyInputField != null) legacyInputField.text = prompt;
        SendCurrentMessage();
    }

    public void SubmitWorldPrompt(string prompt, ObjectMetadata responseSpeaker)
    {
        explicitResponseSpeaker = responseSpeaker;
        SubmitWorldPrompt(prompt);
    }

    private void HandleChatSuccess(ChatResponse response)
    {
        SetLoadingState(false);
        AppendChatLine($"Assistant: {response.response}");

        if (agentResponseHandler != null)
        {
            agentResponseHandler.HandleResponse(response, pendingResponseSpeaker);
        }
        else if (!string.IsNullOrWhiteSpace(response.next_scenario))
        {
            ScenarioTransitionService.LoadScenario(response.next_scenario);
        }
        pendingResponseSpeaker = null;
    }

    private void HandleChatError(string errorMessage)
    {
        pendingResponseSpeaker = null;
        explicitResponseSpeaker = null;
        SetLoadingState(false);
        SetStatus(errorMessage);
        AppendChatLine($"System: {errorMessage}");
    }

    private string GetInputText()
    {
        if (tmpInputField != null)
        {
            return tmpInputField.text;
        }

        if (legacyInputField != null)
        {
            return legacyInputField.text;
        }

        return "";
    }

    private void ClearInputText()
    {
        if (tmpInputField != null)
        {
            tmpInputField.text = "";
        }

        if (legacyInputField != null)
        {
            legacyInputField.text = "";
        }
    }

    private void AppendChatLine(string line)
    {
        chatLines.Add(line);

        while (chatLines.Count > maxStoredMessages)
        {
            chatLines.RemoveAt(0);
        }

        string chatText = string.Join("\n", chatLines) + "\n";

        if (tmpChatOutput != null)
        {
            tmpChatOutput.text = chatText;
            ResizeChatOutput();
        }

        if (legacyChatOutput != null)
        {
            legacyChatOutput.text = chatText;
        }

        if (chatScrollRect != null)
        {
            StartCoroutine(ScrollToBottomNextFrame());
        }
    }

    private void ConfigureChatText()
    {
        if (tmpChatOutput != null)
        {
            // Keep chat readable instead of shrinking the font after many messages.
            tmpChatOutput.enableAutoSizing = false;
            tmpChatOutput.textWrappingMode = TextWrappingModes.Normal;
            tmpChatOutput.overflowMode = TextOverflowModes.Overflow;
            tmpChatOutput.alignment = TextAlignmentOptions.TopLeft;
            tmpChatOutput.fontSize = 19f;
            tmpChatOutput.lineSpacing = 4f;
            tmpChatOutput.paragraphSpacing = 8f;
        }

        if (legacyChatOutput != null)
        {
            legacyChatOutput.resizeTextForBestFit = false;
            legacyChatOutput.horizontalOverflow = HorizontalWrapMode.Wrap;
            legacyChatOutput.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();
        ResizeChatOutput();

        if (chatScrollRect != null)
        {
            chatScrollRect.StopMovement();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void InitializeChatOutput()
    {
        chatLines.Clear();
        string scene = SceneManager.GetActiveScene().name;
        string introduction = scene == "HistoricalPlace" ? "Welcome to Ancient Rome. Look at a landmark and ask your guide what it reveals."
            : scene == "Cafestore" ? "Welcome to the café. Meet a guest, practise conversation, or place a practice order."
            : "Welcome to the learning studio. Ask about an exhibit, or ask to see a model.";
        chatLines.Add("Assistant: " + introduction);
        chatLines.Add("System: Local guide ready.");

        string chatText = string.Join("\n", chatLines) + "\n";

        if (tmpChatOutput != null)
        {
            tmpChatOutput.text = chatText;
            ResizeChatOutput();
        }
    }

    private void ApplyDemoVisualPolish()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            return;
        }

        ConfigureCanvas(canvas);
        SetupChatPanel(canvas);
        SetupInputRow(canvas);
        SetupStatusLabels(canvas);
        AddDemoHeader(canvas);
    }

    private void ConfigureCanvas(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            return;
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void SetupChatPanel(Canvas canvas)
    {
        if (tmpChatOutput == null)
        {
            return;
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        GameObject panel = GameObject.Find("DemoChatPanel");

        if (panel == null)
        {
            panel = new GameObject("DemoChatPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            panel.transform.SetParent(canvasRect, false);
        }

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        SetRect(panelRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-44f, 8f), new Vector2(chatPanelWidth, chatPanelHeight));

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.07f, 0.16f, 0.88f);
        AddShadow(panel, new Color(0f, 0f, 0f, 0.32f), new Vector2(0f, -8f));

        RectTransform textRect = tmpChatOutput.rectTransform;
        textRect.SetParent(panelRect, false);
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = new Vector2(chatPadding, -chatPadding);
        textRect.localScale = Vector3.one;

        tmpChatOutput.color = new Color(0.94f, 0.97f, 1f, 1f);
        tmpChatOutput.raycastTarget = false;

        chatScrollRect = panel.GetComponent<ScrollRect>();
        chatScrollRect.viewport = panelRect;
        chatScrollRect.content = textRect;
        chatScrollRect.horizontal = false;
        chatScrollRect.vertical = true;
        chatScrollRect.movementType = ScrollRect.MovementType.Clamped;
        chatScrollRect.inertia = false;
        chatScrollRect.scrollSensitivity = 24f;

        ResizeChatOutput();
    }

    private void SetupInputRow(Canvas canvas)
    {
        if (tmpInputField != null)
        {
            RectTransform inputRect = tmpInputField.GetComponent<RectTransform>();
            SetRect(inputRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-80f, 42f), new Vector2(780f, 56f));

            Image inputImage = tmpInputField.GetComponent<Image>();
            if (inputImage != null)
            {
                inputImage.color = new Color(1f, 1f, 1f, 0.96f);
            }

            if (tmpInputField.textComponent != null)
            {
                tmpInputField.textComponent.fontSize = 19f;
                tmpInputField.textComponent.color = new Color(0.10f, 0.12f, 0.16f, 1f);
                tmpInputField.textComponent.enableAutoSizing = false;
            }

            if (tmpInputField.placeholder is TMP_Text placeholder)
            {
                placeholder.text = "Ask about the object you are looking at...";
                placeholder.fontSize = 19f;
                placeholder.color = new Color(0.42f, 0.46f, 0.55f, 0.85f);
                placeholder.enableAutoSizing = false;
            }
        }

        StyleButton(sendButton, "Send", new Color(0.13f, 0.74f, 0.78f, 1f), new Color(0.20f, 0.86f, 0.90f, 1f), new Color(0.08f, 0.10f, 0.14f, 1f), 18f);
        SetButtonRect(sendButton, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(384f, 42f), new Vector2(124f, 56f));
    }

    private void SetupStatusLabels(Canvas canvas)
    {
        if (tmpStatusText != null)
        {
            tmpStatusText.fontSize = 18f;
            tmpStatusText.enableAutoSizing = false;
            tmpStatusText.color = new Color(0.78f, 0.88f, 1f, 1f);
            tmpStatusText.alignment = TextAlignmentOptions.Left;
            SetRect(tmpStatusText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(520f, 38f));
        }

        TMP_Text detectedLabel = objectRaycaster != null
            ? objectRaycaster.GetComponentInChildren<TMP_Text>()
            : null;

        if (detectedLabel != null)
        {
            detectedLabel.fontSize = 18f;
            detectedLabel.enableAutoSizing = false;
            detectedLabel.color = new Color(0.92f, 0.96f, 1f, 1f);
            detectedLabel.alignment = TextAlignmentOptions.Center;
            SetRect(detectedLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(520f, 38f));
        }
    }

    private void AddDemoHeader(Canvas canvas)
    {
        GameObject headerObject = GameObject.Find("DemoHeaderText");
        if (headerObject == null)
        {
            headerObject = new GameObject("DemoHeaderText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            headerObject.transform.SetParent(canvas.transform, false);
        }

        TMP_Text headerText = headerObject.GetComponent<TMP_Text>();
        string scene = SceneManager.GetActiveScene().name;
        headerText.text = scene == "HistoricalPlace" ? "ANCIENT ROME  ·  A CITY TO EXPLORE"
            : scene == "Cafestore" ? "THE CORNER TABLE  ·  A PLACE TO PRACTISE"
            : "CLASSROOM  ·  IDEAS YOU CAN EXPLORE";
        headerText.fontSize = 30f;
        headerText.fontStyle = FontStyles.Bold;
        headerText.enableAutoSizing = false;
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.color = new Color(0.92f, 0.96f, 1f, 1f);
        AddShadow(headerObject, new Color(0f, 0f, 0f, 0.40f), new Vector2(0f, -2f));

        SetRect(headerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(720f, 48f));
    }

    private void SetupBackButton()
    {
        if (backButton == null && createBackButtonOnStart)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
            }

            if (canvas != null)
            {
                GameObject buttonObject = GameObject.Find("DemoBackButton");
                if (buttonObject == null)
                {
                    buttonObject = new GameObject("DemoBackButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                    buttonObject.transform.SetParent(canvas.transform, false);

                    GameObject textObject = new GameObject("BackText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    textObject.transform.SetParent(buttonObject.transform, false);
                }

                backButton = buttonObject.GetComponent<Button>();
            }
        }

        if (backButton == null)
        {
            return;
        }

        StyleButton(backButton, "Back", new Color(0.96f, 0.98f, 1f, 0.96f), new Color(0.82f, 0.90f, 1f, 1f), new Color(0.08f, 0.10f, 0.16f, 1f), 18f);
        SetButtonRect(backButton, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(118f, 44f));

        backButton.onClick.RemoveListener(LoadHomeScene);
        backButton.onClick.AddListener(LoadHomeScene);
    }

    private void ResizeChatOutput()
    {
        if (tmpChatOutput == null)
        {
            return;
        }

        RectTransform textRect = tmpChatOutput.rectTransform;
        RectTransform viewportRect = chatScrollRect != null && chatScrollRect.viewport != null
            ? chatScrollRect.viewport
            : textRect.parent as RectTransform;

        float width = Mathf.Max(280f, (viewportRect != null ? viewportRect.rect.width : chatPanelWidth) - chatPadding * 2f);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        float preferredHeight = tmpChatOutput.GetPreferredValues(tmpChatOutput.text, width, float.PositiveInfinity).y + chatPadding * 2f;

        if (viewportRect != null)
        {
            preferredHeight = Mathf.Max(preferredHeight, viewportRect.rect.height);
        }

        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
    }

    private void SetupRuntimeChatScrollRect()
    {
        if (tmpChatOutput == null || chatScrollRect == null) return;
        RectTransform content = tmpChatOutput.rectTransform;
        RectTransform viewport = chatScrollRect.viewport;
        if (viewport == null) viewport = content.parent as RectTransform;
        chatScrollRect.viewport = viewport;
        chatScrollRect.content = content;
        chatScrollRect.horizontal = false;
        chatScrollRect.vertical = true;
        chatScrollRect.movementType = ScrollRect.MovementType.Clamped;
        chatScrollRect.scrollSensitivity = 24f;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 1f);
        content.anchoredPosition = new Vector2(chatPadding, -chatPadding);
        ResizeChatOutput();
    }

    private void BuildSupervisorShowcase()
    {
        if (GameObject.Find("Phase1SupervisorShowcase") != null)
        {
            return;
        }

        GameObject root = new GameObject("Phase1SupervisorShowcase");
        Material academicMat = CreateMaterial("Academic Cyan", new Color(0.10f, 0.78f, 0.84f, 1f));
        Material historyMat = CreateMaterial("History Violet", new Color(0.42f, 0.28f, 0.78f, 1f));
        Material socialMat = CreateMaterial("Social Pink", new Color(0.86f, 0.35f, 0.62f, 1f));
        Material darkMat = CreateMaterial("Demo Dark", new Color(0.05f, 0.07f, 0.12f, 1f));

        CreateScenarioPod(root.transform, "Academic Tutor Zone", new Vector3(-5.5f, 0.08f, 3.5f), academicMat, "Academic\nTutor");
        CreateScenarioPod(root.transform, "Historical Guide Zone", new Vector3(0f, 0.08f, 5.4f), historyMat, "Historical\nGuide");
        CreateScenarioPod(root.transform, "Social Coach Zone", new Vector3(5.5f, 0.08f, 3.5f), socialMat, "Social\nCoach");

        StyleMetadataObjects(academicMat, historyMat, socialMat);
        UpgradeAssistantVisual(academicMat, darkMat);
    }

    private void CreateScenarioPod(Transform parent, string name, Vector3 position, Material material, string label)
    {
        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObject.name = name;
        baseObject.transform.SetParent(parent, false);
        baseObject.transform.position = position;
        baseObject.transform.localScale = new Vector3(1.7f, 0.08f, 1.7f);
        AssignMaterial(baseObject, material);

        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.name = $"{name} Sign";
        sign.transform.SetParent(parent, false);
        sign.transform.position = position + new Vector3(0f, 1.05f, 0f);
        sign.transform.localScale = new Vector3(2.15f, 0.1f, 0.95f);
        AssignMaterial(sign, material);

        TextMeshPro text = CreateWorldText($"{name} Label", label, position + new Vector3(0f, 1.18f, -0.52f), 3.2f, Color.white);
        text.transform.SetParent(parent, false);
        text.alignment = TextAlignmentOptions.Center;
    }

    private void StyleMetadataObjects(Material academicMat, Material historyMat, Material socialMat)
    {
        ObjectMetadata[] metadataObjects = FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Exclude);
        foreach (ObjectMetadata metadata in metadataObjects)
        {
            Material material = academicMat;
            if (metadata.scenario == "historical_guide")
            {
                material = historyMat;
            }
            else if (metadata.scenario == "social_coach")
            {
                material = socialMat;
            }

            Renderer renderer = metadata.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material = material;
            }

            TextMeshPro label = CreateWorldText(
                $"{metadata.GetDisplayName()} Label",
                metadata.GetDisplayName(),
                metadata.transform.position + Vector3.up * 1.6f,
                2.4f,
                Color.white);
            label.alignment = TextAlignmentOptions.Center;
        }
    }

    private void UpgradeAssistantVisual(Material accentMat, Material darkMat)
    {
        GameObject assistant = GameObject.Find("Assistant");
        if (assistant == null)
        {
            return;
        }

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "AI Core Glow";
        core.transform.SetParent(assistant.transform, false);
        core.transform.localPosition = new Vector3(0f, 0.75f, 0.48f);
        core.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        AssignMaterial(core, accentMat);

        Light coreLight = core.AddComponent<Light>();
        coreLight.type = LightType.Point;
        coreLight.color = new Color(0.25f, 0.95f, 1f, 1f);
        coreLight.range = 3f;
        coreLight.intensity = 1.4f;

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "AI Identity Ring";
        ring.transform.SetParent(assistant.transform, false);
        ring.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ring.transform.localScale = new Vector3(0.72f, 0.025f, 0.72f);
        AssignMaterial(ring, darkMat);
    }

    private TextMeshPro CreateWorldText(string name, string text, Vector3 position, float fontSize, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
        textObject.transform.position = position;
        textObject.transform.rotation = Quaternion.Euler(65f, 0f, 0f);

        TextMeshPro tmp = textObject.GetComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.enableAutoSizing = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.rectTransform.sizeDelta = new Vector2(3.2f, 1.1f);
        return tmp;
    }

    private Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        return material;
    }

    private void AssignMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material;
        }
    }

    private void StyleButton(Button button, string label, Color normal, Color highlighted, Color textColor, float fontSize)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = normal;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.pressedColor = Color.Lerp(normal, Color.black, 0.12f);
        colors.selectedColor = highlighted;
        colors.disabledColor = new Color(0.35f, 0.38f, 0.45f, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>(true);
        if (buttonText != null)
        {
            buttonText.text = label;
            buttonText.fontSize = fontSize;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.enableAutoSizing = false;
            buttonText.color = textColor;

            RectTransform textRect = buttonText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localScale = Vector3.one;
        }
    }

    private void SetButtonRect(Button button, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        if (button == null)
        {
            return;
        }

        SetRect(button.GetComponent<RectTransform>(), anchor, pivot, anchoredPosition, size);
    }

    private void SetRect(RectTransform rectTransform, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
        rectTransform.localScale = Vector3.one;
    }

    private void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        if (target == null)
        {
            return;
        }

        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = target.AddComponent<Shadow>();
        }

        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private void SetLoadingState(bool isLoading)
    {
        isWaitingForResponse = isLoading;

        if (sendButton != null)
        {
            sendButton.interactable = !isLoading;
        }

        SetStatus(isLoading ? "Waiting for assistant..." : "");
    }

    private void SetStatus(string message)
    {
        if (tmpStatusText != null)
        {
            tmpStatusText.text = message;
        }

        if (legacyStatusText != null)
        {
            legacyStatusText.text = message;
        }
    }
}

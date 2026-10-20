using PlayerPrefs = LumenPreferences;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeUIManager : MonoBehaviour
{
    private const string SelectedScenarioKey = "SelectedScenario";
    private const string AcademicTutorScenario = "classroom";
    private const string HistoricalGuideScenario = "ancient_rome";
    private const string SocialCoachScenario = "cafe";

    [Header("Backend Chat")]
    public AIClient aiClient;
    public AppConfig appConfig;
    public bool useBackendChat = true;
    public string homeChatScenario = "general_assistant";
    public string homeChatObjectName = "";
    public string homeChatObjectDescription = "";

    [Header("Text")]
    public TMP_Text welcomeText;
    public TMP_Text chatOutputText;
    public TMP_Text selectedPersonalityText;
    public bool autoManageChatLayout = true;

    [Header("Chat Scrolling")]
    public ScrollRect chatScrollRect;
    public int maxStoredMessages = 80;
    public bool autoCreateScrollRectIfMissing = true;
    public bool autoScrollToLatestMessage = true;
    public float chatHorizontalPadding = 34f;
    public float chatVerticalPadding = 24f;
    public float minimumChatTextWidth = 300f;
    public float fallbackChatTextWidth = 382f;

    [Header("Input")]
    public TMP_InputField userInputField;

    [Header("Main Buttons")]
    public Button sendButton;
    public Button exploreButton;
    public Button signUpButton;
    public Button loginButton;
    public Button startButton;

    [Header("Explore Panel")]
    public GameObject explorePanel;
    public Button academicTutorButton;
    public Button historicalGuideButton;
    public Button socialCoachButton;

    [Header("Scene")]
    public string demoSceneName = "Phase1_DemoScene";

    private string pendingScenario;
    private readonly List<string> chatLines = new List<string>();
    private bool isWaitingForBackend;
    private int liveInputLine=-1,liveOutputLine=-1;
    public void ShowLiveInput(string text)
    {
        if(liveInputLine<0){AddUserMessage(text);liveInputLine=chatLines.Count-1;}
        else if(liveInputLine<chatLines.Count)chatLines[liveInputLine]="You: "+text;
    }
    public void ShowLiveOutput(string text)
    {
        var identity=FindAnyObjectByType<ScenarioCharacterIdentity>();string label=identity!=null?identity.DisplayName:"Kai";
        if(liveOutputLine<0){AddChatLine(label+": "+text);liveOutputLine=chatLines.Count-1;}
        else if(liveOutputLine<chatLines.Count)chatLines[liveOutputLine]=label+": "+text;
        if(chatOutputText!=null)chatOutputText.text=string.Join("\n",chatLines)+"\n";
    }
    public void ResetLiveTurn(){liveInputLine=liveOutputLine=-1;}
    public void FinishLiveReply(ChatResponse response){ShowLiveOutput(response.response);liveInputLine=liveOutputLine=-1;}

    private void Start()
    {
        EnsureBackendServices();
        SetExplorePanel(false);
        UpdateSelectedPersonalityLabel();
        if (autoManageChatLayout)
        {
            ConfigureChatText();
            SetupChatScrolling();
        }
        InitializeChatOutput();
        if (autoManageChatLayout) StartCoroutine(RefreshChatLayoutNextFrame());

        if (welcomeText != null)
        {
            welcomeText.text = "Hi, how can I help you today?";
        }

        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(SendLocalMessage);
            sendButton.onClick.AddListener(SendLocalMessage);
        }

        if (exploreButton != null)
        {
            exploreButton.onClick.AddListener(ToggleExplorePanel);
        }

        if (academicTutorButton != null)
        {
            academicTutorButton.onClick.AddListener(() => SelectScenarioAndLoad(AcademicTutorScenario));
        }

        if (historicalGuideButton != null)
        {
            historicalGuideButton.onClick.AddListener(() => SelectScenarioAndLoad(HistoricalGuideScenario));
        }

        if (socialCoachButton != null)
        {
            socialCoachButton.onClick.AddListener(() => SelectScenarioAndLoad(SocialCoachScenario));
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(StartDemoScene);
        }

        if (signUpButton != null)
        {
            signUpButton.onClick.AddListener(() => AddAssistantMessage("Sign up will be added in a future version."));
        }

        if (loginButton != null)
        {
            loginButton.onClick.AddListener(() => AddAssistantMessage("Login will be added in a future version."));
        }
    }

    private void OnDestroy()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(SendLocalMessage);
        }

        if (exploreButton != null)
        {
            exploreButton.onClick.RemoveListener(ToggleExplorePanel);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartDemoScene);
        }
    }

    public void ToggleExplorePanel()
    {
        if (explorePanel == null)
        {
            return;
        }

        explorePanel.SetActive(!explorePanel.activeSelf);

    }

    public void SetExplorePanel(bool isVisible)
    {
        if (explorePanel != null)
        {
            explorePanel.SetActive(isVisible);
        }
    }

    public void SendLocalMessage()
    {
        if (isWaitingForBackend)
        {
            return;
        }

        if (userInputField == null)
        {
            AddAssistantMessage("Input field is not assigned.");
            return;
        }

        string userMessage = userInputField.text.Trim();
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            AddAssistantMessage("Type a question or choose Explore to begin.");
            return;
        }

        AddUserMessage(userMessage);
        userInputField.text = "";

        if (useBackendChat && aiClient != null)
        {
            SendMessageToBackend(userMessage);
            return;
        }

        string lowerMessage = userMessage.ToLowerInvariant();

        if (IsConfirmation(lowerMessage) && !string.IsNullOrEmpty(pendingScenario))
        {
            SelectScenarioAndLoad(pendingScenario);
            return;
        }

        string requestedScenario = DetectScenarioFromMessage(lowerMessage);
        if (!string.IsNullOrEmpty(requestedScenario))
        {
            pendingScenario = requestedScenario;
            AddAssistantMessage(
                "Great question! This needs a specific scenario. Select a scenario from Explore to begin your embodied AI experience, or say OK and I will open the matching scenario.");
            return;
        }

        AddAssistantMessage(
            "Great question! Answer normally here for general questions. If your question needs a specific scenario, select one from Explore to begin your embodied AI experience.");
    }

    public void SubmitVoiceTranscription(string transcription)
    {
        if (string.IsNullOrWhiteSpace(transcription) || userInputField == null) return;
        aiClient?.CancelRequest();FindAnyObjectByType<SpeechOutputController>()?.CancelSpeech();
        isWaitingForBackend=false;
        if(VoiceCommandRouter.TryExecute(transcription,out string reply)){AddUserMessage(transcription);AddAssistantMessage(reply);FindAnyObjectByType<AgentResponseHandler>()?.SpeakIncremental(reply);return;}
        userInputField.text = transcription.Trim();
        SendLocalMessage();
    }

    private void SendMessageToBackend(string userMessage)
    {
        // The Home screen should behave like a normal AI assistant.
        // Scenario-specific personalities are only used after the user enters the 3D demo scene.
        string scenario = string.IsNullOrWhiteSpace(homeChatScenario)
            ? "general_assistant"
            : homeChatScenario;

        isWaitingForBackend = true;

        if (sendButton != null)
        {
            sendButton.interactable = false;
        }

        AddAssistantMessage("Lumen is thinking on-device…");

        StartCoroutine(aiClient.SendChatMessage(
            userMessage,
            scenario,
            homeChatObjectName,
            homeChatObjectDescription,
            HandleBackendSuccess,
            HandleBackendError,
            text=>FindAnyObjectByType<AgentResponseHandler>()?.SpeakIncremental(text)));
    }

    private void HandleBackendSuccess(ChatResponse response)
    {
        RemoveLastThinkingMessage();
        isWaitingForBackend = false;

        if (sendButton != null)
        {
            sendButton.interactable = true;
        }

        AddAssistantMessage(response.response);
        AgentResponseHandler responseHandler = FindAnyObjectByType<AgentResponseHandler>();
        if (responseHandler != null)
        {
            responseHandler.HandleResponse(response);
            return;
        }

        AgentAnimator animator = FindAnyObjectByType<AgentAnimator>();
        if (animator != null) animator.ApplyResponseTags(response.action, response.emotion);
        SpeechOutputController voice = FindAnyObjectByType<SpeechOutputController>();
        if (voice != null) voice.Speak(response.response);
        if (!string.IsNullOrWhiteSpace(response.next_scenario)) ScenarioTransitionService.LoadScenario(response.next_scenario);
    }

    private void HandleBackendError(string errorMessage)
    {
        RemoveLastThinkingMessage();
        isWaitingForBackend = false;

        if (sendButton != null)
        {
            sendButton.interactable = true;
        }

        AddAssistantMessage($"I could not reach the backend yet. {errorMessage}");
    }

    private void RemoveLastThinkingMessage()
    {
        if (chatLines.Count > 0 && chatLines[chatLines.Count - 1] == "Assistant: Lumen is thinking on-device…")
        {
            chatLines.RemoveAt(chatLines.Count - 1);
        }
    }

    public void SelectScenarioAndLoad(string scenario)
    {
        if (!IsValidScenario(scenario))
        {
            AddAssistantMessage("Invalid scenario selected. Please choose Academic Tutor, Historical Guide, or Social Coach.");
            return;
        }

        PlayerPrefs.SetString(SelectedScenarioKey, scenario);
        PlayerPrefs.Save();
        UpdateSelectedPersonalityLabel();
        LoadDemoScene();
    }

    public void StartDemoScene()
    {
        string selectedScenario = PlayerPrefs.GetString(SelectedScenarioKey, AcademicTutorScenario);

        if (!IsValidScenario(selectedScenario))
        {
            selectedScenario = AcademicTutorScenario;
            PlayerPrefs.SetString(SelectedScenarioKey, selectedScenario);
            PlayerPrefs.Save();
        }

        LoadDemoScene();
    }

    private void LoadDemoScene()
    {
        if (string.IsNullOrWhiteSpace(demoSceneName))
        {
            AddAssistantMessage("Demo scene name is missing.");
            return;
        }

        ScenarioTransitionService.LoadScenario(PlayerPrefs.GetString(SelectedScenarioKey, AcademicTutorScenario));
    }

    private void UpdateSelectedPersonalityLabel()
    {
        if (selectedPersonalityText == null)
        {
            return;
        }

        string selectedScenario = GetSelectedScenario();
        selectedPersonalityText.text = $"Selected: {GetScenarioDisplayName(selectedScenario)}";
    }

    private string GetSelectedScenario()
    {
        string selectedScenario = PlayerPrefs.GetString(SelectedScenarioKey, AcademicTutorScenario);
        return IsValidScenario(selectedScenario) ? selectedScenario : AcademicTutorScenario;
    }

    private void EnsureBackendServices()
    {
        if (appConfig == null)
        {
            appConfig = FindAnyObjectByType<AppConfig>();
        }

        if (aiClient == null)
        {
            aiClient = FindAnyObjectByType<AIClient>();
        }

        if ((appConfig == null || aiClient == null) && useBackendChat)
        {
            GameObject backendObject = GameObject.Find("HomeBackendClient");

            if (backendObject == null)
            {
                backendObject = new GameObject("HomeBackendClient");
            }

            if (appConfig == null)
            {
                appConfig = backendObject.GetComponent<AppConfig>();
                if (appConfig == null)
                {
                    appConfig = backendObject.AddComponent<AppConfig>();
                }
            }

            if (aiClient == null)
            {
                aiClient = backendObject.GetComponent<AIClient>();
                if (aiClient == null)
                {
                    aiClient = backendObject.AddComponent<AIClient>();
                }
            }
        }
    }

    private string DetectScenarioFromMessage(string lowerMessage)
    {
        if (lowerMessage.Contains("class") || lowerMessage.Contains("academic") || lowerMessage.Contains("tutor") || lowerMessage.Contains("study"))
        {
            return AcademicTutorScenario;
        }

        if (lowerMessage.Contains("rome") || lowerMessage.Contains("history") || lowerMessage.Contains("historical") || lowerMessage.Contains("guide"))
        {
            return HistoricalGuideScenario;
        }

        if (lowerMessage.Contains("cafe") || lowerMessage.Contains("café") || lowerMessage.Contains("coffee") || lowerMessage.Contains("social") || lowerMessage.Contains("coach") || lowerMessage.Contains("conversation"))
        {
            return SocialCoachScenario;
        }

        if (lowerMessage.Contains("museum") || lowerMessage.Contains("gallery") || lowerMessage.Contains("artifact"))
        {
            return "museum";
        }

        return "";
    }

    private bool IsConfirmation(string lowerMessage)
    {
        return lowerMessage == "ok"
            || lowerMessage == "okay"
            || lowerMessage == "yes"
            || lowerMessage == "start"
            || lowerMessage == "go";
    }

    private bool IsValidScenario(string scenario)
    {
        return scenario == AcademicTutorScenario || scenario == "academic_tutor"
            || scenario == HistoricalGuideScenario || scenario == "historical_guide"
            || scenario == SocialCoachScenario || scenario == "social_coach" || scenario == "museum";
    }

    private string GetScenarioDisplayName(string scenario)
    {
        switch (scenario)
        {
            case AcademicTutorScenario:
            case "academic_tutor":
                return "Academic Tutor";
            case HistoricalGuideScenario:
            case "historical_guide":
                return "Historical Guide";
            case SocialCoachScenario:
            case "social_coach":
                return "Social Coach";
            case "museum":
                return "Museum of Human Ideas";
            default:
                return "Academic Tutor";
        }
    }

    private void AddUserMessage(string message)
    {
        AddChatLine($"You: {message}");
    }

    private void AddAssistantMessage(string message)
    {
        AddChatLine($"Assistant: {message}");
    }

    private void AddChatLine(string line)
    {
        if (chatOutputText == null)
        {
            return;
        }

        chatLines.Add(line);

        while (chatLines.Count > maxStoredMessages)
        {
            chatLines.RemoveAt(0);
        }

        chatOutputText.text = string.Join("\n", chatLines) + "\n";
        if (autoManageChatLayout) ResizeChatTextToContent();

        if (chatScrollRect != null && autoScrollToLatestMessage)
        {
            StartCoroutine(ScrollToBottomNextFrame());
        }
    }

    private void InitializeChatOutput()
    {
        if (chatOutputText == null)
        {
            return;
        }

        chatLines.Clear();
        chatLines.Add("Guide: Welcome. I can take you to Ancient Rome, open the Classroom, visit the Museum, or bring you into the Café.");
        chatLines.Add("Guide: Ask me a question, or choose a world above to begin.");
        chatOutputText.text = string.Join("\n", chatLines) + "\n";
        if (autoManageChatLayout) ResizeChatTextToContent();
    }

    private void ConfigureChatText()
    {
        if (chatOutputText == null)
        {
            return;
        }

        // Keep the font size stable and let the Scroll View handle extra text.
        chatOutputText.enableAutoSizing = false;
        chatOutputText.fontSize = 22f;
        chatOutputText.lineSpacing = 5f;
        chatOutputText.paragraphSpacing = 6f;
        chatOutputText.textWrappingMode = TextWrappingModes.Normal;
        chatOutputText.overflowMode = TextOverflowModes.Overflow;
        chatOutputText.alignment = TextAlignmentOptions.TopLeft;
    }

    private void SetupChatScrolling()
    {
        if (chatOutputText == null)
        {
            return;
        }

        if (chatScrollRect == null)
        {
            chatScrollRect = chatOutputText.GetComponentInParent<ScrollRect>();
        }

        RectTransform chatTextRect = chatOutputText.rectTransform;
        RectTransform viewportRect = chatScrollRect != null
            ? chatScrollRect.viewport
            : chatTextRect.parent as RectTransform;

        if (viewportRect == null)
        {
            viewportRect = chatTextRect.parent as RectTransform;
        }

        if (chatScrollRect == null && autoCreateScrollRectIfMissing && viewportRect != null)
        {
            chatScrollRect = viewportRect.GetComponent<ScrollRect>();

            if (chatScrollRect == null)
            {
                chatScrollRect = viewportRect.gameObject.AddComponent<ScrollRect>();
            }

            if (viewportRect.GetComponent<RectMask2D>() == null)
            {
                viewportRect.gameObject.AddComponent<RectMask2D>();
            }

            chatScrollRect.viewport = viewportRect;
            chatScrollRect.content = chatTextRect;
        }

        if (chatScrollRect != null)
        {
            chatScrollRect.horizontal = false;
            chatScrollRect.vertical = true;
            chatScrollRect.movementType = ScrollRect.MovementType.Clamped;
            chatScrollRect.inertia = false;
            chatScrollRect.scrollSensitivity = 24f;

            if (chatScrollRect.viewport == null && viewportRect != null)
            {
                chatScrollRect.viewport = viewportRect;
            }

            if (chatScrollRect.content == null)
            {
                chatScrollRect.content = chatTextRect;
            }
        }

        // Anchor the text to the top-left and force an explicit width.
        // This prevents TMP from wrapping into a narrow vertical column.
        chatTextRect.anchorMin = new Vector2(0f, 1f);
        chatTextRect.anchorMax = new Vector2(0f, 1f);
        chatTextRect.pivot = new Vector2(0f, 1f);
        chatTextRect.anchoredPosition = new Vector2(chatHorizontalPadding, -chatVerticalPadding);

        ResizeChatTextToContent();
    }

    private void ResizeChatTextToContent()
    {
        if (chatOutputText == null)
        {
            return;
        }

        RectTransform chatTextRect = chatOutputText.rectTransform;
        RectTransform viewportRect = chatScrollRect != null
            ? chatScrollRect.viewport
            : chatTextRect.parent as RectTransform;

        if (viewportRect == null)
        {
            viewportRect = chatTextRect.parent as RectTransform;
        }

        float availableWidth = ApplyChatTextWidth(chatTextRect, viewportRect);
        float preferredHeight = chatOutputText.GetPreferredValues(
            chatOutputText.text,
            availableWidth,
            float.PositiveInfinity).y + chatVerticalPadding * 2f;

        if (viewportRect != null)
        {
            preferredHeight = Mathf.Max(preferredHeight, viewportRect.rect.height);
        }

        chatTextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
    }

    private float ApplyChatTextWidth(RectTransform chatTextRect, RectTransform viewportRect)
    {
        float width = fallbackChatTextWidth;

        if (viewportRect != null && viewportRect.rect.width > 1f)
        {
            width = viewportRect.rect.width - chatHorizontalPadding * 2f;
        }
        else if (chatTextRect.parent is RectTransform parentRect && parentRect.rect.width > 1f)
        {
            width = parentRect.rect.width - chatHorizontalPadding * 2f;
        }

        width = Mathf.Max(minimumChatTextWidth, width);
        chatTextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        return width;
    }

    private IEnumerator RefreshChatLayoutNextFrame()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();
        ResizeChatTextToContent();
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();
        ResizeChatTextToContent();

        if (chatScrollRect != null)
        {
            chatScrollRect.StopMovement();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }
}

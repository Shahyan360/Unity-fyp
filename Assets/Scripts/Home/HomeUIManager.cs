using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeUIManager : MonoBehaviour
{
    private const string SelectedScenarioKey = "SelectedScenario";
    private const string AcademicTutorScenario = "academic_tutor";
    private const string HistoricalGuideScenario = "historical_guide";
    private const string SocialCoachScenario = "social_coach";

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

    private void Start()
    {
        EnsureBackendServices();
        SetExplorePanel(false);
        UpdateSelectedPersonalityLabel();
        ConfigureChatText();

        SetupChatScrolling();
        InitializeChatOutput();
        StartCoroutine(RefreshChatLayoutNextFrame());

        if (welcomeText != null)
        {
            welcomeText.text = "Hi, how can I help you today?";
        }

        if (sendButton != null)
        {
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

        AddAssistantMessage("Thinking...");

        StartCoroutine(aiClient.SendChatMessage(
            userMessage,
            scenario,
            homeChatObjectName,
            homeChatObjectDescription,
            HandleBackendSuccess,
            HandleBackendError));
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
        if (chatLines.Count > 0 && chatLines[chatLines.Count - 1] == "Assistant: Thinking...")
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

        SceneManager.LoadScene(demoSceneName);
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
            appConfig = FindFirstObjectByType<AppConfig>();
        }

        if (aiClient == null)
        {
            aiClient = FindFirstObjectByType<AIClient>();
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
        if (lowerMessage.Contains("academic") || lowerMessage.Contains("tutor") || lowerMessage.Contains("study"))
        {
            return AcademicTutorScenario;
        }

        if (lowerMessage.Contains("history") || lowerMessage.Contains("historical") || lowerMessage.Contains("guide"))
        {
            return HistoricalGuideScenario;
        }

        if (lowerMessage.Contains("social") || lowerMessage.Contains("coach") || lowerMessage.Contains("conversation"))
        {
            return SocialCoachScenario;
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
        return scenario == AcademicTutorScenario
            || scenario == HistoricalGuideScenario
            || scenario == SocialCoachScenario;
    }

    private string GetScenarioDisplayName(string scenario)
    {
        switch (scenario)
        {
            case AcademicTutorScenario:
                return "Academic Tutor";
            case HistoricalGuideScenario:
                return "Historical Guide";
            case SocialCoachScenario:
                return "Social Coach";
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
        ResizeChatTextToContent();

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
        chatLines.Add("Assistant: Hello! I am your virtual assistant.");
        chatLines.Add("Assistant: Choose a scenario from Explore or ask me anything to begin.");
        chatOutputText.text = string.Join("\n", chatLines) + "\n";
        ResizeChatTextToContent();
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
        chatOutputText.enableWordWrapping = true;
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

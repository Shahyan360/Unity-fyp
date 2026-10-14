using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Single, validated route table and brief fade between the available experiences.</summary>
public sealed class ScenarioTransitionService : MonoBehaviour
{
    private static ScenarioTransitionService instance;
    private CanvasGroup curtain;
    private TMP_Text title;
    private bool transitioning;

    private static readonly Dictionary<string, string> SceneByScenario = new Dictionary<string, string>
    {
        { "ancient_rome", "HistoricalPlace" },
        { "historical_guide", "HistoricalPlace" },
        { "classroom", "Classroom" },
        { "academic_tutor", "Classroom" },
        { "cafe", "Cafestore" },
        { "social_coach", "Cafestore" },
        { "museum", "Classroom" }
    };

    public static void LoadScenario(string scenario)
    {
        string normalized = ScenarioRuntimeManager.NormalizeScenario(scenario ?? string.Empty);
        if (!SceneByScenario.TryGetValue(normalized, out string sceneName))
        {
            Debug.LogWarning("Scenario request was ignored because it did not match a configured experience: " + scenario);
            return;
        }

        string previous = PlayerPrefs.GetString(ScenarioRuntimeManager.SelectedScenarioKey, string.Empty);
        bool switchVariantInSameScene = previous != normalized && SceneManager.GetActiveScene().name == sceneName;
        PlayerPrefs.SetString(ScenarioRuntimeManager.SelectedScenarioKey, normalized);
        PlayerPrefs.Save();
        Service().Begin(sceneName, DisplayName(normalized), switchVariantInSameScene);
    }

    public static void LoadHome(string sceneName = "HomeScene")
    {
        Service().Begin(string.IsNullOrWhiteSpace(sceneName) ? "HomeScene" : sceneName, "Your worlds are ready");
    }

    private static ScenarioTransitionService Service()
    {
        if (instance != null) return instance;
        GameObject service = new GameObject("ScenarioTransitionService");
        instance = service.AddComponent<ScenarioTransitionService>();
        DontDestroyOnLoad(service);
        instance.CreateCurtain();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateCurtain();
    }

    private void Begin(string sceneName, string transitionTitle, bool reloadSameScene = false)
    {
        if (transitioning) return;
        if (SceneManager.GetActiveScene().name == sceneName && !reloadSameScene) return;
        if (SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + sceneName + ".unity") < 0)
        {
            Debug.LogError("Scene '" + sceneName + "' is not enabled in Build Settings.");
            return;
        }
        StartCoroutine(Transition(sceneName, transitionTitle));
    }

    private IEnumerator Transition(string sceneName, string transitionTitle)
    {
        transitioning = true;
        title.text = transitionTitle;
        curtain.gameObject.SetActive(true);
        curtain.alpha = 0f;
        yield return Fade(1f, 0.24f);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (load == null)
        {
            transitioning = false;
            yield return Fade(0f, 0.2f);
            yield break;
        }
        while (!load.isDone) yield return null;
        yield return new WaitForSecondsRealtime(0.08f);
        yield return Fade(0f, 0.32f);
        curtain.gameObject.SetActive(false);
        transitioning = false;
    }

    private IEnumerator Fade(float target, float duration)
    {
        float start = curtain.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            curtain.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }
        curtain.alpha = target;
    }

    private void CreateCurtain()
    {
        if (curtain != null) return;
        GameObject canvasObject = new GameObject("TransitionCurtain", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(Image));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform full = canvasObject.GetComponent<RectTransform>();
        full.anchorMin = Vector2.zero;
        full.anchorMax = Vector2.one;
        full.offsetMin = Vector2.zero;
        full.offsetMax = Vector2.zero;
        Image background = canvasObject.GetComponent<Image>();
        background.color = new Color(0.035f, 0.055f, 0.09f, 1f);
        curtain = canvasObject.GetComponent<CanvasGroup>();
        curtain.alpha = 0f;
        curtain.interactable = false;
        curtain.blocksRaycasts = true;

        GameObject labelObject = new GameObject("TransitionTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1000f, 100f);
        title = labelObject.GetComponent<TMP_Text>();
        title.font = TMP_Settings.defaultFontAsset;
        title.fontSize = 42f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.95f, 0.91f, 0.83f);
        title.text = "Entering your experience";
        canvasObject.SetActive(false);
    }

    private static string DisplayName(string scenario)
    {
        switch (scenario)
        {
            case "ancient_rome": return "Ancient Rome · guided exploration";
            case "classroom": return "Classroom · learning studio";
            case "cafe": return "Café · social world";
            case "museum": return "Museum of Human Ideas · gallery";
            default: return "Your experience";
        }
    }
}

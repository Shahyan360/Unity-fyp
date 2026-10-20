using PlayerPrefs = LumenPreferences;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Owns the selected scenario, scene ambience, and object-context consistency.</summary>
public class ScenarioRuntimeManager : MonoBehaviour
{
    public const string SelectedScenarioKey = "SelectedScenario";
    public event Action<string> ScenarioChanged;
    public string ActiveScenario { get; private set; } = "classroom";

    private void Awake()
    {
        string sceneScenario = GetScenarioForScene(SceneManager.GetActiveScene().name);
        ActiveScenario = sceneScenario == "general_assistant"
            ? PlayerPrefs.GetString(SelectedScenarioKey, "classroom")
            : sceneScenario;
        ActiveScenario = NormalizeScenario(ActiveScenario);
        PlayerPrefs.SetString(SelectedScenarioKey, ActiveScenario);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    public void SelectScenario(string scenario)
    {
        if (!IsKnownScenario(scenario)) return;
        ActiveScenario = NormalizeScenario(scenario);
        PlayerPrefs.SetString(SelectedScenarioKey, ActiveScenario);
        PlayerPrefs.Save();
        ScenarioChanged?.Invoke(ActiveScenario);
        ApplySceneTheme();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneScenario = GetScenarioForScene(scene.name);
        if (sceneScenario != "general_assistant")
        {
            ActiveScenario = sceneScenario;
            PlayerPrefs.SetString(SelectedScenarioKey, ActiveScenario);
            PlayerPrefs.Save();
        }
        ApplySceneTheme();
    }

    private void ApplySceneTheme()
    {
        Color ambient = ActiveScenario switch
        {
            "ancient_rome" => new Color(0.55f, 0.34f, 0.16f),
            "cafe" => new Color(0.45f, 0.22f, 0.45f),
            _ => new Color(0.14f, 0.38f, 0.46f)
        };
        RenderSettings.ambientLight = ambient;
        foreach (ObjectMetadata metadata in FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Exclude))
        {
            if (string.IsNullOrWhiteSpace(metadata.scenario)) metadata.scenario = ActiveScenario;
        }
    }

    public static string GetScenarioForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "HistoricalPlace": return "ancient_rome";
            case "Classroom": return PlayerPrefs.GetString(SelectedScenarioKey, "classroom") == "museum" ? "museum" : "classroom";
            case "Cafestore": return "cafe";
            default: return "general_assistant";
        }
    }

    public static string NormalizeScenario(string scenario)
    {
        switch (scenario)
        {
            case "academic_tutor": return "classroom";
            case "historical_guide": return "ancient_rome";
            case "social_coach": return "cafe";
            default: return scenario;
        }
    }

    private static bool IsKnownScenario(string scenario) => scenario == "classroom" || scenario == "ancient_rome" || scenario == "cafe" || scenario == "museum" || scenario == "academic_tutor" || scenario == "historical_guide" || scenario == "social_coach" || scenario == "general_assistant";
}

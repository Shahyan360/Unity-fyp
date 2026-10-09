using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Owns the selected scenario, scene ambience, and object-context consistency.</summary>
public class ScenarioRuntimeManager : MonoBehaviour
{
    public const string SelectedScenarioKey = "SelectedScenario";
    public event Action<string> ScenarioChanged;
    public string ActiveScenario { get; private set; } = "academic_tutor";

    private void Awake()
    {
        ActiveScenario = PlayerPrefs.GetString(SelectedScenarioKey, "academic_tutor");
        if (!IsKnownScenario(ActiveScenario)) ActiveScenario = "academic_tutor";
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    public void SelectScenario(string scenario)
    {
        if (!IsKnownScenario(scenario)) return;
        ActiveScenario = scenario;
        PlayerPrefs.SetString(SelectedScenarioKey, scenario);
        PlayerPrefs.Save();
        ScenarioChanged?.Invoke(scenario);
        ApplySceneTheme();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplySceneTheme();

    private void ApplySceneTheme()
    {
        Color ambient = ActiveScenario switch
        {
            "historical_guide" => new Color(0.55f, 0.34f, 0.16f),
            "social_coach" => new Color(0.45f, 0.22f, 0.45f),
            _ => new Color(0.14f, 0.38f, 0.46f)
        };
        RenderSettings.ambientLight = ambient;
        foreach (ObjectMetadata metadata in FindObjectsByType<ObjectMetadata>(FindObjectsSortMode.None))
        {
            if (string.IsNullOrWhiteSpace(metadata.scenario)) metadata.scenario = ActiveScenario;
        }
    }

    private static bool IsKnownScenario(string scenario) => scenario == "academic_tutor" || scenario == "historical_guide" || scenario == "social_coach" || scenario == "general_assistant";
}

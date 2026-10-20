using PlayerPrefs = LumenPreferences;
using UnityEngine;

public class AppConfig : MonoBehaviour
{
    private const string SelectedScenarioKey = "SelectedScenario";
    private const string AcademicTutorScenario = "classroom";
    private const string HistoricalGuideScenario = "ancient_rome";
    private const string SocialCoachScenario = "cafe";

    [Header("Backend")]
    public string backendUrl = "http://127.0.0.1:5000";

    [Header("Scenario")]
    public string defaultScenario = "classroom";

    private void Awake()
    {
        LoadSelectedScenario();
    }

    private void LoadSelectedScenario()
    {
        if (PlayerPrefs.HasKey(SelectedScenarioKey))
        {
            string savedScenario = PlayerPrefs.GetString(SelectedScenarioKey);

            if (IsValidScenario(savedScenario))
            {
                defaultScenario = ScenarioRuntimeManager.NormalizeScenario(savedScenario);
            }
            else
            {
                defaultScenario = AcademicTutorScenario;
                Debug.LogWarning($"Invalid saved scenario '{savedScenario}'. Falling back to {defaultScenario}.");
            }
        }
        else
        {
            defaultScenario = AcademicTutorScenario;
        }

        Debug.Log($"Selected scenario: {defaultScenario}");
    }

    private bool IsValidScenario(string scenario)
    {
        return scenario == AcademicTutorScenario
            || scenario == HistoricalGuideScenario
            || scenario == SocialCoachScenario
            || scenario == "academic_tutor"
            || scenario == "historical_guide"
            || scenario == "social_coach"
            || scenario == "museum";
    }
}

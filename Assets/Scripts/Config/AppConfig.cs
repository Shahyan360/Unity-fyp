using UnityEngine;

public class AppConfig : MonoBehaviour
{
    private const string SelectedScenarioKey = "SelectedScenario";
    private const string AcademicTutorScenario = "academic_tutor";
    private const string HistoricalGuideScenario = "historical_guide";
    private const string SocialCoachScenario = "social_coach";

    [Header("Backend")]
    public string backendUrl = "http://127.0.0.1:5000";

    [Header("Scenario")]
    public string defaultScenario = "academic_tutor";

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
                defaultScenario = savedScenario;
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
            || scenario == SocialCoachScenario;
    }
}

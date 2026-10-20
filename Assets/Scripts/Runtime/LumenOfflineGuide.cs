using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit local exhibit help; never impersonates a connected AI conversation.</summary>
public static class LumenOfflineGuide
{
    public static string Reply(string query)
    {
        string text=(query??"").ToLowerInvariant();
        if(Regex.IsMatch(text,@"\b(show|display|bring up)\b")&&!Regex.IsMatch(text,@"\b(don't|do not|never)\b"))
        {
            string scene=SceneManager.GetActiveScene().name;
            if(scene=="Classroom"&&ScenarioRuntimeManager.GetScenarioForScene(scene)!="museum")
            {
                bool shown=ClassroomLearningExhibit.ShowModel(query);
                return shown?"Local exhibit displayed: "+ClassroomLearningExhibit.ActiveModel+". "+ClassroomLearningExhibit.ActiveDescription:"That model is not in the local collection. Try a heart, DNA, Solar System, water cycle, or plant cell.";
            }
        }
        return "Offline exhibit help: "+WorldStateContext.Snapshot()+". You can walk, inspect labelled exhibits and use the portals. Connect the assistant in Visit settings for conversation. No conversational reply has been generated.";
    }
}

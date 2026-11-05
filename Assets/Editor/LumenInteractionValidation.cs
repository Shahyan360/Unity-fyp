using UnityEditor;
using UnityEditor.SceneManagement;
[InitializeOnLoad]
public static class LumenInteractionValidation
{
    static LumenInteractionValidation() { EditorApplication.playModeStateChanged+=Changed; }
    public static void Run()
    {
        SessionState.SetBool("Lumen.InteractionValidation",true);
        EditorSceneManager.OpenScene("Assets/Scenes/HomeScene.unity");
        EditorApplication.isPlaying=true;
    }
    public static void RunHybrid()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/HomeScene.unity");
        EditorApplication.isPlaying=true;
    }
    private static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Lumen.InteractionValidation",false))
        {
            SessionState.SetBool("Lumen.InteractionValidation",false);
            LumenInteractionVerification.Begin();
        }
    }
}

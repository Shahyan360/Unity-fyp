using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Opt-in validation entry point. Does not save or modify authored scene assets.</summary>
[InitializeOnLoad]
public static class LumenFrontPageValidation
{
    private const string Pending="Lumen.FrontPage.ValidationPending";
    static LumenFrontPageValidation(){EditorApplication.playModeStateChanged+=OnPlayMode;}
    [MenuItem("Lumen/Validation/Review Front Page and Three Destinations")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before starting this review.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save your scene edits before running the review.");
        EditorSceneManager.OpenScene("Assets/Scenes/HomeScene.unity");
        SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    private static void OnPlayMode(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false))return;
        SessionState.SetBool(Pending,false);
        new GameObject("Front page validation (opt-in)").AddComponent<LumenFrontPageProbe>().Begin(Path.GetFullPath("Logs/FrontPage/Journey"));
    }
}

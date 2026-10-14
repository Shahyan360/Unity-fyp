#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Fixes the Unity 6 toolbar layout loop ("Layout update failed to stabilize after 10 iterations")
/// and prevents it from halting Play mode.
/// </summary>
[InitializeOnLoad]
public static class ConsoleErrorPauseHelper
{
    static ConsoleErrorPauseHelper()
    {
        EditorApplication.delayCall += EnsureErrorPauseDisabled;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        EnsureErrorPauseDisabled();
    }

    [MenuItem("Tools/Embodied AI/Fix Toolbar Layout Loop & Unpause")]
    public static void FixToolbarAndUnpause()
    {
        EnsureErrorPauseDisabled();
        EditorApplication.isPaused = false;
        Debug.Log("<color=green>[Embodied AI]</color> Disabled Console Error Pause and unpaused Play mode.");
    }

    public static void EnsureErrorPauseDisabled()
    {
        try
        {
            Type consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
            if (consoleWindowType == null) return;

            MethodInfo setPauseMethod = consoleWindowType.GetMethod("SetConsoleErrorPause", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (setPauseMethod != null)
            {
                setPauseMethod.Invoke(null, new object[] { false });
            }

            FieldInfo errorPauseField = consoleWindowType.GetField("m_ErrorPause", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (errorPauseField != null)
            {
                UnityEngine.Object[] consoles = Resources.FindObjectsOfTypeAll(consoleWindowType);
                foreach (UnityEngine.Object console in consoles)
                {
                    if (console is EditorWindow window)
                    {
                        errorPauseField.SetValue(window, false);
                        window.Repaint();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[ConsoleErrorPauseHelper] {0}", ex.Message);
        }
    }
}
#endif

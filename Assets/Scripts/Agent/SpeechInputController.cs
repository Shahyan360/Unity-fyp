using UnityEngine;
using System;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Push-to-talk input for Windows builds. Hold V to dictate; recognised text is
/// sent through exactly the same chat pipeline as typed input.
/// </summary>
public class SpeechInputController : MonoBehaviour
{
    [SerializeField] private KeyCode pushToTalkKey = KeyCode.V;
    [SerializeField] private ChatUIManager chat;
    [SerializeField] private HomeUIManager homeChat;
    [SerializeField] private bool enabledForThisBuild = true;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private DictationRecognizer recognizer;
    private bool wasHeld;
#endif

    private void Awake()
    {
        chat ??= FindAnyObjectByType<ChatUIManager>();
        homeChat ??= FindAnyObjectByType<HomeUIManager>();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!enabledForThisBuild) return;
        recognizer = new DictationRecognizer();
        recognizer.DictationResult += OnDictationResult;
        recognizer.DictationError += (error, hresult) => Debug.LogWarning($"Voice input unavailable: {error} ({hresult})");
#endif
    }

    private void Update()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!enabledForThisBuild || recognizer == null) return;
        bool held = IsPushToTalkHeld();
        if (held && !wasHeld && recognizer.Status == SpeechSystemStatus.Stopped) recognizer.Start();
        if (!held && wasHeld && recognizer.Status == SpeechSystemStatus.Running) recognizer.Stop();
        wasHeld = held;
#endif
    }

    private bool IsPushToTalkHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && Enum.TryParse(pushToTalkKey.ToString(), true, out Key key) && keyboard[key].isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(pushToTalkKey);
#else
        return false;
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private void OnDictationResult(string text, ConfidenceLevel confidence)
    {
        if (confidence == ConfidenceLevel.Rejected) return;
        chat ??= FindAnyObjectByType<ChatUIManager>();
        homeChat ??= FindAnyObjectByType<HomeUIManager>();
        if (chat != null) chat.SubmitVoiceTranscription(text);
        else homeChat?.SubmitVoiceTranscription(text);
    }
#endif

    private void OnDestroy()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (recognizer == null) return;
        recognizer.DictationResult -= OnDictationResult;
        recognizer.Dispose();
#endif
    }
}

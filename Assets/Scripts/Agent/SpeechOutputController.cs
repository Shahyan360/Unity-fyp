using TMPro;
using UnityEngine;

/// <summary>
/// Always provides readable live captions. An optional AudioSource can be wired to a
/// platform TTS provider without changing the conversation pipeline.
/// </summary>
public class SpeechOutputController : MonoBehaviour
{
    [SerializeField] private TMP_Text liveCaption;
    [SerializeField] private float captionSeconds = 8f;
    private float clearAt;

    private void Awake()
    {
        if (liveCaption == null)
            liveCaption = GameObject.Find("StatusText")?.GetComponent<TMP_Text>();
    }

    public void Speak(string text)
    {
        if (liveCaption == null || string.IsNullOrWhiteSpace(text)) return;
        liveCaption.text = text;
        clearAt = Time.unscaledTime + Mathf.Max(captionSeconds, text.Length * 0.045f);
    }

    private void Update()
    {
        if (liveCaption != null && clearAt > 0f && Time.unscaledTime > clearAt)
        {
            liveCaption.text = string.Empty;
            clearAt = 0f;
        }
    }
}

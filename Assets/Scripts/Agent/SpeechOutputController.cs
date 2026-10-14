using System;
using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Offline Windows text-to-speech with visible live captions.</summary>
public sealed class SpeechOutputController : MonoBehaviour
{
    [SerializeField] private TMP_Text liveCaption;
    [SerializeField] private AudioSource speaker;
    [SerializeField] private float captionSeconds = 8f;
    [SerializeField, Range(0, 100)] private int speechVolume = 90;
    [SerializeField, Range(-5, 5)] private int speechRate = 0;
    private float clearAt;
    private string readyCaption = string.Empty;
    private Coroutine speechRoutine;
    private string activeWavePath;
    private System.Diagnostics.Process synthesisProcess;

    private void Awake()
    {
        if (liveCaption == null)
            liveCaption = GameObject.Find("StatusText")?.GetComponent<TMP_Text>();
        if (speaker == null)
            speaker = GetComponent<AudioSource>();
        if (speaker == null)
            speaker = gameObject.AddComponent<AudioSource>();
        speaker.playOnAwake = false;
        speaker.spatialBlend = 0f;
        speaker.volume = 0.9f;
    }

    public void BindCaption(TMP_Text caption)
    {
        liveCaption = caption;
        readyCaption = caption != null ? caption.text : string.Empty;
    }

    public void Speak(string text)
    {
        SpeakAs(text, string.Empty);
    }

    public void SpeakAs(string text, string voiceName)
    {
        SpeakAs(text, voiceName, "Lumen");
    }

    public void SpeakAs(string text, string voiceName, string speakerLabel)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (liveCaption != null)
        {
            string label = string.IsNullOrWhiteSpace(speakerLabel) ? "Lumen" : speakerLabel.Trim();
            liveCaption.text = label.ToUpperInvariant() + "  ·  " + text;
            clearAt = Time.unscaledTime + Mathf.Max(captionSeconds, text.Length * 0.045f);
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (speechRoutine != null) StopCoroutine(speechRoutine);
        StopCurrentSpeech();
        speechRoutine = StartCoroutine(SynthesizeAndPlay(text, voiceName));
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private IEnumerator SynthesizeAndPlay(string text, string voiceName)
    {
        activeWavePath = Path.Combine(Path.GetTempPath(), "LumenSpeech-" + Guid.NewGuid().ToString("N") + ".wav");
        string textBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        string pathBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeWavePath));
        string voiceBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(voiceName ?? string.Empty));
        string script = "$ErrorActionPreference='Stop'; Add-Type -AssemblyName System.Speech; " +
            "$p=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + pathBase64 + "')); " +
            "$t=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + textBase64 + "')); " +
            "$n=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + voiceBase64 + "')); " +
            "$v=New-Object System.Speech.Synthesis.SpeechSynthesizer; $v.Rate=" + speechRate + "; $v.Volume=" + speechVolume + "; " +
            "if($n){try{$v.SelectVoice($n)}catch{}}; " +
            "$v.SetOutputToWaveFile($p); $v.Speak($t); $v.Dispose();";
        string encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

        var start = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand " + encodedCommand,
            CreateNoWindow = true,
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            UseShellExecute = false,
            RedirectStandardError = true
        };
        string startError = string.Empty;
        try { synthesisProcess = System.Diagnostics.Process.Start(start); }
        catch (Exception exception) { startError = exception.Message; }
        if (synthesisProcess == null)
        {
            Debug.LogWarning("Offline voice could not start on this device. " + startError);
            CleanupSpeechFile();
            yield break;
        }
        while (!synthesisProcess.HasExited) yield return new WaitForSecondsRealtime(0.05f);
        string error = synthesisProcess.StandardError.ReadToEnd();
        if (synthesisProcess.ExitCode != 0)
        {
            Debug.LogWarning("Offline voice synthesis failed: " + error);
            CleanupSpeechFile();
            yield break;
        }

        string uri = new Uri(activeWavePath).AbsoluteUri;
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Offline voice audio could not be played: " + request.error);
                CleanupSpeechFile();
                yield break;
            }
            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip != null && speaker != null)
            {
                speaker.Stop();
                speaker.clip = clip;
                speaker.Play();
                yield return new WaitForSecondsRealtime(clip.length);
                speaker.Stop();
                Destroy(clip);
            }
        }
        CleanupSpeechFile();
    }

    private void StopCurrentSpeech()
    {
        if (speaker != null) speaker.Stop();
        if (synthesisProcess == null) return;
        try { if (!synthesisProcess.HasExited) synthesisProcess.Kill(); }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        synthesisProcess.Dispose();
        synthesisProcess = null;
        CleanupSpeechFile();
    }

    private void CleanupSpeechFile()
    {
        if (synthesisProcess != null)
        {
            synthesisProcess.Dispose();
            synthesisProcess = null;
        }
        if (!string.IsNullOrEmpty(activeWavePath) && File.Exists(activeWavePath))
        {
            try { File.Delete(activeWavePath); } catch (IOException) { }
        }
        activeWavePath = null;
    }
#endif

    private void Update()
    {
        if (liveCaption != null && clearAt > 0f && Time.unscaledTime > clearAt)
        {
            liveCaption.text = readyCaption;
            clearAt = 0f;
        }
    }

    private void OnDestroy()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        StopCurrentSpeech();
#endif
    }
}

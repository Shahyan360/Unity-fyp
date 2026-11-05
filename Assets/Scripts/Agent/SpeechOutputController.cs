using PlayerPrefs = LumenPreferences;
using System;
using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Speaks application replies through configured TTS, with local Windows fallback.</summary>
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
    private AgentAnimator activeCharacter;
    private AudioClip activeClip;
    private UnityWebRequest synthesisRequest;
    private struct QueuedSpeech {public string text,voice,label;public AgentAnimator character;public int rate;public float pitch;}
    private readonly System.Collections.Generic.Queue<QueuedSpeech> queue=new System.Collections.Generic.Queue<QueuedSpeech>();
    private Coroutine queueRoutine;
    private float lastAudioAt=-100;
    public string RecentSpokenText {get;private set;}="";
    public bool WasRecentlySpeaking => IsSpeaking||Time.unscaledTime-lastAudioAt<1.2f;
    private bool ducked;
    public void SetVoiceVolume(float value){speechVolume=Mathf.RoundToInt(Mathf.Clamp01(value)*100);if(speaker!=null)speaker.volume=value*(ducked?.18f:1);}
    public void SetListeningDucked(bool value){ducked=value;SetVoiceVolume(LumenPreferences.GetInt("Lumen.SpeechVolume",90)/100f);}
    public void CancelSpeech()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if(queueRoutine!=null)StopCoroutine(queueRoutine);queueRoutine=null;queue.Clear();
        if(speechRoutine!=null)StopCoroutine(speechRoutine);speechRoutine=null;StopCurrentSpeech();
        SetListeningDucked(false);
#endif
    }
    public void EnqueueAs(string text,string voice,string label,AgentAnimator character,int rate=0,float pitch=1)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if(string.IsNullOrWhiteSpace(text))return;
        RecentSpokenText=(RecentSpokenText+" "+text).Trim();if(RecentSpokenText.Length>1200)RecentSpokenText=RecentSpokenText.Substring(RecentSpokenText.Length-1200);
        queue.Enqueue(new QueuedSpeech{text=text,voice=voice,label=label,character=character,rate=rate,pitch=pitch});
        if(queueRoutine==null)queueRoutine=StartCoroutine(PlayQueue());
#endif
    }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private IEnumerator PlayQueue()
    {
        while(queue.Count>0)
        {
            var item=queue.Dequeue();activeCharacter=item.character;speaker.pitch=item.pitch;
            if(liveCaption!=null)liveCaption.text=item.label.ToUpperInvariant()+"  /  SPEAKING";
            clearAt=Time.unscaledTime+captionSeconds;
            yield return SynthesizeAndPlay(item.text,item.voice,item.rate,item.label);
        }
        queueRoutine=null;
    }
#endif
    public bool IsSpeaking => speaker != null && speaker.isPlaying;
    public string PlaybackStage { get; private set; } = "Idle";
    public string LastSpeechEngine { get; private set; } = "";
    public string ConfiguredSpeechEngine { get; private set; } = "";
    public bool IsSpeechPending => speechRoutine != null || queueRoutine != null;
    public void SetVoiceMuted(bool muted)
    {
        if (speaker != null) speaker.mute = muted;
    }

    private void Awake()
    {
        if (liveCaption == null)
            liveCaption = GameObject.Find("StatusText")?.GetComponent<TMP_Text>();
        if (speaker == null)
            speaker = GetComponent<AudioSource>();
        if (speaker == null)
            speaker = gameObject.AddComponent<AudioSource>();
        speaker.playOnAwake = false;
        LumenAudioMix.Route(speaker,"Speech");
        speaker.spatialBlend = 0f;
        SetVoiceVolume(LumenPreferences.GetInt("Lumen.SpeechVolume",90)/100f);
        speaker.mute = PlayerPrefs.GetInt("Lumen.MuteVoice", 0) != 0;
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
        SpeakAs(text, voiceName, speakerLabel, null, speechRate, 1f);
    }

    public void SpeakAs(string text, string voiceName, string speakerLabel, AgentAnimator character, int rate = 0, float pitch = 1f)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        CancelSpeech();RecentSpokenText=text;
        if (liveCaption != null)
        {
            string label = string.IsNullOrWhiteSpace(speakerLabel) ? "Lumen" : speakerLabel.Trim();
            // Full accessible captions are retained in the chat transcript.
            liveCaption.text = label.ToUpperInvariant() + "  /  SPEAKING";
            clearAt = Time.unscaledTime + Mathf.Max(captionSeconds, text.Length * 0.045f);
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (speechRoutine != null) StopCoroutine(speechRoutine);
        StopCurrentSpeech();
        activeCharacter = character;
        speaker.pitch = Mathf.Clamp(pitch, 0.9f, 1.1f);
        speechRoutine = StartCoroutine(SynthesizeAndPlay(text, voiceName, Mathf.Clamp(rate, -5, 5), speakerLabel));
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [Serializable] private sealed class NeuralVoiceConfig { public bool ready; public string engine; }
    [Serializable] private sealed class NeuralVoiceRequest { public string text, speaker, speaker_id, object_name, voice_name; }
    private bool neuralReady;
    private string speechEngine="piper";
    private IEnumerator Start()
    {
        var client=GetComponent<AIClient>();
        if(client==null)client=FindAnyObjectByType<AIClient>();
        while(enabled)
        {
            using(var request=UnityWebRequest.Get((client!=null?client.BackendUrl:"http://127.0.0.1:5000")+"/speech/config"))
            {
                request.timeout=2;
                yield return request.SendWebRequest();
                neuralReady=false;
                if(request.result==UnityWebRequest.Result.Success)
                {
                    NeuralVoiceConfig config=null;
                    try { config=JsonUtility.FromJson<NeuralVoiceConfig>(request.downloadHandler.text); } catch(Exception) {}
                    neuralReady=config!=null&&config.ready;
                    if(config!=null)speechEngine=ConfiguredSpeechEngine=config.engine??"piper";
                }
            }
            yield return new WaitForSecondsRealtime(10);
        }
    }
    private IEnumerator SynthesizeAndPlay(string text, string voiceName, int rate, string speakerLabel)
    {
        if(neuralReady)
        {
            var identity=activeCharacter!=null?activeCharacter.GetComponent<ScenarioCharacterIdentity>():null;
            string voice=identity!=null?identity.NeuralSpeakerLabel:(voiceName??"").Contains("Zira")?"p229":"p226";
            var client=FindAnyObjectByType<AIClient>();
            string url=(client!=null?client.BackendUrl:"http://127.0.0.1:5000")+"/speech/synthesize";
            using(var request=UnityWebRequestMultimedia.GetAudioClip(url,AudioType.WAV))
            {
                request.method="POST";
                request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new NeuralVoiceRequest{
                    text=text,speaker=voice,speaker_id=identity!=null?identity.ConversationId:"",
                    object_name=identity==null?speakerLabel:"",voice_name=identity!=null?identity.CloudVoiceName:""})));
                request.SetRequestHeader("Content-Type","application/json");
                request.timeout=speechEngine=="gemini-tts"?40:12;
                synthesisRequest=request;
                PlaybackStage=speechEngine=="gemini-tts"?"Gemini speech synthesis":"Local neural synthesis";
                yield return request.SendWebRequest();
                synthesisRequest=null;
                if(request.result==UnityWebRequest.Result.Success)
                {
                    activeClip=DownloadHandlerAudioClip.GetContent(request);
                    speaker.clip=activeClip;speaker.pitch=1f;speaker.Play();
                    if(activeCharacter!=null)activeCharacter.BeginSpeech(speaker);
                    LastSpeechEngine=request.GetResponseHeader("X-Lumen-Speech-Engine")??"piper";
                    PlaybackStage=LastSpeechEngine=="gemini-tts"?"Playing Gemini voice":"Playing local neural voice";
                    while(speaker.isPlaying)yield return null;
                    if(activeCharacter!=null)activeCharacter.EndSpeech();
                    speaker.clip=null;Destroy(activeClip);activeClip=null;activeCharacter=null;speechRoutine=null;
                    yield break;
                }
                if(speechEngine=="gemini-tts")Debug.LogWarning("Gemini speech is unavailable; using the local voice for this answer.");
            }
        }

        PlaybackStage = "Starting local synthesis";
        LastSpeechEngine = "windows";
        activeWavePath = Path.Combine(Path.GetTempPath(), "LumenSpeech-" + Guid.NewGuid().ToString("N") + ".wav");
        string textBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        string pathBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeWavePath));
        string voiceBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(voiceName ?? string.Empty));
        string script = "$ErrorActionPreference='Stop'; Add-Type -AssemblyName System.Speech; " +
            "$p=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + pathBase64 + "')); " +
            "$t=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + textBase64 + "')); " +
            "$n=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + voiceBase64 + "')); " +
            "$v=New-Object System.Speech.Synthesis.SpeechSynthesizer; $v.Rate=" + rate + "; $v.Volume=100; " +
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
            // A Windows GUI player has no console output handle to inherit.
            // Give native voice tools valid pipes and drain both streams.
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        string startError = string.Empty;
        System.Threading.Tasks.Task<string> stderr = null;
        System.Threading.Tasks.Task<string> stdout = null;
        try
        {
            synthesisProcess = System.Diagnostics.Process.Start(start);
            if (synthesisProcess != null)
            {
                stderr = synthesisProcess.StandardError.ReadToEndAsync();
                stdout = synthesisProcess.StandardOutput.ReadToEndAsync();

            }
        }
        catch (Exception exception) { startError = exception.Message; }
        if (synthesisProcess == null)
        {
            Debug.LogWarning("Offline voice could not start on this device. " + startError);
            CleanupSpeechFile();
            yield break;
        }
        float synthesisDeadline = Time.realtimeSinceStartup + 30f;
        PlaybackStage = "Waiting for local synthesis";
        while (!synthesisProcess.HasExited)
        {
            if (Time.realtimeSinceStartup >= synthesisDeadline)
            {
                Debug.LogWarning("Offline voice synthesis exceeded 30 seconds. The response remains available in captions.");
                StopCurrentSpeech();
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.05f);
        }
        PlaybackStage = "Draining local voice output";
        while ((stderr != null && !stderr.IsCompleted) || (stdout != null && !stdout.IsCompleted)) yield return null;
        string error = stderr != null && stderr.Status == System.Threading.Tasks.TaskStatus.RanToCompletion ? stderr.Result : string.Empty;
        if (synthesisProcess.ExitCode != 0)
        {
            Debug.LogWarning("Offline voice synthesis failed: " + error);
            CleanupSpeechFile();
            yield break;
        }

        string uri = new Uri(activeWavePath).AbsoluteUri;
        PlaybackStage = "Loading generated audio";
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
                activeClip = clip;
                speaker.Stop();
                speaker.clip = clip;
                speaker.Play();
                PlaybackStage = "Playing local audio";
                if (activeCharacter != null) activeCharacter.BeginSpeech(speaker);
                while (speaker != null && speaker.isPlaying) yield return null;
                if (activeCharacter != null) activeCharacter.EndSpeech();
                activeCharacter = null;
                speaker.Stop();
                speaker.clip = null;
                Destroy(clip);
                activeClip = null;
            }
        }
        CleanupSpeechFile();
        speechRoutine = null;
    }

    private void StopCurrentSpeech()
    {
        if(synthesisRequest!=null)
        {
            var pending=synthesisRequest;synthesisRequest=null;
            try{pending.Abort();}catch(ObjectDisposedException){}
        }
        if (speaker != null) speaker.Stop();
        if (activeCharacter != null) activeCharacter.EndSpeech();
        activeCharacter = null;
        if (activeClip != null)
        {
            if (speaker != null) speaker.clip = null;
            Destroy(activeClip);
            activeClip = null;
        }
        if (synthesisProcess != null)
        {
            try { if (!synthesisProcess.HasExited) synthesisProcess.Kill(); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
            synthesisProcess.Dispose();
            synthesisProcess = null;
        }
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
            try { File.Delete(activeWavePath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        activeWavePath = null;
    }
#endif

    private void Update()
    {
        if(IsSpeaking)lastAudioAt=Time.unscaledTime;
        if (liveCaption != null && clearAt > 0f && Time.unscaledTime > clearAt)
        {
            liveCaption.text = readyCaption;
            clearAt = 0f;
        }
    }

    private void OnDisable()
    {
        CancelSpeech();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (speechRoutine != null) StopCoroutine(speechRoutine);
        speechRoutine = null;
        StopCurrentSpeech();
#endif
    }
}

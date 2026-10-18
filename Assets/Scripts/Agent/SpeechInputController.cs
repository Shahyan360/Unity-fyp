using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Continuous local microphone capture, bounded VAD utterances and interruptible conversation.</summary>
public sealed class SpeechInputController:MonoBehaviour
{
    public static SpeechInputController Instance {get;private set;}
    [SerializeField] private KeyCode pushToTalkKey=KeyCode.V;
    [SerializeField] private ChatUIManager chat;
    [SerializeField] private HomeUIManager homeChat;
    [SerializeField] private bool enabledForThisBuild=true;
    [SerializeField] private float minimumLevel=.006f;
    private AudioClip microphone;private string device;
    private bool listening,voiced,held,recognizing;
    private int readAt,preAt,voicedFrames;
    private float noise=.0015f,silence,micStarted,nextCheck;
    private int lastPosition=-1;private float lastCaptureAt;
    private readonly float[] frame=new float[320],pre=new float[4800];
    private readonly List<float> utterance=new List<float>(200000);
    private struct Capture {public byte[] wav;public bool forced;}
    private readonly Queue<Capture> pending=new Queue<Capture>();
    private UnityWebRequest active;
    private TMP_Text statusText;private GameObject statusCanvas;
    private string state="Starting microphone",lastHeard="";
    public string State=>state;
    public string LastTranscript=>lastHeard;
    public bool IsListening=>listening&&microphone!=null;
    public float InputLevel {get;private set;}
    private void Awake()
    {
        if(Instance!=null&&Instance!=this){Destroy(this);return;}Instance=this;
        listening=PlayerPrefs.GetInt("Lumen.VoiceEnabled",1)!=0;
        if(GetComponent<GeminiLiveConversation>()==null)gameObject.AddComponent<GeminiLiveConversation>();
    }
    private IEnumerator Start()
    {
        if(Instance!=this||!enabledForThisBuild)yield break;
        yield return new WaitForSecondsRealtime(1);
        BuildControl();if(listening)StartMicrophone();else state="Microphone off";
    }
    public void ToggleListening()
    {
        listening=!listening;PlayerPrefs.SetInt("Lumen.VoiceEnabled",listening?1:0);PlayerPrefs.Save();
        if(listening)StartMicrophone();else{StopMicrophone();state="Microphone off";}
    }
    public void SelectMicrophone(string name)
    {
        if(Array.IndexOf(Microphone.devices,name)<0)return;
        StopMicrophone();PlayerPrefs.SetString("Lumen.Microphone",name);listening=true;StartMicrophone();
    }
    private void StartMicrophone()
    {
        if(microphone!=null)return;
        if(Microphone.devices.Length==0){state="No microphone found. Connect one, then switch the mic on.";return;}
        device=PlayerPrefs.GetString("Lumen.Microphone",Microphone.devices[0]);
        if(Array.IndexOf(Microphone.devices,device)<0)device=Microphone.devices[0];
        try{microphone=Microphone.Start(device,true,2,16000);readAt=0;lastPosition=-1;micStarted=lastCaptureAt=Time.unscaledTime;state="Listening";}
        catch(Exception e){state="Microphone unavailable: "+e.Message;}
    }
    private void StopMicrophone()
    {
        GeminiLiveConversation.Instance?.PauseMicrophone();
        if(microphone!=null){Microphone.End(device);Destroy(microphone);microphone=null;}
        voiced=false;utterance.Clear();pending.Clear();
        active?.Abort();FindAnyObjectByType<SpeechOutputController>()?.SetListeningDucked(false);
    }
    private void Update()
    {
        if(Instance!=this||!enabledForThisBuild)return;
        bool forced=false;
#if ENABLE_INPUT_SYSTEM
        var key=Keyboard.current;
        var selected=UnityEngine.EventSystems.EventSystem.current!=null?UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject:null;
        bool typing=selected!=null&&selected.GetComponent<TMP_InputField>()!=null;
        if(key!=null&&!typing)
        {
            if(key.mKey.wasPressedThisFrame)ToggleListening();
            forced=Enum.TryParse(pushToTalkKey.ToString(),true,out Key binding)&&key[binding].isPressed;
        }
#endif
        if(forced&&microphone==null)StartMicrophone();
        if(!listening&&!forced&&held)Flush(true);
        if(microphone!=null&&(listening||forced))
        {
            int position=Microphone.GetPosition(device);
            if(position!=lastPosition){lastPosition=position;lastCaptureAt=Time.unscaledTime;if(state.StartsWith("No microphone signal"))state="Listening";}
            if(Time.unscaledTime-lastCaptureAt>4)state="No microphone signal. Check Windows microphone access and selected input.";
            int available=(position-readAt+microphone.samples)%microphone.samples;
            int blocks=0;
            while(available>=frame.Length&&blocks++<80)
            {
                microphone.GetData(frame,readAt);readAt=(readAt+frame.Length)%microphone.samples;available-=frame.Length;
                Consume(frame,forced);
            }
        }
        if(held&&!forced&&voiced)Flush(true);held=forced;
        if(!recognizing&&pending.Count>0)StartCoroutine(Recognize(pending.Dequeue()));
        if(!listening&&!forced&&!recognizing&&pending.Count==0&&microphone!=null)StopMicrophone();
        if(statusText!=null&&Time.unscaledTime>=nextCheck)
        {
            nextCheck=Time.unscaledTime+.15f;
            statusText.text="M / MIC "+(listening?"ON":"OFF")+"  |  "+state+(string.IsNullOrEmpty(lastHeard)?"":"  |  Heard: "+lastHeard);
        }
    }
    private void Consume(float[] samples,bool forced)
    {
        float energy=0;foreach(float value in samples)energy+=value*value;
        InputLevel=Mathf.Sqrt(energy/samples.Length);
        var live=GeminiLiveConversation.Instance;
        if(live!=null&&live.Selected)
        {
            if(forced&&!held)live.StopPlayback();
            live.SendMicrophone(samples);state=live.State;lastHeard=live.LastHeard;
            return;
        }
        bool signal=forced||InputLevel>Mathf.Max(minimumLevel,noise*3.3f);
        if(!voiced&&!signal)noise=Mathf.Lerp(noise,InputLevel,.015f);
        if(signal&&!voiced)
        {
            voiced=true;silence=0;voicedFrames=0;utterance.Clear();
            for(int i=0;i<pre.Length;i++)utterance.Add(pre[(preAt+i)%pre.Length]);
            FindAnyObjectByType<SpeechOutputController>()?.SetListeningDucked(true);
        }
        foreach(float value in samples){pre[preAt]=value;preAt=(preAt+1)%pre.Length;}
        if(!voiced)return;
        utterance.AddRange(samples);if(signal){silence=0;voicedFrames++;}else silence+=.02f;
        state="Hearing you";
        if((!forced&&silence>.55f)||utterance.Count>=16000*12)Flush(forced);
    }
    private void Flush(bool forced)
    {
        if(!voiced)return;voiced=false;
        if(voicedFrames>=7&&pending.Count<4)pending.Enqueue(new Capture{wav=EncodeWave(utterance),forced=forced});
        utterance.Clear();state=pending.Count>0?"Understanding your words":"Listening";
        if(pending.Count==0&&!recognizing)FindAnyObjectByType<SpeechOutputController>()?.SetListeningDucked(false);
    }
    public static byte[] EncodeWave(IList<float> samples)
    {
        using(var memory=new MemoryStream())using(var writer=new BinaryWriter(memory))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples.Count*2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
            writer.Write(16000);writer.Write(32000);writer.Write((short)2);writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples.Count*2);
            for(int i=0;i<samples.Count;i++)writer.Write((short)(Mathf.Clamp(samples[i],-1,1)*32767));return memory.ToArray();
        }
    }
    [Serializable] private sealed class Recognition {public string text,error;public bool retry;public float recognition_ms;}
    private IEnumerator Recognize(Capture capture)
    {
        recognizing=true;var client=GetComponent<AIClient>();if(client==null)client=FindAnyObjectByType<AIClient>();
        using(var request=new UnityWebRequest((client!=null?client.BackendUrl:"http://127.0.0.1:5000")+"/transcribe","POST"))
        {
            active=request;request.uploadHandler=new UploadHandlerRaw(capture.wav);request.downloadHandler=new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type","audio/wav");request.timeout=20;
            yield return request.SendWebRequest();active=null;
            Recognition result=null;try{result=JsonUtility.FromJson<Recognition>(request.downloadHandler.text);}catch(Exception){}
            if(request.responseCode==429){pending.Enqueue(capture);yield return new WaitForSecondsRealtime(.3f);}
            else if(request.result==UnityWebRequest.Result.Success&&result!=null)
            {if(!string.IsNullOrWhiteSpace(result.text))AcceptTranscript(result.text,capture.forced);state="Listening";}
            else state=result!=null&&!string.IsNullOrWhiteSpace(result.error)?result.error:"Speech service unavailable. Start the backend and check microphone access.";
        }
        recognizing=false;
        if(!voiced&&pending.Count==0)FindAnyObjectByType<SpeechOutputController>()?.SetListeningDucked(false);
    }
    public static bool LooksLikeEcho(string heard,string spoken)
    {
        var words=Regex.Matches(heard.ToLowerInvariant(),@"[a-z']+");if(words.Count<3)return false;
        string normal=" "+Regex.Replace(spoken.ToLowerInvariant(),@"[^a-z']+"," ")+" ";int matched=0;
        foreach(Match word in words)if(normal.Contains(" "+word.Value+" "))matched++;
        return matched/(float)words.Count>.78f;
    }
    public bool AcceptTranscript(string text,bool forced=false)
    {
        text=text.Trim();if(text.Length<2)return false;
        var output=FindAnyObjectByType<SpeechOutputController>();
        if(!forced&&output!=null&&output.WasRecentlySpeaking)
        {
            if(LooksLikeEcho(text,output.RecentSpokenText))return false;
            bool addressed=Regex.IsMatch(text.ToLowerInvariant(),@"\b(?:kai|orion|theo|maya|lucius|aurelius|you|i|we|stop|wait|go|come|follow|why|how|what|where|when|yes|no|actually|but)\b");
            var topic=new HashSet<string>();foreach(Match m in Regex.Matches(output.RecentSpokenText.ToLowerInvariant(),@"[a-z]{4,}"))topic.Add(m.Value);
            foreach(Match m in Regex.Matches(text.ToLowerInvariant(),@"[a-z]{4,}"))if(topic.Contains(m.Value))addressed=true;
            if(!addressed)return false;
        }
        lastHeard=text.Length>90?text.Substring(0,90)+"...":text;
        chat=FindAnyObjectByType<ChatUIManager>();homeChat=FindAnyObjectByType<HomeUIManager>();
        if(chat!=null)chat.SubmitVoiceTranscription(text);else homeChat?.SubmitVoiceTranscription(text);
        return chat!=null||homeChat!=null;
    }
    private void BuildControl()
    {
        statusCanvas=new GameObject("Voice listening control",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=statusCanvas.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=120;
        var scaler=statusCanvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
        var button=new GameObject("Toggle microphone",typeof(RectTransform),typeof(Image),typeof(Button));button.transform.SetParent(statusCanvas.transform,false);
        var rect=button.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.025f,.007f);rect.anchorMax=new Vector2(.975f,.038f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        button.GetComponent<Image>().color=new Color(.035f,.10f,.12f,.96f);button.GetComponent<Button>().onClick.AddListener(ToggleListening);
        var label=new GameObject("Microphone status",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(button.transform,false);
        statusText=label.GetComponent<TextMeshProUGUI>();statusText.rectTransform.anchorMin=Vector2.zero;statusText.rectTransform.anchorMax=Vector2.one;
        statusText.rectTransform.offsetMin=new Vector2(12,0);statusText.rectTransform.offsetMax=new Vector2(-12,0);statusText.font=TMP_Settings.defaultFontAsset;
        statusText.fontSize=14;statusText.color=new Color(.72f,.92f,.86f);statusText.alignment=TextAlignmentOptions.MidlineLeft;statusText.raycastTarget=false;
    }
    private void OnDisable(){if(Instance==this)StopMicrophone();}
    private void OnDestroy(){if(Instance!=this)return;StopMicrophone();if(statusCanvas!=null)Destroy(statusCanvas);Instance=null;}
}

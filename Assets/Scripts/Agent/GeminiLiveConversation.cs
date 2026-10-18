using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Persistent native audio through the local backend; no API key in Unity.</summary>
public sealed class GeminiLiveConversation:MonoBehaviour
{
    public static GeminiLiveConversation Instance {get;private set;}
    [Serializable] private sealed class Health {public bool configured;public string model;}
    [Serializable] private sealed class Setup {public string scenario,speaker_id,object_name,object_description;}
    [Serializable] private sealed class Event {public string @event,text,id,name,command,scenario;public bool ok;}
    private sealed class Wire
    {
        public ClientWebSocket socket=new ClientWebSocket();public CancellationTokenSource cancel=new CancellationTokenSource();
        public ConcurrentQueue<Outgoing> outgoing=new ConcurrentQueue<Outgoing>();public int epoch;
    }
    private struct Outgoing {public byte[] data;public WebSocketMessageType type;}
    private struct Incoming {public int epoch;public byte[] audio;public string text;}
    private readonly ConcurrentQueue<Incoming> incoming=new ConcurrentQueue<Incoming>();
    private readonly object audioLock=new object();
    private readonly float[] ring=new float[24000*60];
    private int read,write,buffered,epoch;
    public ObjectMetadata Partner {get;private set;}
    private bool introduced;
    private string emotion="friendly";
    public void SelectPartner(ObjectMetadata person)
    {
        if(person!=null&&person.category!="Person")person=null;
        if(Partner==person)return;
        Partner=person;fingerprint="";Disconnect();failed=false;retryAt=0;
    }
    private AgentAnimator ActiveActor()
    {
        if(Partner!=null)return Partner.GetComponentInParent<AgentAnimator>()??Partner.GetComponentInChildren<AgentAnimator>();
        var identity=FindAnyObjectByType<ScenarioCharacterIdentity>();return identity!=null?identity.GetComponent<AgentAnimator>():null;
    }
    private Wire wire;private AudioSource output;private AudioClip clip;private AgentAnimator actor;
    private string fingerprint="",backend="http://127.0.0.1:5000",speaker="kai",scenario="general_assistant";
    private float nextIdentityCheck,retryAt,sentAt;private bool turnEnded,playing,connecting,failed;
    private Action<ChatResponse> completed;private Action<string> partial,error;
    public bool Selected {get;private set;}
    public bool Ready {get;private set;}
    public string State {get;private set;}="Connecting Gemini Live";
    public string LastHeard {get;private set;}="";
    public int MicrophoneFrames {get;private set;}
    public int AudioChunks {get;private set;}
    public int Interruptions {get;private set;}
    public int Commands {get;private set;}
    public float FirstAudioSeconds {get;private set;}=-1;
    public float BufferedSeconds {get{lock(audioLock)return buffered/24000f;}}
    private void Awake()
    {
        if(Instance!=null&&Instance!=this){Destroy(this);return;}Instance=this;
        output=gameObject.AddComponent<AudioSource>();output.playOnAwake=false;output.spatialBlend=0;output.volume=.9f;
        output.mute=PlayerPrefs.GetInt("Lumen.MuteVoice",0)!=0;
        clip=AudioClip.Create("Gemini live voice",24000,1,24000,true,ReadAudio);output.clip=clip;output.loop=true;
    }
    private IEnumerator Start()
    {
        while(enabled)
        {
            var client=GetComponent<AIClient>();if(client!=null)backend=client.BackendUrl;
            if(!Selected)
            {
                using(var request=UnityWebRequest.Get(backend+"/live/status"))
                {
                    request.timeout=4;yield return request.SendWebRequest();
                    if(request.result==UnityWebRequest.Result.Success)
                    {
                        Health h=null;try{h=JsonUtility.FromJson<Health>(request.downloadHandler.text);}catch(Exception){}
                        Selected=h!=null&&h.configured;
                    }
                }
            }
            yield return new WaitForSecondsRealtime(5);
        }
    }
    private void Update()
    {
        if(Selected&&Time.unscaledTime>=nextIdentityCheck)
        {
            nextIdentityCheck=Time.unscaledTime+.5f;
            var identity=FindAnyObjectByType<ScenarioCharacterIdentity>();
            string nextSpeaker=Partner!=null?Partner.objectId:identity!=null?identity.ConversationId:"kai";
            string nextScenario=ScenarioRuntimeManager.GetScenarioForScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            nextScenario=ScenarioRuntimeManager.NormalizeScenario(nextScenario);
            if(nextScenario=="home")nextScenario="general_assistant";
            string key=nextScenario+":"+nextSpeaker;
            if(key!=fingerprint){fingerprint=key;speaker=nextSpeaker;scenario=nextScenario;Disconnect();failed=false;retryAt=0;}
            if(!Ready&&!connecting&&!failed&&Time.unscaledTime>=retryAt)Connect();
        }
        while(incoming.TryDequeue(out Incoming item))
        {
            if(item.epoch!=epoch)continue;
            if(item.audio!=null)
            {
                lock(audioLock)for(int i=0;i+1<item.audio.Length;i+=2)
                {
                    if(buffered>=ring.Length){State="Voice playback buffer full";break;}
                    ring[write]=(short)(item.audio[i]|(item.audio[i+1]<<8))/32768f;write=(write+1)%ring.Length;buffered++;
                }
                AudioChunks++;turnEnded=false;
                if(FirstAudioSeconds<0&&sentAt>0)FirstAudioSeconds=Time.realtimeSinceStartup-sentAt;
            }
            else HandleEvent(item.text);
        }
        if(!playing&&BufferedSeconds>=.08f)
        {
            var identity=FindAnyObjectByType<ScenarioCharacterIdentity>();actor=ActiveActor();
            FindAnyObjectByType<SpeechOutputController>()?.CancelSpeech();
            output.Play();playing=true;if(actor!=null){actor.ApplyEmotion(emotion);actor.BeginSpeech(output);}
        }
        if(playing&&turnEnded&&BufferedSeconds<=.001f){output.Stop();playing=false;if(actor!=null)actor.EndSpeech();}
    }
    private void Connect()
    {
        Disconnect();connecting=true;State="Connecting Gemini Live";
        wire=new Wire{epoch=epoch};var current=wire;
        string setup=JsonUtility.ToJson(new Setup{scenario=scenario,speaker_id=speaker,object_name=Partner!=null?Partner.GetDisplayName():"",object_description=Partner!=null?Partner.description:""});
        string address=backend.Replace("https://","wss://").Replace("http://","ws://")+"/live";
        _=Task.Run(async()=>
        {
            try
            {
                await current.socket.ConnectAsync(new Uri(address),current.cancel.Token);
                var initial=Encoding.UTF8.GetBytes(setup);
                await current.socket.SendAsync(new ArraySegment<byte>(initial),WebSocketMessageType.Text,true,current.cancel.Token);
                var send=SendLoop(current);var receive=ReceiveLoop(current);
                await Task.WhenAny(send,receive);current.cancel.Cancel();await Task.WhenAll(send,receive);
            }
            catch(OperationCanceledException){}
            catch(Exception){incoming.Enqueue(new Incoming{epoch=current.epoch,text="{\"event\":\"disconnected\"}"});}
            finally{current.socket.Dispose();}
        });
    }
    private async Task SendLoop(Wire current)
    {
        while(!current.cancel.IsCancellationRequested&&current.socket.State==WebSocketState.Open)
        {
            if(current.outgoing.TryDequeue(out Outgoing item))
                await current.socket.SendAsync(new ArraySegment<byte>(item.data),item.type,true,current.cancel.Token);
            else await Task.Delay(5,current.cancel.Token);
        }
    }
    private async Task ReceiveLoop(Wire current)
    {
        var bytes=new byte[32768];
        using(var message=new MemoryStream())
        while(!current.cancel.IsCancellationRequested)
        {
            var result=await current.socket.ReceiveAsync(new ArraySegment<byte>(bytes),current.cancel.Token);
            if(result.MessageType==WebSocketMessageType.Close)break;
            message.Write(bytes,0,result.Count);if(message.Length>1048576)throw new IOException("Live message too large");
            if(!result.EndOfMessage)continue;
            byte[] data=message.ToArray();message.SetLength(0);
            incoming.Enqueue(result.MessageType==WebSocketMessageType.Binary?new Incoming{epoch=current.epoch,audio=data}:new Incoming{epoch=current.epoch,text=Encoding.UTF8.GetString(data)});
        }
        incoming.Enqueue(new Incoming{epoch=current.epoch,text="{\"event\":\"disconnected\"}"});
    }
    private void HandleEvent(string json)
    {
        Event e=null;try{e=JsonUtility.FromJson<Event>(json);}catch(Exception){}if(e==null)return;
        if(e.@event=="ready"){Ready=true;connecting=false;State="Gemini listening";
            if(!introduced&&scenario=="cafe"&&Partner==null){introduced=true;Send(new Event{@event="text",text="Welcome me briefly to the cafe as my companion, not the barista. Offer a choice of relaxed exploration or optional social practice; do not assume I need training."});}
            return;}
        if(e.@event=="error"){State=e.text;failed=true;Ready=false;connecting=false;error?.Invoke(e.text);ClearCallbacks();StopPlayback();return;}
        if(e.@event=="disconnected"||e.@event=="reconnect"){Ready=false;connecting=false;retryAt=Time.unscaledTime+2;State="Reconnecting Gemini Live";return;}
        if(e.@event=="interrupted"){Interruptions++;StopPlayback();State="Gemini listening";return;}
        if(e.@event=="input_text")
        {
            LastHeard=e.text;State="Hearing you";
            string tone=System.Text.RegularExpressions.Regex.IsMatch(e.text.ToLowerInvariant(),@"\b(sad|nervous|anxious|afraid|upset|lonely)\b")?"concerned":e.text.ToLowerInvariant().Contains("confused")?"thinking":"friendly";
            emotion=tone;ActiveActor()?.ApplyEmotion(tone);
            if(completed==null){var chat=FindAnyObjectByType<ChatUIManager>();if(chat!=null)chat.ShowLiveInput(e.text);else FindAnyObjectByType<HomeUIManager>()?.ShowLiveInput(e.text);}
        }
        if(e.@event=="output_text")
        {
            State="Gemini speaking";
            if(partial!=null)partial(e.text);
            else{var chat=FindAnyObjectByType<ChatUIManager>();if(chat!=null)chat.ShowLiveOutput(e.text);else FindAnyObjectByType<HomeUIManager>()?.ShowLiveOutput(e.text);}
        }
        if(e.@event=="turn_complete")
        {
            turnEnded=true;State="Gemini listening";
            var identity=FindAnyObjectByType<ScenarioCharacterIdentity>();
            var response=new ChatResponse{response=e.text??"",speaker_id=speaker,speaker_name=Partner!=null?Partner.GetDisplayName():identity!=null?identity.DisplayName:speaker,
                scenario=scenario,emotion="friendly",action="gesture",intent="conversation",spoken_stream=true,response_source="gemini-live"};
            if(completed!=null){var done=completed;ClearCallbacks();done(response);}
            else if(!string.IsNullOrWhiteSpace(e.text)){var chat=FindAnyObjectByType<ChatUIManager>();if(chat!=null)chat.FinishLiveReply(response);else FindAnyObjectByType<HomeUIManager>()?.FinishLiveReply(response);}
        }
        if(e.@event=="command")
        {
            bool ok=false;string reply="Unsupported command";
            if(e.name=="move_character")ok=VoiceCommandRouter.TryExecute(e.command,out reply);
            else if(e.name=="show_model")
            {
                ok=scenario=="classroom"&&ClassroomLearningExhibit.ShowModel(e.command);
                if(ok)ActiveActor()?.PlayAction("show_model");
                reply=ok?"Displayed "+e.command+" in the classroom.":"That model is unavailable here. Offer a model from the supported list; no online image feature is enabled.";
            }
            else if(e.name=="talk_to_person")
            {
                ObjectMetadata match=null;
                foreach(var person in FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Exclude))
                    if(person.category=="Person"&&person.GetDisplayName().IndexOf(e.command,StringComparison.OrdinalIgnoreCase)>=0){match=person;break;}
                ok=match!=null;reply=ok?"Switching conversation to "+match.GetDisplayName():"No person matches that name. Ask which person.";
                if(ok)StartCoroutine(SwitchPartner(match));
            }
            else if(e.name=="change_world")
            {
                ok=e.scenario=="home"||e.scenario=="ancient_rome"||e.scenario=="museum"||e.scenario=="classroom"||e.scenario=="cafe";
                reply=ok?"Opening "+e.scenario:"Unknown world";
            }
            Send(new Event{@event="tool_result",id=e.id,name=e.name,text=reply,ok=ok});Commands++;
            if(ok&&e.name=="change_world")StartCoroutine(ChangeWorld(e.scenario));
        }
    }
    private IEnumerator SwitchPartner(ObjectMetadata person){yield return new WaitForSecondsRealtime(.4f);SelectPartner(person);float until=Time.unscaledTime+12;while(!Ready&&Time.unscaledTime<until)yield return null;if(Ready)Send(new Event{@event="text",text="Say a brief friendly hello to the visitor as yourself."});}
    private IEnumerator ChangeWorld(string name){yield return new WaitForSecondsRealtime(.4f);if(name=="home")ScenarioTransitionService.LoadHome();else ScenarioTransitionService.LoadScenario(name);}
    public void SendMicrophone(float[] frame)
    {
        if(!Ready||wire==null)return;
        if(wire.outgoing.Count>100){State="Microphone connection is falling behind";return;}
        var pcm=new byte[frame.Length*2];for(int i=0;i<frame.Length;i++){short v=(short)(Mathf.Clamp(frame[i],-1,1)*32767);pcm[i*2]=(byte)v;pcm[i*2+1]=(byte)(v>>8);}
        wire.outgoing.Enqueue(new Outgoing{data=pcm,type=WebSocketMessageType.Binary});MicrophoneFrames++;
    }
    private void Send(Event value){wire?.outgoing.Enqueue(new Outgoing{data=Encoding.UTF8.GetBytes(JsonUtility.ToJson(value)),type=WebSocketMessageType.Text});}
    public bool SendText(string text,Action<ChatResponse> onDone,Action<string> onPartial,Action<string> onError)
    {
        if(!Ready)return false;CancelReply();completed=onDone;partial=onPartial;error=onError;sentAt=Time.realtimeSinceStartup;FirstAudioSeconds=-1;
        Send(new Event{@event="text",text=text});return true;
    }
    public void SetVoiceMuted(bool muted){output.mute=muted;}
    public void PauseMicrophone(){if(Ready)Send(new Event{@event="audio_end"});}
    public void CancelReply(){ClearCallbacks();StopPlayback();}
    private void ClearCallbacks(){completed=null;partial=null;error=null;}
    public void StopPlayback(){lock(audioLock){buffered=read=write=0;}if(output!=null)output.Stop();playing=false;turnEnded=false;if(actor!=null)actor.EndSpeech();}
    private void ReadAudio(float[] data){lock(audioLock)for(int i=0;i<data.Length;i++){if(buffered>0){data[i]=ring[read];read=(read+1)%ring.Length;buffered--;}else data[i]=0;}}
    private void Disconnect(){epoch++;Ready=false;connecting=false;wire?.cancel.Cancel();wire?.socket.Abort();wire=null;StopPlayback();ClearCallbacks();}
    private void OnDisable(){Disconnect();}
    private void OnDestroy(){Disconnect();if(clip!=null)Destroy(clip);if(Instance==this)Instance=null;}
}

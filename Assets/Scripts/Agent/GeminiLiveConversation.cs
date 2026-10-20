using PlayerPrefs = LumenPreferences;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
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
    [Serializable] private sealed class Setup {public string scenario,speaker_id,object_name,object_description,voice_name,session_id,world_context;public bool coaching_active;public string[] people;}
    [Serializable] private sealed class Event {public string @event,text,id,name,command,scenario,voice,state,action_id,turn_id;public bool ok,retryable;}
    private sealed class Wire
    {
        public ClientWebSocket socket=new ClientWebSocket();public CancellationTokenSource cancel=new CancellationTokenSource();
        public ConcurrentQueue<Outgoing> outgoing=new ConcurrentQueue<Outgoing>();public int epoch;
    }
    private struct Outgoing {public byte[] data;public WebSocketMessageType type;}
    private struct Incoming {public int epoch;public byte[] audio;public string text;}
    private readonly ConcurrentQueue<Incoming> incoming=new ConcurrentQueue<Incoming>();
    private int epoch;
    private int textSubmission;
    private GeminiLiveAudio audio;
    public ConversationImagePanel Images {get;private set;}
    private bool receivingVoiceQuestion;
    private float emptySince=-1;
    // Remains pending through audio/tool continuations; a delayed interruption can belong to the preceding reply.
    private bool awaitingTypedOutput;
    public ObjectMetadata Partner {get;private set;}
    private bool introduced,turnActive;
    private int retryCount;
    private string activeTurnId="", currentQuestion="";
    private WorldActionExecution activeAction;
    private string emotion="friendly";
    public void SelectPartner(ObjectMetadata person)
    {
        if(person!=null&&person.GetComponentInParent<ScenarioCharacterIdentity>()!=null)person=null;
        if(person!=null&&person.category!="Person")person=null;
        if(Partner==person)return;
        FindAnyObjectByType<ChatUIManager>()?.InterruptCurrentReply();
        ResetTranscriptTurn();Partner=person;fingerprint="";Disconnect();failed=false;retryAt=0;
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
    public bool IsSpeaking=>playing;
    public string VoiceName {get;private set;}="";
    public string LastReply {get;private set;}="";
    public int Replies {get;private set;}
    public string ActiveSpeakerName=>Partner!=null?Partner.GetDisplayName():FindAnyObjectByType<ScenarioCharacterIdentity>()?.DisplayName??"Kai";
    public Transform ActiveSpeakerTransform=>ActiveActor()?.transform;
    public bool Selected {get;private set;}
    public bool Ready {get;private set;}
    public string State {get;private set;}="Assistant connecting";
    public string LastHeard {get;private set;}="";
    public int MicrophoneFrames {get;private set;}
    public int AudioChunks {get;private set;}
    public int Interruptions {get;private set;}
    public int Commands {get;private set;}
    public float FirstAudioSeconds {get;private set;}=-1;
    public float BufferedSeconds=>audio!=null?audio.BufferedSeconds:0;
    public float OutputLevel=>audio!=null?audio.Level:0;
    private void Awake()
    {
        if(Instance!=null&&Instance!=this){Destroy(this);return;}Instance=this;
        Images=GetComponent<ConversationImagePanel>()??gameObject.AddComponent<ConversationImagePanel>();
        var voiceObject=new GameObject("Gemini native audio");voiceObject.transform.SetParent(transform,false);
        output=voiceObject.AddComponent<AudioSource>();audio=voiceObject.AddComponent<GeminiLiveAudio>();output.playOnAwake=false;output.spatialBlend=0;output.volume=LumenPreferences.GetInt("Lumen.SpeechVolume",90)/100f;
        output.mute=PlayerPrefs.GetInt("Lumen.MuteVoice",0)!=0;
        LumenAudioMix.Route(output,"Speech");
        clip=AudioClip.Create("Gemini live carrier",24000,1,24000,false);output.clip=clip;output.loop=true;
    }
    private IEnumerator Start()
    {
        while(enabled)
        {
            var client=GetComponent<AIClient>();if(client!=null)backend=client.BackendUrl;
            if(LumenPreferences.GetInt("Lumen.Offline",0)!=0){Disconnect();Selected=false;State="Offline / exhibit labels available";}
            else if(LumenPreferences.GetInt("Lumen.Offline",0)!=0){Disconnect();Selected=false;State="Offline / exhibit labels available";}
            else if(!Selected)
            {
                using(var request=UnityWebRequest.Get(backend+"/live/status"))
                {
                    request.timeout=4;yield return request.SendWebRequest();
                    if(request.result!=UnityWebRequest.Result.Success)State="Assistant unavailable / start the local service";
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
        if(Selected&&LumenPreferences.GetInt("Lumen.Offline",0)==0&&Time.unscaledTime>=nextIdentityCheck)
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
                audio.Enqueue(item.audio);emptySince=-1;
                AudioChunks++;turnEnded=false;
                if(FirstAudioSeconds<0&&sentAt>0)FirstAudioSeconds=Time.realtimeSinceStartup-sentAt;
            }
            else HandleEvent(item.text);
        }
        if(!playing&&BufferedSeconds>=.08f)
        {
            var identity=FindAnyObjectByType<ScenarioCharacterIdentity>();actor=ActiveActor();
            FindAnyObjectByType<SpeechOutputController>()?.CancelSpeech();
            output.Play();playing=true;if(actor!=null){actor.ApplyEmotion(emotion);actor.BeginSpeech(output,()=>audio.Level);}
        }
        if(playing&&turnEnded&&BufferedSeconds<=.001f)
        {
            if(emptySince<0)emptySince=Time.unscaledTime;
            if(Time.unscaledTime-emptySince>.15f){output.Stop();playing=false;if(actor!=null)actor.EndSpeech();}
        }
        else emptySince=-1;
    }
    private string NpcVoice(ObjectMetadata[] people,ObjectMetadata person,string[] fallback)
    {
        if(scenario!="cafe")return fallback[Array.IndexOf(people,person)%fallback.Length];
        bool Female(ObjectMetadata p)=>p.objectId.ToLowerInvariant().Contains("woman")||p.GetComponentInChildren<ScenarioCharacterVisual>()?.ResourceId=="Maya"||p.GetComponentInChildren<ScenarioCharacterVisual>()?.ResourceId=="Ada";
        bool female=Female(person);var group=people.Where(p=>Female(p)==female).ToArray();
        string[] pool=female?new[]{"Kore","Leda","Zephyr","Despina","Gacrux","Laomedeia"}:new[]{"Schedar","Algenib","Rasalgethi","Algieba"};
        return pool[Array.IndexOf(group,person)%pool.Length];
    }
    private void Connect()
    {
        Disconnect();connecting=true;State="Assistant connecting";
        wire=new Wire{epoch=epoch};var current=wire;
        var people=FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Exclude).Where(p=>p.category=="Person").OrderBy(p=>p.objectId,StringComparer.Ordinal).ToArray();
        string[] voices={"Kore","Leda","Zephyr","Schedar","Algenib","Rasalgethi","Algieba","Despina","Gacrux","Laomedeia","Pulcherrima","Sulafat"};
        int personIndex=Array.IndexOf(people,Partner);
        string setup=JsonUtility.ToJson(new Setup{session_id=GetComponent<AIClient>()?.SessionId??"",coaching_active=FindAnyObjectByType<SocialCoachingController>()?.IsActive??false,world_context=WorldStateContext.Snapshot()+" "+VisitorConversationMemory.Context(speaker),voice_name=personIndex>=0?NpcVoice(people,Partner,voices):"",people=people.Select(p=>p.GetDisplayName()).ToArray(),scenario=scenario,speaker_id=speaker,object_name=Partner!=null?Partner.GetDisplayName():"",object_description=Partner!=null?Partner.description:""});
        string address=backend.Replace("https://","wss://").Replace("http://","ws://")+"/live";
        _=Task.Run(async()=>
        {
            try
            {
                current.cancel.CancelAfter(TimeSpan.FromSeconds(12));
                await current.socket.ConnectAsync(new Uri(address),current.cancel.Token);
                current.cancel.CancelAfter(Timeout.Infinite);
                var initial=Encoding.UTF8.GetBytes(setup);
                await current.socket.SendAsync(new ArraySegment<byte>(initial),WebSocketMessageType.Text,true,current.cancel.Token);
                var send=SendLoop(current);var receive=ReceiveLoop(current);
                await Task.WhenAny(send,receive);current.cancel.Cancel();await Task.WhenAll(send,receive);
            }
            catch(OperationCanceledException){incoming.Enqueue(new Incoming{epoch=current.epoch,text="{\"event\":\"disconnected\"}"});}
            catch(Exception){incoming.Enqueue(new Incoming{epoch=current.epoch,text="{\"event\":\"disconnected\"}"});}
            finally{current.socket.Dispose();current.cancel.Dispose();}
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
            if(incoming.Count>=256)throw new IOException("Conversation output queue is full");
            incoming.Enqueue(result.MessageType==WebSocketMessageType.Binary?new Incoming{epoch=current.epoch,audio=data}:new Incoming{epoch=current.epoch,text=Encoding.UTF8.GetString(data)});
        }
        incoming.Enqueue(new Incoming{epoch=current.epoch,text="{\"event\":\"disconnected\"}"});
    }
    private void HandleEvent(string json)
    {
        Event e=null;try{e=JsonUtility.FromJson<Event>(json);}catch(Exception){}if(e==null)return;
        if(e.@event=="practice_expired"){FindAnyObjectByType<SocialCoachingController>()?.NotifyExpired();return;}
        if(e.@event=="ready"){VoiceName=e.voice;Ready=true;connecting=false;retryCount=0;State="Assistant ready";SendWorldContext();
            if(!introduced&&scenario=="cafe"&&Partner==null&&PlayerPrefs.GetInt("Lumen.VoiceEnabled",0)!=0){introduced=true;Send(new Event{@event="text",text="Welcome me briefly to the cafe as my companion, not the barista. Offer a choice of relaxed exploration or optional social practice; do not assume I need training."});}
            return;}
        if(e.@event=="error")
        {
            var notify=error;Disconnect();
            if(e.retryable)ScheduleRetry();else{failed=true;State="Assistant unavailable / retry after checking service configuration";}
            notify?.Invoke("The assistant is unavailable. You can keep exploring and retry.");return;
        }
        if(e.@event=="disconnected"||e.@event=="reconnect"){var notify=error;Disconnect();ScheduleRetry();notify?.Invoke("Connection interrupted. Please retry when the assistant is ready.");return;}
        if(e.@event=="interrupted"){Interruptions++;StopPlayback();if(!awaitingTypedOutput){error?.Invoke("Reply interrupted; listening to you.");ClearCallbacks();ResetTranscriptTurn();}State="Assistant ready";return;}
        if(e.@event=="cancel_commands")
        {
            if(activeAction!=null&&!activeAction.Terminal)
            {activeAction.Set("cancelled","The current request was interrupted.");FindAnyObjectByType<VoicePlayerNavigation>()?.Cancel();FindAnyObjectByType<ScenarioCharacterIdentity>()?.GetComponent<AgentCompanionController>()?.SetVoiceFollow(false);}
            return;
        }
        if(e.@event=="input_text")
        {
            turnActive=true;awaitingTypedOutput=false;LastHeard=e.text;currentQuestion=e.text;VisitorConversationMemory.ObservePreference(e.text);State="Assistant processing";if(!receivingVoiceQuestion){activeTurnId=Guid.NewGuid().ToString("N");SendWorldContext();}
            if(completed!=null){error?.Invoke("Reply interrupted; listening to you.");ClearCallbacks();ResetTranscriptTurn();}
            if(!receivingVoiceQuestion){receivingVoiceQuestion=true;Images.BeginQuestion(e.text,scenario!="cafe");}
            Images.UpdateQuestion(e.text);
            ApplyInputEmotion(e.text);
            if(completed==null){var chat=FindAnyObjectByType<ChatUIManager>();if(chat!=null)chat.ShowLiveInput(e.text);else FindAnyObjectByType<HomeUIManager>()?.ShowLiveInput(e.text);}
        }
        if(e.@event=="output_text")
        {
            turnActive=true;State="Assistant speaking";
            if(partial!=null)partial(e.text);
            else{var chat=FindAnyObjectByType<ChatUIManager>();if(chat!=null)chat.ShowLiveOutput(e.text);else FindAnyObjectByType<HomeUIManager>()?.ShowLiveOutput(e.text);}
        }
        if(e.@event=="turn_complete")
        {
            turnEnded=true;State="Assistant ready";
            if(string.IsNullOrWhiteSpace(e.text))return; // Empty tool boundaries are not conversational answers.
            LastReply=e.text;Replies++;receivingVoiceQuestion=false;turnActive=false;VisitorConversationMemory.Record(speaker,currentQuestion,e.text);FindAnyObjectByType<SocialCoachingController>()?.CompleteTurn(currentQuestion,activeTurnId);
            Images.CompleteAnswer(e.text,backend);
            var identity=FindAnyObjectByType<ScenarioCharacterIdentity>();
            var response=new ChatResponse{response=e.text??"",speaker_id=speaker,speaker_name=Partner!=null?Partner.GetDisplayName():identity!=null?identity.DisplayName:speaker,
                scenario=scenario,emotion=emotion,action="idle",intent="conversation",spoken_stream=true,response_source="gemini-live"};
            if(completed!=null){var done=completed;ClearCallbacks();done(response);}
            else if(!string.IsNullOrWhiteSpace(e.text)){var chat=FindAnyObjectByType<ChatUIManager>();if(chat!=null)chat.FinishLiveReply(response);else FindAnyObjectByType<HomeUIManager>()?.FinishLiveReply(response);}
        }
        if(e.@event=="command")
        {
            bool ok=false;string reply="Unsupported command",lifecycle="completed",actionId=e.id;
            if(e.name=="show_image")
            {
                ok=Images.CanShowForQuestion&&!string.IsNullOrWhiteSpace(e.command);
                if(ok)Images.RequestImage(e.command,backend);
                reply=ok?"Image search started in a separate panel. Speak your explanation now; do not claim the image is already visible. The panel will show the image with credits or an unavailable message.":"No image requested. In the cafe keep this answer voice/text only unless the visitor explicitly requests a visual aid.";
            }
            else if(e.name=="move_character")
            {
                activeAction=VoiceCommandRouter.Execute(e.command,activeTurnId);
                StartCoroutine(CompleteMovement(e,activeAction,epoch));Commands++;return;
            }
            else if(e.name=="show_model")
            {
                ok=scenario=="classroom"&&ClassroomLearningExhibit.ShowModel(e.command);
                if(ok)ActiveActor()?.PlayAction("show_model");
                reply=ok?"Displayed "+e.command+". "+ClassroomLearningExhibit.ActiveDescription:"That interactive 3D model is unavailable here. Use show_image to look up a relevant picture instead; clearly call it an image.";
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
                var travel=ScenarioTransitionService.RequestTravel(e.scenario);
                ok=travel.accepted;reply=travel.reason;lifecycle=travel.state;actionId=travel.actionId;
            }
            Send(new Event{@event="tool_result",id=e.id,name=e.name,text=reply+" Authoritative state: "+WorldStateContext.Snapshot(),ok=ok,state=ok?lifecycle:"failed",action_id=actionId,turn_id=activeTurnId});Commands++;
            
        }
    }
    private IEnumerator SwitchPartner(ObjectMetadata person){yield return new WaitForSecondsRealtime(.4f);SelectPartner(person);float until=Time.unscaledTime+12;while(!Ready&&Time.unscaledTime<until)yield return null;if(Ready)Send(new Event{@event="text",text="Say a brief friendly hello to the visitor as yourself."});}
    private IEnumerator ChangeWorld(string name){yield return new WaitForSecondsRealtime(.4f);if(name=="home")ScenarioTransitionService.LoadHome();else ScenarioTransitionService.LoadScenario(name);}
    public void SendMicrophone(float[] frame)
    {
        if(!Ready||wire==null)return;
        if(SpeechInputController.Instance!=null&&SpeechInputController.Instance.InputLevel>.01f)turnActive=true;
        if(wire.outgoing.Count>100){State="Microphone connection is falling behind";return;}
        var pcm=new byte[frame.Length*2];for(int i=0;i<frame.Length;i++){short v=(short)(Mathf.Clamp(frame[i],-1,1)*32767);pcm[i*2]=(byte)v;pcm[i*2+1]=(byte)(v>>8);}
        wire.outgoing.Enqueue(new Outgoing{data=pcm,type=WebSocketMessageType.Binary});MicrophoneFrames++;
    }
    private void Send(Event value){if(value.@event=="text")turnActive=true;wire?.outgoing.Enqueue(new Outgoing{data=Encoding.UTF8.GetBytes(JsonUtility.ToJson(value)),type=WebSocketMessageType.Text});}
    private void ApplyInputEmotion(string text)
    {
        string value=(text??"").ToLowerInvariant();
        emotion=System.Text.RegularExpressions.Regex.IsMatch(value,@"\b(sad|nervous|anxious|afraid|upset|lonely)\b")?"concerned":value.Contains("confused")?"thinking":"friendly";
        ActiveActor()?.ApplyEmotion(emotion);
    }
    public bool SendText(string text,Action<ChatResponse> onDone,Action<string> onPartial,Action<string> onError)
    {
        if(!Ready)return false;
        if(turnActive||completed!=null||playing||BufferedSeconds>.001f)
        {
            CancelReply();int ticket=++textSubmission;
            StartCoroutine(SubmitAfterStop(text,onDone,onPartial,onError,ticket));return true;
        }
        textSubmission++;return SendReadyText(text,onDone,onPartial,onError);
    }
    private IEnumerator SubmitAfterStop(string text,Action<ChatResponse> done,Action<string> partialReply,Action<string> failure,int ticket)
    {
        float until=Time.unscaledTime+15;
        while(!Ready&&Time.unscaledTime<until&&ticket==textSubmission)yield return null;
        if(ticket!=textSubmission)yield break;
        if(!Ready){failure?.Invoke("The assistant did not reconnect. Select Retry and send your question again.");yield break;}
        SendReadyText(text,done,partialReply,failure);
    }
    private bool SendReadyText(string text,Action<ChatResponse> onDone,Action<string> onPartial,Action<string> onError)
    {
        if(!Ready)return false;ClearCallbacks();StopPlayback();activeTurnId=Guid.NewGuid().ToString("N");currentQuestion=text;VisitorConversationMemory.ObservePreference(text);SendWorldContext();receivingVoiceQuestion=false;Images.BeginQuestion(text,scenario!="cafe");ApplyInputEmotion(text);awaitingTypedOutput=true;completed=onDone;partial=onPartial;error=onError;sentAt=Time.realtimeSinceStartup;FirstAudioSeconds=-1;
        Send(new Event{@event="text",text=text,turn_id=activeTurnId});State="Assistant processing";return true;
    }
    public void Reconnect(){Disconnect();Selected=false;ResetTranscriptTurn();failed=false;retryCount=0;retryAt=0;fingerprint="";State="Assistant reconnecting";}
    private void ResetTranscriptTurn(){FindAnyObjectByType<ChatUIManager>()?.ResetLiveTurn();FindAnyObjectByType<HomeUIManager>()?.ResetLiveTurn();}
    public void SetVoiceMuted(bool muted){if(output!=null)output.mute=muted;}
    public void SetVoiceVolume(float value){if(output!=null)output.volume=Mathf.Clamp01(value);}
    public void PauseMicrophone(){if(Ready)Send(new Event{@event="audio_end"});}
    public void CancelReply()
    {
        textSubmission++;
        bool pending=turnActive||receivingVoiceQuestion||completed!=null||awaitingTypedOutput||playing||BufferedSeconds>.001f||(activeAction!=null&&!activeAction.Terminal);
        if(activeAction!=null&&!activeAction.Terminal)
        {
            activeAction.Set("cancelled","The visitor cancelled this turn.");
            FindAnyObjectByType<VoicePlayerNavigation>()?.Cancel();
            FindAnyObjectByType<ScenarioCharacterIdentity>()?.GetComponent<AgentCompanionController>()?.SetVoiceFollow(false);
        }
        ClearCallbacks();StopPlayback();
        // Reopening a local provider session invalidates every queued PCM/text packet by connection epoch.
        // This is deliberate turn cancellation, not a claim that the provider supports timed phonemes.
        if(pending){Disconnect();failed=false;retryAt=0;State="Assistant reconnecting after stop";}
    }
    private void SendWorldContext()=>Send(new Event{@event="world_context",text=WorldStateContext.Snapshot()+" "+VisitorConversationMemory.Context(speaker)+" "+(Partner==null?FindAnyObjectByType<SocialCoachingController>()?.ConversationContext:"Structured practice is paused while speaking to another person. Do not share other people's conversations.")});
    private void ScheduleRetry()
    {
        retryCount++;
        failed=retryCount>5;
        retryAt=Time.unscaledTime+Mathf.Min(16,Mathf.Pow(2,retryCount-1));
        State=failed?"Assistant unavailable / select Retry to reconnect":"Assistant reconnecting ("+retryCount+"/5)";
    }
    private IEnumerator CompleteMovement(Event command,WorldActionExecution execution,int connectionEpoch)
    {
        if(!execution.handled)execution.Set("failed","That is not an executable local walk request. World destinations use a separate transition.");
        float until=Time.unscaledTime+95;
        while(!execution.Terminal&&epoch==connectionEpoch&&Time.unscaledTime<until)
        {State="Walking to "+execution.target;yield return null;}
        if(epoch!=connectionEpoch)yield break;
        if(!execution.Terminal)execution.Set("failed","The route did not finish within its time limit.");
        Send(new Event{@event="tool_result",id=command.id,name=command.name,ok=execution.Succeeded,
            state=execution.state,action_id=execution.actionId,turn_id=execution.turnId,
            text=execution.reason+" "+WorldStateContext.Snapshot()});
        State=execution.Succeeded?"Arrived / assistant ready":execution.reason;
    }
    private void ClearCallbacks(){awaitingTypedOutput=false;completed=null;partial=null;error=null;}
    public void StopPlayback(){if(audio!=null)audio.Clear();emptySince=-1;if(output!=null)output.Stop();playing=false;turnEnded=false;if(actor!=null)actor.EndSpeech();}
    private void Disconnect(){turnActive=false;currentQuestion="";epoch++;receivingVoiceQuestion=false;Images?.ResetConversation();Ready=false;connecting=false;if(wire!=null){try{wire.cancel.Cancel();wire.socket.Abort();}catch(ObjectDisposedException){}}wire=null;StopPlayback();ClearCallbacks();}
    private void OnDisable(){Disconnect();}
    private void OnDestroy(){Disconnect();if(clip!=null)Destroy(clip);if(Instance==this)Instance=null;}
}

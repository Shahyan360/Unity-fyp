using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIClient : MonoBehaviour
{
    private static string sharedSessionId;
    [SerializeField] private AppConfig appConfig;
    [SerializeField] private string fallbackBackendUrl="http://127.0.0.1:5000";
    private UnityWebRequest activeRequest;
    private int generation;
    private string partial="", interrupted="";
    public string SessionId {get;private set;}
    public string BackendUrl => (appConfig!=null&&!string.IsNullOrWhiteSpace(appConfig.backendUrl)?appConfig.backendUrl:fallbackBackendUrl).TrimEnd('/');
    private void Awake(){SessionId=sharedSessionId;if(appConfig==null)appConfig=FindAnyObjectByType<AppConfig>();EmbodiedExperienceBootstrap.Ensure(this);}
    public void CancelRequest(){generation++;interrupted=partial;partial="";activeRequest?.Abort();activeRequest=null;GeminiLiveConversation.Instance?.CancelReply();}
    private void OnDestroy(){CancelRequest();}

    [Serializable] private sealed class Packet {public string @event,text;public ChatResponse result;}
    private sealed class Lines : DownloadHandlerScript
    {
        private readonly Decoder decoder=Encoding.UTF8.GetDecoder();
        private readonly StringBuilder pending=new StringBuilder();
        private readonly Queue<string> lines=new Queue<string>();
        private readonly char[] chars=new char[32768];
        public Lines():base(new byte[16384]){}
        protected override bool ReceiveData(byte[] data,int count)
        {
            if(data==null||count==0)return true;
            lock(lines)
            {
                int n=decoder.GetChars(data,0,count,chars,0);
                for(int i=0;i<n;i++)if(chars[i]=='\n'){lines.Enqueue(pending.ToString());pending.Clear();}else pending.Append(chars[i]);
                return pending.Length<65536&&lines.Count<4096;
            }
        }
        public string Take(){lock(lines)return lines.Count>0?lines.Dequeue():null;}
    }
    public IEnumerator SendChatMessage(string query,string scenario,string objectName,string objectDescription,
        Action<ChatResponse> onSuccess,Action<string> onError,Action<string> onSentence=null,Action<string> onPartial=null)
    {
        if(string.IsNullOrWhiteSpace(query)){onError?.Invoke("Please enter a message.");yield break;}
        int ticket=++generation;partial="";
        var live=GeminiLiveConversation.Instance;
        if(live!=null&&live.Selected)
        {
            if(!string.IsNullOrWhiteSpace(objectName))
            {
                foreach(var person in FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Exclude))
                    if(person.category=="Person"&&person.GetDisplayName()==objectName){live.SelectPartner(person);break;}
            }
            float deadline=Time.realtimeSinceStartup+12;
            while(!live.Ready&&Time.realtimeSinceStartup<deadline){if(ticket!=generation)yield break;yield return null;}
            bool done=false;
            if(!live.SendText(query,response=>{if(ticket==generation)onSuccess?.Invoke(response);done=true;},
                text=>{partial=text;onPartial?.Invoke(text);},message=>{if(ticket==generation)onError?.Invoke(message);done=true;}))
            {onError?.Invoke(live.State);yield break;}
            deadline=Time.realtimeSinceStartup+45;
            while(!done&&ticket==generation&&Time.realtimeSinceStartup<deadline)yield return null;
            if(!done&&ticket==generation){live.CancelReply();onError?.Invoke("Gemini Live did not finish this turn. Please try again.");}
            yield break;
        }
        var identity=FindAnyObjectByType<ScenarioCharacterIdentity>();
        var body=new ChatRequest{query=query,scenario=scenario,object_name=objectName,object_description=objectDescription,
            session_id=SessionId,speaker_id=string.IsNullOrWhiteSpace(objectName)&&identity!=null?identity.ConversationId:"",
            interrupted_response=interrupted};interrupted="";
        using(var request=new UnityWebRequest(BackendUrl+"/chat/stream","POST"))
        {
            var lines=new Lines();request.downloadHandler=lines;
            request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
            request.SetRequestHeader("Content-Type","application/json");request.timeout=40;activeRequest=request;
            var operation=request.SendWebRequest();ChatResponse final=null;string error=null;bool spoken=false;
            while(true)
            {
                if(ticket!=generation)yield break;
                string line;
                while((line=lines.Take())!=null)
                {
                    Packet packet=null;
                    try{packet=JsonUtility.FromJson<Packet>(line);}catch(Exception e){error="Could not read the local reply: "+e.Message;}
                    if(packet==null)continue;
                    if(packet.@event=="delta"){partial+=packet.text;onPartial?.Invoke(partial);}
                    else if(packet.@event=="sentence"&&onSentence!=null){onSentence(packet.text);spoken=true;}
                    else if(packet.@event=="done")final=packet.result;
                    else if(packet.@event=="error")error=packet.text;
                }
                if(operation.isDone)break;
                yield return null;
            }
            activeRequest=null;
            if(final!=null&&!string.IsNullOrWhiteSpace(final.response))
            {
                final.spoken_stream=spoken;
                if(!string.IsNullOrWhiteSpace(final.session_id))SessionId=sharedSessionId=final.session_id;
                partial="";onSuccess?.Invoke(final);
            }
            else onError?.Invoke(error??("Local conversation unavailable. Check the backend on "+BackendUrl+". "+request.error));
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>One optional rehearsal state adopted from the bounded backend session.</summary>
public class SocialCoachingController:MonoBehaviour
{
    [SerializeField] private AIClient client;
    public string ActiveMode {get;private set;}="";
    public string CurrentPrompt {get;private set;}="";
    public string State {get;private set;}="Off";
    public int Turn {get;private set;}
    public bool IsActive=>State=="Active";
    public bool IsAdvancing {get;private set;}
    public string ConversationContext=>IsActive?"Optional practice mode="+ActiveMode+"; current step="+Turn+"/3; prompt="+CurrentPrompt+". Give one concrete observation about the visitor's words. The practice panel alone advances progress after your reply; never invent a completed stage.":"No structured practice is active. Respect relaxed exploration and opt-out.";
    private string practiceSessionId;
    private int generation;
    private UnityWebRequest active;
    private readonly Queue<Answer> pending=new Queue<Answer>();
    private readonly HashSet<string> seen=new HashSet<string>();
    private sealed class Answer {public string text,id;}
    [Serializable] private sealed class StartBody {public string mode,session_id="";}
    [Serializable] private sealed class TurnBody {public string query,turn_id,session_id;}
    [Serializable] private sealed class EndBody {public string session_id;}
    [Serializable] private sealed class Result {public string session_id,mode,prompt;public int turn;public bool active,completed;}
    private void Awake()=>client??=GetComponent<AIClient>();
    public void StartPractice(string mode)
    {
        if(mode!="conversation"&&mode!="interview"&&mode!="presentation")return;
        EndPractice();State="Starting";int ticket=++generation;
        StartCoroutine(Post("/coaching/start",JsonUtility.ToJson(new StartBody{mode=mode}),ticket,result=>
        {
            practiceSessionId=result.session_id;client.AdoptSessionId(result.session_id);Apply(result);
            GeminiLiveConversation.Instance?.Reconnect();
        }));
    }
    public void CompleteTurn(string text,string turnId)
    {
        if(!IsActive||GeminiLiveConversation.Instance?.Partner!=null||string.IsNullOrWhiteSpace(text)||string.IsNullOrWhiteSpace(turnId)||seen.Contains(turnId))return;
        if(pending.Count>=4){State="Unavailable";CurrentPrompt="Practice paused. End or restart when you are ready.";return;}
        if(seen.Count>=32)seen.Clear();seen.Add(turnId);pending.Enqueue(new Answer{text=text.Length>2000?text.Substring(0,2000):text,id=turnId});
        if(!IsAdvancing)StartCoroutine(Advance());
    }
    private IEnumerator Advance()
    {
        IsAdvancing=true;int ticket=generation;
        while(IsActive&&pending.Count>0&&ticket==generation)
        {
            var answer=pending.Dequeue();
            yield return Post("/coaching/turn",JsonUtility.ToJson(new TurnBody{session_id=practiceSessionId,query=answer.text,turn_id=answer.id}),ticket,Apply);
        }
        IsAdvancing=false;
    }
    private void Apply(Result result)
    {
        ActiveMode=result.active?result.mode:"";CurrentPrompt=result.prompt??"";Turn=result.turn;
        State=result.completed?"Completed":result.active?"Active":"Off";
    }
    private IEnumerator Post(string path,string body,int ticket,Action<Result> done)
    {
        if(client==null){State="Unavailable";CurrentPrompt="Practice needs the local service. You can keep exploring.";yield break;}
        using(var request=new UnityWebRequest(client.BackendUrl+path,"POST"))
        {
            request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));request.downloadHandler=new DownloadHandlerBuffer();request.SetRequestHeader("Content-Type","application/json");request.timeout=10;active=request;
            yield return request.SendWebRequest();
            if(ticket!=generation)yield break;active=null;
            Result result=null;if(request.result==UnityWebRequest.Result.Success)try{result=JsonUtility.FromJson<Result>(request.downloadHandler.text);}catch(ArgumentException){}
            if(result==null||string.IsNullOrWhiteSpace(result.session_id)){State="Unavailable";ActiveMode="";CurrentPrompt="Practice is unavailable. End or restart it after reconnecting; free exploration remains available.";yield break;}
            done(result);
        }
    }
    public void EndPractice()
    {
        generation++;active?.Abort();active=null;pending.Clear();seen.Clear();IsAdvancing=false;
        string old=practiceSessionId;practiceSessionId=null;ActiveMode="";CurrentPrompt="";Turn=0;State="Off";
        GeminiLiveConversation.Instance?.CancelReply();
        if(isActiveAndEnabled&&!string.IsNullOrWhiteSpace(old))StartCoroutine(EndRequest(old));
    }
    private IEnumerator EndRequest(string id)
    {
        if(client==null)yield break;
        using(var request=new UnityWebRequest(client.BackendUrl+"/coaching/end","POST"))
        {
            request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new EndBody{session_id=id})));request.downloadHandler=new DownloadHandlerBuffer();request.SetRequestHeader("Content-Type","application/json");request.timeout=5;
            yield return request.SendWebRequest();
        }
    }
    public void NotifyExpired(){generation++;active?.Abort();active=null;pending.Clear();seen.Clear();IsAdvancing=false;practiceSessionId=null;ActiveMode="";State="Expired";CurrentPrompt="Practice expired when the service restarted. Choose a mode to begin again.";}
    private void OnDestroy(){generation++;active?.Abort();}
}

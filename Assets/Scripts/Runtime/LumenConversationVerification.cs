using PlayerPrefs = LumenPreferences;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Explicit opt-in integration journey. Uses real Gemini, never records the human microphone.</summary>
public sealed class LumenConversationVerification:MonoBehaviour
{
    [Serializable] private sealed class Result
    {
        public string status="Running",startedUtc,finishedUtc,buildId,environment;
        public List<string> checks=new List<string>(),errors=new List<string>(),replies=new List<string>(),actions=new List<string>();
        public List<float> firstAudioSeconds=new List<float>();
    }
    private Result report;private string directory;private bool finished;private float deadline;
    private readonly Dictionary<string,string> preferences=new Dictionary<string,string>();
    private int microphoneEnabled;
    private int offlineBefore;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Requested()
    {
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-lumen-conversation-dir");
        if(i>=0&&i+1<args.Length)StartRun(args[i+1]);
    }
    public static bool StartRun(string directory)
    {
        if(FindAnyObjectByType<LumenConversationVerification>()!=null)return false;
        var go=new GameObject("Conversation integration verification");DontDestroyOnLoad(go);
        go.AddComponent<LumenConversationVerification>().Begin(directory);return true;
    }
    private void Begin(string path)
    {
        directory=Path.GetFullPath(path);Directory.CreateDirectory(directory);
        report=new Result{startedUtc=DateTime.UtcNow.ToString("O"),buildId=Application.isEditor?"EDITOR_SOURCE_REVIEW":Application.buildGUID,environment=Application.isEditor?"Editor source review":"Standalone Windows player"};deadline=Time.realtimeSinceStartup+420;
        foreach(string key in new[]{ScenarioRuntimeManager.SelectedScenarioKey,"Lumen.Character.HomeScene","Lumen.Character.Cafestore","Lumen.Character.Classroom"})
            preferences[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetString(key):null;
        microphoneEnabled=PlayerPrefs.GetInt("Lumen.VoiceEnabled",0);
        offlineBefore=PlayerPrefs.GetInt("Lumen.Offline",0);PlayerPrefs.SetInt("Lumen.Offline",0);GeminiLiveConversation.Instance?.Reconnect();
        PlayerPrefs.SetInt("Lumen.VoiceEnabled",0);SpeechInputController.Instance?.SetListening(false);
        Application.runInBackground=true;StartCoroutine(Journey());
    }
    private void Check(bool ok,string text){report.checks.Add((ok?"PASS ":"FAIL ")+text);if(!ok)report.errors.Add(text);Save();}
    private void Save(){File.WriteAllText(Path.Combine(directory,"journey.json"),JsonUtility.ToJson(report,true));}
    private void Update(){if(!finished&&report!=null&&Time.realtimeSinceStartup>deadline){Check(false,"Journey timeout");Finish();}}
    private IEnumerator Ready()
    {
        float until=Time.realtimeSinceStartup+18;
        while(Time.realtimeSinceStartup<until&&(GeminiLiveConversation.Instance==null||!GeminiLiveConversation.Instance.Ready))yield return null;
        Check(GeminiLiveConversation.Instance!=null&&GeminiLiveConversation.Instance.Ready,"Live ready in "+SceneManager.GetActiveScene().name);
    }
    private IEnumerator Load(string scenario,string scene)
    {
        if(scenario=="home")ScenarioTransitionService.LoadHome();else ScenarioTransitionService.LoadScenario(scenario);
        float until=Time.realtimeSinceStartup+15;
        while(SceneManager.GetActiveScene().name!=scene&&Time.realtimeSinceStartup<until)yield return null;
        yield return new WaitForSecondsRealtime(3);
        string actual=ScenarioRuntimeManager.GetScenarioForScene(SceneManager.GetActiveScene().name);
        bool valid=SceneManager.GetActiveScene().name==scene&&actual==(scenario=="home"?"general_assistant":scenario);
        Check(valid,"Actual scene/variant="+SceneManager.GetActiveScene().name+"/"+actual+" expected="+scene+"/"+scenario);
        if(!valid){Finish();yield break;}yield return Ready();
    }
    private IEnumerator Ask(string question,AgentAnimator expected=null,ObjectMetadata addressed=null)
    {
        var live=GeminiLiveConversation.Instance;if(live==null||!live.Ready){Check(false,"Cannot ask: "+question);yield break;}
        bool done=false;string answer="",error="";float otherJaw=0,speakerJaw=0,peakOutput=0;
        var actors=FindObjectsByType<AgentAnimator>(FindObjectsInactive.Exclude);
        var chat=FindAnyObjectByType<ChatUIManager>();
        Action<ChatResponse> received=r=>{answer=r.response;done=true;};
        if(addressed!=null&&chat!=null){chat.ResponseReceived+=received;chat.SubmitWorldPrompt(question,addressed);}
        else live.SendText(question,received,_=>{},e=>{error=e;done=true;});
        float until=Time.realtimeSinceStartup+35;
        while(Time.realtimeSinceStartup<until&&(!done||live.IsSpeaking||live.BufferedSeconds>.01f))
        {
            peakOutput=Mathf.Max(peakOutput,live.OutputLevel);
            if(expected!=null)foreach(var actor in actors)
            {
                if(actor==null)continue;float jaw=Jaw(actor);
                if(actor==expected)speakerJaw=Mathf.Max(speakerJaw,jaw);else otherJaw=Mathf.Max(otherJaw,jaw);
            }
            yield return null;
        }
        if(addressed!=null&&chat!=null)chat.ResponseReceived-=received;
        Check(done&&string.IsNullOrWhiteSpace(error)&&!string.IsNullOrWhiteSpace(answer),"Reply: "+question+(error==""?"":" / "+error));
        report.replies.Add(live.ActiveSpeakerName+" ["+live.VoiceName+"] "+answer);report.firstAudioSeconds.Add(live.FirstAudioSeconds);
        report.actions.Add(question+" | "+live.LastToolCommand+" | "+JsonUtility.ToJson(VoiceCommandRouter.LastExecution));
        if(expected!=null){Check(peakOutput>.005f,"Native PCM reached audio device ("+peakOutput+")");Check(speakerJaw>3,"Addressed speaker jaw moved ("+speakerJaw+")");Check(otherJaw<1,"Other jaws silent ("+otherJaw+")");}
        Save();yield return new WaitForSecondsRealtime(.5f);
    }
    private static float Jaw(AgentAnimator actor)
    {
        float value=0;foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mesh=skin.sharedMesh;if(mesh==null)continue;
            for(int i=0;i<mesh.blendShapeCount;i++)if(mesh.GetBlendShapeName(i).ToLowerInvariant().Contains("ak_")&&mesh.GetBlendShapeName(i).ToLowerInvariant().EndsWith("jawopen"))value=Mathf.Max(value,skin.GetBlendShapeWeight(i));
        }
        return value;
    }
    private IEnumerator SpokenMarket()
    {
        string fixture=Path.Combine(Application.streamingAssetsPath,"Verification","take-me-market-16k.wav");
        var live=GeminiLiveConversation.Instance;
        if(!File.Exists(fixture)){Check(false,"Synthetic voice fixture unavailable");yield break;}
        byte[] wav=File.ReadAllBytes(fixture);int before=live.Commands;int replies=live.Replies;
        // Exercise native barge-in with synthetic audio, independent of the physical microphone.
        int interrupted=live.Interruptions;
        live.SendText("Tell me about Roman daily life in six sentences.",_=>{},_=>{},_=>{});
        float speakingUntil=Time.realtimeSinceStartup+12;while(!live.IsSpeaking&&Time.realtimeSinceStartup<speakingUntil)yield return null;
        Check(live.IsSpeaking,"Reply playing before spoken interruption");
        float[] frame=new float[320];
        for(int start=44;start<wav.Length+16000*2;start+=640)
        {
            for(int i=0;i<320;i++){int offset=start+i*2;frame[i]=offset+1<wav.Length?(short)(wav[offset]|(wav[offset+1]<<8))/32768f:0;}
            live.SendMicrophone(frame);yield return new WaitForSecondsRealtime(.02f);
        }
        float until=Time.realtimeSinceStartup+25;
        while(Time.realtimeSinceStartup<until&&(live.Replies==replies||live.IsSpeaking||live.BufferedSeconds>.01f))yield return null;
        Check(live.Interruptions>interrupted,"Native spoken command interrupted the previous answer");
        Check(live.LastHeard.ToLowerInvariant().Contains("market"),"Native recognition understood synthetic market request");
        Check(live.Commands>before,"Spoken request invoked a real Gemini movement tool");
        Check(live.Replies>replies,"Spoken movement request received a finished response");
        report.actions.Add("Synthetic spoken market | "+live.LastToolCommand+" | "+JsonUtility.ToJson(VoiceCommandRouter.LastExecution));Save();
    }
    private IEnumerator WalkFinished()
    {
        float until=Time.realtimeSinceStartup+35;yield return new WaitForSecondsRealtime(.5f);
        while(Time.realtimeSinceStartup<until&&FindAnyObjectByType<VoicePlayerNavigation>()?.IsWalking==true)yield return null;
    }
    private IEnumerator Capture(string name)
    {
        yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();
        string scene=SceneManager.GetActiveScene().name,variant=ScenarioRuntimeManager.GetScenarioForScene(scene);
        var image=ScreenCapture.CaptureScreenshotAsTexture();
        if(image==null){Check(false,"Rendered capture unavailable: "+name);yield break;}
        var pixels=image.GetPixels32();int lit=0;for(int i=0;i<pixels.Length;i+=97)if(pixels[i].r+pixels[i].g+pixels[i].b>24)lit++;
        Check(lit>pixels.Length/97*.04f,"Rendered capture is not blank: "+name);
        File.WriteAllBytes(Path.Combine(directory,name+"__"+scene+"__"+variant+"__"+report.buildId+".png"),image.EncodeToPNG());Destroy(image);
        yield return null;
    }
    private IEnumerator Journey()
    {
        Screen.SetResolution(1600,900,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(2);
        yield return Load("home","HomeScene");if(finished)yield break;Check(GameObject.Find("LumenWalkableHub")!=null,"Portal garden visible");
        var button=GameObject.Find("SwitchGuideCharacter");Check(button!=null,"Guide button visible");
        button?.GetComponent<Button>().onClick.Invoke();
        var professor=GameObject.Find("Choose Professor");Check(professor!=null,"Named guide choices visible");
        yield return Capture("01-hub-guide-menu");professor?.GetComponent<Button>().onClick.Invoke();yield return new WaitForSecondsRealtime(1);yield return Ready();
        Check(FindAnyObjectByType<ScenarioCharacterIdentity>()?.DisplayName=="Professor Theo","Guide button switches appearance");
        PlayerPrefs.SetString("Lumen.Character.HomeScene","OpeningHero");
        yield return Load("ancient_rome","HistoricalPlace");if(finished)yield break;
        var player=FindAnyObjectByType<CafeWalkController>();Vector3 start=player.transform.position;
        yield return SpokenMarket();yield return WalkFinished();
        Check(Vector3.Distance(start,player.transform.position)>2,"Visitor walked automatically to market");
        var helper=FindAnyObjectByType<ScenarioCharacterIdentity>();Check(Vector3.Distance(player.transform.position,helper.transform.position)<7,"Helper accompanied visitor");
        yield return Capture("02-market-automatic-walk");
        yield return Ask("Take me inside the Colosseum to watch the fight.");yield return WalkFinished();
        Check(Vector3.Distance(player.transform.position,new Vector3(0,.78f,-3))<.8f,"Automatic arrival at interior viewing terrace");
        player.FaceTarget(GameObject.Find("Cassius")?.transform);yield return Capture("03-arena-inside");
        yield return Load("classroom","Classroom");if(finished)yield break;
        yield return Ask("Show me an atom model now.");Check(GameObject.Find("ActiveLearningModel_Atom")!=null,"Gemini actually displayed atom");
        player=FindAnyObjectByType<CafeWalkController>();var model=GameObject.Find("ActiveLearningModel_Atom");if(model!=null)player.FaceTarget(model.transform);
        yield return Capture("04-classroom-atom");
        yield return Ask("Now show me a DNA model.");Check(GameObject.Find("ActiveLearningModel_DNA")!=null,"Gemini replaced model with DNA");
        yield return Ask("Show me an Eiffel Tower 3D model.");Check(GameObject.Find("ActiveLearningModel_DNA")!=null,"Unsupported model did not replace valid exhibit");
        yield return Load("cafe","Cafestore");if(finished)yield break;yield return new WaitForSecondsRealtime(2);
        float welcomeUntil=Time.realtimeSinceStartup+20;while((GeminiLiveConversation.Instance.IsSpeaking||GeminiLiveConversation.Instance.BufferedSeconds>.01f)&&Time.realtimeSinceStartup<welcomeUntil)yield return null;
        yield return Ask("Are you my friend and practice companion, or are you the barista?");
        var guests=FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Exclude).Where(p=>p.category=="Person"&&p.objectId.StartsWith("cafe_cafeguest_")).OrderBy(p=>p.objectId).Take(2).ToArray();
        Check(guests.Length==2,"Two actual cafe guests available");var voices=new HashSet<string>();
        foreach(var person in guests)
        {
            var chat=FindAnyObjectByType<ChatUIManager>();GeminiLiveConversation.Instance.SelectPartner(person);yield return Ready();voices.Add(GeminiLiveConversation.Instance.VoiceName);
            var actor=person.GetComponent<AgentAnimator>()??person.GetComponentInChildren<AgentAnimator>();
            yield return Ask("Hello. Who are you? Tell me in one short sentence.",actor,person);
            yield return Ask("Are you a guest here or do you work behind the counter?",actor);
            Check(GeminiLiveConversation.Instance.Partner==person,"Conversation stayed with selected guest");
            FindAnyObjectByType<CafeWalkController>().FaceTarget(person.transform);yield return Capture("05-cafe-"+person.objectId);
        }
        Check(voices.Count==guests.Length,"Cafe guests have distinct provider voices");
        GameObject.Find("SwitchGuideCharacter")?.GetComponent<Button>().onClick.Invoke();
        GameObject.Find("Return to helper")?.GetComponent<Button>().onClick.Invoke();yield return Ready();
        Check(GeminiLiveConversation.Instance.Partner==null,"Return to helper button works");
        yield return Ask("I am nervous about saying hello to someone. Help me practise one small step.",FindAnyObjectByType<ScenarioCharacterIdentity>()?.GetComponent<AgentAnimator>());
        yield return Load("museum","Classroom");if(finished)yield break;yield return Capture("06-museum");
        Check(ScenarioRuntimeManager.GetScenarioForScene("Classroom")=="museum","Museum variant still loads");
        yield return Load("home","HomeScene");if(finished)yield break;Finish();
    }
    private void Finish()
    {
        if(finished)return;finished=true;
        foreach(var p in preferences){if(p.Value==null)PlayerPrefs.DeleteKey(p.Key);else PlayerPrefs.SetString(p.Key,p.Value);}
        PlayerPrefs.SetInt("Lumen.VoiceEnabled",microphoneEnabled);PlayerPrefs.Save();
        PlayerPrefs.SetInt("Lumen.Offline",offlineBefore);PlayerPrefs.Save();GeminiLiveConversation.Instance?.Reconnect();
        SpeechInputController.Instance?.SetListening(microphoneEnabled!=0);
        report.finishedUtc=DateTime.UtcNow.ToString("O");report.status=report.errors.Count==0?"Passed":"Failed";Save();
        if(!Application.isEditor)Application.Quit(report.errors.Count==0?0:1);
    }
}

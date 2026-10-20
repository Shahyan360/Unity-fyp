using PlayerPrefs = LumenPreferences;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit acceptance run only; does not record the human microphone.</summary>
public sealed class LumenImageVerification : MonoBehaviour
{
    [Serializable] private sealed class Report
    {
        public string status="Running",startedUtc,finishedUtc,buildId;
        public List<string> checks=new List<string>(),errors=new List<string>(),answers=new List<string>(),images=new List<string>();
        public List<float> firstAudioSeconds=new List<float>();
    }
    private Report report;
    private string directory,oldScenario;
    private int oldMicrophone;
    private int oldOffline,oldVisuals;
    private string Backend=>FindAnyObjectByType<AIClient>()?.BackendUrl??"http://127.0.0.1:5000";
    private bool finished;
    private float deadline;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Requested()
    {
        string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-lumen-images-dir");
        if(index>=0&&index+1<args.Length)StartRun(args[index+1]);
    }
    public static bool StartRun(string path)
    {
        if(!Application.isPlaying||!LumenPreferences.IsIsolated||FindAnyObjectByType<LumenImageVerification>()!=null)return false;
        var go=new GameObject("Live image acceptance verification");DontDestroyOnLoad(go);
        var runner=go.AddComponent<LumenImageVerification>();runner.Begin(path);return true;
    }
    private void Begin(string path)
    {
        directory=Path.GetFullPath(path);Directory.CreateDirectory(directory);if(File.Exists(Path.Combine(directory,"journey.json")))throw new InvalidOperationException("Preserve previous image evidence.");report=new Report{startedUtc=DateTime.UtcNow.ToString("O"),buildId=Application.isEditor?"EDITOR_SOURCE_REVIEW":Application.buildGUID};
        oldScenario=PlayerPrefs.GetString(ScenarioRuntimeManager.SelectedScenarioKey,"");
        oldMicrophone=PlayerPrefs.GetInt("Lumen.VoiceEnabled",0);PlayerPrefs.SetInt("Lumen.VoiceEnabled",0);SpeechInputController.Instance?.SetListening(false);
        oldOffline=PlayerPrefs.GetInt("Lumen.Offline",0);oldVisuals=PlayerPrefs.GetInt("Lumen.VisualAids",1);PlayerPrefs.SetInt("Lumen.Offline",0);PlayerPrefs.SetInt("Lumen.VisualAids",1);GeminiLiveConversation.Instance?.Reconnect();
        Application.runInBackground=true;deadline=Time.realtimeSinceStartup+480;StartCoroutine(Journey());
    }
    private void Check(bool value,string text){report.checks.Add((value?"PASS ":"FAIL ")+text);if(!value)report.errors.Add(text);Save();}
    private void Save(){File.WriteAllText(Path.Combine(directory,"journey.json"),JsonUtility.ToJson(report,true));}
    private void Update(){if(report!=null&&!finished&&Time.realtimeSinceStartup>deadline){Check(false,"Acceptance deadline");Finish();}}
    private IEnumerator Ready()
    {
        float until=Time.realtimeSinceStartup+20;
        while(Time.realtimeSinceStartup<until&&(GeminiLiveConversation.Instance==null||!GeminiLiveConversation.Instance.Ready))yield return null;
        Check(GeminiLiveConversation.Instance!=null&&GeminiLiveConversation.Instance.Ready,"Gemini ready in "+SceneManager.GetActiveScene().name);
    }
    private IEnumerator Load(string scenario,string scene)
    {
        if(scenario=="home")ScenarioTransitionService.LoadHome();else ScenarioTransitionService.LoadScenario(scenario);
        float until=Time.realtimeSinceStartup+18;while(SceneManager.GetActiveScene().name!=scene&&Time.realtimeSinceStartup<until)yield return null;
        yield return new WaitForSecondsRealtime(3);
        string variant=ScenarioRuntimeManager.GetScenarioForScene(SceneManager.GetActiveScene().name);bool valid=SceneManager.GetActiveScene().name==scene&&variant==(scenario=="home"?"general_assistant":scenario);
        Check(valid,"Actual scene/variant="+SceneManager.GetActiveScene().name+"/"+variant+" expected="+scene+"/"+scenario);if(!valid){Finish();yield break;}yield return Ready();
    }
    private IEnumerator Ask(string question,string expected,string screenshot,bool expectImage=true)
    {
        var live=GeminiLiveConversation.Instance;if(live==null||!live.Ready){Check(false,"Not ready: "+question);yield break;}
        bool done=false;string answer="",error="";int images=live.Images.ImagesShown;
        live.SendText(question,r=>{answer=r.response;done=true;},_=>{},e=>{error=e;done=true;});
        float until=Time.realtimeSinceStartup+45;
        while(Time.realtimeSinceStartup<until&&(!done||live.IsSpeaking||live.BufferedSeconds>.01f))yield return null;
        Check(done&&answer.Length>0&&error.Length==0,"Spoken reply: "+question+" "+error);
        report.answers.Add(answer);report.firstAudioSeconds.Add(live.FirstAudioSeconds);
        if(!expectImage)
        {
            yield return new WaitForSecondsRealtime(1);
            Check(!live.Images.HasImage&&live.Images.ImagesShown==images&&!live.Images.RequestedForTurn,"Answer did not request or display an unsolicited or refused image");
            Save();yield return Capture(screenshot);yield break;
        }
        until=Time.realtimeSinceStartup+30;
        while(Time.realtimeSinceStartup<until&&live.Images.State!="Ready"&&live.Images.State!="Unavailable")yield return null;
        Check(live.Images.HasImage&&live.Images.ImagesShown>images,"New image displayed: "+question+" / "+live.Images.State);
        Check(live.Images.ImageTitle.IndexOf(expected,StringComparison.OrdinalIgnoreCase)>=0,"Relevant image title: "+live.Images.ImageTitle+" (expected "+expected+")");
        Check(live.Images.SourceUrl.StartsWith("https://commons.wikimedia.org/",StringComparison.Ordinal),"Source credit available");
        report.images.Add(live.Images.Topic+" -> "+live.Images.ImageTitle+" / "+live.Images.SourceUrl);Save();
        yield return Capture(screenshot);
    }
    private IEnumerator Capture(string name)
    {
        yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();var pixels=image.GetPixels32();int lit=0;for(int i=0;i<pixels.Length;i+=97)if(pixels[i].r+pixels[i].g+pixels[i].b>24)lit++;
        Check(lit>pixels.Length/97*.04f,"Nonblank rendered capture "+name);string scene=SceneManager.GetActiveScene().name;
        File.WriteAllBytes(Path.Combine(directory,name+"__"+scene+"__"+ScenarioRuntimeManager.GetScenarioForScene(scene)+"__"+report.buildId+".png"),image.EncodeToPNG());Destroy(image);
    }
    private IEnumerator SpokenQuestion()
    {
        string fixture=Path.Combine(Application.streamingAssetsPath,"Verification","transformer-question-16k.wav");
        if(!File.Exists(fixture)){Check(false,"Synthetic spoken question fixture available");yield break;}
        var live=GeminiLiveConversation.Instance;int before=live.Replies,images=live.Images.ImagesShown;
        byte[] wav=File.ReadAllBytes(fixture);var frame=new float[320];
        for(int offset=44;offset<wav.Length+32000;offset+=640)
        {
            for(int i=0;i<frame.Length;i++){int k=offset+i*2;frame[i]=k+1<wav.Length?(short)(wav[k]|(wav[k+1]<<8))/32768f:0;}
            live.SendMicrophone(frame);yield return new WaitForSecondsRealtime(.02f);
        }
        float until=Time.realtimeSinceStartup+40;
        while(Time.realtimeSinceStartup<until&&(live.Replies==before||live.IsSpeaking||live.BufferedSeconds>.01f))yield return null;
        Check(live.Replies>before&&live.LastHeard.IndexOf("transformer",StringComparison.OrdinalIgnoreCase)>=0,"Spoken question recognized and answered through native audio");
        until=Time.realtimeSinceStartup+30;
        while(Time.realtimeSinceStartup<until&&live.Images.State!="Ready"&&live.Images.State!="Unavailable")yield return null;
        Check(live.Images.HasImage&&live.Images.ImagesShown>images&&live.Images.ImageTitle=="Transformer","Spoken question automatically displays electrical transformer image");
        report.answers.Add(live.LastReply);report.images.Add("Spoken: "+live.LastHeard+" -> "+live.Images.ImageTitle);Save();
        yield return Capture("04b-spoken-transformer");
    }
    private IEnumerator Journey()
    {
        yield return Load("home","HomeScene");
        yield return Ask("What is an electrical transformer? Explain it simply.","Transformer","01-electrical-transformer");
        yield return Ask("I mean the transformer architecture used in artificial intelligence instead. How does that work?","Transformer","02-ai-transformer");
        Check(GeminiLiveConversation.Instance.Images.ImageTitle.Contains("learning"),"Changed meaning to AI architecture, not electrical hardware");
        GeminiLiveConversation.Instance.Images.SetExpanded(true);yield return Capture("03-expanded-diagram");GeminiLiveConversation.Instance.Images.SetExpanded(false);
        yield return Ask("What is the Eiffel Tower?","Eiffel Tower","04-eiffel");
        yield return SpokenQuestion();
        yield return Load("classroom","Classroom");
        yield return Ask("Show me the DNA model and explain DNA simply.","DNA","05-dna-model-and-image");
        Check(GameObject.Find("ActiveLearningModel_DNA")!=null,"Existing interactive DNA model still displays");
        yield return Ask("What is photosynthesis?","Photosynthesis","06-photosynthesis");
        yield return Ask("No pictures. Explain DNA in words only.","","06b-classroom-refused-image",false);
        yield return Load("ancient_rome","HistoricalPlace");
        yield return Ask("Show me a picture of the Colosseum and explain what it was used for.","Colosseum","07-rome");
        yield return Load("museum","Classroom");
        yield return Ask("Show me a picture of a triceratops and explain what it was.","Triceratops","08-museum");
        yield return Load("cafe","Cafestore");
        var live=GeminiLiveConversation.Instance;float until=Time.realtimeSinceStartup+15;
        while(Time.realtimeSinceStartup<until&&(live.Replies==0||live.IsSpeaking||live.BufferedSeconds>.01f))yield return null;
        Check(!ConversationImagePanel.RequestsVisual("Do people enjoy photos?")&&ConversationImagePanel.RequestsVisual("Could I see a picture of Rome?")&&!ConversationImagePanel.RequestsVisual("Don't show any pictures."),"Cafe visual intent distinguishes requests from discussion and refusal");
        yield return Ask("What is active listening, and how can it help me talk to someone here?","","09-cafe-no-auto-image",false);
        yield return Ask("Show me a picture illustrating active listening, please.","listening","09b-cafe-requested-image");
        yield return Ask("Don't show any more pictures. How can I greet someone?","","09c-cafe-no-more-images",false);
        live.Images.RequestImage("", Backend);
        Check(!live.Images.HasImage&&live.Images.State=="Unavailable","No stale image after an invalid request");
        yield return Capture("10-honest-unavailable");
        live.Images.BeginQuestion("Show me a picture of DNA.",false);
        live.Images.RequestImage("Eiffel Tower",Backend);
        live.Images.RequestImage("DNA",Backend);until=Time.realtimeSinceStartup+30;
        while(live.Images.State!="Ready"&&live.Images.State!="Unavailable"&&Time.realtimeSinceStartup<until)yield return null;
        Check(live.Images.HasImage&&live.Images.ImageTitle=="DNA","Superseded download cannot replace the newest topic");
        live.Images.RequestImage("Eiffel Tower",Backend);until=Time.realtimeSinceStartup+20;
        while(live.Images.State!="Ready"&&live.Images.State!="Unavailable"&&Time.realtimeSinceStartup<until)yield return null;
        live.Images.BeginQuestion("What is DNA?");live.Images.CompleteAnswer("DNA stores genetic information.",Backend);until=Time.realtimeSinceStartup+20;
        while(live.Images.State!="Ready"&&live.Images.State!="Unavailable"&&Time.realtimeSinceStartup<until)yield return null;
        Check(live.Images.HasImage&&live.Images.Topic=="DNA"&&live.Images.ImageTitle=="DNA","Missed tool fallback honors a short NEW topic instead of reusing Eiffel Tower");
        yield return Load("home","HomeScene");
        Check(!GeminiLiveConversation.Instance.Images.HasImage,"Scene transition clears previous conversation image");
        Finish();
    }
    private void Finish()
    {
        if(finished)return;finished=true;report.status=report.errors.Count==0?"Passed":"Failed";report.finishedUtc=DateTime.UtcNow.ToString("O");Save();
        PlayerPrefs.SetInt("Lumen.VoiceEnabled",oldMicrophone);PlayerPrefs.SetString(ScenarioRuntimeManager.SelectedScenarioKey,oldScenario);PlayerPrefs.Save();SpeechInputController.Instance?.SetListening(oldMicrophone!=0);
        PlayerPrefs.SetInt("Lumen.Offline",oldOffline);PlayerPrefs.SetInt("Lumen.VisualAids",oldVisuals);PlayerPrefs.Save();GeminiLiveConversation.Instance?.Reconnect();
        if(!Application.isEditor)Application.Quit(report.errors.Count==0?0:2);
    }
}

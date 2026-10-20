using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit bounded ten-minute native-provider walkthrough. Physical microphone remains off.</summary>
public sealed class LumenLiveWalkVerification:MonoBehaviour
{
    [Serializable] private sealed class Report
    {
        public string status="Running",build,startedUtc,finishedUtc;
        public double elapsedSeconds;
        public int questions;
        public List<string> checks=new List<string>(),errors=new List<string>(),answers=new List<string>(),actions=new List<string>();
        public List<float> firstAudioSeconds=new List<float>();
    }
    private Report report;private string output;private bool finished;private double started;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Requested()
    {
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-lumen-live-walk-dir");
        if(i<0||i+1>=args.Length||Application.isEditor)return;
        if(!LumenPreferences.IsIsolated)return;var go=new GameObject("Bounded ten minute live acceptance");DontDestroyOnLoad(go);go.AddComponent<LumenLiveWalkVerification>().Begin(args[i+1]);
    }
    private void Begin(string path)
    {
        output=Path.GetFullPath(path);Directory.CreateDirectory(output);if(File.Exists(Path.Combine(output,"live-walk.json")))throw new InvalidOperationException("Preserve previous live output.");
        report=new Report{build=Application.buildGUID,startedUtc=DateTime.UtcNow.ToString("O")};started=Time.realtimeSinceStartupAsDouble;
        LumenPreferences.SetInt("Lumen.Offline",0);LumenPreferences.SetInt("Lumen.VoiceEnabled",0);LumenPreferences.SetInt("Lumen.VoiceChoiceSeen",1);LumenPreferences.SetInt("Lumen.VisualAids",0);LumenPreferences.SetInt("Lumen.PresentationQuality",1);LumenPreferences.Save();
        SpeechInputController.Instance?.SetListening(false);GeminiLiveConversation.Instance?.Reconnect();Application.runInBackground=true;Application.logMessageReceived+=Log;StartCoroutine(Journey());
    }
    private void Check(bool ok,string text){report.checks.Add((ok?"PASS ":"FAIL ")+text);if(!ok)report.errors.Add(text);Save();}
    private void Save(){report.elapsedSeconds=Time.realtimeSinceStartupAsDouble-started;File.WriteAllText(Path.Combine(output,"live-walk.json"),JsonUtility.ToJson(report,true));}
    private void Log(string message,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&report.errors.Count<100){report.errors.Add(SceneManager.GetActiveScene().name+": "+message);Save();}}
    private void Update(){if(!finished&&report!=null&&Time.realtimeSinceStartupAsDouble-started>900){Check(false,"Fifteen minute live limit exceeded");Finish();}}
    private IEnumerator Ready()
    {
        float until=Time.realtimeSinceStartup+20;while(GeminiLiveConversation.Instance?.Ready!=true&&Time.realtimeSinceStartup<until)yield return null;
        Check(GeminiLiveConversation.Instance?.Ready==true,"Actual live readiness in "+SceneManager.GetActiveScene().name);
    }
    private IEnumerator Ask(string question,string contains="")
    {
        if(++report.questions>14){Check(false,"Question budget exceeded");Finish();yield break;}
        var live=GeminiLiveConversation.Instance;if(!live.Ready){Check(false,"Cannot ask while disconnected");yield break;}
        bool done=false;string answer="",error="";float peak=0;
        live.SendText(question,r=>{answer=r.response;done=true;},s=>{},e=>{error=e;done=true;});float until=Time.realtimeSinceStartup+45;
        while(Time.realtimeSinceStartup<until&&(!done||live.IsSpeaking||live.BufferedSeconds>.01f)){peak=Mathf.Max(peak,live.OutputLevel);yield return null;}
        Check(done&&string.IsNullOrWhiteSpace(error)&&!string.IsNullOrWhiteSpace(answer),"Real reply: "+question+" / "+error);Check(peak>.005f,"Native speech output for question "+report.questions);
        if(contains!="")Check(answer.IndexOf(contains,StringComparison.OrdinalIgnoreCase)>=0,"Recall/identity contains "+contains);
        report.answers.Add(live.ActiveSpeakerName+" | "+question+" | "+answer);report.actions.Add(live.LastToolCommand+" | "+JsonUtility.ToJson(VoiceCommandRouter.LastExecution));report.firstAudioSeconds.Add(live.FirstAudioSeconds);Save();
    }
    private IEnumerator LocalWalk(string command)
    {
        var action=VoiceCommandRouter.Execute(command,"live-walk-"+Guid.NewGuid().ToString("N"));float until=Time.realtimeSinceStartup+45;
        while(!action.Terminal&&Time.realtimeSinceStartup<until)yield return null;
        Check(action.Succeeded,"Local walk "+command+" / "+action.reason);
    }
    private IEnumerator Journey()
    {
        Screen.SetResolution(1600,900,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(4);
        string[] worlds={"home","ancient_rome","classroom","museum","cafe"};
        for(int index=0;index<worlds.Length&&!finished;index++)
        {
            double segment=Time.realtimeSinceStartupAsDouble;var travel=ScenarioTransitionService.RequestTravel(worlds[index]);float until=Time.realtimeSinceStartup+45;
            while(!travel.Terminal&&Time.realtimeSinceStartup<until)yield return null;yield return new WaitForSecondsRealtime(5);
            Check(travel.Succeeded,"Actual world "+worlds[index]+" / "+SceneManager.GetActiveScene().name);yield return Ready();
            if(index==0)
            {
                yield return Ask("Please remember that my name is Robin and I enjoy astronomy. Briefly introduce yourself.","Kai");
                GeminiLiveConversation.Instance.Reconnect();yield return Ready();yield return Ask("What is my name, and what topic did I say I enjoy?","Robin");
                var helper=FindAnyObjectByType<ScenarioCharacterIdentity>();var guide=helper.GetComponent<WalkableGuideFollower>();VoiceCommandRouter.Execute("follow me");Check(guide!=null&&guide.IsFollowing,"Home follower active during live walkthrough");
            }
            else if(index==1)
            {
                yield return Ask("Take me to the market.");Check(VoiceCommandRouter.LastExecution?.Succeeded==true&&VoiceCommandRouter.LastExecution.target.Contains("market"),"Live market tool arrived");
                yield return Ask("Take me inside the Colosseum to watch the fight.");Check(VoiceCommandRouter.LastExecution?.Succeeded==true&&VoiceCommandRouter.LastExecution.target.Contains("arena"),"Live arena tool arrived");
            }
            else if(index==2)
            {
                yield return Ask("Show me an atom model, without images.");Check(ClassroomLearningExhibit.ActiveModel=="Atom","Live atom actually displayed");
                yield return Ask("Now show me DNA, still without images.");Check(ClassroomLearningExhibit.ActiveModel=="DNA","Live DNA actually displayed");
                var model=LearningModelInspection.Current;model?.Enter();model?.Rotate();model?.Zoom();yield return new WaitForSecondsRealtime(8);model?.ResetView();model?.Exit();
            }
            else if(index==3)
            {
                yield return Ask("Are these museum specimens originals or interpretive replicas? Please distinguish what you know.");
                yield return Ask("Do not take me to the cafe. Explain the difference between an ancient Roman food stall and the modern cafe world.");
                Check(ScenarioRuntimeManager.GetScenarioForScene(SceneManager.GetActiveScene().name)=="museum","Negative world request did not travel");
            }
            else
            {
                yield return Ask("Are you the barista or my practice companion?","companion");
                yield return Ask("I want to start a conversation with a guest. Suggest one small opening without pictures.");
            }
            while(Time.realtimeSinceStartupAsDouble-segment<120&&!finished)
            {
                var player=FindAnyObjectByType<CafeWalkController>();var speaker=GeminiLiveConversation.Instance.ActiveSpeakerTransform;if(player!=null&&speaker!=null)player.FaceTarget(speaker);
                yield return new WaitForSecondsRealtime(5);Save();
            }
            yield return new WaitForEndOfFrame();var capture=ScreenCapture.CaptureScreenshotAsTexture();if(capture!=null){File.WriteAllBytes(Path.Combine(output,worlds[index]+"__"+SceneManager.GetActiveScene().name+"__"+report.build+".png"),capture.EncodeToPNG());Destroy(capture);}
        }
        Check(Time.realtimeSinceStartupAsDouble-started>=600,"At least ten minutes of actual live workload");Finish();
    }
    private void Finish()
    {
        if(finished)return;finished=true;Application.logMessageReceived-=Log;LumenPreferences.SetInt("Lumen.Offline",1);GeminiLiveConversation.Instance?.Reconnect();
        report.finishedUtc=DateTime.UtcNow.ToString("O");report.status=report.errors.Count==0?"Passed; performance analysis separate":"Failed";Save();Application.Quit(report.errors.Count==0?0:1);
    }
}

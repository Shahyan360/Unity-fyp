using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Opt-in isolated acceptance of routes, exhibits, rehearsal, orders and responsive UI.</summary>
public sealed class LumenFunctionalVerification:MonoBehaviour
{
    [Serializable] private sealed class Report
    {
        public string status="Running",build,startedUtc,finishedUtc;
        public List<string> checks=new List<string>(),errors=new List<string>(),overflow=new List<string>();
    }
    private Report report;private string output;private bool finished;private float deadline;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Requested()
    {
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-lumen-functional-dir");
        if(i>=0&&i+1<args.Length)StartRun(args[i+1]);
    }
    public static bool StartRun(string path)
    {
        if(FindAnyObjectByType<LumenFunctionalVerification>()!=null||!Application.isPlaying||!LumenPreferences.IsIsolated)return false;
        var go=new GameObject("Isolated functional acceptance");DontDestroyOnLoad(go);go.AddComponent<LumenFunctionalVerification>().Begin(path);return true;
    }
    private void Begin(string path)
    {
        output=Path.GetFullPath(path);Directory.CreateDirectory(output);
        if(File.Exists(Path.Combine(output,"functional.json")))throw new InvalidOperationException("Preserve existing acceptance output.");
        report=new Report{build=Application.isEditor?"EDITOR_SOURCE_REVIEW":Application.buildGUID,startedUtc=DateTime.UtcNow.ToString("O")};deadline=Time.realtimeSinceStartup+900;
        LumenPreferences.SetInt("Lumen.Offline",1);LumenPreferences.SetInt("Lumen.VoiceEnabled",0);LumenPreferences.SetInt("Lumen.VoiceChoiceSeen",1);LumenPreferences.SetInt("Lumen.TextTier",0);LumenPreferences.Save();
        SpeechInputController.Instance?.SetListening(false);GeminiLiveConversation.Instance?.Reconnect();Application.runInBackground=true;
        Application.logMessageReceived+=Log;Check(LumenPreferences.IsIsolated,"Preferences are isolated");StartCoroutine(Run());
    }
    private void Check(bool ok,string text){report.checks.Add((ok?"PASS ":"FAIL ")+text);if(!ok)report.errors.Add(text);Save();}
    private void Save()=>File.WriteAllText(Path.Combine(output,"functional.json"),JsonUtility.ToJson(report,true));
    private void Log(string text,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&report.errors.Count<100){report.errors.Add(SceneManager.GetActiveScene().name+": "+text);Save();}}
    private void Update(){if(!finished&&report!=null&&Time.realtimeSinceStartup>deadline){Check(false,"Fifteen minute acceptance limit");Finish();}}
    private IEnumerator Load(string world)
    {
        var action=ScenarioTransitionService.RequestTravel(world);float until=Time.realtimeSinceStartup+45;
        while(!action.Terminal&&Time.realtimeSinceStartup<until)yield return null;
        yield return new WaitForSecondsRealtime(3);
        string variant=ScenarioRuntimeManager.GetScenarioForScene(SceneManager.GetActiveScene().name);
        Check(action.Succeeded&&variant==(world=="home"?"general_assistant":world),"Actual world "+world+" = "+SceneManager.GetActiveScene().name+"/"+variant);
        if(!action.Succeeded){Finish();yield break;}
    }
    private IEnumerator Walk(string command,string target)
    {
        var action=VoiceCommandRouter.Execute(command,"functional-"+Guid.NewGuid().ToString("N"));float until=Time.realtimeSinceStartup+70;
        while(!action.Terminal&&Time.realtimeSinceStartup<until)yield return null;
        Check(action.Succeeded,target+" actual arrival / "+JsonUtility.ToJson(action));
    }
    private IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(3);yield return Load("home");if(finished)yield break;
        VoiceCommandRouter.Execute("stop");var follower=FindAnyObjectByType<WalkableGuideFollower>();Check(follower!=null&&!follower.IsFollowing,"Home guide stop controls actual follower");
        VoiceCommandRouter.Execute("follow me");Check(follower!=null&&follower.IsFollowing,"Home guide follow controls actual follower");
        Check(!VoiceCommandRouter.Execute("Do not take me to the market").handled,"Negated movement does not execute");
        yield return Load("ancient_rome");if(finished)yield break;
        Check(WorldStateContext.Snapshot().Contains("Roman market stall"),"Provider context includes actual market destinations");
        yield return Walk("take me to market","Market");yield return Walk("take me to arena viewing","Arena terrace");
        var walk=VoiceCommandRouter.Execute("move me backward ten meters");VoiceCommandRouter.Execute("stop");yield return null;Check(!FindAnyObjectByType<VoicePlayerNavigation>().IsWalking,"Stop cancels actual visitor motor");
        yield return Load("classroom");if(finished)yield break;ExplorationNotebook.Reset();
        string[] models={"Atom","Solar System","Mercury","Venus","Earth","Mars","Jupiter","Saturn","Uranus","Neptune","Plant Cell","Animal Cell","DNA","Human Heart","Water Cycle","Electric Circuit","Water Molecule","Volcano","Pythagorean Theorem","Plant","Animal Anatomy","Sunflower","Oak Tree","Light Spectrum","Periodic Table","Triceratops","Fern","Apollo 11"};
        foreach(string name in models)
        {
            bool shown=ClassroomLearningExhibit.ShowModel(name);yield return new WaitForSecondsRealtime(.5f);
            Check(shown&&ClassroomLearningExhibit.ActiveModel==name&&ClassroomLearningExhibit.HasActiveModel,name+" actual exhibit and lesson context");
            Check(ExplorationNotebook.Summary().Split('\n').Count(x=>x.TrimStart('•',' ')==name)==1,name+" one canonical discovery");
            var inspect=LearningModelInspection.Current;var camera=Camera.main.transform;Vector3 p=camera.localPosition;Quaternion q=camera.localRotation;
            inspect.Enter();inspect.Rotate();for(int i=0;i<12;i++)inspect.Zoom();inspect.ResetView();inspect.Exit();
            Check(!inspect.IsInspecting&&Vector3.Distance(camera.localPosition,p)<.001f&&Quaternion.Angle(camera.localRotation,q)<.01f,name+" inspection restores camera");
        }
        string retained=ClassroomLearningExhibit.ActiveModel;Check(!ClassroomLearningExhibit.ShowModel("Eiffel Tower")&&ClassroomLearningExhibit.ActiveModel==retained,"Unsupported model preserves previous valid exhibit");
        ClassroomLearningExhibit.ShowModel("DNA");ClassroomLearningExhibit.ShowModel("DNA");yield return null;Check(ExplorationNotebook.Summary().Split('\n').Count(x=>x.TrimStart('•',' ')=="DNA")==1,"Repeated model display does not duplicate notebook");
        yield return Load("cafe");if(finished)yield break;
        var order=FindAnyObjectByType<CafeOrderExperience>();order.Show();
        GameObject.Find("MenuItem_0/Plus")?.GetComponent<Button>().onClick.Invoke();
        // Find by the actual nested row, rather than an ambiguous repeated Plus button.
        var row=GameObject.Find("MenuItem_0");row.transform.Find("Plus").GetComponent<Button>().onClick.Invoke();GameObject.Find("PlaceOrder").GetComponent<Button>().onClick.Invoke();
        Check(order.OrderState.StartsWith("Preparing"),"Order enters preparing");order.CancelOrder();yield return new WaitForSecondsRealtime(9);
        Check(order.OrderState=="No pending order","Cancelled preparation cannot become ready later");
        row.transform.Find("Plus").GetComponent<Button>().onClick.Invoke();GameObject.Find("PlaceOrder").GetComponent<Button>().onClick.Invoke();yield return new WaitForSecondsRealtime(9);
        Check(order.OrderState.StartsWith("Ready"),"Restarted order reaches ready");GameObject.Find("PlaceOrder").GetComponent<Button>().onClick.Invoke();Check(order.OrderState=="No pending order","Order collection completes cleanly");order.Hide();
        var coach=FindAnyObjectByType<SocialCoachingController>();coach.StartPractice("conversation");Check(coach.State=="Unavailable"&&!coach.IsActive,"Offline practice explains connection requirement");
        var live=GeminiLiveConversation.Instance;live.enabled=false;LumenPreferences.SetInt("Lumen.Offline",0);
        foreach(string mode in new[]{"conversation","interview","presentation"})
        {
            coach.StartPractice(mode);float until=Time.realtimeSinceStartup+15;while(coach.State=="Starting"&&Time.realtimeSinceStartup<until)yield return null;
            Check(coach.IsActive&&coach.ActiveMode==mode,mode+" starts real backend session");
            for(int i=0;i<3&&coach.IsActive;i++)
            {coach.CompleteTurn("I would introduce myself and ask a clear question.",mode+"-"+i);until=Time.realtimeSinceStartup+15;while(coach.IsAdvancing&&Time.realtimeSinceStartup<until)yield return null;Check(coach.Turn==i+1,mode+" actual backend progression "+(i+1));}
            Check(coach.State=="Completed",mode+" completes three steps");coach.EndPractice();Check(coach.State=="Off",mode+" opt out/reset");
        }
        LumenPreferences.SetInt("Lumen.Offline",1);live.enabled=true;live.Reconnect();
        foreach(string world in new[]{"home","ancient_rome","classroom","museum","cafe"})
        {
            yield return Load(world);if(finished)yield break;
            foreach(var resolution in new[]{new Vector2Int(1024,768),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1080)})
            {
                Screen.SetResolution(resolution.x,resolution.y,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(1);
                foreach(int tier in new[]{0,2})
                {
                    LumenPreferences.SetInt("Lumen.TextTier",tier);LumenAccessibility.Refresh();yield return new WaitForSecondsRealtime(1.2f);Canvas.ForceUpdateCanvases();
                    string label=world+"-"+resolution.x+"x"+resolution.y+"-text"+tier;
                    var labels=FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Exclude);foreach(var text in labels){text.ForceMeshUpdate();if(text.isTextOverflowing)report.overflow.Add(label+" / "+text.transform.parent.name+"/"+text.name+" / "+text.text);}
                    Check(labels.All(t=>t.fontSize*t.canvas.scaleFactor>=15.9f),label+" minimum readable text");
                    yield return Capture(label);
                }
            }
        }
        LumenPreferences.SetInt("Lumen.TextTier",0);Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return Load("home");Finish();
    }
    private IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
        var pixels=image.GetPixels32();int lit=0;for(int i=0;i<pixels.Length;i+=97)if(pixels[i].r+pixels[i].g+pixels[i].b>24)lit++;
        Check(lit>pixels.Length/97*.04f,name+" nonblank actual view "+Screen.width+"x"+Screen.height);
        File.WriteAllBytes(Path.Combine(output,name+"__"+SceneManager.GetActiveScene().name+"__"+report.build+".png"),image.EncodeToPNG());Destroy(image);
    }
    private void Finish()
    {
        if(finished)return;finished=true;LumenPreferences.SetInt("Lumen.Offline",1);GeminiLiveConversation.Instance?.Reconnect();Application.logMessageReceived-=Log;
        report.finishedUtc=DateTime.UtcNow.ToString("O");report.status=report.errors.Count==0?"Behavior passed; visual and overflow review required":"Failed";Save();if(!Application.isEditor)Application.Quit(report.errors.Count==0?0:1);
    }
}

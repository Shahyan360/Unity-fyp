using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in thirty destination-and-home cycles, with identical cleanup/inventory points.</summary>
public sealed class LumenSoakVerification:MonoBehaviour
{
    [Serializable] private sealed class Inventory
    {
        public int cycle,frame,materials,meshes,textures,audioClips,persistentObjects,audioSources,playingSources,liveControllers;
        public string destination;
        public double elapsedSeconds,visitLoadSeconds,homeLoadSeconds;
        public long allocatedBytes,monoBytes,graphicsDriverBytes,workingSetBytes;
    }
    [Serializable] private sealed class Report
    {
        public string status="Running",build,unity,startedUtc,finishedUtc,cycleDefinition="One cycle: visit a destination, exercise its local content, return Home, then wait14 seconds before inventory. No forced garbage collection or explicit asset unload. Destinations rotate Rome/Classroom/Cafe/Museum. No provider or microphone use.";
        public int completedCycles;
        public List<string> checks=new List<string>(),errors=new List<string>();
        public List<Inventory> inventories=new List<Inventory>();
    }
    private Report report;private string output;private double started,loadSeconds;private bool finished;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Requested()
    {
        if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-lumen-soak-dir");
        if(i<0||i+1>=args.Length||FindAnyObjectByType<LumenSoakVerification>()!=null)return;
        var owner=new GameObject("Bounded thirty-cycle acceptance");DontDestroyOnLoad(owner);owner.AddComponent<LumenSoakVerification>().Begin(args[i+1]);
    }
    private void Begin(string directory)
    {
        output=Path.GetFullPath(directory);Directory.CreateDirectory(output);
        if(File.Exists(Path.Combine(output,"soak.json"))){Debug.LogError("Soak output already exists; preserve it and choose a new directory.");Application.Quit(2);return;}
        started=Time.realtimeSinceStartupAsDouble;report=new Report{build=Application.buildGUID,unity=Application.unityVersion,startedUtc=DateTime.UtcNow.ToString("O")};
        if(!LumenPreferences.IsIsolated){Debug.LogError("Soak requires isolated preferences.");Application.Quit(2);return;}
        LumenPreferences.SetInt("Lumen.Offline",1);LumenPreferences.SetInt("Lumen.VoiceEnabled",0);LumenPreferences.SetInt("Lumen.VoiceChoiceSeen",1);LumenPreferences.SetInt("Lumen.PresentationQuality",1);LumenPreferences.Save();
        SpeechInputController.Instance?.SetListening(false);Application.logMessageReceived+=OnLog;Application.runInBackground=true;
        Check(LumenPreferences.IsIsolated,"Acceptance preferences are isolated");Save();StartCoroutine(Journey());
    }
    private void Check(bool ok,string text){report.checks.Add((ok?"PASS ":"FAIL ")+text);if(!ok)report.errors.Add(text);}
    private void OnLog(string message,string stack,LogType type)
    {if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&report.errors.Count<100)report.errors.Add(SceneManager.GetActiveScene().name+": "+message);}
    private void Save()=>File.WriteAllText(Path.Combine(output,"soak.json"),JsonUtility.ToJson(report,true));
    private void Update(){if(!finished&&report!=null&&Time.realtimeSinceStartupAsDouble-started>2700){Check(false,"Forty-five minute bounded soak deadline exceeded");Finish();}}
    private IEnumerator Load(string scenario)
    {
        double at=Time.realtimeSinceStartupAsDouble;var action=ScenarioTransitionService.RequestTravel(scenario);
        float until=Time.realtimeSinceStartup+60;while(!action.Terminal&&Time.realtimeSinceStartup<until)yield return null;
        loadSeconds=Time.realtimeSinceStartupAsDouble-at;
        string scene=SceneManager.GetActiveScene().name,variant=ScenarioRuntimeManager.GetScenarioForScene(scene);
        string expected= scenario=="home"?"general_assistant":scenario;
        Check(action.Succeeded&&variant==expected,"Actual load "+scenario+" -> "+scene+"/"+variant+"; "+action.state+"; "+action.actionId);
        if(!action.Succeeded||variant!=expected){Finish();yield break;}
        yield return new WaitForSecondsRealtime(14);
    }
    private IEnumerator Journey()
    {
        Screen.SetResolution(1600,900,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(4);
        Check(SceneManager.GetActiveScene().name=="HomeScene","Startup is HomeScene");
        string[] destinations={"ancient_rome","classroom","cafe","museum"};
        for(int cycle=1;cycle<=30&&!finished;cycle++)
        {
            string destination=destinations[(cycle-1)%destinations.Length];yield return Load(destination);if(finished)yield break;double visit=loadSeconds;
            if(destination=="classroom")Check(ClassroomLearningExhibit.ShowModel(cycle%2==0?"DNA":"Atom"),"Requested classroom exhibit displayed in cycle"+cycle);
            if(destination=="cafe")Check(FindAnyObjectByType<CafeOrderExperience>()!=null,"Cafe ordering remains available in cycle"+cycle);
            if(destination=="museum")Check(ScenarioRuntimeManager.GetScenarioForScene("Classroom")=="museum","Museum variant retained in cycle"+cycle);
            int missing=SceneManager.GetActiveScene().GetRootGameObjects().Sum(root=>root.GetComponentsInChildren<Transform>(true).Sum(t=>t.GetComponents<Component>().Count(c=>c==null)));
            Check(missing==0,"No missing components in "+destination+" cycle"+cycle);
            yield return new WaitForSecondsRealtime(5);
            yield return Load("home");if(finished)yield break;double home=loadSeconds;
            yield return new WaitForSecondsRealtime(14);
            var process=System.Diagnostics.Process.GetCurrentProcess();process.Refresh();
            report.inventories.Add(new Inventory{cycle=cycle,frame=Time.frameCount,destination=destination,elapsedSeconds=Time.realtimeSinceStartupAsDouble-started,visitLoadSeconds=visit,homeLoadSeconds=home,allocatedBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),monoBytes=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong(),graphicsDriverBytes=UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver(),workingSetBytes=process.WorkingSet64,materials=Resources.FindObjectsOfTypeAll<Material>().Length,meshes=Resources.FindObjectsOfTypeAll<Mesh>().Length,textures=Resources.FindObjectsOfTypeAll<Texture>().Length,audioClips=Resources.FindObjectsOfTypeAll<AudioClip>().Length});
            var inventory=report.inventories[report.inventories.Count-1];
            inventory.persistentObjects=Resources.FindObjectsOfTypeAll<GameObject>().Count(o=>o.scene.IsValid()&&o.scene.name=="DontDestroyOnLoad");
            var audio=FindObjectsByType<AudioSource>(FindObjectsInactive.Include);
            inventory.audioSources=audio.Length;inventory.playingSources=audio.Count(a=>a.isPlaying);
            inventory.liveControllers=FindObjectsByType<GeminiLiveConversation>(FindObjectsInactive.Include).Length;
            process.Dispose();report.completedCycles=cycle;Save();
        }
        Finish();
    }
    private void Finish()
    {
        if(finished)return;finished=true;Application.logMessageReceived-=OnLog;
        report.finishedUtc=DateTime.UtcNow.ToString("O");report.status=report.errors.Count==0&&report.completedCycles==30?"Completed; resource trend analysis required":"Failed or incomplete";Save();Application.Quit(report.errors.Count==0&&report.completedCycles==30?0:1);
    }
}

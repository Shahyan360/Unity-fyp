using PlayerPrefs = LumenPreferences;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Opt-in Windows player acceptance journey. Inactive in ordinary launches.</summary>
public sealed class LumenPlayerVerification : MonoBehaviour
{
    [Serializable] private sealed class FrameReport
    {
        public string view, scene, variant, guide, graphics, buildId, captureFile;
        public Vector3 cameraPosition, cameraEuler;
        public int width, height;
        public int frames, people, validNavigators, navigators, missingComponents, errorMaterials;
        public float averageFrameMs, maximumFrameMs;
        public float maximumJaw;
        public long allocatedBytes;
    }
    [Serializable] private sealed class RunReport
    {
        public string unity, buildId, startedUtc, completedUtc, status = "Running";
        public List<string> errors = new List<string>();
        public List<string> checks = new List<string>();
        public List<string> invalidMaterials = new List<string>();
        public List<FrameReport> views = new List<FrameReport>();
    }
    private readonly Dictionary<string,string> preferences = new Dictionary<string,string>();
    private RunReport report;
    private string output;
    private float deadline;
    private bool finished;
    private string expectedScene="HomeScene", expectedVariant="general_assistant";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartIfRequested()
    {
        if (Application.isEditor) return;
        string[] args = Environment.GetCommandLineArgs();
        int flag = Array.IndexOf(args, "-lumen-verify-dir");
        if (flag < 0 || flag + 1 >= args.Length) return;
        var go = new GameObject("Player acceptance verification");
        DontDestroyOnLoad(go);
        go.AddComponent<LumenPlayerVerification>().Begin(args[flag + 1]);
    }

    private void Begin(string directory)
    {
        output = Path.GetFullPath(directory);
        Directory.CreateDirectory(output);
        report = new RunReport { unity = Application.unityVersion, buildId = Application.buildGUID, startedUtc = DateTime.UtcNow.ToString("O") };
        foreach (string key in new[] { ScenarioRuntimeManager.SelectedScenarioKey, "Lumen.Character.HomeScene", "Lumen.Character.Classroom", "Lumen.Character.Museum", "Lumen.Character.HistoricalPlace", "Lumen.Character.Cafestore" })
            preferences[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
        Application.logMessageReceived += OnLog;
        Application.runInBackground = true;
        deadline = Time.realtimeSinceStartup + 300;
        StartCoroutine(Journey());
    }

    private void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            report.errors.Add(SceneManager.GetActiveScene().name + ": " + message + "\n" + stack);
    }
    private void Update()
    {
        if (!finished && report != null && Time.realtimeSinceStartup > deadline)
        {
            report.errors.Add("Acceptance journey exceeded 300 seconds.");
            Finish();
        }
    }
    private void Check(bool passed, string description)
    {
        report.checks.Add((passed ? "PASS " : "FAIL ") + description);
        if (!passed) report.errors.Add(description);
    }
    private IEnumerator Journey()
    {
        Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
        yield return new WaitForSecondsRealtime(4);
        Check(SceneManager.GetActiveScene().name == "HomeScene", "Startup opens HomeScene");
        yield return Capture("01-opening");
        // Exercise the real portal component, independent of decorative button text.
        var portal = FindObjectsByType<HomeHubPortal>().FirstOrDefault(p=>p.scenario=="ancient_rome");
        Check(portal!=null,"Rome portal exists");
        if(portal==null){Finish();yield break;}
        portal.Enter();
        yield return WaitScene("HistoricalPlace");
        SetView(new Vector3(0,1.8f,-28), Quaternion.identity);
        yield return Capture("02-rome-streets");
        Check(FindObjectsByType<RomanArenaPerformance>().Length == 1, "Arena performance installed");
        Check(GameObject.Find("Cassius") != null && GameObject.Find("Titus") != null, "Both arena performers spawned");
        Check(FindObjectsByType<MeshRenderer>().Any(r => r.name == "Seated Roman audience"), "Baked seated audience renders");
        SetView(new Vector3(0,4,-4), Quaternion.Euler(26,0,0));
        yield return Capture("03-arena");
        SetView(new Vector3(18,1.8f,-1), Quaternion.Euler(0,-22,0));
        yield return Capture("04-market");

        ScenarioTransitionService.LoadScenario("museum");
        yield return WaitScene("Classroom","museum");
        yield return Capture("05-museum-arrival");
        Check(GameObject.Find("Natural history wing") != null && GameObject.Find("Historical collections") != null && GameObject.Find("Painting gallery") != null, "Three connected museum wings exist");
        foreach (int number in new[] { 436535, 436105, 11417 })
        {
            Texture2D painting = Resources.Load<Texture2D>("MuseumPaintings/Met"+number);
            Check(painting != null && Math.Max(painting.width, painting.height) >= 1000, "Museum painting "+number+" high-resolution resource");
        }
        SetView(new Vector3(36,2,52), Quaternion.identity);
        yield return Capture("06-skeletons");
        SetView(new Vector3(84,1.8f,60), Quaternion.identity);
        yield return Capture("07-paintings");
        SetView(new Vector3(54,1.8f,89), Quaternion.identity);
        yield return Capture("08-history-bust");
        var guide = FindAnyObjectByType<ScenarioCharacterIdentity>();
        if (guide != null)
        {
            var nav = guide.GetComponent<NavMeshAgent>();
            if (nav != null && nav.enabled && nav.isOnNavMesh) nav.isStopped = true;
            var companion = guide.GetComponent<AgentCompanionController>();
            if (companion != null) companion.enabled = false;
            guide.transform.SetPositionAndRotation(new Vector3(84,.08f,61), Quaternion.Euler(0,180,0));
            SetView(new Vector3(84,1.3f,57.3f), Quaternion.Euler(4,0,0));
            var selector = GameObject.Find("SwitchGuideCharacter").GetComponent<Button>();
            var seen = new HashSet<string>();
            for (int i=0;i<CharacterWardrobe.Styles.Length;i++)
            {
                selector.onClick.Invoke();
                string style=CharacterWardrobe.Styles[i];
                var choice=GameObject.Find("Choose "+style)?.GetComponent<Button>();
                Check(choice!=null,"Named guide entry "+style+" exists");
                if(choice==null){Finish();yield break;}
                choice.onClick.Invoke();
                yield return new WaitForSecondsRealtime(.8f);
                seen.Add(guide.DisplayName);
                yield return Capture("09-guide-"+i);
                if(guide.DisplayName=="Kai"||guide.DisplayName=="Orion"||guide.DisplayName=="Professor Theo")
                {
                    string label=guide.DisplayName.Replace(" ","-");
                    Check(guide.GetComponentsInChildren<TailoredClothingMesh>().Length==1,label+" uses fitted native clothing");
                    Check(guide.GetComponent<CharacterFacialAnimation>().BoundShapeCount>=12,label+" retains facial channels");
                    var actor=guide.GetComponent<AgentAnimator>();
                    var visual=guide.transform.Find("LumenGuideVisual").GetComponentInChildren<Animator>();
                    actor.enabled=false;visual.Play("Armature|Walk_Loop",0,.20f);visual.Update(0);
                    yield return Capture("09-walking-"+label);
                    actor.enabled=true;actor.RefreshVisual();actor.PlayAction("point");
                    yield return Capture("09-gesture-"+label,.3f);
                    var speech=FindAnyObjectByType<SpeechOutputController>();
                    speech.SpeakAs("Hello. Come and explore with me. There is always something interesting to discover together.",guide.VoiceName,guide.DisplayName,actor,guide.SpeechRate,guide.VoicePitch);
                    float timeout=Time.realtimeSinceStartup+32;
                    while(!speech.IsSpeaking&&Time.realtimeSinceStartup<timeout)yield return null;
                    Check(speech.IsSpeaking,label+" local speech starts without a backend request ("+speech.PlaybackStage+")");
                    SetView(new Vector3(84,1.50f,59.85f),Quaternion.Euler(0,0,0));
                    yield return Capture("09-speaking-"+label);
                    Check(report.views[report.views.Count-1].maximumJaw>1,label+" mouth animates with actual audio");
                    SetView(new Vector3(84,1.3f,57.3f),Quaternion.Euler(4,0,0));
                }
            }
            Check(seen.Count == 6, "Named guide entries select six distinct appearances");
        }

        ScenarioTransitionService.LoadScenario("cafe");
        yield return WaitScene("Cafestore");
        yield return Capture("10-cafe");
        Check(FindAnyObjectByType<CafeOrderExperience>() != null, "Cafe order experience installed");
        Check(FindAnyObjectByType<ScenarioCharacterIdentity>()?.DisplayName == "Maya", "Cafe default guide is Maya");
        ScenarioTransitionService.LoadScenario("classroom");
        yield return WaitScene("Classroom","classroom");
        Check(ClassroomLearningExhibit.ShowModel("Solar System"), "Science Solar System exhibit opens");
        yield return Capture("11-science");
        ScenarioTransitionService.LoadHome();
        yield return WaitScene("HomeScene");
        yield return Capture("12-return-home");
        Finish();
    }

    private IEnumerator WaitScene(string scene,string variant=null)
    {
        expectedScene=scene;
        expectedVariant=variant??ScenarioRuntimeManager.GetScenarioForScene(scene);
        float until = Time.realtimeSinceStartup + 30;
        while (SceneManager.GetActiveScene().name != scene && Time.realtimeSinceStartup < until) yield return null;
        Check(SceneManager.GetActiveScene().name == scene, "Transition to "+scene);
        yield return new WaitForSecondsRealtime(3);
        bool valid=SceneManager.GetActiveScene().name==scene&&ScenarioRuntimeManager.GetScenarioForScene(scene)==expectedVariant;
        Check(valid,"Actual scene/variant matches "+scene+" / "+expectedVariant);
        if(!valid){Finish();StopAllCoroutines();}
    }
    private void SetView(Vector3 position, Quaternion rotation)
    {
        var player = FindAnyObjectByType<CafeWalkController>();
        if (player != null) player.enabled = false;
        if (Camera.main != null) Camera.main.transform.SetPositionAndRotation(position,rotation);
    }
    private IEnumerator Capture(string name,float duration=1.5f)
    {
        string scene=SceneManager.GetActiveScene().name;
        string variant=ScenarioRuntimeManager.GetScenarioForScene(scene);
        if(finished||scene!=expectedScene||variant!=expectedVariant)
        {
            Check(false,"Capture aborted: expected "+expectedScene+"/"+expectedVariant+", actual "+scene+"/"+variant);
            Finish();StopAllCoroutines();yield break;
        }
        var frame = new FrameReport { view=name,scene=scene,variant=variant,buildId=Application.buildGUID,
            graphics=SystemInfo.graphicsDeviceType.ToString(),width=Screen.width,height=Screen.height,
            cameraPosition=Camera.main.transform.position,cameraEuler=Camera.main.transform.eulerAngles };
        frame.captureFile=name+"__"+scene+"__"+variant+"__"+Application.buildGUID+".png";
        float until = Time.realtimeSinceStartup + duration, total=0;
        var identity=FindAnyObjectByType<ScenarioCharacterIdentity>();
        var facial=identity!=null?identity.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(s=>s.sharedMesh.blendShapeCount>0):null;
        int jaw=-1;
        if(facial!=null)for(int i=0;i<facial.sharedMesh.blendShapeCount;i++)
        {
            string shape=facial.sharedMesh.GetBlendShapeName(i).ToLowerInvariant();
            if(shape.Contains("ak_")&&shape.EndsWith("jawopen")){jaw=i;break;}
        }
        while (Time.realtimeSinceStartup < until)
        {
            yield return null;
            float ms = Time.unscaledDeltaTime*1000;
            total += ms;frame.frames++;frame.maximumFrameMs=Mathf.Max(frame.maximumFrameMs,ms);
            if(jaw>=0)frame.maximumJaw=Mathf.Max(frame.maximumJaw,facial.GetBlendShapeWeight(jaw));
        }
        frame.averageFrameMs = total/Math.Max(1,frame.frames);
        frame.guide = FindAnyObjectByType<ScenarioCharacterIdentity>()?.DisplayName;
        frame.people = FindObjectsByType<ObjectMetadata>().Count(p=>p.category=="Person");
        var agents = FindObjectsByType<NavMeshAgent>();
        frame.navigators=agents.Count(a=>a.enabled);frame.validNavigators=agents.Count(a=>a.enabled&&a.isOnNavMesh);
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach(var t in root.GetComponentsInChildren<Transform>(true)) frame.missingComponents+=t.GetComponents<Component>().Count(c=>c==null);
        foreach(var r in FindObjectsByType<Renderer>().Where(r=>r.enabled&&(r is MeshRenderer||r is SkinnedMeshRenderer)))
            foreach(var material in r.sharedMaterials)
                if(material==null||material.shader==null||material.shader.name=="Hidden/InternalErrorShader")
                {
                    frame.errorMaterials++;
                    report.invalidMaterials.Add(name+": "+r.name+" / "+(material!=null?material.name:"null material"));
                }
        frame.allocatedBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        report.views.Add(frame);
        Check(frame.missingComponents==0,name+" has no missing components");
        Check(frame.errorMaterials==0,name+" has no missing/error material shaders");
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(output,frame.captureFile));
        yield return new WaitForEndOfFrame();yield return null;
        File.WriteAllText(Path.Combine(output,"journey.json"),JsonUtility.ToJson(report,true));
    }
    private void Finish()
    {
        if (finished) return;finished=true;
        foreach(var pair in preferences)
        {
            if(pair.Value==null)PlayerPrefs.DeleteKey(pair.Key);else PlayerPrefs.SetString(pair.Key,pair.Value);
        }
        PlayerPrefs.Save();Application.logMessageReceived-=OnLog;
        report.completedUtc=DateTime.UtcNow.ToString("O");report.status=report.errors.Count==0?"Passed":"Failed";
        File.WriteAllText(Path.Combine(output,"journey.json"),JsonUtility.ToJson(report,true));
        Debug.Log("Lumen player acceptance journey: "+report.status+" at "+output);
        Application.Quit(report.errors.Count==0?0:1);
    }
}

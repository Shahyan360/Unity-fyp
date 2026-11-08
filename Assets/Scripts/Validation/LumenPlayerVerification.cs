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
    private static string EvidenceBuildId=>Application.isEditor?"EDITOR_SOURCE_REVIEW":Application.buildGUID;
    public static bool StartEditorReview(string directory)
    {
        if(!Application.isEditor||!LumenPreferences.IsIsolated||FindAnyObjectByType<LumenPlayerVerification>()!=null)return false;
        var owner=new GameObject("Isolated editor source review");DontDestroyOnLoad(owner);owner.AddComponent<LumenPlayerVerification>().Begin(directory);return true;
    }

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
        report = new RunReport { unity = Application.unityVersion, buildId = EvidenceBuildId, startedUtc = DateTime.UtcNow.ToString("O") };
        foreach (string key in new[] { ScenarioRuntimeManager.SelectedScenarioKey, "Lumen.Character.HomeScene", "Lumen.Character.Classroom", "Lumen.Character.Museum", "Lumen.Character.HistoricalPlace", "Lumen.Character.Cafestore", "Lumen.VoiceChoiceSeen", "Lumen.VisualAids" })
            preferences[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
        PlayerPrefs.SetInt("Lumen.VoiceChoiceSeen", 1);
        PlayerPrefs.SetInt("Lumen.VisualAids", 0);
        Application.logMessageReceived += OnLog;
        Application.runInBackground = true;
        deadline = Time.realtimeSinceStartup + 600;
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
        Check(GameObject.Find("EXIT APPLICATION") == null, "Opening menu has no application-exit action");
        yield return Capture("01-opening");
        var front = FindAnyObjectByType<LumenFrontPage>();
        if (front != null)
        {
            GameObject.Find("CHOOSE ENVIRONMENT")?.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(GameObject.Find("Choose environment") != null && GameObject.Find("MUSEUM") != null,
                "The destination chooser includes the museum");
            yield return Capture("01b-destinations", .35f);
            front.ShowMenu();
            GameObject.Find("WORLD MAP")?.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(GameObject.Find("World atlas") != null && GameObject.Find("Illustrated regions") != null,
                "The atlas remains accessible from the opening page");
            yield return Capture("01c-atlas", .35f);
            front.ShowMenu();
            GameObject.Find("SETTINGS")?.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(GameObject.Find("VisitSettings") != null, "Opening settings still uses the shared visit preferences");
            front.ShowMenu();
        }
        yield return Capture("01d-opening-world",.5f,true);
        // Exercise the real portal component, independent of decorative button text.
        var portal = FindObjectsByType<HomeHubPortal>().FirstOrDefault(p=>p.scenario=="ancient_rome");
        Check(portal!=null,"Rome portal exists");
        if(portal==null){Finish();yield break;}
        portal.Enter();
        yield return WaitScene("HistoricalPlace");
        SetView(new Vector3(0,1.8f,-28), Quaternion.identity);
        yield return Capture("02-rome-streets");
        yield return Capture("02b-rome-world",.5f,true);
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
        yield return Capture("05b-museum-world",.5f,true);
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
                // Exercise walking, gesture and actual speech on every named guide.
                {
                    string label=guide.DisplayName.Replace(" ","-");
                    Check(guide.GetComponentsInChildren<TailoredClothingMesh>().Length==1,label+" uses fitted native clothing");
                    Check(guide.GetComponent<CharacterFacialAnimation>().BoundShapeCount>=12,label+" retains facial channels");
                    var actor=guide.GetComponent<AgentAnimator>();
                    var visual=guide.transform.Find("LumenGuideVisual").GetComponentInChildren<Animator>();
                    actor.enabled=false;visual.Play("Armature|Walk_Loop",0,.20f);visual.Update(0);
                    yield return Capture("09-walking-"+label);
                    SetView(new Vector3(87.7f,1.3f,61),Quaternion.Euler(4,-90,0));
                    yield return Capture("09-walking-side-"+label,.35f);
                    SetView(new Vector3(84,1.3f,64.7f),Quaternion.Euler(4,180,0));
                    yield return Capture("09-walking-rear-"+label,.35f);
                    SetView(new Vector3(86.5f,1.3f,58.5f),Quaternion.Euler(4,-45,0));
                    yield return Capture("09-walking-threequarter-"+label,.35f);
                    SetView(new Vector3(84,1.3f,57.3f),Quaternion.Euler(4,0,0));
                    actor.enabled=true;actor.RefreshVisual();actor.PlayAction("point");
                    yield return Capture("09-gesture-"+label,.3f);
                    SetView(new Vector3(84,1.3f,64.7f),Quaternion.Euler(4,180,0));
                    yield return Capture("09-gesture-rear-"+label,.3f);
                    var speech=FindAnyObjectByType<SpeechOutputController>();
                    speech.SpeakAs("Peter baked a blueberry muffin. Mum packed a paper bag. Come and explore with me.",guide.VoiceName,guide.DisplayName,actor,guide.SpeechRate,guide.VoicePitch);
                    float timeout=Time.realtimeSinceStartup+32;
                    while(!speech.IsSpeaking&&Time.realtimeSinceStartup<timeout)yield return null;
                    Check(speech.IsSpeaking,label+" local speech starts without a backend request ("+speech.PlaybackStage+")");
                    SetView(new Vector3(84,1.50f,59.85f),Quaternion.Euler(0,0,0));
                    yield return Capture("09-speaking-"+label);
                    Check(report.views[report.views.Count-1].maximumJaw>1,label+" mouth animates with actual audio");
                    speech.CancelSpeech();yield return new WaitForSecondsRealtime(.6f);
                    yield return Capture("09-silence-"+label,.3f);
                    Check(report.views[report.views.Count-1].maximumJaw<1,label+" jaw settles after manual speech cancellation");
                    SetView(new Vector3(84,1.3f,57.3f),Quaternion.Euler(4,0,0));
                }
            }
            Check(seen.Count == 6, "Named guide entries select six distinct appearances");
        }

        ScenarioTransitionService.LoadScenario("cafe");
        yield return WaitScene("Cafestore");
        yield return Capture("10-cafe");
        yield return Capture("10b-cafe-world",.5f,true);
        Check(FindAnyObjectByType<CafeOrderExperience>() != null, "Cafe order experience installed");
        Check(FindAnyObjectByType<ScenarioCharacterIdentity>()?.DisplayName == "Maya", "Cafe default guide is Maya");
        yield return CheckTalkPrompt();
        ScenarioTransitionService.LoadScenario("classroom");
        yield return WaitScene("Classroom","classroom");
        Check(ClassroomLearningExhibit.ShowModel("Solar System"), "Science Solar System exhibit opens");
        yield return Capture("11-science");
        yield return Capture("11b-science-world",.5f,true);
        yield return CheckClassroomCatalog();
        ScenarioTransitionService.LoadHome();
        yield return WaitScene("HomeScene");
        yield return Capture("12-return-home");
        Finish();
    }

    private IEnumerator CheckTalkPrompt()
    {
        var view = Camera.main;
        Vector3 position = view.transform.position;
        Quaternion rotation = view.transform.rotation;
        GameObject guest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guest.transform.position = new Vector3(0f, 1100f, 0f);
        var metadata = guest.AddComponent<ObjectMetadata>();
        metadata.objectId = "submission_guest";
        metadata.displayName = "Submission guest";
        metadata.description = "A fictional café guest.";
        metadata.category = "Person";
        metadata.scenario = "cafe";
        SetView(guest.transform.position + Vector3.back * 4.6f, Quaternion.identity);
        Physics.SyncTransforms();
        yield return null;
        yield return null;
        var talk = GameObject.Find("TalkAbovePerson")?.GetComponent<UnityEngine.UI.Button>();
        Check(talk != null && talk.interactable, "Looking at a person shows a clickable overhead Talk button");
        if (talk != null)
        {
            var chat = FindAnyObjectByType<ChatUIManager>();
            ChatResponse reply = null;
            Action<ChatResponse> receive = response => reply = response;
            chat.ResponseReceived += receive;
            talk.onClick.Invoke();
            float end = Time.realtimeSinceStartup + 12f;
            while (reply == null && Time.realtimeSinceStartup < end) yield return null;
            chat.ResponseReceived -= receive;
            Check(reply != null && reply.speaker_name == metadata.GetDisplayName(), "Talk opens chat with the person being viewed");
            GeminiLiveConversation.Instance?.SelectPartner(null);
        }
        Destroy(guest);
        view.transform.SetPositionAndRotation(position, rotation);
    }

    private IEnumerator CheckClassroomCatalog()
    {
        string[] models = { "Atom", "Solar System", "Plant Cell", "Animal Cell", "DNA", "Human Heart",
            "Water Cycle", "Electric Circuit", "Water Molecule", "Volcano", "Pythagorean Theorem", "Plant",
            "Animal Anatomy", "Sunflower", "Oak Tree", "Light Spectrum", "Periodic Table", "Triceratops", "Fern",
            "Apollo 11", "Mercury", "Venus", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune", "Earth" };
        foreach (string title in models)
        {
            bool opened = ClassroomLearningExhibit.ShowModel(title);
            var model = GameObject.Find("ActiveLearningModel_" + title.Replace(' ', '_'));
            Check(opened && model != null && model.GetComponentsInChildren<MeshRenderer>().Length > 1,
                "Classroom exhibit renders geometry: " + title);
            yield return null;
        }
        var otherExhibit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        otherExhibit.transform.position = new Vector3(0f, 1200f, 0f);
        LearningModelInspection.MakeSelectable(otherExhibit);
        otherExhibit.GetComponent<LearningModelInspection>().Select();
        ClassroomLearningExhibit.ShowModel("Earth");
        yield return null;
        Check(otherExhibit != null, "Replacing the classroom model preserves a separate selected exhibit");
        Check(FindObjectsByType<LearningModelInspection>(FindObjectsInactive.Exclude)
            .Count(inspection => inspection.name == "Learning model inspection pivot") == 1,
            "Switching classroom exhibits leaves only one active model and caption");
        Destroy(otherExhibit);
        var earth = GameObject.Find("ActiveLearningModel_Earth");
        Check(earth != null && !earth.GetComponentsInChildren<Transform>().Any(part => part.name.StartsWith("Continental region")),
            "Textured NASA Earth has no decorative continent overlays");
        if (earth == null) yield break;
        var target = earth.GetComponentsInChildren<ObjectMetadata>().FirstOrDefault(facts => facts.GetDisplayName() == "Earth");
        if (target == null) { Check(false, "Earth model provides gaze metadata"); yield break; }
        var globe = target.transform.Find("Earth 3D model");
        var collider = globe != null ? globe.GetComponent<BoxCollider>() : null;
        if (collider == null) { Check(false, "Earth model provides a selectable collider"); yield break; }
        Vector3 focus = collider.bounds.center;
        SetView(focus + Vector3.back * 2.3f, Quaternion.identity);
        Physics.SyncTransforms();
        var gaze = FindAnyObjectByType<ObjectRaycaster>();
        var selected = gaze != null ? gaze.RefreshTarget() : null;
        Check(selected == target, "The visible Earth model is resolved by the gameplay raycaster (found " + (selected != null ? selected.GetDisplayName() : "none") + ")");
        yield return Capture("11c-earth", .3f, true);
        var chat = FindAnyObjectByType<ChatUIManager>();
        ChatResponse reply = null;
        Action<ChatResponse> receive = response => reply = response;
        chat.ResponseReceived += receive;
        chat.SubmitWorldPrompt("whats this");
        float end = Time.realtimeSinceStartup + 12f;
        while (reply == null && Time.realtimeSinceStartup < end) yield return null;
        chat.ResponseReceived -= receive;
        Check(reply != null && reply.response_source == "local-object" && reply.response.Contains("Earth"),
            "Typed gaze question identifies Earth through the live backend");
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
    private IEnumerator Capture(string name,float duration=1.5f,bool hideHud=false)
    {
        string scene=SceneManager.GetActiveScene().name;
        string variant=ScenarioRuntimeManager.GetScenarioForScene(scene);
        if(finished||scene!=expectedScene||variant!=expectedVariant)
        {
            Check(false,"Capture aborted: expected "+expectedScene+"/"+expectedVariant+", actual "+scene+"/"+variant);
            Finish();StopAllCoroutines();yield break;
        }
        var frame = new FrameReport { view=name,scene=scene,variant=variant,buildId=EvidenceBuildId,
            graphics=SystemInfo.graphicsDeviceType.ToString(),width=Screen.width,height=Screen.height,
            cameraPosition=Camera.main.transform.position,cameraEuler=Camera.main.transform.eulerAngles };
        frame.captureFile=name+"__"+scene+"__"+variant+"__"+EvidenceBuildId+".png";
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
        var hidden=hideHud?FindObjectsByType<Canvas>(FindObjectsInactive.Exclude).Where(c=>c.enabled&&c.renderMode!=RenderMode.WorldSpace).ToArray():new Canvas[0];
        foreach(var overlay in hidden)overlay.enabled=false;
        yield return new WaitForEndOfFrame();
        var capture = Environment.GetCommandLineArgs().Contains("-lumen-offscreen-evidence")
            ? CaptureOffscreen(Camera.main) : ScreenCapture.CaptureScreenshotAsTexture();
        if(capture==null)Check(false,"No rendered capture for "+name);
        else
        {
            var pixels=capture.GetPixels32();int lit=0;byte low=255,high=0;
            for(int i=0;i<pixels.Length;i+=97){var p=pixels[i];byte brightness=(byte)((p.r+p.g+p.b)/3);if(brightness>8)lit++;low=Math.Min(low,brightness);high=Math.Max(high,brightness);}
            Check(lit>pixels.Length/97*.04f&&high-low>20,"Rendered capture contains visible scene detail: "+name);
            File.WriteAllBytes(Path.Combine(output,frame.captureFile),capture.EncodeToPNG());Destroy(capture);
        }
        foreach(var overlay in hidden)if(overlay!=null)overlay.enabled=true;
        yield return new WaitForEndOfFrame();yield return null;
        File.WriteAllText(Path.Combine(output,"journey.json"),JsonUtility.ToJson(report,true));
    }
    private static Texture2D CaptureOffscreen(Camera camera)
    {
        var overlays = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude)
            .Where(canvas => canvas.enabled && canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            .Select(canvas => (canvas, camera: canvas.worldCamera, distance: canvas.planeDistance)).ToArray();
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
        try
        {
            camera.targetTexture = target;
            foreach (var overlay in overlays)
            {
                overlay.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                overlay.canvas.worldCamera = camera;
                overlay.canvas.planeDistance = camera.nearClipPlane + .1f;
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            texture.Apply();
            return texture;
        }
        finally
        {
            foreach (var overlay in overlays)
            {
                overlay.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                overlay.canvas.worldCamera = overlay.camera;
                overlay.canvas.planeDistance = overlay.distance;
            }
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
        }
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
        if(!Application.isEditor)Application.Quit(report.errors.Count==0?0:1);else Destroy(gameObject);
    }
}

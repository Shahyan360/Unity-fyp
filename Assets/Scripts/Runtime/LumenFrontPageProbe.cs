#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Opt-in Play Mode journey. Never installed by application startup.</summary>
public sealed class LumenFrontPageProbe : MonoBehaviour
{
    private readonly List<string> results=new List<string>();
    private string output;
    private int errors;
    private bool hadScenario;
    private string previousScenario;
    private void OnEnable()=>Application.logMessageReceived+=OnLog;
    private void OnDisable()
    {
        Application.logMessageReceived-=OnLog;
        if(hadScenario)LumenPreferences.SetString(ScenarioRuntimeManager.SelectedScenarioKey,previousScenario);
        else LumenPreferences.DeleteKey(ScenarioRuntimeManager.SelectedScenarioKey);
        LumenPreferences.Save();
    }
    private void OnLog(string condition,string trace,LogType type)
    {if(type==LogType.Error||type==LogType.Exception){errors++;results.Add("CONSOLE: "+condition);if(output!=null)Flush();}}
    public void Begin(string directory)
    {
        hadScenario=LumenPreferences.HasKey(ScenarioRuntimeManager.SelectedScenarioKey);
        previousScenario=LumenPreferences.GetString(ScenarioRuntimeManager.SelectedScenarioKey);
        output=directory;Directory.CreateDirectory(output);DontDestroyOnLoad(gameObject);StartCoroutine(Run());
    }
    private void Check(bool success,string label){results.Add((success?"PASS: ":"FAIL: ")+label);Flush();}
    private void Flush()=>File.WriteAllLines(Path.Combine(output,"journey.txt"),results.Concat(new[]{"Console errors observed: "+errors}));
    private static Button FindButton(LumenFrontPage front,string name)=>front.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.gameObject.name==name);
    private IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(5);
        var front=FindAnyObjectByType<LumenFrontPage>();
        Check(front!=null,"Front page installed in HomeScene");if(front==null)yield break;
        Check(LumenFrontPage.IsOpen&&LumenAccessibility.MenuOpen,"Front page captures movement input");
        Check(FindObjectsByType<HomeHubPortal>().Length>=3,"Existing environment portals remain present");
        Check(GameObject.Find("HomeAgent")?.GetComponentInChildren<SkinnedMeshRenderer>()!=null,"Existing human host mesh present");
        int initial=Time.frameCount;yield return new WaitForSecondsRealtime(.4f);
        Check(Time.frameCount>initial,"Player loop advances");
        yield return Capture("01-front-page");
        FindButton(front,"RESUME SESSION").onClick.Invoke();
        Check(front.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("not supported")),"Resume explains lack of full session persistence");
        FindButton(front,"CHOOSE ENVIRONMENT").onClick.Invoke();
        Check(GameObject.Find("Choose environment")!=null&&GameObject.Find("World atlas")==null,"Chooser is separate from atlas");
        yield return Capture("02-environment-chooser");
        front.ShowMenu();FindButton(front,"WORLD MAP").onClick.Invoke();
        yield return null;
        Canvas.ForceUpdateCanvases();
        var terrain=front.GetComponentInChildren<LumenAtlasTerrain>();
        var atlasRenderer=terrain!=null?terrain.GetComponent<CanvasRenderer>():null;
        var atlasMesh=atlasRenderer!=null?atlasRenderer.GetMesh():null;
        Check(atlasMesh!=null&&atlasMesh.vertexCount>0,"Atlas terrain has rendered geometry");
        for(int i=0;i<3;i++)
        {
            front.SelectDestination(i);yield return new WaitForSecondsRealtime(.4f);
            string title=new[]{"CAFÉ","CLASSROOM","ANCIENT PLACE"}[i];
            Check(front.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==title),"Atlas selects "+title);
        }
        yield return Capture("03-world-atlas");
        front.ShowMenu();FindButton(front,"ABOUT").onClick.Invoke();
        Check(front.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Environmental Navigation")),"About contains project title");
        yield return Capture("04-about");
        front.ShowMenu();FindButton(front,"SETTINGS").onClick.Invoke();
        Check(GameObject.Find("VisitSettings")!=null,"Settings reuses existing visit drawer");
        yield return Capture("05-settings");
        Check(LumenFrontPage.HandleEscape(),"Escape returns to main menu");
        Vector3 cameraMenu=Camera.main.transform.position;
        FindButton(front,"START EXPERIENCE").onClick.Invoke();yield return new WaitForSecondsRealtime(1);
        Check(!LumenFrontPage.IsOpen&&!LumenAccessibility.MenuOpen,"Start enters courtyard and releases input");
        Check(FindAnyObjectByType<CafeWalkController>().enabled,"Player movement restored");
        Vector3 visit=Camera.main.transform.position;
        front.ShowMenu();yield return new WaitForSecondsRealtime(.7f);front.StartExperience();yield return new WaitForSecondsRealtime(.7f);
        Check(Vector3.Distance(Camera.main.transform.position,visit)<.15f,"Reopening menu restores visitor camera");
        yield return Capture("06-courtyard");
        string[] ids={"cafe","classroom","ancient_rome"};string[] scenes={"Cafestore","Classroom","HistoricalPlace"};
        for(int i=0;i<3;i++)
        {
            var travel=ScenarioTransitionService.RequestTravel(ids[i]);Check(travel.accepted,"Travel accepts "+ids[i]);
            float until=Time.realtimeSinceStartup+70;
            while(SceneManager.GetActiveScene().name!=scenes[i]&&Time.realtimeSinceStartup<until)yield return null;
            Check(SceneManager.GetActiveScene().name==scenes[i],"Arrives in "+scenes[i]);
            yield return new WaitForSecondsRealtime(10);
            CapturePreview(ids[i]);yield return Capture("07-"+ids[i]);
            Check(FindAnyObjectByType<AIClient>()!=null,"AI client preserved in "+scenes[i]);
            Check(FindAnyObjectByType<AgentAnimator>()!=null,"Character animator preserved in "+scenes[i]);
        }
        UnityEditor.AssetDatabase.Refresh();
        ScenarioTransitionService.LoadHome();
        float deadline=Time.realtimeSinceStartup+60;
        while((SceneManager.GetActiveScene().name!="HomeScene"||!LumenFrontPage.IsOpen)&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(SceneManager.GetActiveScene().name=="HomeScene"&&LumenFrontPage.IsOpen,"Returning home restores front page");
        Check(errors==0,"No Console errors during journey");
        results.Add("COMPLETE");Flush();
        UnityEditor.AssetDatabase.Refresh();
        Destroy(gameObject);
    }
    private IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var image=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);
    }
    private void CapturePreview(string id)
    {
        var camera=Camera.main;if(camera==null)return;
        var old=camera.targetTexture;var active=RenderTexture.active;
        var target=RenderTexture.GetTemporary(960,540,24);
        Texture2D image=null;
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            image=new Texture2D(960,540,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();
            string directory=Path.Combine(Application.dataPath,"Resources/DestinationPreviews");Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory,id+".png"),image.EncodeToPNG());
        }
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);if(image!=null)Destroy(image);}
    }
}
#endif

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class LumenWindowsBuild
{
    public static void PrepareGuideNavigation()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        string backup="D:/LumenProject/Archive/BeforeDeferredGuideNavigation";
        Directory.CreateDirectory(backup);
        var records=new System.Collections.Generic.List<string>();
        foreach(string sceneName in new[]{"HistoricalPlace","Classroom","Cafestore"})
        {
            string path="Assets/Scenes/"+sceneName+".unity";
            string copy=backup+"/"+sceneName+".unity";
            if(!File.Exists(copy))File.Copy(path,copy);
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
            foreach(var root in scene.GetRootGameObjects())
                foreach(var nav in root.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))
                {
                    // The named scenario guide is later owned by AgentCompanionController.
                    if(nav.name!="HistoricalPlace_Navigation_Demo_Agent"&&nav.name!="Classroom_Navigation_Demo_Agent"&&nav.name!="Cafe_Follow_Agent")continue;
                    records.Add(sceneName+" / "+nav.name+" / previouslyEnabled="+nav.enabled+" / position="+nav.transform.position);
                    nav.enabled=false;
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
        }
        File.WriteAllLines(backup+"/repair-record.txt",records);
    }
    [MenuItem("Lumen/Validation/Build Windows on D")]
    public static void Build()
    {
        BuildInto("D:/LumenProject/Builds/Windows-Frontend-2026-10-17");
    }

    public static void BuildCharacterUpgrade()
    {
        BuildInto("D:/LumenProject/Builds/Windows-Characters-2026-10-18");
    }

    public static void BuildConversationUpgrade()
    {
        BuildInto("D:/LumenProject/Builds/Windows-Interaction-2026-10-19");
    }

    public static void BuildLiveImages()
    {
        var preloaded=PlayerSettings.GetPreloadedAssets();
        try { BuildInto("D:/LumenProject/Builds/Windows-LiveImages-2026-10-19"); }
        finally { PlayerSettings.SetPreloadedAssets(preloaded); AssetDatabase.SaveAssets(); }
    }

    private static void BuildInto(string output)
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before building.");
        Directory.CreateDirectory(output);
        try
        {
            var options=new BuildPlayerOptions
            {
                scenes=EditorBuildSettings.scenes.Where(x=>x.enabled).Select(x=>x.path).ToArray(),
                target=BuildTarget.StandaloneWindows64,
                locationPathName=output+"/Lumen.exe",
                options=BuildOptions.None
            };
            BuildReport report=BuildPipeline.BuildPlayer(options);
            string summary="Result: "+report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+
                "\nBytes: "+report.summary.totalSize+"\nDuration: "+report.summary.totalTime+"\nOutput: "+output;
            File.WriteAllText(output+"/build-result.txt",summary);
            File.WriteAllLines(output+"/build-messages.txt",report.steps.SelectMany(step=>step.messages)
                .Where(message=>message.type==LogType.Warning||message.type==LogType.Error||message.type==LogType.Exception)
                .Select(message=>message.type+": "+message.content));
            Debug.Log(summary);
        }
        catch(Exception error) {File.WriteAllText(output+"/build-result.txt",error.ToString());throw;}
    }
}

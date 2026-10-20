using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class LumenFinalizationBuild
{
    [InitializeOnLoadMethod]
    private static void RequestBaselineOnce()
    {
        EditorApplication.update += BuildWhenIdle;
    }
    private static void BuildWhenIdle()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying) return;
        EditorApplication.update -= BuildWhenIdle;
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        if(!File.Exists(Path.Combine(root,"Reports","IMPLEMENTATION_BRIEF.md"))) return;
        string marker=Path.Combine(root,"Evidence","baseline-build-started.txt");
        if(File.Exists(marker)) return;
        File.WriteAllText(marker,DateTime.UtcNow.ToString("O"));
        try { Baseline(); LumenCharacterTailoring.ExportReferences(); }
        catch(Exception error) { File.WriteAllText(Path.Combine(root,"Evidence","baseline-build-error.txt"),error.ToString()); Debug.LogException(error); }
    }
    public static void Baseline() => Build("Baseline");
    public static void Candidate() => Build("Candidate");
    private static void Build(string stage)
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        string output=Path.Combine(root,"Builds",stage+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(root,"Evidence","latest-build-path.txt"),output);
        var options=new BuildPlayerOptions {
            scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
            target=BuildTarget.StandaloneWindows64,locationPathName=Path.Combine(output,"Lumen.exe"),
            options=BuildOptions.DetailedBuildReport
        };
        var report=BuildPipeline.BuildPlayer(options);
        File.WriteAllText(Path.Combine(output,"build-result.txt"),
            "Stage: "+stage+"\nResult: "+report.summary.result+"\nGUID: "+report.summary.guid+
            "\nUnity: "+Application.unityVersion+"\nVersion: "+PlayerSettings.bundleVersion+
            "\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+
            "\nBytes: "+report.summary.totalSize+"\nDuration: "+report.summary.totalTime+
            "\nScenes: "+string.Join(", ",options.scenes));
        File.WriteAllLines(Path.Combine(output,"build-messages.txt"),report.steps.SelectMany(s=>s.messages)
            .Where(m=>m.type==LogType.Warning||m.type==LogType.Error||m.type==LogType.Exception).Select(m=>m.type+": "+m.content));
        File.WriteAllLines(Path.Combine(output,"asset-build-residency.tsv"),
            new[]{"asset\tpacked_bytes"}.Concat(report.packedAssets.SelectMany(p=>p.contents)
            .OrderByDescending(a=>a.packedSize).Select(a=>a.sourceAssetPath+"\t"+a.packedSize)));
        if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed; see "+output);
        Debug.Log("Lumen "+stage+" built at "+output);
    }
}

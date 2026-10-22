using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Reproducible Windows build; all outputs stay outside Assets.</summary>
public static class LumenReleaseBuild
{
    public static void Build()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for an idle Editor outside Play mode.");
        foreach(var assembly in UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Editor)
            .Where(a=>a.name=="Assembly-CSharp"||a.name=="Assembly-CSharp-Editor"))
            if(!File.Exists(assembly.outputPath)||assembly.sourceFiles.Any(p=>File.Exists(p)&&File.GetLastWriteTimeUtc(p)>File.GetLastWriteTimeUtc(assembly.outputPath)))
                throw new InvalidOperationException("Source newer than loaded Editor assembly. Refresh/recompile before building: "+assembly.name);
        if (Application.unityVersion != "6000.6.2f1")
            throw new InvalidOperationException("Use the version recorded in ProjectVersion.txt.");
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string output = Environment.GetEnvironmentVariable("LUMEN_BUILD_OUTPUT");
        if (string.IsNullOrWhiteSpace(output))
            output = Path.Combine(project, "Builds", "Windows-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        output = Path.GetFullPath(output);
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
            throw new InvalidOperationException("Choose an empty output directory; preserve previous builds.");
        Directory.CreateDirectory(output);
        var preloaded = PlayerSettings.GetPreloadedAssets();
        try
        {
            var options = new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/HomeScene.unity", "Assets/Scenes/Classroom.unity",
                    "Assets/Scenes/HistoricalPlace.unity", "Assets/Scenes/Cafestore.unity" },
                target = BuildTarget.StandaloneWindows64,
                locationPathName = Path.Combine(output, "Lumen.exe"),
                options = BuildOptions.DetailedBuildReport
            };
            var report = BuildPipeline.BuildPlayer(options);
            File.WriteAllText(Path.Combine(output, "build-result.txt"),
                $"Result: {report.summary.result}\nGUID: {report.summary.guid}\nUnity: {Application.unityVersion}\n" +
                $"Errors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\n" +
                $"Bytes: {report.summary.totalSize}\nDuration: {report.summary.totalTime}\n" +
                "Scenes: " + string.Join(", ", options.scenes));
            File.WriteAllLines(Path.Combine(output, "build-messages.txt"), report.steps.SelectMany(s => s.messages)
                .Where(m => m.type == LogType.Warning || m.type == LogType.Error || m.type == LogType.Exception)
                .Select(m => m.type + ": " + m.content));
            File.WriteAllLines(Path.Combine(output, "asset-build-residency.tsv"),
                new[] { "asset\tpacked_bytes" }.Concat(report.packedAssets.SelectMany(p => p.contents)
                    .OrderByDescending(a => a.packedSize).Select(a => a.sourceAssetPath + "\t" + a.packedSize)));
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build failed; see " + output);
        }
        finally { PlayerSettings.SetPreloadedAssets(preloaded); AssetDatabase.SaveAssets(); }
    }
}

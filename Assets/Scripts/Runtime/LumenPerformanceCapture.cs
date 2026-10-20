using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in bounded raw telemetry; missing recorder/GPU timings remain unavailable.</summary>
public sealed class LumenPerformanceCapture:MonoBehaviour
{
    private StreamWriter frames,resources;
    private ProfilerRecorder mainThread,batches,draws,gc;
    private readonly FrameTiming[] timings=new FrameTiming[1];
    private long previous;
    private double started,nextInventory,settleUntil;
    private string world="",model="";
    private int rows;
    private Func<long> managedAllocated;
    private long previousManaged;
    private int previousCollections;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Requested()
    {
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-lumen-metrics-dir");
        if(i<0||i+1>=args.Length||UnityEngine.Object.FindAnyObjectByType<LumenPerformanceCapture>()!=null)return;
        var owner=new GameObject("Bounded performance evidence");DontDestroyOnLoad(owner);owner.AddComponent<LumenPerformanceCapture>().Begin(args[i+1]);
    }
    private void Begin(string path)
    {
        Directory.CreateDirectory(path);frames=new StreamWriter(Path.Combine(path,"frames.csv"));resources=new StreamWriter(Path.Combine(path,"resources.csv"));
        mainThread=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
        var available=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(available);var descriptions=new List<string>();
        foreach(var handle in available){var description=ProfilerRecorderHandle.GetDescription(handle);if(description.Category==ProfilerCategory.Render||description.Category==ProfilerCategory.Memory)descriptions.Add(description.Category.Name+" / "+description.Name+" / "+description.Flags);}
        File.WriteAllLines(Path.Combine(path,"available-counters.txt"),descriptions);
        var allocationMethod=typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
        if(allocationMethod!=null)managedAllocated=(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),allocationMethod);
        previousManaged=managedAllocated?.Invoke()??0;previousCollections=GC.CollectionCount(0);
        File.WriteAllText(Path.Combine(path,"machine.json"),JsonUtility.ToJson(new Machine{build=Application.buildGUID,unity=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),cpu=SystemInfo.processorType,memoryMb=SystemInfo.systemMemorySize,gpuMemoryMb=SystemInfo.graphicsMemorySize,width=Screen.width,height=Screen.height,quality=WorldPresentationQuality.SelectedQuality.ToString(),mainThreadAvailable=mainThread.Valid,batchesAvailable=batches.Valid,drawCallsAvailable=draws.Valid,gcAvailable=gc.Valid,startedUtc=DateTime.UtcNow.ToString("O")},true));
        frames.WriteLine("elapsed_s,world,model,settling,focused,wall_frame_ms,unity_frame_ms,cpu_frame_ms,gpu_frame_ms,main_thread_ms,batches,draw_calls,gc_bytes,speaking,gc_gen0_collection_delta,managed_main_thread_allocated_delta,width,height,quality");
        resources.WriteLine("elapsed_s,world,settling,total_allocated_bytes,mono_used_bytes,graphics_driver_bytes,materials,meshes,textures,audio_clips");
        started=Time.realtimeSinceStartupAsDouble;previous=Stopwatch.GetTimestamp();settleUntil=started+10;
    }
    [Serializable] private sealed class Machine {public string build,unity,gpu,api,cpu,quality,startedUtc;public int memoryMb,gpuMemoryMb,width,height;public bool mainThreadAvailable,batchesAvailable,drawCallsAvailable,gcAvailable;}
    private static string Number(double value)=>value.ToString("F4",CultureInfo.InvariantCulture);
    private void Update()
    {
        if(frames==null)return;double now=Time.realtimeSinceStartupAsDouble;double elapsed=now-started;
        if(elapsed>3600){Close();enabled=false;return;}
        long tick=Stopwatch.GetTimestamp();double wall=(tick-previous)*1000.0/Stopwatch.Frequency;previous=tick;
        string nextWorld=SceneManager.GetActiveScene().name+"/"+ScenarioRuntimeManager.GetScenarioForScene(SceneManager.GetActiveScene().name);
        string nextModel=ClassroomLearningExhibit.HasActiveModel?ClassroomLearningExhibit.ActiveModel:"none";
        if(nextWorld!=world){world=nextWorld;settleUntil=now+10;}
        if(nextModel!=model){model=nextModel;settleUntil=Math.Max(settleUntil,now+3);}
        bool settling=now<settleUntil||WorldStateContext.LastTransition?.state=="loading";
        FrameTimingManager.CaptureFrameTimings();uint count=FrameTimingManager.GetLatestTimings(1,timings);
        double cpu=count>0&&timings[0].cpuFrameTime>0?timings[0].cpuFrameTime:-1,gpu=count>0&&timings[0].gpuFrameTime>0?timings[0].gpuFrameTime:-1;
        bool speaking=GeminiLiveConversation.Instance?.IsSpeaking==true||UnityEngine.Object.FindAnyObjectByType<SpeechOutputController>()?.IsSpeaking==true;
        int collections=GC.CollectionCount(0);long allocated=managedAllocated?.Invoke()??-1;long allocationDelta=allocated<0?-1:Math.Max(0,allocated-previousManaged);
        frames.WriteLine(string.Join(",",Number(elapsed),world,model,settling?1:0,Application.isFocused?1:0,Number(wall),Number(Time.unscaledDeltaTime*1000),Number(cpu),Number(gpu),mainThread.Valid?Number(mainThread.LastValue/1000000.0):"-1",batches.Valid?batches.LastValue:-1,draws.Valid?draws.LastValue:-1,gc.Valid?gc.LastValue:-1,speaking?1:0,collections-previousCollections,allocationDelta,Screen.width,Screen.height,WorldPresentationQuality.SelectedQuality));
        previousCollections=collections;previousManaged=allocated;
        if(now>=nextInventory)
        {
            nextInventory=now+5;
            resources.WriteLine(string.Join(",",Number(elapsed),world,settling?1:0,UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong(),UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver(),Resources.FindObjectsOfTypeAll<Material>().Length,Resources.FindObjectsOfTypeAll<Mesh>().Length,Resources.FindObjectsOfTypeAll<Texture>().Length,Resources.FindObjectsOfTypeAll<AudioClip>().Length));
            // The following frame includes inventory instrumentation; exclude that frame from steady-state summaries.
            settleUntil=Math.Max(settleUntil,now+.10);
        }
        if(++rows%120==0){frames.Flush();resources.Flush();}
    }
    private void Close(){frames?.Dispose();frames=null;resources?.Dispose();resources=null;mainThread.Dispose();batches.Dispose();draws.Dispose();gc.Dispose();}
    private void OnDestroy()=>Close();
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Editor-only observation of actual Play Mode frames; does not change scene assets.</summary>
public static class LumenRuntimeVerification
{
    [Serializable] private sealed class Report
    {
        public string scene;
        public int frames, speakingFrames, validNavFrames;
        public float minimumJaw = 100f, maximumJaw, maximumBlink, maximumSpeed, minimumFeetGap = 100f, maximumFeetGap = -100f;
        public Vector3 initialPosition, finalPosition;
        public string character;
        public string[] blendShapes;
        public float audioLength;
        public float maximumOtherGuideJaw;
        public float maximumSpeakingSpeed;
        public string animatorState;
    }
    private static Report report;
    private static float until;
    private static int lastFrame;
    private static string destination;
    private static Transform observedRoot;
    private static SkinnedMeshRenderer otherGuideMesh;
    private static int otherJawIndex;
    private static SkinnedMeshRenderer mesh;
    private static NavMeshAgent agent;
    private static int jawIndex, blinkIndex;

    public static string Observe(string name, float seconds = 14f)
    {
        if(!EditorApplication.isPlaying) return "Enter Play Mode first";
        var identity=UnityEngine.Object.FindAnyObjectByType<ScenarioCharacterIdentity>();
        return identity==null?"No scenario character":ObserveTarget(identity.gameObject,identity.DisplayName,name,seconds);
    }

    public static string ObservePerson(ObjectMetadata person,string name,float seconds=14f)
    {
        return person==null?"No person":ObserveTarget(person.gameObject,person.GetDisplayName(),name,seconds);
    }

    private static string ObserveTarget(GameObject target,string label,string name,float seconds)
    {
        if(!EditorApplication.isPlaying)return "Enter Play Mode first";
        observedRoot=target.transform;
        var visual=observedRoot.Find("LumenGuideVisual")??observedRoot.Find("NpcAssetVisual")??observedRoot.Find("CitizenVisual")??observedRoot;
        mesh=visual.GetComponentInChildren<SkinnedMeshRenderer>();
        if(mesh==null)return "No facial mesh";
        agent=target.GetComponent<NavMeshAgent>();
        otherGuideMesh=null;otherJawIndex=-1;
        var guide=UnityEngine.Object.FindAnyObjectByType<ScenarioCharacterIdentity>();
        if(guide!=null&&guide.transform!=observedRoot)
        {
            var guideVisual=guide.transform.Find("LumenGuideVisual")??guide.transform;
            otherGuideMesh=guideVisual.GetComponentInChildren<SkinnedMeshRenderer>();
            if(otherGuideMesh!=null)for(int i=0;i<otherGuideMesh.sharedMesh.blendShapeCount;i++)
            {
                string shape=otherGuideMesh.sharedMesh.GetBlendShapeName(i).ToLowerInvariant();
                if(shape.Contains("ak_")&&shape.EndsWith("jawopen")){otherJawIndex=i;break;}
            }
        }
        jawIndex=blinkIndex=-1;
        var shapes=new List<string>();
        for(int i=0;i<mesh.sharedMesh.blendShapeCount;i++)
        {
            string shape=mesh.sharedMesh.GetBlendShapeName(i); shapes.Add(shape);
            if(shape.ToLowerInvariant().Contains("ak_") && shape.ToLowerInvariant().EndsWith("jawopen")) jawIndex=i;
            if(shape.ToLowerInvariant().Contains("ak_") && shape.ToLowerInvariant().EndsWith("eyeblinkleft")) blinkIndex=i;
        }
        report=new Report{scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,character=label,
            initialPosition=observedRoot.position,blendShapes=shapes.ToArray()};
        destination="D:/LumenProject/Verification/FinalFrontend/"+name;
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        until=Time.realtimeSinceStartup+seconds; lastFrame=-1;
        EditorApplication.update-=Sample; EditorApplication.update+=Sample;
        return "Observing "+label+" into "+destination+".json";
    }

    private static void Sample()
    {
        if(!EditorApplication.isPlaying || observedRoot==null || Time.realtimeSinceStartup>=until)
        {
            EditorApplication.update-=Sample;
            if(report!=null)
            {
                if(observedRoot!=null) report.finalPosition=observedRoot.position;
                File.WriteAllText(destination+".json",JsonUtility.ToJson(report,true));
                ScreenCapture.CaptureScreenshot(destination+".png");
                report=null;
            }
            return;
        }
        if(lastFrame==Time.frameCount) return;
        lastFrame=Time.frameCount; report.frames++;
        if(otherGuideMesh!=null&&otherJawIndex>=0)report.maximumOtherGuideJaw=Mathf.Max(report.maximumOtherGuideJaw,otherGuideMesh.GetBlendShapeWeight(otherJawIndex));
        if(jawIndex>=0) {float value=mesh.GetBlendShapeWeight(jawIndex);report.minimumJaw=Mathf.Min(report.minimumJaw,value);report.maximumJaw=Mathf.Max(report.maximumJaw,value);}
        if(blinkIndex>=0) report.maximumBlink=Mathf.Max(report.maximumBlink,mesh.GetBlendShapeWeight(blinkIndex));
        if(agent!=null && agent.enabled && agent.isOnNavMesh) {report.validNavFrames++;report.maximumSpeed=Mathf.Max(report.maximumSpeed,agent.velocity.magnitude);}
        foreach(var output in UnityEngine.Object.FindObjectsByType<SpeechOutputController>())
        {
            var source=output.GetComponent<AudioSource>();
            if(source!=null && source.isPlaying)
            {
                report.speakingFrames++;if(source.clip!=null)report.audioLength=source.clip.length;
                if(agent!=null&&agent.enabled&&agent.isOnNavMesh)report.maximumSpeakingSpeed=Mathf.Max(report.maximumSpeakingSpeed,agent.velocity.magnitude);
            }
        }
        if(WalkableFloorProbe.TryFind(observedRoot,observedRoot.position,.25f,.6f,out RaycastHit floor))
        {
            float gap=mesh.bounds.min.y-floor.point.y;
            report.minimumFeetGap=Mathf.Min(report.minimumFeetGap,gap); report.maximumFeetGap=Mathf.Max(report.maximumFeetGap,gap);
        }
        var animator=mesh.GetComponentInParent<Animator>();
        report.animatorState=animator.GetCurrentAnimatorStateInfo(0).fullPathHash.ToString();
    }
}

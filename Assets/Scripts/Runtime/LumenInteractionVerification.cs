using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in regression checks through real scene, gaze, partner and image entry points.</summary>
public sealed class LumenInteractionVerification : MonoBehaviour
{
    [Serializable] public sealed class Report { public List<string> checks=new List<string>(); public List<string> errors=new List<string>(); }
    private readonly Report report=new Report();
    private void Check(bool passed,string message) { report.checks.Add((passed?"PASS ":"FAIL ")+message);if(!passed)report.errors.Add(message); }
    public static void Begin() { var go=new GameObject("Interaction regression checks");DontDestroyOnLoad(go);go.AddComponent<LumenInteractionVerification>().StartCoroutine(go.GetComponent<LumenInteractionVerification>().Run()); }
    private IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(3);
        int previousVisuals=LumenPreferences.GetInt("Lumen.VisualAids",1);
        LumenPreferences.SetInt("Lumen.VisualAids",0);
        Check(SpeechInputController.Instance!=null&&!SpeechInputController.Instance.IsListening,"Microphone starts off");
        foreach(string world in Environment.GetCommandLineArgs().Contains("-lumen-travel-only")?Array.Empty<string>():new[]{"classroom","ancient_rome","cafe","museum"})
        {
            var travel=ScenarioTransitionService.RequestTravel(world);
            float arrival=Time.realtimeSinceStartup+45;
            while(!travel.Terminal&&Time.realtimeSinceStartup<arrival)yield return null;
            yield return new WaitForSecondsRealtime(3);
            Check(ScenarioRuntimeManager.NormalizeScenario(ScenarioRuntimeManager.GetScenarioForScene(SceneManager.GetActiveScene().name))==world,"Scene navigation "+world);
            var gaze=FindAnyObjectByType<ObjectRaycaster>();
            var camera=Camera.main;
            Check(gaze!=null&&camera!=null,"Gaze services "+world);
            if(gaze!=null&&camera!=null)
            {
                var target=GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position=new Vector3(0,1000,0);
                var metadata=target.AddComponent<ObjectMetadata>();metadata.displayName="Verification object";metadata.description="A known cube.";metadata.category="Object";
                var position=camera.transform.position;var rotation=camera.transform.rotation;
                camera.transform.position=target.transform.position+Vector3.back*2;camera.transform.LookAt(target.transform);
                Physics.SyncTransforms();
                Check(gaze.RefreshTarget()==metadata,"Gaze identifies an object without E in "+world);
                camera.transform.SetPositionAndRotation(position,rotation);Destroy(target);
            }
            var chat=FindAnyObjectByType<ChatUIManager>();
            var people=FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Exclude).Where(p=>p.category=="Person"&&p.GetComponentInParent<ScenarioCharacterIdentity>()==null).ToArray();
            foreach(var person in people)
            {
                if(chat==null) { Check(false,"Missing world chat "+world);break; }
                GeminiLiveConversation.Instance.SelectPartner(person);
                ChatResponse reply=null;Action<ChatResponse> received=r=>reply=r;chat.ResponseReceived+=received;
                chat.SubmitWorldPrompt("Who are you?");
                float until=Time.realtimeSinceStartup+15;
                while(reply==null&&Time.realtimeSinceStartup<until)yield return null;
                Check(reply!=null&&reply.speaker_name==person.GetDisplayName(),"Selected person owns ordinary follow-up in "+world+" expected="+person.GetDisplayName()+" actual="+(reply!=null?reply.speaker_name:"no reply"));
                chat.ResponseReceived-=received;
                GeminiLiveConversation.Instance.SelectPartner(null);
            }
        }
        LumenPreferences.SetInt("Lumen.VisualAids",1);
        var client=FindAnyObjectByType<AIClient>();
        bool done=false;string error="";
        yield return client.SendChatMessage("What is an atom?","classroom","","",r=>done=true,e=>error=e);
        Check(done&&error=="","Ordinary atom question answered");
        var images=GeminiLiveConversation.Instance.Images;
        float imageUntil=Time.realtimeSinceStartup+40;
        while(images.State!="Ready"&&images.State!="Unavailable"&&Time.realtimeSinceStartup<imageUntil)yield return null;
        Check(images.RequestedForTurn,"Ordinary question automatically requests an image");
        Check(images.HasImage,"Relevant atom image downloaded");
        var homeTravel=ScenarioTransitionService.RequestTravel("home");
        float homeUntil=Time.realtimeSinceStartup+45;
        while(!homeTravel.Terminal&&Time.realtimeSinceStartup<homeUntil)yield return null;
        yield return new WaitForSecondsRealtime(3);
        var home=FindAnyObjectByType<HomeUIManager>();
        Check(home!=null,"Home chat ready for topic-based travel");
        if(home!=null)
        {
            home.SubmitVoiceTranscription("What is an atom?");
            float arrivalUntil=Time.realtimeSinceStartup+45;
            while(SceneManager.GetActiveScene().name!="Classroom"&&Time.realtimeSinceStartup<arrivalUntil)yield return null;
            yield return new WaitForSecondsRealtime(3);
            Check(SceneManager.GetActiveScene().name=="Classroom","Ordinary atom question travels from hub to classroom");
            var arrivalImages=GeminiLiveConversation.Instance.Images;
            float carriedUntil=Time.realtimeSinceStartup+40;
            while(arrivalImages.State!="Ready"&&Time.realtimeSinceStartup<carriedUntil)yield return null;
            Check(arrivalImages.HasImage&&arrivalImages.Topic.ToLowerInvariant().Contains("atom"),"Question image survives automatic world travel");
        }
        var path=Path.Combine(Application.dataPath,"../Builds/InteractionVerification.json");
        File.WriteAllText(path,JsonUtility.ToJson(report,true));
        LumenPreferences.SetInt("Lumen.VisualAids",previousVisuals);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(report.errors.Count==0?0:1);
#endif
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Opt-in local recognition/answer to real Gemini speech acceptance.</summary>
public sealed class LumenHybridVoiceVerification : MonoBehaviour
{
    [Serializable] private sealed class Report
    {
        public string status="Running",build;
        public List<string> checks=new List<string>(),errors=new List<string>(),answers=new List<string>();
    }
    [Serializable] private sealed class Transcript { public string text; }
    private readonly Report report=new Report();
    private string directory;
    private float deadline;
    private bool finished;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Requested()
    {
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-lumen-hybrid-dir");
        if(i<0||i+1>=args.Length||FindAnyObjectByType<LumenHybridVoiceVerification>()!=null)return;
        var go=new GameObject("Hybrid voice verification");DontDestroyOnLoad(go);
        var verification=go.AddComponent<LumenHybridVoiceVerification>();
        verification.directory=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(verification.directory);
        verification.deadline=Time.realtimeSinceStartup+240;
        verification.report.build=Application.isEditor?"EDITOR_SOURCE_REVIEW":Application.buildGUID;
        Application.runInBackground=true;
        verification.StartCoroutine(verification.Run());
    }
    private void Check(bool ok,string text){report.checks.Add((ok?"PASS ":"FAIL ")+text);if(!ok)report.errors.Add(text);}
    private void Update(){if(!finished&&deadline>0&&Time.realtimeSinceStartup>deadline){Check(false,"Hybrid voice verification timed out");Finish();}}
    private IEnumerator Run()
    {
        Check(LumenPreferences.IsIsolated,"Acceptance preferences are isolated");
        if(!LumenPreferences.IsIsolated){Finish();yield break;}
        LumenPreferences.SetInt("Lumen.Offline",0);LumenPreferences.SetInt("Lumen.VisualAids",0);
        LumenPreferences.SetInt("Lumen.VoiceChoiceSeen",1);LumenPreferences.SetInt("Lumen.MuteVoice",0);
        yield return new WaitForSecondsRealtime(6);
        var front=FindAnyObjectByType<LumenFrontPage>();
        if(front!=null)
        {
            Check(LumenFrontPage.IsOpen&&LumenAccessibility.MenuOpen,"Merged front page opens and owns menu input");
            Check(FindObjectsByType<HomeHubPortal>(FindObjectsInactive.Exclude).Length>=4,"All existing courtyard destinations remain available");
            yield return Capture("front-page");
            front.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(button=>button.name=="CHOOSE ENVIRONMENT").onClick.Invoke();
            Check(GameObject.Find("Choose environment")!=null&&GameObject.Find("World atlas")==null,"Environment chooser remains separate from the atlas");
            front.ShowMenu();
            front.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(button=>button.name=="WORLD MAP").onClick.Invoke();
            yield return null;Canvas.ForceUpdateCanvases();
            var terrain=front.GetComponentInChildren<LumenAtlasTerrain>();
            var mesh=terrain!=null?terrain.GetComponent<CanvasRenderer>()?.GetMesh():null;
            Check(mesh!=null&&mesh.vertexCount>0,"Merged atlas renders its terrain mesh");
            foreach(string destination in new[]{"cafe","classroom","ancient_rome"})
                Check(Resources.Load<Texture2D>("DestinationPreviews/"+destination)!=null,"Incoming scene preview is present: "+destination);
            front.SelectDestination(1);
            yield return Capture("world-atlas");
            front.ShowMenu();
            front.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(button=>button.name=="SETTINGS").onClick.Invoke();
            Check(GameObject.Find("VisitSettings")!=null,"Front-page settings retain the existing visit controls");
            front.ShowMenu();front.StartExperience();
            yield return new WaitForSecondsRealtime(.7f);
            Check(!LumenFrontPage.IsOpen&&!LumenAccessibility.MenuOpen,"Start Experience restores the existing conversation and movement flow");
        }
        var output=FindAnyObjectByType<SpeechOutputController>();
        Check(FindObjectsByType<SpeechOutputController>(FindObjectsInactive.Exclude).Length==1,"Playback and cancellation share one speech controller");
        Check(output!=null&&output.ConfiguredSpeechEngine=="gemini-tts","Hybrid uses the Gemini TTS speech endpoint");
        var live=GeminiLiveConversation.Instance;
        Check(live!=null&&live.VoiceOnly&&!live.Selected&&!live.Ready,"Hybrid never opens a Gemini Live answer session");
        Check(SpeechInputController.Instance!=null&&!SpeechInputController.Instance.IsListening,"Microphone remains off during recorded-input verification");
        if(output==null){Finish();yield break;}
        yield return Ask("Give me one short sentence to welcome a new classmate.",false);
        if(finished)yield break;
        var identity=FindAnyObjectByType<ScenarioCharacterIdentity>();
        identity.ApplyAppearance("MuseumMystic");
        Check(identity.CloudVoiceName=="Charon","Switched guide selects its authored Gemini voice");
        var client=FindAnyObjectByType<AIClient>();
        string recognized="";
        using(var request=new UnityWebRequest(client.BackendUrl+"/transcribe","POST"))
        {
            request.uploadHandler=new UploadHandlerRaw(File.ReadAllBytes(Path.Combine(directory,"voice-question.wav")));
            request.downloadHandler=new DownloadHandlerBuffer();request.SetRequestHeader("Content-Type","audio/wav");request.timeout=30;
            yield return request.SendWebRequest();
            if(request.result==UnityWebRequest.Result.Success)
                recognized=JsonUtility.FromJson<Transcript>(request.downloadHandler.text)?.text??"";
        }
        Check(!string.IsNullOrWhiteSpace(recognized)&&recognized.IndexOf("greet",StringComparison.OrdinalIgnoreCase)>=0,
            "Recorded speech is transcribed locally: "+recognized);
        if(!string.IsNullOrWhiteSpace(recognized))yield return Ask(recognized,true);
        output.SpeakAs("This pending speech must be cancelled.",identity.VoiceName,identity.DisplayName,identity.GetComponent<AgentAnimator>());
        yield return null;
        output.CancelSpeech();
        yield return new WaitForSecondsRealtime(.3f);
        Check(!output.IsSpeaking&&!output.IsSpeechPending,"Cancelling aborts pending speech and clears playback");
        Finish();
    }
    private IEnumerator Ask(string text,bool voice)
    {
        var home=FindAnyObjectByType<HomeUIManager>();
        var output=FindAnyObjectByType<SpeechOutputController>();
        ChatResponse response=null;Action<ChatResponse> receive=value=>response=value;
        home.ResponseReceived+=receive;
        if(voice)Check(SpeechInputController.Instance.AcceptTranscript(text,true),"Recognized speech enters the regular local conversation flow");
        else{home.userInputField.text=text;home.sendButton.onClick.Invoke();}
        float until=Time.realtimeSinceStartup+90;bool geminiPlayed=false;float peak=0;
        var samples=new float[4096];
        while(Time.realtimeSinceStartup<until)
        {
            if(output.IsSpeaking&&output.LastSpeechEngine=="gemini-tts")
            {
                geminiPlayed=true;
                var clip=output.GetComponents<AudioSource>().FirstOrDefault(source=>source.isPlaying)?.clip;
                if(clip!=null&&clip.samples>=samples.Length&&clip.GetData(samples,Mathf.Min(clip.samples/3,clip.samples-samples.Length)))peak=Mathf.Max(peak,samples.Max(sample=>Mathf.Abs(sample)));
            }
            if(response!=null&&geminiPlayed&&!output.IsSpeechPending&&!output.IsSpeaking)break;
            yield return null;
        }
        home.ResponseReceived-=receive;
        Check(response!=null&&response.response_source=="ollama",(voice?"Voice":"Typed")+" answer comes from the local LLM");
        Check(response!=null&&!response.spoken_stream,"Hybrid voices one completed local reply rather than separate sentence requests");
        Check(geminiPlayed&&peak>.001f,(voice?"Voice":"Typed")+" answer plays non-silent Gemini audio");
        if(response!=null)report.answers.Add(response.response);
    }
    private IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var image=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());Destroy(image);
    }
    private void Finish()
    {
        if(finished)return;finished=true;
        FindAnyObjectByType<SpeechOutputController>()?.CancelSpeech();
        report.status=report.errors.Count==0?"Passed":"Failed";
        File.WriteAllText(Path.Combine(directory,"hybrid-voice.json"),JsonUtility.ToJson(report,true));
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(report.errors.Count==0?0:1);
#else
        Application.Quit(report.errors.Count==0?0:1);
#endif
    }
}

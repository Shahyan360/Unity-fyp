using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>Runs local text and recorded speech through the actual Windows player.</summary>
public sealed class LumenDemoVerification : MonoBehaviour
{
    [Serializable] private sealed class Transcript { public string text; }
    [Serializable] private sealed class Turn
    {
        public string question, answer, source;
        public float firstVisibleSeconds = -1, firstAudioSeconds = -1, finishedSeconds = -1, peakAudio, peakJaw;
    }
    [Serializable] private sealed class Report
    {
        public string build, recognized, status = "Running";
        public float recognitionSeconds;
        public List<string> errors = new List<string>();
        public List<Turn> turns = new List<Turn>();
    }

    private readonly Report report = new Report();
    private string directory;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartIfRequested()
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-lumen-demo-dir");
        if (index < 0 || index + 1 >= args.Length || !LumenPreferences.IsIsolated) return;
        var runner = new GameObject("Local demo verification").AddComponent<LumenDemoVerification>();
        runner.directory = Path.GetFullPath(args[index + 1]);
        runner.StartCoroutine(runner.Run());
    }

    private IEnumerator Run()
    {
        report.build = Application.buildGUID;
        Application.logMessageReceived += RecordError;
        Application.runInBackground = true;
        Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
        yield return new WaitForSecondsRealtime(3);
        var home = FindAnyObjectByType<HomeUIManager>();
        var input = SpeechInputController.Instance;
        var identity = FindAnyObjectByType<ScenarioCharacterIdentity>();
        if (home == null || home.aiClient == null || input == null || identity == null)
        {
            report.errors.Add("Home conversation services were unavailable.");
            Finish();
            yield break;
        }
        input.SetListening(false);
        yield return Capture("01-home");
        var worlds = GameObject.Find("RevealWorlds")?.GetComponent<Button>();
        if (worlds == null) report.errors.Add("World picker button was unavailable.");
        else
        {
            worlds.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.5f);
            yield return Capture("01b-world-picker");
            worlds.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.5f);
        }
        yield return Ask(home, identity, "Give me a friendly way to welcome a new classmate.", false, true, "hi", "hello", "welcome", "introduce");
        yield return Ask(home, identity, "Suggest a twenty-minute study routine with a short break.", false, false, "14 minutes");
        yield return Ask(home, identity, "Explain why the Moon has phases in simple terms.", false, false, "sunlit", "illuminat");

        byte[] recording = File.ReadAllBytes(Path.Combine(directory, "voice-question.wav"));
        float started = Time.realtimeSinceStartup;
        using (var request = new UnityWebRequest(home.aiClient.BackendUrl + "/transcribe", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(recording);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "audio/wav");
            request.timeout = 20;
            yield return request.SendWebRequest();
            report.recognitionSeconds = Time.realtimeSinceStartup - started;
            if (request.result == UnityWebRequest.Result.Success)
                report.recognized = JsonUtility.FromJson<Transcript>(request.downloadHandler.text)?.text;
            else
                report.errors.Add("Recorded voice failed to transcribe: " + request.error);
        }
        if (report.recognized?.IndexOf("sky", StringComparison.OrdinalIgnoreCase) >= 0)
            yield return Ask(home, identity, report.recognized, true, false, "scatter", "wavelength");
        else
            report.errors.Add("Recorded sky question was not recognized.");
        yield return Capture("03-voice-transcript");
        Finish();
    }

    private IEnumerator Ask(HomeUIManager home, ScenarioCharacterIdentity identity, string question, bool voice, bool requireStreaming, params string[] expected)
    {
        var turn = new Turn { question = question };
        report.turns.Add(turn);
        ChatResponse response = null;
        void Receive(ChatResponse value) => response = value;
        home.ResponseReceived += Receive;
        var output = FindAnyObjectByType<SpeechOutputController>();
        var skins = identity.GetComponentsInChildren<SkinnedMeshRenderer>();
        var jaws = skins.Where(s => s.sharedMesh != null).SelectMany(s => Enumerable.Range(0, s.sharedMesh.blendShapeCount)
            .Where(i => s.sharedMesh.GetBlendShapeName(i).EndsWith("jawopen", StringComparison.OrdinalIgnoreCase))
            .Select(i => (skin: s, index: i))).ToArray();
        var samples = new float[256];
        float started = Time.realtimeSinceStartup;
        if (voice)
            SpeechInputController.Instance.AcceptTranscript(question, true);
        else
        {
            home.userInputField.text = question;
            home.sendButton.onClick.Invoke();
        }
        bool captured = false;
        while (Time.realtimeSinceStartup - started < 40)
        {
            float elapsed = Time.realtimeSinceStartup - started;
            string lastLine = home.chatOutputText.text.Split('\n', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (turn.firstVisibleSeconds < 0 && lastLine?.StartsWith(identity.DisplayName + ": ") == true)
                turn.firstVisibleSeconds = elapsed;
            if (response != null && turn.finishedSeconds < 0) turn.finishedSeconds = elapsed;
            if (output != null && output.IsSpeaking)
            {
                if (turn.firstAudioSeconds < 0) turn.firstAudioSeconds = elapsed;
                var speaker = output.GetComponents<AudioSource>().FirstOrDefault(s => s.isPlaying);
                if (speaker != null)
                {
                    speaker.GetOutputData(samples, 0);
                    turn.peakAudio = Mathf.Max(turn.peakAudio, samples.Max(sample => Mathf.Abs(sample)));
                }
                foreach (var (skin, index) in jaws)
                    turn.peakJaw = Mathf.Max(turn.peakJaw, skin.GetBlendShapeWeight(index));
                if (!captured && turn.peakJaw > 3)
                {
                    captured = true;
                    yield return Capture(voice ? "04-voice-speaking" : "02-typed-speaking");
                }
            }
            if (response != null && turn.firstAudioSeconds >= 0 && !output.IsSpeaking) break;
            yield return null;
        }
        home.ResponseReceived -= Receive;
        turn.answer = response?.response;
        turn.source = response?.response_source;
        bool supportedSource = turn.source == "ollama" ||
            (!requireStreaming && (turn.source == "local-reference" || turn.source == "local-plan"));
        if (!supportedSource || turn.answer == null ||
            !expected.Any(word => turn.answer.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0))
            report.errors.Add("Missing grounded Ollama answer for: " + question);
        if (turn.firstVisibleSeconds < 0 || (requireStreaming && turn.firstVisibleSeconds >= turn.finishedSeconds))
            report.errors.Add("No visible partial reply before completion for: " + question);
        if (turn.firstAudioSeconds < 0 || turn.peakAudio < .001f || turn.peakJaw < 3)
            report.errors.Add("Audio output or jaw movement was missing for: " + question);
        yield return new WaitForSecondsRealtime(2);
    }

    private IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
        Destroy(image);
    }

    private void RecordError(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            report.errors.Add(message);
    }

    private void Finish()
    {
        Application.logMessageReceived -= RecordError;
        report.status = report.errors.Count == 0 ? "Passed" : "Failed";
        File.WriteAllText(Path.Combine(directory, "demo.json"), JsonUtility.ToJson(report, true));
        Application.Quit(report.errors.Count == 0 ? 0 : 1);
    }
}

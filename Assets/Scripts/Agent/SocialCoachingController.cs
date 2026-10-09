using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SocialCoachingController : MonoBehaviour
{
    [SerializeField] private AIClient client;
    public string ActiveMode { get; private set; }
    public string CurrentPrompt { get; private set; }

    private void Awake() => client ??= GetComponent<AIClient>();

    public void StartPractice(string mode) => StartCoroutine(StartPracticeRequest(mode));

    private IEnumerator StartPracticeRequest(string mode)
    {
        if (client == null) yield break;
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new CoachingStartRequest { mode = mode, session_id = client.SessionId }));
        using UnityWebRequest request = new UnityWebRequest(client.BackendUrl + "/coaching/start", UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            CoachingStartResponse result = JsonUtility.FromJson<CoachingStartResponse>(request.downloadHandler.text);
            if (result != null) { ActiveMode = result.mode; CurrentPrompt = result.prompt; }
        }
    }

    [System.Serializable] private class CoachingStartRequest { public string mode; public string session_id; }
    [System.Serializable] private class CoachingStartResponse { public string session_id; public string mode; public string prompt; public int turn; }
}

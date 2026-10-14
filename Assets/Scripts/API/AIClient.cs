using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIClient : MonoBehaviour
{
    private static string sharedSessionId;
    [Header("Configuration")]
    [SerializeField] private AppConfig appConfig;

    [Tooltip("Used if AppConfig is not assigned.")]
    [SerializeField] private string fallbackBackendUrl = "http://127.0.0.1:5000";

    /// <summary>Ephemeral local conversation key returned by the Flask service.</summary>
    public string SessionId { get; private set; }

    public string BackendUrl
    {
        get
        {
            if (appConfig != null && !string.IsNullOrWhiteSpace(appConfig.backendUrl))
            {
                return appConfig.backendUrl.TrimEnd('/');
            }

            return fallbackBackendUrl.TrimEnd('/');
        }
    }

    private void Awake()
    {
        SessionId = sharedSessionId;
        if (appConfig == null)
        {
            appConfig = FindAnyObjectByType<AppConfig>();
        }

        EmbodiedExperienceBootstrap.Ensure(this);
    }

    public IEnumerator SendChatMessage(
        string query,
        string scenario,
        string objectName,
        string objectDescription,
        Action<ChatResponse> onSuccess,
        Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            onError?.Invoke("Please enter a message before sending.");
            yield break;
        }

        ChatRequest requestBody = new ChatRequest
        {
            query = query,
            scenario = scenario,
            object_name = objectName,
            object_description = objectDescription,
            session_id = SessionId
        };

        string jsonBody = JsonUtility.ToJson(requestBody);
        byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
        string chatUrl = $"{BackendUrl}/chat";

        using (UnityWebRequest request = new UnityWebRequest(chatUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 60;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                onError?.Invoke("Could not connect to the backend. Make sure Flask is running.");
                yield break;
            }

            if (request.result == UnityWebRequest.Result.ProtocolError)
            {
                string message = $"Backend returned HTTP {request.responseCode}.";
                if (!string.IsNullOrWhiteSpace(request.downloadHandler.text))
                {
                    message += $" Details: {request.downloadHandler.text}";
                }

                onError?.Invoke(message);
                yield break;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Request failed: {request.error}");
                yield break;
            }

            try
            {
                ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);

                if (response == null || string.IsNullOrWhiteSpace(response.response))
                {
                    onError?.Invoke("Backend returned an empty or invalid chat response.");
                    yield break;
                }

                if (!string.IsNullOrWhiteSpace(response.session_id))
                {
                    SessionId = response.session_id;
                    sharedSessionId = response.session_id;
                }

                onSuccess?.Invoke(response);
            }
            catch (Exception exception)
            {
                onError?.Invoke($"Could not parse backend response: {exception.Message}");
            }
        }
    }
}

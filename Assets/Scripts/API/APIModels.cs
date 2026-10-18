using System;

// These classes match the Flask backend JSON contract.
// Unity's JsonUtility needs serializable classes with public fields.

[Serializable]
public class ChatRequest
{
    public string query;
    public string scenario;
    public string object_name;
    public string object_description;
    public string session_id;
    public string speaker_id;
    public string interrupted_response;
}

[Serializable]
public class ChatResponse
{
    public string response;
    public string emotion;
    public string action;
    public string intent;
    public string navigation_target;
    public string next_scenario;
    public string session_id;
    public string scenario;
    public bool offline;
    public string diagnostic;
    public string speaker_id;
    public string speaker_name;
    public string speaker_role;
    public string response_source;
    public float first_token_ms;
    public float response_time_ms;
    [NonSerialized] public bool spoken_stream;
}

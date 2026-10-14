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
}

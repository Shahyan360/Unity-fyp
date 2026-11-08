using PlayerPrefs = LumenPreferences;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A bounded local list of discovered exhibits; never stores chat text.</summary>
public static class ExplorationNotebook
{
    [Serializable] private sealed class Entries { public List<string> items = new List<string>(); }
    private const string Key = "Lumen.ExplorationNotebook.v1";
    public static void Record(ChatResponse response)
    {
        if (response == null || response.action != "show_model" || string.IsNullOrWhiteSpace(response.navigation_target)) return;
        // Compatibility hook reads the actual displayed model, never an unexecuted transport target.
        if(ClassroomLearningExhibit.HasActiveModel)RecordExhibit(ClassroomLearningExhibit.ActiveModel);
    }
    public static void RecordExhibit(string stableName)
    {
        if(string.IsNullOrWhiteSpace(stableName))return;
        Entries data=Read();
        string item=stableName.Trim();if(item.Length>80)item=item.Substring(0,80);
        if(data.items.Exists(v=>string.Equals(v,item,StringComparison.OrdinalIgnoreCase)))return;
        data.items.Add(item);if(data.items.Count>40)data.items.RemoveAt(0);
        PlayerPrefs.SetString(Key,JsonUtility.ToJson(data));PlayerPrefs.Save();
    }
    public static void Reset(){PlayerPrefs.DeleteKey(Key);PlayerPrefs.Save();}
    public static string Summary()
    {
        Entries data = Read();
        return data.items.Count == 0 ? "Your discoveries will appear here. Ask a guide to show an exhibit." : "• "+string.Join("\n• ", data.items);
    }
    private static Entries Read()
    {
        try { Entries data = JsonUtility.FromJson<Entries>(PlayerPrefs.GetString(Key, "{}")); return data != null && data.items != null ? data : new Entries(); }
        catch (ArgumentException) { return new Entries(); }
    }
}

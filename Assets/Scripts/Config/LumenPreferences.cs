using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Normal preferences use Unity storage; acceptance runs use a separate local file.</summary>
public static class LumenPreferences
{
    [Serializable] private sealed class Entry { public string key, value; }
    [Serializable] private sealed class Store { public List<Entry> entries = new List<Entry>(); }
    private static readonly Dictionary<string,string> values = new Dictionary<string,string>();
    private static readonly string testFile;
    public static bool IsIsolated => testFile != null;
    static LumenPreferences()
    {
        string[] args = Environment.GetCommandLineArgs();
        foreach (string flag in new[] { "-lumen-test-profile", "-lumen-verify-dir", "-lumen-conversation-dir", "-lumen-image-dir", "-lumen-metrics-dir", "-lumen-soak-dir", "-lumen-functional-dir", "-lumen-live-walk-dir" })
        {
            int i = Array.IndexOf(args, flag);
            if (i < 0 || i + 1 >= args.Length) continue;
            string directory = Path.GetFullPath(args[i + 1]);
            Directory.CreateDirectory(directory);
            testFile = Path.Combine(directory, "isolated-preferences.json");
            if (File.Exists(testFile))
            {
                var store = JsonUtility.FromJson<Store>(File.ReadAllText(testFile));
                if (store?.entries != null) foreach (var entry in store.entries) values[entry.key] = entry.value;
            }
            break;
        }
        if(testFile==null&&Application.isEditor)
        {
            string implementation=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            if(File.Exists(Path.Combine(implementation,"Reports","IMPLEMENTATION_BRIEF.md")))
            {
                string directory=Path.Combine(implementation,"Evidence","EditorProfile");Directory.CreateDirectory(directory);
                testFile=Path.Combine(directory,"isolated-preferences.json");
                if(File.Exists(testFile))
                {
                    var store=JsonUtility.FromJson<Store>(File.ReadAllText(testFile));
                    if(store?.entries!=null)foreach(var entry in store.entries)values[entry.key]=entry.value;
                }
                if(!values.ContainsKey("Lumen.VoiceEnabled"))values["Lumen.VoiceEnabled"]="0";
            }
        }
    }
    public static bool HasKey(string key) => IsIsolated ? values.ContainsKey(key) : UnityEngine.PlayerPrefs.HasKey(key);
    public static string GetString(string key, string fallback = "") => IsIsolated ? (values.TryGetValue(key,out string value) ? value : fallback) : UnityEngine.PlayerPrefs.GetString(key,fallback);
    public static int GetInt(string key, int fallback = 0) => IsIsolated ? (int.TryParse(GetString(key),out int value) ? value : fallback) : UnityEngine.PlayerPrefs.GetInt(key,fallback);
    public static void SetString(string key,string value) { if(IsIsolated) values[key]=value; else UnityEngine.PlayerPrefs.SetString(key,value); }
    public static void SetInt(string key,int value) { if(IsIsolated) values[key]=value.ToString(System.Globalization.CultureInfo.InvariantCulture); else UnityEngine.PlayerPrefs.SetInt(key,value); }
    public static void DeleteKey(string key) { if(IsIsolated) values.Remove(key); else UnityEngine.PlayerPrefs.DeleteKey(key); }
    public static void Save()
    {
        if(!IsIsolated) { UnityEngine.PlayerPrefs.Save(); return; }
        var store = new Store(); foreach(var pair in values) store.entries.Add(new Entry {key=pair.Key,value=pair.Value});
        File.WriteAllText(testFile,JsonUtility.ToJson(store,true));
    }
}

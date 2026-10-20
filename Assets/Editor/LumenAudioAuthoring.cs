using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>Uses the installed Editor's mixer authoring API; no package changes.</summary>
public static class LumenAudioAuthoring
{
    public static string Build()
    {
        const string path="Assets/Resources/LumenAudio.mixer";
        var existing=AssetDatabase.LoadAssetAtPath<AudioMixer>(path);if(existing!=null)return string.Join(",",existing.FindMatchingGroups("").Select(g=>g.name));
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.Audio.AudioMixerController")).First(t=>t!=null);
        var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        var mixer=type.GetMethod("CreateMixerControllerAtPath",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{path});
        var master=type.GetProperty("masterGroup").GetValue(mixer);var groupType=master.GetType();var children=Array.CreateInstance(groupType,3);
        string[] names={"Speech","Ambience","UI"};float[] levels={0f,-20f,-6f};
        var snapshot=type.GetProperty("startSnapshot").GetValue(mixer);
        for(int i=0;i<3;i++)
        {
            var group=type.GetMethod("CreateNewGroup",flags).Invoke(mixer,new object[]{names[i],false});children.SetValue(group,i);
            groupType.GetMethod("SetValueForVolume",flags).Invoke(group,new object[]{mixer,snapshot,levels[i]});EditorUtility.SetDirty((UnityEngine.Object)group);
        }
        groupType.GetProperty("children").SetValue(master,children);EditorUtility.SetDirty((UnityEngine.Object)master);EditorUtility.SetDirty((UnityEngine.Object)snapshot);EditorUtility.SetDirty((UnityEngine.Object)mixer);AssetDatabase.SaveAssets();
        return string.Join(",",((AudioMixer)mixer).FindMatchingGroups("").Select(g=>g.name));
    }
}

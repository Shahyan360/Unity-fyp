using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public static class LumenAudioMix
{
    public static void Route(AudioSource source,string group)
    {
        if(source==null)return;var mixer=Resources.Load<AudioMixer>("LumenAudio");
        source.outputAudioMixerGroup=mixer?.FindMatchingGroups(group).FirstOrDefault(g=>g.name==group);
    }
}

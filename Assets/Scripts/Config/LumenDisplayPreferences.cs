using UnityEngine;

/// <summary>Applies display and global audio preferences once; the existing visit drawer edits them.</summary>
public static class LumenDisplayPreferences
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        AudioListener.volume=Mathf.Clamp01(LumenPreferences.GetInt("Lumen.MasterVolume",100)/100f);
        if(Application.isEditor)return;
        bool fullscreen=LumenPreferences.GetInt("Lumen.Fullscreen",Screen.fullScreen?1:0)!=0;
        int width=LumenPreferences.GetInt("Lumen.DisplayWidth",Screen.width);
        int height=LumenPreferences.GetInt("Lumen.DisplayHeight",Screen.height);
        bool supported=false;
        foreach(var mode in Screen.resolutions)if(mode.width==width&&mode.height==height){supported=true;break;}
        if(supported)Screen.SetResolution(width,height,fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
        else if(LumenPreferences.HasKey("Lumen.Fullscreen"))Screen.fullScreen=fullscreen;
    }
}

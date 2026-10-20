using PlayerPrefs = LumenPreferences;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Runtime-only world finishing, selectable camera effects, and honest frame-time sampling.</summary>
[DisallowMultipleComponent]
public sealed class WorldPresentationQuality : MonoBehaviour
{
    public enum QualityPreset { Low, Balanced, High }
    private const string PreferenceKey = "Lumen.PresentationQuality";
    private const int SampleCount = 180;
    private readonly float[] frameTimes = new float[SampleCount];
    private readonly float[] sortedFrameTimes = new float[SampleCount];
    private int samples;
    private int sampleIndex;
    private float nextSummary;
    private string world;
    private Volume volume;
    private VolumeProfile profile;
    private Bloom bloom;
    private UniversalAdditionalCameraData cameraData;

    public static QualityPreset SelectedQuality => (QualityPreset)Mathf.Clamp(
        PlayerPrefs.GetInt(PreferenceKey, (int)QualityPreset.Balanced), 0, 2);
    public float AverageFrameMilliseconds { get; private set; }
    public float P95FrameMilliseconds { get; private set; }
    public int RecordedFrames => samples;

    public static WorldPresentationQuality Ensure(string sceneName)
    {
        GameObject host = GameObject.Find("WorldPresentationQuality");
        if (host == null) host = new GameObject("WorldPresentationQuality");
        WorldPresentationQuality quality = host.GetComponent<WorldPresentationQuality>();
        if (quality == null) quality = host.AddComponent<WorldPresentationQuality>();
        quality.Configure(sceneName);
        return quality;
    }

    public static void SetQuality(QualityPreset preset)
    {
        PlayerPrefs.SetInt(PreferenceKey, Mathf.Clamp((int)preset, 0, 2));
        PlayerPrefs.Save();
        WorldPresentationQuality active = FindAnyObjectByType<WorldPresentationQuality>();
        if (active != null) active.ApplyQuality();
    }

    public static QualityPreset CycleQuality()
    {
        QualityPreset next = (QualityPreset)(((int)SelectedQuality + 1) % 3);
        SetQuality(next);
        return next;
    }

    private void Configure(string sceneName)
    {
        if (profile != null && world == sceneName)
        {
            ApplyQuality();
            return;
        }
        world = sceneName;
        ReleaseProfile();
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "Lumen " + sceneName + " finishing (runtime)";
        profile.hideFlags = HideFlags.DontSave;
        volume = GetComponent<Volume>();
        if (volume == null) volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 20f;
        volume.sharedProfile = profile;

        bool home = sceneName.StartsWith("Home", StringComparison.OrdinalIgnoreCase);
        bool cafe = sceneName == "Cafestore";
        bool rome = sceneName == "HistoricalPlace";
        Tonemapping tonemapping = profile.Add<Tonemapping>();
        tonemapping.mode.Override(TonemappingMode.ACES);
        ColorAdjustments colors = profile.Add<ColorAdjustments>();
        colors.postExposure.Override(home ? 0.10f : 0.05f);
        colors.contrast.Override(home ? 8f : 4f);
        colors.saturation.Override(rome ? -3f : 2f);
        WhiteBalance whiteBalance = profile.Add<WhiteBalance>();
        whiteBalance.temperature.Override(cafe ? 5f : rome ? 4f : home ? -3f : 0f);

        bloom = profile.Add<Bloom>();
        bloom.threshold.Override(1.15f);
        bloom.intensity.Override(home ? 0.25f : 0.10f);
        bloom.scatter.Override(0.55f);
        Vignette vignette = profile.Add<Vignette>();
        vignette.intensity.Override(home ? 0.14f : 0.08f);
        vignette.smoothness.Override(0.5f);
        // First-person text, exhibits and facial detail stay clear during movement.
        profile.Add<MotionBlur>().intensity.Override(0f);
        profile.Add<DepthOfField>().mode.Override(DepthOfFieldMode.Off);
        profile.Add<ChromaticAberration>().intensity.Override(0f);
        ApplyQuality();
    }

    private void ApplyQuality()
    {
        Camera view = Camera.main;
        if (view == null) return;
        cameraData = view.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData == null) cameraData = view.gameObject.AddComponent<UniversalAdditionalCameraData>();
        QualityPreset quality = SelectedQuality;
        cameraData.volumeLayerMask |= 1 << gameObject.layer;
        cameraData.renderPostProcessing = quality != QualityPreset.Low;
        cameraData.antialiasing = quality == QualityPreset.Low
            ? AntialiasingMode.FastApproximateAntialiasing
            : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = quality == QualityPreset.High
            ? AntialiasingQuality.High : AntialiasingQuality.Medium;
        cameraData.dithering = quality != QualityPreset.Low;
        if (bloom != null)
        {
            bloom.highQualityFiltering.Override(quality == QualityPreset.High);
            bloom.downscale.Override(quality == QualityPreset.High ? BloomDownscaleMode.Half : BloomDownscaleMode.Quarter);
            bloom.maxIterations.Override(quality == QualityPreset.High ? 5 : 3);
        }
        // Each preset starts a fresh observation window; no unmeasured FPS claim.
        samples = 0;
        sampleIndex = 0;
        AverageFrameMilliseconds = 0f;
        P95FrameMilliseconds = 0f;
        nextSummary = Time.unscaledTime + 1f;
    }

    private void Update()
    {
        if (!Application.isFocused || Time.unscaledDeltaTime <= 0f) return;
        frameTimes[sampleIndex] = Time.unscaledDeltaTime * 1000f;
        sampleIndex = (sampleIndex + 1) % SampleCount;
        samples = Mathf.Min(samples + 1, SampleCount);
        if (Time.unscaledTime < nextSummary || samples < 30) return;
        nextSummary = Time.unscaledTime + 1f;
        float total = 0f;
        for (int i = 0; i < samples; i++)
        {
            total += frameTimes[i];
            sortedFrameTimes[i] = frameTimes[i];
        }
        Array.Sort(sortedFrameTimes, 0, samples);
        AverageFrameMilliseconds = total / samples;
        P95FrameMilliseconds = sortedFrameTimes[Mathf.Clamp(Mathf.CeilToInt(samples * 0.95f) - 1, 0, samples - 1)];
    }

    private void ReleaseProfile()
    {
        if (profile == null) return;
        if (volume != null) volume.sharedProfile = null;
        foreach (VolumeComponent component in profile.components)
            if (component != null) Destroy(component);
        Destroy(profile);
        profile = null;
    }

    private void OnDestroy()
    {
        ReleaseProfile();
    }
}

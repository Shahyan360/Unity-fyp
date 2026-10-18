using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Audio-driven articulation, blinking, and restrained emotional expression on facial blendshapes.</summary>
[DisallowMultipleComponent]
public sealed class CharacterFacialAnimation : MonoBehaviour
{
    private struct Shape
    {
        public SkinnedMeshRenderer renderer;
        public int index;
        public int channel;
    }
    private readonly List<Shape> shapes = new List<Shape>();
    private readonly float[] samples = new float[256];
    private readonly float[] values = new float[8];
    private AudioSource speech;
    private float nextBlink;
    private float blinkStarted = -10f;
    private float expressionUntil;
    private float smileTarget = 8f;
    private float browTarget;
    private float frownTarget;
    private float amplitude;
    private bool initialized;
    private float restSmile=7f;
    private float restBrow=2f;
    public int BoundShapeCount => shapes.Count;

    public void SetRestExpression(float smile,float brow)
    {
        restSmile=Mathf.Clamp(smile,0,18);restBrow=Mathf.Clamp(brow,0,8);
    }

    public void RefreshVisual()
    {
        shapes.Clear();
        foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Mesh mesh = renderer.sharedMesh;
            if (mesh == null) continue;
            for (int index = 0; index < mesh.blendShapeCount; index++)
            {
                string name = mesh.GetBlendShapeName(index).ToLowerInvariant();
                // One compatible family only: stacking ARKit, FACS and vendor shapes distorts the same vertices.
                if (!name.Contains("ak_")) continue;
                int channel = name.Contains("jawopen") ? 0 : name.Contains("mouthfunnel") ? 1 :
                    name.Contains("mouthpucker") ? 2 : name.Contains("eyeblink") ? 3 :
                    name.Contains("mouthsmile") ? 4 : name.Contains("browinnerup") ? 5 :
                    name.Contains("browdown") ? 6 : name.Contains("mouthfrown") ? 7 : -1;
                if (channel >= 0) shapes.Add(new Shape { renderer = renderer, index = index, channel = channel });
            }
        }
        initialized = true;
        nextBlink = Time.unscaledTime + UnityEngine.Random.Range(1.3f, 4.2f);
    }

    public void SetEmotion(string emotion)
    {
        string value = (emotion ?? string.Empty).Trim().ToLowerInvariant();
        smileTarget = value == "excited" ? 36f : value == "confident" ? 19f : value == "concerned" ? 0f : 10f;
        browTarget = value == "excited" ? 18f : value == "concerned" ? 28f : value == "thinking" ? 12f : 3f;
        frownTarget = value == "concerned" ? 14f : 0f;
        expressionUntil = Time.unscaledTime + 9f;
    }

    public void BeginSpeech(AudioSource source) { speech = source; }
    public void EndSpeech() { speech = null; amplitude = 0f; }

    private void LateUpdate()
    {
        if (!initialized) RefreshVisual();
        if (shapes.Count == 0) return;
        float now = Time.unscaledTime;
        float step = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f);
        bool speaking = speech != null && speech.isPlaying;
        float targetAmplitude = 0f;
        if (speaking)
        {
            speech.GetOutputData(samples, 0);
            float square = 0f;
            for (int i = 0; i < samples.Length; i++) square += samples[i] * samples[i];
            targetAmplitude = Mathf.Clamp01((Mathf.Sqrt(square / samples.Length) - 0.006f) * 11f);
        }
        amplitude = Mathf.Lerp(amplitude, targetAmplitude, step);
        values[0] = amplitude * 72f;
        // RMS follows actual audio/silence. The secondary mouth shapes approximate articulation, not phoneme alignment.
        values[1] = amplitude * (12f + 12f * Mathf.Sin(now * 8.2f));
        values[2] = amplitude * (7f + 7f * Mathf.Sin(now * 5.1f + 1.2f));
        if (now >= nextBlink)
        {
            blinkStarted = now;
            nextBlink = now + UnityEngine.Random.Range(2.8f, 5.8f);
        }
        float blink = (now - blinkStarted) / 0.18f;
        values[3] = blink >= 0f && blink <= 1f ? Mathf.Sin(blink * Mathf.PI) * 100f : 0f;
        bool expression = now < expressionUntil;
        values[4] = Mathf.Lerp(values[4], expression ? smileTarget : restSmile, step * 0.25f);
        values[5] = Mathf.Lerp(values[5], expression ? browTarget : restBrow, step * 0.25f);
        values[6] = Mathf.Lerp(values[6], expression && frownTarget > 0f ? 8f : 0f, step * 0.25f);
        values[7] = Mathf.Lerp(values[7], expression ? frownTarget : 0f, step * 0.25f);
        foreach (Shape shape in shapes)
            if (shape.renderer != null) shape.renderer.SetBlendShapeWeight(shape.index, values[shape.channel]);
    }

    private void OnDisable()
    {
        EndSpeech();
        foreach (Shape shape in shapes)
            if (shape.renderer != null) shape.renderer.SetBlendShapeWeight(shape.index, 0f);
    }
}

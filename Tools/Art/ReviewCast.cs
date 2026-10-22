using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>Run with Pipeline run_script, entry LumenCastReview.Run. No scene saves.</summary>
public static class LumenCastReview
{
    public static string Corrected()=>At("Cast-Corrected");
    public static string Final()=>At("Cast-Final");
    private static string At(string directory)
    {
        string previous=Environment.GetEnvironmentVariable("LUMEN_CAST_REVIEW");
        Environment.SetEnvironmentVariable("LUMEN_CAST_REVIEW",Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds/Finalization-20261022/"+directory)));
        try {return Run();} finally {Environment.SetEnvironmentVariable("LUMEN_CAST_REVIEW",previous);}
    }
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        string output = Path.GetFullPath(Environment.GetEnvironmentVariable("LUMEN_CAST_REVIEW")
            ?? Path.Combine(Application.dataPath, "../Builds/Finalization-20261022/Cast-Baseline"));
        if (Directory.Exists(output)) throw new InvalidOperationException("Preserve existing character evidence.");
        Directory.CreateDirectory(output);
        var inventory = new List<string> { "character\trenderer\tvertices\ttriangles\tbones\tblendshapes\tinvalid_materials" };
        foreach (string character in new[] { "Kai", "Orion", "Theo", "Aurelius", "Lucius", "Maya" })
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LumenAgents/" + character + ".prefab");
            if (asset == null) throw new InvalidOperationException("Missing dressed prefab " + character);
            var preview = new PreviewRenderUtility();
            try
            {
                var instance = UnityEngine.Object.Instantiate(asset);
                instance.transform.position = Vector3.zero;
                preview.AddSingleGO(instance);
                foreach (var animator in instance.GetComponentsInChildren<Animator>()) animator.enabled = false;
                var renderers = instance.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh = skin.sharedMesh;
                    inventory.Add(string.Join("\t", character, skin.name, mesh.vertexCount, mesh.triangles.Length / 3,
                        skin.bones.Length, mesh.blendShapeCount,
                        skin.sharedMaterials.Count(m => m == null || m.shader == null || !m.shader.isSupported)));
                    if (mesh.boneWeights.Length != mesh.vertexCount || mesh.normals.Length != mesh.vertexCount || mesh.uv.Length != mesh.vertexCount)
                        throw new InvalidOperationException(character + " has incomplete mesh streams: " + skin.name);
                }
                var camera = preview.camera;
                camera.orthographic = true;
                camera.orthographicSize = bounds.size.y * .58f;
                camera.nearClipPlane = .01f;
                camera.farClipPlane = 1000;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.16f, .18f, .21f);
                preview.lights[0].intensity = 1.2f;
                preview.lights[0].transform.rotation = Quaternion.Euler(35, 35, 0);
                preview.lights[1].intensity = .8f;
                preview.lights[1].transform.rotation = Quaternion.Euler(15, 210, 0);
                preview.ambientColor = new Color(.45f, .45f, .45f);
                foreach (int angle in new[] { 0, 90, 180, 315 })
                {
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                    camera.transform.position = bounds.center + direction * Mathf.Max(4, bounds.size.y * 3);
                    camera.transform.LookAt(bounds.center);
                    preview.BeginPreview(new Rect(0, 0, 512, 768), GUIStyle.none);
                    preview.Render(true);
                    var texture = preview.EndPreview();
                    var temporary = RenderTexture.GetTemporary(512, 768, 0);
                    var old = RenderTexture.active;
                    try
                    {
                        Graphics.Blit(texture, temporary);
                        RenderTexture.active = temporary;
                        var image = new Texture2D(512, 768, TextureFormat.RGB24, false);
                        image.ReadPixels(new Rect(0, 0, 512, 768), 0, 0);
                        image.Apply();
                        File.WriteAllBytes(Path.Combine(output, character + "-" + angle + ".png"), image.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(image);
                    }
                    finally { RenderTexture.active = old; RenderTexture.ReleaseTemporary(temporary); }
                }
            }
            finally { preview.Cleanup(); }
        }
        File.WriteAllLines(Path.Combine(output, "mesh-inventory.tsv"), inventory);
        return output;
    }
}

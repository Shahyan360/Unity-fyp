using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class InspectLumenCast
{
    public static string Run()
    {
        var lines = new List<string>();
        foreach (string style in new[] { "OpeningHero", "RomanCitizen", "CafeHost" })
        {
            var d = Resources.Load<GameObject>("TailoredCharacters/" + style).GetComponent<TailoredCharacterWardrobe>();
            var source = Resources.Load<GameObject>("ScenarioCharacters/" + d.character);
            var skin = source.GetComponentInChildren<SkinnedMeshRenderer>();
            lines.Add(style + " bounds=" + d.garments.bounds + " source scale=" + source.transform.lossyScale);
            foreach (string bone in new[] { "Bip01 Pelvis", "Bip01 Neck", "Bip01 Head" })
                lines.Add(bone + " " + source.transform.InverseTransformPoint(skin.bones.First(b => b.name == bone).position));
            for (int i = 0; i < d.garmentSurfaces.Length; i++)
            {
                var points = d.garments.GetTriangles(i).Distinct().Select(n => source.transform.InverseTransformPoint(skin.transform.TransformPoint(d.garments.vertices[n]))).ToArray();
                lines.Add(i + " " + d.garmentSurfaces[i].name + " min=" + new Vector3(points.Min(p=>p.x),points.Min(p=>p.y),points.Min(p=>p.z)) + " max=" + new Vector3(points.Max(p=>p.x),points.Max(p=>p.y),points.Max(p=>p.z)));
            }
            for (int i = 0; i < d.nativeSurfaces.Length; i++)
                lines.Add("native " + i + " " + d.nativeSurfaces[i].name + " " + AssetDatabase.GetAssetPath(d.nativeSurfaces[i].mainTexture));
        }
        string path = Path.Combine(Application.dataPath, "../Builds/Finalization-20261022/cast-inspection.txt");
        File.WriteAllLines(path, lines);
        return path;
    }
}

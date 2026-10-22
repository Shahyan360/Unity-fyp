using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class VerifyLumenCast
{
    public static string Run()
    {
        var log=new List<string>();
        foreach(string relative in new[]{"OpeningHero/Garments.asset","RomanCitizen/Garments.asset","CafeHost/FacialBody.asset","CafeHost/Garments.asset"})
        {
            string backup="Builds/Finalization-20261022/Cast-Mesh-Backup/"+relative.Replace('/','-');
            var loaded=UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(backup);
            try
            {
                var before=loaded.OfType<Mesh>().Single();
                var after=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/TailoredCharacters/"+relative);
                if(before.vertexCount!=after.vertexCount||!before.boneWeights.SequenceEqual(after.boneWeights)
                    ||!before.bindposes.SequenceEqual(after.bindposes)||!before.uv.SequenceEqual(after.uv)
                    ||!before.normals.SequenceEqual(after.normals)||before.blendShapeCount!=after.blendShapeCount)
                    throw new InvalidOperationException("Rig/UV/normal buffers changed: "+relative);
                var b=new Vector3[before.vertexCount];var bn=new Vector3[b.Length];var bt=new Vector3[b.Length];
                var a=new Vector3[b.Length];var an=new Vector3[b.Length];var at=new Vector3[b.Length];
                for(int shape=0;shape<before.blendShapeCount;shape++)
                {
                    if(before.GetBlendShapeName(shape)!=after.GetBlendShapeName(shape)||before.GetBlendShapeFrameCount(shape)!=after.GetBlendShapeFrameCount(shape))
                        throw new InvalidOperationException("Facial channel changed.");
                    for(int frame=0;frame<before.GetBlendShapeFrameCount(shape);frame++)
                    {
                        before.GetBlendShapeFrameVertices(shape,frame,b,bn,bt);after.GetBlendShapeFrameVertices(shape,frame,a,an,at);
                        if(!b.SequenceEqual(a)||!bn.SequenceEqual(an)||!bt.SequenceEqual(at)||before.GetBlendShapeFrameWeight(shape,frame)!=after.GetBlendShapeFrameWeight(shape,frame))
                            throw new InvalidOperationException("Facial frame buffer changed.");
                    }
                }
                if(relative.EndsWith("FacialBody.asset")&&!before.vertices.SequenceEqual(after.vertices))
                    throw new InvalidOperationException("Native facial positions changed.");
                log.Add("PASS "+relative+": weights, bindposes, UVs, normals and "+after.blendShapeCount+" morph channels match baseline exactly.");
            }
            finally {foreach(var item in loaded)UnityEngine.Object.DestroyImmediate(item);}
        }
        string path="Builds/Finalization-20261022/cast-buffer-verification.txt";
        File.WriteAllLines(path,log);return string.Join("\n",log);
    }
}

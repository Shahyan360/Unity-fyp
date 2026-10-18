using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Exports the exact imported rest anatomy for fitted Blender garments.</summary>
public static class LumenCharacterTailoring
{
    private const string Working = "D:/LumenProject/Art/CharacterTailoring";
    [Serializable] public sealed class Surface { public string name, color, normal;public int[] triangles; }
    [Serializable] public sealed class Bone { public string name;public Vector3 position; }
    [Serializable] public sealed class Weight { public int[] bones;public float[] values; }
    [Serializable] public sealed class Reference
    {
        public string character; public Vector3[] vertices; public Vector2[] uv;
        public Surface[] surfaces; public Bone[] skeleton; public Weight[] weights;
    }
    public static void ExportReferences()
    {
        Directory.CreateDirectory(Working+"/Reference");
        foreach(string name in new[]{"Kai","Orion","Theo","Marcus"})
        {
            var prefab=Resources.Load<GameObject>("ScenarioCharacters/"+name);
            var skin=prefab.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;
            var data=new Reference { character=name,
                vertices=mesh.vertices.Select(v=>prefab.transform.InverseTransformPoint(skin.transform.TransformPoint(v))).ToArray(),
                uv=mesh.uv,
                skeleton=skin.bones.Select(b=>new Bone{name=b.name,position=prefab.transform.InverseTransformPoint(b.position)}).ToArray(),
                weights=mesh.boneWeights.Select(w=>new Weight{bones=new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3},values=new[]{w.weight0,w.weight1,w.weight2,w.weight3}}).ToArray(),
                surfaces=skin.sharedMaterials.Select((m,i)=>new Surface{name=m.name,triangles=mesh.GetTriangles(i),
                    color=AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")),normal=AssetDatabase.GetAssetPath(m.GetTexture("_BumpMap"))}).ToArray()
            };
            File.WriteAllText(Working+"/Reference/"+name+".json",JsonUtility.ToJson(data));
        }
    }

    [Serializable] public sealed class Coverage { public int submesh,triangle; }
    [Serializable] public sealed class Vertex
    {
        public Vector3 position,normal;public Vector2 uv;public int[] bones;public float[] weights;
    }
    [Serializable] public sealed class Part
    {
        public string name,material,color;public bool useNativeNormal;public float roughness,metallic;public float[] baseColor;
        public Vertex[] vertices;public int[] triangles;
    }
    [Serializable] public sealed class Garment { public string style,character;public Part[] parts;public Coverage[] covered; }

    public static string Build(string style)
    {
        var data=JsonUtility.FromJson<Garment>(File.ReadAllText(Working+"/"+style+"/"+style+".json"));
        string folder="Assets/Art/TailoredCharacters/"+style;
        string resource="Assets/Resources/TailoredCharacters";
        Directory.CreateDirectory(folder);Directory.CreateDirectory(resource);
        foreach(string file in Directory.GetFiles(Working+"/"+style,"*.png"))
        {
            if(file.EndsWith("-full.png")||file.EndsWith("-portrait.png"))continue;
            string dest=folder+"/"+Path.GetFileName(file);File.Copy(file,dest,true);
            AssetDatabase.ImportAsset(dest,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(dest);
            importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
        var source=Resources.Load<GameObject>("ScenarioCharacters/"+data.character);
        var body=source.GetComponentInChildren<SkinnedMeshRenderer>();
        Matrix4x4 toBody=body.transform.worldToLocalMatrix*source.transform.localToWorldMatrix;
        Matrix4x4 normalToBody=toBody.inverse.transpose;
        var vertices=new System.Collections.Generic.List<Vector3>();var normals=new System.Collections.Generic.List<Vector3>();
        var uvs=new System.Collections.Generic.List<Vector2>();var weights=new System.Collections.Generic.List<BoneWeight>();
        var surfaces=new System.Collections.Generic.List<Material>();
        var triangles=new System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<int>>();
        var order=new System.Collections.Generic.List<string>();
        var unique=new System.Collections.Generic.Dictionary<string,int>();
        foreach(var part in data.parts)
        {
            if(!triangles.ContainsKey(part.material))
            {
                triangles[part.material]=new System.Collections.Generic.List<int>();order.Add(part.material);
                string materialPath=folder+"/"+part.material+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}
                material.color=Color.white;
                if(!string.IsNullOrEmpty(part.color))material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+part.color);
                else material.color=new Color(part.baseColor[0],part.baseColor[1],part.baseColor[2]);
                material.SetFloat("_Smoothness",1-part.roughness);material.SetFloat("_Metallic",part.metallic);
                if(part.useNativeNormal&&!part.material.Contains("hair"))
                {
                    var native=body.sharedMaterials.FirstOrDefault(m=>m.name.Contains("_body"));
                    if(native!=null){material.SetTexture("_BumpMap",native.GetTexture("_BumpMap"));material.SetFloat("_BumpScale",.65f);material.EnableKeyword("_NORMALMAP");}
                }
                else {material.SetTexture("_BumpMap",null);material.DisableKeyword("_NORMALMAP");}
                material.SetFloat("_Cull",0);surfaces.Add(material);
            }
            var remap=new int[part.vertices.Length];
            for(int i=0;i<part.vertices.Length;i++)
            {
                var vertex=part.vertices[i];
                string key=vertex.position.ToString("F5")+vertex.normal.ToString("F4")+vertex.uv.ToString("F5")+
                    string.Join(",",vertex.bones)+string.Join(",",vertex.weights.Select(w=>w.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)));
                if(!unique.TryGetValue(key,out int index))
                {
                    index=vertices.Count;unique[key]=index;
                    // The custom coat extension needs ease over trouser folds
                    // in a walking pose, beyond the original jacket's rest fit.
                    if(part.name=="Split tailored coat tails")
                    {
                        var outward=new Vector3(vertex.position.x,0,vertex.position.z).normalized;
                        vertex.position+=outward*.018f;
                    }
                    vertices.Add(toBody.MultiplyPoint3x4(vertex.position));normals.Add(normalToBody.MultiplyVector(vertex.normal).normalized);uvs.Add(vertex.uv);
                    weights.Add(new BoneWeight{boneIndex0=vertex.bones[0],boneIndex1=vertex.bones[1],boneIndex2=vertex.bones[2],boneIndex3=vertex.bones[3],
                        weight0=vertex.weights[0],weight1=vertex.weights[1],weight2=vertex.weights[2],weight3=vertex.weights[3]});
                }
                remap[i]=index;
            }
            // Blender exports CCW surfaces with outward normals. Unity's front
            // faces are clockwise; preserve the normal and reverse winding.
            // Without this, thin panels render black from their back faces.
            for(int i=0;i<part.triangles.Length;i+=3)
            {
                triangles[part.material].Add(remap[part.triangles[i+2]]);
                triangles[part.material].Add(remap[part.triangles[i+1]]);
                triangles[part.material].Add(remap[part.triangles[i]]);
            }
        }
        var mesh=new Mesh{name=style+" native weighted clothing",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.boneWeights=weights.ToArray();mesh.bindposes=body.sharedMesh.bindposes;
        mesh.subMeshCount=order.Count;for(int i=0;i<order.Count;i++)mesh.SetTriangles(triangles[order[i]],i);
        mesh.RecalculateBounds();mesh.RecalculateTangents();
        var masked=UnityEngine.Object.Instantiate(body.sharedMesh);masked.name=style+" facial body mask";
        for(int sub=0;sub<masked.subMeshCount;sub++)
        {
            var covered=new HashSet<int>(data.covered.Where(c=>c.submesh==sub).Select(c=>c.triangle));
            var original=body.sharedMesh.GetTriangles(sub);var kept=new System.Collections.Generic.List<int>();
            for(int i=0;i<original.Length;i+=3)if(!covered.Contains(i/3)){kept.Add(original[i]);kept.Add(original[i+1]);kept.Add(original[i+2]);}
            masked.SetTriangles(kept,sub);
        }
        int faceCount=masked.blendShapeCount;
        SaveMesh(mesh,folder+"/Garments.asset");SaveMesh(masked,folder+"/FacialBody.asset");
        var root=new GameObject(style+" tailored wardrobe");var definition=root.AddComponent<TailoredCharacterWardrobe>();
        definition.character=data.character;definition.garments=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/Garments.asset");
        definition.maskedBody=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/FacialBody.asset");definition.garmentSurfaces=surfaces.ToArray();definition.nativeSurfaces=body.sharedMaterials;
        PrefabUtility.SaveAsPrefabAsset(root,resource+"/"+style+".prefab");UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
        // A complete dressed prefab is also available for ordinary artist/scene
        // use; the runtime definition above keeps world switching lightweight.
        const string dressedFolder="Assets/Prefabs/LumenAgents";
        Directory.CreateDirectory(dressedFolder);
        var dressed=UnityEngine.Object.Instantiate(source);dressed.name=data.character;
        var dressedBody=dressed.GetComponentInChildren<SkinnedMeshRenderer>();
        dressedBody.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/FacialBody.asset");
        for(int i=0;i<dressedBody.sharedMesh.blendShapeCount;i++)
            if(dressedBody.sharedMesh.GetBlendShapeName(i).ToLowerInvariant().Contains("ak_mouthsmile"))dressedBody.SetBlendShapeWeight(i,data.character=="Kai"?13:10);
        var clothing=new GameObject("Tailored clothing");clothing.transform.SetParent(dressedBody.transform,false);
        clothing.AddComponent<TailoredClothingMesh>();
        var dressedSkin=clothing.AddComponent<SkinnedMeshRenderer>();dressedSkin.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/Garments.asset");
        dressedSkin.bones=dressedBody.bones;dressedSkin.rootBone=dressedBody.rootBone;dressedSkin.sharedMaterials=surfaces.ToArray();
        Bounds dressedBounds=dressedBody.localBounds;dressedBounds.Expand(.45f);dressedSkin.localBounds=dressedBounds;
        PrefabUtility.SaveAsPrefabAsset(dressed,dressedFolder+"/"+data.character+".prefab");UnityEngine.Object.DestroyImmediate(dressed);AssetDatabase.SaveAssets();
        string result=style+": "+vertices.Count+" vertices, "+triangles.Values.Sum(t=>t.Count/3)+" triangles, "+order.Count+" clothing surfaces, "+faceCount+" facial shapes";
        File.WriteAllText(Working+"/"+style+"/unity-import.txt",result);Debug.Log(result);return result;
    }
    private static void SaveMesh(Mesh mesh,string path)
    {
        var current=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(current==null)AssetDatabase.CreateAsset(mesh,path);
        else{EditorUtility.CopySerialized(mesh,current);UnityEngine.Object.DestroyImmediate(mesh);}
    }
}

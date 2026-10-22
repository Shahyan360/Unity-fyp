using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;

/// <summary>Apply once to the transferred baseline, preserving asset GUIDs and rig streams.</summary>
public static class CorrectLumenCast
{
    [Serializable] private class AssetHash { public string path, sha256; }
    [Serializable] private class CapUV { public float topMinV, crownMinU, crownMaxV; }
    [Serializable] private class Baseline { public AssetHash[] assets; public CapUV mayaCapUV; }
    private static string Hash(string path) { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(path).Replace("\r\n","\n")))).Replace("-", "").ToLowerInvariant(); }
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        var json=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("Tools/Art/cast-correction-baseline.json"));
        var baseline=new Baseline {
            assets=json["assets"].Select(a=>new AssetHash {path=(string)a["path"],sha256=(string)a["sha256"]}).ToArray(),
            mayaCapUV=new CapUV {topMinV=(float)json["mayaCapUV"]["topMinV"],crownMinU=(float)json["mayaCapUV"]["crownMinU"],crownMaxV=(float)json["mayaCapUV"]["crownMaxV"]}
        };
        string backup="Builds/Finalization-20261022/Cast-Mesh-Backup";
        if(Directory.Exists(backup))throw new InvalidOperationException("Correction already attempted; preserve it and review the receipt.");
        foreach(var item in baseline.assets)
            if(Hash(item.path)!=item.sha256)throw new InvalidOperationException("Baseline differs; refusing repeated/destructive correction: "+item.path);
        Directory.CreateDirectory(backup);
        foreach(var item in baseline.assets) {File.Copy(item.path,Path.Combine(backup,item.path.Split('/')[3]+"-"+Path.GetFileName(item.path)));File.Copy(item.path+".meta",Path.Combine(backup,item.path.Split('/')[3]+"-"+Path.GetFileName(item.path)+".meta"));}
        var log=new List<string>();
        Tuck("OpeningHero", "OpeningHero fitted trousers", .84f, .61f, .72f, log);
        Tuck("RomanCitizen", "RomanCitizen lower cloth", .80f, .51f, .64f, log);
        RemoveCap(baseline.mayaCapUV,log);
        AssetDatabase.SaveAssets();
        foreach(var item in baseline.assets)log.Add(item.path+" sha256="+Hash(item.path)+" guid="+AssetDatabase.AssetPathToGUID(item.path));
        File.WriteAllLines(backup+"/correction-receipt.txt",log);
        return string.Join("\n",log);
    }
    private static void Save(Mesh mesh, Mesh target)
    {
        if(mesh.blendShapeCount!=target.blendShapeCount)throw new InvalidOperationException("Facial shape count changed.");
        mesh.name=target.name;
        target.Clear(false);EditorUtility.CopySerialized(mesh,target);target.UploadMeshData(false);EditorUtility.SetDirty(target);UnityEngine.Object.DestroyImmediate(mesh);
    }
    public static string CloseMayaCrown()
    {
        const string path="Assets/Art/TailoredCharacters/CafeHost/Garments.asset";
        string receipt="Builds/Finalization-20261022/Cast-Mesh-Backup/maya-crown-receipt.txt";
        if(File.Exists(receipt))throw new InvalidOperationException("Crown correction already applied.");
        var baseline=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("Tools/Art/cast-correction-baseline.json"));
        string expected=(string)baseline["assets"].First(a=>(string)a["path"]==path)["sha256"];
        if(Hash(path)!=expected)throw new InvalidOperationException("Crown baseline differs; refusing repeat application.");
        var d=Resources.Load<GameObject>("TailoredCharacters/CafeHost").GetComponent<TailoredCharacterWardrobe>();
        var source=Resources.Load<GameObject>("ScenarioCharacters/Maya");
        var skin=source.GetComponentInChildren<SkinnedMeshRenderer>();
        Matrix4x4 toRoot=source.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;
        int sub=Array.FindIndex(d.garmentSurfaces,m=>m.name=="Maya natural dark bob crown");
        if(sub<0)throw new InvalidOperationException("Missing authored crown.");
        string backup="Builds/Finalization-20261022/Cast-Mesh-Backup/CafeHost-Garments.asset";
        if(!File.Exists(backup))File.Copy(path,backup);
        var mesh=UnityEngine.Object.Instantiate(d.garments);var vertices=mesh.vertices;int count=0;
        foreach(int i in mesh.GetTriangles(sub).Distinct())
        {
            Vector3 p=toRoot.MultiplyPoint3x4(vertices[i]);
            float amount=1-Mathf.InverseLerp(1.60f,1.685f,p.y);
            p.y-=.05f*amount;p.x*=1+.06f*amount;p.z=(p.z+.04f)*(1+.08f*amount)-.04f;
            vertices[i]=toRoot.inverse.MultiplyPoint3x4(p);count++;
        }
        mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateTangents();Save(mesh,d.garments);AssetDatabase.SaveAssets();
        File.WriteAllText(receipt,"Extended retained dark crown to close the former cap-rim seam. Vertices: "+count+"; sha256="+Hash(path));
        return File.ReadAllText(receipt);
    }
    public static string RestoreMeshNames()
    {
        foreach(string relative in new[]{"OpeningHero/Garments.asset","RomanCitizen/Garments.asset","CafeHost/FacialBody.asset","CafeHost/Garments.asset"})
        {
            string backup="Builds/Finalization-20261022/Cast-Mesh-Backup/"+relative.Replace('/','-');
            var loaded=UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(backup);
            try {
                var before=loaded.OfType<Mesh>().Single();
                var current=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/TailoredCharacters/"+relative);
                current.name=before.name;EditorUtility.SetDirty(current);
            } finally {foreach(var item in loaded)UnityEngine.Object.DestroyImmediate(item);}
        }
        AssetDatabase.SaveAssets();return "Original mesh names restored; geometry and GUIDs unchanged.";
    }
    private static void Tuck(string style,string surface,float scale,float bottom,float full,List<string> log)
    {
        var d=Resources.Load<GameObject>("TailoredCharacters/"+style).GetComponent<TailoredCharacterWardrobe>();
        var native=Resources.Load<GameObject>("ScenarioCharacters/"+d.character);
        var skin=native.GetComponentInChildren<SkinnedMeshRenderer>();
        Matrix4x4 toRoot=native.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;
        int sub=Array.FindIndex(d.garmentSurfaces,m=>m.name==surface);if(sub<0)throw new InvalidOperationException(surface);
        var mesh=UnityEngine.Object.Instantiate(d.garments);var vertices=mesh.vertices;int changed=0;
        foreach(int i in mesh.GetTriangles(sub).Distinct())
        {
            Vector3 p=toRoot.MultiplyPoint3x4(vertices[i]);
            float amount=Mathf.SmoothStep(0,1,Mathf.InverseLerp(bottom,full,p.y));
            if(amount<=0)continue;
            float factor=Mathf.Lerp(1,scale,amount);p.x*=factor;p.z*=factor;
            vertices[i]=toRoot.inverse.MultiplyPoint3x4(p);changed++;
        }
        mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateTangents();
        // Native normals, UVs, topology, bind poses and weights are retained.
        Save(mesh,d.garments);log.Add(style+": tucked "+changed+" hidden trouser vertices; no expanded garment shells.");
    }
    private static void RemoveCap(CapUV region,List<string> log)
    {
        var d=Resources.Load<GameObject>("TailoredCharacters/CafeHost").GetComponent<TailoredCharacterWardrobe>();
        int sub=Array.FindIndex(d.nativeSurfaces,m=>m.name.Contains("head"));
        var mesh=UnityEngine.Object.Instantiate(d.maskedBody);var uv=mesh.uv;
        int[] triangles=mesh.GetTriangles(sub);var kept=new List<int>();int removed=0;
        for(int i=0;i<triangles.Length;i+=3)
        {
            Vector2 p=(uv[triangles[i]]+uv[triangles[i+1]]+uv[triangles[i+2]])/3;
            bool cap=p.y>region.topMinV||(p.x>region.crownMinU&&p.y<region.crownMaxV);
            if(cap){removed++;continue;}
            kept.Add(triangles[i]);kept.Add(triangles[i+1]);kept.Add(triangles[i+2]);
        }
        if(removed==0||removed>300)throw new InvalidOperationException("Unexpected remaining cap triangle count: "+removed);
        mesh.SetTriangles(kept,sub);mesh.RecalculateBounds();
        Save(mesh,d.maskedBody);log.Add("Maya: removed "+removed+" cap-island triangles. Existing dark crown retained; all facial vertex/morph buffers unchanged.");
    }
}

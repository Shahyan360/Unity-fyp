using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class LumenPropAssetBuilder
{
    [MenuItem("Lumen/Art/Build Blender Collection")]
    public static void BuildAll()
    {
        Build(new[]{"EspressoStation","DiscoveryOrrery","RomanStudy"});
    }

    [MenuItem("Lumen/Art/Build Compact Museum Collection")]
    public static void BuildCompact()
    {
        Build(new[]{"MuseumCourtyard","FossilCabinet","ForumTable","RomanArtifactCase"});
    }

    [MenuItem("Lumen/Art/Build Roman City Collection")]
    public static void BuildCity()
    {
        Build(new[]{"RomanMarketStall","RomanArcadeRestoration"});
    }

    [MenuItem("Lumen/Art/Build Street Greenery")]
    public static void BuildGreenery(){Build(new[]{"CityOlivePlanter"});}

    [MenuItem("Lumen/Art/Build Temple Restoration")]
    public static void BuildTemple(){Build(new[]{"RomanTempleRestoration"});}

    [MenuItem("Lumen/Art/Build Final Environment Collection")]
    public static void BuildFinal()
    {
        Build(new[]{"RomanInsula","TheropodSkeleton","HornedDinosaurSkeleton"});
        foreach(string name in new[]{"OpeningHeroOutfit","MuseumMysticOutfit","ProfessorOutfit","RomanCitizenOutfit","RomanGuardOutfit"})
        {
            BuildWardrobe(name);
        }
        AssetDatabase.SaveAssets();
    }

    public static void BuildWardrobe(string name)
    {
        GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/LumenCollection/"+name+".glb");
        if(source==null)throw new System.InvalidOperationException("Missing wardrobe export: "+name);
        GameObject instance=Object.Instantiate(source);
        PrefabUtility.SaveAsPrefabAsset(instance,"Assets/Resources/LumenCollection/"+name+".prefab");
        Object.DestroyImmediate(instance);
    }

    private static void Build(string[] names)
    {
        const string output="Assets/Resources/LumenCollection";
        Directory.CreateDirectory(output);
        foreach(string name in names)
        {
            GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/LumenCollection/"+name+".glb");
            if(source==null) throw new System.InvalidOperationException("Import the Blender GLB first: "+name);
            GameObject instance=Object.Instantiate(source);
            instance.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            GameObject root=new GameObject(name);
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();
                if(renderer==null || filter.sharedMesh==null) continue;
                for(int i=0;i<filter.sharedMesh.subMeshCount;i++)
                {
                    Material mat=renderer.sharedMaterials[Mathf.Min(i,renderer.sharedMaterials.Length-1)];
                    if(!groups.TryGetValue(mat,out var group)) { group=new List<CombineInstance>(); groups[mat]=group; }
                    group.Add(new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=i,transform=filter.transform.localToWorldMatrix});
                }
            }
            int index=0;
            foreach(var pair in groups)
            {
                Mesh mesh=new Mesh{name=name+"_"+index,indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(pair.Value.ToArray(),true,true);
                string path=output+"/"+name+"_"+index+".asset";
                Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing!=null) { EditorUtility.CopySerialized(mesh,existing); Object.DestroyImmediate(mesh); mesh=existing; }
                else AssetDatabase.CreateAsset(mesh,path);
                GameObject part=new GameObject(pair.Key.name,typeof(MeshFilter),typeof(MeshRenderer));
                part.transform.SetParent(root.transform,false); part.GetComponent<MeshFilter>().sharedMesh=mesh;
                part.GetComponent<MeshRenderer>().sharedMaterial=pair.Key;
                index++;
            }
            var bounds=new Bounds(); bool first=true;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>()) { if(first) {bounds=renderer.bounds;first=false;} else bounds.Encapsulate(renderer.bounds); }
            // The courtyard has a genuine open doorway and level path. Its
            // structural colliders are supplied by the gallery, never one solid box.
            if(name!="MuseumCourtyard" && name!="RomanArcadeRestoration" && name!="RomanInsula" && name!="CityOlivePlanter" && name!="RomanTempleRestoration")
            {
                var collider=root.AddComponent<BoxCollider>(); collider.center=bounds.center; collider.size=bounds.size;
                if(name=="RomanMarketStall")
                {
                    collider.center=new Vector3(0f,.49f,-.38f);collider.size=new Vector3(3.28f,1f,.96f);
                    foreach(float x in new[]{-1.55f,1.55f})foreach(float z in new[]{-.85f,.85f})
                    {
                        var post=root.AddComponent<BoxCollider>();post.center=new Vector3(x,1.42f,z);post.size=new Vector3(.13f,2.84f,.13f);
                    }
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root,output+"/"+name+".prefab");
            Object.DestroyImmediate(instance); Object.DestroyImmediate(root);
            Debug.Log("Blender collection built: "+name+"; material groups="+index+"; dimensions="+bounds.size);
        }
        AssetDatabase.SaveAssets();
    }

    public static void BuildFinalizedExhibits()=>Build(new[]{"TheropodSkeleton","HornedDinosaurSkeleton","ArenaPracticeSword"});
}

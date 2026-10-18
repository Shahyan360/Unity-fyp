using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Repeatable, source-preserving Rocketbox import and URP prefab setup.</summary>
public static class LumenCharacterAssetBuilder
{
    private const string Source = "Assets/Art/Rocketbox";
    private const string Output = "Assets/Resources/ScenarioCharacters";

    [MenuItem("Lumen/Art/Build Scenario Characters")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(Output);
        foreach (string name in new[] { "Ada", "Maya", "Marcus" }) Build(name);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Lumen/Art/Build Distinct Final Hosts")]
    public static void BuildFinalHosts()
    {
        foreach(string name in new[]{"Kai","Orion","Theo"})Build(name);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Lumen/Art/Repair Facial Retargeting")]
    public static void RepairFacialRetargeting()
    {
        foreach(string name in new[]{"Ada","Maya","Marcus","Kai","Orion","Theo"})Build(name);
        AssetDatabase.SaveAssets();
    }

    private static void Build(string name)
    {
        string folder = Source + "/" + name;
        string path = folder + "/" + name + ".fbx";
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("Missing character source: " + path);
        importer.importBlendShapes = true;
        importer.isReadable = name!="Ada"&&name!="Maya"; // Shared CPU mask for articulated wardrobes/audience baking.
        importer.importCameras = false; importer.importLights = false;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        HumanDescription description = importer.humanDescription;
        var mapping = new Dictionary<string,string>
        {
            {"Hips","Bip01 Pelvis"}, {"Spine","Bip01 Spine"}, {"Chest","Bip01 Spine1"}, {"UpperChest","Bip01 Spine2"},
            {"Neck","Bip01 Neck"}, {"Head","Bip01 Head"}
        };
        // Body clips have different optional facial-bone neutral axes. Mapping
        // them rotated Rocketbox's jaw by ~90 degrees and rolled its eyes.
        // Facial blendshapes own articulation; preserve these bones' source pose.
        foreach (string side in new[] { "Left", "Right" })
        {
            string prefix = "Bip01 " + (side == "Left" ? "L " : "R ");
            mapping[side+"Shoulder"] = prefix+"Clavicle";
            mapping[side+"UpperArm"] = prefix+"UpperArm"; mapping[side+"LowerArm"] = prefix+"Forearm"; mapping[side+"Hand"] = prefix+"Hand";
            mapping[side+"UpperLeg"] = prefix+"Thigh"; mapping[side+"LowerLeg"] = prefix+"Calf"; mapping[side+"Foot"] = prefix+"Foot"; mapping[side+"Toes"] = prefix+"Toe0";
            string[] fingers={"Thumb","Index","Middle","Ring","Little"};
            string[] parts={"Proximal","Intermediate","Distal"};
            for(int f=0;f<5;f++) for(int j=0;j<3;j++) mapping[side+fingers[f]+parts[j]]=prefix+"Finger"+f+(j==0?"":j.ToString());
        }
        description.human = mapping.Select(pair => new HumanBone { humanName=pair.Key, boneName=pair.Value, limit=new HumanLimit { useDefaultValues=true } }).ToArray();
        description.upperArmTwist=.5f; description.lowerArmTwist=.5f; description.upperLegTwist=.5f; description.lowerLegTwist=.5f;
        description.armStretch=.05f; description.legStretch=.05f; description.feetSpacing=0f;
        importer.humanDescription=description;
        importer.SaveAndReimport();
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (model == null) throw new InvalidOperationException("Character import failed: "+name);
        foreach (string texturePath in Directory.GetFiles(folder,"*.png"))
        {
            var ti=AssetImporter.GetAtPath(texturePath.Replace('\\','/')) as TextureImporter;
            if(ti==null) continue;
            ti.maxTextureSize=2048; ti.mipmapEnabled=true; ti.textureCompression=TextureImporterCompression.Compressed;
            if(texturePath.Contains("normal")) ti.textureType=TextureImporterType.NormalMap;
            ti.SaveAndReimport();
        }
        GameObject root=new GameObject(name);
        root.AddComponent<ScenarioCharacterVisual>().ResourceId=name;
        GameObject visual=(GameObject)PrefabUtility.InstantiatePrefab(model);
        visual.transform.SetParent(root.transform,false);
        var animator=visual.GetComponent<Animator>();
        if(animator==null || animator.avatar==null || !animator.avatar.isValid || !animator.avatar.isHuman)
        {
            UnityEngine.Object.DestroyImmediate(root);
            throw new InvalidOperationException("Humanoid avatar invalid: "+name);
        }
        animator.applyRootMotion=false;
        animator.runtimeAnimatorController=Resources.Load<RuntimeAnimatorController>("LumenPeople/QuaterniusHumanoid");
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        Directory.CreateDirectory(folder+"/Materials");
        foreach(var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Material[] remapped=renderer.sharedMaterials;
            for(int i=0;i<remapped.Length;i++)
            {
                string materialName=remapped[i]!=null?remapped[i].name.Split('.')[0]:"body";
                string matPath=folder+"/Materials/"+materialName+".mat";
                Material mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if(mat==null) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,matPath); }
                string texturePrefix=materialName.Replace("_A_head","_head");
                string colorPath=Directory.GetFiles(folder,texturePrefix+"*color*.png").FirstOrDefault();
                string normalPath=Directory.GetFiles(folder,texturePrefix+"*normal*.png").FirstOrDefault();
                mat.SetColor("_BaseColor",Color.white); mat.SetFloat("_Smoothness",.32f);
                if(colorPath!=null) mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath.Replace('\\','/')));
                if(normalPath!=null) { mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath.Replace('\\','/'))); mat.EnableKeyword("_NORMALMAP"); }
                if(materialName.Contains("opacity") || materialName.Contains("glasses"))
                { mat.SetFloat("_AlphaClip",1); mat.SetFloat("_Cutoff",.35f); mat.EnableKeyword("_ALPHATEST_ON"); mat.SetFloat("_Cull",0); mat.renderQueue=2450; }
                remapped[i]=mat;
            }
            renderer.sharedMaterials=remapped;
            renderer.updateWhenOffscreen=false;
        }
        Renderer[] renderers=visual.GetComponentsInChildren<Renderer>();
        Bounds bounds=renderers[0].bounds;
        foreach(var r in renderers) bounds.Encapsulate(r.bounds);
        float scale=1.73f/bounds.size.y; visual.transform.localScale*=scale;
        bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
        visual.transform.localPosition-=Vector3.up*bounds.min.y;
        PrefabUtility.SaveAsPrefabAsset(root,Output+"/"+name+".prefab");
        int shapeCount=renderers.OfType<SkinnedMeshRenderer>().Sum(r=>r.sharedMesh.blendShapeCount);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("Lumen character built: "+name+"; humanoid valid; facial shapes="+shapeCount);
    }
}

using UnityEngine;

/// <summary>Original Blender garments follow the existing humanoid skeleton, preserving its facial rig.</summary>
public static class CharacterWardrobe
{
    private static readonly System.Collections.Generic.Dictionary<Mesh,Mesh> maskedBodies=new System.Collections.Generic.Dictionary<Mesh,Mesh>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){foreach(var mesh in maskedBodies.Values)if(mesh!=null)Object.Destroy(mesh);maskedBodies.Clear();foreach(var material in sleeveSurfaces.Values)if(material!=null)Object.Destroy(material);sleeveSurfaces.Clear();}
    private static readonly System.Collections.Generic.Dictionary<string,Material> sleeveSurfaces=new System.Collections.Generic.Dictionary<string,Material>();
    public static readonly string[] Styles={"OpeningHero","MuseumMystic","Professor","RomanCitizen","RomanGuard","CafeHost"};
    public static string Resource(string style)=>style=="OpeningHero"?"Kai":style=="MuseumMystic"||style=="RomanGuard"?"Orion":style=="Professor"?"Theo":style=="CafeHost"?"Maya":"Marcus";
    public static string Label(string style)=>style=="CafeHost"?"Maya":style=="OpeningHero"?"Kai":style=="MuseumMystic"?"Orion":style=="Professor"?"Professor Theo":style=="RomanGuard"?"Lucius":"Aurelius";
    public static string Default(string scene)=>scene=="Cafestore"?"CafeHost":scene=="Museum"?"MuseumMystic":scene=="Classroom"?"Professor":scene=="HistoricalPlace"?"RomanCitizen":"OpeningHero";

    public static bool Apply(Transform visual,string style)
    {
        if(visual==null)return false;
        Transform old=visual.Find("CharacterWardrobe");
        if(old!=null){old.gameObject.SetActive(false);Object.Destroy(old.gameObject);}
        if(TailoredCharacterWardrobe.TryApply(visual,style))return true;
        GameObject prefab=Resources.Load<GameObject>("LumenCollection/"+style+"Outfit");
        Animator animator=visual.GetComponentInChildren<Animator>();
        if(prefab==null||animator==null||!animator.isHuman)return false;
        animator.Play("Armature|Idle_Loop",0,0f);animator.Update(0f);
        SkinnedMeshRenderer body=visual.GetComponentInChildren<SkinnedMeshRenderer>();
        if(body==null)return false;
        Bounds bounds=body.bounds;
        var previousMask=visual.GetComponent<WardrobeBodyMask>();
        if(previousMask!=null&&previousMask.style!=style)
        {
            body.sharedMesh=previousMask.original;
            if(previousMask.originalMaterials!=null)body.sharedMaterials=previousMask.originalMaterials;
        }
        if(previousMask==null||previousMask.style!=style)
        {
            var mask=previousMask!=null?previousMask:visual.gameObject.AddComponent<WardrobeBodyMask>();
            mask.body=body;mask.original=body.sharedMesh;mask.originalMaterials=body.sharedMaterials;mask.style=style;
            if(maskedBodies.TryGetValue(body.sharedMesh,out Mesh existingMask)){mask.mesh=existingMask;body.sharedMesh=existingMask;}
            else
            {
            Mesh replacement=Object.Instantiate(body.sharedMesh);
            var vertices=replacement.vertices;
            var weights=replacement.boneWeights;
            var handBones=new System.Collections.Generic.HashSet<int>();
            var armBones=new System.Collections.Generic.HashSet<int>();
            for(int boneIndex=0;boneIndex<body.bones.Length;boneIndex++)
            {
                string boneName=body.bones[boneIndex]!=null?body.bones[boneIndex].name:"";
                if(boneName.Contains(" Hand")||boneName.Contains("Finger"))handBones.Add(boneIndex);
                if(boneName.Contains("UpperArm")||boneName.Contains("Forearm"))armBones.Add(boneIndex);
            }
            var hands=new bool[vertices.Length];
            var arms=new bool[vertices.Length];
            for(int vertex=0;vertex<weights.Length;vertex++)
            {
                BoneWeight w=weights[vertex];
                hands[vertex]=(w.weight0>.3f&&handBones.Contains(w.boneIndex0))||(w.weight1>.3f&&handBones.Contains(w.boneIndex1))||
                    (w.weight2>.3f&&handBones.Contains(w.boneIndex2))||(w.weight3>.3f&&handBones.Contains(w.boneIndex3));
                arms[vertex]=(w.weight0>.3f&&armBones.Contains(w.boneIndex0))||(w.weight1>.3f&&armBones.Contains(w.boneIndex1))||
                    (w.weight2>.3f&&armBones.Contains(w.boneIndex2))||(w.weight3>.3f&&armBones.Contains(w.boneIndex3));
            }
            int originalSubmeshes=replacement.subMeshCount;
            var sleeves=new System.Collections.Generic.List<int>();
            for(int sub=0;sub<originalSubmeshes;sub++)
            {
                if(style!="Professor"&&sub<body.sharedMaterials.Length&&body.sharedMaterials[sub].name.Contains("opacity")){replacement.SetTriangles(System.Array.Empty<int>(),sub);continue;}
                var source=replacement.GetTriangles(sub);var filtered=new System.Collections.Generic.List<int>();
                for(int i=0;i<source.Length;i+=3)
                {
                    var centre=(vertices[source[i]]+vertices[source[i+1]]+vertices[source[i+2]])/3f;
                    centre=visual.InverseTransformPoint(body.transform.TransformPoint(centre));
                    bool hand=hands[source[i]]||hands[source[i+1]]||hands[source[i+2]];
                    bool arm=!hand&&(arms[source[i]]||arms[source[i+1]]||arms[source[i+2]]);
                    if(!hand&&centre.y<.97f)continue;
                    if(!hand&&!arm&&centre.y<1.40f&&Mathf.Abs(centre.x)<.23f)continue;
                    arm=arm||(!hand&&centre.y<1.48f&&centre.y>.90f&&Mathf.Abs(centre.x)>.20f);
                    var target=arm?sleeves:filtered;
                    target.Add(source[i]);target.Add(source[i+1]);target.Add(source[i+2]);
                }
                replacement.SetTriangles(filtered,sub);
            }
            replacement.subMeshCount=originalSubmeshes+1;
            replacement.SetTriangles(sleeves,originalSubmeshes);
            mask.mesh=replacement;maskedBodies[body.sharedMesh]=replacement;body.sharedMesh=replacement;
            }
        }
        var sleeveMaterials=new System.Collections.Generic.List<Material>(body.sharedMaterials);
        while(sleeveMaterials.Count<body.sharedMesh.subMeshCount)sleeveMaterials.Add(null);
        Color sleeveColor=style.StartsWith("Roman")?new Color(.58f,.40f,.28f):style=="OpeningHero"?new Color(.055f,.17f,.09f):style=="Professor"?new Color(.22f,.11f,.055f):new Color(.025f,.033f,.035f);
        if(!sleeveSurfaces.TryGetValue(style,out Material sleeve))
        {
            sleeve=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=style+" sleeves",color=sleeveColor};
            sleeve.SetFloat("_Smoothness",.2f);sleeveSurfaces[style]=sleeve;
        }
        sleeveMaterials[body.sharedMesh.subMeshCount-1]=sleeve;body.sharedMaterials=sleeveMaterials.ToArray();
        GameObject outfit=Object.Instantiate(prefab,visual,false);outfit.name="CharacterWardrobe";
        outfit.transform.SetPositionAndRotation(new Vector3(visual.position.x,bounds.min.y,visual.position.z),visual.rotation);
        outfit.transform.localScale=Vector3.one;
        animator.Update(0f);
        var ownership=outfit.AddComponent<WardrobeAttachments>();
        foreach(MeshFilter filter in outfit.GetComponentsInChildren<MeshFilter>())
        {
            string name=filter.name;HumanBodyBones type=HumanBodyBones.Chest;
            Vector3 nominal=new Vector3(0,1.50f,0);
            if(name.StartsWith("Head_")){type=HumanBodyBones.Head;nominal=new Vector3(0,1.6f,0);}
            else if(name.StartsWith("LeftUpperArm_")){type=HumanBodyBones.LeftUpperArm;nominal=new Vector3(-.23f,1.45f,0);}
            else if(name.StartsWith("RightUpperArm_")){type=HumanBodyBones.RightUpperArm;nominal=new Vector3(.23f,1.45f,0);}
            else if(name.StartsWith("Hips_")){type=HumanBodyBones.Hips;nominal=new Vector3(0,.85f,0);}
            else if(name.StartsWith("LeftUpperLeg_")){type=HumanBodyBones.LeftUpperLeg;nominal=new Vector3(-.10f,.85f,0);}
            else if(name.StartsWith("RightUpperLeg_")){type=HumanBodyBones.RightUpperLeg;nominal=new Vector3(.10f,.85f,0);}
            else if(name.StartsWith("LeftLowerLeg_")){type=HumanBodyBones.LeftLowerLeg;nominal=new Vector3(-.10f,.50f,0);}
            else if(name.StartsWith("RightLowerLeg_")){type=HumanBodyBones.RightLowerLeg;nominal=new Vector3(.10f,.50f,0);}
            else if(name.StartsWith("LeftFoot_")){type=HumanBodyBones.LeftFoot;nominal=new Vector3(-.10f,.10f,0);}
            else if(name.StartsWith("RightFoot_")){type=HumanBodyBones.RightFoot;nominal=new Vector3(.10f,.10f,0);}
            Transform bone=animator.GetBoneTransform(type);if(bone==null)bone=animator.GetBoneTransform(HumanBodyBones.Spine);
            if(bone==null)continue;
            // Fit the torso to its neckline, not the rig's arbitrary chest pivot.
            // Head and limb reference points use the author's standing anatomy.
            Transform landmark=type==HumanBodyBones.Chest?animator.GetBoneTransform(HumanBodyBones.Neck):bone;
            if(landmark!=null)filter.transform.position+=landmark.position-outfit.transform.TransformPoint(nominal);
            if(type==HumanBodyBones.Head||type==HumanBodyBones.LeftFoot||type==HumanBodyBones.RightFoot||type==HumanBodyBones.LeftUpperArm||type==HumanBodyBones.RightUpperArm)
            {
                filter.transform.SetParent(bone,true);
                ownership.parts.Add(filter.gameObject);
            }
            else BindGarment(filter,animator,type,visual);
        }
        return true;
    }

    private static void BindGarment(MeshFilter filter,Animator animator,HumanBodyBones group,Transform visual)
    {
        var previous=filter.GetComponent<MeshRenderer>();if(previous==null)return;
        Material[] materials=previous.sharedMaterials;
        Mesh mesh=Object.Instantiate(filter.sharedMesh);mesh.name=filter.name+" fitted skin";
        Transform hips=animator.GetBoneTransform(HumanBodyBones.Hips),chest=animator.GetBoneTransform(HumanBodyBones.Chest);
        Transform leftThigh=animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg),rightThigh=animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        Transform leftCalf=animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg),rightCalf=animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        Transform[] bones={hips,chest,leftThigh,rightThigh,leftCalf,rightCalf};
        Matrix4x4[] poses=new Matrix4x4[bones.Length];
        for(int i=0;i<bones.Length;i++)poses[i]=bones[i].worldToLocalMatrix*filter.transform.localToWorldMatrix;
        Vector3[] vertices=mesh.vertices;BoneWeight[] weights=new BoneWeight[vertices.Length];
        float waist=visual.InverseTransformPoint(hips.position).y+.12f;
        float collar=visual.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Neck).position).y-.10f;
        bool upper=group==HumanBodyBones.LeftUpperLeg||group==HumanBodyBones.RightUpperLeg;
        bool lower=group==HumanBodyBones.LeftLowerLeg||group==HumanBodyBones.RightLowerLeg;
        if(upper||lower)
        {
            bool left=group==HumanBodyBones.LeftUpperLeg||group==HumanBodyBones.LeftLowerLeg;
            Transform start=upper?(left?leftThigh:rightThigh):(left?leftCalf:rightCalf);
            Transform end=upper?(left?leftCalf:rightCalf):animator.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
            Vector3 segment=end.position-start.position;
            Quaternion alignment=Quaternion.FromToRotation(-visual.up,segment.normalized);
            float authoredLength=upper?.35f:.40f;
            for(int i=0;i<vertices.Length;i++)
            {
                Vector3 relative=filter.transform.TransformPoint(vertices[i])-start.position;
                float vertical=Vector3.Dot(relative,visual.up);
                Vector3 radial=relative-visual.up*vertical;
                Vector3 centre=upper&&vertical>0?start.position+visual.up*vertical:start.position+segment*(-vertical/authoredLength);
                vertices[i]=filter.transform.InverseTransformPoint(centre+alignment*radial);
            }
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 p=visual.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
            int first=0,second=1;float secondWeight=0f;
            if(group==HumanBodyBones.Chest)secondWeight=Mathf.InverseLerp(waist,collar,p.y);
            else if(group==HumanBodyBones.Hips)
            {
                second=p.x<0?2:3;secondWeight=.18f*(1-Mathf.InverseLerp(waist-.25f,waist,p.y));
            }
            else
            {
                bool left=group==HumanBodyBones.LeftUpperLeg||group==HumanBodyBones.LeftLowerLeg;
                int thigh=left?2:3,calf=left?4:5;
                float knee=visual.InverseTransformPoint((left?leftCalf:rightCalf).position).y;
                if(p.y>waist-.10f){first=thigh;second=0;secondWeight=Mathf.InverseLerp(waist-.10f,waist+.06f,p.y);}
                else{first=calf;second=thigh;secondWeight=Mathf.InverseLerp(knee-.08f,knee+.10f,p.y);}
            }
            weights[i]=new BoneWeight{boneIndex0=first,weight0=1-secondWeight,boneIndex1=second,weight1=secondWeight};
        }
        mesh.bindposes=poses;mesh.boneWeights=weights;
        // Use a separate object to avoid incompatible Renderer components while
        // the old static component awaits destruction at the end of this frame.
        var fitted=new GameObject(filter.name+" skinned");fitted.transform.SetParent(filter.transform.parent,false);
        fitted.transform.localPosition=filter.transform.localPosition;fitted.transform.localRotation=filter.transform.localRotation;fitted.transform.localScale=filter.transform.localScale;
        var skin=fitted.AddComponent<SkinnedMeshRenderer>();
        skin.sharedMesh=mesh;skin.bones=bones;skin.sharedMaterials=materials;
        skin.localBounds=mesh.bounds;skin.updateWhenOffscreen=true;
        fitted.AddComponent<WardrobeMeshLifetime>().mesh=mesh;
        previous.enabled=false;Object.Destroy(filter.gameObject);
    }
}

public sealed class WardrobeAttachments:MonoBehaviour
{
    public readonly System.Collections.Generic.List<GameObject> parts=new System.Collections.Generic.List<GameObject>();
    private void OnDisable(){foreach(var part in parts)if(part!=null)part.SetActive(false);}
    private void OnDestroy(){foreach(var part in parts)if(part!=null)Destroy(part);}
}

public sealed class WardrobeBodyMask:MonoBehaviour
{
    public SkinnedMeshRenderer body;public Mesh original,mesh;
    public Material[] originalMaterials;public string style;
}

public sealed class WardrobeMeshLifetime:MonoBehaviour
{
    public Mesh mesh;
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);}
}

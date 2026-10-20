using UnityEngine;
using System.Collections.Generic;

/// <summary>Non-graphic interpretive arena performance and seated clothed audience.</summary>
public sealed class RomanArenaPerformance:MonoBehaviour
{
    private Transform first,second;
    private Animator firstAnimator,secondAnimator;
    private float nextGesture;
    private bool firstAttacks;
    public static void Install()
    {
        if(GameObject.Find("RomanArenaPerformance")!=null)return;
        new GameObject("RomanArenaPerformance").AddComponent<RomanArenaPerformance>();
    }
    private void Start()
    {
        var perimeter=gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();perimeter.carving=true;perimeter.center=new Vector3(0,.75f,0);perimeter.size=new Vector3(6,1.5f,4.7f);
        first=CreateFighter("Cassius",-1.1f);second=CreateFighter("Titus",1.1f);
        firstAnimator=first!=null?first.GetComponentInChildren<Animator>():null;secondAnimator=second!=null?second.GetComponentInChildren<Animator>():null;
        // Three baked seated poses and shared costume palettes; no extra live facial rigs.
        var source=Resources.Load<GameObject>("ScenarioCharacters/Marcus");if(source==null)return;
        var variants=new List<List<(Mesh mesh,Material[] materials,Matrix4x4 transform)>>();
        Color[] palette={new Color(.90f,.87f,.78f),new Color(.63f,.72f,.68f),new Color(.68f,.56f,.45f)};
        for(int variation=0;variation<3;variation++)
        {
            var template=Instantiate(source);template.transform.position=Vector3.zero;CharacterWardrobe.Apply(template.transform,"RomanCitizen");
            var animator=template.GetComponentInChildren<Animator>();animator.Play("Armature|Sitting_Idle_Loop",0,.15f+variation*.28f);animator.Update(0f);animator.enabled=false;
            var head=animator.GetBoneTransform(HumanBodyBones.Head);if(head!=null)head.Rotate(Vector3.up,(variation-1)*12,Space.Self);
            var meshes=new List<(Mesh mesh,Material[] materials,Matrix4x4 transform)>();
            var paletteMaterials=new Dictionary<Material,Material>();
            foreach(var skin in template.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=new Mesh();skin.BakeMesh(mesh);var materials=skin.sharedMaterials;
                for(int sub=0;sub<materials.Length;sub++)
                {
                    var material=materials[sub];
                    if(material.name.Contains("woven cloth")||material.name.Contains("lower cloth"))
                    {
                        if(!paletteMaterials.TryGetValue(material,out var replacement))
                        {replacement=LumenRuntimeResources.Own(new Material(material){name="Audience cloth palette "+variation});replacement.color=palette[variation];paletteMaterials[material]=replacement;}
                        materials[sub]=replacement;
                    }
                }
                meshes.Add((mesh,materials,skin.transform.localToWorldMatrix));
            }
            variants.Add(meshes);template.SetActive(false);Destroy(template);
        }
        var groups=new Dictionary<Material,List<CombineInstance>>();
        for(int row=0;row<3;row++)for(int seat=0;seat<16;seat++)
        {
            float angle=(seat+.5f+row*.25f)*Mathf.PI*2f/16f;
            Vector3 p=new Vector3((6.1f+row*.85f)*Mathf.Cos(angle),.73f+row*.43f,(4.15f+row*.60f)*Mathf.Sin(angle));
            if(p.z<0&&Mathf.Abs(p.x)<1.9f)continue;
            Quaternion facing=Quaternion.LookRotation(-new Vector3(p.x,0,p.z))*Quaternion.Euler(0,(seat%3-1)*4,0);
            float height=.79f+(seat%4)*.023f;
            Matrix4x4 placement=Matrix4x4.TRS(p,facing,new Vector3(height*(1+(seat%3-1)*.035f),height,height));
            foreach(var item in variants[(row+seat)%3])for(int sub=0;sub<item.mesh.subMeshCount;sub++)
            {
                Material material=item.materials[Mathf.Min(sub,item.materials.Length-1)];
                if(!groups.TryGetValue(material,out var group)){group=new List<CombineInstance>();groups[material]=group;}
                group.Add(new CombineInstance{mesh=item.mesh,subMeshIndex=sub,transform=placement*item.transform});
            }
        }
        foreach(var pair in groups)
        {
            Mesh mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);
            var part=new GameObject("Seated Roman audience",typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(transform,false);
            part.GetComponent<MeshFilter>().sharedMesh=mesh;part.GetComponent<MeshRenderer>().sharedMaterial=pair.Key;part.AddComponent<WardrobeMeshLifetime>().mesh=mesh;
        }
        foreach(var meshes in variants)foreach(var item in meshes)Destroy(item.mesh);
    }

    private Transform CreateFighter(string label,float x)
    {
        var root=ScenarioPopulation.Spawn(transform,label,new Vector3(x,.9f,0),"RomanGuard","arena performer",false,"ancient_rome");
        if(root==null)return null;
        root.GetComponent<AmbientNpcRoutine>().enabled=false;
        root.GetComponent<AgentAnimator>().enabled=false; // Arena choreography owns combat clips.
        var nav=root.GetComponent<UnityEngine.AI.NavMeshAgent>();if(nav!=null)nav.enabled=false;
        var ground=root.GetComponent<CharacterGrounding>();if(ground!=null)ground.enabled=false;
        root.transform.position=new Vector3(x,.9f,0);
        Animator animator=root.GetComponentInChildren<Animator>();Transform hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
        if(hand!=null)
        {
            var prefab=Resources.Load<GameObject>("LumenCollection/ArenaPracticeSword");
            if(prefab!=null)
            {
                var sword=Instantiate(prefab,hand,false);sword.name="Authored wooden practice sword";sword.transform.localPosition=Vector3.zero;sword.transform.localRotation=Quaternion.identity;
                foreach(var collider in sword.GetComponentsInChildren<Collider>())collider.enabled=false;
            }
            else Debug.LogWarning("Authored practice sword unavailable; no placeholder substituted.");
        }
        root.GetComponent<ObjectMetadata>().description="A fictional staged, non-graphic sparring performance using practice weapons. It is an interpretive scene, not a claim about a specific historical bout.";
        return root.transform;
    }
    private void Update()
    {
        if(first==null||second==null)return;
        float phase=LumenAccessibility.ReducedMotion?0:Time.time*.6f;
        first.position=new Vector3(-1.0f+.20f*Mathf.Sin(phase),.9f,.5f*Mathf.Sin(phase));
        second.position=new Vector3(1.0f+.20f*Mathf.Sin(phase+Mathf.PI),.9f,.5f*Mathf.Sin(phase+Mathf.PI));
        first.rotation=Quaternion.LookRotation(second.position-first.position);second.rotation=Quaternion.LookRotation(first.position-second.position);
        if(Time.time>nextGesture)
        {
            nextGesture=Time.time+2.5f;
            firstAttacks=!firstAttacks;
            if(firstAnimator!=null)firstAnimator.CrossFade(firstAttacks?"Armature|Sword_Attack":"Armature|Sword_Idle",.2f);
            if(secondAnimator!=null)secondAnimator.CrossFade(firstAttacks?"Armature|Sword_Idle":"Armature|Sword_Attack",.25f);
        }
    }
}

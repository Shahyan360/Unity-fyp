using UnityEngine;
using System.Collections.Generic;

/// <summary>Non-graphic interpretive arena performance and seated clothed audience.</summary>
public sealed class RomanArenaPerformance:MonoBehaviour
{
    private Transform first,second;
    private Animator firstAnimator,secondAnimator;
    private float nextGesture;
    private bool firstAttacks;
    private float floorHeight;
    private const float ExchangeSeconds = 3.1f;
    public static void Install()
    {
        if(GameObject.Find("RomanArenaPerformance")!=null)return;
        new GameObject("RomanArenaPerformance").AddComponent<RomanArenaPerformance>();
    }
    private void Start()
    {
        var perimeter=gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();perimeter.carving=true;perimeter.center=new Vector3(0,.75f,0);perimeter.size=new Vector3(6,1.5f,4.7f);
        var floor = GameObject.Find("Arena_Sand_Floor")?.GetComponent<Renderer>();
        floorHeight = floor != null ? floor.bounds.max.y + .015f : .83f;
        first = CreateFighter("Cassius", -1.14f, "RomanGuard");
        second = CreateFighter("Titus", 1.14f, "RomanCitizen");
        firstAnimator=first!=null?first.GetComponentInChildren<Animator>():null;secondAnimator=second!=null?second.GetComponentInChildren<Animator>():null;
        // Three baked seated poses and shared costume palettes; no extra live facial rigs.
        var source=Resources.Load<GameObject>("ScenarioCharacters/Marcus");if(source==null)return;
        var variants = new List<(List<(Mesh mesh, Material[] materials, Matrix4x4 transform)> meshes, Vector3 hips)>();
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
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            variants.Add((meshes, hips != null ? hips.position : Vector3.up * .54f));
            template.SetActive(false);
            Destroy(template);
        }
        var groups=new Dictionary<Material,List<CombineInstance>>();
        foreach (var bench in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
        {
            if (!bench.name.StartsWith("Seating_Tier_", System.StringComparison.Ordinal)) continue;
            string[] parts = bench.name.Split('_');
            if (parts.Length != 4 || !int.TryParse(parts[2], out int row) || !int.TryParse(parts[3], out int seat)
                || row < 1 || row > 4 || (seat + row) % 4 != 0) continue;
            Bounds bounds = bench.bounds;
            Vector3 outward = Vector3.ProjectOnPlane(bounds.center - transform.position, Vector3.up);
            if (outward.z < 0 && Mathf.Abs(outward.x) < 1.9f) continue;
            Vector3 seatPoint = new Vector3(bounds.center.x, bounds.max.y + .075f, bounds.center.z) - outward.normalized * .23f;
            Quaternion facing = Quaternion.LookRotation(-outward) * Quaternion.Euler(0, (seat % 5 - 2) * 4f, 0);
            float height = .86f + (row % 3) * .045f;
            var variant = variants[(row * 2 + seat) % variants.Count];
            Vector3 origin = seatPoint - facing * (variant.hips * height);
            Matrix4x4 placement = Matrix4x4.TRS(origin, facing, Vector3.one * height);
            foreach(var item in variant.meshes)for(int sub=0;sub<item.mesh.subMeshCount;sub++)
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
        foreach (var variant in variants) foreach (var item in variant.meshes) Destroy(item.mesh);
    }

    private Transform CreateFighter(string label, float x, string wardrobe)
    {
        var root = ScenarioPopulation.Spawn(transform, label, new Vector3(x, floorHeight, 0), wardrobe, "arena performer", false, "ancient_rome");
        if(root==null)return null;
        root.GetComponent<AmbientNpcRoutine>().enabled=false;
        root.GetComponent<AgentAnimator>().enabled=false; // Arena choreography owns combat clips.
        var nav=root.GetComponent<UnityEngine.AI.NavMeshAgent>();if(nav!=null)nav.enabled=false;
        var ground=root.GetComponent<CharacterGrounding>();if(ground!=null)ground.enabled=false;
        root.transform.position = new Vector3(x, floorHeight, 0);
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
        if (LumenAccessibility.ReducedMotion)
        {
            SetPositions(0f, 0f);
            if (nextGesture >= 0f)
            {
                firstAnimator?.Play("Armature|Sword_Idle");
                secondAnimator?.Play("Armature|Sword_Idle");
                nextGesture = -1f;
            }
            return;
        }
        if (Time.time >= nextGesture)
        {
            nextGesture = Time.time + ExchangeSeconds;
            firstAttacks = !firstAttacks;
            firstAnimator?.CrossFadeInFixedTime(firstAttacks ? "Armature|Sword_Attack" : "Armature|Sword_Idle", .22f);
            secondAnimator?.CrossFadeInFixedTime(firstAttacks ? "Armature|Sword_Idle" : "Armature|Sword_Attack", .22f);
        }
        float elapsed = ExchangeSeconds - (nextGesture - Time.time);
        float step = .27f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(elapsed / 1.35f));
        SetPositions(step * (firstAttacks ? 1f : -1f), .20f * Mathf.Sin(Time.time * .8f));
    }

    private void SetPositions(float step, float offset)
    {
        first.SetPositionAndRotation(new Vector3(-1.14f + step, floorHeight, offset), Quaternion.LookRotation(new Vector3(2.28f, 0, -2f * offset)));
        second.SetPositionAndRotation(new Vector3(1.14f + step, floorHeight, -offset), Quaternion.LookRotation(new Vector3(-2.28f, 0, 2f * offset)));
    }
}

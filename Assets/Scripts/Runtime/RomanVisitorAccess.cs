using System.Collections;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

/// <summary>Open the authored south entrance and bake a continuous visitor path into the arena.</summary>
public sealed class RomanVisitorAccess:MonoBehaviour
{
    public bool Complete {get;private set;}
    private IEnumerator Start()
    {
        yield return null;yield return null;
        foreach(var c in FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
        {
            if(!c.enabled||c is CharacterController||c.isTrigger)continue;
            var b=c.bounds;
            bool structure=c.name.StartsWith("Seating_Tier_")||c.name.StartsWith("Foundation_Stone_")||c.name.StartsWith("Lower_Podium_")||c.name.StartsWith("Story_1_Base_")||c.name.StartsWith("Arcade_Shadow_0_");
            if(structure&&b.min.x<1.5f&&b.max.x>-1.5f&&b.center.z< -3&&b.center.z> -12&&b.max.y<3.1f)
            {c.enabled=false;var r=c.GetComponent<Renderer>();if(r!=null)r.enabled=false;}
            if(c.name=="Arena_Sand_Floor")
            {
                c.enabled=false;var f=c.GetComponent<MeshFilter>();
                if(f!=null){var mesh=c.gameObject.AddComponent<MeshCollider>();mesh.sharedMesh=f.sharedMesh;}
            }
        }
        // A gradual accessible approach replaces the original unwalkable seating steps.
        var root=new GameObject("Arena visitor entrance");
        var material=LumenRuntimeResources.Own(new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.56f,.48f,.36f)});
        var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);ramp.name="Arena entrance ramp";ramp.transform.SetParent(root.transform);
        ramp.transform.position=new Vector3(0,.33f,-7.5f);ramp.transform.localScale=new Vector3(2.7f,.16f,9f);ramp.transform.rotation=Quaternion.Euler(-5,0,0);ramp.GetComponent<Renderer>().sharedMaterial=material;
        var deck=GameObject.CreatePrimitive(PrimitiveType.Cube);deck.name="Arena viewing terrace";deck.transform.SetParent(root.transform);
        deck.transform.position=new Vector3(0,.66f,-2.95f);deck.transform.localScale=new Vector3(5.5f,.2f,1.3f);deck.GetComponent<Renderer>().sharedMaterial=material;
        var marker=new GameObject("Arena viewing destination");marker.transform.position=new Vector3(0,.78f,-3);marker.transform.SetParent(root.transform);
        var data=marker.AddComponent<ObjectMetadata>();data.displayName=data.objectName="Arena viewing";data.isInteractable=true;data.category="Destination";data.scenario="ancient_rome";
        data.description="The public viewing terrace inside the amphitheatre, safely outside the sparring ring.";
        Physics.SyncTransforms();
        var surfaces=FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Exclude);
        foreach(var surface in surfaces)surface.BuildNavMesh();
        Complete=true;
    }
}

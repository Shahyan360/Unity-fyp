using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.AI;

public sealed class CafeVisualFinishing:MonoBehaviour
{
    public static void Install(){if(GameObject.Find("CafeVisualFinishing")==null)new GameObject("CafeVisualFinishing").AddComponent<CafeVisualFinishing>();}
    private void Start()
    {
        var ceramic=Material("Cafe sage ceramic",new Color(.20f,.36f,.28f));var leaf=Material("Cafe plant leaves",new Color(.12f,.30f,.10f));var wood=Material("Cafe pastry tray",new Color(.22f,.12f,.055f));var pastry=Material("Cafe baked pastry",new Color(.65f,.39f,.15f));
        int count=0;
        foreach(Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if(count>=8)break;
            if(!renderer.name.ToLowerInvariant().Contains("table")||renderer.bounds.size.y>.35f||renderer.bounds.size.x<.8f)continue;
            Vector3 p=renderer.bounds.center;p.y=renderer.bounds.max.y;
            Primitive(PrimitiveType.Cylinder,"Table herb pot",p+Vector3.up*.075f,new Vector3(.17f,.075f,.17f),ceramic);
            for(int i=0;i<4;i++)Primitive(PrimitiveType.Sphere,"Small table greenery",p+new Vector3(Mathf.Sin(i*1.7f)*.06f,.20f,Mathf.Cos(i*1.7f)*.06f),new Vector3(.08f,.20f,.08f),leaf);
            count++;
        }
        var quiet=GameObject.Find("UpperTable_1");
        if(quiet!=null)
        {
            var top=quiet.GetComponentsInChildren<Renderer>().OrderByDescending(r=>r.bounds.max.y).FirstOrDefault();
            if(top!=null)
            {
                Vector3 p=top.bounds.center;float floor=top.bounds.max.y-.78f;
                var marker=new GameObject("Quiet conversation table");marker.transform.SetParent(transform,true);marker.transform.position=new Vector3(p.x,floor,p.z-1.1f);
                bool walkable=NavMesh.SamplePosition(marker.transform.position,out var hit,.8f,NavMesh.AllAreas);
                if(walkable)marker.transform.position=hit.position;
                var data=marker.AddComponent<ObjectMetadata>();data.objectName=data.displayName="Quiet table";data.category=walkable?"Destination":"Cafe furniture";data.isInteractable=true;data.scenario="cafe";
                data.description="The upper-floor quiet table is a place for relaxed conversation. Practice is optional. Use the stairs to reach this floor; no lift is represented here.";data.descriptionSummary=data.description;
                var label=new GameObject("Quiet corner wayfinding",typeof(TextMeshPro));label.transform.SetParent(transform,true);label.transform.position=new Vector3(p.x,floor+1.75f,p.z+.35f);
                var text=label.GetComponent<TextMeshPro>();text.text="A QUIET CORNER";text.fontSize=1.1f;text.color=new Color(.90f,.84f,.69f);text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(3.3f,.5f);label.AddComponent<WorldLabelBillboard>();
                var fillObject=new GameObject("Upper conversation soft fill");fillObject.transform.SetParent(transform,true);fillObject.transform.position=new Vector3(p.x,floor+2.2f,p.z-1);
                var fill=fillObject.AddComponent<Light>();fill.type=LightType.Point;fill.intensity=1.4f;fill.range=4;fill.color=new Color(1,.91f,.79f);fill.shadows=LightShadows.None;
            }
        }
    }
    private Material Material(string name,Color color)=>LumenRuntimeResources.Own(new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color});
    private void Primitive(PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(transform,true);go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<Collider>().enabled=false;
    }
}

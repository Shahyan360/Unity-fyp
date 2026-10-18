using UnityEngine;

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
        var counter=GameObject.Find("Counter_Top");var surface=counter!=null?counter.GetComponent<Renderer>():null;
        if(surface!=null)
        {
            Vector3 p=surface.bounds.center;p.y=surface.bounds.max.y+.04f;
            Primitive(PrimitiveType.Cube,"Pastry display tray",p,new Vector3(1.1f,.06f,.45f),wood);
            for(int i=0;i<4;i++)Primitive(PrimitiveType.Capsule,"Fresh baked croissant",p+new Vector3(-.36f+i*.24f,.12f,0),new Vector3(.18f,.09f,.13f),pastry);
        }
    }
    private Material Material(string name,Color color)=>new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};
    private void Primitive(PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(transform,true);go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<Collider>().enabled=false;
    }
}

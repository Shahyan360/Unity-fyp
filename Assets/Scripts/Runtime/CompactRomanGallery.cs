using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>A 12 x 10 m interpretive Roman exhibition, preserving the authored scene on disk.</summary>
public static class CompactRomanGallery
{
    private static readonly Vector3 Origin = new Vector3(200f, 0f, 200f);
    public static Vector3 EntryPosition => Origin + new Vector3(0f, .04f, -4f);
    public static Vector3 GuidePosition => Origin + new Vector3(-2.1f, .04f, -2.0f);

    public static void Ensure()
    {
        if (GameObject.Find("CompactRomanGallery") != null) return;
        // The old city remains recoverable in the authored scene. Disable its
        // renderers/lights/colliders, keeping player, guide and service scripts.
        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            if (renderer.GetComponentInParent<NavMeshAgent>() == null) renderer.enabled = false;
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude)) light.enabled = false;
        foreach (Collider collider in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
            if (collider.GetComponentInParent<NavMeshAgent>() == null && !(collider is CharacterController)) collider.enabled = false;
        foreach (ObjectMetadata metadata in Object.FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Exclude))
            if (metadata.GetComponentInParent<NavMeshAgent>() == null)
            {
                metadata.isInteractable=false;
                metadata.enabled=false;
            }

        GameObject root = new GameObject("CompactRomanGallery"); root.transform.position = Origin;
        Material stone = Material("Roman gallery limestone", new Color(.66f,.58f,.45f));
        Material plaster = Material("Roman gallery plaster", new Color(.31f,.25f,.18f));
        Material walnut = Material("Roman gallery walnut", new Color(.095f,.05f,.027f));
        Material bronze = Material("Roman gallery bronze", new Color(.55f,.36f,.13f),.7f);
        Material cloth = Material("Roman gallery oxblood",new Color(.25f,.035f,.035f));
        Material ivory = Material("Roman gallery ivory",new Color(.83f,.75f,.60f));

        Box(root.transform,"Gallery walkable floor",new Vector3(0f,-.15f,0f),new Vector3(12f,.3f,10f),stone,true);
        Box(root.transform,"Gallery back wall",new Vector3(0f,2.3f,5f),new Vector3(12f,4.6f,.3f),plaster,true);
        Box(root.transform,"Gallery front wall",new Vector3(0f,2.3f,-5f),new Vector3(12f,4.6f,.3f),plaster,true);
        Box(root.transform,"Gallery ceiling",new Vector3(0f,4.65f,0f),new Vector3(12f,.2f,10f),walnut,false);
        foreach(int side in new[]{-1,1})
        {
            Box(root.transform,"Gallery side wall",new Vector3(side*6f,2.3f,0f),new Vector3(.3f,4.6f,10f),plaster,true);
            Box(root.transform,"Limestone dado",new Vector3(side*5.8f,.5f,0f),new Vector3(.16f,1f,10f),stone,false);
            Box(root.transform,"Bronze dado rail",new Vector3(side*5.7f,1.05f,0f),new Vector3(.06f,.045f,10f),bronze,false);
            Box(root.transform,"Roman heritage banner",new Vector3(side*3.9f,2.7f,4.8f),new Vector3(1.25f,2.7f,.04f),cloth,false);
            Label(root.transform,"SPQR",new Vector3(side*3.9f,3f,4.75f),3.2f,ivory.color,new Vector2(1.15f,.45f));
            Label(root.transform,"ROME",new Vector3(side*3.9f,2.1f,4.75f),3.2f,ivory.color,new Vector2(1.15f,.45f));
            foreach(float z in new[]{-3.8f,0f,3.8f})
            {
                Box(root.transform,"Engaged stone pilaster",new Vector3(side*5.65f,2.2f,z),new Vector3(.35f,4.4f,.45f),stone,false);
                Box(root.transform,"Pilaster abacus",new Vector3(side*5.6f,4.15f,z),new Vector3(.5f,.18f,.7f),ivory,false);
            }
        }
        foreach(float z in new[]{-3.8f,0f,3.8f})
        {
            Box(root.transform,"Coffered ceiling beam",new Vector3(0f,4.4f,z),new Vector3(11.9f,.25f,.22f),stone,false);
            Box(root.transform,"Coffer bronze line",new Vector3(0f,4.25f,z),new Vector3(11.9f,.025f,.035f),bronze,false);
        }
        Box(root.transform,"Exhibition textile",new Vector3(0f,.012f,-.1f),new Vector3(5.3f,.025f,7.5f),cloth,false);
        // Inlaid compass, assembled from eight thin rays instead of a texture download.
        for(int i=0;i<8;i++)
        {
            GameObject ray=Box(root.transform,"Floor compass inlay",new Vector3(0f,.03f,-3.5f),new Vector3(.035f,.008f,.75f),bronze,false);
            ray.transform.localRotation=Quaternion.Euler(0f,i*45f,0f);
        }
        AddDisplay(root.transform,"ForumTable","Roman Forum architectural study",new Vector3(0f,0f,1f),Vector3.one,
            "A modern interpretive miniature: colonnaded temples, an arcaded amphitheatre and terracotta-roofed houses. These features illustrate Roman building types; the arrangement is not an accurate map of the Forum.");
        AddDisplay(root.transform,"RomanStudy","Temple architecture study",new Vector3(-4.25f,0f,1.7f),Vector3.one*.85f,
            "An original architectural study of fluted columns, capitals, entablature and a pediment. These components explain classical construction; this is not a scan of a particular ancient monument.");
        AddDisplay(root.transform,"RomanArtifactCase","Roman trade and daily life",new Vector3(4.2f,0f,1.9f),Vector3.one*.80f,
            "Original interpretive pottery and coin replicas. Amphorae carried goods such as wine and oil; coins circulated images and political messages. These schematic models are not scans of excavated artifacts.");
        Label(root.transform,"ROME / BUILT TO REMEMBER",new Vector3(0f,3.8f,4.78f),4.2f,ivory.color,new Vector2(6f,.7f));
        Label(root.transform,"AN INTERPRETIVE ARCHITECTURE GALLERY",new Vector3(0f,3.2f,4.77f),1.9f,ivory.color,new Vector2(6f,.5f));
        AddBench(root.transform,new Vector3(-4.2f,0f,-3f),walnut,bronze);
        AddBench(root.transform,new Vector3(4.2f,0f,-3f),walnut,bronze);

        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.58f,.52f,.43f); RenderSettings.ambientIntensity=1f;
        RenderSettings.fog=false;
        foreach(float x in new[]{-3.5f,0f,3.5f})
        {
            GameObject fixture=new GameObject("Warm gallery ceiling light");fixture.transform.SetParent(root.transform,false);
            fixture.transform.localPosition=new Vector3(x,4.05f,.5f);
            Light light=fixture.AddComponent<Light>(); light.type=LightType.Point;
            light.color=new Color(1f,.85f,.63f);light.intensity=12f;light.range=10f;light.shadows=LightShadows.None;
            Box(root.transform,"Bronze ceiling luminaire",new Vector3(x,4.17f,.5f),new Vector3(.55f,.10f,.18f),bronze,false);
        }
        GameObject sun=new GameObject("Roman gallery soft fill");sun.transform.SetParent(root.transform,false);
        sun.transform.localRotation=Quaternion.Euler(40f,-25f,0f);
        Light fill=sun.AddComponent<Light>();fill.type=LightType.Directional;fill.color=new Color(1f,.92f,.78f);fill.intensity=.8f;
        NavMeshSurface surface=root.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;
        surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.BuildNavMesh();
    }

    private static void AddDisplay(Transform parent,string resource,string label,Vector3 position,Vector3 scale,string description)
    {
        GameObject prefab=Resources.Load<GameObject>("LumenCollection/"+resource);
        if(prefab==null){Debug.LogWarning("Roman gallery display missing: "+resource);return;}
        GameObject display=Object.Instantiate(prefab,parent,false);display.name=label;
        display.transform.localPosition=position;display.transform.localScale=scale;
        ObjectMetadata data=display.AddComponent<ObjectMetadata>(); data.objectName=label;data.displayName=label;
        data.objectId="roman_gallery_"+resource;data.contextualTags="rome, gallery, interpretive, architecture";
        data.description=description;data.descriptionSummary=description;data.scenario="ancient_rome";
        data.category="Interpretive architectural exhibit";data.isInteractable=true;data.interactionHint="Press E to ask Marcus about this exhibit";
    }

    private static void AddBench(Transform parent,Vector3 position,Material wood,Material metal)
    {
        Box(parent,"Gallery bench seat",position+new Vector3(0f,.5f,0f),new Vector3(2.2f,.12f,.6f),wood,true);
        foreach(float x in new[]{-.8f,.8f})Box(parent,"Bench bronze support",position+new Vector3(x,.23f,0f),new Vector3(.08f,.46f,.5f),metal,false);
        Box(parent,"Bench back",position+new Vector3(0f,.85f,.23f),new Vector3(2.2f,.45f,.06f),wood,false);
    }

    private static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool collision)
    {
        GameObject o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);
        o.transform.localPosition=position;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=material;
        o.GetComponent<Collider>().enabled=collision;return o;
    }

    private static void Label(Transform parent,string content,Vector3 position,float size,Color color,Vector2 dimensions)
    {
        GameObject go=new GameObject(content,typeof(TextMeshPro));go.transform.SetParent(parent,false);go.transform.localPosition=position;
        TextMeshPro label=go.GetComponent<TextMeshPro>();label.text=content;label.fontSize=size;
        label.color=color;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=dimensions;
        label.textWrappingMode=TextWrappingModes.NoWrap;
    }

    private static Material Material(string name,Color color,float metallic=0f)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};
        m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",.25f);return m;
    }
}

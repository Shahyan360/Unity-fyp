using TMPro;
using UnityEngine;

/// <summary>Connected natural-history, painting and historical-collection wings.</summary>
public static class MuseumArchitecture
{
    public static Vector3 ExhibitPosition(string name,Vector3 original)
    {
        switch(name)
        {
            case "Rosetta Stone":return new Vector3(-6,0,22);
            case "Antikythera Mechanism":return new Vector3(-2,0,28);
            case "Roman Coin":return new Vector3(3,0,26);
            case "Terracotta Warrior":return new Vector3(6,0,20);
            case "Greek Amphora":return new Vector3(-7,0,29);
            case "Venus de Milo":return new Vector3(7,0,29);
            case "Sutton Hoo Helmet":return new Vector3(2,0,18);
            case "Gutenberg Press":return new Vector3(-2,0,18);
            case "Triceratops":return new Vector3(-32,0,5);
            case "Cyathea Fern":return new Vector3(-15,0,5);
            case "Apollo 11 Command Module":return new Vector3(6,0,32);
            case "George Washington Bust":return new Vector3(-6,0,33);
            default:return original;
        }
    }

    public static void Extend(Transform gallery)
    {
        Material stone=Mat("Museum wing limestone",new Color(.64f,.59f,.48f));
        var pavers=Resources.Load<Texture2D>("EnvironmentTextures/MuseumLimestone");
        if(pavers!=null){stone.color=Color.white;stone.mainTexture=pavers;stone.mainTextureScale=new Vector2(3,2);stone.SetFloat("_Smoothness",.25f);}
        Material plaster=Mat("Museum wing plaster",new Color(.29f,.25f,.20f));
        Material wood=Mat("Museum wing timber",new Color(.12f,.065f,.035f));
        Material gold=Mat("Museum wing bronze",new Color(.63f,.43f,.18f));
        Wing(gallery,"Natural history wing",new Vector3(-24,0,0),new Vector2(26,18),1,stone,plaster,wood,gold);
        Wing(gallery,"Painting gallery",new Vector3(24,0,0),new Vector2(26,18),-1,stone,plaster,wood,gold);
        Wing(gallery,"Historical collections",new Vector3(0,0,24),new Vector2(21,22),0,stone,plaster,wood,gold);
        Box(gallery,"West connecting threshold",new Vector3(-10.5f,-.15f,0),new Vector3(3,.3f,4),stone,true);
        Box(gallery,"East connecting threshold",new Vector3(10.5f,-.15f,0),new Vector3(3,.3f,4),stone,true);
        Box(gallery,"History connecting passage",new Vector3(0,-.15f,11),new Vector3(4,.3f,5),stone,true);
        foreach(int side in new[]{-1,1})Box(gallery,"History passage side",new Vector3(side*2.1f,2,11),new Vector3(.2f,4,4),plaster,true);
        Skeleton(gallery,"TheropodSkeleton",new Vector3(-24,0,0),"THEROPOD SKELETON STUDY");
        Skeleton(gallery,"HornedDinosaurSkeleton",new Vector3(-24,0,-5.5f),"HORNED DINOSAUR / INTERPRETIVE STUDY",.75f);
        Painting(gallery,436535,"Wheat Field with Cypresses","Vincent van Gogh / 1889",new Vector3(16,3.2f,8.30f),gold);
        Painting(gallery,436105,"The Death of Socrates","Jacques Louis David / 1787",new Vector3(24,3.2f,8.30f),gold);
        Painting(gallery,11417,"Washington Crossing the Delaware","Emanuel Leutze / 1851",new Vector3(32,3.2f,8.30f),gold);
        Label(gallery,"NATURAL HISTORY",new Vector3(-24,5.2f,8.72f),3,new Vector2(16,1));
        Label(gallery,"PAINTING / MEMORY / PERSPECTIVE",new Vector3(24,5.9f,8.20f),2.8f,new Vector2(23,1));
        Label(gallery,"HUMAN STORIES",new Vector3(0,5.2f,34.72f),3,new Vector2(16,1));
        Label(gallery,"NATURAL HISTORY  <     HUMAN STORIES  ^     >  PAINTINGS",new Vector3(0,3.5f,7.8f),3.2f,new Vector2(18,1));
        var olive=Resources.Load<GameObject>("LumenCollection/CityOlivePlanter");
        if(olive!=null)foreach(Vector3 p in new[]{new Vector3(-8,0,-6),new Vector3(8,0,-6),new Vector3(13,0,7),new Vector3(35,0,7)})
        {
            var plant=Object.Instantiate(olive,gallery,false);plant.transform.localPosition=p;plant.transform.localScale=Vector3.one*.75f;
            var collider=plant.AddComponent<BoxCollider>();collider.center=Vector3.up*.33f;collider.size=new Vector3(1.3f,.66f,1.3f);
        }
        foreach(Vector3 position in new[]{new Vector3(15,0,-5),new Vector3(24,0,-5),new Vector3(33,0,-5),new Vector3(-16,0,7)})
        {
            Box(gallery,"Museum visitor bench",position+Vector3.up*.48f,new Vector3(2.4f,.14f,.65f),wood,true);
            foreach(float x in new[]{-.9f,.9f})Box(gallery,"Bench leg",position+new Vector3(x,.23f,0),new Vector3(.1f,.46f,.5f),gold,false);
        }
    }

    public static void Populate(Transform gallery)
    {
        int i=0;
        foreach(Vector3 p in new[]{new Vector3(-18,0,2),new Vector3(-29,0,6),new Vector3(-20,0,-6),new Vector3(16,0,4),new Vector3(28,0,4),new Vector3(32,0,-2),new Vector3(-4,0,23),new Vector3(4,0,29)})
            ScenarioPopulation.Spawn(gallery,"Museum visitor "+(++i),gallery.TransformPoint(p),i%2==0?"CafeWoman":"Modern","museum visitor",true,"museum");
    }

    private static void Wing(Transform gallery,string name,Vector3 centre,Vector2 size,int doorSide,Material floor,Material wall,Material wood,Material gold)
    {
        var root=new GameObject(name);root.transform.SetParent(gallery,false);root.transform.localPosition=centre;
        float x=size.x/2,z=size.y/2;
        Box(root.transform,"Wing walkable floor",new Vector3(0,-.15f,0),new Vector3(size.x,.3f,size.y),floor,true);
        Box(root.transform,"Coffered ceiling",new Vector3(0,6.4f,0),new Vector3(size.x,.2f,size.y),wood,false);
        foreach(int side in new[]{-1,1})
        {
            if(side==doorSide)
            {
                foreach(int end in new[]{-1,1})Box(root.transform,"Side entrance wing",new Vector3(side*x,3.2f,end*(z+2)/2),new Vector3(.3f,6.4f,z-2),wall,true);
                Box(root.transform,"Side door lintel",new Vector3(side*x,5,0),new Vector3(.3f,2.8f,4),wall,true);
            }
            else Box(root.transform,"Wing side wall",new Vector3(side*x,3.2f,0),new Vector3(.3f,6.4f,size.y),wall,true);
            if(doorSide==0&&side==-1)
            {
                foreach(int end in new[]{-1,1})Box(root.transform,"History doorway wall",new Vector3(end*(x+2)/2,3.2f,-z),new Vector3(x-2,6.4f,.3f),wall,true);
                Box(root.transform,"History door lintel",new Vector3(0,5,-z),new Vector3(4,2.8f,.3f),wall,true);
            }
            else Box(root.transform,"Wing end wall",new Vector3(0,3.2f,side*z),new Vector3(size.x,6.4f,.3f),wall,true);
        }
        for(float p=-x+2;p<x;p+=4)
        {
            Box(root.transform,"Stone wall pilaster",new Vector3(p,3,z-.2f),new Vector3(.35f,6,.3f),floor,false);
            Box(root.transform,"Ceiling cross beam",new Vector3(p,6.12f,0),new Vector3(.22f,.3f,size.y),floor,false);
        }
        foreach(float p in new[]{-size.x*.25f,size.x*.25f})
        {
            var go=new GameObject("Warm museum wing fill");go.transform.SetParent(root.transform,false);go.transform.localPosition=new Vector3(p,5,0);
            var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.88f,.68f);light.intensity=30;light.range=22;
            Box(root.transform,"Gallery light fixture",new Vector3(p,5.8f,0),new Vector3(1.5f,.08f,.15f),gold,false);
        }
    }

    private static void Skeleton(Transform parent,string resource,Vector3 p,string title,float scale=1)
    {
        var prefab=Resources.Load<GameObject>("LumenCollection/"+resource);if(prefab==null)return;
        var exhibit=Object.Instantiate(prefab,parent,false);exhibit.transform.localPosition=p;exhibit.transform.localScale=Vector3.one*scale;
        exhibit.name=title;var data=exhibit.AddComponent<ObjectMetadata>();data.objectName=title;data.displayName=title;data.scenario="museum";data.category="Interpretive skeletal reconstruction";data.isInteractable=true;
        data.description="An original schematic skeletal reconstruction made for this exhibit. It shows the spine, ribs, limbs, tail and open skull structure. It is not a scanned specimen or a scientifically exact anatomical reconstruction.";data.descriptionSummary=data.description;
        Box(parent,"Exhibit caption backing",p+new Vector3(0,.43f,-2.0f),new Vector3(4.9f,.55f,.045f),Mat("Skeleton caption slate",new Color(.045f,.075f,.08f)),false);
        Label(parent,title,p+new Vector3(0,.55f,-2.04f),1.15f,new Vector2(4.7f,.24f));
        Label(parent,"ORIGINAL INTERPRETIVE MODEL / NOT A SCANNED SPECIMEN",p+new Vector3(0,.30f,-2.04f),.80f,new Vector2(4.7f,.20f));
        LearningModelInspection.MakeSelectable(exhibit);
        var spot=new GameObject("Skeleton exhibit lighting");spot.transform.SetParent(parent,false);spot.transform.localPosition=p+new Vector3(0,4.8f,-2.8f);spot.transform.LookAt(parent.TransformPoint(p+Vector3.up*1.5f));
        var light=spot.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=65;light.range=10;light.intensity=9;light.color=new Color(.96f,.94f,.87f);light.shadows=LightShadows.None;
    }

    private static void Painting(Transform parent,int number,string title,string artist,Vector3 position,Material frame)
    {
        Texture2D texture=Resources.Load<Texture2D>("MuseumPaintings/Met"+number);if(texture==null)return;
        float width=5.8f,height=width*texture.height/texture.width;
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name=title;quad.transform.SetParent(parent,false);quad.transform.localPosition=position;quad.transform.localScale=new Vector3(width,height,1);
        var template=Resources.Load<Material>("MuseumPaintings/PaintingSurface");
        var material=template!=null?LumenRuntimeResources.Own(new Material(template)):LumenRuntimeResources.Own(new Material(Shader.Find("Universal Render Pipeline/Unlit")));
        material.mainTexture=texture;quad.GetComponent<Renderer>().sharedMaterial=material;
        foreach(int s in new[]{-1,1})
        {
            Box(parent,"Bronze painting frame",position+new Vector3(s*(width/2+.06f),0,0),new Vector3(.12f,height+.24f,.10f),frame,false);
            Box(parent,"Bronze painting frame",position+new Vector3(0,s*(height/2+.06f),0),new Vector3(width+.24f,.12f,.10f),frame,false);
        }
        Vector3 caption=position+new Vector3(0,-height/2-.45f,-.22f);
        // Dark plaques keep the captions readable across pale wall pilasters.
        Box(parent,"Painting caption plaque",caption+new Vector3(0,0,.03f),new Vector3(6.6f,.75f,.035f),Mat("Painting caption backing",new Color(.045f,.035f,.025f)),false);
        Label(parent,title,caption+Vector3.up*.13f,2.4f,new Vector2(6.4f,.32f));
        Label(parent,artist+" / The Met / Public Domain",caption-Vector3.up*.17f,1.4f,new Vector2(6.4f,.28f));
        var data=quad.AddComponent<ObjectMetadata>();data.objectName=title;data.displayName=title;data.scenario="museum";data.category="Historical painting";data.isInteractable=true;
        data.description=title+" by "+artist+". Public-domain image from The Metropolitan Museum of Art Open Access collection, object "+number+". Interpret the painting as an artist's composition, not a photographic record.";data.descriptionSummary=data.description;
        LearningModelInspection.MakeSelectable(quad);
    }
    private static GameObject Box(Transform parent,string name,Vector3 p,Vector3 scale,Material material,bool collision)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<Collider>().enabled=collision;return go;
    }
    private static void Label(Transform parent,string content,Vector3 p,float size,Vector2 dimensions)
    {
        var go=new GameObject(content,typeof(TextMeshPro));go.transform.SetParent(parent,false);go.transform.localPosition=p;
        var t=go.GetComponent<TextMeshPro>();t.text=content;t.fontSize=size;t.color=new Color(.88f,.80f,.63f);t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=dimensions;
    }
    private static Material Mat(string name,Color color)=>LumenRuntimeResources.Own(new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color});
}

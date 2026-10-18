using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Clean first-launch world selection over the real 3D host and courtyard.</summary>
public sealed class OpeningExperienceScreen:MonoBehaviour
{
    private Transform host;
    private Camera openingCamera;
    private int previousMask;
    private CharacterGrounding grounding;
    private bool groundingWasEnabled;
    private readonly Dictionary<GameObject,int> hostLayers=new Dictionary<GameObject,int>();
    private void LateUpdate()
    {
        if(host==null)return;
        foreach(Transform part in host.GetComponentsInChildren<Transform>(true))
        {
            if(!hostLayers.ContainsKey(part.gameObject))hostLayers[part.gameObject]=part.gameObject.layer;
            part.gameObject.layer=31;
        }
    }
    private void OnDisable()
    {
        if(openingCamera!=null)openingCamera.cullingMask=previousMask;
        if(grounding!=null)grounding.enabled=groundingWasEnabled;
        foreach(var pair in hostLayers)if(pair.Key!=null)pair.Key.layer=pair.Value;
    }
    public static void Install(Canvas canvas)
    {
        var legacy=new List<GameObject>();foreach(Transform t in canvas.transform){legacy.Add(t.gameObject);t.gameObject.SetActive(false);}
        GameObject panel=Panel(canvas.transform,"OpeningExperience",Vector2.zero,Vector2.one,Color.clear);
        Text(panel.transform,"LUMEN / EXPLORE WITH COMPANY",new Vector2(.045f,.925f),new Vector2(.53f,.975f),16,new Color(.93f,.72f,.40f));
        Text(panel.transform,"DISCOVER\nTOGETHER",new Vector2(.045f,.72f),new Vector2(.52f,.92f),48,new Color(.97f,.94f,.86f));
        Text(panel.transform,"Meet your guide. Step into a world.\nAsk a question and follow your curiosity.",new Vector2(.045f,.63f),new Vector2(.52f,.72f),18,new Color(.78f,.84f,.85f));
        string[] titles={"ANCIENT ROME","SCIENCE STUDIO","THE CORNER TABLE","MUSEUM OF DISCOVERY"};
        string[] descriptions={"Streets, markets and the arena","Ideas you can explore in three dimensions","Conversation, company and a coffee","Natural history, paintings and human stories"};
        string[] scenarios={"ancient_rome","classroom","cafe","museum"};
        Color[] accents={new Color(.88f,.43f,.26f),new Color(.25f,.67f,.78f),new Color(.40f,.68f,.49f),new Color(.64f,.52f,.79f)};
        for(int i=0;i<4;i++)
        {
            int index=i;float top=.84f-i*.17f;
            var card=Panel(panel.transform,"Enter "+titles[i],new Vector2(.59f,top-.135f),new Vector2(.955f,top),new Color(.045f,.075f,.095f,.97f));
            var stripe=Panel(card.transform,"Accent",new Vector2(0,0),new Vector2(.012f,1),accents[i]);stripe.GetComponent<Image>().raycastTarget=false;
            Text(card.transform,"0"+(i+1),new Vector2(.05f,.62f),new Vector2(.17f,.92f),13,accents[i]);
            Text(card.transform,titles[i],new Vector2(.05f,.31f),new Vector2(.96f,.70f),23,Color.white);
            Text(card.transform,descriptions[i],new Vector2(.05f,.06f),new Vector2(.96f,.33f),13,new Color(.72f,.78f,.78f));
            card.AddComponent<Button>().onClick.AddListener(()=>ScenarioTransitionService.LoadScenario(scenarios[index]));
        }
        var explore=Panel(panel.transform,"ExploreCourtyard",new Vector2(.045f,.055f),new Vector2(.36f,.12f),new Color(.92f,.68f,.34f));
        Text(explore.transform,"EXPLORE THE COURTYARD",Vector2.zero,Vector2.one,16,new Color(.035f,.055f,.065f));
        var guide=GameObject.Find("HomeAgent");MonoBehaviour follower=null;
        Camera openingCamera=Camera.main;
        CameraClearFlags previousClear=openingCamera!=null?openingCamera.clearFlags:CameraClearFlags.Skybox;
        Color previousBackground=openingCamera!=null?openingCamera.backgroundColor:Color.black;
        if(guide!=null&&Camera.main!=null)
        {
            var screen=panel.AddComponent<OpeningExperienceScreen>();screen.host=guide.transform;screen.openingCamera=openingCamera;screen.previousMask=openingCamera.cullingMask;
            screen.grounding=guide.GetComponent<CharacterGrounding>();
            if(screen.grounding!=null){screen.groundingWasEnabled=screen.grounding.enabled;screen.grounding.enabled=false;}
            openingCamera.cullingMask=1<<31;screen.LateUpdate();
            openingCamera.clearFlags=CameraClearFlags.SolidColor;openingCamera.backgroundColor=new Color(.035f,.075f,.10f);
            foreach(var behaviour in guide.GetComponents<MonoBehaviour>())if(behaviour.GetType().Name=="WalkableGuideFollower"){follower=behaviour;follower.enabled=false;}
            Vector3 p=Camera.main.ViewportToWorldPoint(new Vector3(.28f,.35f,3.1f));p.y=.30f;guide.transform.position=p;
            Vector3 face=Camera.main.transform.position-p;face.y=0;guide.transform.rotation=Quaternion.LookRotation(face);
            var fill=new GameObject("Opening host soft fill");fill.transform.SetParent(guide.transform,false);fill.transform.localPosition=new Vector3(0,2,1.4f);
            var light=fill.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.88f,.78f);light.range=5;light.intensity=6;
        }
        MonoBehaviour capturedFollower=follower;
        explore.AddComponent<Button>().onClick.AddListener(()=>
        {
            foreach(var go in legacy)if(go!=null)go.SetActive(true);
            if(openingCamera!=null){openingCamera.clearFlags=previousClear;openingCamera.backgroundColor=previousBackground;}
            panel.SetActive(false);if(capturedFollower!=null)capturedFollower.enabled=true;
        });
        Text(panel.transform,"Four worlds. One curious mind.",new Vector2(.59f,.055f),new Vector2(.955f,.12f),15,new Color(.65f,.73f,.75f));
    }
    private static GameObject Panel(Transform parent,string name,Vector2 min,Vector2 max,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;go.GetComponent<Image>().color=color;return go;
    }
    private static void Text(Transform parent,string value,Vector2 min,Vector2 max,float size,Color color)
    {
        var go=new GameObject(value,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var t=go.GetComponent<TextMeshProUGUI>();t.rectTransform.anchorMin=min;t.rectTransform.anchorMax=max;t.rectTransform.offsetMin=new Vector2(4,2);t.rectTransform.offsetMax=new Vector2(-4,-2);
        t.font=TMP_Settings.defaultFontAsset;t.text=value;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;
    }
}

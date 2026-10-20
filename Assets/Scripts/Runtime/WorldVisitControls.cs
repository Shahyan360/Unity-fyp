using TMPro;
using UnityEngine;

/// <summary>One restrained visit drawer shared by the hub and every destination.</summary>
public sealed class WorldVisitControls:MonoBehaviour
{
    private GameObject drawer;
    private TMP_Text notes;
    private RectTransform notesViewport;
    public static void Install(Canvas canvas,string scene)
    {var owner=canvas.gameObject.AddComponent<WorldVisitControls>();owner.Build(canvas.transform,scene);LumenActivityPanel.Install(canvas);}
    private void Build(Transform root,string scene)
    {
        string title=scene.StartsWith("Home")?"LUMEN / DISCOVER":scene=="HistoricalPlace"?"ANCIENT ROME":scene=="Cafestore"?"THE CAFE":ScenarioRuntimeManager.GetScenarioForScene(scene)=="museum"?"MUSEUM":"CLASSROOM";
        var header=Panel(root,"VisitHeader",new Vector2(.025f,.925f),new Vector2(.545f,.985f));
        Label(header.transform,title,new Vector2(.035f,.025f),new Vector2(.73f,.975f),22);
        var open=Button(header.transform,"VISIT",new Vector2(.76f,.08f),new Vector2(.97f,.92f));
        drawer=Panel(root,"VisitSettings",new Vector2(.025f,.075f),new Vector2(.59f,.915f));
        Label(drawer.transform,"YOUR VISIT",new Vector2(.045f,.92f),new Vector2(.95f,.985f),25);
        Label(drawer.transform,"Tab: focus · Enter: choose · Esc: close\nRight-click or Numpad: look · E: interact",new Vector2(.045f,.82f),new Vector2(.95f,.92f),17);
        var quality=Button(drawer.transform,"Graphics: "+WorldPresentationQuality.SelectedQuality,new Vector2(.045f,.73f),new Vector2(.49f,.81f));
        quality.onClick.AddListener(()=>quality.GetComponentInChildren<TMP_Text>().text="Graphics: "+WorldPresentationQuality.CycleQuality());
        var cues=Button(drawer.transform,"UI cues: "+LumenPreferences.GetInt("Lumen.UIVolume",80)+"%",new Vector2(.51f,.73f),new Vector2(.95f,.81f));
        cues.onClick.AddListener(()=>
        {
            int level=LumenPreferences.GetInt("Lumen.UIVolume",80);level=level==0?50:level==50?80:level==80?100:0;LumenPreferences.SetInt("Lumen.UIVolume",level);LumenPreferences.Save();
            foreach(var source in FindObjectsByType<AudioSource>())if(source.outputAudioMixerGroup!=null&&source.outputAudioMixerGroup.name=="UI")source.volume=.34f*level/100f;
            cues.GetComponentInChildren<TMP_Text>().text="UI cues: "+level+"%";
        });
        var text=Button(drawer.transform,TextLabel(),new Vector2(.045f,.64f),new Vector2(.49f,.72f));
        text.onClick.AddListener(()=>{LumenPreferences.SetInt("Lumen.TextTier",(LumenAccessibility.TextTier+1)%3);LumenPreferences.Save();text.GetComponentInChildren<TMP_Text>().text=TextLabel();LumenAccessibility.Refresh();});
        var motion=Button(drawer.transform,MotionLabel(),new Vector2(.51f,.64f),new Vector2(.95f,.72f));
        motion.onClick.AddListener(()=>{LumenPreferences.SetInt("Lumen.ReducedMotion",LumenAccessibility.ReducedMotion?0:1);LumenPreferences.Save();motion.GetComponentInChildren<TMP_Text>().text=MotionLabel();});
        var movement=Button(drawer.transform,"Move: "+LumenAccessibility.MovementLabel,new Vector2(.045f,.55f),new Vector2(.95f,.63f));
        movement.onClick.AddListener(()=>{LumenPreferences.SetInt("Lumen.MovementLayout",(LumenAccessibility.MovementLayout+1)%3);LumenPreferences.Save();movement.GetComponentInChildren<TMP_Text>().text="Move: "+LumenAccessibility.MovementLabel;});
        var voice=Button(drawer.transform,VoiceLabel(),new Vector2(.045f,.46f),new Vector2(.49f,.54f));
        voice.onClick.AddListener(()=>
        {
            int volume=LumenPreferences.GetInt("Lumen.SpeechVolume",90);volume=volume==0?60:volume==60?90:volume==90?100:0;
            LumenPreferences.SetInt("Lumen.SpeechVolume",volume);LumenPreferences.SetInt("Lumen.MuteVoice",volume==0?1:0);LumenPreferences.Save();
            foreach(var output in FindObjectsByType<SpeechOutputController>()){output.SetVoiceMuted(volume==0);output.SetVoiceVolume(volume/100f);}
            GeminiLiveConversation.Instance?.SetVoiceMuted(volume==0);GeminiLiveConversation.Instance?.SetVoiceVolume(volume/100f);
            voice.GetComponentInChildren<TMP_Text>().text=VoiceLabel();
        });
        var visual=Button(drawer.transform,VisualLabel(),new Vector2(.51f,.46f),new Vector2(.95f,.54f));
        visual.onClick.AddListener(()=>{bool on=LumenPreferences.GetInt("Lumen.VisualAids",1)==0;LumenPreferences.SetInt("Lumen.VisualAids",on?1:0);LumenPreferences.Save();if(!on)GeminiLiveConversation.Instance?.Images?.ResetConversation();visual.GetComponentInChildren<TMP_Text>().text=VisualLabel();});
        var connection=Button(drawer.transform,ConnectionLabel(),new Vector2(.045f,.37f),new Vector2(.95f,.45f));
        connection.onClick.AddListener(()=>{bool offline=LumenPreferences.GetInt("Lumen.Offline",0)==0;if(offline)FindAnyObjectByType<SocialCoachingController>()?.EndPractice();LumenPreferences.SetInt("Lumen.Offline",offline?1:0);LumenPreferences.Save();GeminiLiveConversation.Instance?.Reconnect();connection.GetComponentInChildren<TMP_Text>().text=ConnectionLabel();});
        var forget=Button(drawer.transform,"Forget conversation",new Vector2(.045f,.28f),new Vector2(.49f,.36f));
        forget.onClick.AddListener(()=>{VisitorConversationMemory.Reset();FindAnyObjectByType<AIClient>()?.ResetSession();GeminiLiveConversation.Instance?.Reconnect();});
        var reset=Button(drawer.transform,"Reset discoveries",new Vector2(.51f,.28f),new Vector2(.95f,.36f));
        reset.onClick.AddListener(()=>{ExplorationNotebook.Reset();StartCoroutine(RefreshNotes());});
        Label(drawer.transform,"DISCOVERY NOTEBOOK / LOCAL TO THIS DEVICE",new Vector2(.045f,.20f),new Vector2(.95f,.27f),17);
        var scrollRoot=Panel(drawer.transform,"Notebook scroll",new Vector2(.045f,.025f),new Vector2(.95f,.20f));
        var scroll=scrollRoot.AddComponent<UnityEngine.UI.ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
        var viewport=Panel(scrollRoot.transform,"Viewport",Vector2.zero,Vector2.one);viewport.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=false;notesViewport=(RectTransform)viewport.transform;scroll.viewport=notesViewport;
        var content=new GameObject("Content",typeof(RectTransform),typeof(UnityEngine.UI.VerticalLayoutGroup),typeof(UnityEngine.UI.ContentSizeFitter));content.transform.SetParent(viewport.transform,false);
        var contentRect=(RectTransform)content.transform;contentRect.anchorMin=new Vector2(0,1);contentRect.anchorMax=Vector2.one;contentRect.pivot=new Vector2(.5f,1);contentRect.offsetMin=contentRect.offsetMax=Vector2.zero;scroll.content=contentRect;
        var layout=content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.padding=new RectOffset(8,8,8,8);
        content.GetComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        notes=Label(content.transform,ExplorationNotebook.Summary(),Vector2.zero,Vector2.one,18);notes.overflowMode=TextOverflowModes.Overflow;notes.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        open.onClick.AddListener(()=>{GameObject.Find("Guide choices")?.SetActive(false);drawer.SetActive(!drawer.activeSelf);LumenAccessibility.MenuOpen=drawer.activeSelf;if(drawer.activeSelf)StartCoroutine(RefreshNotes());});
        drawer.SetActive(false);
    }
    private System.Collections.IEnumerator RefreshNotes()
    {
        notes.text=ExplorationNotebook.Summary();yield return null;Canvas.ForceUpdateCanvases();
        notes.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight=notes.GetPreferredValues(notes.text,Mathf.Max(100,notesViewport.rect.width-16),float.PositiveInfinity).y+8;
    }
    private static string TextLabel()=>"Text: "+(LumenAccessibility.TextTier==0?"standard":LumenAccessibility.TextTier==1?"large":"largest");
    private static string MotionLabel()=>"Motion: "+(LumenAccessibility.ReducedMotion?"reduced":"standard");
    private static string VoiceLabel()=>"Speech: "+LumenPreferences.GetInt("Lumen.SpeechVolume",90)+"%";
    private static string VisualLabel()=>"Images: "+(LumenPreferences.GetInt("Lumen.VisualAids",1)==0?"off":"optional");
    private static string ConnectionLabel()=>LumenPreferences.GetInt("Lumen.Offline",0)!=0?"Connect assistant":"Switch to offline labels";
    private static GameObject Panel(Transform root,string name,Vector2 min,Vector2 max)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(root,false);Place((RectTransform)go.transform,min,max);
        go.GetComponent<UnityEngine.UI.Image>().color=new Color(.055f,.105f,.12f,.985f);return go;
    }
    private static TMP_Text Label(Transform root,string value,Vector2 min,Vector2 max,float size)
    {
        var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(root,false);
        var label=go.GetComponent<TMP_Text>();Place(label.rectTransform,min,max);label.text=value;label.fontSize=size;label.color=new Color(.96f,.95f,.91f);label.font=TMP_Settings.defaultFontAsset;label.raycastTarget=false;label.alignment=TextAlignmentOptions.MidlineLeft;label.richText=false;return label;
    }
    private static UnityEngine.UI.Button Button(Transform root,string value,Vector2 min,Vector2 max)
    {
        var go=Panel(root,"VisitButton",min,max);go.GetComponent<UnityEngine.UI.Image>().color=new Color(.12f,.26f,.28f);
        var button=go.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=go.GetComponent<UnityEngine.UI.Image>();
        Label(go.transform,value,new Vector2(.04f,.06f),new Vector2(.96f,.94f),18).alignment=TextAlignmentOptions.Center;return button;
    }
    private static void Place(RectTransform rect,Vector2 min,Vector2 max){rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
}

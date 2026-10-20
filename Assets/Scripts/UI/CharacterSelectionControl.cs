using TMPro;
using UnityEngine;

public sealed class CharacterSelectionControl:MonoBehaviour
{
    public static void Install(Canvas canvas,string scene)
    {
        string key="Lumen.Character."+scene;
        string selected=LumenPreferences.GetString(key,CharacterWardrobe.Default(scene));
        var button=Button(canvas.transform,"SwitchGuideCharacter","Guide: "+CharacterWardrobe.Label(selected),new Vector2(.555f,.925f),new Vector2(.775f,.985f));
        button.gameObject.AddComponent<GuideTargetLabel>();
        var menu=Panel(canvas.transform,"Guide choices",new Vector2(.025f,.24f),new Vector2(.33f,.915f));
        var portraitWell=Panel(menu.transform,"Selected guide portrait",new Vector2(.04f,.72f),new Vector2(.37f,.97f));
        var portraitObject=new GameObject("Portrait",typeof(RectTransform),typeof(UnityEngine.UI.RawImage),typeof(UnityEngine.UI.AspectRatioFitter));portraitObject.transform.SetParent(portraitWell.transform,false);
        var portrait=portraitObject.GetComponent<UnityEngine.UI.RawImage>();portrait.raycastTarget=false;
        var aspect=portraitObject.GetComponent<UnityEngine.UI.AspectRatioFitter>();aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
        var name=Label(menu.transform,"",new Vector2(.41f,.84f),new Vector2(.96f,.96f),23);
        var role=Label(menu.transform,"",new Vector2(.41f,.72f),new Vector2(.96f,.84f),18);
        void RefreshPortrait()
        {
            name.text=CharacterWardrobe.Label(selected);role.text=LumenCharacterDefinitions.ForStyle(selected)?.role??"Your guide";
            portrait.texture=Resources.Load<Texture2D>("GuidePortraits/"+selected);
            if(portrait.texture!=null)aspect.aspectRatio=portrait.texture.width/(float)portrait.texture.height;
        }
        RefreshPortrait();
        for(int i=0;i<CharacterWardrobe.Styles.Length;i++)
        {
            string style=CharacterWardrobe.Styles[i];
            var choice=Button(menu.transform,"Choose "+style,CharacterWardrobe.Label(style),new Vector2(.04f,.65f-(i+1)*.078f),new Vector2(.96f,.642f-i*.078f));
            choice.onClick.AddListener(()=>
            {
                selected=style;LumenPreferences.SetString(key,style);LumenPreferences.Save();
                FindAnyObjectByType<ScenarioCharacterIdentity>()?.ApplyAppearance(style);
                GeminiLiveConversation.Instance?.SelectPartner(null);RefreshPortrait();menu.SetActive(false);LumenAccessibility.MenuOpen=false;
            });
        }
        var helper=Button(menu.transform,"Return to helper","Return to companion",new Vector2(.04f,.035f),new Vector2(.96f,.135f));
        helper.gameObject.AddComponent<ConversationPartnerLabel>();
        helper.onClick.AddListener(()=>
        {
            var live=GeminiLiveConversation.Instance;
            if(live!=null){if(live.Partner!=null)live.SelectPartner(null);else if(!live.Ready)live.Reconnect();}
            menu.SetActive(false);LumenAccessibility.MenuOpen=false;
        });
        menu.SetActive(false);
        button.onClick.AddListener(()=>
        {
            GameObject.Find("VisitSettings")?.SetActive(false);menu.SetActive(!menu.activeSelf);LumenAccessibility.MenuOpen=menu.activeSelf;
        });
    }
    private static GameObject Panel(Transform parent,string name,Vector2 min,Vector2 max)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        go.GetComponent<UnityEngine.UI.Image>().color=new Color(.055f,.105f,.12f,.985f);return go;
    }
    private static TMP_Text Label(Transform parent,string text,Vector2 min,Vector2 max,float size)
    {
        var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var label=go.GetComponent<TMP_Text>();label.rectTransform.anchorMin=min;label.rectTransform.anchorMax=max;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
        label.font=TMP_Settings.defaultFontAsset;label.fontSize=size;label.color=new Color(.96f,.95f,.91f);label.text=text;label.raycastTarget=false;label.alignment=TextAlignmentOptions.MidlineLeft;return label;
    }
    private static UnityEngine.UI.Button Button(Transform parent,string name,string text,Vector2 min,Vector2 max)
    {
        var go=Panel(parent,name,min,max);go.GetComponent<UnityEngine.UI.Image>().color=new Color(.12f,.26f,.28f);
        var result=go.AddComponent<UnityEngine.UI.Button>();result.targetGraphic=go.GetComponent<UnityEngine.UI.Image>();
        var label=Label(go.transform,text,new Vector2(.05f,.08f),new Vector2(.95f,.92f),18);label.alignment=TextAlignmentOptions.Center;
        return result;
    }
}

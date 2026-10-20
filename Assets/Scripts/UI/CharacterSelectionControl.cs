using PlayerPrefs = LumenPreferences;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterSelectionControl:MonoBehaviour
{
    public static void Install(Canvas canvas,string scene)
    {
        var go=new GameObject("SwitchGuideCharacter",typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(canvas.transform,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=new Vector2(.57f,.925f);rect.anchorMax=new Vector2(.77f,.98f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        go.GetComponent<Image>().color=new Color(.14f,.25f,.28f,.97f);
        var textGO=new GameObject("CharacterLabel",typeof(RectTransform),typeof(TextMeshProUGUI));textGO.transform.SetParent(go.transform,false);
        var label=textGO.GetComponent<TextMeshProUGUI>();var tr=label.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(5,2);tr.offsetMax=new Vector2(-5,-2);
        label.font=TMP_Settings.defaultFontAsset;label.fontSize=13;label.color=Color.white;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
        string key="Lumen.Character."+scene;
        string selected=PlayerPrefs.GetString(key,CharacterWardrobe.Default(scene));
        label.text="CHANGE GUIDE / "+CharacterWardrobe.Label(selected);
        var menu=new GameObject("Guide choices",typeof(RectTransform),typeof(Image));menu.transform.SetParent(canvas.transform,false);
        var mr=(RectTransform)menu.transform;mr.anchorMin=new Vector2(.57f,.46f);mr.anchorMax=new Vector2(.79f,.855f);mr.offsetMin=mr.offsetMax=Vector2.zero;
        menu.GetComponent<Image>().color=new Color(.035f,.07f,.09f,.98f);
        for(int i=0;i<CharacterWardrobe.Styles.Length;i++)
        {
            string style=CharacterWardrobe.Styles[i];
            var choice=new GameObject("Choose "+style,typeof(RectTransform),typeof(Image),typeof(Button));choice.transform.SetParent(menu.transform,false);
            var cr=(RectTransform)choice.transform;cr.anchorMin=new Vector2(.03f,1-(i+1f)/7);cr.anchorMax=new Vector2(.97f,1-(i+.1f)/7);cr.offsetMin=cr.offsetMax=Vector2.zero;
            choice.GetComponent<Image>().color=new Color(.12f,.25f,.28f);
            var txt=Instantiate(label,choice.transform);txt.text=CharacterWardrobe.Label(style);
            choice.GetComponent<Button>().onClick.AddListener(()=>{
                selected=style;PlayerPrefs.SetString(key,style);PlayerPrefs.Save();
                var identity=FindAnyObjectByType<ScenarioCharacterIdentity>();if(identity!=null)identity.ApplyAppearance(style);
                GeminiLiveConversation.Instance?.SelectPartner(null);
                label.text="GUIDES / "+CharacterWardrobe.Label(style);menu.SetActive(false);
            });
        }
        menu.SetActive(false);go.GetComponent<Button>().onClick.AddListener(()=>menu.SetActive(!menu.activeSelf));
        var helper=Instantiate(go,canvas.transform);helper.name="Return to helper";
        var hr=(RectTransform)helper.transform;hr.anchorMin=new Vector2(.57f,.865f);hr.anchorMax=new Vector2(.77f,.915f);
        helper.GetComponentInChildren<TMP_Text>().text="TALK TO MY HELPER";
        helper.GetComponent<Button>().onClick.RemoveAllListeners();helper.GetComponent<Button>().onClick.AddListener(()=>{var live=GeminiLiveConversation.Instance;if(live!=null){if(live.Partner!=null)live.SelectPartner(null);else if(!live.Ready)live.Reconnect();}});
        helper.AddComponent<ConversationPartnerLabel>();
    }
}

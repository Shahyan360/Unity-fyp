using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Optional rehearsal and reversible inspection remain explicit visitor choices.</summary>
public sealed class LumenActivityPanel:MonoBehaviour
{
    private GameObject practice,inspection,welcome;
    private TMP_Text practiceState,inspectLabel,assistant;
    private float next;
    public static void Install(Canvas canvas)
    {
        var owner=canvas.gameObject.AddComponent<LumenActivityPanel>();owner.Build(canvas.transform);
    }
    private void Build(Transform root)
    {
        var statePanel=Panel(root,"Assistant connection state",new Vector2(.555f,.86f),new Vector2(.975f,.92f));
        assistant=Label(statePanel.transform,"Assistant connecting",new Vector2(.025f,.05f),new Vector2(.975f,.95f),18);
        var open=Button(root,"Optional practice",new Vector2(.76f,.80f),new Vector2(.975f,.855f));
        practice=Panel(root,"Optional practice panel",new Vector2(.49f,.36f),new Vector2(.975f,.79f));
        Label(practice.transform,"PRACTISE AT YOUR PACE",new Vector2(.04f,.84f),new Vector2(.96f,.98f),23);
        Label(practice.transform,"Three short turns with your companion.\nNo scores. Leave whenever you wish.",new Vector2(.04f,.65f),new Vector2(.96f,.85f),18);
        string[] modes={"conversation","interview","presentation"};
        for(int i=0;i<modes.Length;i++)
        {
            string mode=modes[i];var choose=Button(practice.transform,mode,new Vector2(.04f+i*.31f,.49f),new Vector2(.33f+i*.31f,.64f));
            choose.onClick.AddListener(()=>{GeminiLiveConversation.Instance?.SelectPartner(null);FindAnyObjectByType<SocialCoachingController>()?.StartPractice(mode);});
        }
        practiceState=Label(practice.transform,"Choose a mode to begin.",new Vector2(.04f,.18f),new Vector2(.96f,.47f),18);
        var end=Button(practice.transform,"End practice",new Vector2(.04f,.025f),new Vector2(.64f,.17f));end.onClick.AddListener(()=>FindAnyObjectByType<SocialCoachingController>()?.EndPractice());
        var close=Button(practice.transform,"Close",new Vector2(.67f,.025f),new Vector2(.96f,.17f));close.onClick.AddListener(()=>{practice.SetActive(false);LumenAccessibility.MenuOpen=false;});
        open.onClick.AddListener(()=>{practice.SetActive(!practice.activeSelf);LumenAccessibility.MenuOpen=practice.activeSelf;});practice.SetActive(false);
        inspection=Panel(root,"Exhibit inspection controls",new Vector2(.23f,.75f),new Vector2(.76f,.815f));
        inspectLabel=Label(inspection.transform,"EXHIBIT",new Vector2(.02f,.10f),new Vector2(.23f,.90f),18);
        string[] controls={"Inspect","Rotate","Zoom","Reset"};
        for(int i=0;i<controls.Length;i++)
        {
            int index=i;var button=Button(inspection.transform,controls[i],new Vector2(.24f+i*.19f,.08f),new Vector2(.42f+i*.19f,.92f));
            button.onClick.AddListener(()=>{var model=LearningModelInspection.Current;if(model==null)return;if(index==0)model.Toggle();else if(index==1)model.Rotate();else if(index==2)model.Zoom();else model.ResetView();});
        }
        if(!LumenPreferences.HasKey("Lumen.VoiceChoiceSeen"))
        {
            welcome=Panel(root,"Choose how to converse",new Vector2(.64f,.45f),new Vector2(.975f,.79f));
            Label(welcome.transform,"WELCOME TO LUMEN",new Vector2(.05f,.82f),new Vector2(.95f,.97f),24);
            Label(welcome.transform,"Explore with text, or switch on your microphone. Captions stay visible.\n\nConnected voice sends audio to the configured AI provider through your local service. Offline speech recognition uses this device. You can switch off the microphone at any time.",new Vector2(.05f,.24f),new Vector2(.95f,.81f),18);
            var text=Button(welcome.transform,"Explore with text",new Vector2(.05f,.04f),new Vector2(.49f,.21f));text.onClick.AddListener(()=>ChooseVoice(false));
            var mic=Button(welcome.transform,"Enable microphone",new Vector2(.51f,.04f),new Vector2(.95f,.21f));mic.onClick.AddListener(()=>ChooseVoice(true));
        }
    }
    private void ChooseVoice(bool enable)
    {
        LumenPreferences.SetInt("Lumen.VoiceChoiceSeen",1);LumenPreferences.Save();SpeechInputController.Instance?.SetListening(enable);Destroy(welcome);
    }
    private void Update()
    {
        if(Time.unscaledTime<next)return;next=Time.unscaledTime+.25f;
        var live=GeminiLiveConversation.Instance;assistant.text=live!=null?live.State:"Assistant unavailable / open Guide to retry";
        var coach=FindAnyObjectByType<SocialCoachingController>();
        if(coach!=null)practiceState.text=coach.State=="Off"?"Choose a mode to begin.":coach.State+" / "+coach.ActiveMode+" / "+Mathf.Clamp(coach.Turn,1,3)+" of 3\n"+coach.CurrentPrompt;
        var model=LearningModelInspection.Current;inspection.SetActive(model!=null);if(model!=null)
        {
            inspectLabel.text=model.IsInspecting?"Esc: return":"EXHIBIT";
            inspection.GetComponentsInChildren<Button>()[0].GetComponentInChildren<TMP_Text>().text=model.IsInspecting?"Return":"Inspect";
        }
    }
    private static GameObject Panel(Transform root,string name,Vector2 min,Vector2 max)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(root,false);Place((RectTransform)go.transform,min,max);go.GetComponent<Image>().color=new Color(.055f,.105f,.12f,.985f);return go;
    }
    private static TMP_Text Label(Transform root,string value,Vector2 min,Vector2 max,float size)
    {
        var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(root,false);var text=go.GetComponent<TMP_Text>();Place(text.rectTransform,min,max);text.text=value;text.font=TMP_Settings.defaultFontAsset;text.fontSize=size;text.color=new Color(.96f,.95f,.91f);text.raycastTarget=false;return text;
    }
    private static Button Button(Transform root,string text,Vector2 min,Vector2 max)
    {
        var go=Panel(root,text,min,max);go.GetComponent<Image>().color=new Color(.12f,.26f,.28f);var result=go.AddComponent<Button>();result.targetGraphic=go.GetComponent<Image>();Label(go.transform,text,new Vector2(.03f,.05f),new Vector2(.97f,.95f),18).alignment=TextAlignmentOptions.Center;return result;
    }
    private static void Place(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}
}

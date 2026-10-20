using PlayerPrefs = LumenPreferences;
using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>One cancellable, attributed topic image beside the current conversation.</summary>
public sealed class ConversationImagePanel : MonoBehaviour
{
    [Serializable] private sealed class Search { public string query; }
    [Serializable] private sealed class Result
    {
        public bool ok,cached;
        public string query,title,description,author,credit,license,license_url,source_url,image_path,message,modification;
        public int width,height;
    }
    private Canvas canvas;
    private GameObject panel;
    private RectTransform panelRect;
    private UnityEngine.UI.RawImage picture;
    private UnityEngine.UI.AspectRatioFitter aspect;
    private TMP_Text title,status,credits,expandLabel;
    private Texture2D texture;
    private UnityWebRequest activeRequest;
    private Coroutine search;
    private Result current;
    private bool expanded,dockRight,automaticImages=true;
    private string backend,question="",topic="",lastTopic="";
    private int revision;
    private float nextDockCheck;
    public string State {get;private set;}="Idle";
    public string Topic=>topic;
    public string ImageTitle=>current!=null?current.title:"";
    public string SourceUrl=>current!=null?current.source_url:"";
    public bool HasImage=>texture!=null;
    public int ImagesShown {get;private set;}
    public bool RequestedForTurn {get;private set;}
    public bool CanShowForQuestion=>!string.IsNullOrWhiteSpace(question)&&!SuppressesVisual(question)&&PlayerPrefs.GetInt("Lumen.VisualAids",1)!=0&&(automaticImages||RequestsVisual(question));
    public static bool SuppressesVisual(string text)=>System.Text.RegularExpressions.Regex.IsMatch(text??"",@"(?i)\b(verbal(?:ly)?[ -]only|words?[ -]only|without (?:any )?(?:images?|pictures?|visuals?)|no (?:more )?(?:images?|pictures?|photos?|visuals?))\b|\b(don'?t|do not|stop)\s+(show|showing|display|displaying|need|want)\b");

    public static bool RequestsVisual(string text)
    {
        string q=text??"";
        if(System.Text.RegularExpressions.Regex.IsMatch(q,@"(?i)\b(don'?t|do not|stop)\s+(show|showing|display|displaying|need|want)|\b(no|without)\s+(more\s+)?(images?|pictures?|photos?|diagrams?)\b"))return false;
        return System.Text.RegularExpressions.Regex.IsMatch(q,@"(?i)\b(show|display|find|fetch|search|get|give|bring|open|see|want|need|use|include|with)\b.{0,65}\b(images?|pictures?|photos?|photographs?|diagrams?|illustrations?|visuals?|visual aid)\b|\b(images?|pictures?|photos?|diagrams?)\s+(of|please)\b|\bvisuali[sz]e\b|\bvisual explanation\b|\b(show|display)\s+me\s+(?!how\b|where\b|the menu\b|menu\b)");
    }

    private void EnsureUi()
    {
        if(canvas!=null)return;
        var root=new GameObject("Conversation image canvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
        root.transform.SetParent(transform,false);canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=900;canvas.overrideSorting=true;
        var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        panel=Box(root.transform,"Topic image",new Vector2(.025f,.36f),new Vector2(.35f,.85f),new Color(.028f,.055f,.075f,1));
        panelRect=(RectTransform)panel.transform;
        Label(panel.transform,"LOOK CLOSER",new Vector2(.045f,.91f),new Vector2(.65f,.985f),13,new Color(.42f,.85f,.8f));
        expandLabel=Button(panel.transform,"+",new Vector2(.77f,.912f),new Vector2(.87f,.982f),()=>SetExpanded(!expanded)).GetComponentInChildren<TMP_Text>();
        Button(panel.transform,"X",new Vector2(.88f,.912f),new Vector2(.98f,.982f),()=>{CancelSearch();panel.SetActive(false);});
        title=Label(panel.transform,"A picture for your question",new Vector2(.045f,.81f),new Vector2(.955f,.91f),21,Color.white);
        title.overflowMode=TextOverflowModes.Ellipsis;
        var well=Box(panel.transform,"Image area",new Vector2(.045f,.285f),new Vector2(.955f,.805f),new Color(.94f,.95f,.94f));
        var image=new GameObject("Fetched image",typeof(RectTransform),typeof(UnityEngine.UI.RawImage),typeof(UnityEngine.UI.AspectRatioFitter));
        image.transform.SetParent(well.transform,false);
        var rect=(RectTransform)image.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        picture=image.GetComponent<UnityEngine.UI.RawImage>();picture.raycastTarget=false;
        aspect=image.GetComponent<UnityEngine.UI.AspectRatioFitter>();aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
        status=Label(well.transform,"",new Vector2(.05f,.05f),new Vector2(.95f,.95f),19,new Color(.1f,.2f,.23f));status.alignment=TextAlignmentOptions.Center;
        credits=Label(panel.transform,"Images are retrieved live with source credits.",new Vector2(.045f,.085f),new Vector2(.955f,.275f),13,new Color(.8f,.88f,.9f));
        credits.overflowMode=TextOverflowModes.Ellipsis;
        Button(panel.transform,"RETRY",new Vector2(.045f,.018f),new Vector2(.25f,.078f),()=>RequestImage(topic,backend));
        Button(panel.transform,"SOURCE",new Vector2(.28f,.018f),new Vector2(.50f,.078f),()=>OpenSource(false));
        Button(panel.transform,"LICENSE",new Vector2(.53f,.018f),new Vector2(.75f,.078f),()=>OpenSource(true));
        Button(root.transform,"PICTURE",new Vector2(.025f,.86f),new Vector2(.097f,.90f),()=>panel.SetActive(!panel.activeSelf));
        panel.SetActive(false);picture.enabled=false;
    }
    public void BeginQuestion(string text,bool automatic=true)
    {
        EnsureUi();DockBesideSpeaker();question=text??"";automaticImages=automatic;RequestedForTurn=false;
        if(SuppressesVisual(question)){PlayerPrefs.SetInt("Lumen.VisualAids",0);PlayerPrefs.Save();}
        else if(RequestsVisual(question)){PlayerPrefs.SetInt("Lumen.VisualAids",1);PlayerPrefs.Save();}
        CancelSearch();ClearImage();current=null;State="Choosing topic";
        title.text="A picture for your question";status.text="Choosing a relevant image…";credits.text="Your explanation will continue while the image loads.";
        if(!CanShowForQuestion){State="Idle";status.text="Ask for a picture or visual explanation when you need one.";credits.text="Images in the café are optional.";}
        panel.SetActive(CanShowForQuestion);
    }
    public void UpdateQuestion(string text)
    {
        question=text??"";
        if(SuppressesVisual(question)){PlayerPrefs.SetInt("Lumen.VisualAids",0);PlayerPrefs.Save();CancelSearch();if(panel!=null)panel.SetActive(false);}
    }
    public void RequestImage(string query,string backendUrl)
    {
        EnsureUi();DockBesideSpeaker();RequestedForTurn=true;backend=backendUrl.TrimEnd('/');topic=(query??"").Trim();
        if(topic.Length>160)topic=topic.Substring(0,160);
        CancelSearch();ClearImage();current=null;panel.SetActive(true);
        title.text=string.IsNullOrWhiteSpace(topic)?"No image available":topic;status.text="Looking up an image…";credits.text="Finding a source and checking image credits.";State="Searching";
        if(string.IsNullOrWhiteSpace(topic)){Unavailable("Please name a topic to see its image.");return;}
        search=StartCoroutine(Fetch(revision));
    }
    public void CompleteAnswer(string answer,string backendUrl)
    {
        if(string.IsNullOrWhiteSpace(question))return; // Automatic greetings are not visitor image requests.
        if(!CanShowForQuestion)return;
        // A missed model tool must not silently leave an old image beside a new answer.
        if(RequestedForTurn)return;
        string q=question.Trim();
        if(q.Length==0){Unavailable("Ask a question to see a related image.");return;}
        if(System.Text.RegularExpressions.Regex.IsMatch(q,@"(?i)\b(transformer|mercury|python)\b")&&System.Text.RegularExpressions.Regex.IsMatch(answer??"",@"(?i)\b(do you mean|which|electrical or|clarify)\b"))
        {Unavailable("Clarify which meaning you want, and I’ll find its image.");return;}
        string query=System.Text.RegularExpressions.Regex.Replace(q,@"(?i)^(please\s+)?(can you\s+)?(tell me about|show me( a(n)? (picture|image|model) of)?|what('s| is| are)|explain)\s+","").Trim(' ','?','.','!');
        bool followup=System.Text.RegularExpressions.Regex.IsMatch(query,@"(?i)\b(it|that|this|they|them)\b")||
            System.Text.RegularExpressions.Regex.IsMatch(query,@"(?i)^(why|how|tell me more)$");
        if(followup&&!string.IsNullOrEmpty(lastTopic))query=lastTopic;
        RequestImage(query,backendUrl);
    }
    public void ResetConversation()
    {
        CancelSearch();ClearImage();current=null;question=topic=lastTopic="";RequestedForTurn=false;State="Idle";
        if(title!=null){title.text="A picture for your question";status.text="Ask a question to see a related image.";credits.text="Images are retrieved live with source credits.";}
        if(panel!=null)panel.SetActive(false);
    }
    private IEnumerator Fetch(int token)
    {
        byte[] body=Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Search{query=topic}));
        using(var request=new UnityWebRequest(backend+"/images/search","POST"))
        {
            activeRequest=request;request.uploadHandler=new UploadHandlerRaw(body);request.downloadHandler=new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type","application/json");request.timeout=25;yield return request.SendWebRequest();activeRequest=null;
            if(token!=revision)yield break;
            if(request.result!=UnityWebRequest.Result.Success){Unavailable("Image search is unavailable. Continue chatting or retry.");yield break;}
            try{current=JsonUtility.FromJson<Result>(request.downloadHandler.text);}catch(Exception){current=null;}
        }
        if(current==null||!current.ok){Unavailable(current?.message??"No suitable image was found.");yield break;}
        if(!System.Text.RegularExpressions.Regex.IsMatch(current.image_path??"",@"^/images/file/[a-f0-9]{64}$"))
        {Unavailable("The image service returned an invalid image reference.");yield break;}
        State="Loading";status.text="Loading the image…";
        using(var request=UnityWebRequestTexture.GetTexture(backend+current.image_path,true))
        {
            activeRequest=request;request.timeout=12;yield return request.SendWebRequest();activeRequest=null;
            if(token!=revision)yield break;
            if(request.result!=UnityWebRequest.Result.Success){Unavailable("The image could not load. Please retry.");yield break;}
            texture=DownloadHandlerTexture.GetContent(request);
        }
        if(texture==null){Unavailable("The image could not be decoded.");yield break;}
        picture.texture=texture;picture.enabled=true;aspect.aspectRatio=(float)texture.width/texture.height;
        title.text=current.title;status.text="";
        credits.text=current.author+" • "+current.license+"\nWikimedia Commons • resized for display"+(string.IsNullOrWhiteSpace(current.credit)?"":"\n"+current.credit);
        lastTopic=topic;State="Ready";ImagesShown++;search=null;
    }
    private void Unavailable(string message){State="Unavailable";status.text=message;credits.text="No image is being shown for this answer. Try a more specific topic.";search=null;}
    private void CancelSearch(){revision++;if(activeRequest!=null){activeRequest.Abort();activeRequest=null;}if(search!=null){StopCoroutine(search);search=null;}}
    private void ClearImage(){if(picture!=null){picture.texture=null;picture.enabled=false;}if(texture!=null){Destroy(texture);texture=null;}}
    public void SetExpanded(bool value)
    {
        EnsureUi();expanded=value;expandLabel.text=value?"−":"+";
        Place(panelRect,value?new Vector2(.18f,.13f):new Vector2(dockRight ? .655f : .025f,.36f),value?new Vector2(.82f,.94f):new Vector2(dockRight ? .98f : .35f,.85f));
    }
    private void DockBesideSpeaker()
    {
        bool previous=dockRight;
        var subject=GetComponent<GeminiLiveConversation>()?.ActiveSpeakerTransform;
        var camera=Camera.main;
        if(subject!=null&&camera!=null)
        {
            Vector3 head=subject.position+Vector3.up*1.5f;
            var animator=subject.GetComponentInChildren<Animator>();
            if(animator!=null&&animator.isHuman){var bone=animator.GetBoneTransform(HumanBodyBones.Head);if(bone!=null)head=bone.position;}
            Vector3 viewport=camera.WorldToViewportPoint(head);
            if(viewport.z>0)
            {
                if(viewport.x<.42f)dockRight=true;
                else if(viewport.x>.58f)dockRight=false;
            }
        }
        if(previous!=dockRight)SetExpanded(expanded);
    }
    private void LateUpdate()
    {
        if(panel==null||!panel.activeSelf||expanded||Time.unscaledTime<nextDockCheck)return;
        nextDockCheck=Time.unscaledTime+.5f;DockBesideSpeaker();
    }
    private void OpenSource(bool license)
    {
        string url=license?current?.license_url:current?.source_url;
        if(Uri.TryCreate(url,UriKind.Absolute,out Uri uri)&&uri.Scheme=="https"&&
           (uri.Host=="commons.wikimedia.org"||uri.Host=="creativecommons.org"))Application.OpenURL(url);
    }
    private static GameObject Box(Transform root,string name,Vector2 min,Vector2 max,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(root,false);
        Place((RectTransform)go.transform,min,max);go.GetComponent<UnityEngine.UI.Image>().color=color;return go;
    }
    private static TMP_Text Label(Transform root,string text,Vector2 min,Vector2 max,float size,Color color)
    {
        var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(root,false);
        var label=go.GetComponent<TMP_Text>();Place(label.rectTransform,min,max);label.font=TMP_Settings.defaultFontAsset;
        label.text=text;label.fontSize=size;label.color=color;label.raycastTarget=false;label.richText=false;return label;
    }
    private static UnityEngine.UI.Button Button(Transform root,string text,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
    {
        var go=Box(root,text,min,max,new Color(.09f,.25f,.28f));var button=go.AddComponent<UnityEngine.UI.Button>();button.onClick.AddListener(action);
        var label=Label(go.transform,text,new Vector2(.04f,.01f),new Vector2(.96f,.99f),14,Color.white);label.alignment=TextAlignmentOptions.Center;return button;
    }
    private static void Place(RectTransform rect,Vector2 min,Vector2 max){rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    private void OnDisable(){CancelSearch();ClearImage();}
    private void OnDestroy(){CancelSearch();ClearImage();}
}

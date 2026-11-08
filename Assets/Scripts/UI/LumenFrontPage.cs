using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Scene-owned presentation over the existing courtyard, host and validated travel service.</summary>
public sealed class LumenFrontPage : MonoBehaviour
{
    private static LumenFrontPage instance;
    public static bool IsOpen => instance != null && instance.menu != null && !instance.exploring;
    private static readonly Color Ink = new Color(.025f, .047f, .065f, .98f);
    private static readonly Color Paper = new Color(.95f, .94f, .88f);
    private readonly string[] destinations = { "cafe", "classroom", "ancient_rome", "museum" };
    private readonly string[] titles = { "CAFÉ", "CLASSROOM", "ANCIENT ROME", "MUSEUM" };
    private readonly string[] roles = { "SOCIAL COACH", "VIRTUAL TUTOR", "HISTORICAL GUIDE", "GALLERY GUIDE" };
    private readonly string[] descriptions = {
        "A welcoming space for relaxed conversation, social practice and everyday confidence.",
        "Explore science and academic ideas with a virtual tutor, interactive objects and clear explanations.",
        "Walk through Ancient Rome with a virtual guide and discover the stories behind its landmarks.",
        "Explore art, archaeology and natural history with a guide." };
    private readonly Color[] accents = { new Color(.45f,.78f,.60f), new Color(.46f,.72f,.90f), new Color(.95f,.66f,.39f), new Color(.78f,.66f,.95f) };
    private readonly Texture2D[] previews = new Texture2D[4];
    private readonly List<Button> markers = new List<Button>();
    private Canvas canvas, legacy;
    private CanvasGroup legacyGroup;
    private GameObject menu, map, chooser, about, settings, reopen, presentationLights;
    private TMP_Text destinationTitle, destinationDescription, previewCaption;
    private RawImage preview;
    private Image previewFrame;
    private Sprite rounded;
    private int selected;
    private Camera cameraView;
    private Transform guide;
    private CafeWalkController movement;
    private Behaviour follower;
    private bool movementWasEnabled, followerWasEnabled, exploring, hasExplored, entering;
    private bool legacyWasEnabled, legacyWasInteractable, legacyWasBlocking;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private Quaternion savedGuideRotation;
    private float savedFov;
    private Transform drawerParent;
    private WorldVisitControls controls;
    private Coroutine cameraTransition, selectionTransition;

    public static void Install()
    {
        if (FindAnyObjectByType<LumenFrontPage>() == null)
            new GameObject("Lumen cinematic front page").AddComponent<LumenFrontPage>();
    }

    private IEnumerator Start()
    {
        instance = this;
        // Existing installers and controller Start methods own initial placement.
        yield return null;
        yield return null;
        legacy = GameObject.Find("LumenWalkableHubHUD")?.GetComponent<Canvas>();
        if (legacy != null)
        {
            // Unity's missing-component objects use its overloaded null comparison.
            // The C# ?? operator can retain a missing native component in the Editor.
            legacyGroup = legacy.GetComponent<CanvasGroup>();
            if (legacyGroup == null) legacyGroup = legacy.gameObject.AddComponent<CanvasGroup>();
            controls = legacy.GetComponent<WorldVisitControls>();
        }
        cameraView = Camera.main;
        guide = (GameObject.Find("HomeAgent") ?? GameObject.Find("Assistant"))?.transform;
        movement = cameraView != null ? cameraView.GetComponentInParent<CafeWalkController>() : null;
        follower = guide != null ? guide.GetComponent<WalkableGuideFollower>() : null;
        for (int i = 0; i < previews.Length; i++)
            previews[i] = Resources.Load<Texture2D>("DestinationPreviews/" + destinations[i]);
        Build();
        exploring = true;
        ShowMenu();
    }

    private void Build()
    {
        rounded = MakeRoundedSprite();
        var root = new GameObject("Lumen front page canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        menu = Panel(root.transform, "Main menu", new Vector2(.025f,.09f), new Vector2(.455f,.91f), new Color(.025f,.052f,.075f,.96f));
        Label(menu.transform, "Lumen", new Vector2(.08f,.85f), new Vector2(.94f,.94f), 32);
        Label(menu.transform, "GUIDED LEARNING  /  FOUR INTERACTIVE WORLDS", new Vector2(.08f,.76f), new Vector2(.94f,.84f), 13).color = accents[0];
        Label(menu.transform, "Explore with a guide", new Vector2(.08f,.63f), new Vector2(.94f,.76f), 34);
        Label(menu.transform, "Ask a question, look closely at an exhibit, or meet someone in one of Lumen’s four destinations.",
            new Vector2(.08f,.46f), new Vector2(.91f,.63f), 17);
        var enter = Button(menu.transform, "START EXPERIENCE", new Vector2(.08f,.34f), new Vector2(.92f,.44f), StartExperience);
        enter.GetComponentInChildren<TMP_Text>().text = "Open the courtyard";
        enter.GetComponent<UnityEngine.UI.Image>().color = new Color(.16f,.40f,.37f);
        Button(menu.transform, "CHOOSE ENVIRONMENT", new Vector2(.08f,.22f), new Vector2(.92f,.32f), () => ShowMap(false))
            .GetComponentInChildren<TMP_Text>().text = "Explore destinations";
        Button(menu.transform, "WORLD MAP", new Vector2(.08f,.09f), new Vector2(.37f,.18f), () => ShowMap(true)).GetComponentInChildren<TMP_Text>().text = "Atlas";
        Button(menu.transform, "SETTINGS", new Vector2(.39f,.09f), new Vector2(.66f,.18f), OpenSettings).GetComponentInChildren<TMP_Text>().text = "Settings";
        Button(menu.transform, "ABOUT", new Vector2(.68f,.09f), new Vector2(.92f,.18f), ShowAbout).GetComponentInChildren<TMP_Text>().text = "About";

        map = Panel(root.transform, "World atlas", new Vector2(.025f,.04f), new Vector2(.975f,.96f), Ink);
        Label(map.transform, "THE WORLD AT YOUR OWN PACE", new Vector2(.035f,.87f), new Vector2(.80f,.97f), 28);
        Label(map.transform, "Choose a place. Meet your companion. Follow your curiosity.", new Vector2(.035f,.80f), new Vector2(.85f,.88f), 16);
        Button(map.transform, "BACK", new Vector2(.86f,.90f), new Vector2(.965f,.97f), ShowMenu);
        var sea = Panel(map.transform, "Atlas ocean", new Vector2(.025f,.075f), new Vector2(.65f,.78f), new Color(.065f,.16f,.20f));
        var terrainObject = new GameObject("Illustrated regions", typeof(RectTransform), typeof(LumenAtlasTerrain));
        terrainObject.transform.SetParent(sea.transform, false);
        Place((RectTransform)terrainObject.transform, Vector2.zero, Vector2.one);
        terrainObject.GetComponent<LumenAtlasTerrain>().raycastTarget = false;
        Vector2[] positions = { new Vector2(.23f,.28f), new Vector2(.48f,.73f), new Vector2(.79f,.32f), new Vector2(.24f,.64f) };
        for (int i = 0; i < destinations.Length; i++)
        {
            int destination = i;
            Vector2 p = positions[i];
            markers.Add(Button(sea.transform, titles[i], p-new Vector2(.16f,.045f), p+new Vector2(.16f,.045f), () => SelectDestination(destination)));
        }
        Label(sea.transform, "N\n│", new Vector2(.91f,.78f), new Vector2(.98f,.95f), 18).alignment = TextAlignmentOptions.Center;
        Label(sea.transform, "FOUR PLACES TO EXPLORE", new Vector2(.035f,.018f), new Vector2(.92f,.075f), 12);
        destinationTitle = Label(map.transform, "", new Vector2(.69f,.68f), new Vector2(.96f,.77f), 25);
        var image = new GameObject("Environment preview", typeof(RectTransform), typeof(RawImage));
        image.transform.SetParent(map.transform, false);
        Place((RectTransform)image.transform, new Vector2(.69f,.43f), new Vector2(.965f,.66f));
        preview = image.GetComponent<RawImage>(); preview.raycastTarget = false;
        previewFrame = Panel(image.transform, "Preview tint", Vector2.zero, Vector2.one, Color.clear).GetComponent<Image>();
        previewFrame.raycastTarget = false;
        previewCaption = Label(map.transform, "", new Vector2(.69f,.36f), new Vector2(.965f,.42f), 13);
        destinationDescription = Label(map.transform, "", new Vector2(.69f,.20f), new Vector2(.965f,.35f), 17);
        Button(map.transform, "ENTER ENVIRONMENT", new Vector2(.69f,.085f), new Vector2(.965f,.175f), EnterSelected);
        chooser = Panel(root.transform, "Choose environment", new Vector2(.04f,.10f), new Vector2(.96f,.90f), Ink);
        Label(chooser.transform, "WHERE SHALL WE BEGIN?", new Vector2(.04f,.80f), new Vector2(.82f,.96f), 30);
        Button(chooser.transform, "BACK", new Vector2(.86f,.85f), new Vector2(.96f,.95f), ShowMenu);
        for (int i = 0; i < destinations.Length; i++)
        {
            int destination = i;
            float left = .035f + (i % 2) * .47f;
            float bottom = i < 2 ? .44f : .065f;
            var card = Panel(chooser.transform, titles[i], new Vector2(left,bottom), new Vector2(left+.45f,bottom+.34f), new Color(.06f,.11f,.14f));
            var photo = new GameObject("Scene preview", typeof(RectTransform), typeof(RawImage));
            photo.transform.SetParent(card.transform, false);
            Place((RectTransform)photo.transform, new Vector2(.035f,.12f), new Vector2(.39f,.88f));
            photo.GetComponent<RawImage>().texture = previews[i];
            photo.GetComponent<RawImage>().color = previews[i] != null ? Color.white : accents[i] * .5f;
            photo.GetComponent<RawImage>().raycastTarget = false;
            if(previews[i]==null)
                Label(photo.transform,"SCENE PREVIEW PENDING",new Vector2(.07f,.1f),new Vector2(.93f,.9f),14).alignment=TextAlignmentOptions.Center;
            Label(card.transform, roles[i], new Vector2(.43f,.79f), new Vector2(.94f,.92f), 13).color = accents[i];
            Label(card.transform, titles[i], new Vector2(.43f,.61f), new Vector2(.94f,.79f), 23);
            Label(card.transform, descriptions[i], new Vector2(.43f,.29f), new Vector2(.94f,.61f), 14);
            Button(card.transform, "EXPLORE DESTINATION", new Vector2(.43f,.06f), new Vector2(.95f,.27f), () => { ShowMap(true); SelectDestination(destination); });
        }
        about = Panel(root.transform, "About Embodied AI", new Vector2(.13f,.12f), new Vector2(.87f,.88f), Ink);
        Label(about.transform, "EMBODIED AI", new Vector2(.07f,.80f), new Vector2(.93f,.94f), 32);
        Label(about.transform, "A Multi-Scenario 3D Virtual Assistant with Intelligent Interaction and Environmental Navigation.", new Vector2(.07f,.63f), new Vector2(.93f,.79f), 22);
        ScrollableText(about.transform, "An intelligent companion with a presence in the world around you. Walk, talk, ask questions and discover ideas through natural interaction.\n\nACADEMIC LEARNING  ·  A virtual tutor brings classroom concepts to life.\n\nHISTORICAL EXPLORATION  ·  A virtual guide connects places with their stories.\n\nSOCIAL PRACTICE  ·  A virtual coach supports everyday conversation in the café.", new Vector2(.07f,.23f), new Vector2(.93f,.62f), 18);
        Button(about.transform, "BACK", new Vector2(.07f,.07f), new Vector2(.30f,.17f), ShowMenu);
        settings = Panel(root.transform, "Settings backdrop", Vector2.zero, Vector2.one, Ink, false);
        Label(settings.transform, "MAKE YOURSELF\nCOMFORTABLE", new Vector2(.64f,.63f), new Vector2(.95f,.85f), 29);
        Label(settings.transform, "Display, sound and accessibility preferences are shared across all your visits.", new Vector2(.64f,.43f), new Vector2(.94f,.61f), 18);
        Button(settings.transform, "BACK", new Vector2(.64f,.14f), new Vector2(.94f,.22f), ShowMenu);
        reopen = Button(root.transform, "MAIN MENU", new Vector2(.83f,.94f), new Vector2(.975f,.99f), ShowMenu).gameObject;
        reopen.name = "Open front page";
        presentationLights = new GameObject("Front page portrait lighting");
        presentationLights.transform.SetParent(transform, false);
        AddPortraitLight("Warm key", new Vector3(-2f,2.6f,-2f), new Color(1,.86f,.70f), 2.6f);
        AddPortraitLight("Cool rim", new Vector3(1.6f,2.2f,1f), new Color(.55f,.77f,1), 2f);
    }

    private void AddPortraitLight(string label, Vector3 offset, Color color, float intensity)
    {
        if (guide == null) return;
        var go = new GameObject(label); go.transform.SetParent(presentationLights.transform, false);
        go.transform.localPosition = offset;
        var light = go.AddComponent<Light>(); light.type = LightType.Point;
        light.color = color; light.intensity = intensity; light.range = 6f;
        light.shadows = LightShadows.None; // Reuse the garden's directional soft shadow.
    }

    public void ShowMenu()
    {
        if (exploring)
        {
            CaptureVisit();
            exploring = false;
            if (movement != null) movement.enabled = false;
            if (follower != null) follower.enabled = false;
            SetLegacy(false);
            presentationLights.SetActive(true);
            if (guide != null)
            {
                presentationLights.transform.position=guide.position;
                guide.rotation = Quaternion.LookRotation(new Vector3(-.2f, 0f, -1f));
                var animator = guide.GetComponent<AgentAnimator>();
                animator?.ApplyEmotion("friendly");
                if (!LumenAccessibility.ReducedMotion) animator?.PlayAction(hasExplored ? "nod" : "gesture");
                if (cameraView != null)
                {
                    Vector3 eye = guide.position + new Vector3(-1.36f,1.42f,-3.70f);
                    Quaternion angle = Quaternion.LookRotation(guide.position + new Vector3(-1.24f,1.07f,0) - eye);
                    MoveCamera(eye, angle, 47f, false);
                }
            }
        }
        ShowOnly(menu);
        LumenAccessibility.MenuOpen = true;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    private void CaptureVisit()
    {
        if (cameraView != null) { savedCameraPosition=cameraView.transform.position; savedCameraRotation=cameraView.transform.rotation; savedFov=cameraView.fieldOfView; }
        movementWasEnabled = movement != null && movement.enabled;
        followerWasEnabled = follower != null && follower.enabled;
        if(guide!=null)savedGuideRotation=guide.rotation;
        if (legacy != null) { legacyWasEnabled=legacy.enabled; legacyWasInteractable=legacyGroup.interactable; legacyWasBlocking=legacyGroup.blocksRaycasts; }
    }

    public void StartExperience()
    {
        if (exploring || entering) return;
        entering = true;
        ShowOnly(null);
        presentationLights.SetActive(false);
        MoveCamera(savedCameraPosition, savedCameraRotation, savedFov, true);
    }

    private void FinishEntry()
    {
        exploring = hasExplored = true;
        entering = false;
        SetLegacy(true);
        reopen.SetActive(true);
        LumenAccessibility.MenuOpen = false;
        if (movement != null) movement.enabled = movementWasEnabled;
        if (follower != null) follower.enabled = followerWasEnabled;
        if (guide != null) guide.rotation = savedGuideRotation;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void MoveCamera(Vector3 position, Quaternion rotation, float fov, bool enter)
    {
        if (cameraTransition != null) StopCoroutine(cameraTransition);
        if (cameraView == null) { if (enter) FinishEntry(); return; }
        cameraTransition = StartCoroutine(BlendCamera(position, rotation, fov, enter));
    }

    private IEnumerator BlendCamera(Vector3 position, Quaternion rotation, float fov, bool enter)
    {
        Vector3 start=cameraView.transform.position; Quaternion angle=cameraView.transform.rotation; float initialFov=cameraView.fieldOfView;
        float duration=LumenAccessibility.ReducedMotion?0f:.55f;
        for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
        {
            float t=Mathf.SmoothStep(0,1,elapsed/duration);
            cameraView.transform.SetPositionAndRotation(Vector3.Lerp(start,position,t),Quaternion.Slerp(angle,rotation,t));
            cameraView.fieldOfView=Mathf.Lerp(initialFov,fov,t); yield return null;
        }
        cameraView.transform.SetPositionAndRotation(position,rotation); cameraView.fieldOfView=fov;
        cameraTransition=null; if(enter)FinishEntry();
    }

    public void ShowMap(bool atlas) { ShowOnly(atlas ? map : chooser); if(atlas)SelectDestination(selected); }
    public void SelectDestination(int index)
    {
        if (index < 0 || index >= destinations.Length) return;
        selected=index; destinationTitle.text=titles[index]; destinationTitle.color=accents[index];
        destinationDescription.text=descriptions[index]; preview.texture=previews[index];
        preview.color=previews[index]!=null?Color.white:accents[index]*.45f;
        previewCaption.text=roles[index]+(previews[index]!=null?"  /  SCENE PREVIEW":"  /  PREVIEW UNAVAILABLE");
        // Crop to fill the preview window while preserving the image proportions.
        Canvas.ForceUpdateCanvases();
        if(previews[index]!=null)
        {
            float source=(float)previews[index].width/previews[index].height;
            float target=preview.rectTransform.rect.width/Mathf.Max(1,preview.rectTransform.rect.height);
            preview.uvRect=source>target?new Rect((1-target/source)*.5f,0,target/source,1):new Rect(0,(1-source/target)*.5f,1,source/target);
        }
        for(int i=0;i<markers.Count;i++)
        {
            markers[i].GetComponent<Image>().color=i==index?Color.Lerp(accents[i],Color.black,.45f):new Color(.055f,.11f,.14f);
            markers[i].GetComponentInChildren<TMP_Text>().text=(i==index?"●  ":"")+titles[i];
        }
        if(selectionTransition!=null)StopCoroutine(selectionTransition);
        selectionTransition=StartCoroutine(SelectionFeedback());
    }
    private IEnumerator SelectionFeedback()
    {
        for(float t=0;t<.3f&&!LumenAccessibility.ReducedMotion;t+=Time.unscaledDeltaTime)
        { previewFrame.color=new Color(accents[selected].r,accents[selected].g,accents[selected].b,.22f*(1-t/.3f)); yield return null; }
        previewFrame.color=Color.clear; selectionTransition=null;
    }
    private void EnterSelected()
    {
        var result=ScenarioTransitionService.RequestTravel(destinations[selected]);
        if(!result.accepted)destinationDescription.text=result.reason;
    }
    private void ShowAbout() => ShowOnly(about);
    private void OpenSettings()
    {
        ShowOnly(settings);
        if(controls==null||controls.SettingsPanel==null)return;
        var drawer=controls.SettingsPanel;
        drawerParent=drawer.transform.parent;
        drawer.transform.SetParent(canvas.transform,false);
        controls.ShowSettings();
        Focus(drawer);
    }
    private void CloseSettings()
    {
        if(drawerParent==null||controls==null)return;
        controls.SettingsPanel.SetActive(false);
        controls.SettingsPanel.transform.SetParent(drawerParent,false); drawerParent=null;
    }
    private void ShowOnly(GameObject page)
    {
        CloseSettings();
        foreach(var item in new[]{menu,map,chooser,about,settings}) item.SetActive(item==page);
        reopen.SetActive(false);
        LumenAccessibility.MenuOpen=true;
        if(page!=null)Focus(page);
    }
    private static void Focus(GameObject page)
    {
        var first=page.GetComponentInChildren<Button>();
        EventSystem.current?.SetSelectedGameObject(first!=null?first.gameObject:null);
    }
    public static bool HandleEscape()
    {
        if(!IsOpen)return false;
        if(instance.entering)return true;
        if(instance.menu.activeSelf) { if(instance.hasExplored)instance.StartExperience(); }
        else instance.ShowMenu();
        return true;
    }
    private void SetLegacy(bool active)
    {
        if(legacy==null)return;
        legacy.enabled=active&&legacyWasEnabled;
        legacyGroup.interactable=active&&legacyWasInteractable;
        legacyGroup.blocksRaycasts=active&&legacyWasBlocking;
    }
    private void OnDestroy()
    {
        if(instance!=this)return;
        instance=null; LumenAccessibility.MenuOpen=false;
    }
    private GameObject Panel(Transform parent,string name,Vector2 min,Vector2 max,Color color,bool round=true)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
        Place((RectTransform)go.transform,min,max); var image=go.GetComponent<Image>();image.color=color;
        if(round){image.sprite=rounded;image.type=Image.Type.Sliced;}
        return go;
    }
    private static TMP_Text Label(Transform parent,string value,Vector2 min,Vector2 max,float size)
    {
        var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var label=go.GetComponent<TMP_Text>();Place(label.rectTransform,min,max);
        label.text=value;label.font=TMP_Settings.defaultFontAsset;label.fontSize=size;label.color=Paper;
        label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.Normal;label.overflowMode=TextOverflowModes.Ellipsis;
        label.alignment=TextAlignmentOptions.MidlineLeft;
        return label;
    }
    private Button Button(Transform parent,string value,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
    {
        var go=Panel(parent,value,min,max,new Color(.08f,.14f,.18f,.98f));
        var button=go.AddComponent<Button>();button.targetGraphic=go.GetComponent<Image>();button.onClick.AddListener(action);
        var colors=button.colors;colors.highlightedColor=new Color(.60f,.87f,.80f);colors.selectedColor=colors.highlightedColor;
        colors.pressedColor=new Color(.4f,.65f,.60f);colors.fadeDuration=.14f;button.colors=colors;
        Label(go.transform,value,new Vector2(.065f,.08f),new Vector2(.95f,.92f),16);
        return button;
    }
    private TMP_Text ScrollableText(Transform parent,string value,Vector2 min,Vector2 max,float size)
    {
        var viewport=Panel(parent,"Readable project introduction",min,max,new Color(0,0,0,.01f));
        viewport.AddComponent<RectMask2D>();
        var scroll=viewport.AddComponent<ScrollRect>();
        scroll.viewport=(RectTransform)viewport.transform;scroll.horizontal=false;
        scroll.movementType=ScrollRect.MovementType.Clamped;
        var label=Label(viewport.transform,value,new Vector2(0,1),Vector2.one,size);
        label.rectTransform.pivot=new Vector2(.5f,1);
        label.alignment=TextAlignmentOptions.TopLeft;label.overflowMode=TextOverflowModes.Overflow;
        label.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.content=label.rectTransform;
        return label;
    }
    private static void Place(RectTransform rect,Vector2 min,Vector2 max)
    {rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    private Sprite MakeRoundedSprite()
    {
        var texture=LumenRuntimeResources.Own(new Texture2D(32,32,TextureFormat.RGBA32,false),gameObject);
        texture.name="Front page rounded corners";texture.wrapMode=TextureWrapMode.Clamp;
        for(int y=0;y<32;y++)for(int x=0;x<32;x++)
        {
            float dx=Mathf.Max(8-x,x-23),dy=Mathf.Max(8-y,y-23);
            float distance=new Vector2(Mathf.Max(0,dx),Mathf.Max(0,dy)).magnitude;
            texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(8-distance)));
        }
        texture.Apply();
        return LumenRuntimeResources.Own(Sprite.Create(texture,new Rect(0,0,32,32),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(9,9,9,9)),gameObject);
    }
}

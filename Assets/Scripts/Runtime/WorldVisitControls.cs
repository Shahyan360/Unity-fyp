using PlayerPrefs = LumenPreferences;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene-local visit settings and an exhibit notebook, shared by all worlds.</summary>
public sealed class WorldVisitControls : MonoBehaviour
{
    private GameObject drawer;
    private TMP_Text notes;
    private static readonly Color TextColor = new Color(0.90f, 0.94f, 0.95f);
    public static void Install(Canvas canvas, string scene)
    {
        var controls = canvas.gameObject.AddComponent<WorldVisitControls>();
        controls.Build(canvas.transform, scene);
    }
    private void Build(Transform root, string scene)
    {
        string title = scene == "HistoricalPlace" ? "ANCIENT ROME" : scene == "Cafestore" ? "THE CAFÉ"
            : scene == "Museum" ? "MUSEUM OF DISCOVERY" : "SCIENCE STUDIO";
        var header = Panel(root, "VisitHeader", new Vector2(.025f, .91f), new Vector2(.55f, .985f));
        Label(header.transform, title, new Vector2(.035f,.42f), new Vector2(.80f,.94f), 22);
        Label(header.transform, "Explore at your own pace. Ask a question. Follow an idea.", new Vector2(.035f,.05f), new Vector2(.80f,.45f), 14);
        var open = Button(header.transform, "VISIT", new Vector2(.82f,.15f), new Vector2(.97f,.85f));
        drawer = Panel(root, "VisitSettings", new Vector2(.025f,.43f), new Vector2(.39f,.89f));
        Label(drawer.transform, "YOUR VISIT", new Vector2(.06f,.84f), new Vector2(.94f,.96f), 25);
        Label(drawer.transform, "WASD: walk   /   Right-click: look\nEsc: free cursor   /   E: interact   /   Hold V: speak", new Vector2(.06f,.63f), new Vector2(.94f,.84f), 17);
        var quality = Button(drawer.transform, "Graphics: " + WorldPresentationQuality.SelectedQuality, new Vector2(.06f,.49f), new Vector2(.94f,.61f));
        quality.onClick.AddListener(() => quality.GetComponentInChildren<TMP_Text>().text = "Graphics: " + WorldPresentationQuality.CycleQuality());
        var voice = Button(drawer.transform, VoiceLabel(), new Vector2(.06f,.34f), new Vector2(.94f,.46f));
        voice.onClick.AddListener(() =>
        {
            bool muted = PlayerPrefs.GetInt("Lumen.MuteVoice", 0) == 0;
            PlayerPrefs.SetInt("Lumen.MuteVoice", muted ? 1 : 0);
            PlayerPrefs.Save();
            foreach (SpeechOutputController output in FindObjectsByType<SpeechOutputController>())
                output.SetVoiceMuted(muted);
            GeminiLiveConversation.Instance?.SetVoiceMuted(muted);
            voice.GetComponentInChildren<TMP_Text>().text = VoiceLabel();
        });
        Label(drawer.transform, "DISCOVERY NOTEBOOK", new Vector2(.06f,.23f), new Vector2(.94f,.33f), 15);
        notes = Label(drawer.transform, ExplorationNotebook.Summary(), new Vector2(.06f,.04f), new Vector2(.94f,.23f), 15);
        notes.overflowMode = TextOverflowModes.Ellipsis;
        open.onClick.AddListener(() => { drawer.SetActive(!drawer.activeSelf); notes.text = ExplorationNotebook.Summary(); });
        drawer.SetActive(false);
    }
    private static string VoiceLabel() => PlayerPrefs.GetInt("Lumen.MuteVoice", 0) == 0 ? "Spoken replies: on" : "Spoken replies: off";
    private static GameObject Panel(Transform root, string name, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(root, false);
        Place((RectTransform)go.transform, min, max);
        var image = go.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(.035f,.06f,.085f,.97f);
        image.raycastTarget = true;
        return go;
    }
    private static TMP_Text Label(Transform root, string value, Vector2 min, Vector2 max, float size)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(root, false);
        var label = go.GetComponent<TMP_Text>();
        Place(label.rectTransform,min,max);
        label.text = value; label.fontSize = size; label.color = TextColor;
        label.font = TMP_Settings.defaultFontAsset; label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.MidlineLeft; label.richText = false;
        return label;
    }
    private static UnityEngine.UI.Button Button(Transform root, string value, Vector2 min, Vector2 max)
    {
        var go = Panel(root, "VisitButton", min, max);
        go.GetComponent<UnityEngine.UI.Image>().color = new Color(.12f,.28f,.31f);
        var button = go.AddComponent<UnityEngine.UI.Button>();
        var text = Label(go.transform,value,new Vector2(.06f,.05f),new Vector2(.94f,.95f),17);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }
    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero;
    }
}

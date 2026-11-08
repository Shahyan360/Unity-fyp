using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Self-contained café ordering practice: browse, build a basket, choose service, place, and collect.</summary>
public sealed class CafeOrderExperience : MonoBehaviour
{
    private struct MenuItem
    {
        public string name;
        public string details;
        public int price;
        public MenuItem(string itemName, string itemDetails, int itemPrice) { name = itemName; details = itemDetails; price = itemPrice; }
    }

    private static readonly MenuItem[] Menu =
    {
        new MenuItem("Espresso", "Double shot · rich and bold", 280),
        new MenuItem("Cappuccino", "Espresso, steamed milk, soft foam", 420),
        new MenuItem("Karak chai", "Spiced black tea · warm and creamy", 250),
        new MenuItem("Iced latte", "Chilled espresso and milk", 480),
        new MenuItem("Butter croissant", "Flaky pastry · baked this morning", 360),
        new MenuItem("Chocolate brownie", "Fudgy cocoa · café favourite", 330)
    };

    private readonly int[] quantities = new int[6];
    private readonly TMP_Text[] itemQuantityLabels = new TMP_Text[6];
    private TMP_Text totalLabel;
    private TMP_Text statusLabel;
    private TMP_Text serviceLabel;
    private Button placeOrderButton;
    private GameObject modal;
    private ChatUIManager chat;
    private CursorLockMode cursorBeforeModal;
    private bool wasCursorVisible;
    private bool orderInProgress;
    private bool orderReady;
    private string serviceMethod = "Pickup";
    private int currentOrderNumber;
    private AudioSource orderSounds;
    private AudioClip orderPlacedCue;
    private AudioClip orderReadyCue;
    private Coroutine preparation;
    public string OrderState=>orderReady?"Ready for simulated collection":orderInProgress?"Preparing simulated order":"No pending order";
    public void CancelOrder()
    {
        if(preparation!=null)StopCoroutine(preparation);preparation=null;orderReady=orderInProgress=false;
        for(int i=0;i<quantities.Length;i++){quantities[i]=0;if(itemQuantityLabels[i]!=null)itemQuantityLabels[i].text="0";}
        if(statusLabel!=null)statusLabel.text="Cancelled. Start again when ready.";RefreshTotal();
    }

    public bool IsOpen => modal != null && modal.activeSelf;

    public void Initialize(Canvas canvas, ChatUIManager chatManager)
    {
        chat = chatManager;
        orderSounds = canvas.GetComponent<AudioSource>();
        if (orderSounds == null) orderSounds = canvas.gameObject.AddComponent<AudioSource>();
        orderSounds.playOnAwake = false;
        orderSounds.spatialBlend = 0f;
        orderSounds.volume = 0.34f*LumenPreferences.GetInt("Lumen.UIVolume",80)/100f;
        LumenAudioMix.Route(orderSounds,"UI");
        orderPlacedCue = CreateTone("CafeOrderPlaced", 660f, 0.12f);
        orderReadyCue = CreateChord("CafeOrderReady", 784f, 988f, 0.42f);
        BuildMenu(canvas.transform);
    }

    public void Show()
    {
        if (modal == null) return;
        cursorBeforeModal = Cursor.lockState;
        wasCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        modal.SetActive(true);
        LumenAccessibility.MenuOpen=true;
        modal.transform.SetAsLastSibling();
        RefreshTotal();
    }

    public void Hide()
    {
        if (modal == null) return;
        modal.SetActive(false);
        LumenAccessibility.MenuOpen=false;
        if (!wasCursorVisible && cursorBeforeModal == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (modal != null && modal.activeSelf && IsEscapePressed()) Hide();
    }

    private void BuildMenu(Transform parent)
    {
        modal = new GameObject("CafeOrderOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        modal.transform.SetParent(parent, false);
        Image veil = modal.GetComponent<Image>();
        veil.color = new Color(0.012f, 0.022f, 0.037f, 0.86f);
        Rect(veil.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CanvasGroup group = modal.GetComponent<CanvasGroup>();
        group.interactable = true;
        group.blocksRaycasts = true;

        Image panel = MakePanel(modal.transform, "CafeMenuPanel", new Color(0.075f, 0.09f, 0.115f, 1f),
            new Vector2(0.055f, 0.035f), new Vector2(0.945f, 0.965f));
        AddText(panel.transform, "MenuEyebrow", "CAFE / PRACTICE ORDER", 12, new Color(0.95f, 0.67f, 0.40f), FontStyles.Bold,
            new Vector2(0.045f, 0.935f), new Vector2(0.55f, 0.995f), TextAlignmentOptions.Left);
        AddText(panel.transform, "MenuTitle", "A little something?", 31, new Color(0.97f, 0.94f, 0.88f), FontStyles.Bold,
            new Vector2(0.045f, 0.84f), new Vector2(0.94f, 0.935f), TextAlignmentOptions.Left);
        AddText(panel.transform, "MenuDescription", "Simulation only. No real payment.", 14, new Color(0.73f, 0.77f, 0.79f), FontStyles.Normal,
            new Vector2(0.045f, 0.79f), new Vector2(0.94f, 0.84f), TextAlignmentOptions.Left);

        for (int i = 0; i < Menu.Length; i++) BuildItemRow(panel.transform, i);

        Button pickup = MakeButton(panel.transform, "PickupChoice", "PICK UP", new Color(0.22f, 0.63f, 0.58f),
            new Vector2(0.045f, 0.11f), new Vector2(0.47f, 0.18f), true);
        Button table = MakeButton(panel.transform, "TableChoice", "TABLE SERVICE", new Color(0.18f, 0.23f, 0.28f),
            new Vector2(0.49f, 0.11f), new Vector2(0.95f, 0.18f), true);
        pickup.onClick.AddListener(() => { serviceMethod = "Pickup"; RefreshTotal(); });
        table.onClick.AddListener(() => { serviceMethod = "Table service"; RefreshTotal(); });
        serviceLabel = AddText(panel.transform, "ServiceLabel", "Pickup selected", 12, new Color(0.70f, 0.87f, 0.80f), FontStyles.Normal,
            new Vector2(0.045f, 0.062f), new Vector2(0.46f, 0.11f), TextAlignmentOptions.Left);
        totalLabel = AddText(panel.transform, "OrderTotal", "Total  ·  Rs 0", 17, new Color(0.97f, 0.94f, 0.88f), FontStyles.Bold,
            new Vector2(0.50f, 0.062f), new Vector2(0.95f, 0.11f), TextAlignmentOptions.Right);
        statusLabel = AddText(panel.transform, "OrderStatus", "", 13, new Color(0.61f, 0.87f, 0.74f), FontStyles.Normal,
            new Vector2(0.045f, 0.005f), new Vector2(0.55f, 0.062f), TextAlignmentOptions.Left);
        placeOrderButton = MakeButton(panel.transform, "PlaceOrder", "PLACE ORDER  ·  Rs 0", new Color(0.95f, 0.69f, 0.42f),
            new Vector2(0.57f, 0.005f), new Vector2(0.95f, 0.062f), true);
        placeOrderButton.onClick.AddListener(PlaceOrder);
        Button close = MakeButton(panel.transform, "CloseMenu", "CLOSE", new Color(0.18f, 0.23f, 0.28f),
            new Vector2(0.81f, 0.935f), new Vector2(0.95f, 0.995f), true);
        close.onClick.AddListener(Hide);
        var cancel=MakeButton(panel.transform,"CancelOrder","RESET",new Color(.18f,.23f,.28f),new Vector2(.63f,.935f),new Vector2(.79f,.995f),true);
        cancel.onClick.AddListener(CancelOrder);
        modal.SetActive(false);
    }

    private void BuildItemRow(Transform parent, int index)
    {
        float top = 0.78f - index * 0.098f;
        float bottom = top - 0.094f;
        Image row = MakePanel(parent, "MenuItem_" + index, new Color(0.11f, 0.13f, 0.16f, 1f),
            new Vector2(0.055f, bottom), new Vector2(0.945f, top));
        AddText(row.transform, "ItemName", Menu[index].name, 16, new Color(0.95f, 0.93f, 0.88f), FontStyles.Bold,
            new Vector2(0.035f, 0.50f), new Vector2(0.64f, 0.995f), TextAlignmentOptions.Left);
        AddText(row.transform, "ItemDetails", Menu[index].details, 12, new Color(0.66f, 0.71f, 0.75f), FontStyles.Normal,
            new Vector2(0.035f, 0.005f), new Vector2(0.67f, 0.50f), TextAlignmentOptions.Left);
        AddText(row.transform, "ItemPrice", "Rs " + Menu[index].price, 13, new Color(0.92f, 0.72f, 0.49f), FontStyles.Bold,
            new Vector2(0.66f, 0.28f), new Vector2(0.82f, 0.77f), TextAlignmentOptions.Right);
        Button minus = MakeButton(row.transform, "Minus", "−", new Color(0.20f, 0.25f, 0.29f), new Vector2(0.835f, 0.18f), new Vector2(0.89f, 0.82f), true);
        Button plus = MakeButton(row.transform, "Plus", "+", new Color(0.22f, 0.63f, 0.58f), new Vector2(0.925f, 0.18f), new Vector2(0.98f, 0.82f), true);
        itemQuantityLabels[index] = AddText(row.transform, "Quantity", "0", 13, new Color(0.95f, 0.93f, 0.88f), FontStyles.Bold,
            new Vector2(0.885f, 0.18f), new Vector2(0.93f, 0.82f), TextAlignmentOptions.Center);
        int item = index;
        minus.onClick.AddListener(() => ChangeQuantity(item, -1));
        plus.onClick.AddListener(() => ChangeQuantity(item, 1));
    }

    private void ChangeQuantity(int index, int change)
    {
        if (orderInProgress) return;
        quantities[index] = Mathf.Clamp(quantities[index] + change, 0, 20);
        itemQuantityLabels[index].text = quantities[index].ToString();
        RefreshTotal();
    }

    private void RefreshTotal()
    {
        int total = 0;
        for (int i = 0; i < Menu.Length; i++) total += quantities[i] * Menu[i].price;
        if (totalLabel != null) totalLabel.text = "Total  ·  Rs " + total;
        if (placeOrderButton != null)
        {
            TMP_Text buttonText = placeOrderButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null) buttonText.text = orderReady ? "COLLECT  #" + currentOrderNumber : orderInProgress ? "PREPARING  #" + currentOrderNumber : "ORDER  ·  Rs " + total;
            placeOrderButton.interactable = orderReady || (!orderInProgress && total > 0);
        }
        if (serviceLabel != null) serviceLabel.text = serviceMethod + " selected";
    }

    private void PlaceOrder()
    {
        if (orderReady)
        {
            statusLabel.text = "Order #" + currentOrderNumber + " collected. Thank you!";
            orderReady = false;
            orderInProgress = false;
            for (int i = 0; i < quantities.Length; i++) { quantities[i] = 0; itemQuantityLabels[i].text = "0"; }
            RefreshTotal();
            if (chat != null) chat.SubmitWorldPrompt("I have collected café order number " + currentOrderNumber + ". Thank the customer and continue the conversation.", FindBarista());
            return;
        }
        if (orderInProgress) return;

        currentOrderNumber = Random.Range(104, 999);
        orderInProgress = true;
        FindBarista()?.GetComponent<AmbientNpcRoutine>()?.ReactToOrder();
        string summary = BuildOrderSummary();
        statusLabel.text = "Order #" + currentOrderNumber + " / preparing";
        RefreshTotal();
        if (orderSounds != null && orderPlacedCue != null) orderSounds.PlayOneShot(orderPlacedCue);
        if (chat != null) chat.SubmitWorldPrompt("I just placed a simulated café order: " + summary + ". It is order number " + currentOrderNumber + " for " + serviceMethod.ToLowerInvariant() + ". Respond warmly as the café companion.", FindBarista());
        preparation=StartCoroutine(PrepareOrder());
    }

    private IEnumerator PrepareOrder()
    {
        yield return new WaitForSeconds(8f);
        preparation=null;
        orderInProgress = false;
        orderReady = true;
        FindBarista()?.GetComponent<AmbientNpcRoutine>()?.ReactToOrder(true);
        if (statusLabel != null) statusLabel.text = "Order #" + currentOrderNumber + (serviceMethod=="Pickup"?" / ready at counter":" / table service ready");
        if (orderSounds != null && orderReadyCue != null) orderSounds.PlayOneShot(orderReadyCue);
        FindBarista()?.GetComponent<AmbientNpcRoutine>()?.ReactToOrder(true);
        RefreshTotal();
    }

    private static ObjectMetadata FindBarista()
    {
        GameObject barista = GameObject.Find("Cafe_Chef");
        return barista != null ? barista.GetComponent<ObjectMetadata>() : null;
    }

    private static AudioClip CreateTone(string name, float frequency, float duration)
    {
        return CreateChord(name, frequency, 0f, duration);
    }

    private static AudioClip CreateChord(string name, float firstFrequency, float secondFrequency, float duration)
    {
        const int sampleRate = 22050;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Min(1f, i / (sampleRate * 0.012f)) * Mathf.Clamp01((samples - i) / (sampleRate * 0.08f));
            float tone = Mathf.Sin(2f * Mathf.PI * firstFrequency * t);
            if (secondFrequency > 0f) tone = (tone + Mathf.Sin(2f * Mathf.PI * secondFrequency * t)) * 0.5f;
            data[i] = tone * envelope * 0.22f;
        }
        clip.SetData(data, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (orderPlacedCue != null) Destroy(orderPlacedCue);
        if (orderReadyCue != null) Destroy(orderReadyCue);
    }

    private string BuildOrderSummary()
    {
        string result = "";
        for (int i = 0; i < Menu.Length; i++)
            if (quantities[i] > 0) result += (result.Length == 0 ? "" : ", ") + quantities[i] + " " + Menu[i].name;
        return result;
    }

    private static Image MakePanel(Transform parent, string name, Color color, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        Rect(image.rectTransform, min, max, Vector2.zero, Vector2.zero);
        return image;
    }

    private static TMP_Text AddText(Transform parent, string name, string content, float size, Color color, FontStyles style,
        Vector2 min, Vector2 max, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
        Rect(text.rectTransform, min, max, Vector2.zero, Vector2.zero);
        return text;
    }

    private static Button MakeButton(Transform parent, string name, string title, Color color, Vector2 min, Vector2 max, bool compact)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Rect(rect, min, max, Vector2.zero, Vector2.zero);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.13f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.14f);
        button.colors = colors;
        Color labelColor = color.grayscale < .42f ? new Color(.93f,.96f,.96f) : new Color(.04f,.06f,.08f);
        AddText(go.transform, "Label", title, compact ? 12 : 15, labelColor, FontStyles.Bold,
            new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), TextAlignmentOptions.Center);
        return button;
    }

    private static void Rect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static bool IsEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}

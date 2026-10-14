using TMPro;
using UnityEngine;

/// <summary>Cycles tiny classroom model previews beside the guide's open hand.</summary>
public sealed class GuideExhibitCatalog : MonoBehaviour
{
    private static readonly string[] PageTitles =
    {
        "ASTRONOMY · 9 models",
        "CHEMISTRY · 3 models",
        "BIOLOGY · 5 models",
        "EARTH & NATURE · 5 models",
        "PHYSICS & MATH · 3 models"
    };

    private Transform showcase;
    private Transform[] miniatures;
    private TMP_Text label;
    private int activePage;
    private float hideAt;
    private float nextPageAt;
    private bool visible;

    private void Awake()
    {
        BuildShowcase();
        if (showcase != null) showcase.gameObject.SetActive(false);
    }

    public void ShowCatalog(float seconds = 16f)
    {
        if (showcase == null) BuildShowcase();
        if (showcase == null) return;
        visible = true;
        activePage = 0;
        hideAt = Time.time + Mathf.Max(6f, seconds);
        nextPageAt = Time.time + 2.7f;
        showcase.gameObject.SetActive(true);
        SetPage(activePage);
    }

    private void Update()
    {
        if (!visible || showcase == null) return;
        if (Time.time >= hideAt)
        {
            visible = false;
            showcase.gameObject.SetActive(false);
            return;
        }
        if (Time.time >= nextPageAt)
        {
            activePage = (activePage + 1) % PageTitles.Length;
            SetPage(activePage);
            nextPageAt = Time.time + 2.7f;
        }
        Transform model = miniatures[activePage];
        if (model != null)
        {
            model.Rotate(Vector3.up, 48f * Time.deltaTime, Space.World);
            Vector3 p = model.localPosition;
            p.y = 0.45f + Mathf.Sin(Time.time * 2.1f) * 0.055f;
            model.localPosition = p;
        }
    }

    private void BuildShowcase()
    {
        Animator animator = GetComponentInChildren<Animator>();
        Transform hand = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
        Transform anchor = hand != null ? hand : transform;
        showcase = new GameObject("Lumen Handheld Learning Catalog").transform;
        showcase.SetParent(anchor, false);
        showcase.localPosition = hand != null ? new Vector3(0.02f, 0.17f, 0.02f) : new Vector3(0.56f, 1.22f, 0.32f);
        showcase.localRotation = Quaternion.identity;
        showcase.localScale = Vector3.one * (hand != null ? 0.42f : 0.36f);

        miniatures = new Transform[PageTitles.Length];
        miniatures[0] = BuildSolarPreview();
        miniatures[1] = BuildAtomPreview();
        miniatures[2] = BuildBiologyPreview();
        miniatures[3] = BuildNaturePreview();
        miniatures[4] = BuildMathPreview();
        foreach (Transform model in miniatures) model.SetParent(showcase, false);

        GameObject textObject = new GameObject("Catalog page title", typeof(TextMeshPro));
        textObject.transform.SetParent(showcase, false);
        textObject.transform.localPosition = new Vector3(0f, 1.27f, 0f);
        label = textObject.GetComponent<TextMeshPro>();
        label.fontSize = 0.95f;
        label.color = new Color(1f, 0.95f, 0.75f);
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.rectTransform.sizeDelta = new Vector2(5.2f, 1.05f);
        textObject.AddComponent<WorldLabelBillboard>();

        GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        halo.name = "Catalog glow";
        halo.transform.SetParent(showcase, false);
        halo.transform.localPosition = new Vector3(0f, 0.48f, 0f);
        halo.transform.localScale = new Vector3(1.48f, 0.16f, 1.1f);
        SetMaterial(halo, new Color(0.98f, 0.69f, 0.31f));
        Object.Destroy(halo.GetComponent<Collider>());
    }

    private void SetPage(int page)
    {
        for (int i = 0; i < miniatures.Length; i++) miniatures[i].gameObject.SetActive(i == page);
        label.text = PageTitles[page];
    }

    private Transform BuildSolarPreview()
    {
        Transform root = NewModel("Miniature solar system");
        MakeSphere(root, "Sun", new Vector3(0f, 0.45f, 0f), 0.27f, new Color(1f, 0.70f, 0.2f));
        for (int i = 0; i < 5; i++)
        {
            float radius = 0.42f + i * 0.13f;
            MakeSphere(root, "Planet", new Vector3(radius, 0.45f, 0f), 0.055f + (i % 2) * 0.025f,
                new[] { new Color(0.68f, 0.58f, 0.45f), new Color(0.38f, 0.70f, 0.95f), new Color(0.91f, 0.39f, 0.27f), new Color(0.82f, 0.68f, 0.48f), new Color(0.73f, 0.62f, 0.91f) }[i]);
            MakeOrbit(root, radius, new Color(0.88f, 0.88f, 0.78f, 0.65f));
        }
        return root;
    }

    private Transform BuildAtomPreview()
    {
        Transform root = NewModel("Miniature atom and chemistry");
        MakeSphere(root, "Nucleus", new Vector3(0f, 0.48f, 0f), 0.22f, new Color(0.95f, 0.38f, 0.28f));
        for (int i = 0; i < 3; i++)
        {
            float angle = i * Mathf.PI * 2f / 3f;
            MakeSphere(root, "Electron", new Vector3(Mathf.Cos(angle) * 0.54f, 0.48f + Mathf.Sin(angle) * 0.16f, Mathf.Sin(angle) * 0.54f), 0.085f, new Color(0.25f, 0.82f, 0.97f));
        }
        MakeOrbit(root, 0.52f, new Color(0.67f, 0.55f, 0.97f));
        MakeBox(root, "Periodic table card", new Vector3(0.58f, 0.66f, -0.22f), new Vector3(0.32f, 0.42f, 0.04f), new Color(0.36f, 0.72f, 0.52f));
        MakeSphere(root, "Water molecule oxygen", new Vector3(-0.62f, 0.55f, 0.08f), 0.14f, new Color(0.92f, 0.28f, 0.24f));
        MakeSphere(root, "Water molecule hydrogen", new Vector3(-0.81f, 0.52f, 0.12f), 0.08f, Color.white);
        MakeSphere(root, "Water molecule hydrogen", new Vector3(-0.49f, 0.53f, 0.17f), 0.08f, Color.white);
        return root;
    }

    private Transform BuildBiologyPreview()
    {
        Transform root = NewModel("Miniature biology models");
        MakeSphere(root, "Plant cell", new Vector3(-0.4f, 0.47f, 0f), 0.42f, new Color(0.36f, 0.72f, 0.42f));
        MakeSphere(root, "Cell nucleus", new Vector3(-0.4f, 0.47f, -0.3f), 0.12f, new Color(0.57f, 0.39f, 0.88f));
        for (int i = 0; i < 9; i++)
        {
            float t = i / 8f;
            MakeSphere(root, "DNA bead", new Vector3(0.35f + Mathf.Sin(t * Mathf.PI * 3f) * 0.15f, 0.2f + t * 0.62f, 0f), 0.07f, new Color(0.34f, 0.68f + t * 0.2f, 0.91f));
            MakeSphere(root, "DNA bead", new Vector3(0.35f - Mathf.Sin(t * Mathf.PI * 3f) * 0.15f, 0.2f + t * 0.62f, 0.04f), 0.07f, new Color(0.98f, 0.54f, 0.32f));
        }
        MakeSphere(root, "Heart model", new Vector3(0.82f, 0.46f, 0f), 0.28f, new Color(0.91f, 0.24f, 0.32f));
        return root;
    }

    private Transform BuildNaturePreview()
    {
        Transform root = NewModel("Miniature Earth and plants");
        MakeSphere(root, "Earth", new Vector3(-0.52f, 0.48f, 0f), 0.38f, new Color(0.23f, 0.56f, 0.92f));
        MakeSphere(root, "Continent", new Vector3(-0.43f, 0.60f, -0.24f), 0.14f, new Color(0.3f, 0.72f, 0.39f));
        MakeBox(root, "Volcano slope", new Vector3(0.4f, 0.43f, 0f), new Vector3(0.54f, 0.62f, 0.54f), new Color(0.56f, 0.35f, 0.25f));
        MakeSphere(root, "Sunflower center", new Vector3(0.98f, 0.78f, 0f), 0.15f, new Color(0.46f, 0.25f, 0.11f));
        MakeBox(root, "Sunflower stem", new Vector3(0.98f, 0.42f, 0f), new Vector3(0.09f, 0.52f, 0.09f), new Color(0.22f, 0.63f, 0.30f));
        MakeSphere(root, "Oak canopy", new Vector3(-0.98f, 0.88f, 0.08f), 0.33f, new Color(0.27f, 0.49f, 0.24f));
        MakeBox(root, "Oak trunk", new Vector3(-0.98f, 0.42f, 0.08f), new Vector3(0.14f, 0.55f, 0.14f), new Color(0.42f, 0.28f, 0.18f));
        return root;
    }

    private Transform BuildMathPreview()
    {
        Transform root = NewModel("Miniature physics and mathematics");
        MakeBox(root, "Circuit battery", new Vector3(-0.62f, 0.42f, 0f), new Vector3(0.26f, 0.52f, 0.22f), new Color(0.32f, 0.74f, 0.51f));
        MakeSphere(root, "Circuit lamp", new Vector3(0.04f, 0.63f, 0f), 0.19f, new Color(1f, 0.77f, 0.26f));
        MakeBox(root, "Spectrum red", new Vector3(0.57f, 0.44f, 0f), new Vector3(0.17f, 0.75f, 0.17f), new Color(0.91f, 0.3f, 0.24f));
        MakeBox(root, "Spectrum green", new Vector3(0.78f, 0.44f, 0f), new Vector3(0.17f, 0.75f, 0.17f), new Color(0.28f, 0.75f, 0.42f));
        MakeBox(root, "Spectrum blue", new Vector3(0.99f, 0.44f, 0f), new Vector3(0.17f, 0.75f, 0.17f), new Color(0.30f, 0.50f, 0.91f));
        MakeBox(root, "Pythagorean square", new Vector3(-0.02f, 0.26f, -0.25f), new Vector3(0.52f, 0.08f, 0.5f), new Color(0.96f, 0.48f, 0.33f));
        return root;
    }

    private static Transform NewModel(string name) => new GameObject(name).transform;

    private static GameObject MakeSphere(Transform parent, string name, Vector3 position, float diameter, Color color)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = Vector3.one * diameter;
        SetMaterial(item, color);
        Object.Destroy(item.GetComponent<Collider>());
        return item;
    }

    private static GameObject MakeBox(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        SetMaterial(item, color);
        Object.Destroy(item.GetComponent<Collider>());
        return item;
    }

    private static void MakeOrbit(Transform parent, float radius, Color color)
    {
        GameObject orbit = new GameObject("Miniature orbital path");
        orbit.transform.SetParent(parent, false);
        LineRenderer line = orbit.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 48;
        line.startWidth = line.endWidth = 0.012f;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default")) { color = color };
        line.sharedMaterial = mat;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.45f, Mathf.Sin(angle) * radius));
        }
    }

    private static void SetMaterial(GameObject item, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        item.GetComponent<Renderer>().sharedMaterial = new Material(shader) { color = color };
    }
}

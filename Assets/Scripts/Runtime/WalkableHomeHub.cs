using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

/// <summary>Runtime-built, walkable 3D atrium for the home scene.</summary>
public static class WalkableHomeHub
{
    private const string WorldName = "LumenWalkableHub";
    private static readonly Vector3 Spawn = new Vector3(0f, 0f, 31.5f);

    public static void Ensure()
    {
        // This unmaterialed plane belongs to the retired prototype front screen.
        // The atrium provides its own solid ground and paths.
        GameObject oldFloor = GameObject.Find("BackgroundFloor");
        if (oldFloor != null) oldFloor.SetActive(false);
        GameObject existing = GameObject.Find(WorldName);
        if (existing == null)
        {
            BuildAtrium();
        }
        else if (existing.transform.Find("Portal · Ancient Rome/Rift Rings") == null)
        {
            // Enter Play Mode can preserve generated objects when Unity's
            // reload options are customized. Replace an older hub so a code
            // update never leaves the player looking at stale portals.
            existing.SetActive(false);
            Object.Destroy(existing);
            BuildAtrium();
        }
        EnsurePlayerAndCamera();
        EnsureGuide();
    }

    public static void BindPrompt(TMP_Text prompt)
    {
        WalkableHubInteractor interactor = Object.FindAnyObjectByType<WalkableHubInteractor>();
        if (interactor != null) interactor.SetPrompt(prompt);
    }

    private static void BuildAtrium()
    {
        GameObject world = new GameObject(WorldName);
        world.transform.position = Vector3.zero;
        Material grass = MaterialFor("Meadow midnight grass", new Color(0.08f, 0.18f, 0.20f));
        Material stone = MaterialFor("Warm moonstone limestone", new Color(0.24f, 0.28f, 0.38f));
        Material brass = MaterialFor("Moonlit brass", new Color(0.75f, 0.47f, 0.18f), true);
        Material cyan = MaterialFor("Classroom nebula cyan", new Color(0.10f, 0.54f, 0.86f), true);
        Material coral = MaterialFor("Rome nebula coral", new Color(0.90f, 0.25f, 0.24f), true);
        Material mint = MaterialFor("Cafe nebula mint", new Color(0.12f, 0.68f, 0.52f), true);
        Material violet = MaterialFor("Museum nebula violet", new Color(0.54f, 0.36f, 0.92f), true);
        Material leaf = MaterialFor("Garden night leaf", new Color(0.08f, 0.23f, 0.25f));

        // Keep the hub physically walkable, but let it read as an open garden
        // rather than a sealed showroom. The sky, surrounding scenery and trees
        // provide depth at first launch.
        AddBox(world.transform, "Open meadow ground", new Vector3(0f, -0.42f, 40f), new Vector3(100f, 0.84f, 100f), grass, true);
        AddBox(world.transform, "Welcome stone path", new Vector3(0f, -0.015f, 40f), new Vector3(12f, 0.06f, 34f), stone, false);
        AddAtriumArchitecture(world.transform, stone, brass, leaf);
        AddSkyStars(world.transform);
        Material pathCoral = MaterialFor("Rome garden trail", new Color(0.84f, 0.49f, 0.39f));
        Material pathCyan = MaterialFor("Classroom garden trail", new Color(0.36f, 0.66f, 0.78f));
        Material pathMint = MaterialFor("Cafe garden trail", new Color(0.38f, 0.66f, 0.42f));
        Material pathViolet = MaterialFor("Museum garden trail", new Color(0.63f, 0.52f, 0.76f));
        AddFloorLine(world.transform, new Vector3(-10.5f, 0.02f, 29f), new Vector3(-10.5f, 0.02f, 48f), pathCoral);
        AddFloorLine(world.transform, new Vector3(-3.5f, 0.02f, 29f), new Vector3(-3.5f, 0.02f, 48f), pathCyan);
        AddFloorLine(world.transform, new Vector3(3.5f, 0.02f, 29f), new Vector3(3.5f, 0.02f, 48f), pathMint);
        AddFloorLine(world.transform, new Vector3(10.5f, 0.02f, 29f), new Vector3(10.5f, 0.02f, 48f), pathViolet);

        AddCentralOrb(world.transform, brass, cyan);
        AddPortal(world.transform, "Ancient Rome", "THE FORUM  ·  AN EMPIRE", "ancient_rome", -10.5f, 50.5f, coral);
        AddPortal(world.transform, "Classroom", "SCIENCE  ·  NATURE  ·  IDEAS", "classroom", -3.5f, 50.5f, cyan);
        AddPortal(world.transform, "Café", "MEET  ·  TALK  ·  ORDER", "cafe", 3.5f, 50.5f, mint);
        AddPortal(world.transform, "Museum", "OBJECTS  ·  STORIES  ·  TIME", "museum", 10.5f, 50.5f, violet);

        AddAreaLight(world.transform, "Garden moon fill", new Vector3(0f, 9f, 38f), new Color(0.34f, 0.58f, 1f), 4.2f, 34f);
        AddAreaLight(world.transform, "Portal color wash", new Vector3(0f, 5f, 48f), new Color(0.34f, 0.70f, 1f), 3.2f, 22f);
        ConfigureGardenLighting(world.transform);
    }

    private static void AddAtriumArchitecture(Transform parent, Material stone, Material brass, Material leaf)
    {
        AddWorldText(parent, "A LITTLE WORLD OF BIG IDEAS", new Vector3(0f, 5.3f, 61.4f), 0.88f,
            new Color(0.14f, 0.20f, 0.18f), new Vector2(24f, 1.5f));
        AddWorldText(parent, "WANDER A WHILE  ·  ASK LUMEN  ·  FOLLOW YOUR CURIOSITY", new Vector3(0f, 4.42f, 61.4f), 0.34f,
            new Color(0.27f, 0.36f, 0.30f), new Vector2(25f, 0.8f));
        AddBox(parent, "Welcome sign backing", new Vector3(0f, 4.9f, 61.65f), new Vector3(26f, 2.4f, 0.22f), stone, false);

        Color[] foliage = { new Color(0.21f, 0.43f, 0.24f), new Color(0.31f, 0.52f, 0.25f), new Color(0.16f, 0.37f, 0.29f) };
        int tree = 0;
        foreach (float x in new[] { -24f, -19f, 19f, 24f })
        foreach (float z in new[] { 22f, 34f, 46f, 59f })
            AddGardenTree(parent, new Vector3(x, 0f, z), stone, MaterialFor("Canopy " + tree, foliage[tree++ % foliage.Length]));

        Color[] petals = { new Color(1f, 0.47f, 0.35f), new Color(1f, 0.79f, 0.26f), new Color(0.75f, 0.55f, 0.91f), new Color(0.94f, 0.83f, 0.69f) };
        AddFlowerPatch(parent, new Vector3(-7.5f, 0f, 27f), leaf, petals);
        AddFlowerPatch(parent, new Vector3(7.5f, 0f, 27f), leaf, petals);
        AddFlowerPatch(parent, new Vector3(-16f, 0f, 42f), leaf, petals);
        AddFlowerPatch(parent, new Vector3(16f, 0f, 42f), leaf, petals);
        AddPlanter(parent, "Sunlit garden planter", new Vector3(-15.7f, 0f, 55f), stone, leaf);
        AddPlanter(parent, "Curiosity garden planter", new Vector3(15.7f, 0f, 55f), stone, leaf);
        AddBench(parent, new Vector3(-14.3f, 0f, 31f), stone, brass);
        AddBench(parent, new Vector3(14.3f, 0f, 31f), stone, brass);
    }

    private static void AddGardenTree(Transform parent, Vector3 position, Material bark, Material leaves)
    {
        AddCylinder(parent, "Garden tree trunk", position + Vector3.up * 1.55f, new Vector3(0.42f, 1.55f, 0.42f), bark);
        for (int i = 0; i < 3; i++)
        {
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Garden tree crown";
            crown.transform.SetParent(parent, false);
            crown.transform.position = position + new Vector3((i - 1) * 0.72f, 3.05f + (i % 2) * 0.34f, (i % 2) * 0.35f);
            crown.transform.localScale = new Vector3(2.0f, 1.85f, 1.9f);
            crown.GetComponent<Renderer>().sharedMaterial = leaves;
            Object.Destroy(crown.GetComponent<Collider>());
        }
    }

    private static void AddSkyStars(Transform parent)
    {
        Material starMaterial = MaterialFor("Lumen stardust", new Color(0.44f, 0.78f, 1f), true);
        for (int i = 0; i < 96; i++)
        {
            float seed = i + 1f;
            float x = Mathf.Sin(seed * 12.9898f) * 28f;
            float z = 18f + Mathf.Abs(Mathf.Cos(seed * 7.123f)) * 68f;
            float y = 7.5f + Mathf.Abs(Mathf.Sin(seed * 3.317f)) * 20f;
            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Lumen stardust star";
            star.transform.SetParent(parent, false);
            star.transform.localPosition = new Vector3(x, y, z);
            float size = 0.035f + (i % 5) * 0.018f;
            star.transform.localScale = Vector3.one * size;
            star.GetComponent<Renderer>().sharedMaterial = starMaterial;
            Object.Destroy(star.GetComponent<Collider>());
        }

        Material rose = MaterialFor("Lumen rose nebula", new Color(0.92f, 0.28f, 0.67f), true);
        Material aqua = MaterialFor("Lumen aqua nebula", new Color(0.18f, 0.73f, 0.96f), true);
        AddSkyOrb(parent, "Rose nebula beacon", new Vector3(-17f, 15f, 69f), 0.52f, rose);
        AddSkyOrb(parent, "Aqua nebula beacon", new Vector3(17f, 12f, 60f), 0.38f, aqua);
    }

    private static void AddSkyOrb(Transform parent, string name, Vector3 position, float size, Material material)
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = name;
        orb.transform.SetParent(parent, false);
        orb.transform.localPosition = position;
        orb.transform.localScale = Vector3.one * size;
        orb.GetComponent<Renderer>().sharedMaterial = material;
        Object.Destroy(orb.GetComponent<Collider>());
    }

    private static void AddFlowerPatch(Transform parent, Vector3 position, Material stems, Color[] palette)
    {
        for (int i = 0; i < 9; i++)
        {
            float angle = i * 2.399f;
            Vector3 p = position + new Vector3(Mathf.Cos(angle) * (0.45f + (i % 3) * 0.18f), 0f, Mathf.Sin(angle) * (0.45f + (i % 3) * 0.18f));
            AddCylinder(parent, "Wildflower stem", p + Vector3.up * 0.23f, new Vector3(0.045f, 0.23f, 0.045f), stems);
            GameObject bloom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bloom.name = "Colorful wildflower";
            bloom.transform.SetParent(parent, false);
            bloom.transform.position = p + Vector3.up * 0.5f;
            bloom.transform.localScale = Vector3.one * 0.22f;
            bloom.GetComponent<Renderer>().sharedMaterial = MaterialFor("Petal " + i, palette[i % palette.Length]);
            Object.Destroy(bloom.GetComponent<Collider>());
        }
    }

    private static void AddBench(Transform parent, Vector3 position, Material stone, Material brass)
    {
        AddBox(parent, "Garden bench seat", position + new Vector3(0f, 0.56f, 0f), new Vector3(2.4f, 0.18f, 0.65f), stone, false);
        AddBox(parent, "Garden bench back", position + new Vector3(0f, 1.03f, -0.25f), new Vector3(2.4f, 0.72f, 0.16f), stone, false);
        foreach (float x in new[] { -0.86f, 0.86f })
            AddBox(parent, "Garden bench leg", position + new Vector3(x, 0.28f, 0f), new Vector3(0.16f, 0.56f, 0.5f), brass, false);
    }

    private static void AddPlanter(Transform parent, string name, Vector3 position, Material stone, Material leaf)
    {
        AddCylinder(parent, name + " bowl", position + Vector3.up * 0.38f, new Vector3(1.25f, 0.38f, 1.25f), stone);
        for (int i = 0; i < 5; i++)
        {
            float angle = i * Mathf.PI * 0.4f;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * 0.28f, 1.02f + (i % 2) * 0.14f, Mathf.Sin(angle) * 0.28f);
            GameObject sprig = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sprig.name = name + " foliage";
            sprig.transform.SetParent(parent, false);
            sprig.transform.localPosition = position + offset;
            sprig.transform.localScale = new Vector3(0.38f, 0.78f, 0.34f);
            sprig.GetComponent<Renderer>().sharedMaterial = leaf;
            Object.Destroy(sprig.GetComponent<Collider>());
        }
    }

    private static void AddCylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = position;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<Renderer>().sharedMaterial = material;
        Object.Destroy(cylinder.GetComponent<Collider>());
    }

    private static void EnsurePlayerAndCamera()
    {
        Camera view = Camera.main;
        if (view == null) return;
        ConfigureGardenCamera(view);
        CafeWalkController existingMovement = view.GetComponentInParent<CafeWalkController>();
        if (existingMovement != null)
        {
            existingMovement.InitializeView(view.transform, 0f);
            return;
        }

        view.transform.SetParent(null, true);
        view.transform.SetPositionAndRotation(Spawn + Vector3.up * 1.62f, Quaternion.identity);
        GameObject player = new GameObject("WorldExplorer");
        player.transform.SetPositionAndRotation(Spawn + Vector3.up * 0.08f, Quaternion.identity);
        CharacterController body = player.AddComponent<CharacterController>();
        body.height = 1.8f;
        body.radius = 0.32f;
        body.center = new Vector3(0f, 0.9f, 0f);
        body.skinWidth = 0.04f;
        body.slopeLimit = 50f;
        body.stepOffset = 0.35f;
        view.transform.SetParent(player.transform, false);
        view.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        view.transform.localRotation = Quaternion.identity;
        CafeWalkController movement = player.AddComponent<CafeWalkController>();
        movement.InitializeView(view.transform, 0f);
        player.AddComponent<WalkableHubInteractor>().Initialize(view.transform, null);
    }

    private static void ConfigureGardenCamera(Camera view)
    {
        // The opening hub uses the bundled nebula cubemap so the atrium feels
        // like a small world suspended in space instead of a flat clear sky.
        Material galaxySky = Resources.Load<Material>("LumenGalaxySky");
        if (galaxySky != null) RenderSettings.skybox = galaxySky;
        view.clearFlags = CameraClearFlags.Skybox;
        view.backgroundColor = new Color(0.015f, 0.025f, 0.07f);
    }

    private static void ConfigureGardenLighting(Transform parent)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.08f, 0.13f, 0.25f);
        RenderSettings.ambientEquatorColor = new Color(0.13f, 0.16f, 0.23f);
        RenderSettings.ambientGroundColor = new Color(0.035f, 0.045f, 0.08f);
        RenderSettings.ambientIntensity = 0.62f;
        RenderSettings.reflectionIntensity = 0.32f;

        GameObject sunObject = new GameObject("Garden daylight");
        sunObject.transform.SetParent(parent, false);
        sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.91f, 0.75f);
        sun.intensity = 0.72f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.42f;
    }

    private static void EnsureGuide()
    {
        GameObject guide = GameObject.Find("HomeAgent") ?? GameObject.Find("Assistant");
        if (guide == null) guide = new GameObject("HomeAgent");

        foreach (Renderer renderer in guide.GetComponentsInChildren<Renderer>(true))
            if (renderer.transform != guide.transform && (guide.transform.Find("LumenGuideVisual")==null||!renderer.transform.IsChildOf(guide.transform.Find("LumenGuideVisual")))) renderer.enabled = false;
        if (guide.transform.Find("LumenGuideVisual") == null)
        {
            GameObject prefab = Resources.Load<GameObject>("ScenarioCharacters/Kai")??Resources.Load<GameObject>("ScenarioCharacters/Marcus");
            if (prefab != null)
            {
                GameObject visual = Object.Instantiate(prefab, guide.transform);
                visual.name = "LumenGuideVisual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.tag = "Untagged";
                    child.gameObject.layer = 0;
                }
                foreach (MonoBehaviour script in visual.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
                foreach (CharacterController controller in visual.GetComponentsInChildren<CharacterController>(true)) controller.enabled = false;
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true)) animator.enabled = true;
            }
            else Debug.LogWarning("Human guide prefabs are missing from Resources/ScenarioCharacters.");
        }

        guide.transform.position = Spawn + Vector3.forward * 3.5f + Vector3.right * 1.3f;
        guide.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        foreach (Renderer renderer in guide.GetComponents<Renderer>()) renderer.enabled = false;
        foreach (Collider collider in guide.GetComponents<Collider>()) collider.enabled = false;
        foreach (NavMeshAgent navigation in guide.GetComponents<NavMeshAgent>()) navigation.enabled = false;
        FloatingAgentIdle floating = guide.GetComponent<FloatingAgentIdle>();
        if (floating != null) floating.enabled = false;
        if (guide.GetComponent<AgentAnimator>() == null) guide.AddComponent<AgentAnimator>();
        ScenarioCharacterIdentity.InstallOnGuide(guide,"HomeScene");
        WalkableGuideFollower follower = guide.GetComponent<WalkableGuideFollower>();
        if (follower == null) follower = guide.AddComponent<WalkableGuideFollower>();
        GameObject player = GameObject.Find("WorldExplorer");
        if (player != null) follower.Initialize(player.transform);
    }

    private static void AddPortal(Transform parent, string title, string description, string scenario, float x, float z, Material accent)
    {
        GameObject portal = new GameObject("Portal · " + title);
        portal.transform.SetParent(parent, false);
        portal.transform.position = new Vector3(x, 0f, z);
        Color accentColor = accent.color;
        Material housing = MaterialFor(title + " rift housing", Color.Lerp(new Color(0.025f, 0.045f, 0.09f), accentColor, 0.22f));
        Material darkCore = MaterialFor(title + " rift depth", new Color(0.008f, 0.018f, 0.045f));
        Material ringBright = MaterialFor(title + " rift edge", Color.Lerp(accentColor, Color.white, 0.34f), true);
        Material ringSoft = MaterialFor(title + " rift aura", Color.Lerp(accentColor, Color.white, 0.08f), true);

        // A split arch reads as a doorway instead of a billboard, leaving a
        // deep aperture in the middle for the live destination preview.
        AddBox(portal.transform, "Portal left arch", new Vector3(-2.05f, 2.75f, 0.34f), new Vector3(0.62f, 5.7f, 0.62f), housing, false);
        AddBox(portal.transform, "Portal right arch", new Vector3(2.05f, 2.75f, 0.34f), new Vector3(0.62f, 5.7f, 0.62f), housing, false);
        AddBox(portal.transform, "Portal crown", new Vector3(0f, 5.56f, 0.34f), new Vector3(4.72f, 0.62f, 0.62f), housing, false);
        AddBox(portal.transform, "Portal crown energy band", new Vector3(0f, 5.22f, 0.02f), new Vector3(4.18f, 0.16f, 0.22f), ringBright, false);
        AddBox(portal.transform, "Portal threshold", new Vector3(0f, 0.08f, 0.44f), new Vector3(4.7f, 0.22f, 1.18f), housing, false);
        AddBox(portal.transform, "Portal threshold energy", new Vector3(0f, 0.22f, 0.03f), new Vector3(4.25f, 0.10f, 0.68f), ringBright, false);

        AddBox(portal.transform, "Rift depth window", new Vector3(0f, 2.68f, 0.31f), new Vector3(3.72f, 4.92f, 0.08f), darkCore, false);
        AddScenarioPreview(portal.transform, title, accent);
        AddPortalRiftLayers(portal.transform, title, ringBright, ringSoft, accentColor);

        GameObject titleObject = new GameObject("Portal title · " + title, typeof(TextMeshPro));
        titleObject.transform.SetParent(portal.transform, false);
        titleObject.transform.localPosition = new Vector3(0f, 6.38f, -0.05f);
        TextMeshPro label = titleObject.GetComponent<TextMeshPro>();
        label.text = title.ToUpperInvariant();
        // TextMeshPro world-space font sizes are world-unit sized; screen-space
        // values such as 16 overwhelm the metre-wide placard from the spawn view.
        label.fontSize = 4.35f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.13f, 0.17f, 0.15f);
        label.alignment = TextAlignmentOptions.Center;
        label.outlineWidth = 0.12f;
        label.outlineColor = new Color(1f, 0.97f, 0.88f);
        label.rectTransform.sizeDelta = new Vector2(7.5f, 1.15f);
        titleObject.AddComponent<WorldLabelBillboard>();

        GameObject subtitleObject = new GameObject("Portal description · " + title, typeof(TextMeshPro));
        subtitleObject.transform.SetParent(portal.transform, false);
        subtitleObject.transform.localPosition = new Vector3(0f, 5.55f, -0.2f);
        TextMeshPro subtitle = subtitleObject.GetComponent<TextMeshPro>();
        subtitle.text = description;
        subtitle.fontSize = 1.38f;
        subtitle.color = new Color(0.23f, 0.29f, 0.27f);
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.rectTransform.sizeDelta = new Vector2(8.6f, 0.82f);
        subtitleObject.AddComponent<WorldLabelBillboard>();

        BoxCollider selection = portal.AddComponent<BoxCollider>();
        selection.isTrigger = true;
        selection.center = new Vector3(0f, 2.65f, 0.1f);
        selection.size = new Vector3(4.05f, 5.85f, 2.2f);
        HomeHubPortal route = portal.AddComponent<HomeHubPortal>();
        route.scenario = scenario;
        route.displayName = title;
        route.Initialize(accent.color);
        AddAreaLight(portal.transform, "Portal glow", new Vector3(0f, 2.3f, -0.9f), accent.color, 4.6f, 8f);
    }

    private static void AddPortalRiftLayers(Transform portal, string title, Material bright, Material soft, Color accent)
    {
        Transform rings = new GameObject("Rift Rings").transform;
        rings.SetParent(portal, false);
        rings.localPosition = new Vector3(0f, 2.68f, -0.11f);
        AddVerticalRing(rings, "Rift Ring Outer", new Vector2(1.88f, 2.49f), bright, 0.105f, 0f);
        AddVerticalRing(rings, "Rift Ring Middle", new Vector2(1.68f, 2.29f), soft, 0.055f, 0.01f);
        AddVerticalRing(rings, "Rift Ring Inner", new Vector2(1.46f, 2.03f), bright, 0.04f, 0.02f);

        Transform motes = new GameObject("Rift Motes").transform;
        motes.SetParent(portal, false);
        motes.localPosition = new Vector3(0f, 2.68f, -0.18f);
        for (int i = 0; i < 18; i++)
        {
            float angle = i / 18f * Mathf.PI * 2f;
            float radius = 1.58f + (i % 3) * 0.16f;
            GameObject mote = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mote.name = "Rift mote";
            mote.transform.SetParent(motes, false);
            mote.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 1.35f, (i % 2) * 0.08f);
            mote.transform.localScale = Vector3.one * (0.045f + (i % 4) * 0.018f);
            mote.GetComponent<Renderer>().sharedMaterial = i % 3 == 0 ? bright : soft;
            Object.Destroy(mote.GetComponent<Collider>());
        }

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "Rift Core";
        core.transform.SetParent(portal, false);
        core.transform.localPosition = new Vector3(0f, 2.68f, -0.2f);
        core.transform.localScale = Vector3.one * 0.36f;
        core.GetComponent<Renderer>().sharedMaterial = bright;
        Object.Destroy(core.GetComponent<Collider>());

        AddVerticalRing(portal, "Ground energy halo", new Vector2(2.08f, 0.26f), soft, 0.045f, 0.02f);
        Transform halo = portal.Find("Ground energy halo");
        if (halo != null) halo.localPosition = new Vector3(0f, 0.24f, -0.2f);

        GameObject leftPylon = AddBox(portal, "Left rift pylon", new Vector3(-2.45f, 2.2f, -0.04f), new Vector3(0.18f, 3.9f, 0.18f), bright, false);
        GameObject rightPylon = AddBox(portal, "Right rift pylon", new Vector3(2.45f, 2.2f, -0.04f), new Vector3(0.18f, 3.9f, 0.18f), bright, false);
        leftPylon.transform.localRotation = Quaternion.Euler(0f, 0f, -7f);
        rightPylon.transform.localRotation = Quaternion.Euler(0f, 0f, 7f);
        AddWorldText(portal, "ENTER THE " + title.ToUpperInvariant(), new Vector3(0f, 6.18f, -0.2f), 0.24f,
            Color.Lerp(accent, Color.white, 0.28f), new Vector2(7.4f, 0.5f));
    }

    private static void AddVerticalRing(Transform parent, string name, Vector2 radius, Material material, float width, float z)
    {
        GameObject ringObject = new GameObject(name);
        ringObject.transform.SetParent(parent, false);
        ringObject.transform.localPosition = new Vector3(0f, 0f, z);
        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 96;
        ring.startWidth = ring.endWidth = width;
        ring.sharedMaterial = material;
        ring.startColor = ring.endColor = material.color;
        ring.shadowCastingMode = ShadowCastingMode.Off;
        ring.receiveShadows = false;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i / (float)ring.positionCount * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y, 0f));
        }
    }

    private static void AddScenarioPreview(Transform portal, string title, Material accent)
    {
        Transform preview = new GameObject("Scenario Preview").transform;
        preview.SetParent(portal, false);
        preview.localPosition = new Vector3(0f, 2.68f, 0.05f);

        Material previewFloor = MaterialFor(title + " preview floor", Color.Lerp(accent.color, new Color(0.015f, 0.02f, 0.05f), 0.72f));
        AddBox(preview, "Preview floor", new Vector3(0f, -2.05f, -0.1f), new Vector3(3.35f, 0.12f, 1.6f), previewFloor, false);
        Transform vignette = new GameObject("Preview vignette").transform;
        vignette.SetParent(preview, false);
        vignette.localPosition = new Vector3(0f, 0f, -0.06f);

        Material highlight = MaterialFor(title + " preview highlight", Color.Lerp(accent.color, Color.white, 0.22f), true);
        switch (title)
        {
            case "Ancient Rome":
                Material stone = MaterialFor("Rome preview marble", new Color(0.66f, 0.50f, 0.31f));
                AddCylinder(vignette, "Rome preview column left", new Vector3(-1.05f, -0.75f, 0f), new Vector3(0.22f, 0.90f, 0.22f), stone);
                AddCylinder(vignette, "Rome preview column right", new Vector3(1.05f, -0.75f, 0f), new Vector3(0.22f, 0.90f, 0.22f), stone);
                AddBox(vignette, "Rome preview lintel", new Vector3(0f, 0.18f, 0f), new Vector3(2.55f, 0.25f, 0.25f), stone, false);
                AddBox(vignette, "Rome preview forum", new Vector3(0f, -1.36f, 0.06f), new Vector3(2.25f, 0.12f, 0.8f), highlight, false);
                AddPreviewSphere(vignette, "Rome preview sun", new Vector3(0.55f, 1.10f, 0f), 0.34f, highlight);
                break;
            case "Classroom":
                Material desk = MaterialFor("Classroom preview desk", new Color(0.42f, 0.25f, 0.16f));
                AddBox(vignette, "Classroom preview desk", new Vector3(0f, -1.28f, 0f), new Vector3(2.2f, 0.22f, 0.82f), desk, false);
                AddBox(vignette, "Classroom preview board", new Vector3(0f, 0.92f, 0.04f), new Vector3(2.48f, 1.15f, 0.12f), highlight, false);
                AddPreviewSphere(vignette, "Classroom preview planet", new Vector3(0f, -0.15f, -0.16f), 0.52f, highlight);
                AddVerticalRing(vignette, "Classroom preview orbit", new Vector2(0.78f, 0.30f), accent, 0.035f, -0.2f);
                Transform orbit = vignette.Find("Classroom preview orbit");
                if (orbit != null) orbit.localPosition = new Vector3(0f, -0.15f, -0.25f);
                break;
            case "Café":
                Material wood = MaterialFor("Cafe preview wood", new Color(0.42f, 0.20f, 0.12f));
                Material coffee = MaterialFor("Cafe preview coffee", new Color(0.18f, 0.07f, 0.035f), true);
                AddCylinder(vignette, "Cafe preview table", new Vector3(0f, -1.35f, 0f), new Vector3(1.20f, 0.14f, 1.20f), wood);
                AddCylinder(vignette, "Cafe preview cup", new Vector3(0f, -0.65f, -0.1f), new Vector3(0.38f, 0.42f, 0.38f), highlight);
                AddPreviewSphere(vignette, "Cafe preview coffee", new Vector3(0f, -0.23f, -0.1f), 0.24f, coffee);
                AddPreviewSphere(vignette, "Cafe preview steam one", new Vector3(-0.14f, 0.25f, -0.06f), 0.08f, highlight);
                AddPreviewSphere(vignette, "Cafe preview steam two", new Vector3(0.12f, 0.52f, -0.06f), 0.06f, highlight);
                AddBox(vignette, "Cafe preview awning", new Vector3(0f, 1.22f, 0f), new Vector3(2.55f, 0.20f, 0.50f), accent, false);
                break;
            case "Museum":
                Material museumStone = MaterialFor("Museum preview stone", new Color(0.44f, 0.47f, 0.58f));
                AddBox(vignette, "Museum preview pedestal", new Vector3(0f, -1.24f, 0f), new Vector3(0.86f, 0.72f, 0.86f), museumStone, false);
                GameObject artifact = AddPreviewSphere(vignette, "Museum preview artifact", new Vector3(0f, -0.18f, -0.02f), 0.55f, highlight);
                artifact.transform.localRotation = Quaternion.Euler(25f, 35f, 12f);
                AddVerticalRing(vignette, "Museum preview halo", new Vector2(0.82f, 1.05f), accent, 0.035f, -0.16f);
                Transform halo = vignette.Find("Museum preview halo");
                if (halo != null) halo.localPosition = new Vector3(0f, -0.16f, -0.24f);
                AddBox(vignette, "Museum preview lintel", new Vector3(0f, 1.22f, 0f), new Vector3(2.1f, 0.18f, 0.22f), museumStone, false);
                break;
            default:
                AddPreviewSphere(vignette, "Scenario preview orb", Vector3.zero, 0.7f, highlight);
                break;
        }

        AddPreviewSphere(vignette, "Preview glint", new Vector3(-1.15f, 1.25f, -0.22f), 0.10f, highlight);
        AddPreviewSphere(vignette, "Preview glint", new Vector3(1.12f, 0.78f, -0.22f), 0.07f, highlight);
    }

    private static GameObject AddPreviewSphere(Transform parent, string name, Vector3 position, float size, Material material)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = position;
        sphere.transform.localScale = Vector3.one * size;
        sphere.GetComponent<Renderer>().sharedMaterial = material;
        Object.Destroy(sphere.GetComponent<Collider>());
        return sphere;
    }

    private static void AddCentralOrb(Transform parent, Material gold, Material cyan)
    {
        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.name = "Welcome beacon plinth";
        pedestal.transform.SetParent(parent, false);
        pedestal.transform.position = new Vector3(0f, 0.16f, 39.7f);
        pedestal.transform.localScale = new Vector3(1.7f, 0.16f, 1.7f);
        pedestal.GetComponent<Renderer>().sharedMaterial = MaterialFor("Beacon sandstone plinth", new Color(0.78f, 0.63f, 0.40f));
        Object.Destroy(pedestal.GetComponent<Collider>());
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "Atrium welcome beacon";
        orb.transform.SetParent(parent, false);
        orb.transform.position = new Vector3(0f, 2.05f, 39.7f);
        orb.transform.localScale = Vector3.one * 0.58f;
        orb.GetComponent<Renderer>().sharedMaterial = cyan;
        Object.Destroy(orb.GetComponent<Collider>());
        AddWorldText(parent, "LUMEN'S GARDEN", new Vector3(0f, 3.1f, 39.7f), 0.52f, new Color(0.17f, 0.25f, 0.17f), new Vector2(5f, 0.6f));
    }

    private static void AddTrim(Transform parent, Material material, float z)
    {
        AddBox(parent, "Inlaid brass gallery band", new Vector3(0f, 0.02f, z), new Vector3(36f, 0.05f, 0.07f), material, false);
    }

    private static void AddFloorLine(Transform parent, Vector3 from, Vector3 to, Material material)
    {
        GameObject lineObject = new GameObject("Guiding light path");
        lineObject.transform.SetParent(parent, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.startWidth = line.endWidth = 0.028f;
        line.sharedMaterial = material;
        line.startColor = line.endColor = material.color;
    }

    private static void AddLine(Transform parent, string name, Vector3 center, float radius, Material material)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = 80;
        line.startWidth = line.endWidth = 0.025f;
        line.sharedMaterial = material;
        line.startColor = line.endColor = material.color;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i / (float)(line.positionCount - 1) * Mathf.PI * 2f;
            line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    private static void AddWorldText(Transform parent, string content, Vector3 position, float size, Color color, Vector2 dimensions)
    {
        GameObject labelObject = new GameObject("Atrium sign · " + content, typeof(TextMeshPro));
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.position = position;
        TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
        label.text = content;
        label.fontSize = size * 2.6f;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = dimensions;
        labelObject.AddComponent<WorldLabelBillboard>();
    }

    private static GameObject AddBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool collider)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider) Object.Destroy(box.GetComponent<Collider>());
        return box;
    }

    private static void AddAreaLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
    }

    private static Material MaterialFor(string name, Color color, bool emissive = false)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
        Material material = new Material(shader) { name = name, color = color };
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", name.IndexOf("porcelain", System.StringComparison.OrdinalIgnoreCase) >= 0 ? 0.72f : 0.38f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", name.IndexOf("brass", System.StringComparison.OrdinalIgnoreCase) >= 0 ? 0.55f : 0.12f);
        if (emissive && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * 1.35f);
        }
        if (name.IndexOf("portal veil", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        return material;
    }
}

public sealed class HomeHubPortal : MonoBehaviour
{
    public string scenario;
    public string displayName;
    private Color accent;
    private float phase;
    private bool entering;
    private Transform player;

    public void Initialize(Color color)
    {
        accent = color;
        phase = color.r * 2.7f + color.g * 4.1f;
    }

    public void Enter()
    {
        if (entering || string.IsNullOrWhiteSpace(scenario)) return;
        entering = true;
        ScenarioTransitionService.LoadScenario(scenario);
    }

    private void OnTriggerEnter(Collider other)
    {
        // The doorway itself is the interaction: walking through it is enough
        // to enter, while E/click/1–4 remain useful intentional shortcuts.
        if (other.GetComponentInParent<CafeWalkController>() != null) Enter();
    }

    private void OnMouseDown()
    {
        // Keep each doorway directly discoverable with a mouse as well as by
        // walking up and pressing E; this is especially helpful on first launch.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        Enter();
    }

    private void Update()
    {
        phase += Time.deltaTime;
        if (!entering)
        {
            if (player == null)
            {
                CafeWalkController movement = Object.FindAnyObjectByType<CafeWalkController>();
                if (movement != null) player = movement.transform;
            }
            if (player != null)
            {
                Vector3 localPlayer = transform.InverseTransformPoint(player.position);
                bool insideOpening = Mathf.Abs(localPlayer.x) < 1.65f
                    && localPlayer.y > -0.1f && localPlayer.y < 2.4f
                    && localPlayer.z > -1.55f && localPlayer.z < 0.70f;
                if (insideOpening) Enter();
            }
        }
        Light glow = GetComponentInChildren<Light>();
        if (glow != null) glow.intensity = 4.6f + Mathf.Sin(phase * 1.6f) * 1.2f;
        Transform rings = transform.Find("Rift Rings");
        if (rings != null)
        {
            rings.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.65f) * 8f);
            float pulse = 1f + Mathf.Sin(phase * 1.8f) * 0.035f;
            rings.localScale = Vector3.one * pulse;
        }
        Transform motes = transform.Find("Rift Motes");
        if (motes != null) motes.localRotation = Quaternion.Euler(0f, 0f, phase * 18f);
        Transform core = transform.Find("Rift Core");
        if (core != null)
        {
            float corePulse = 1f + Mathf.Sin(phase * 2.4f) * 0.16f;
            core.localScale = Vector3.one * (0.36f * corePulse);
        }
        Transform preview = transform.Find("Scenario Preview");
        if (preview != null)
        {
            preview.localPosition = new Vector3(0f, 2.68f + Mathf.Sin(phase * 1.15f) * 0.035f, 0.05f);
            preview.localScale = Vector3.one * (1f + Mathf.Sin(phase * 1.45f) * 0.012f);
        }
    }
}

public sealed class WalkableHubInteractor : MonoBehaviour
{
    private Transform view;
    private TMP_Text prompt;
    private HomeHubPortal currentPortal;

    public void Initialize(Transform cameraTransform, TMP_Text promptText)
    {
        view = cameraTransform;
        prompt = promptText;
    }

    public void SetPrompt(TMP_Text promptText) => prompt = promptText;

    private void Update()
    {
        if (view == null) return;
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            TMP_InputField selectedInput = EventSystem.current.currentSelectedGameObject.GetComponentInParent<TMP_InputField>();
            if (selectedInput != null && selectedInput.isFocused) return;
        }
        currentPortal = null;
        if (Physics.Raycast(view.position, view.forward, out RaycastHit hit, 22f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            currentPortal = hit.collider.GetComponentInParent<HomeHubPortal>();
        if (prompt != null)
            prompt.text = currentPortal != null
                ? "[E] ENTER  ·  " + currentPortal.displayName + "\n" + "Walk closer or press the matching 1–4 key."
                : string.Empty;

        if (currentPortal != null && PressedInteract()) currentPortal.Enter();
        int shortcut = ReadShortcut();
        if (shortcut >= 0)
        {
            HomeHubPortal[] portals = Object.FindObjectsByType<HomeHubPortal>(FindObjectsInactive.Exclude);
            System.Array.Sort(portals, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            if (shortcut < portals.Length) portals[shortcut].Enter();
        }
    }

    private static bool PressedInteract()
    {
#if ENABLE_INPUT_SYSTEM
        return UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    private static int ReadShortcut()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return -1;
        if (keyboard.digit1Key.wasPressedThisFrame) return 0;
        if (keyboard.digit2Key.wasPressedThisFrame) return 1;
        if (keyboard.digit3Key.wasPressedThisFrame) return 2;
        if (keyboard.digit4Key.wasPressedThisFrame) return 3;
#else
        if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) return 3;
#endif
        return -1;
    }
}

public sealed class WalkableGuideFollower : MonoBehaviour
{
    private Transform player;
    private Vector3 velocity;

    public void Initialize(Transform playerTransform) => player = playerTransform;

    private void Update()
    {
        if (player == null)
        {
            GameObject found = GameObject.Find("WorldExplorer");
            if (found != null) player = found.transform;
        }
        if (player == null) return;
        Vector3 target = player.position + player.forward * 3.5f + player.right * 1.3f;
        if (Physics.Raycast(target + Vector3.up * 2f, Vector3.down, out RaycastHit ground, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) target.y = ground.point.y;
        else target.y = 0f;
        float speed = Vector3.Distance(transform.position, target) > 4.5f ? 4.4f : 2.4f;
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, 0.32f, speed, Time.deltaTime);
        Vector3 travel = target - transform.position;
        travel.y = 0f;
        Vector3 facing = travel.sqrMagnitude > 0.04f ? travel : player.position - transform.position;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.05f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing), Time.deltaTime * 5f);
    }
}

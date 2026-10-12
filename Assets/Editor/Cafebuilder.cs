using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CafeBuilder
{
    const string RootName = "Cafe_Build";
    const string CafeName = "CAFE STORE";
    const string AutoBuildSessionKey = "EmbodiedAI.CafeBuilder.AutoBuildAttempted";

    const float YU = 3.6f;                 // top of upper floor
    const int StepCount = 18;
    const float StepRise = 0.2f, StepRun = 0.3f;
    const float StairZ0 = -5.0f, StairX0 = -7.85f, StairX1 = -6.25f;

    static System.Random rnd;
    static Material navy, navyDark, neon, wall, concrete, wood, woodDark, cream, red, steel, black,
                    green, pot, glass, lampM, winGlow, brick, plank, tile, orange,
                    floorTileGround, floorTileDeck, floorTileUpper;

    static CafeBuilder()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += TryAutoBuild;
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += TryAutoBuild;
    }

    static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(AutoBuildSessionKey, false)) return;
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded || activeScene.path != "Assets/Scenes/Cafestore.unity") return;
        if (GameObject.Find(RootName) != null)
        {
            SetupMood();
            EditorSceneManager.MarkSceneDirty(activeScene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("Existing Cafe_Build saved to Cafestore; no objects were rebuilt.");
            SessionState.SetBool(AutoBuildSessionKey, true);
            return;
        }
        SessionState.SetBool(AutoBuildSessionKey, true);
        Build();
    }

    [MenuItem("Tools/Build Cafe")]
    public static void Build()
    {
        if (!SceneManager.GetActiveScene().name.ToLower().StartsWith("cafe"))
        {
            EditorUtility.DisplayDialog("Open the Cafe scene first",
                "Open Assets/Scenes/Cafestore, then run Tools > Build Cafe again.", "OK");
            return;
        }
        GameObject old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);

        rnd = new System.Random(11);
        MakeMaterials();

        GameObject rootGo = new GameObject(RootName);
        Transform root = rootGo.transform;

        BuildGround(root);
        BuildShell(root);
        BuildFacade(root);
        BuildStairs(root);
        BuildUpperFloor(root);
        BuildGroundFloorInterior(root);
        BuildUpperFurniture(root);
        BuildExterior(root);
        BuildLights(root);
        BuildMarkers(root);
        SetupMood();

        Selection.activeGameObject = rootGo;
        Scene activeScene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("Cafe built and saved. The two-floor interior, collidable stairs, facade, materials, and cafe waypoint markers are ready.");
    }

    // ---------------------------------------------------------------- materials
    static void MakeMaterials()
    {
        navy     = Mat("Navy",     new Color(0.09f, 0.12f, 0.21f), 0.45f, 0.1f);
        navyDark = Mat("NavyDark", new Color(0.03f, 0.04f, 0.08f), 0.85f, 0.2f);
        neon     = Mat("Neon",     new Color(1f, 0.42f, 0.05f), 0.2f, 0f, 3.5f);
        wall     = Mat("Wall",     new Color(0.93f, 0.92f, 0.88f), 0.2f);
        concrete = Mat("Concrete", new Color(0.50f, 0.50f, 0.48f), 0.55f);
        wood     = Mat("Wood",     new Color(0.72f, 0.52f, 0.32f), 0.35f);
        woodDark = Mat("WoodDark", new Color(0.42f, 0.27f, 0.16f), 0.3f);
        cream    = Mat("Cream",    new Color(0.90f, 0.82f, 0.55f), 0.2f);
        red      = Mat("Red",      new Color(0.62f, 0.10f, 0.14f), 0.35f);
        steel    = Mat("Steel",    new Color(0.60f, 0.62f, 0.65f), 0.65f, 0.7f);
        black    = Mat("Black",    new Color(0.05f, 0.05f, 0.06f), 0.4f, 0.3f);
        green    = Mat("Green",    new Color(0.18f, 0.45f, 0.22f), 0.3f);
        pot      = Mat("Pot",      new Color(0.36f, 0.36f, 0.34f), 0.3f);
        orange   = Mat("OrangeSeat", new Color(0.95f, 0.45f, 0.10f), 0.3f);
        tile     = Mat("Tile",     new Color(0.95f, 0.95f, 0.93f), 0.7f);
        Texture2D floorTileTexture = MakeTexture("FloorTileTex", 128, FloorTilePixel);
        floorTileGround = FloorTileMaterial("FloorTile_Ground", floorTileTexture, new Vector2(4f, 3f));
        floorTileDeck   = FloorTileMaterial("FloorTile_Deck", floorTileTexture, new Vector2(6f, 6f));
        floorTileUpper  = FloorTileMaterial("FloorTile_Upper", floorTileTexture, new Vector2(4f, 1.5f));
        glass    = Mat("Glass",    new Color(0.65f, 0.82f, 1f, 0.22f), 0.9f, 0f, 0f, true);
        lampM    = Mat("Lamp",     new Color(1f, 0.80f, 0.50f), 0.2f, 0f, 3f);
        winGlow  = Mat("WindowGlow", new Color(0.70f, 0.85f, 1f), 0.5f, 0f, 1.8f);

        brick = Mat("Brick", Color.white, 0.1f);
        brick.SetTexture("_BaseMap", MakeTexture("BrickTex", 128, BrickPixel));
        brick.SetTextureScale("_BaseMap", new Vector2(18f, 4.5f));
        plank = Mat("Plank", Color.white, 0.25f);
        plank.SetTexture("_BaseMap", MakeTexture("PlankTex", 128, PlankPixel));
        plank.SetTextureScale("_BaseMap", new Vector2(40f, 19f));
        EditorUtility.SetDirty(brick);
        EditorUtility.SetDirty(plank);
    }

    static Material FloorTileMaterial(string name, Texture2D texture, Vector2 tiling)
    {
        Material m = Mat(name, Color.white, 0.22f, 0.04f);
        m.SetTexture("_BaseMap", texture);
        m.SetTextureScale("_BaseMap", tiling);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Mat(string name, Color c, float smooth = 0.3f, float metal = 0f, float emis = 0f, bool transparent = false)
    {
        string path = "Assets/Materials/Cafe_" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        if (transparent)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", 5f);
            m.SetFloat("_DstBlend", 10f);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetShaderPassEnabled("ShadowCaster", false);
            m.renderQueue = 3000;
        }
        if (emis > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(c.r * emis, c.g * emis, c.b * emis, 1f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D MakeTexture(string name, int size, System.Func<int, int, Color> pixel)
    {
        string path = "Assets/Materials/Cafe_" + name + ".png";
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                t.SetPixel(x, y, pixel(x, y));
        t.Apply();
        System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path);
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null) { imp.wrapMode = TextureWrapMode.Repeat; imp.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Color BrickPixel(int x, int y)
    {
        int row = y / 16;
        int off = (row % 2 == 0) ? 0 : 32;
        int xx = (x + off) % 128;
        bool mortar = (y % 16) < 2 || (xx % 64) < 2;
        if (mortar) return new Color(0.72f, 0.69f, 0.64f);
        int id = (xx / 64) + row * 7;
        float v = ((id * 37) % 11) / 11f * 0.14f;
        return new Color(0.50f + v, 0.24f + v * 0.5f, 0.18f + v * 0.4f);
    }

    static Color PlankPixel(int x, int y)
    {
        int pl = x / 32;
        if ((x % 32) < 1) return new Color(0.25f, 0.15f, 0.08f);
        float v = ((pl * 53) % 7) / 7f * 0.12f;
        float grain = (((y * 7 + x * 3) % 11) / 11f) * 0.03f;
        return new Color(0.52f + v + grain, 0.34f + v * 0.7f + grain, 0.20f + v * 0.5f);
    }

    static Color FloorTilePixel(int x, int y)
    {
        const int cell = 32;
        int lx = x % cell, ly = y % cell;
        if (lx < 2 || ly < 2 || lx >= cell - 2 || ly >= cell - 2)
            return new Color(0.39f, 0.40f, 0.41f);

        int tileId = (x / cell) + (y / cell) * 4;
        float tileShade = ((tileId * 17) % 7) * 0.012f;
        float grain = (((x * 13 + y * 7) % 9) / 9f) * 0.018f;
        float gray = 0.67f + tileShade + grain;
        return new Color(gray, gray + 0.005f, gray + 0.012f);
    }

    [MenuItem("Tools/Update Cafe Floor Tiles")]
    public static void UpdateCafeFloorTiles()
    {
        if (!SceneManager.GetActiveScene().name.ToLower().StartsWith("cafe"))
        {
            EditorUtility.DisplayDialog("Open the Cafe scene first",
                "Open Assets/Scenes/Cafestore, then run Tools > Update Cafe Floor Tiles again.", "OK");
            return;
        }

        MakeMaterials();
        GameObject rootGo = GameObject.Find(RootName);
        if (rootGo == null)
        {
            EditorUtility.DisplayDialog("Cafe build not found",
                "Run Tools > Build Cafe first, then update its floors.", "OK");
            return;
        }

        AssignFloorMaterial(rootGo.transform, "Ground/Deck", floorTileDeck);
        AssignFloorMaterial(rootGo.transform, "Ground/PlatformBase", floorTileDeck);
        AssignFloorMaterial(rootGo.transform, "Shell/InteriorFloor", floorTileGround);
        AssignFloorMaterial(rootGo.transform, "UpperFloor/UpperSlab", floorTileUpper);

        Scene activeScene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("Cafe floors updated and saved: grey tiles applied to the outside deck and both interior levels.");
    }

    static void AssignFloorMaterial(Transform root, string path, Material material)
    {
        Transform floor = root.Find(path);
        if (floor == null)
        {
            Debug.LogWarning("Cafe floor was not found: " + path);
            return;
        }
        Renderer renderer = floor.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    // ---------------------------------------------------------------- sections
    static void BuildGround(Transform root)
    {
        Transform g = Grp(root, "Ground", Vector3.zero);
        Slab(g, "DarkGround", -70, 70, -1.2f, -0.6f, -70, 70, navyDark);
        Slab(g, "PlatformBase", -12.4f, 12.4f, -0.6f, -0.3f, -16.4f, 7.4f, floorTileDeck);
        Slab(g, "Deck", -12, 12, -0.3f, 0f, -16, 7, floorTileDeck);
    }

    static void BuildShell(Transform root)
    {
        Transform s = Grp(root, "Shell", Vector3.zero);
        Slab(s, "InteriorFloor", -7.95f, 7.95f, -0.3f, 0.02f, -5.95f, 5.95f, floorTileGround);

        Slab(s, "Wall_L_Out", -8.15f, -7.95f, 0, 7.2f, -6.15f, 6.15f, navy);
        Slab(s, "Wall_R_Out", 7.95f, 8.15f, 0, 7.2f, -6.15f, 6.15f, navy);
        Slab(s, "Wall_B_Out", -7.95f, 7.95f, 0, 7.2f, 5.95f, 6.15f, navy);

        Slab(s, "Wall_L_In", -7.95f, -7.85f, 0, 6.9f, -5.85f, 5.95f, wall);
        Slab(s, "Wall_R_In", 7.85f, 7.95f, 0, 6.9f, -5.85f, 5.95f, wall);
        Slab(s, "Wall_B_In", -7.85f, 7.85f, 0, 6.9f, 5.85f, 5.95f, wall);

        Slab(s, "Ceiling_In", -7.95f, 7.95f, 6.9f, 7.0f, -5.95f, 5.95f, wall);
        Slab(s, "Roof", -8.15f, 8.15f, 7.0f, 7.2f, -6.15f, 6.15f, navy);
    }

    static void BuildFacade(Transform root)
    {
        Transform f = Grp(root, "Facade", Vector3.zero);
        Slab(f, "Pier_Left", -8.15f, -7.15f, 0f, 3.6f, -6.15f, -5.85f, navy);
        Slab(f, "Knee_Left", -7.15f, 1.6f, 0f, 0.8f, -6.1f, -5.9f, navy);
        Slab(f, "Knee_Right", 3.4f, 7.95f, 0f, 0.8f, -6.1f, -5.9f, navy);
        Slab(f, "Glass_Left", -7.15f, 1.6f, 0.8f, 3.3f, -6.03f, -5.97f, glass);
        Slab(f, "Glass_Right", 3.4f, 7.95f, 0.8f, 3.3f, -6.03f, -5.97f, glass);
        foreach (float mx in new[] { -4.25f, -1.35f, 1.55f, 3.45f, 5.7f })
            Slab(f, "Mullion", mx - 0.05f, mx + 0.05f, 0f, 3.4f, -6.1f, -5.9f, black);
        Slab(f, "Door_Header", 1.6f, 3.4f, 3.1f, 3.4f, -6.1f, -5.9f, navy);
        Slab(f, "Fascia", -7.95f, 7.95f, 3.3f, 3.6f, -6.15f, -5.85f, navy);
        Slab(f, "UpperFacade", -8.15f, 8.15f, 3.6f, 7.2f, -6.15f, -5.95f, navy);
        Slab(f, "UpperFacade_In", -7.85f, 7.85f, 3.6f, 6.9f, -5.95f, -5.85f, wall);

        // awning + neon
        Slab(f, "Awning", -8.15f, 8.15f, 3.6f, 3.85f, -7.6f, -6.15f, navy);
        Slab(f, "Neon_AwningEdge", -8.15f, 8.15f, 3.55f, 3.62f, -7.65f, -7.58f, neon, false);
        Slab(f, "Neon_Band", 1.0f, 8.15f, 4.35f, 4.42f, -6.2f, -6.15f, neon, false);
        Slab(f, "Neon_Vertical", -7.95f, -7.87f, 0f, 7.0f, -6.2f, -6.15f, neon, false);
        Slab(f, "Neon_Top", -7.95f, 0.9f, 6.92f, 7.0f, -6.2f, -6.15f, neon, false);

        // roof block with louvers
        Slab(f, "RoofBlock", 1.0f, 8.15f, 7.2f, 8.6f, -6.15f, 1.5f, navy);
        for (int k = 0; k < 7; k++)
            Slab(f, "Louver", 1.3f, 7.9f, 7.35f + k * 0.17f, 7.42f + k * 0.17f, -6.22f, -6.15f, steel, false);
        for (int k = 0; k < 6; k++)
            Slab(f, "VentSlat", -8.22f, -8.15f, 4.6f + k * 0.2f, 4.68f + k * 0.2f, -4.5f, -2.5f, woodDark, false);

        Label(f, "Sign_Cafe", new Vector3(-3.5f, 5.4f, -6.25f), CafeName, 14f, new Color(1f, 0.75f, 0.4f));
    }

    static void BuildStairs(Transform root)
    {
        Transform st = Grp(root, "Stairs", Vector3.zero);
        for (int i = 0; i < StepCount; i++)
            Slab(st, "Step_" + i, StairX0, StairX1, 0f, (i + 1) * StepRise,
                 StairZ0 + i * StepRun, StairZ0 + (i + 1) * StepRun, concrete);

        float run = StepCount * StepRun;
        float rise = StepCount * StepRise;
        float len = Mathf.Sqrt(run * run + rise * rise);
        float ang = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
        float bx = StairX1 - 0.05f;

        GameObject hr = Slab(st, "Handrail", 0, 1, 0, 1, 0, 1, red, false);
        hr.transform.localScale = new Vector3(0.07f, 0.07f, len);
        hr.transform.localPosition = new Vector3(bx, rise / 2f + 1.0f, StairZ0 + run / 2f);
        hr.transform.localRotation = Quaternion.Euler(-ang, 0f, 0f);

        for (int j = 0; j < StepCount; j += 2)
        {
            float z = StairZ0 + (j + 0.5f) * StepRun;
            float bot = (j + 1) * StepRise;
            Slab(st, "Baluster_" + j, bx - 0.02f, bx + 0.02f, bot, bot + 0.9f, z - 0.02f, z + 0.02f, black, false);
        }
    }

    static void BuildUpperFloor(Transform root)
    {
        Transform u = Grp(root, "UpperFloor", Vector3.zero);
        Slab(u, "UpperSlab", -7.85f, 7.85f, 3.4f, 3.6f, 0.4f, 5.85f, floorTileUpper);
        Slab(u, "UpperSlab_Ceil", -7.85f, 7.85f, 3.3f, 3.4f, 0.4f, 5.85f, wall);

        float rx0 = -6.25f, rx1 = 7.85f;
        Slab(u, "Rail_Top", rx0, rx1, 4.55f, 4.62f, 0.38f, 0.5f, red, false);
        Slab(u, "Rail_Bottom", rx0, rx1, 3.65f, 3.72f, 0.40f, 0.48f, black, false);
        for (float x = rx0 + 0.15f; x < rx1; x += 0.28f)
            Slab(u, "Baluster", x - 0.015f, x + 0.015f, 3.6f, 4.55f, 0.42f, 0.46f, black, false);
        ColliderBox(u, "Rail_Collider", rx0, rx1, 3.6f, 4.7f, 0.38f, 0.5f);

        // glowing windows on the upper walls
        foreach (float wx in new[] { -4f, 0f, 4f })
            Slab(u, "GlowWindow_Back", wx - 1.2f, wx + 1.2f, 4.6f, 6.2f, 5.80f, 5.85f, winGlow, false);
        foreach (float wz in new[] { 1.8f, 4.0f })
            Slab(u, "GlowWindow_Right", 7.80f, 7.85f, 4.6f, 6.2f, wz - 0.9f, wz + 0.9f, winGlow, false);
    }

    static void BuildGroundFloorInterior(Transform root)
    {
        Transform gf = Grp(root, "GroundFloor", Vector3.zero);

        // brick feature wall, sign, bench
        Slab(gf, "BrickWall", -6.4f, 2.4f, 0.45f, 3.15f, 5.75f, 5.85f, brick, false);
        Slab(gf, "SignBoard", -2.4f, -0.4f, 2.2f, 2.9f, 5.68f, 5.75f, woodDark, false);
        Label(gf, "Sign_COFFEE", new Vector3(-1.4f, 2.55f, 5.66f), "COFFEE", 4.5f, Color.white);
        Slab(gf, "Bench_Base", -6f, 2f, 0f, 0.38f, 5.1f, 5.7f, woodDark);
        Slab(gf, "Bench_Seat", -6f, 2f, 0.38f, 0.52f, 5.12f, 5.7f, cream);
        Slab(gf, "Bench_Back", -6f, 2f, 0.52f, 1.0f, 5.6f, 5.72f, cream, false);
        foreach (float x in new[] { -4.8f, -2.0f, 0.8f })
        {
            RoundTable(gf, "WallTable", new Vector3(x, 0, 4.5f), 0.65f, wood);
            Chair(gf, "WallTable_Chair", new Vector3(x, 0, 3.85f), 0f, woodDark, wood);
        }

        // counter
        Slab(gf, "Counter_Body", 4.6f, 5.4f, 0f, 1.0f, -2.0f, 4.0f, wood);
        Slab(gf, "Counter_Top", 4.5f, 5.5f, 1.0f, 1.06f, -2.1f, 4.1f, wall);
        for (int i = 0; i < 15; i++)
            Slab(gf, "Counter_Slat", 4.56f, 4.6f, 0.06f, 0.94f, -1.9f + i * 0.4f, -1.85f + i * 0.4f, woodDark, false);
        for (int i = 0; i < 6; i++)
            Stool(gf, "Stool_" + (i + 1), new Vector3(3.95f, 0, -1.3f + i * 0.95f));

        // on the counter
        Slab(gf, "CoffeeMachine", 4.8f, 5.3f, 1.06f, 1.5f, 0.6f, 1.2f, black);
        Slab(gf, "CoffeeMachine_Top", 4.8f, 5.3f, 1.5f, 1.55f, 0.6f, 1.2f, steel, false);
        Slab(gf, "Grinder", 4.85f, 5.2f, 1.06f, 1.45f, 1.5f, 1.9f, black, false);
        foreach (float dz in new[] { -1.3f, -0.5f })
        {
            Sph(gf, "PastryDome", new Vector3(5.0f, 1.06f, dz), Vector3.one * 0.42f, glass);
            Cyl(gf, "Cake", new Vector3(5.0f, 1.10f, dz), 0.22f, 0.08f, cream);
        }
        for (int i = 0; i < 4; i++)
            Cyl(gf, "Cup", new Vector3(4.9f, 1.105f, 2.6f + i * 0.28f), 0.08f, 0.09f, wall);

        // kitchen back-wall (right)
        Slab(gf, "BackSplash", 7.75f, 7.85f, 0.9f, 3.0f, -2.0f, 5.5f, tile, false);
        Slab(gf, "BackCabinet", 7.0f, 7.75f, 0f, 0.9f, 0.4f, 5.5f, steel);
        Slab(gf, "BackCounterTop", 6.95f, 7.8f, 0.9f, 0.95f, 0.4f, 5.5f, black, false);
        Slab(gf, "Fridge", 7.0f, 7.75f, 0f, 2.0f, -0.8f, 0.3f, steel);
        Slab(gf, "Oven_Tall", 7.0f, 7.75f, 0f, 2.4f, -2.0f, -0.9f, black);
        Slab(gf, "Oven_Window1", 6.97f, 7.0f, 0.7f, 1.3f, -1.8f, -1.1f, steel, false);
        Slab(gf, "Oven_Window2", 6.97f, 7.0f, 1.4f, 2.0f, -1.8f, -1.1f, steel, false);
        Slab(gf, "Espresso_Machine", 7.1f, 7.6f, 0.95f, 1.35f, 1.6f, 2.4f, black, false);
        Slab(gf, "Espresso_Top", 7.1f, 7.6f, 1.35f, 1.4f, 1.6f, 2.4f, steel, false);
        Slab(gf, "Shelf_1", 7.45f, 7.8f, 1.8f, 1.84f, 0.6f, 5.3f, wood, false);
        Slab(gf, "Shelf_2", 7.45f, 7.8f, 2.35f, 2.39f, 0.6f, 5.3f, wood, false);
        for (int i = 0; i < 10; i++)
        {
            Cyl(gf, "Jar", new Vector3(7.6f, 1.95f, 0.8f + i * 0.45f), 0.12f, 0.22f, i % 2 == 0 ? glass : cream);
            Slab(gf, "Pack", 7.5f, 7.7f, 2.39f, 2.55f, 0.8f + i * 0.45f, 0.95f + i * 0.45f, i % 3 == 0 ? red : cream, false);
        }

        // round tables with wooden chairs
        Vector3[] tp = { new Vector3(-4.2f, 0, -3.0f), new Vector3(-1.2f, 0, -3.0f), new Vector3(-4.2f, 0, 1.0f),
                         new Vector3(-1.2f, 0, 1.0f), new Vector3(1.9f, 0, 2.4f) };
        for (int i = 0; i < tp.Length; i++)
            TableSet(gf, "Table_" + (i + 1), tp[i], 4, 45f, wood, woodDark, wood, 0.9f);

        // plants
        Plant(gf, "Plant_1", new Vector3(-5.9f, 0, 3.2f), 1.1f);
        Plant(gf, "Plant_2", new Vector3(2.9f, 0, 5.0f), 1.1f);
        Plant(gf, "Plant_3", new Vector3(-2.7f, 0, -1.0f), 0.9f);
        Plant(gf, "Plant_4", new Vector3(0.6f, 0, -5.2f), 1.0f);
        Cactus(gf, "Cactus_1", new Vector3(-7.1f, 0, 5.0f), 1.0f);
        Cactus(gf, "Cactus_2", new Vector3(-6.7f, 0, 5.5f), 0.8f);
    }

    static void BuildUpperFurniture(Transform root)
    {
        Transform uf = Grp(root, "UpperFurniture", new Vector3(0, YU, 0));
        TableSet(uf, "UpperTable_1", new Vector3(-3.0f, 0, 3.2f), 3, 0f, red, black, wood, 0.9f);
        TableSet(uf, "UpperTable_2", new Vector3(1.0f, 0, 3.2f), 3, 0f, red, black, wood, 0.9f);
        TableSet(uf, "UpperTable_3", new Vector3(5.0f, 0, 3.2f), 4, 45f, red, black, wood, 0.9f);

        Slab(uf, "Sofa_Base", 3.0f - 10f + 3.0f, 3.0f - 10f + 6.0f, 0f, 0.4f, 5.0f, 5.7f, woodDark); // x -4..-1 (back wall)
        Slab(uf, "Sofa_Cushion", -4f, -1f, 0.4f, 0.55f, 5.02f, 5.7f, red);
        Slab(uf, "Sofa_Back", -4f, -1f, 0.55f, 1.0f, 5.55f, 5.7f, red, false);

        Plant(uf, "UpperPlant_1", new Vector3(7.0f, 0, 5.1f), 1.1f);
        Plant(uf, "UpperPlant_2", new Vector3(-7.1f, 0, 5.1f), 1.0f);
        Plant(uf, "UpperPlant_3", new Vector3(7.0f, 0, 1.0f), 0.9f);
    }

    static void BuildExterior(Transform root)
    {
        Transform e = Grp(root, "Exterior", Vector3.zero);
        Slab(e, "EntranceMat", 1.7f, 3.3f, 0f, 0.02f, -7.2f, -5.5f, navyDark, false);

        Slab(e, "Bench_Seat", -11.8f, -9.6f, 0.42f, 0.48f, -6.9f, -6.4f, woodDark);
        Slab(e, "Bench_Back", -11.8f, -9.6f, 0.48f, 1.0f, -6.45f, -6.4f, woodDark, false);
        foreach (float lx in new[] { -11.7f, -9.7f })
            Slab(e, "Bench_Leg", lx, lx + 0.08f, 0f, 0.42f, -6.9f, -6.4f, black, false);

        Topiary(e, "Topiary_1", new Vector3(-9.0f, 0, -7.0f), 1.0f);
        Topiary(e, "Topiary_2", new Vector3(-7.3f, 0, -7.0f), 0.85f);

        TableSet(e, "Outdoor_1", new Vector3(3.5f, 0, -9.5f), 2, 90f, orange, black, woodDark, 0.8f);
        TableSet(e, "Outdoor_2", new Vector3(6.8f, 0, -9.5f), 2, 90f, orange, black, woodDark, 0.8f);
        TableSet(e, "Outdoor_3", new Vector3(-3.0f, 0, -10.0f), 2, 90f, orange, black, woodDark, 0.8f);

        // lamp post
        Cyl(e, "LampPole", new Vector3(10f, 2.0f, -8.5f), 0.12f, 4.0f, black, true);
        foreach (Vector3 o in new[] { new Vector3(0, 4.25f, 0), new Vector3(-0.3f, 3.95f, 0), new Vector3(0.3f, 3.95f, 0) })
            Sph(e, "LampGlobe", new Vector3(10f, 0, -8.5f) + o, Vector3.one * 0.36f, lampM);
        PointLight(e, "LampPostLight", new Vector3(10f, 4.0f, -8.5f), new Color(1f, 0.8f, 0.5f), 4f, 9f);
    }

    static void BuildLights(Transform root)
    {
        Transform l = Grp(root, "Lights", Vector3.zero);
        foreach (float x in new[] { -2.5f, 1.0f, 4.5f })
            Pendant(l, "VoidPendant", new Vector3(x, 4.4f, -2.5f), 6.9f, true, 3.5f);
        Pendant(l, "TablePendant_1", new Vector3(-4.2f, 2.3f, 1.0f), 3.3f, true, 2.5f);
        Pendant(l, "TablePendant_2", new Vector3(-1.2f, 2.3f, 1.0f), 3.3f, true, 2.5f);
        Pendant(l, "TablePendant_3", new Vector3(1.9f, 2.3f, 2.4f), 3.3f, true, 2.5f);
        foreach (float x in new[] { -3f, 1f, 5f })
            Pendant(l, "UpperPendant", new Vector3(x, 5.8f, 3.2f), 6.9f, true, 3f);
        PointLight(l, "CounterLight", new Vector3(5.0f, 2.6f, 1.5f), new Color(1f, 0.82f, 0.55f), 2.5f, 7f);
        PointLight(l, "NeonSpill_L", new Vector3(-3f, 3.4f, -8.5f), new Color(1f, 0.45f, 0.1f), 5f, 10f);
        PointLight(l, "NeonSpill_R", new Vector3(4f, 3.4f, -8.5f), new Color(1f, 0.45f, 0.1f), 5f, 10f);
    }

    static void BuildMarkers(Transform root)
    {
        Transform m = Grp(root, "Markers", Vector3.zero);
        Grp(m, "PlayerSpawn", new Vector3(2.5f, 0.1f, -12f));
        Grp(m, "AgentSpawn", new Vector3(3.8f, 0.1f, -11f));
        Transform w = Grp(m, "Waypoints", Vector3.zero);
        Grp(w, "waypoint_entrance", new Vector3(2.5f, 0f, -4.8f));
        Grp(w, "waypoint_counter", new Vector3(3.2f, 0f, 1.0f));
        Grp(w, "waypoint_table_1", new Vector3(-2.7f, 0f, -3.0f));
        Grp(w, "waypoint_bench", new Vector3(-3.4f, 0f, 4.0f));
        Grp(w, "waypoint_stairs_bottom", new Vector3(-7.0f, 0f, -5.4f));
        Grp(w, "waypoint_upper_floor", new Vector3(-5.0f, YU, 1.5f));
        Grp(w, "waypoint_upper_table", new Vector3(1.0f, YU, 1.8f));
        Grp(w, "waypoint_outdoor_seating", new Vector3(5.0f, 0f, -8.0f));
    }

    static void SetupMood()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.17f, 0.26f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.006f;
        RenderSettings.fogColor = new Color(0.03f, 0.04f, 0.09f);

        GameObject dl = GameObject.Find("Directional Light");
        if (dl != null)
        {
            Light l = dl.GetComponent<Light>();
            if (l != null) { l.intensity = 0.35f; l.color = new Color(0.55f, 0.65f, 1f); }
            dl.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
        }
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.07f);
            cam.transform.position = new Vector3(-20f, 11f, -22f);
            cam.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 3.4f, 0f) - cam.transform.position, Vector3.up);
            cam.fieldOfView = 50f;
        }
    }

    // ---------------------------------------------------------------- furniture
    static void Chair(Transform p, string n, Vector3 pos, float yaw, Material frame, Material seat)
    {
        Transform g = Grp(p, n, pos);
        g.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Cyl(g, "Seat", new Vector3(0, 0.45f, 0), 0.42f, 0.04f, seat);
        foreach (float sx in new[] { -0.16f, 0.16f })
            foreach (float sz in new[] { -0.16f, 0.16f })
                Cyl(g, "Leg", new Vector3(sx, 0.215f, sz), 0.035f, 0.43f, frame);
        foreach (float sx in new[] { -0.16f, 0.16f })
            Cyl(g, "BackPost", new Vector3(sx, 0.68f, -0.18f), 0.03f, 0.46f, frame);
        Slab(g, "BackRail1", -0.18f, 0.18f, 0.78f, 0.86f, -0.2f, -0.16f, frame, false);
        Slab(g, "BackRail2", -0.18f, 0.18f, 0.62f, 0.68f, -0.2f, -0.16f, frame, false);
        AddBox(g.gameObject, new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.9f, 0.45f));
    }

    static void Stool(Transform p, string n, Vector3 pos)
    {
        Transform g = Grp(p, n, pos);
        Cyl(g, "Seat", new Vector3(0, 0.72f, 0), 0.36f, 0.05f, woodDark);
        foreach (float sx in new[] { -0.12f, 0.12f })
            foreach (float sz in new[] { -0.12f, 0.12f })
                Cyl(g, "Leg", new Vector3(sx, 0.35f, sz), 0.03f, 0.7f, black);
        Cyl(g, "Ring", new Vector3(0, 0.28f, 0), 0.3f, 0.02f, black);
        AddBox(g.gameObject, new Vector3(0, 0.4f, 0), new Vector3(0.38f, 0.8f, 0.38f));
    }

    static Transform RoundTable(Transform p, string n, Vector3 c, float dia, Material top)
    {
        Transform g = Grp(p, n, c);
        Cyl(g, "Top", new Vector3(0, 0.74f, 0), dia, 0.04f, top);
        Cyl(g, "Pole", new Vector3(0, 0.36f, 0), 0.07f, 0.72f, black);
        Cyl(g, "Base", new Vector3(0, 0.015f, 0), 0.45f, 0.03f, black);
        AddBox(g.gameObject, new Vector3(0, 0.37f, 0), new Vector3(dia, 0.76f, dia));
        return g;
    }

    static void TableSet(Transform p, string n, Vector3 c, int chairs, float a0, Material seat, Material frame, Material top, float dia)
    {
        RoundTable(p, n, c, dia, top);
        float r = dia / 2f + 0.30f;
        for (int i = 0; i < chairs; i++)
        {
            float a = (a0 + i * 360f / chairs) * Mathf.Deg2Rad;
            float dx = Mathf.Sin(a), dz = Mathf.Cos(a);
            Chair(p, n + "_Chair" + (i + 1), c + new Vector3(dx * r, 0, dz * r),
                  Mathf.Atan2(-dx, -dz) * Mathf.Rad2Deg, frame, seat);
        }
    }

    static void Plant(Transform p, string n, Vector3 pos, float s)
    {
        Transform g = Grp(p, n, pos);
        Cyl(g, "Pot", new Vector3(0, 0.25f * s, 0), 0.5f * s, 0.5f * s, pot);
        Cyl(g, "Stem", new Vector3(0, 0.75f * s, 0), 0.05f * s, 0.5f * s, green);
        for (int i = 0; i < 9; i++)
        {
            float a = i * 40f + rnd.Next(0, 20);
            float r = 0.12f + (float)rnd.NextDouble() * 0.18f;
            Vector3 lp = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * r,
                                     (0.75f + (float)rnd.NextDouble() * 0.55f) * s,
                                     Mathf.Cos(a * Mathf.Deg2Rad) * r);
            GameObject leaf = Sph(g, "Leaf", lp, new Vector3(0.45f * s, 0.06f * s, 0.3f * s), green);
            leaf.transform.localRotation = Quaternion.Euler(rnd.Next(-25, 25), a, rnd.Next(-25, 25));
        }
    }

    static void Cactus(Transform p, string n, Vector3 pos, float s)
    {
        Transform g = Grp(p, n, pos);
        Cyl(g, "Pot", new Vector3(0, 0.2f * s, 0), 0.4f * s, 0.4f * s, pot);
        Cyl(g, "Body", new Vector3(0, 1.1f * s, 0), 0.22f * s, 1.4f * s, green);
        GameObject arm = Cyl(g, "Arm", new Vector3(0.2f * s, 1.1f * s, 0), 0.12f * s, 0.4f * s, green);
        arm.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        Cyl(g, "ArmUp", new Vector3(0.38f * s, 1.35f * s, 0), 0.12f * s, 0.5f * s, green);
    }

    static void Topiary(Transform p, string n, Vector3 pos, float s)
    {
        Transform g = Grp(p, n, pos);
        Cyl(g, "Planter", new Vector3(0, 0.35f * s, 0), 0.8f * s, 0.7f * s, woodDark, true);
        Cyl(g, "Trunk", new Vector3(0, 0.9f * s, 0), 0.08f * s, 0.5f * s, woodDark);
        Sph(g, "Ball1", new Vector3(0, 1.3f * s, 0), Vector3.one * 0.75f * s, green);
        Sph(g, "Ball2", new Vector3(0, 1.85f * s, 0), Vector3.one * 0.5f * s, green);
    }

    static void Pendant(Transform p, string n, Vector3 lampPos, float ceilY, bool withLight, float intensity)
    {
        Transform g = Grp(p, n, Vector3.zero);
        float len = ceilY - lampPos.y - 0.15f;
        Cyl(g, "Cord", new Vector3(lampPos.x, lampPos.y + 0.15f + len / 2f, lampPos.z), 0.012f, len, black);
        Sph(g, "Bulb", lampPos, Vector3.one * 0.3f, lampM);
        if (withLight) PointLight(g, "Light", lampPos, new Color(1f, 0.82f, 0.55f), intensity, 9f);
    }

    // ---------------------------------------------------------------- primitives
    static Transform Grp(Transform p, string n, Vector3 pos)
    {
        GameObject g = new GameObject(n);
        g.transform.SetParent(p, false);
        g.transform.localPosition = pos;
        return g.transform;
    }

    static GameObject Slab(Transform p, string n, float x0, float x1, float y0, float y1, float z0, float z1, Material m, bool col = true)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n;
        g.transform.SetParent(p, false);
        g.transform.localPosition = new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, (z0 + z1) / 2f);
        g.transform.localScale = new Vector3(x1 - x0, y1 - y0, z1 - z0);
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (!col) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static GameObject Cyl(Transform p, string n, Vector3 c, float dia, float h, Material m, bool col = false)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = n;
        g.transform.SetParent(p, false);
        g.transform.localPosition = c;
        g.transform.localScale = new Vector3(dia, h / 2f, dia);
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (!col) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static GameObject Sph(Transform p, string n, Vector3 c, Vector3 size, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = n;
        g.transform.SetParent(p, false);
        g.transform.localPosition = c;
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static void ColliderBox(Transform p, string n, float x0, float x1, float y0, float y1, float z0, float z1)
    {
        GameObject g = Slab(p, n, x0, x1, y0, y1, z0, z1, black, true);
        Object.DestroyImmediate(g.GetComponent<MeshRenderer>());
        Object.DestroyImmediate(g.GetComponent<MeshFilter>());
    }

    static void AddBox(GameObject go, Vector3 center, Vector3 size)
    {
        BoxCollider b = go.AddComponent<BoxCollider>();
        b.center = center;
        b.size = size;
    }

    static Light PointLight(Transform p, string n, Vector3 pos, Color c, float intensity, float range)
    {
        GameObject g = new GameObject(n);
        g.transform.SetParent(p, false);
        g.transform.localPosition = pos;
        Light l = g.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        return l;
    }

    static void Label(Transform p, string n, Vector3 pos, string txt, float size, Color col)
    {
        GameObject g = new GameObject(n);
        g.transform.SetParent(p, false);
        g.transform.localPosition = pos;
        TMPro.TextMeshPro t = g.AddComponent<TMPro.TextMeshPro>();
        t.text = txt;
        t.fontSize = size;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.color = col;
        t.rectTransform.sizeDelta = new Vector2(10f, 2f);
    }
}

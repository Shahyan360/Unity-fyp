#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AncientRomeScenarioBuilder
{
    private const string RootName = "Ancient_Rome_Scenario";
    private static Material stone, trim, brick, roof, dark, road, water, sand, arena, gold;
    private static Material[] pavingBricks;

    [MenuItem("Tools/Embodied AI/Build Ancient Rome Scenario")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.path.EndsWith("/HistoricalPlace.unity"))
        {
            EditorUtility.DisplayDialog("Open HistoricalPlace", "Open Assets/Scenes/HistoricalPlace.unity, then run this builder again.", "OK");
            return;
        }

        GameObject old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);

        EnsureMaterialFolder();
        SetUpSceneMaterials();

        GameObject root = new GameObject(RootName);
        GameObject ground = GameObject.Find("RomanGround");
        if (ground != null)
        {
            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = sand;
        }

        BuildColosseum(root);
        BuildEntranceAndRoads(root);
        BuildTriumphalArch(root);
        BuildShops(root);
        BuildTemple(root, new Vector3(-25f, 0f, 14f));
        BuildTemple(root, new Vector3(25f, 0f, 14f));
        BuildRiver(root);
        BuildFountain(root);
        BuildFrontForumComplex(root);
        FrameScene();

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Ancient Rome scenario built and saved in HistoricalPlace.");
        Selection.activeGameObject = root;
    }

    [MenuItem("Tools/Embodied AI/Add Roman Forum Frontage")]
    public static void AddRomanForumFrontage()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.path.EndsWith("/HistoricalPlace.unity"))
        {
            EditorUtility.DisplayDialog("Open HistoricalPlace", "Open Assets/Scenes/HistoricalPlace.unity, then run this tool again.", "OK");
            return;
        }

        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Scenario not found", "Build or open the Ancient Rome scenario first, then run this tool again.", "OK");
            return;
        }

        if (root.transform.Find("Front_Forum_Complex") != null)
        {
            EditorUtility.DisplayDialog("Forum frontage already exists", "The Roman forum frontage is already in this scene.", "OK");
            Selection.activeGameObject = root.transform.Find("Front_Forum_Complex").gameObject;
            return;
        }

        EnsureMaterialFolder();
        SetUpSceneMaterials();
        GameObject frontage = BuildFrontForumComplex(root);
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = frontage;
        Debug.Log("Roman forum frontage added and saved in front of the Colosseum.");
    }

    private static void SetUpSceneMaterials()
    {
        stone = MakeMaterial("Roman_Travertine", new Color(0.76f, 0.69f, 0.55f));
        trim = MakeMaterial("Roman_Limestone", new Color(0.91f, 0.83f, 0.68f));
        brick = MakeMaterial("Roman_Ochre", new Color(0.70f, 0.43f, 0.27f));
        roof = MakeMaterial("Roman_Terracotta", new Color(0.66f, 0.25f, 0.15f));
        dark = MakeMaterial("Roman_Shadow", new Color(0.16f, 0.14f, 0.12f));
        road = MakeMaterial("Roman_Paving", new Color(0.54f, 0.48f, 0.38f));
        water = MakeMaterial("Roman_River", new Color(0.12f, 0.34f, 0.43f));
        SetUpDesertMaterials();
        arena = MakeMaterial("Roman_Arena_Sand", new Color(0.69f, 0.55f, 0.37f));
        gold = MakeMaterial("Roman_Decoration", new Color(0.76f, 0.59f, 0.29f));
    }

    [MenuItem("Tools/Embodied AI/Apply Desert Ground and Brick Paths")]
    public static void ApplyDesertGroundAndBrickPaths()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.path.EndsWith("/HistoricalPlace.unity"))
        {
            EditorUtility.DisplayDialog("Open HistoricalPlace", "Open Assets/Scenes/HistoricalPlace.unity, then run this tool again.", "OK");
            return;
        }

        GameObject ground = GameObject.Find("RomanGround");
        GameObject root = GameObject.Find(RootName);
        if (ground == null || root == null)
        {
            EditorUtility.DisplayDialog("Scenario not found", "Build or open the Ancient Rome scenario first, then run this tool again.", "OK");
            return;
        }

        EnsureMaterialFolder();
        SetUpDesertMaterials();
        Renderer groundRenderer = ground.GetComponent<Renderer>();
        if (groundRenderer != null) groundRenderer.sharedMaterial = sand;

        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
        {
            if (item.name.StartsWith("Cypress_Trunk_") || item.name.StartsWith("Cypress_Crown_"))
                item.gameObject.SetActive(false);
        }

        if (root.transform.Find("Roman_Brick_Paving") == null)
            BuildPaving(root.transform);

        EditorUtility.SetDirty(ground);
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Desert sand ground and Roman brick paths applied; trees hidden.");
        Selection.activeGameObject = root;
    }

    private static void SetUpDesertMaterials()
    {
        sand = MakeMaterial("Roman_Desert_Sand", new Color(0.73f, 0.59f, 0.38f));
        pavingBricks = new[]
        {
            MakeMaterial("Roman_Paving_Brick_1", new Color(0.55f, 0.31f, 0.22f)),
            MakeMaterial("Roman_Paving_Brick_2", new Color(0.64f, 0.38f, 0.26f)),
            MakeMaterial("Roman_Paving_Brick_3", new Color(0.70f, 0.44f, 0.30f))
        };
    }

    private static void EnsureMaterialFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/HistoricalPlace"))
            AssetDatabase.CreateFolder("Assets/Materials", "HistoricalPlace");
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = "Assets/Materials/HistoricalPlace/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject Make(PrimitiveType type, string name, Transform parent,
        Vector3 position, Vector3 scale, Material material, Quaternion rotation)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        obj.transform.localRotation = rotation;
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
        return obj;
    }

    private static GameObject Box(string name, Transform parent, Vector3 p, Vector3 s, Material m)
    {
        return Make(PrimitiveType.Cube, name, parent, p, s, m, Quaternion.identity);
    }

    private static GameObject Box(string name, Transform parent, Vector3 p, Vector3 s, Material m, Quaternion rotation)
    {
        return Make(PrimitiveType.Cube, name, parent, p, s, m, rotation);
    }

    private static GameObject Cylinder(string name, Transform parent, Vector3 p, Vector3 s, Material m)
    {
        return Make(PrimitiveType.Cylinder, name, parent, p, s, m, Quaternion.identity);
    }

    private static float YawFor(float angle, float rx, float rz)
    {
        float tx = -rx * Mathf.Sin(angle);
        float tz = rz * Mathf.Cos(angle);
        return Mathf.Atan2(-tz, tx) * Mathf.Rad2Deg;
    }

    private static void Ring(Transform parent, string label, float rx, float rz, float y,
        float height, float depth, int count, Material material)
    {
        float circumference = Mathf.PI * 2f * Mathf.Sqrt((rx * rx + rz * rz) * 0.5f);
        float length = circumference / count * 1.12f;
        for (int i = 0; i < count; i++)
        {
            float a = Mathf.PI * 2f * i / count;
            Vector3 p = new Vector3(rx * Mathf.Cos(a), y, rz * Mathf.Sin(a));
            Quaternion rot = Quaternion.Euler(0f, YawFor(a, rx, rz), 0f);
            Box(label + "_" + i.ToString("00"), parent, p, new Vector3(length, height, depth), material)
                .transform.localRotation = rot;
        }
    }

    private static void BuildColosseum(GameObject root)
    {
        Transform parent = root.transform;
        Ring(parent, "Foundation_Stone", 13.6f, 9.5f, 0.38f, 0.58f, 1.2f, 48, trim);
        Ring(parent, "Lower_Podium", 13.1f, 9.1f, 0.83f, 0.34f, 0.9f, 48, stone);

        for (int level = 0; level < 3; level++)
        {
            float rx = 12.8f - level * 0.42f;
            float rz = 8.9f - level * 0.30f;
            float baseY = 1.02f + level * 2.42f;
            float height = 2.15f;
            int count = 40;
            Ring(parent, "Story_" + (level + 1) + "_Base", rx, rz, baseY, 0.24f, 0.62f, count, trim);
            Ring(parent, "Story_" + (level + 1) + "_Cornice", rx, rz, baseY + height, 0.30f, 0.72f, count, stone);

            for (int i = 0; i < count; i++)
            {
                float a = Mathf.PI * 2f * i / count;
                float x = rx * Mathf.Cos(a);
                float z = rz * Mathf.Sin(a);
                if (z < -7.7f && Mathf.Abs(x) < 1.25f) continue;

                float yaw = YawFor(a, rx, rz);
                Vector3 column = new Vector3(x, baseY + height * 0.50f, z);
                Cylinder("Story_" + (level + 1) + "_Pillar_" + i.ToString("00"),
                    parent, column, new Vector3(0.25f, height * 0.42f, 0.25f), stone);

                Vector3 capital = new Vector3(x, baseY + height * 0.90f, z);
                Make(PrimitiveType.Cylinder, "Pillar_Capital", parent, capital,
                    new Vector3(0.38f, 0.10f, 0.38f), trim, Quaternion.identity);

                Vector3 inset = new Vector3(x * 1.012f, baseY + height * 0.52f, z * 1.012f);
                Make(PrimitiveType.Cube, "Arcade_Shadow_" + level + "_" + i, parent, inset,
                    new Vector3(0.43f, 0.90f, 0.12f), dark, Quaternion.Euler(0f, yaw, 0f));
            }
        }

        Ring(parent, "Attic_Cornice", 11.55f, 8.05f, 8.31f, 0.45f, 0.85f, 40, trim);
        for (int i = 0; i < 28; i++)
        {
            float a = Mathf.PI * 2f * i / 28f;
            Vector3 p = new Vector3(11.55f * Mathf.Cos(a), 8.86f, 8.05f * Mathf.Sin(a));
            Box("Attic_Crenellation_" + i.ToString("00"), parent, p,
                new Vector3(0.55f, 0.62f, 0.55f), stone);
        }

        Cylinder("Arena_Sand_Floor", parent, new Vector3(0f, 0.73f, 0f),
            new Vector3(5.2f, 0.16f, 3.8f), arena);

        for (int row = 0; row < 6; row++)
        {
            float rx = 5.9f + row * 0.88f;
            float rz = 4.0f + row * 0.57f;
            float y = 1.03f + row * 0.38f;
            Ring(parent, "Seating_Tier_" + (row + 1), rx, rz, y, 0.26f, 0.72f,
                40, row % 2 == 0 ? trim : stone);
        }
    }

    private static void BuildEntranceAndRoads(GameObject root)
    {
        Transform p = root.transform;
        Box("Via_Sacra_Approach", p, new Vector3(0f, 0.08f, -23f), new Vector3(7f, 0.16f, 27f), road);
        Box("West_Street", p, new Vector3(-21f, 0.07f, -11f), new Vector3(16f, 0.14f, 3.5f), road);
        Box("East_Street", p, new Vector3(21f, 0.07f, -11f), new Vector3(16f, 0.14f, 3.5f), road);
        BuildPaving(p);

        Box("Entrance_Pier_Left", p, new Vector3(-1.8f, 1.55f, -9.1f), new Vector3(0.8f, 2.5f, 0.8f), trim);
        Box("Entrance_Pier_Right", p, new Vector3(1.8f, 1.55f, -9.1f), new Vector3(0.8f, 2.5f, 0.8f), trim);
        Box("Entrance_Lintel", p, new Vector3(0f, 2.95f, -9.1f), new Vector3(4.2f, 0.55f, 1.0f), stone);
        Box("Entrance_Keystone", p, new Vector3(0f, 3.35f, -9.1f), new Vector3(1.0f, 0.45f, 1.05f), gold);
    }

    private static void BuildPaving(Transform parent)
    {
        GameObject paving = new GameObject("Roman_Brick_Paving");
        paving.transform.SetParent(parent, false);
        PaveRect(paving.transform, "Via_Sacra_Paver", new Vector3(0f, 0.18f, -23f), 7f, 27f, true);
        PaveRect(paving.transform, "West_Street_Paver", new Vector3(-21f, 0.16f, -11f), 16f, 3.5f, false);
        PaveRect(paving.transform, "East_Street_Paver", new Vector3(21f, 0.16f, -11f), 16f, 3.5f, false);
    }

    private static void PaveRect(Transform parent, string name, Vector3 center, float sizeX, float sizeZ, bool longAlongZ)
    {
        const float gap = 0.08f;
        const float thickness = 0.06f;
        float tileX = longAlongZ ? 1.20f : 0.72f;
        float tileZ = longAlongZ ? 0.72f : 1.20f;
        int columns = Mathf.Max(1, Mathf.FloorToInt((sizeX + gap) / (tileX + gap)));
        int rows = Mathf.Max(1, Mathf.FloorToInt((sizeZ + gap) / (tileZ + gap)));
        float usedX = columns * tileX + (columns - 1) * gap;
        float usedZ = rows * tileZ + (rows - 1) * gap;
        float startX = center.x - usedX * 0.5f + tileX * 0.5f;
        float startZ = center.z - usedZ * 0.5f + tileZ * 0.5f;
        System.Random random = new System.Random(longAlongZ ? 23 : 57);

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                float stagger = row % 2 == 0 ? 0f : (tileX + gap) * 0.5f;
                float x = startX + column * (tileX + gap) + stagger;
                if (x + tileX * 0.5f > center.x + sizeX * 0.5f) continue;
                float z = startZ + row * (tileZ + gap);
                GameObject tile = Box(name + "_R" + row + "_C" + column, parent,
                    new Vector3(x, center.y, z), new Vector3(tileX, thickness, tileZ),
                    pavingBricks[random.Next(pavingBricks.Length)]);
                Collider collider = tile.GetComponent<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);
            }
        }
    }

    private static void BuildTriumphalArch(GameObject root)
    {
        Transform p = root.transform;
        float x = 20f, z = -17f;
        Box("Triumphal_Arch_Left", p, new Vector3(x - 3f, 2.1f, z), new Vector3(1.35f, 4.2f, 1.8f), stone);
        Box("Triumphal_Arch_Right", p, new Vector3(x + 3f, 2.1f, z), new Vector3(1.35f, 4.2f, 1.8f), stone);
        Box("Triumphal_Arch_Main_Beam", p, new Vector3(x, 4.35f, z), new Vector3(8.0f, 1.15f, 2.0f), trim);
        Box("Triumphal_Arch_Top", p, new Vector3(x, 5.15f, z), new Vector3(6.6f, 0.42f, 1.8f), brick);
        Box("Archway_Shadow", p, new Vector3(x, 1.65f, z - 0.96f), new Vector3(3.7f, 2.9f, 0.08f), dark);
        Box("Arch_Inscription_Panel", p, new Vector3(x, 4.4f, z - 1.08f), new Vector3(2.6f, 0.48f, 0.08f), gold);
    }

    private static void BuildShops(GameObject root)
    {
        Shop(root.transform, "Forum_Shop_West", new Vector3(-25f, 0f, -19f), 7f, 5f, brick);
        Shop(root.transform, "Forum_Shop_East", new Vector3(27f, 0f, -19f), 7f, 5f, trim);
        Shop(root.transform, "Market_West", new Vector3(-31f, 0f, 1f), 6f, 5f, trim);
        Shop(root.transform, "Market_East", new Vector3(31f, 0f, 1f), 6f, 5f, brick);
        Shop(root.transform, "Market_Back_West", new Vector3(-17f, 0f, 22f), 7f, 5f, brick);
        Shop(root.transform, "Market_Back_East", new Vector3(17f, 0f, 22f), 7f, 5f, trim);
    }

    private static void Shop(Transform parent, string name, Vector3 center, float width, float depth, Material wall)
    {
        Box(name + "_Walls", parent, center + Vector3.up * 1.45f,
            new Vector3(width, 2.9f, depth), wall);
        Box(name + "_Front_Door", parent,
            new Vector3(center.x, 0.9f, center.z + depth * 0.505f),
            new Vector3(1.25f, 1.8f, 0.12f), dark);
        Box(name + "_Awning", parent,
            new Vector3(center.x, 2.45f, center.z + depth * 0.62f),
            new Vector3(width * 0.76f, 0.20f, 1.05f), roof);

        Vector3 roofPos = center + Vector3.up * 3.03f;
        Make(PrimitiveType.Cube, name + "_Roof_Front", parent,
            roofPos + new Vector3(0f, 0.10f, depth * 0.20f),
            new Vector3(width + 0.5f, 0.28f, depth * 0.66f), roof,
            Quaternion.Euler(22f, 0f, 0f));
        Make(PrimitiveType.Cube, name + "_Roof_Back", parent,
            roofPos + new Vector3(0f, 0.10f, -depth * 0.20f),
            new Vector3(width + 0.5f, 0.28f, depth * 0.66f), roof,
            Quaternion.Euler(-22f, 0f, 0f));
    }

    private static void BuildTemple(GameObject root, Vector3 center)
    {
        Transform p = root.transform;
        Box("Temple_Podium", p, center + new Vector3(0f, 0.42f, 0f), new Vector3(8f, 0.84f, 6f), trim);
        Box("Temple_Cell", p, center + new Vector3(0f, 2.25f, 0.55f), new Vector3(5.6f, 2.8f, 4.5f), brick);
        for (int i = 0; i < 6; i++)
        {
            float x = center.x - 3.2f + i * 1.28f;
            Cylinder("Temple_Front_Column", p, new Vector3(x, 2.05f, center.z - 2.0f),
                new Vector3(0.28f, 1.62f, 0.28f), trim);
            Cylinder("Temple_Column_Capital", p, new Vector3(x, 3.72f, center.z - 2.0f),
                new Vector3(0.43f, 0.12f, 0.43f), gold);
        }
        Box("Temple_Entablature", p, center + new Vector3(0f, 3.95f, -2.0f), new Vector3(8f, 0.52f, 1.1f), stone);
        Make(PrimitiveType.Cube, "Temple_Pediment", p, center + new Vector3(0f, 4.45f, -2.0f),
            new Vector3(7.6f, 0.55f, 1.15f), trim, Quaternion.Euler(0f, 0f, 0f));
    }

    private static GameObject BuildFrontForumComplex(GameObject root)
    {
        GameObject frontage = new GameObject("Front_Forum_Complex");
        frontage.transform.SetParent(root.transform, false);
        Transform p = frontage.transform;

        // A low forum terrace sits beneath the new frontage and joins the existing paved approach.
        Box("Forum_Front_Terrace", p, new Vector3(0f, 0.14f, -33f),
            new Vector3(57f, 0.28f, 10.4f), stone);
        Box("Forum_Front_Step", p, new Vector3(0f, 0.08f, -38.35f),
            new Vector3(52f, 0.16f, 0.45f), trim);

        BuildFrontTempleWing(p, "West_Temple_Wing", -21.25f);
        BuildFrontTempleWing(p, "East_Temple_Wing", 21.25f);
        BuildFrontPortico(p, "West_Colonnade", -10.35f);
        BuildFrontPortico(p, "East_Colonnade", 10.35f);
        BuildForumGateway(p);
        return frontage;
    }

    private static void BuildFrontTempleWing(Transform parent, string name, float x)
    {
        const float centerZ = -33f;
        const float frontZ = -37.05f;
        const float width = 11.8f;

        Box(name + "_Podium", parent, new Vector3(x, 0.55f, centerZ),
            new Vector3(width + 0.8f, 0.82f, 8.35f), trim);
        Box(name + "_Stone_Cell", parent, new Vector3(x, 2.64f, centerZ),
            new Vector3(width, 3.45f, 7.55f), stone);
        Box(name + "_Entry_Shadow", parent, new Vector3(x, 1.92f, frontZ - 0.03f),
            new Vector3(1.8f, 2.45f, 0.08f), dark);
        Box(name + "_Entry_Threshold", parent, new Vector3(x, 0.98f, frontZ - 0.23f),
            new Vector3(2.25f, 0.18f, 0.48f), trim);

        float[] offsets = { -4.45f, -2.23f, 2.23f, 4.45f };
        for (int i = 0; i < offsets.Length; i++)
            FrontColumn(parent, name + "_Porch_Column_" + (i + 1), x + offsets[i], frontZ - 0.38f, 0.86f, 3.70f, 0.96f);

        Box(name + "_Entablature", parent, new Vector3(x, 4.79f, frontZ - 0.38f),
            new Vector3(width + 1.2f, 0.62f, 1.18f), trim);
        Box(name + "_Upper_Cornice", parent, new Vector3(x, 5.13f, frontZ - 0.38f),
            new Vector3(width + 1.55f, 0.16f, 1.38f), stone);
        TriangularPrism(name + "_Pediment", parent, new Vector3(x, 5.21f, frontZ - 0.98f),
            width + 1.4f, 1.45f, 1.2f, trim);

        // Two terracotta roof planes rise to a ridge running along the temple wing.
        Make(PrimitiveType.Cube, name + "_Roof_South", parent,
            new Vector3(x, 5.88f, -35.15f), new Vector3(width + 1.0f, 0.24f, 4.45f), roof,
            Quaternion.Euler(-20f, 0f, 0f));
        Make(PrimitiveType.Cube, name + "_Roof_North", parent,
            new Vector3(x, 5.88f, -30.85f), new Vector3(width + 1.0f, 0.24f, 4.45f), roof,
            Quaternion.Euler(20f, 0f, 0f));
        Box(name + "_Roof_Ridge", parent, new Vector3(x, 6.60f, centerZ),
            new Vector3(width + 1.15f, 0.22f, 0.32f), brick);

        // Small pale pilasters and a darker inner opening add depth to the front wall.
        for (int side = -1; side <= 1; side += 2)
        {
            float px = x + side * 5.45f;
            Box(name + "_Corner_Pilaster", parent, new Vector3(px, 2.60f, frontZ - 0.12f),
                new Vector3(0.42f, 3.45f, 0.34f), trim);
            Box(name + "_Corner_Cap", parent, new Vector3(px, 4.42f, frontZ - 0.12f),
                new Vector3(0.72f, 0.22f, 0.5f), stone);
        }
    }

    private static void BuildFrontPortico(Transform parent, string name, float centerX)
    {
        const float span = 10.15f;
        const float frontZ = -37.0f;
        const float backZ = -29.2f;
        Box(name + "_Rear_Wall", parent, new Vector3(centerX, 2.25f, backZ),
            new Vector3(span, 4.2f, 0.42f), stone);
        Box(name + "_Rear_Wall_Cornice", parent, new Vector3(centerX, 4.44f, backZ),
            new Vector3(span + 0.5f, 0.28f, 0.72f), trim);

        for (int i = 0; i < 5; i++)
        {
            float x = centerX - 4.55f + i * 2.275f;
            FrontColumn(parent, name + "_Front_Column_" + (i + 1), x, frontZ, 0.76f, 4.05f, 0.28f);
            FrontColumn(parent, name + "_Inner_Column_" + (i + 1), x, -31.0f, 0.66f, 3.55f, 0.28f);
        }

        Box(name + "_Front_Entablature", parent, new Vector3(centerX, 4.62f, frontZ),
            new Vector3(span + 0.55f, 0.58f, 0.95f), trim);
        Box(name + "_Front_Cornice", parent, new Vector3(centerX, 4.96f, frontZ),
            new Vector3(span + 0.85f, 0.16f, 1.18f), stone);
        Box(name + "_Roof_South", parent, new Vector3(centerX, 5.18f, -35.0f),
            new Vector3(span + 0.9f, 0.22f, 4.4f), roof, Quaternion.Euler(-15f, 0f, 0f));
        Box(name + "_Roof_North", parent, new Vector3(centerX, 5.18f, -30.7f),
            new Vector3(span + 0.9f, 0.22f, 4.4f), roof, Quaternion.Euler(15f, 0f, 0f));
        Box(name + "_Roof_Ridge", parent, new Vector3(centerX, 5.76f, -32.85f),
            new Vector3(span + 1.0f, 0.22f, 0.30f), brick);
    }

    private static void BuildForumGateway(Transform parent)
    {
        const float z = -34.1f;
        const float pierX = 5.15f;
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * pierX;
            Box("Forum_Gateway_Pier", parent, new Vector3(x, 2.33f, z),
                new Vector3(0.92f, 4.66f, 1.15f), stone);
            Box("Forum_Gateway_Pier_Base", parent, new Vector3(x, 0.42f, z),
                new Vector3(1.25f, 0.34f, 1.42f), trim);
            Box("Forum_Gateway_Capital", parent, new Vector3(x, 4.68f, z),
                new Vector3(1.28f, 0.34f, 1.45f), trim);
        }

        Box("Forum_Gateway_Entablature", parent, new Vector3(0f, 4.92f, z),
            new Vector3(11.0f, 0.64f, 1.55f), trim);
        Box("Forum_Gateway_Cornice", parent, new Vector3(0f, 5.29f, z),
            new Vector3(11.5f, 0.16f, 1.72f), stone);
        TriangularPrism("Forum_Gateway_Pediment", parent, new Vector3(0f, 5.37f, z - 0.78f),
            11.4f, 1.65f, 1.55f, trim);
        Box("Forum_Gateway_Keystone", parent, new Vector3(0f, 5.53f, z - 0.82f),
            new Vector3(0.72f, 0.34f, 0.18f), gold);
    }

    private static void FrontColumn(Transform parent, string name, float x, float z, float radius, float height, float baseY)
    {
        Cylinder(name + "_Shaft", parent, new Vector3(x, baseY + height * 0.5f, z),
            new Vector3(radius, height * 0.5f, radius), trim);
        Cylinder(name + "_Base", parent, new Vector3(x, baseY + 0.07f, z),
            new Vector3(radius * 1.5f, 0.14f, radius * 1.5f), stone);
        Cylinder(name + "_Capital", parent, new Vector3(x, baseY + height + 0.12f, z),
            new Vector3(radius * 1.42f, 0.16f, radius * 1.42f), stone);
    }

    private static GameObject TriangularPrism(string name, Transform parent, Vector3 position,
        float width, float height, float depth, Material material)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;

        float half = width * 0.5f;
        Vector3[] vertices =
        {
            new Vector3(-half, 0f, 0f), new Vector3(half, 0f, 0f), new Vector3(0f, height, 0f),
            new Vector3(-half, 0f, depth), new Vector3(half, 0f, depth), new Vector3(0f, height, depth)
        };
        int[] triangles =
        {
            0, 2, 1, 3, 4, 5,
            0, 3, 5, 0, 5, 2,
            1, 2, 5, 1, 5, 4,
            0, 1, 4, 0, 4, 3
        };
        Mesh mesh = new Mesh { name = name + "_Mesh", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        return obj;
    }

    private static void BuildRiver(GameObject root)
    {
        Transform p = root.transform;
        Box("Tiber_River", p, new Vector3(0f, 0.04f, 36f), new Vector3(78f, 0.10f, 6.5f), water);
        Box("North_Bank", p, new Vector3(0f, 0.14f, 32.6f), new Vector3(78f, 0.28f, 0.55f), trim);
        Box("South_Bank", p, new Vector3(0f, 0.14f, 39.4f), new Vector3(78f, 0.28f, 0.55f), trim);
        Box("River_Pier", p, new Vector3(-8f, 0.30f, 29.7f), new Vector3(2.7f, 0.34f, 5.5f), road);
        Box("River_Boat_Hull", p, new Vector3(9f, 0.40f, 35.5f), new Vector3(5.8f, 0.55f, 1.5f), brick);
        Box("River_Boat_Cargo", p, new Vector3(9f, 0.82f, 35.5f), new Vector3(2.5f, 0.48f, 1.0f), gold);
    }

    private static void BuildFountain(GameObject root)
    {
        Transform p = root.transform;
        Cylinder("Forum_Fountain_Base", p, new Vector3(0f, 0.35f, -31f), new Vector3(2.7f, 0.35f, 2.7f), stone);
        Cylinder("Forum_Fountain_Bowl", p, new Vector3(0f, 0.85f, -31f), new Vector3(1.8f, 0.22f, 1.8f), trim);
        Cylinder("Forum_Fountain_Pillar", p, new Vector3(0f, 1.55f, -31f), new Vector3(0.28f, 0.72f, 0.28f), gold);
        Sphere("Forum_Fountain_Water", p, new Vector3(0f, 1.83f, -31f), new Vector3(0.75f, 0.15f, 0.75f), water);
    }

    private static void Sphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        Make(PrimitiveType.Sphere, name, parent, position, scale, material, Quaternion.identity);
    }

    private static void FrameScene()
    {
        Camera camera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }
        camera.transform.position = new Vector3(0f, 20f, -48f);
        camera.transform.LookAt(new Vector3(0f, 3.5f, 3f));
        camera.fieldOfView = 58f;

        Light sun = Object.FindAnyObjectByType<Light>();
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            sun.color = new Color(1f, 0.88f, 0.70f);
            sun.intensity = 1.35f;
        }
    }
}
#endif

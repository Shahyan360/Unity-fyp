#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RomanGroundUpgrade
{
    private const string ScenarioRoot = "Ancient_Rome_Scenario";
    private const string UpgradeRootName = "RomanGround_Upgrade";
    private const string BoundaryLakeRootName = "RomanGround_BoundaryLake";
    private const float TombX = 29f;
    private const float TombZ = 24f;

    private static Material sandstone;
    private static Material lightStone;
    private static Material sandstoneCourse;
    private static Material bronze;
    private static Material shadow;
    private static Material camelCoat;
    private static Material horseCoat;
    private static Material darkLeather;
    private static Material saddleCloth;

    [MenuItem("Tools/Roman Ground/Add Boundary Lake (Keep Front Clear)")]
    public static void AddBoundaryLake()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.path.EndsWith("/HistoricalPlace.unity"))
        {
            EditorUtility.DisplayDialog("Open HistoricalPlace", "Open Assets/Scenes/HistoricalPlace.unity, then run this lake tool again.", "OK");
            return;
        }

        GameObject scenario = GameObject.Find(ScenarioRoot);
        if (scenario == null)
        {
            EditorUtility.DisplayDialog("Roman scene not found", "The HistoricalPlace scene needs Ancient_Rome_Scenario before the boundary lake can be added.", "OK");
            return;
        }

        Transform existing = scenario.transform.Find(BoundaryLakeRootName);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog("Boundary lake already added", "The side lakes are already in the scene. The front approach remains open.", "OK");
            return;
        }

        Renderer riverRenderer = GameObject.Find("Tiber_River")?.GetComponent<Renderer>();
        Material water = riverRenderer != null ? riverRenderer.sharedMaterial : null;
        if (water == null)
            water = GetMaterial("RomanGround_BoundaryWater", new Color(0.06f, 0.28f, 0.36f), 0.72f);

        Renderer groundRenderer = GameObject.Find("RomanGround")?.GetComponent<Renderer>();
        Material shoreSand = groundRenderer != null ? groundRenderer.sharedMaterial : null;
        if (shoreSand == null)
            shoreSand = GetMaterial("RomanGround_LakeShore", new Color(0.72f, 0.56f, 0.36f), 0.06f);

        GameObject lakeRoot = new GameObject(BoundaryLakeRootName);
        lakeRoot.transform.SetParent(scenario.transform, false);

        // The existing Tiber already forms the back edge. These two channels join it
        // at z=33 and run to the front corners, leaving the full front edge open.
        const float sideLength = 73f;
        const float sideCenterZ = -3.5f;
        const float channelOffsetX = 42.25f;
        const float channelWidth = 7f;
        const float channelDepth = 0.10f;
        Box("Boundary_Lake_Left", lakeRoot.transform,
            new Vector3(-channelOffsetX, 0.04f, sideCenterZ),
            new Vector3(channelWidth, channelDepth, sideLength), water, Quaternion.identity, false);
        Box("Boundary_Lake_Right", lakeRoot.transform,
            new Vector3(channelOffsetX, 0.04f, sideCenterZ),
            new Vector3(channelWidth, channelDepth, sideLength), water, Quaternion.identity, false);

        // Low sand lips mark the shore at the land edge without blocking the route in.
        Box("Boundary_Lake_Left_Shore", lakeRoot.transform,
            new Vector3(-39.7f, 0.10f, sideCenterZ),
            new Vector3(0.60f, 0.20f, sideLength), shoreSand, Quaternion.identity, false);
        Box("Boundary_Lake_Right_Shore", lakeRoot.transform,
            new Vector3(39.7f, 0.10f, sideCenterZ),
            new Vector3(0.60f, 0.20f, sideLength), shoreSand, Quaternion.identity, false);

        EditorUtility.SetDirty(scenario);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = lakeRoot;
        Debug.Log("Boundary lake added along both sides and joined to the Tiber back edge; the front remains open.");
    }

    [MenuItem("Tools/Roman Ground/Upgrade Tomb, Camels and Horses")]
    public static void Upgrade()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.path.EndsWith("/HistoricalPlace.unity"))
        {
            EditorUtility.DisplayDialog("Open HistoricalPlace", "Open Assets/Scenes/HistoricalPlace.unity, then run this upgrade tool again.", "OK");
            return;
        }

        GameObject scenario = GameObject.Find(ScenarioRoot);
        if (scenario == null)
        {
            EditorUtility.DisplayDialog("Roman scene not found", "Build or open the Ancient Rome scenario first, then run this upgrade tool again.", "OK");
            return;
        }

        Transform old = scenario.transform.Find(UpgradeRootName);
        if (old != null)
        {
            Selection.activeGameObject = old.gameObject;
            EditorUtility.DisplayDialog("Roman ground already upgraded", "The Pharaoh tomb, camels and horses are already in the scene. The existing upgrade was left in place.", "OK");
            return;
        }

        EnsureMaterialFolder();
        CreateMaterials();
        GameObject upgrade = new GameObject(UpgradeRootName);
        upgrade.transform.SetParent(scenario.transform, false);

        BuildPharaohTomb(upgrade.transform);
        BuildCamel(upgrade.transform, "Camel_01", new Vector3(-34f, 0f, -9f), 18f);
        BuildCamel(upgrade.transform, "Camel_02", new Vector3(-34f, 0f, 22f), -12f);
        BuildHorse(upgrade.transform, "Horse_01", new Vector3(34f, 0f, 8f), -18f);
        BuildHorse(upgrade.transform, "Horse_02", new Vector3(18f, 0f, 29f), 160f);

        EditorUtility.SetDirty(scenario);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = upgrade;
        Debug.Log("Roman ground upgraded with a Pharaoh-era tomb, two camels and two horses.");
    }

    private static void EnsureMaterialFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/HistoricalPlace"))
            AssetDatabase.CreateFolder("Assets/Materials", "HistoricalPlace");
    }

    private static void CreateMaterials()
    {
        sandstone = GetMaterial("RomanGround_PharaohSandstone", new Color(0.66f, 0.47f, 0.30f), 0.05f);
        lightStone = GetMaterial("RomanGround_TombLimestone", new Color(0.84f, 0.69f, 0.49f), 0.08f);
        sandstoneCourse = GetMaterial("RomanGround_TombCourses", new Color(0.57f, 0.38f, 0.23f), 0.04f);
        bronze = GetMaterial("RomanGround_EgyptianBronze", new Color(0.68f, 0.43f, 0.18f), 0.28f);
        shadow = GetMaterial("RomanGround_TombShadow", new Color(0.095f, 0.061f, 0.037f), 0.02f);
        camelCoat = GetMaterial("RomanGround_CamelCoat", new Color(0.61f, 0.40f, 0.23f), 0.06f);
        horseCoat = GetMaterial("RomanGround_HorseCoat", new Color(0.31f, 0.14f, 0.08f), 0.10f);
        darkLeather = GetMaterial("RomanGround_AnimalLeather", new Color(0.16f, 0.09f, 0.052f), 0.12f);
        saddleCloth = GetMaterial("RomanGround_SaddleCloth", new Color(0.50f, 0.13f, 0.075f), 0.14f);
    }

    private static Material GetMaterial(string name, Color color, float smoothness)
    {
        string path = "Assets/Materials/HistoricalPlace/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        else material.color = color;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void BuildPharaohTomb(Transform parent)
    {
        GameObject tomb = new GameObject("Pharaoh_Tomb_Complex");
        tomb.transform.SetParent(parent, false);
        tomb.transform.localPosition = new Vector3(TombX, 0f, TombZ);
        Transform p = tomb.transform;

        // Raised stone foundation and layered entrance apron.
        Box("Tomb_Foundation", p, new Vector3(0f, 0.24f, 0f), new Vector3(16f, 0.48f, 14f), sandstone);
        Box("Tomb_Foundation_Coping", p, new Vector3(0f, 0.52f, 0f), new Vector3(15.5f, 0.12f, 13.5f), lightStone);
        Box("Tomb_Front_Apron", p, new Vector3(0f, 0.31f, -7.35f), new Vector3(7f, 0.18f, 1.0f), sandstoneCourse);

        const float baseY = 0.58f;
        const float pyramidWidth = 12.5f;
        const float pyramidDepth = 11f;
        const float pyramidHeight = 8.55f;
        CreatePyramid("Pharaoh_Tomb_Pyramid", p, Vector3.zero, baseY,
            pyramidWidth, pyramidDepth, pyramidHeight, sandstone);
        AddPyramidCourses(p, baseY, pyramidWidth, pyramidDepth, pyramidHeight);
        BuildTombEntrance(p, baseY, pyramidDepth, pyramidHeight);
        BuildObelisk(p, "West_Obelisk", -7.05f, -5.4f);
        BuildObelisk(p, "East_Obelisk", 7.05f, -5.4f);
        BuildSphinx(p, "West_Sphinx", -3.35f);
        BuildSphinx(p, "East_Sphinx", 3.35f);

        Box("Tomb_Inscription_Stela", p, new Vector3(0f, 1.05f, 6.35f),
            new Vector3(1.2f, 1.05f, 0.22f), lightStone);
        Box("Stela_Sun_Symbol", p, new Vector3(0f, 1.32f, 6.19f),
            new Vector3(0.38f, 0.26f, 0.06f), bronze, Quaternion.identity, false);
    }

    private static void CreatePyramid(string name, Transform parent, Vector3 center, float baseY,
        float width, float depth, float height, Material material)
    {
        float hx = width * 0.5f;
        float hz = depth * 0.5f;
        Vector3[] vertices =
        {
            new Vector3(-hx, baseY, -hz), new Vector3(hx, baseY, -hz),
            new Vector3(hx, baseY, hz), new Vector3(-hx, baseY, hz),
            new Vector3(0f, baseY + height, 0f)
        };
        int[] triangles =
        {
            0, 4, 1, 1, 4, 2, 2, 4, 3, 3, 4, 0,
            0, 1, 2, 0, 2, 3
        };
        Mesh mesh = new Mesh { name = name + "_Mesh", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject pyramid = new GameObject(name);
        pyramid.transform.SetParent(parent, false);
        pyramid.transform.localPosition = center;
        pyramid.AddComponent<MeshFilter>().sharedMesh = mesh;
        pyramid.AddComponent<MeshRenderer>().sharedMaterial = material;
        pyramid.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    private static void AddPyramidCourses(Transform parent, float baseY,
        float width, float depth, float height)
    {
        float frontSlope = Mathf.Atan((depth * 0.5f) / height) * Mathf.Rad2Deg;
        for (int row = 1; row <= 8; row++)
        {
            float t = row / 9f;
            float y = baseY + height * t;
            float rowWidth = width * (1f - t);
            float rowDepth = depth * (1f - t);
            if (rowWidth < 0.8f) continue;

            float frontZ = -depth * 0.5f * (1f - t) - 0.035f;
            float backZ = depth * 0.5f * (1f - t) + 0.035f;
            Cube("Tomb_Front_Course_" + row, parent, new Vector3(0f, y, frontZ),
                new Vector3(rowWidth + 0.12f, 0.075f, 0.08f), sandstoneCourse,
                Quaternion.Euler(frontSlope, 0f, 0f), false);
            Cube("Tomb_Back_Course_" + row, parent, new Vector3(0f, y, backZ),
                new Vector3(rowWidth + 0.12f, 0.075f, 0.08f), sandstoneCourse,
                Quaternion.Euler(-frontSlope, 0f, 0f), false);

            float sideX = width * 0.5f * (1f - t) + 0.025f;
            Cube("Tomb_Left_Course_" + row, parent, new Vector3(-sideX, y, 0f),
                new Vector3(rowDepth + 0.12f, 0.075f, 0.08f), sandstoneCourse,
                Quaternion.Euler(0f, 90f, -frontSlope), false);
            Cube("Tomb_Right_Course_" + row, parent, new Vector3(sideX, y, 0f),
                new Vector3(rowDepth + 0.12f, 0.075f, 0.08f), sandstoneCourse,
                Quaternion.Euler(0f, 90f, frontSlope), false);
        }
    }

    private static void BuildTombEntrance(Transform parent, float baseY, float depth, float height)
    {
        float facadeZ = -depth * 0.5f - 0.43f;
        float yBottom = baseY + 0.04f;
        float doorHeight = 2.45f;
        float doorCenterY = yBottom + doorHeight * 0.5f;

        // Temple-like pylon projects from the sloped pyramid face and frames a recessed dark doorway.
        Box("Tomb_Pylon_Left", parent, new Vector3(-1.28f, yBottom + 1.55f, facadeZ + 0.38f),
            new Vector3(0.58f, 3.1f, 1.25f), lightStone);
        Box("Tomb_Pylon_Right", parent, new Vector3(1.28f, yBottom + 1.55f, facadeZ + 0.38f),
            new Vector3(0.58f, 3.1f, 1.25f), lightStone);
        Box("Tomb_Door_Lintel", parent, new Vector3(0f, yBottom + 3.22f, facadeZ + 0.38f),
            new Vector3(3.15f, 0.48f, 1.25f), sandstoneCourse);
        Box("Tomb_Door_Threshold", parent, new Vector3(0f, yBottom - 0.01f, facadeZ - 0.05f),
            new Vector3(2.65f, 0.16f, 1.7f), lightStone);
        Box("Tomb_Door_Shadow", parent, new Vector3(0f, doorCenterY, facadeZ - 0.14f),
            new Vector3(1.52f, doorHeight, 0.12f), shadow);
        Box("Tomb_Golden_Door_Inset", parent, new Vector3(0f, doorCenterY, facadeZ - 0.215f),
            new Vector3(0.16f, 1.65f, 0.035f), bronze, Quaternion.identity, false);
        Sphere("Tomb_Sun_Disk", parent, new Vector3(0f, yBottom + 3.72f, facadeZ - 0.18f),
            new Vector3(0.50f, 0.50f, 0.12f), bronze, false);

        for (int i = 0; i < 5; i++)
        {
            float z = -7.02f + i * 0.48f;
            float heightStep = 0.18f + i * 0.12f;
            Box("Tomb_Entrance_Step_" + (i + 1), parent,
                new Vector3(0f, 0.50f + heightStep * 0.5f, z),
                new Vector3(4.25f, heightStep, 0.55f), sandstoneCourse);
        }

        // Repeating sun and reed-like marks evoke carved Egyptian reliefs without a flat decal.
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 1.28f;
            for (int glyph = 0; glyph < 4; glyph++)
            {
                float y = yBottom + 0.62f + glyph * 0.57f;
                Sphere("Tomb_Relief_Sun_" + side + "_" + glyph, parent,
                    new Vector3(x, y, facadeZ - 0.27f), new Vector3(0.12f, 0.12f, 0.035f), bronze, false);
                Box("Tomb_Relief_Stroke_" + side + "_" + glyph, parent,
                    new Vector3(x + side * 0.23f, y, facadeZ - 0.27f),
                    new Vector3(0.07f, 0.27f, 0.035f), bronze, Quaternion.identity, false);
            }
        }
    }

    private static void BuildObelisk(Transform parent, string name, float x, float z)
    {
        Cylinder(name + "_Base", parent, new Vector3(x, 0.70f, z), new Vector3(1.05f, 0.20f, 1.05f), sandstoneCourse);
        Cylinder(name + "_Shaft", parent, new Vector3(x, 2.35f, z), new Vector3(0.68f, 1.45f, 0.68f), lightStone);
        Box(name + "_Shaft_Face", parent, new Vector3(x, 2.4f, z - 0.35f),
            new Vector3(0.20f, 2.5f, 0.035f), bronze, Quaternion.identity, false);
        // Keep the cap's mesh base at the shaft top in the parent's local space.
        CreatePyramid(name + "_Pyramidion", parent, new Vector3(x, 0f, z),
            3.80f, 0.75f, 0.75f, 0.82f, bronze);
    }

    private static void BuildSphinx(Transform parent, string name, float x)
    {
        GameObject sphinx = new GameObject(name);
        sphinx.transform.SetParent(parent, false);
        sphinx.transform.localPosition = new Vector3(x, 0.52f, -5.75f);
        sphinx.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Transform p = sphinx.transform;
        Sphere("Sphinx_Body", p, new Vector3(0f, 0.42f, 0f), new Vector3(0.88f, 0.62f, 1.7f), sandstone, false);
        Sphere("Sphinx_Head", p, new Vector3(0f, 0.98f, 0.70f), new Vector3(0.55f, 0.69f, 0.56f), lightStone, false);
        Box("Sphinx_Headdress", p, new Vector3(0f, 1.25f, 0.67f), new Vector3(0.82f, 0.20f, 0.60f), bronze, Quaternion.identity, false);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Sphinx_Forepaw", p, new Vector3(side * 0.28f, 0.17f, 0.72f),
                new Vector3(0.30f, 0.30f, 1.05f), lightStone, Quaternion.identity, false);
            Sphere("Sphinx_Eye", p, new Vector3(side * 0.20f, 1.04f, 0.95f),
                new Vector3(0.09f, 0.08f, 0.05f), shadow, false);
        }
    }

    private static void BuildCamel(Transform parent, string name, Vector3 position, float yaw)
    {
        GameObject camel = NewAnimal(parent, name, position, yaw);
        Transform p = camel.transform;
        Sphere("Camel_Body", p, new Vector3(0f, 1.22f, 0f), new Vector3(0.92f, 0.82f, 2.15f), camelCoat, false);
        Sphere("Camel_Hump", p, new Vector3(0f, 1.89f, -0.10f), new Vector3(0.68f, 0.78f, 0.78f), camelCoat, false);
        AddSegment("Camel_Neck", p, new Vector3(0f, 1.36f, 0.73f), new Vector3(0f, 2.28f, 1.17f), 0.20f, camelCoat);
        Sphere("Camel_Head", p, new Vector3(0f, 2.34f, 1.28f), new Vector3(0.48f, 0.44f, 0.58f), camelCoat, false);
        Sphere("Camel_Muzzle", p, new Vector3(0f, 2.17f, 1.58f), new Vector3(0.40f, 0.25f, 0.36f), lightStone, false);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Camel_Ear", p, new Vector3(side * 0.22f, 2.62f, 1.20f),
                new Vector3(0.14f, 0.30f, 0.13f), camelCoat, Quaternion.Euler(12f, 0f, side * -10f), false);
            Sphere("Camel_Eye", p, new Vector3(side * 0.23f, 2.42f, 1.53f),
                new Vector3(0.07f, 0.07f, 0.05f), shadow, false);
        }
        Sphere("Camel_Tail", p, new Vector3(0f, 0.86f, -1.16f), new Vector3(0.10f, 0.44f, 0.13f), darkLeather, false);
        Box("Camel_Saddle_Rug", p, new Vector3(0f, 1.59f, -0.12f),
            new Vector3(0.98f, 0.15f, 1.35f), saddleCloth, Quaternion.identity, false);
        for (int side = -1; side <= 1; side += 2)
            Box("Camel_Pack", p, new Vector3(side * 0.43f, 1.62f, -0.20f),
                new Vector3(0.30f, 0.48f, 0.54f), sandstoneCourse, Quaternion.identity, false);

        for (int side = -1; side <= 1; side += 2)
            for (int foreAft = -1; foreAft <= 1; foreAft += 2)
                AddLeg(p, "Camel_Leg_" + side + "_" + foreAft, side * 0.34f, foreAft * 0.65f,
                    1.05f, 0.58f, 0.12f, 0.085f, camelCoat, darkLeather);
    }

    private static void BuildHorse(Transform parent, string name, Vector3 position, float yaw)
    {
        GameObject horse = NewAnimal(parent, name, position, yaw);
        Transform p = horse.transform;
        Sphere("Horse_Body", p, new Vector3(0f, 1.12f, 0f), new Vector3(0.78f, 0.76f, 1.72f), horseCoat, false);
        Sphere("Horse_Shoulder", p, new Vector3(0f, 1.26f, 0.48f), new Vector3(0.78f, 0.82f, 0.78f), horseCoat, false);
        Sphere("Horse_Rump", p, new Vector3(0f, 1.25f, -0.50f), new Vector3(0.82f, 0.80f, 0.86f), horseCoat, false);
        AddSegment("Horse_Neck", p, new Vector3(0f, 1.28f, 0.54f), new Vector3(0f, 2.02f, 1.07f), 0.27f, horseCoat);
        Sphere("Horse_Head", p, new Vector3(0f, 2.03f, 1.17f), new Vector3(0.42f, 0.57f, 0.63f), horseCoat, false);
        Sphere("Horse_Muzzle", p, new Vector3(0f, 1.78f, 1.50f), new Vector3(0.36f, 0.27f, 0.42f), camelCoat, false);
        AddSegment("Horse_Mane", p, new Vector3(0f, 2.22f, 0.72f), new Vector3(0f, 2.20f, 1.35f), 0.11f, darkLeather);
        AddSegment("Horse_Tail", p, new Vector3(0f, 1.24f, -1.22f), new Vector3(0f, 0.62f, -1.58f), 0.085f, darkLeather);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Horse_Ear", p, new Vector3(side * 0.16f, 2.46f, 1.12f),
                new Vector3(0.11f, 0.30f, 0.14f), darkLeather, Quaternion.Euler(-12f, 0f, side * -8f), false);
            Sphere("Horse_Eye", p, new Vector3(side * 0.20f, 2.12f, 1.43f),
                new Vector3(0.07f, 0.07f, 0.05f), shadow, false);
        }
        Box("Horse_Saddle_Pad", p, new Vector3(0f, 1.58f, -0.05f),
            new Vector3(0.88f, 0.15f, 0.96f), saddleCloth, Quaternion.identity, false);
        Box("Horse_Saddle", p, new Vector3(0f, 1.70f, -0.05f),
            new Vector3(0.55f, 0.16f, 0.62f), darkLeather, Quaternion.identity, false);

        for (int side = -1; side <= 1; side += 2)
            for (int foreAft = -1; foreAft <= 1; foreAft += 2)
                AddLeg(p, "Horse_Leg_" + side + "_" + foreAft, side * 0.30f, foreAft * 0.57f,
                    0.98f, 0.53f, 0.095f, 0.07f, horseCoat, darkLeather);
    }

    private static void AddLeg(Transform parent, string name, float x, float z,
        float hipY, float kneeY, float upperRadius, float lowerRadius, Material coat, Material hoof)
    {
        Vector3 hip = new Vector3(x, hipY, z);
        Vector3 knee = new Vector3(x * 1.06f, kneeY, z + (z > 0f ? 0.05f : -0.04f));
        Vector3 foot = new Vector3(x * 1.08f, 0.13f, knee.z + (z > 0f ? 0.10f : -0.04f));
        AddSegment(name + "_Upper", parent, hip, knee, upperRadius, coat);
        Sphere(name + "_Joint", parent, knee, Vector3.one * upperRadius * 1.45f, coat, false);
        AddSegment(name + "_Lower", parent, knee, foot, lowerRadius, coat);
        Box(name + "_Hoof", parent, new Vector3(foot.x, 0.10f, foot.z + 0.04f),
            new Vector3(lowerRadius * 2.1f, 0.18f, 0.28f), hoof, Quaternion.identity, false);
    }

    private static GameObject NewAnimal(Transform parent, string name, Vector3 position, float yaw)
    {
        GameObject animal = new GameObject(name);
        animal.transform.SetParent(parent, false);
        animal.transform.localPosition = position;
        animal.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return animal;
    }

    private static void AddSegment(string name, Transform parent, Vector3 start, Vector3 end,
        float radius, Material material)
    {
        Vector3 direction = end - start;
        if (direction.sqrMagnitude < 0.0001f) return;
        GameObject segment = Primitive(PrimitiveType.Cylinder, name, parent,
            (start + end) * 0.5f, new Vector3(radius, direction.magnitude * 0.5f, radius), material, false);
        segment.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
    }

    private static void Box(string name, Transform parent, Vector3 position, Vector3 scale,
        Material material, Quaternion rotation = default, bool collider = true)
    {
        if (rotation == default) rotation = Quaternion.identity;
        Primitive(PrimitiveType.Cube, name, parent, position, scale, material, collider).transform.localRotation = rotation;
    }

    private static void Cube(string name, Transform parent, Vector3 position, Vector3 scale,
        Material material, Quaternion rotation, bool collider)
    {
        Primitive(PrimitiveType.Cube, name, parent, position, scale, material, collider).transform.localRotation = rotation;
    }

    private static void Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        Primitive(PrimitiveType.Cylinder, name, parent, position, scale, material, true);
    }

    private static void Sphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider = true)
    {
        Primitive(PrimitiveType.Sphere, name, parent, position, scale, material, collider);
    }

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent,
        Vector3 position, Vector3 scale, Material material, bool collider)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        if (!collider)
        {
            Collider existing = obj.GetComponent<Collider>();
            if (existing != null) Object.DestroyImmediate(existing);
        }
        return obj;
    }
}
#endif

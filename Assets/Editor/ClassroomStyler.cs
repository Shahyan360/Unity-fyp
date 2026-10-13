using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ClassroomStyler
{
    const string RootName = "Classroom_Style";

    // ---- Tweak these and run again if needed ----
    const float LightIntensity = 3f;
    static readonly Color Ambient = new Color(0.30f, 0.33f, 0.55f);
    const float FogDensity = 0.015f;

    [MenuItem("Tools/Style Classroom (Dark Blue)")]
    static void Style()
    {
        GameObject env = GameObject.Find("Environment");
        GameObject props = GameObject.Find("props");
        if (env == null) { Debug.LogError("No 'Environment' object. Open the Classroom scene first."); return; }

        // Clean up anything from earlier runs / older scripts
        GameObject old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);
        if (props != null)
            for (int i = props.transform.childCount - 1; i >= 0; i--)
            {
                string n = props.transform.GetChild(i).name;
                if (n.StartsWith("Student_Desk") || n.StartsWith("Student_Chair") || n.StartsWith("Book_On_Desk"))
                    Object.DestroyImmediate(props.transform.GetChild(i).gameObject);
            }

        // ---- Materials ----
        Material wallM  = GetMat("Wall", new Color(0.76f, 0.68f, 0.53f), 0.25f);
        Material floorM = GetMat("Floor", new Color(0.80f, 0.74f, 0.64f), 0.45f);
        Material ceilM  = GetMat("CeilingDark", new Color(0.15f, 0.15f, 0.18f), 0.2f);
        Material slatM  = GetMat("CeilingSlat", new Color(0.85f, 0.85f, 0.88f), 0.3f);
        Material tableM = GetMat("Table", new Color(0.17f, 0.15f, 0.52f), 0.5f);
        Material chairM = GetMat("Chair", new Color(0.22f, 0.22f, 0.25f), 0.5f, 0.5f);
        Material woodM  = GetMat("Wood", new Color(0.30f, 0.17f, 0.09f), 0.35f);
        Material boardM = GetMat("Chalkboard", new Color(0.16f, 0.18f, 0.18f), 0.15f);
        Material winM   = GetMat("Window", new Color(0.75f, 0.88f, 1f), 0.6f, 0f, 1.6f);
        Material lampM  = GetMat("Lamp", new Color(1f, 0.95f, 0.8f), 0.2f, 0f, 2.5f);
        Material darkM  = GetMat("Dark", new Color(0.05f, 0.05f, 0.06f), 0.3f);
        Material[] bookM =
        {
            GetMat("BookRed",    new Color(0.70f, 0.10f, 0.10f)),
            GetMat("BookGreen",  new Color(0.10f, 0.50f, 0.20f)),
            GetMat("BookYellow", new Color(0.85f, 0.75f, 0.10f)),
            GetMat("BookTeal",   new Color(0.10f, 0.55f, 0.60f)),
            GetMat("BookBlue",   new Color(0.15f, 0.25f, 0.70f))
        };

        // ---- Walls and floor colors ----
        foreach (Transform t in env.transform)
        {
            Renderer r = t.GetComponent<Renderer>();
            if (r == null) continue;
            if (t.name.StartsWith("Wall_") || t.name.StartsWith("Door_")) r.sharedMaterial = wallM;
            else if (t.name.StartsWith("Floor")) r.sharedMaterial = floorM;
        }

        GameObject root = new GameObject(RootName);
        System.Random rnd = new System.Random(7);

        // ---- Ceiling with slats ----
        Transform ceil = NewGroup("Ceiling", root.transform);
        Prim(PrimitiveType.Cube, "CeilingBase", ceil, new Vector3(0, 3.65f, 0), new Vector3(16.4f, 0.1f, 12.4f), ceilM, false);
        for (int i = 0; i < 40; i++)
            Prim(PrimitiveType.Cube, "Slat", ceil, new Vector3(-7.8f + i * 0.4f, 3.55f, 0), new Vector3(0.2f, 0.12f, 12f), slatM, false);

        // ---- Long tables, swivel chairs, books ----
        Transform furn = NewGroup("StudentFurniture", root.transform);
        float[] rowZ = { 2f, -0.5f, -3f };
        float[] colX = { -4f, 4f };
        int tn = 1, cn = 1;
        foreach (float z in rowZ)
            foreach (float x in colX)
            {
                MakeTable(furn, "Student_Desk_" + tn, new Vector3(x, 0, z), tableM);
                foreach (float cx in new[] { -2f, 0f, 2f })
                    MakeChair(furn, "Student_Chair_" + cn++, new Vector3(x + cx, 0, z - 0.95f), chairM);
                for (int b = 0; b < 2; b++)
                {
                    GameObject book = Prim(PrimitiveType.Cube, "Book_On_Desk_" + tn + "_" + b, furn,
                        new Vector3(x + (b == 0 ? -1.5f : 0.9f), 0.80f, z + (float)(rnd.NextDouble() * 0.2 - 0.1)),
                        new Vector3(0.22f, 0.04f, 0.30f), bookM[rnd.Next(bookM.Length)]);
                    book.transform.localRotation = Quaternion.Euler(0, rnd.Next(-25, 25), 0);
                }
                tn++;
            }

        // ---- Bookshelf (back wall, right side) ----
        MakeBookshelf(root.transform, new Vector3(6.3f, 0, 5.55f), woodM, bookM, rnd);

        // ---- Glowing windows on the left wall ----
        Transform win = NewGroup("Windows", root.transform);
        foreach (float z in new[] { -3.5f, 0f, 3.5f })
        {
            Prim(PrimitiveType.Cube, "WinFrame", win, new Vector3(-7.82f, 2f, z), new Vector3(0.04f, 1.5f, 2.5f), darkM, false);
            Prim(PrimitiveType.Cube, "WinGlass", win, new Vector3(-7.78f, 2f, z), new Vector3(0.04f, 1.4f, 2.4f), winM, false);
            Prim(PrimitiveType.Cube, "WinBarV", win, new Vector3(-7.76f, 2f, z), new Vector3(0.03f, 1.4f, 0.06f), darkM, false);
            Prim(PrimitiveType.Cube, "WinBarH", win, new Vector3(-7.76f, 2f, z), new Vector3(0.03f, 0.06f, 2.4f), darkM, false);
        }

        // ---- Chalkboard look and wooden frame on the main board ----
        GameObject board = GameObject.Find("White_Board");
        if (board != null)
        {
            board.GetComponent<Renderer>().sharedMaterial = boardM;
            Transform frame = NewGroup("BoardFrame", root.transform);
            Vector3 bp = board.transform.position, bs = board.transform.lossyScale;
            float th = 0.12f, fz = bp.z - 0.02f;
            Prim(PrimitiveType.Cube, "FrameTop", frame, new Vector3(bp.x, bp.y + bs.y / 2 + th / 2, fz), new Vector3(bs.x + 2 * th, th, 0.1f), woodM, false);
            Prim(PrimitiveType.Cube, "FrameBottom", frame, new Vector3(bp.x, bp.y - bs.y / 2 - th / 2, fz), new Vector3(bs.x + 2 * th, th, 0.1f), woodM, false);
            Prim(PrimitiveType.Cube, "FrameLeft", frame, new Vector3(bp.x - bs.x / 2 - th / 2, bp.y, fz), new Vector3(th, bs.y, 0.1f), woodM, false);
            Prim(PrimitiveType.Cube, "FrameRight", frame, new Vector3(bp.x + bs.x / 2 + th / 2, bp.y, fz), new Vector3(th, bs.y, 0.1f), woodM, false);
        }
        GameObject td = GameObject.Find("Teacher_Desk");
        if (td != null) td.GetComponent<Renderer>().sharedMaterial = woodM;

        // Make the board text white so it reads on the dark board
        foreach (TMPro.TMP_Text tmp in Object.FindObjectsByType<TMPro.TMP_Text>())
            if (tmp.gameObject.name.ToLower().Contains("board")) { tmp.color = Color.white; EditorUtility.SetDirty(tmp); }

        // ---- Ceiling lamps ----
        Transform lights = NewGroup("Lights", root.transform);
        foreach (float lx in new[] { -5f, 0f, 5f })
            foreach (float lz in new[] { -3f, 2f })
            {
                GameObject lg = new GameObject("CeilingLight");
                lg.transform.SetParent(lights, false);
                lg.transform.localPosition = new Vector3(lx, 3.2f, lz);
                Light l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.93f, 0.8f);
                l.intensity = LightIntensity;
                l.range = 9f;
                l.shadows = LightShadows.None;
                Prim(PrimitiveType.Cube, "Fixture", lg.transform, new Vector3(0, 0.3f, 0), new Vector3(0.8f, 0.05f, 0.3f), lampM, false);
            }

        // ---- Mood: dim sunlight, blue ambient, fog ----
        GameObject dl = GameObject.Find("Directional Light");
        if (dl != null && dl.GetComponent<Light>() != null) dl.GetComponent<Light>().intensity = 0.25f;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Ambient;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = FogDensity;
        RenderSettings.fogColor = new Color(0.12f, 0.14f, 0.30f);

        MakeDust(root.transform);
        ApplyPost();

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("Classroom styled. Save with Ctrl+S.");
    }

    // ---------- helpers ----------
    static Transform NewGroup(string name, Transform parent)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        return g.transform;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = true)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static void MakeTable(Transform parent, string name, Vector3 center, Material m)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = center;
        Prim(PrimitiveType.Cube, "Top", g.transform, new Vector3(0, 0.75f, 0), new Vector3(6f, 0.06f, 0.9f), m);
        foreach (float sx in new[] { -2.9f, 2.9f })
            foreach (float sz in new[] { -0.4f, 0.4f })
                Prim(PrimitiveType.Cube, "Leg", g.transform, new Vector3(sx, 0.36f, sz), new Vector3(0.08f, 0.72f, 0.08f), m);
    }

    static void MakeChair(Transform parent, string name, Vector3 pos, Material m)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        Prim(PrimitiveType.Cylinder, "Base", g.transform, new Vector3(0, 0.02f, 0), new Vector3(0.5f, 0.01f, 0.5f), m);
        Prim(PrimitiveType.Cylinder, "Pole", g.transform, new Vector3(0, 0.24f, 0), new Vector3(0.07f, 0.22f, 0.07f), m);
        Prim(PrimitiveType.Cylinder, "Seat", g.transform, new Vector3(0, 0.46f, 0), new Vector3(0.5f, 0.03f, 0.5f), m);
        Prim(PrimitiveType.Cube, "Back", g.transform, new Vector3(0, 0.72f, -0.24f), new Vector3(0.46f, 0.38f, 0.04f), m);
    }

    static void MakeBookshelf(Transform parent, Vector3 c, Material wood, Material[] books, System.Random rnd)
    {
        float w = 2.4f, h = 2.4f, d = 0.5f;
        GameObject g = new GameObject("Bookshelf");
        g.transform.SetParent(parent, false);
        g.transform.localPosition = c;
        Prim(PrimitiveType.Cube, "Left", g.transform, new Vector3(-w / 2 + 0.025f, h / 2, 0), new Vector3(0.05f, h, d), wood);
        Prim(PrimitiveType.Cube, "Right", g.transform, new Vector3(w / 2 - 0.025f, h / 2, 0), new Vector3(0.05f, h, d), wood);
        Prim(PrimitiveType.Cube, "Back", g.transform, new Vector3(0, h / 2, d / 2 - 0.015f), new Vector3(w, h, 0.03f), wood);
        for (int s = 0; s < 5; s++)
        {
            float y = 0.025f + s * 0.5875f;
            Prim(PrimitiveType.Cube, "Shelf" + s, g.transform, new Vector3(0, y, 0), new Vector3(w, 0.05f, d), wood);
            if (s == 4) break;
            float x = -w / 2 + 0.12f;
            while (x < w / 2 - 0.12f)
            {
                float bw = 0.07f + (float)rnd.NextDouble() * 0.06f;
                float bh = 0.30f + (float)rnd.NextDouble() * 0.15f;
                Prim(PrimitiveType.Cube, "Book", g.transform, new Vector3(x + bw / 2, y + 0.025f + bh / 2, 0),
                    new Vector3(bw, bh, 0.3f), books[rnd.Next(books.Length)], false);
                x += bw + 0.01f;
            }
        }
    }

    static Material GetMat(string name, Color c, float smooth = 0.3f, float metal = 0f, float emission = 0f)
    {
        string path = "Assets/Materials/Style_" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static void MakeDust(Transform parent)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) { Debug.LogWarning("Particle shader not found, skipping dust."); return; }
        string path = "Assets/Materials/Style_Dust.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", 5f);
        m.SetFloat("_DstBlend", 10f);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        m.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.5f));
        EditorUtility.SetDirty(m);

        GameObject go = new GameObject("Dust");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0, 1.8f, 0);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 12f;
        main.startSpeed = 0.05f;
        main.startSize = 0.03f;
        main.maxParticles = 300;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(1f, 1f, 1f, 0.6f);
        var em = ps.emission;
        em.rateOverTime = 25f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(14f, 3f, 10f);
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = m;
    }

    static void ApplyPost()
    {
        GameObject vgo = GameObject.Find("Global Volume");
        Volume vol = vgo != null ? vgo.GetComponent<Volume>() : null;
        if (vol == null) { Debug.LogWarning("No Global Volume found, skipping post-processing."); return; }
        VolumeProfile p = vol.sharedProfile;
        if (p == null)
        {
            p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, "Assets/Settings/ClassroomVolumeProfile.asset");
            vol.sharedProfile = p;
        }
        if (!p.TryGet(out Bloom bloom)) bloom = p.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(0.9f);
        bloom.threshold.Override(0.95f);
        bloom.scatter.Override(0.65f);

        if (!p.TryGet(out Vignette vig)) vig = p.Add<Vignette>(true);
        vig.active = true;
        vig.intensity.Override(0.35f);
        vig.smoothness.Override(0.45f);

        if (!p.TryGet(out ColorAdjustments ca)) ca = p.Add<ColorAdjustments>(true);
        ca.active = true;
        ca.contrast.Override(12f);
        ca.saturation.Override(8f);
        ca.colorFilter.Override(new Color(0.92f, 0.96f, 1f));

        if (!p.TryGet(out Tonemapping tm)) tm = p.Add<Tonemapping>(true);
        tm.active = true;
        tm.mode.Override(TonemappingMode.ACES);

        EditorUtility.SetDirty(p);
        AssetDatabase.SaveAssets();
    }
}

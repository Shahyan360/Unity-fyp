#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class RomanGroundStyler
{
    private const string ScenarioRoot = "Ancient_Rome_Scenario";
    private const string StyleRootName = "RomanGround_Styler";
    private const string SandMaterialPath = "Assets/Materials/HistoricalPlace/RomanGround_DesertSand.mat";
    private const string NoiseTexturePath = "Assets/Materials/HistoricalPlace/RomanGround_SandDetail.asset";
    private const string DustMaterialPath = "Assets/Materials/HistoricalPlace/RomanGround_Dust.mat";
    private const string VolumeProfilePath = "Assets/Settings/RomanGround_DesertVolumeProfile.asset";

    [MenuItem("Tools/Roman Ground/Style Desert Ground and Effects")]
    public static void ApplyStyle()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.path.EndsWith("/HistoricalPlace.unity"))
        {
            EditorUtility.DisplayDialog("Open HistoricalPlace", "Open Assets/Scenes/HistoricalPlace.unity, then run this style tool again.", "OK");
            return;
        }

        GameObject ground = GameObject.Find("RomanGround");
        GameObject scenario = GameObject.Find(ScenarioRoot);
        if (ground == null || scenario == null)
        {
            EditorUtility.DisplayDialog("Roman scene not found", "The HistoricalPlace scene needs RomanGround and Ancient_Rome_Scenario before it can be styled.", "OK");
            return;
        }

        EnsureFolders();
        Material sand = GetMaterial(SandMaterialPath, "RomanGround Desert Sand", new Color(0.78f, 0.65f, 0.45f), 0.08f);
        Texture2D detail = GetSandDetail();
        if (detail != null)
        {
            string textureProperty = sand.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            sand.SetTexture(textureProperty, detail);
            sand.SetTextureScale(textureProperty, new Vector2(28f, 28f));
        }
        Renderer groundRenderer = ground.GetComponent<Renderer>();
        if (groundRenderer != null) groundRenderer.sharedMaterial = sand;

        Transform styleRoot = scenario.transform.Find(StyleRootName);
        if (styleRoot == null)
        {
            GameObject group = new GameObject(StyleRootName);
            group.transform.SetParent(scenario.transform, false);
            styleRoot = group.transform;
            BuildDunes(styleRoot);
        }

        SetDesertLighting();
        SetDesertFog();
        SetUpDust(styleRoot);
        ApplyPostProcessing();

        EditorUtility.SetDirty(ground);
        EditorUtility.SetDirty(scenario);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = styleRoot.gameObject;
        Debug.Log("Roman ground desert style and cinematic effects applied and saved.");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/HistoricalPlace"))
            AssetDatabase.CreateFolder("Assets/Materials", "HistoricalPlace");
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");
    }

    private static Material GetMaterial(string path, string label, Color color, float smoothness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { name = label };
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        else material.color = color;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D GetSandDetail()
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseTexturePath);
        if (texture != null) return texture;

        const int resolution = 128;
        texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, true, true)
        {
            name = "RomanGround_SandDetail",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 4
        };
        Color[] pixels = new Color[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float u = (x + 0.5f) / resolution;
                float v = (y + 0.5f) / resolution;
                float grain = 0.87f + 0.10f * ValueNoise(u, v, 5)
                    + 0.055f * ValueNoise(u, v, 17)
                    + 0.025f * ValueNoise(u, v, 53);
                pixels[y * resolution + x] = new Color(grain, grain, grain, 1f);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        AssetDatabase.CreateAsset(texture, NoiseTexturePath);
        return texture;
    }

    private static float ValueNoise(float u, float v, int cells)
    {
        float gx = u * cells;
        float gy = v * cells;
        int x0 = Mathf.FloorToInt(gx) % cells;
        int y0 = Mathf.FloorToInt(gy) % cells;
        int x1 = (x0 + 1) % cells;
        int y1 = (y0 + 1) % cells;
        float tx = gx - Mathf.Floor(gx);
        float ty = gy - Mathf.Floor(gy);
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        float bottom = Mathf.Lerp(Hash01(x0, y0), Hash01(x1, y0), tx);
        float top = Mathf.Lerp(Hash01(x0, y1), Hash01(x1, y1), tx);
        return Mathf.Lerp(bottom, top, ty);
    }

    private static float Hash01(int x, int y)
    {
        unchecked
        {
            uint value = (uint)x * 374761393u + (uint)y * 668265263u + 1442695041u;
            value = (value ^ (value >> 13)) * 1274126177u;
            value ^= value >> 16;
            return (value & 0x00FFFFFFu) / 16777215f;
        }
    }

    private static void BuildDunes(Transform parent)
    {
        Material duneLight = GetMaterial("Assets/Materials/HistoricalPlace/RomanGround_DuneLight.mat",
            "RomanGround Dune Light", new Color(0.80f, 0.66f, 0.45f), 0.05f);
        Material duneShade = GetMaterial("Assets/Materials/HistoricalPlace/RomanGround_DuneShade.mat",
            "RomanGround Dune Shade", new Color(0.69f, 0.55f, 0.37f), 0.04f);

        CreateDune(parent, "Desert_Dune_West_Front", new Vector3(-34f, 0f, -19f),
            new Vector2(10f, 14f), 0.85f, duneShade);
        CreateDune(parent, "Desert_Dune_East_Front", new Vector3(35f, 0f, -8f),
            new Vector2(9f, 9f), 0.72f, duneLight);
        CreateDune(parent, "Desert_Dune_West_Back", new Vector3(-34f, 0f, 24f),
            new Vector2(9f, 12f), 0.78f, duneLight);
    }

    private static void CreateDune(Transform parent, string name, Vector3 center,
        Vector2 footprint, float peak, Material material)
    {
        const int divisions = 16;
        Vector3[] vertices = new Vector3[(divisions + 1) * (divisions + 1)];
        int[] triangles = new int[divisions * divisions * 6];
        for (int z = 0; z <= divisions; z++)
        {
            for (int x = 0; x <= divisions; x++)
            {
                float u = x / (float)divisions * 2f - 1f;
                float v = z / (float)divisions * 2f - 1f;
                float radial = Mathf.Clamp01(1f - u * u - v * v);
                float height = peak * radial * radial * (3f - 2f * radial);
                vertices[z * (divisions + 1) + x] = new Vector3(u * footprint.x * 0.5f, height, v * footprint.y * 0.5f);
            }
        }

        int tri = 0;
        for (int z = 0; z < divisions; z++)
        {
            for (int x = 0; x < divisions; x++)
            {
                int a = z * (divisions + 1) + x;
                int b = a + 1;
                int c = a + divisions + 1;
                int d = c + 1;
                triangles[tri++] = a; triangles[tri++] = c; triangles[tri++] = b;
                triangles[tri++] = b; triangles[tri++] = c; triangles[tri++] = d;
            }
        }

        Mesh mesh = new Mesh { name = name + "_Mesh", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GameObject dune = new GameObject(name);
        dune.transform.SetParent(parent, false);
        dune.transform.localPosition = center;
        dune.AddComponent<MeshFilter>().sharedMesh = mesh;
        dune.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void SetDesertLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.60f, 0.52f, 0.40f);
        RenderSettings.ambientEquatorColor = new Color(0.48f, 0.39f, 0.29f);
        RenderSettings.ambientGroundColor = new Color(0.29f, 0.22f, 0.15f);
        RenderSettings.ambientIntensity = 1f;

        GameObject sunObject = GameObject.Find("Directional Light");
        Light sun = sunObject != null ? sunObject.GetComponent<Light>() : null;
        if (sun != null)
        {
            sun.color = new Color(1f, 0.84f, 0.63f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(38f, -31f, 0f);
            EditorUtility.SetDirty(sun);
        }
    }

    private static void SetDesertFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 38f;
        RenderSettings.fogEndDistance = 115f;
        RenderSettings.fogColor = new Color(0.72f, 0.60f, 0.43f);
    }

    private static void SetUpDust(Transform parent)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            Debug.LogWarning("URP particle shader was not found; desert dust was skipped.");
            return;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "RomanGround Desert Dust" };
            AssetDatabase.CreateAsset(material, DustMaterialPath);
        }
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", 5f);
        material.SetFloat("_DstBlend", 10f);
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        material.SetColor("_BaseColor", new Color(0.91f, 0.73f, 0.48f, 0.23f));
        EditorUtility.SetDirty(material);

        Transform existing = parent.Find("Desert_Dust");
        GameObject dust = existing != null ? existing.gameObject : new GameObject("Desert_Dust");
        if (existing == null) dust.transform.SetParent(parent, false);
        dust.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        ParticleSystem particles = dust.GetComponent<ParticleSystem>();
        if (particles == null) particles = dust.AddComponent<ParticleSystem>();

        var main = particles.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 18f;
        main.startSpeed = 0.025f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.075f);
        main.maxParticles = 180;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(0.94f, 0.78f, 0.54f, 0.22f);
        var emission = particles.emission;
        emission.rateOverTime = 5f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(75f, 2.4f, 70f);
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        // Unity requires all axes in this module to use the same MinMaxCurve mode.
        velocity.x = new ParticleSystem.MinMaxCurve(0.035f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0.01f);
        ParticleSystemRenderer renderer = dust.GetComponent<ParticleSystemRenderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        particles.Play();
    }

    private static void ApplyPostProcessing()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "RomanGround Desert Volume Profile";
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }

        GameObject volumeObject = GameObject.Find("Global Volume");
        if (volumeObject == null) volumeObject = new GameObject("Global Volume");
        Volume volume = volumeObject.GetComponent<Volume>();
        if (volume == null) volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.weight = 1f;
        volume.sharedProfile = profile;

        if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(0.22f);
        bloom.threshold.Override(1.2f);
        bloom.scatter.Override(0.2f);

        if (!profile.TryGet(out ColorAdjustments color)) color = profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.contrast.Override(10f);
        color.saturation.Override(-4f);
        color.colorFilter.Override(new Color(1f, 0.96f, 0.87f, 1f));

        if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(0.12f);
        vignette.smoothness.Override(0.55f);

        if (!profile.TryGet(out Tonemapping tonemapping)) tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        Camera camera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (camera != null)
        {
            camera.allowHDR = true;
            UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null) cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            EditorUtility.SetDirty(cameraData);
        }

        EditorUtility.SetDirty(profile);
        EditorUtility.SetDirty(volume);
    }
}
#endif

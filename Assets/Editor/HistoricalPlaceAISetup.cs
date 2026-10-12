#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

/// <summary>Sets up HistoricalPlace metadata, visitor routes, and its saved NavMesh.</summary>
[InitializeOnLoad]
public static class HistoricalPlaceAISetup
{
    const string AutoRunSessionKey = "EmbodiedAI.HistoricalPlaceAISetup.AutoRunAttempted";

    static HistoricalPlaceAISetup()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += TryAutoSetup;
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += TryAutoSetup;
    }

    static void TryAutoSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(AutoRunSessionKey, false)) return;
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded || activeScene.path != "Assets/Scenes/HistoricalPlace.unity") return;
        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshPath) != null) return;
        SessionState.SetBool(AutoRunSessionKey, true);
        Setup();
    }

    const string ScenarioName = "Ancient_Rome_Scenario";
    const string NavMeshPath = "Assets/Navigation/HistoricalPlaceNavMesh.asset";
    const string WaterPath = "Assets/Materials/HistoricalPlace/RomanGround_BoundaryWater.mat";
    const string DemoMaterialPath = "Assets/Navigation/HistoricalPlaceAI_Demo.mat";
    static readonly Color LakeBlue = new Color(0.30f, 0.70f, 0.94f, 1f);

    sealed class Point
    {
        public string id, label;
        public Vector3 position;
        public Point(string i, string l, Vector3 p) { id=i; label=l; position=p; }
    }

    [MenuItem("Tools/Embodied AI/Setup Historical Place AI")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || !scene.path.EndsWith("/HistoricalPlace.unity"))
        {
            EditorUtility.DisplayDialog("Open HistoricalPlace", "Open Assets/Scenes/HistoricalPlace.unity, then run this setup again.", "OK");
            return;
        }

        GameObject scenario = GameObject.Find(ScenarioName);
        if (scenario == null || GameObject.Find("RomanGround") == null)
        {
            EditorUtility.DisplayDialog("Historical Place not ready", "The scene needs RomanGround and Ancient_Rome_Scenario before setup.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Set Up Historical Place AI");
        int undoGroup = Undo.GetCurrentGroup();

        EnsureFolders();
        ApplyLightBlueLake(scenario);
        CreateLandmarks(scenario);
        ClassroomWaypointGraph graph = CreateWaypoints(scenario);
        CreateVisitorAgent(scenario, graph);
        ApplyMetadata(scene);
        NavMeshSurface surface = CreateSurface(scenario);
        EditorSceneManager.MarkSceneDirty(scene);
        BakeAndSave(surface, scene);
        Undo.CollapseUndoOperations(undoGroup);
    }

    static void ApplyLightBlueLake(GameObject scenario)
    {
        Material water = GetMaterial(WaterPath, "RomanGround_BoundaryWater", LakeBlue, 0.78f);
        Transform root = scenario.transform.Find("RomanGround_BoundaryLake");
        if (root == null)
        {
            Debug.LogWarning("Historical Place: boundary lake not found; no water color changed.");
            return;
        }
        int sides = 0;
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
        {
            if (item.name != "Boundary_Lake_Left" && item.name != "Boundary_Lake_Right") continue;
            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer == null) continue;
            renderer.sharedMaterial = water;
            EditorUtility.SetDirty(renderer);
            sides++;
        }
        Debug.Log("Historical Place: lake water changed to light blue on " + sides + " sides; the front stays open.");
    }

    static void CreateLandmarks(GameObject scenario)
    {
        Transform root = GetOrCreateChild(scenario.transform, "HistoricalPlace_Landmarks").transform;
        AddLandmark(root, "Colosseum_Landmark", new Vector3(0,0,0));
        AddLandmark(root, "Roman_Forum_Landmark", new Vector3(0,0,-33));
        AddLandmark(root, "West_Temple_Landmark", new Vector3(-25,0,14));
        AddLandmark(root, "East_Temple_Landmark", new Vector3(25,0,14));
        AddLandmark(root, "Pharaoh_Tomb_Landmark", new Vector3(29,0,24));
        AddLandmark(root, "Tiber_River_Landmark", new Vector3(0,0,36));
    }

    static void AddLandmark(Transform root, string name, Vector3 position)
    {
        GameObject landmark = GetOrCreateChild(root, name);
        landmark.transform.localPosition = position;
    }

    static ClassroomWaypointGraph CreateWaypoints(GameObject scenario)
    {
        GameObject root = GetOrCreateChild(scenario.transform, "HistoricalPlace_Waypoint_Graph");
        ClassroomWaypointGraph graph = root.GetComponent<ClassroomWaypointGraph>();
        if (graph == null) graph = Undo.AddComponent<ClassroomWaypointGraph>(root);

        Point[] plan =
        {
            new Point("site_entrance", "Open site entrance", new Vector3(0,.10f,-38.5f)),
            new Point("forum_gate", "Forum gateway", new Vector3(0,.12f,-34.4f)),
            new Point("forum_entry", "Forum entry court", new Vector3(0,.10f,-28f)),
            new Point("via_sacra_mid", "Via Sacra, middle", new Vector3(0,.10f,-19f)),
            new Point("colosseum_south_approach", "Colosseum south approach", new Vector3(0,.10f,-12.2f)),
            new Point("colosseum_west_flank", "Colosseum west path", new Vector3(-15.5f,.10f,-7f)),
            new Point("colosseum_northwest", "Northwest courtyard", new Vector3(-15.5f,.10f,11f)),
            new Point("west_temple_view", "West temple approach", new Vector3(-19.5f,.10f,15f)),
            new Point("west_market_clearance", "Market lane, west", new Vector3(-12.5f,.10f,17.5f)),
            new Point("north_courtyard", "North courtyard", new Vector3(0,.10f,30.8f)),
            new Point("tomb_north_approach", "Pharaoh tomb, north approach", new Vector3(29f,.10f,31.7f)),
            new Point("tomb_east_view", "Pharaoh tomb, east side", new Vector3(37.8f,.10f,24f))
        };

        List<ClassroomWaypoint> nodes = new List<ClassroomWaypoint>();
        foreach (Point preset in plan)
        {
            ClassroomWaypoint node = null;
            foreach (ClassroomWaypoint candidate in root.GetComponentsInChildren<ClassroomWaypoint>(true))
                if (candidate.waypointId == preset.id) { node = candidate; break; }
            if (node == null)
            {
                GameObject pointObject = new GameObject("Waypoint_" + preset.id);
                Undo.RegisterCreatedObjectUndo(pointObject, "Create historical place waypoint");
                pointObject.transform.SetParent(root.transform, false);
                node = Undo.AddComponent<ClassroomWaypoint>(pointObject);
            }
            Undo.RecordObject(node, "Configure historical place waypoint");
            node.transform.position = preset.position;
            node.waypointId = preset.id;
            node.displayName = preset.label;
            node.ClearConnections();
            nodes.Add(node);
            EditorUtility.SetDirty(node);
        }
        for (int i=0; i<nodes.Count-1; i++) nodes[i].ConnectBidirectionally(nodes[i+1]);
        graph.SetWaypoints(nodes);
        EditorUtility.SetDirty(graph);
        return graph;
    }

    static void CreateVisitorAgent(GameObject scenario, ClassroomWaypointGraph graph)
    {
        GameObject visitor = GetOrCreateChild(scenario.transform, "HistoricalPlace_Navigation_Demo_Agent");
        bool created = visitor.GetComponent<ClassroomWaypointNavigator>() == null;
        NavMeshAgent agent = visitor.GetComponent<NavMeshAgent>();
        if (agent == null) agent = Undo.AddComponent<NavMeshAgent>(visitor);
        agent.radius = .22f; agent.height = 1.8f; agent.speed = 1.8f;
        agent.acceleration = 8f; agent.angularSpeed = 240f; agent.stoppingDistance = .15f;
        agent.autoBraking = true;

        ClassroomWaypointNavigator navigator = visitor.GetComponent<ClassroomWaypointNavigator>();
        if (navigator == null) navigator = Undo.AddComponent<ClassroomWaypointNavigator>(visitor);
        navigator.Configure(graph, true, "colosseum_south_approach");
        if (created) visitor.transform.position = new Vector3(0,0,-38.5f);

        Transform visual = visitor.transform.Find("Visual");
        if (visual == null)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Visual";
            Undo.RegisterCreatedObjectUndo(capsule, "Create visitor visual");
            capsule.transform.SetParent(visitor.transform, false);
            capsule.transform.localPosition = new Vector3(0,.9f,0);
            capsule.transform.localScale = new Vector3(.36f,.9f,.36f);
            Collider collider = capsule.GetComponent<Collider>();
            if (collider != null) Undo.DestroyObjectImmediate(collider);
            visual = capsule.transform;
        }
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = GetMaterial(DemoMaterialPath, "HistoricalPlaceAI_Demo", new Color(.12f,.70f,.80f), .35f);
        EditorUtility.SetDirty(agent);
        EditorUtility.SetDirty(navigator);
        EditorUtility.SetDirty(visitor);
    }

    static NavMeshSurface CreateSurface(GameObject scenario)
    {
        GameObject root = GetOrCreateChild(scenario.transform, "HistoricalPlace_NavMesh_Surface");
        NavMeshSurface surface = root.GetComponent<NavMeshSurface>();
        if (surface == null) surface = Undo.AddComponent<NavMeshSurface>(root);
        surface.agentTypeID = 0;
        surface.collectObjects = CollectObjects.Volume;
        surface.center = new Vector3(0,1.35f,-3.75f);
        surface.size = new Vector3(78,3.8f,71.5f);
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.ignoreNavMeshAgent = true;
        surface.ignoreNavMeshObstacle = true;
        surface.overrideVoxelSize = true;
        surface.voxelSize = .12f;
        EditorUtility.SetDirty(surface);
        return surface;
    }

    static void BakeAndSave(NavMeshSurface surface, Scene scene)
    {
        NavMeshData existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshPath);
        if (existing == null)
        {
            surface.navMeshData = null;
            surface.BuildNavMesh();
            NavMeshData built = surface.navMeshData;
            if (built == null)
            {
                EditorUtility.DisplayDialog("NavMesh not created", "Unity could not create Navigation data. Confirm RomanGround has a collider and check the Console.", "OK");
                return;
            }
            built.name = "HistoricalPlaceNavMesh";
            AssetDatabase.CreateAsset(built, NavMeshPath);
            Finish(surface, scene, built);
            return;
        }
        surface.RemoveData();
        surface.navMeshData = existing;
        surface.AddData();
        AsyncOperation operation = surface.UpdateNavMesh(existing);
        if (operation == null || operation.isDone) { Finish(surface, scene, existing); return; }
        EditorApplication.update += Wait;
        void Wait()
        {
            if (operation == null || !operation.isDone) return;
            EditorApplication.update -= Wait;
            Finish(surface, scene, existing);
        }
    }

    static void Finish(NavMeshSurface surface, Scene scene, NavMeshData data)
    {
        EditorUtility.SetDirty(surface);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Historical Place AI setup complete: metadata, visitor waypoints, demo agent, light blue lake, and saved NavMesh are ready.");
    }

    static void ApplyMetadata(Scene scene)
    {
        int count = 0;
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        foreach (Transform item in sceneRoot.GetComponentsInChildren<Transform>(true))
        {
            string category, tags, description;
            bool interactable;
            string hint;
            if (!Describe(item, out category, out tags, out description, out interactable, out hint)) continue;
            ObjectMetadata metadata = item.GetComponent<ObjectMetadata>();
            if (metadata == null) metadata = Undo.AddComponent<ObjectMetadata>(item.gameObject);
            Undo.RecordObject(metadata, "Set historical place metadata");
            metadata.objectId = BuildId(item);
            metadata.displayName = item.name.Replace('_',' ');
            metadata.category = category;
            metadata.descriptionSummary = description;
            metadata.contextualTags = MergeTags(metadata.contextualTags, tags);
            metadata.isInteractable = interactable;
            metadata.interactionHint = hint;
            EditorUtility.SetDirty(metadata);
            count++;
        }
        Debug.Log("Historical Place: searchable metadata added to " + count + " site objects.");
    }

    static bool Describe(Transform item, out string category, out string tags, out string description, out bool interactable, out string hint)
    {
        string n = item.name;
        category = tags = description = hint = "";
        interactable = false;
        if (n == ScenarioName) { category="Historic site"; tags="historical-place,ancient-rome,visitor-site,landmark"; description="A recreated ancient Roman site with a Colosseum, forum buildings, temples, a Pharaoh-era tomb, and river."; return true; }
        if (n == "RomanGround") { category="Terrain"; tags="historical-place,desert,sand,walkable"; description="The sandy base terrain of the historical site."; return true; }
        if (n == "HistoricalPlace_Landmarks") { category="Landmark collection"; tags="historical-place,landmarks,locations"; description="Named reference points for the major site landmarks."; return true; }
        if (n == "Colosseum_Landmark") { category="Amphitheatre"; tags="historical-place,colosseum,amphitheatre,landmark"; description="The central Roman amphitheatre landmark."; return true; }
        if (n == "Roman_Forum_Landmark") { category="Public square"; tags="historical-place,roman-forum,landmark"; description="The front forum and public gathering area."; return true; }
        if (n == "West_Temple_Landmark" || n == "East_Temple_Landmark") { category="Temple"; tags="historical-place,roman-temple,landmark"; description="A Roman temple landmark on the " + (n.StartsWith("West_") ? "west" : "east") + " side."; return true; }
        if (n == "Pharaoh_Tomb_Landmark" || n == "Pharaoh_Tomb_Complex") { category="Tomb"; tags="historical-place,pharaoh,tomb,ancient-egypt"; description="A Pharaoh-era sandstone tomb complex."; return true; }
        if (n == "Tiber_River_Landmark" || n == "Tiber_River") { category="Waterway"; tags="historical-place,tiber,river,water"; description="The river along the rear boundary of the Roman site."; return true; }
        if (n == "RomanGround_BoundaryLake") { category="Water feature"; tags="historical-place,lake,light-blue,boundary"; description="Light blue side channels join the rear river while leaving the front entrance open."; return true; }
        if (n == "Boundary_Lake_Left" || n == "Boundary_Lake_Right") { category="Lake channel"; tags="historical-place,lake,light-blue,water"; description="One light blue side channel of the boundary lake."; return true; }
        if (n == "Front_Forum_Complex" || n == "Forum_Gateway_Entablature") { category="Roman forum"; tags="historical-place,forum,entrance,architecture"; description="The stone Roman forum frontage and gateway at the front of the site."; return true; }
        if (n == "Entrance_Lintel" || n == "Forum_Gateway_Pier") { category="Entrance"; tags="historical-place,entrance,forum,gate"; description="A stone gateway structure along the visitor entrance route."; return true; }
        if (n == "Arena_Sand_Floor") { category="Amphitheatre"; tags="historical-place,colosseum,arena"; description="The sand arena floor inside the Colosseum."; return true; }
        if (n == "Roman_Brick_Paving" || n == "Via_Sacra_Approach") { category="Roman road"; tags="historical-place,via-sacra,brick-path,walkway"; description="A paved path from the entrance toward the central monuments."; return true; }
        if (n == "Forum_Fountain_Base") { category="Fountain"; tags="historical-place,roman-forum,fountain,water"; description="A stone fountain in the Roman forum."; return true; }
        if ((n.StartsWith("Forum_Shop_") || n.StartsWith("Market_")) && n.EndsWith("_Walls")) { category="Roman marketplace"; tags="historical-place,market,shop,commerce"; description="A small Roman market building beside the visitor streets."; return true; }
        if (n == "Temple_Podium" || n == "West_Temple_Wing_Podium" || n == "East_Temple_Wing_Podium") { category="Temple"; tags="historical-place,roman-temple,architecture"; description="A raised stone base of a Roman temple."; return true; }
        if (n == "Camel_01" || n == "Camel_02") { category="Animal"; tags="historical-place,camel,desert"; description="A camel in the desert environment."; return true; }
        if (n == "Horse_01" || n == "Horse_02") { category="Animal"; tags="historical-place,horse"; description="A horse in the historical environment."; return true; }
        ClassroomWaypoint waypoint = item.GetComponent<ClassroomWaypoint>();
        if (waypoint != null) { category="Visitor waypoint"; tags="historical-place,waypoint,navigation,visitor-route"; description="A named point on the visitor route: " + waypoint.displayName + "."; return true; }
        if (n == "HistoricalPlace_Navigation_Demo_Agent") { category="Navigation demo"; tags="historical-place,visitor,navmesh"; description="A sample visitor that follows the route from the entrance toward the Colosseum."; return true; }
        return false;
    }

    static string BuildId(Transform item)
    {
        List<string> parts = new List<string>();
        for (Transform node=item; node!=null; node=node.parent)
            parts.Add(Slug(node.name) + "_" + node.GetSiblingIndex());
        parts.Reverse();
        return "historical_place_" + string.Join("_", parts);
    }

    static string Slug(string value)
    {
        StringBuilder result = new StringBuilder();
        foreach (char c in value.ToLowerInvariant())
            if (char.IsLetterOrDigit(c)) result.Append(c);
            else if (result.Length > 0 && result[result.Length-1] != '_') result.Append('_');
        return result.ToString().Trim('_');
    }

    static string MergeTags(string existing, string defaults)
    {
        List<string> tags = new List<string>();
        foreach (string source in new[] { existing, defaults })
        {
            if (string.IsNullOrWhiteSpace(source)) continue;
            foreach (string part in source.Split(new[] {',',';'}, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string tag = part.Trim();
                if (tag.Length == 0) continue;
                bool found = false;
                foreach (string current in tags)
                    if (string.Equals(tag,current,System.StringComparison.OrdinalIgnoreCase)) { found=true; break; }
                if (!found) tags.Add(tag);
            }
        }
        return string.Join(", ", tags);
    }

    static GameObject GetOrCreateChild(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null) return found.gameObject;
        GameObject created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, "Create " + name);
        created.transform.SetParent(parent, false);
        return created;
    }

    static Material GetMaterial(string path, string name, Color color, float smoothness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) { Debug.LogError("Historical Place: could not find a Lit shader for " + name); return null; }
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        else material.color = color;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Navigation")) AssetDatabase.CreateFolder("Assets","Navigation");
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets","Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/HistoricalPlace")) AssetDatabase.CreateFolder("Assets/Materials","HistoricalPlace");
    }
}
#endif




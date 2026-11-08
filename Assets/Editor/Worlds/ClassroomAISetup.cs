using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

/// <summary>One-click setup for classroom metadata, route waypoints, and AI Navigation.</summary>
[InitializeOnLoad]
public static class ClassroomAISetup
{
    private const string AutoRunSessionKey = "EmbodiedAI.ClassroomAISetup.AutoRunAttempted";

    static ClassroomAISetup()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += SetupOpenClassroomOnce;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += () => TryAutoSetup(scene);
    }

    private static void SetupOpenClassroomOnce()
    {
        TryAutoSetup(SceneManager.GetActiveScene());
    }

    private static void TryAutoSetup(Scene activeScene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(AutoRunSessionKey, false)) return;
        if (!activeScene.IsValid() || activeScene.path != "Assets/Scenes/Classroom.unity") return;
        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshAssetPath) != null) return;
        SessionState.SetBool(AutoRunSessionKey, true);
        SetupClassroomAI();
    }
    private const string NavMeshAssetPath = "Assets/Navigation/ClassroomNavMesh.asset";
    private const string DemoMaterialPath = "Assets/Navigation/ClassroomAI_Demo.mat";

    private sealed class WaypointPreset
    {
        public readonly string Id;
        public readonly string Label;
        public readonly Vector3 Position;
        public WaypointPreset(string id, string label, Vector3 position) { Id = id; Label = label; Position = position; }
    }

    [MenuItem("Tools/Embodied AI/Setup Classroom AI")]
    public static void SetupClassroomAI()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("Classroom AI Setup", "Open the Classroom scene first.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Set Up Classroom AI");
        int undoGroup = Undo.GetCurrentGroup();

        ApplyObjectMetadata(scene);
        ClassroomWaypointGraph graph = CreateWaypointGraph();
        CreateDemoAgent(graph);
        NavMeshSurface surface = CreateNavMeshSurface();
        ConfigureDoorObstacle(scene, surface);
        EditorSceneManager.MarkSceneDirty(scene);
        BakeAndSaveNavMesh(surface, scene);
        Undo.CollapseUndoOperations(undoGroup);
    }

    private static void ApplyObjectMetadata(Scene scene)
    {
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                string category;
                string tags;
                string description;
                bool interactable;
                string hint;
                if (!TryDescribe(item.name, out category, out tags, out description, out interactable, out hint)) continue;

                ObjectMetadata metadata = item.GetComponent<ObjectMetadata>();
                if (metadata == null) metadata = Undo.AddComponent<ObjectMetadata>(item.gameObject);
                Undo.RecordObject(metadata, "Set classroom object metadata");
                metadata.objectId = BuildStableId(item);
                metadata.displayName = Humanize(item.name);
                metadata.category = category;
                metadata.descriptionSummary = description;
                metadata.contextualTags = MergeTags(metadata.contextualTags, tags);
                metadata.isInteractable = interactable;
                metadata.interactionHint = hint;
                EditorUtility.SetDirty(metadata);
                count++;
            }
        }
        Debug.Log("Classroom AI: added searchable metadata to " + count + " classroom objects.");
    }

    private static bool TryDescribe(string name, out string category, out string tags, out string description, out bool interactable, out string hint)
    {
        interactable = false;
        hint = string.Empty;
        if (name == "Environment" || name == "Classroom_Style")
        {
            category = "Classroom structure"; tags = "classroom,architecture,room"; description = "Main classroom structure and interior layout."; return true;
        }
        if (name == "Floor")
        {
            category = "Architecture"; tags = "classroom,floor,walkable,room"; description = "The walkable floor of the classroom."; return true;
        }
        if (name.StartsWith("Wall_", StringComparison.Ordinal) || name.StartsWith("Ceiling", StringComparison.Ordinal))
        {
            category = "Architecture"; tags = name.StartsWith("Wall_", StringComparison.Ordinal) ? "classroom,wall,cream,perimeter" : "classroom,ceiling,architecture";
            description = "Part of the classroom's architectural shell."; return true;
        }
        if (name == "White_Board" || name == "Standing_Board")
        {
            category = "Teaching tool"; tags = "classroom,teaching,board,front"; description = "A board used by the teacher during lessons."; return true;
        }
        if (name == "Board_text")
        {
            category = "Lesson content"; tags = "classroom,teaching,lesson,board-text"; description = "The lesson text displayed on the classroom board."; return true;
        }
        if (name == "Teacher_Desk")
        {
            category = "Teacher furniture"; tags = "classroom,furniture,teacher,front"; description = "The teacher's desk at the front of the classroom."; return true;
        }
        if (name == "Door_Leaf" || name == "Front_Door_Hinge")
        {
            category = "Entrance"; tags = "classroom,door,entrance,interactable"; description = "The classroom front entrance door.";
            interactable = name == "Door_Leaf";
            hint = interactable ? "Click the door in Play mode to open or close it." : string.Empty;
            return true;
        }
        if (name.StartsWith("Student_Desk_", StringComparison.Ordinal))
        {
            category = "Student furniture"; tags = "classroom,furniture,student,desk"; description = "A student desk in the classroom seating area."; return true;
        }
        if (name.StartsWith("Student_Chair_", StringComparison.Ordinal))
        {
            category = "Student furniture"; tags = "classroom,furniture,student,chair,seat"; description = "A student chair beside a classroom desk."; return true;
        }
        if (name.StartsWith("Book_On_Desk_", StringComparison.Ordinal) || name == "Book")
        {
            category = "Learning material"; tags = "classroom,learning,book,student"; description = "A book used as classroom learning material."; return true;
        }
        category = tags = description = hint = string.Empty;
        return false;
    }

    private static string BuildStableId(Transform item)
    {
        var segments = new List<string>();
        Transform cursor = item;
        while (cursor != null)
        {
            segments.Add(Slug(cursor.name) + "_" + cursor.GetSiblingIndex());
            cursor = cursor.parent;
        }
        segments.Reverse();
        return "classroom_" + string.Join("_", segments);
    }

    private static string Slug(string value)
    {
        var result = new StringBuilder();
        foreach (char c in value.ToLowerInvariant())
            if (char.IsLetterOrDigit(c)) result.Append(c);
            else if (result.Length > 0 && result[result.Length - 1] != '_') result.Append('_');
        return result.ToString().Trim('_');
    }

    private static string Humanize(string value) => value.Replace('_', ' ');

    private static string MergeTags(string existing, string defaults)
    {
        var tags = new List<string>();
        AddUniqueTags(tags, existing);
        AddUniqueTags(tags, defaults);
        return string.Join(", ", tags);
    }

    private static void AddUniqueTags(List<string> result, string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return;
        string[] parts = source.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in parts)
        {
            string tag = part.Trim();
            bool found = false;
            foreach (string current in result)
                if (string.Equals(current, tag, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
            if (!found && tag.Length > 0) result.Add(tag);
        }
    }

    private static ClassroomWaypointGraph CreateWaypointGraph()
    {
        GameObject root = GetOrCreateRoot("Classroom_Waypoint_Graph");
        ClassroomWaypointGraph graph = root.GetComponent<ClassroomWaypointGraph>();
        if (graph == null) graph = Undo.AddComponent<ClassroomWaypointGraph>(root);

        WaypointPreset[] presets =
        {
            new WaypointPreset("entrance_inside", "Inside entrance", new Vector3(0f, 0.05f, -5.45f)),
            new WaypointPreset("front_aisle", "Front aisle", new Vector3(0f, 0.05f, -4.1f)),
            new WaypointPreset("middle_front", "Middle aisle, front", new Vector3(0f, 0.05f, -1.8f)),
            new WaypointPreset("center_aisle", "Center aisle", new Vector3(0f, 0.05f, 0.5f)),
            new WaypointPreset("rear_aisle", "Rear aisle", new Vector3(0f, 0.05f, 3.2f)),
            new WaypointPreset("teacher_approach", "Teacher desk approach", new Vector3(2.35f, 0.05f, 4.35f))
        };

        var points = new List<ClassroomWaypoint>();
        foreach (WaypointPreset preset in presets)
        {
            ClassroomWaypoint point = null;
            foreach (ClassroomWaypoint candidate in root.GetComponentsInChildren<ClassroomWaypoint>(true))
                if (candidate.waypointId == preset.Id) { point = candidate; break; }
            if (point == null)
            {
                var pointObject = new GameObject("Waypoint_" + preset.Id);
                Undo.RegisterCreatedObjectUndo(pointObject, "Create classroom waypoint");
                pointObject.transform.SetParent(root.transform, false);
                pointObject.transform.position = preset.Position;
                point = Undo.AddComponent<ClassroomWaypoint>(pointObject);
            }
            Undo.RecordObject(point, "Configure classroom waypoint");
            point.waypointId = preset.Id;
            point.displayName = preset.Label;
            point.ClearConnections();
            points.Add(point);
        }

        for (int i = 0; i < points.Count - 1; i++) points[i].ConnectBidirectionally(points[i + 1]);
        graph.SetWaypoints(points);
        EditorUtility.SetDirty(graph);
        foreach (ClassroomWaypoint point in points) EditorUtility.SetDirty(point);
        return graph;
    }

    private static GameObject GetOrCreateRoot(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name) return root;
        var created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, "Create " + name);
        return created;
    }

    private static void CreateDemoAgent(ClassroomWaypointGraph graph)
    {
        GameObject demo = GetOrCreateRoot("Classroom_Navigation_Demo_Agent");
        bool created = demo.GetComponent<ClassroomWaypointNavigator>() == null;
        NavMeshAgent agent = demo.GetComponent<NavMeshAgent>();
        if (agent == null) agent = Undo.AddComponent<NavMeshAgent>(demo);
        agent.radius = 0.22f;
        agent.height = 1.8f;
        agent.speed = 1.8f;
        agent.acceleration = 8f;
        agent.angularSpeed = 240f;
        agent.stoppingDistance = 0.15f;
        agent.autoBraking = true;
        ClassroomWaypointNavigator navigator = demo.GetComponent<ClassroomWaypointNavigator>();
        if (navigator == null) navigator = Undo.AddComponent<ClassroomWaypointNavigator>(demo);
        navigator.Configure(graph, true, "teacher_approach");
        if (created) demo.transform.position = new Vector3(0f, 0f, -5.35f);

        Transform visual = demo.transform.Find("Visual");
        if (visual == null)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Visual";
            Undo.RegisterCreatedObjectUndo(capsule, "Create AI demo character visual");
            capsule.transform.SetParent(demo.transform, false);
            capsule.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            capsule.transform.localScale = new Vector3(0.36f, 0.9f, 0.36f);
            Collider visualCollider = capsule.GetComponent<Collider>();
            if (visualCollider != null) Undo.DestroyObjectImmediate(visualCollider);
            visual = capsule.transform;
        }
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = GetDemoMaterial();
        EditorUtility.SetDirty(agent);
        EditorUtility.SetDirty(navigator);
    }

    private static Material GetDemoMaterial()
    {
        EnsureNavigationFolder();
        Material material = AssetDatabase.LoadAssetAtPath<Material>(DemoMaterialPath);
        if (material != null) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        material = new Material(shader) { name = "ClassroomAI_Demo" };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.1f, 0.75f, 0.68f));
        else if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.1f, 0.75f, 0.68f));
        AssetDatabase.CreateAsset(material, DemoMaterialPath);
        return material;
    }

    private static NavMeshSurface CreateNavMeshSurface()
    {
        GameObject root = GetOrCreateRoot("Classroom_NavMesh_Surface");
        NavMeshSurface surface = root.GetComponent<NavMeshSurface>();
        if (surface == null) surface = Undo.AddComponent<NavMeshSurface>(root);
        surface.agentTypeID = 0;
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.ignoreNavMeshAgent = true;
        surface.ignoreNavMeshObstacle = true;
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.1f;
        EditorUtility.SetDirty(surface);
        return surface;
    }

    private static void ConfigureDoorObstacle(Scene scene, NavMeshSurface surface)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (item.name != "Door_Leaf") continue;
                NavMeshObstacle obstacle = item.GetComponent<NavMeshObstacle>();
                if (obstacle == null) obstacle = Undo.AddComponent<NavMeshObstacle>(item.gameObject);
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = Vector3.one;
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;
                obstacle.carvingMoveThreshold = 0.1f;
                EditorUtility.SetDirty(obstacle);
                surface.ignoreNavMeshObstacle = true;
                return;
            }
        }
        Debug.LogWarning("Classroom AI: Door_Leaf was not found, so no dynamic door obstacle was added.");
    }

    private static void BakeAndSaveNavMesh(NavMeshSurface surface, Scene scene)
    {
        EnsureNavigationFolder();
        NavMeshData existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshAssetPath);
        if (existing == null)
        {
            surface.navMeshData = null;
            surface.BuildNavMesh();
            NavMeshData built = surface.navMeshData;
            if (built == null)
            {
                EditorUtility.DisplayDialog("NavMesh not created", "Unity did not produce NavMesh data. Check the Console and confirm the classroom floor has a collider.", "OK");
                return;
            }
            built.name = "ClassroomNavMesh";
            AssetDatabase.CreateAsset(built, NavMeshAssetPath);
            FinishBake(surface, scene, built);
            return;
        }

        surface.RemoveData();
        surface.navMeshData = existing;
        surface.AddData();
        AsyncOperation operation = surface.UpdateNavMesh(existing);
        if (operation == null || operation.isDone)
        {
            FinishBake(surface, scene, existing);
            return;
        }
        EditorApplication.update += WaitForBake;
        void WaitForBake()
        {
            if (operation == null || !operation.isDone) return;
            EditorApplication.update -= WaitForBake;
            FinishBake(surface, scene, existing);
        }
    }

    private static void FinishBake(NavMeshSurface surface, Scene scene, NavMeshData data)
    {
        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(surface);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Classroom AI setup complete: metadata, aisle waypoints, demo agent, and NavMesh are ready. The demo capsule will walk to the teacher desk when you press Play.");
    }

    private static void EnsureNavigationFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Navigation"))
            AssetDatabase.CreateFolder("Assets", "Navigation");
    }
}






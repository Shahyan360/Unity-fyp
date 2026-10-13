using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

/// <summary>Adds cafe guests, searchable metadata, stair waypoints, a follow demo, and a saved NavMesh.</summary>
public static class CafeStoreAISetup
{
    private const string CafeRootName = "Cafe_Build";
    private const string GraphRootName = "Cafe_Waypoint_Graph";
    private const string NavRootName = "Cafe_NavMesh_Surface";
    private const string NavMeshAssetPath = "Assets/Navigation/CafestoreNavMesh.asset";
    private const string MaterialsFolder = "Assets/Materials";

    private sealed class WaypointPreset
    {
        public readonly string Id;
        public readonly string Label;
        public readonly Vector3 Position;
        public WaypointPreset(string id, string label, Vector3 position) { Id = id; Label = label; Position = position; }
    }

    private struct SeatPose
    {
        public Vector3 Position;
        public float Yaw;
        public SeatPose(Vector3 position, float yaw) { Position = position; Yaw = yaw; }
    }

    [MenuItem("Tools/Embodied AI/Setup Cafe Environment")]
    public static void SetupCafeEnvironment()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.path != "Assets/Scenes/Cafestore.unity")
        {
            EditorUtility.DisplayDialog("Open Cafestore first", "Open Assets/Scenes/Cafestore, then run Tools > Embodied AI > Setup Cafe Environment.", "OK");
            return;
        }

        GameObject cafe = GameObject.Find(CafeRootName);
        if (cafe == null)
        {
            EditorUtility.DisplayDialog("Cafe build not found", "Run Tools > Build Cafe first.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Set Up Cafe Environment");
        int undoGroup = Undo.GetCurrentGroup();

        CafeBuilder.UpdateCafeFloorTiles();
        CreateCafeCharacters(cafe.transform);
        ApplyCafeMetadata(cafe.transform);
        CreateCafeWaypointGraph();
        GameObject player = CreatePlayer();
        GameObject follower = CreateFollower(player.transform);
        NavMeshSurface surface = CreateNavMeshSurface(cafe.transform, player.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        BakeAndSaveNavMesh(surface, scene, follower.transform.position, new Vector3(1f, 3.65f, 2.2f));
        Undo.CollapseUndoOperations(undoGroup);
    }

    // ---------------------------------------------------------------- outdoor and indoor people
    private static Transform CreateCafeCharacters(Transform cafe)
    {
        Transform old = cafe.Find("Characters");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        GameObject group = new GameObject("Characters");
        Undo.RegisterCreatedObjectUndo(group, "Create cafe people");
        group.transform.SetParent(cafe, false);

        Material skin = GetMaterial("Cafe_Skin", new Color(0.73f, 0.48f, 0.33f));
        Material hair = GetMaterial("Cafe_Hair", new Color(0.12f, 0.075f, 0.055f));
        Material pants = GetMaterial("Cafe_Pants", new Color(0.12f, 0.16f, 0.23f));
        Material shoes = GetMaterial("Cafe_Shoes", new Color(0.07f, 0.06f, 0.055f));
        Material[] outfits =
        {
            GetMaterial("Cafe_Guest_Teal", new Color(0.12f, 0.40f, 0.38f)),
            GetMaterial("Cafe_Guest_Rose", new Color(0.62f, 0.23f, 0.30f)),
            GetMaterial("Cafe_Guest_Ochre", new Color(0.72f, 0.43f, 0.18f)),
            GetMaterial("Cafe_Guest_Navy", new Color(0.15f, 0.22f, 0.34f)),
            GetMaterial("Cafe_Guest_Sage", new Color(0.34f, 0.46f, 0.34f)),
            GetMaterial("Cafe_Guest_Cream", new Color(0.78f, 0.68f, 0.48f))
        };

        List<SeatPose> indoor = new List<SeatPose>();
        AddTableSeats(indoor, new Vector3(-4.2f, 0f, -3f), 4, 45f, 0.9f);
        AddTableSeats(indoor, new Vector3(-1.2f, 0f, -3f), 4, 45f, 0.9f);
        AddTableSeats(indoor, new Vector3(-4.2f, 0f, 1f), 4, 45f, 0.9f);
        AddTableSeats(indoor, new Vector3(-1.2f, 0f, 1f), 4, 45f, 0.9f);
        AddTableSeats(indoor, new Vector3(1.9f, 0f, 2.4f), 4, 45f, 0.9f);

        List<SeatPose> outside = new List<SeatPose>();
        AddTableSeats(outside, new Vector3(3.5f, 0f, -9.5f), 2, 90f, 0.8f);
        AddTableSeats(outside, new Vector3(6.8f, 0f, -9.5f), 2, 90f, 0.8f);
        AddTableSeats(outside, new Vector3(-3f, 0f, -10f), 2, 90f, 0.8f);

        System.Random random = new System.Random(5317);
        Shuffle(indoor, random);
        Shuffle(outside, random);
        for (int i = 0; i < 4; i++)
            CreateSeatedGuest(group.transform, indoor[i], i % 2 == 1, i + 1, outfits[i % outfits.Length], skin, hair, pants, shoes);
        for (int i = 0; i < 2; i++)
            CreateSeatedGuest(group.transform, outside[i], i == 0, i + 5, outfits[(i + 4) % outfits.Length], skin, hair, pants, shoes);

        CreateChef(group.transform, skin, hair, shoes);
        return group.transform;
    }

    private static void AddTableSeats(List<SeatPose> seats, Vector3 center, int count, float firstAngle, float diameter)
    {
        float radius = diameter * 0.5f + 0.30f;
        for (int i = 0; i < count; i++)
        {
            float angle = (firstAngle + i * 360f / count) * Mathf.Deg2Rad;
            float dx = Mathf.Sin(angle), dz = Mathf.Cos(angle);
            Vector3 position = center + new Vector3(dx * radius, 0f, dz * radius);
            seats.Add(new SeatPose(position, Mathf.Atan2(-dx, -dz) * Mathf.Rad2Deg));
        }
    }

    private static void Shuffle<T>(IList<T> items, System.Random random)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T value = items[i]; items[i] = items[j]; items[j] = value;
        }
    }

    private static void CreateSeatedGuest(Transform parent, SeatPose seat, bool female, int number,
        Material outfit, Material skin, Material hair, Material pants, Material shoes)
    {
        string label = female ? "Woman" : "Man";
        Transform guest = NewGroup(parent, "CafeGuest_" + label + "_" + number.ToString("00"), seat.Position, seat.Yaw);
        MakeCylinder(guest, "Torso", new Vector3(0f, 0.98f, 0f), 0.46f, 0.68f, outfit);
        if (female)
            MakeCylinder(guest, "Skirt", new Vector3(0f, 0.62f, 0.03f), 0.48f, 0.42f, outfit);
        MakeSphere(guest, "Neck", new Vector3(0f, 1.36f, 0.02f), new Vector3(0.12f, 0.14f, 0.12f), skin);
        MakeSphere(guest, "Head", new Vector3(0f, 1.53f, 0.02f), new Vector3(0.28f, 0.31f, 0.27f), skin);
        MakeSphere(guest, "Hair", new Vector3(0f, 1.66f, -0.015f), new Vector3(0.30f, 0.14f, 0.29f), hair);
        if (female)
            MakeSphere(guest, "LongHair", new Vector3(0f, 1.39f, -0.12f), new Vector3(0.28f, 0.34f, 0.16f), hair);

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 0.13f;
            MakeCube(guest, "Thigh", new Vector3(x, 0.49f, 0.22f), new Vector3(0.22f, 0.17f, 0.48f), female ? outfit : pants);
            MakeCube(guest, "LowerLeg", new Vector3(x, 0.34f, 0.44f), new Vector3(0.18f, 0.14f, 0.30f), pants);
            MakeCube(guest, "Shoe", new Vector3(x, 0.23f, 0.57f), new Vector3(0.20f, 0.12f, 0.31f), shoes);

            Vector3 shoulder = new Vector3(side * 0.22f, 1.21f, 0.01f);
            Vector3 elbow = new Vector3(side * 0.28f, 1.04f, 0.22f);
            Vector3 hand = new Vector3(side * 0.23f, 0.84f, 0.48f);
            MakeSegment(guest, "UpperArm", shoulder, elbow, 0.12f, skin);
            MakeSegment(guest, "Forearm", elbow, hand, 0.10f, skin);
            MakeSphere(guest, "Hand", hand, Vector3.one * 0.12f, skin);
        }
    }

    private static void CreateChef(Transform parent, Material skin, Material hair, Material shoes)
    {
        Transform chef = NewGroup(parent, "Cafe_Chef", new Vector3(6.25f, 0f, 1.1f), -90f);
        Material coat = GetMaterial("Cafe_Chef_White", new Color(0.88f, 0.86f, 0.80f));
        Material apron = GetMaterial("Cafe_Chef_Apron", new Color(0.22f, 0.13f, 0.10f));
        MakeCube(chef, "Coat", new Vector3(0f, 1.12f, 0f), new Vector3(0.56f, 0.78f, 0.36f), coat);
        MakeCube(chef, "Apron", new Vector3(0f, 0.90f, 0.20f), new Vector3(0.44f, 0.60f, 0.06f), apron);
        MakeSphere(chef, "Neck", new Vector3(0f, 1.56f, 0f), new Vector3(0.14f, 0.18f, 0.14f), skin);
        MakeSphere(chef, "Head", new Vector3(0f, 1.78f, 0f), new Vector3(0.29f, 0.32f, 0.28f), skin);
        MakeCylinder(chef, "ChefHatBand", new Vector3(0f, 2.00f, 0f), 0.34f, 0.12f, coat);
        MakeSphere(chef, "ChefHat", new Vector3(0f, 2.12f, 0f), new Vector3(0.36f, 0.22f, 0.34f), coat);
        for (int side = -1; side <= 1; side += 2)
        {
            MakeSegment(chef, "Sleeve", new Vector3(side * 0.25f, 1.39f, 0f), new Vector3(side * 0.39f, 1.00f, 0.18f), 0.17f, coat);
            MakeSegment(chef, "Hand", new Vector3(side * 0.39f, 1.00f, 0.18f), new Vector3(side * 0.34f, 0.82f, 0.36f), 0.10f, skin);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            MakeCylinder(chef, "TrouserLeg", new Vector3(side * 0.15f, 0.43f, 0f), 0.20f, 0.82f, apron);
            MakeCube(chef, "Shoe", new Vector3(side * 0.15f, 0.12f, 0.10f), new Vector3(0.24f, 0.18f, 0.38f), shoes);
        }
    }

    // ---------------------------------------------------------------- metadata
    private static void ApplyCafeMetadata(Transform cafe)
    {
        int count = 0;
        AddMetadata(cafe, "GroundFloor/Counter_Body", "cafe.counter", "Service counter", "service counter",
            "Cafe service", "counter,ordering,payment,service,interactable", "Order or pay at the cafe counter.", ref count);
        AddMetadata(cafe, "GroundFloor/CoffeeMachine", "cafe.coffee_machine", "Coffee machine", "espresso machine",
            "Cafe equipment", "coffee,machine,espresso,barista,equipment", "Ask the chef or barista to prepare a drink.", ref count);
        AddMetadata(cafe, "Facade/Sign_Cafe", "cafe.sign", "Cafe sign", "cafe storefront sign",
            "Signage", "cafe,sign,storefront,entrance", "The sign identifies the cafe entrance.", ref count);

        for (int i = 1; i <= 5; i++)
            AddMetadata(cafe, "GroundFloor/Table_" + i, "cafe.table.indoor." + i, "Indoor cafe table " + i,
                "Indoor seating table", "Cafe furniture", "table,seating,indoor,customer", "Sit here to eat or drink.", ref count);
        for (int i = 1; i <= 3; i++)
            AddMetadata(cafe, "Exterior/Outdoor_" + i, "cafe.table.outdoor." + i, "Outdoor cafe table " + i,
                "Outdoor patio seating table", "Cafe furniture", "table,seating,outdoor,patio,customer", "Sit here in the outdoor cafe area.", ref count);

        Debug.Log("Cafe searchable metadata added to " + count + " counters, equipment, tables, and signs.");
    }

    private static void AddMetadata(Transform root, string path, string id, string displayName, string description,
        string category, string tags, string hint, ref int count)
    {
        Transform item = root.Find(path);
        if (item == null) { Debug.LogWarning("Cafe metadata target not found: " + path); return; }
        ObjectMetadata metadata = item.GetComponent<ObjectMetadata>();
        if (metadata == null) metadata = Undo.AddComponent<ObjectMetadata>(item.gameObject);
        Undo.RecordObject(metadata, "Set cafe object metadata");
        metadata.objectId = id;
        metadata.displayName = displayName;
        metadata.category = category;
        metadata.descriptionSummary = description;
        metadata.contextualTags = tags;
        metadata.isInteractable = true;
        metadata.interactionHint = hint;
        EditorUtility.SetDirty(metadata);
        count++;
    }

    // ---------------------------------------------------------------- visitor route
    private static ClassroomWaypointGraph CreateCafeWaypointGraph()
    {
        GameObject graphRoot = GetOrCreateSceneRoot(GraphRootName);
        ClassroomWaypointGraph graph = graphRoot.GetComponent<ClassroomWaypointGraph>();
        if (graph == null) graph = Undo.AddComponent<ClassroomWaypointGraph>(graphRoot);

        WaypointPreset[] presets =
        {
            new WaypointPreset("cafe_outdoor_entry", "Outdoor cafe entrance", new Vector3(2.5f, 0.05f, -9.0f)),
            new WaypointPreset("cafe_front_door", "Front door", new Vector3(2.5f, 0.05f, -6.3f)),
            new WaypointPreset("cafe_inside_entry", "Inside entrance", new Vector3(2.5f, 0.05f, -4.7f)),
            new WaypointPreset("cafe_left_aisle", "Left aisle", new Vector3(-5.2f, 0.05f, -4.65f)),
            new WaypointPreset("cafe_stairs_bottom", "Stairs bottom", new Vector3(-7.05f, 0.05f, -4.75f)),
            new WaypointPreset("cafe_stair_03", "Stair step 3", new Vector3(-7.05f, 0.65f, -4.25f)),
            new WaypointPreset("cafe_stair_06", "Stair step 6", new Vector3(-7.05f, 1.25f, -3.35f)),
            new WaypointPreset("cafe_stair_09", "Stair step 9", new Vector3(-7.05f, 1.85f, -2.45f)),
            new WaypointPreset("cafe_stair_12", "Stair step 12", new Vector3(-7.05f, 2.45f, -1.55f)),
            new WaypointPreset("cafe_stair_15", "Stair step 15", new Vector3(-7.05f, 3.05f, -0.65f)),
            new WaypointPreset("cafe_upper_landing", "Upper floor landing", new Vector3(-5.4f, 3.65f, 0.55f)),
            new WaypointPreset("cafe_upper_seating", "Upper floor seating", new Vector3(1f, 3.65f, 2.2f))
        };

        List<ClassroomWaypoint> points = new List<ClassroomWaypoint>();
        foreach (WaypointPreset preset in presets)
        {
            ClassroomWaypoint point = FindWaypoint(graphRoot.transform, preset.Id);
            if (point == null)
            {
                GameObject node = new GameObject("Waypoint_" + preset.Id);
                Undo.RegisterCreatedObjectUndo(node, "Create cafe waypoint");
                node.transform.SetParent(graphRoot.transform, false);
                point = Undo.AddComponent<ClassroomWaypoint>(node);
            }
            Undo.RecordObject(point, "Configure cafe waypoint");
            point.transform.position = preset.Position;
            point.waypointId = preset.Id;
            point.displayName = preset.Label;
            point.ClearConnections();
            points.Add(point);
        }

        for (int i = 0; i < points.Count - 1; i++) points[i].ConnectBidirectionally(points[i + 1]);
        ClassroomWaypoint counter = FindWaypoint(graphRoot.transform, "cafe_counter_approach");
        if (counter == null)
        {
            GameObject node = new GameObject("Waypoint_cafe_counter_approach");
            Undo.RegisterCreatedObjectUndo(node, "Create counter waypoint");
            node.transform.SetParent(graphRoot.transform, false);
            counter = Undo.AddComponent<ClassroomWaypoint>(node);
        }
        counter.transform.position = new Vector3(3.6f, 0.05f, 1.0f);
        counter.waypointId = "cafe_counter_approach";
        counter.displayName = "Counter approach";
        counter.ClearConnections();
        counter.ConnectBidirectionally(points[2]);
        graph.SetWaypoints(points);
        List<ClassroomWaypoint> allPoints = new List<ClassroomWaypoint>(points) { counter };
        graph.SetWaypoints(allPoints);
        foreach (ClassroomWaypoint point in allPoints) EditorUtility.SetDirty(point);
        EditorUtility.SetDirty(graph);
        return graph;
    }

    private static ClassroomWaypoint FindWaypoint(Transform root, string id)
    {
        foreach (ClassroomWaypoint point in root.GetComponentsInChildren<ClassroomWaypoint>(true))
            if (point.waypointId == id) return point;
        return null;
    }

    // ---------------------------------------------------------------- player and following demo agent
    private static GameObject CreatePlayer()
    {
        GameObject player = GetOrCreateSceneRoot("Cafe_Player");
        player.transform.position = new Vector3(2.5f, 0f, -13f);
        player.transform.rotation = Quaternion.identity;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null) controller = Undo.AddComponent<CharacterController>(player);
        controller.height = 1.8f;
        controller.radius = 0.28f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = 0.28f;
        controller.slopeLimit = 45f;

        CafeWalkController walk = player.GetComponent<CafeWalkController>();
        if (walk == null) walk = Undo.AddComponent<CafeWalkController>(player);

        GameObject oldCamera = GameObject.Find("Main Camera");
        Color background = new Color(0.02f, 0.03f, 0.07f);
        if (oldCamera != null)
        {
            Camera oldCam = oldCamera.GetComponent<Camera>();
            if (oldCam != null) { background = oldCam.backgroundColor; oldCam.enabled = false; }
            oldCamera.tag = "Untagged";
            AudioListener oldListener = oldCamera.GetComponent<AudioListener>();
            if (oldListener != null) oldListener.enabled = false;
        }

        Transform view = player.transform.Find("PlayerCamera");
        GameObject viewObject;
        if (view == null)
        {
            viewObject = new GameObject("PlayerCamera");
            Undo.RegisterCreatedObjectUndo(viewObject, "Create cafe player camera");
            viewObject.transform.SetParent(player.transform, false);
        }
        else viewObject = view.gameObject;

        Camera camera = viewObject.GetComponent<Camera>();
        if (camera == null) camera = Undo.AddComponent<Camera>(viewObject);
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        camera.fieldOfView = 68f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 250f;
        viewObject.transform.localPosition = new Vector3(0f, 1.58f, 0f);
        viewObject.transform.localRotation = Quaternion.identity;

        Type urpCameraData = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpCameraData != null && viewObject.GetComponent(urpCameraData) == null)
            Undo.AddComponent(viewObject, urpCameraData);
        AudioListener listener = viewObject.GetComponent<AudioListener>();
        if (listener == null) Undo.AddComponent<AudioListener>(viewObject);
        walk.view = viewObject.transform;
        EditorUtility.SetDirty(walk);
        EditorUtility.SetDirty(controller);
        return player;
    }

    private static GameObject CreateFollower(Transform player)
    {
        GameObject follower = GetOrCreateSceneRoot("Cafe_Follow_Agent");
        follower.transform.position = new Vector3(5f, 0f, -13.8f);
        NavMeshAgent agent = follower.GetComponent<NavMeshAgent>();
        if (agent == null) agent = Undo.AddComponent<NavMeshAgent>(follower);
        agent.radius = 0.22f;
        agent.height = 1.8f;
        agent.speed = 2.3f;
        agent.acceleration = 8f;
        agent.angularSpeed = 240f;
        agent.stoppingDistance = 0.9f;
        agent.autoBraking = true;

        CafeFollowPlayer follow = follower.GetComponent<CafeFollowPlayer>();
        if (follow == null) follow = Undo.AddComponent<CafeFollowPlayer>(follower);
        follow.target = player;
        follow.followDistance = 1.5f;

        Transform visual = follower.transform.Find("FollowerVisual");
        if (visual == null)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "FollowerVisual";
            Undo.RegisterCreatedObjectUndo(capsule, "Create follower visual");
            capsule.transform.SetParent(follower.transform, false);
            capsule.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            capsule.transform.localScale = new Vector3(0.40f, 0.9f, 0.40f);
            Collider collider = capsule.GetComponent<Collider>();
            if (collider != null) Undo.DestroyObjectImmediate(collider);
            visual = capsule.transform;
        }
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = GetMaterial("Cafe_Follower_Teal", new Color(0.08f, 0.72f, 0.68f));
        EditorUtility.SetDirty(agent);
        EditorUtility.SetDirty(follow);
        return follower;
    }

    // ---------------------------------------------------------------- NavMesh bake and stair reachability check
    private static NavMeshSurface CreateNavMeshSurface(Transform cafe, Transform player)
    {
        GameObject root = GetOrCreateSceneRoot(NavRootName);
        NavMeshSurface surface = root.GetComponent<NavMeshSurface>();
        if (surface == null) surface = Undo.AddComponent<NavMeshSurface>(root);
        surface.agentTypeID = 0;
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.ignoreNavMeshAgent = true;
        surface.ignoreNavMeshObstacle = true;
        surface.overrideVoxelSize = true;
        // Unity recommends about 8 voxels per 0.5 m agent radius.
        surface.voxelSize = 0.0625f;

        IgnoreDecorativeColliders(cafe, "GroundFloor");
        IgnoreDecorativeColliders(cafe, "UpperFurniture");
        IgnoreDecorativeColliders(cafe, "Exterior");
        IgnoreDecorativeColliders(cafe, "Lights");
        IgnoreDecorativeColliders(cafe, "Facade");
        NavMeshModifier playerModifier = player.GetComponent<NavMeshModifier>();
        if (playerModifier == null) playerModifier = Undo.AddComponent<NavMeshModifier>(player.gameObject);
        playerModifier.ignoreFromBuild = true;
        playerModifier.applyToChildren = true;
        EditorUtility.SetDirty(surface);
        return surface;
    }

    private static void IgnoreDecorativeColliders(Transform cafe, string groupName)
    {
        Transform group = cafe.Find(groupName);
        if (group == null) return;
        NavMeshModifier modifier = group.GetComponent<NavMeshModifier>();
        if (modifier == null) modifier = Undo.AddComponent<NavMeshModifier>(group.gameObject);
        modifier.ignoreFromBuild = true;
        modifier.applyToChildren = true;
        EditorUtility.SetDirty(modifier);
    }

    private static void BakeAndSaveNavMesh(NavMeshSurface surface, Scene scene, Vector3 start, Vector3 destination)
    {
        EnsureFolder("Assets/Navigation");
        NavMeshData existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshAssetPath);
        if (existing == null)
        {
            surface.navMeshData = null;
            surface.BuildNavMesh();
            NavMeshData built = surface.navMeshData;
            if (built == null)
            {
                Debug.LogError("Cafe NavMesh build returned no data. Check that the deck, cafe floors, and stairs have colliders.");
                return;
            }
            built.name = "CafestoreNavMesh";
            AssetDatabase.CreateAsset(built, NavMeshAssetPath);
            FinishBake(surface, scene, built, start, destination);
            return;
        }

        surface.RemoveData();
        surface.navMeshData = existing;
        surface.AddData();
        AsyncOperation operation = surface.UpdateNavMesh(existing);
        if (operation == null || operation.isDone)
        {
            FinishBake(surface, scene, existing, start, destination);
            return;
        }
        EditorApplication.update += WaitForBake;
        void WaitForBake()
        {
            if (operation != null && !operation.isDone) return;
            EditorApplication.update -= WaitForBake;
            FinishBake(surface, scene, existing, start, destination);
        }
    }

    private static void FinishBake(NavMeshSurface surface, Scene scene, NavMeshData data, Vector3 start, Vector3 destination)
    {
        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(surface);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        bool startFound = NavMesh.SamplePosition(start, out NavMeshHit startHit, 3f, NavMesh.AllAreas);
        bool endFound = NavMesh.SamplePosition(destination, out NavMeshHit endHit, 3f, NavMesh.AllAreas);
        NavMeshPath path = new NavMeshPath();
        bool complete = startFound && endFound && NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path)
                        && path.status == NavMeshPathStatus.PathComplete;
        float highestPoint = 0f;
        if (path.corners != null)
            foreach (Vector3 corner in path.corners) highestPoint = Mathf.Max(highestPoint, corner.y);
        bool stairPath = complete && highestPoint > 3.2f;
        Debug.Log("Cafe setup saved: six seated guests, chef, searchable metadata, stair waypoints, and NavMesh. Ground-to-upper-floor path: "
                  + (stairPath ? "reachable over the stairs." : "not verified; see the Cafe NavMesh warning."));
        if (!stairPath)
            Debug.LogWarning("Cafe NavMesh did not produce a complete ground-to-upper-floor route. Check the stair colliders and NavMesh visualization before testing the follower.");
        else
            Debug.Log("Play Cafestore, use WASD and the mouse, and walk up the left staircase. The teal capsule follows you.");
    }

    // ---------------------------------------------------------------- object and material helpers
    private static GameObject GetOrCreateSceneRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) return existing;
        GameObject created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, "Create " + name);
        return created;
    }

    private static Transform NewGroup(Transform parent, string name, Vector3 position, float yaw)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        group.transform.localPosition = position;
        group.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return group.transform;
    }

    private static void MakeCube(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Undo.DestroyObjectImmediate(collider);
    }

    private static void MakeSphere(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Undo.DestroyObjectImmediate(collider);
    }

    private static void MakeCylinder(Transform parent, string name, Vector3 position, float diameter, float height, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Undo.DestroyObjectImmediate(collider);
    }

    private static void MakeSegment(Transform parent, string name, Vector3 start, Vector3 end, float diameter, Material material)
    {
        Vector3 direction = end - start;
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        segment.name = name;
        segment.transform.SetParent(parent, false);
        segment.transform.localPosition = (start + end) * 0.5f;
        segment.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        segment.transform.localScale = new Vector3(diameter, direction.magnitude * 0.5f, diameter);
        Renderer renderer = segment.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        Collider collider = segment.GetComponent<Collider>();
        if (collider != null) Undo.DestroyObjectImmediate(collider);
    }

    private static Material GetMaterial(string name, Color color)
    {
        EnsureFolder(MaterialsFolder);
        string path = MaterialsFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}

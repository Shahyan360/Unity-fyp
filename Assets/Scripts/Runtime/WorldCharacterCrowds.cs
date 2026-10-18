using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Adds conversational café regulars and a small Roman street crowd at runtime.</summary>
public static class WorldCharacterCrowds
{
    private static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
    private static readonly Color[] TunicColors =
    {
        new Color(0.64f, 0.24f, 0.18f), new Color(0.27f, 0.43f, 0.56f),
        new Color(0.67f, 0.53f, 0.30f), new Color(0.37f, 0.50f, 0.35f),
        new Color(0.58f, 0.36f, 0.51f), new Color(0.78f, 0.69f, 0.52f)
    };

    public static void Ensure(string sceneName)
    {
        if (sceneName == "Cafestore") EnsureCafePeople();
        else if (sceneName == "HistoricalPlace") EnsureRomanCrowd();
    }

    private static void EnsureCafePeople()
    {
        GameObject barista = GameObject.Find("Cafe_Chef");
        if (barista != null) SetupCafePerson(barista, "barista", false, 0f, 101);
        int guestIndex = 0;
        foreach (Transform guest in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (guest == null || !guest.name.StartsWith("CafeGuest_", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (guest.parent != null && guest.parent.name.StartsWith("CafeGuest_", System.StringComparison.OrdinalIgnoreCase)) continue;
            bool wander = guestIndex % 3 == 1;
            SetupCafePerson(guest.gameObject, "cafe guest", wander, 1.15f, 200 + guestIndex);
            guestIndex++;
        }
    }

    private static void SetupCafePerson(GameObject person, string role, bool wander, float radius, int seed)
    {
        Transform visualRoot = person.transform.Find("NpcAssetVisual");
        if (visualRoot == null)
        {
            string resource = person.name.IndexOf("woman", System.StringComparison.OrdinalIgnoreCase) >= 0
                ? "ScenarioCharacters/Ada" : "ScenarioCharacters/Marcus";
            if(role=="barista")resource="ScenarioCharacters/Maya";
            GameObject prefab = Resources.Load<GameObject>(resource);
            if (prefab != null)
            {
                foreach (Renderer oldRenderer in person.GetComponentsInChildren<Renderer>(true))
                    if (oldRenderer.transform != person.transform) oldRenderer.enabled = false;
                foreach (Collider oldCollider in person.GetComponentsInChildren<Collider>(true))
                    if (oldCollider.transform != person.transform) oldCollider.enabled = false;
                GameObject visual = Object.Instantiate(prefab, person.transform, false);
                visual.name = "NpcAssetVisual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                foreach (Rigidbody rb in visual.GetComponentsInChildren<Rigidbody>(true)) Object.Destroy(rb);
                foreach (CharacterController cc in visual.GetComponentsInChildren<CharacterController>(true)) cc.enabled = false;
                foreach (MonoBehaviour script in visual.GetComponentsInChildren<MonoBehaviour>(true))
                    script.enabled = false;
                foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
                {
                    animator.enabled = true;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
            }
        }

        AmbientNpcRoutine routine = person.GetComponent<AmbientNpcRoutine>();
        if (routine == null) routine = person.AddComponent<AmbientNpcRoutine>();
        routine.Configure(role, wander, radius, seed);
        var expression=person.GetComponent<AgentAnimator>();if(expression==null)expression=person.AddComponent<AgentAnimator>();else expression.RefreshVisual();

        // Keep these runtime-swapped humanoids' feet on the real floor instead of
        // sinking into it when their NavMesh/seat height differs from the deck.
        Transform npcVisual = person.transform.Find("NpcAssetVisual");
        if (npcVisual != null)
        {
            CharacterGrounding grounding = person.GetComponent<CharacterGrounding>();
            if (grounding == null) grounding = person.AddComponent<CharacterGrounding>();
            grounding.Initialize(npcVisual);
        }
    }

    private static void EnsureRomanCrowd()
    {
        if (GameObject.Find("AncientRome_CitizenCrowd") != null) return;
        GameObject crowd = new GameObject("AncientRome_CitizenCrowd");
        Transform market = FindMarker("Waypoint_west_market_clearance", "market");
        Transform forum = FindMarker("Waypoint_forum_entry", "Roman_Forum_Landmark", "forum");
        Transform arena = FindMarker("Colosseum_Landmark", "colosseum");
        Vector3 marketCenter = GroundPosition(market, new Vector3(-12f, 0f, 4f));
        Vector3 forumCenter = GroundPosition(forum, new Vector3(0f, 0f, 0f));
        Vector3 arenaCenter = GroundPosition(arena, new Vector3(12f, 0f, -10f));

        // Use actual imported humanoids; the old primitive tunic crowd is retired.
        SpawnImportedVisitor(crowd.transform, GroundPosition(null, marketCenter + new Vector3(1.8f, 0f, -1.6f)), 0, "market");
        SpawnImportedVisitor(crowd.transform, GroundPosition(null, forumCenter + new Vector3(-2.3f, 0f, 1.5f)), 1, "forum");
        SpawnImportedVisitor(crowd.transform, GroundPosition(null, arenaCenter + new Vector3(2.2f, 0f, 1.6f)), 2, "Colosseum");
        int citizenId=3;
        foreach (Vector3 position in new[]{new Vector3(-6f,0f,-22f),new Vector3(6f,0f,-23f),new Vector3(-20f,0f,3f),new Vector3(20f,0f,2f),new Vector3(-18f,0f,18f)})
            SpawnImportedVisitor(crowd.transform,GroundPosition(null,position),citizenId++,"city street");
    }

    private static void SpawnImportedVisitor(Transform parent, Vector3 position, int id, string place)
    {
        string style=id%3==0?"RomanGuard":"RomanCitizen";
        string resource = "ScenarioCharacters/"+CharacterWardrobe.Resource(style);
        GameObject prefab = Resources.Load<GameObject>(resource);
        if (prefab == null) return;

        GameObject visitor = new GameObject("RomanCitizen_" + id.ToString("00"));
        visitor.transform.SetParent(parent, true);
        visitor.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, (id * 137) % 360, 0f));
        GameObject visual = Object.Instantiate(prefab, visitor.transform, false);
        visual.name = "Imported Quaternius Humanoid";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) Object.Destroy(collider);
        foreach (MonoBehaviour behaviour in visual.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;
        foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
        {
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        CharacterWardrobe.Apply(visual.transform,style);
        CapsuleCollider select = visitor.AddComponent<CapsuleCollider>();
        select.center = new Vector3(0f, 0.88f, 0f);
        select.height = 1.8f;
        select.radius = 0.36f;
        select.isTrigger = true;
        CharacterGrounding grounding = visitor.GetComponent<CharacterGrounding>();
        if (grounding == null) grounding = visitor.AddComponent<CharacterGrounding>();
        grounding.Initialize(visual.transform);
        AmbientNpcRoutine routine = visitor.AddComponent<AmbientNpcRoutine>();
        routine.Configure("Roman resident", true, 2.8f, 1800 + id);
        visitor.AddComponent<AgentAnimator>();

        ObjectMetadata metadata = visitor.AddComponent<ObjectMetadata>();
        metadata.objectId = "rome_visitor_" + id;
        metadata.displayName = id%3==0?"Roman guard Lucius":"Roman citizen "+id;
        metadata.objectName = metadata.displayName;
        metadata.category = "Person";
        metadata.descriptionSummary = "A fictional clothed Roman resident in this interpretive reconstruction. Ask about daily life, markets and the city; their dialogue is not an ancient eyewitness account.";
        metadata.description = metadata.descriptionSummary;
        metadata.contextualTags = "ancient rome, visitor, researcher, conversation, " + place;
        metadata.isInteractable = true;
        metadata.interactionHint = "Press E to talk with this visitor";
        metadata.scenario = "ancient_rome";
    }

    private static void SpawnGroup(Transform parent, Vector3 center, int count, float radius, string place, ref int id)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = i * 2.399f + (place.Length * 0.11f);
            float distance = radius * Mathf.Sqrt((i + 1f) / (count + 1f));
            Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            Vector3 position = NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : GroundPosition(null, candidate);
            GameObject citizen = CreateCitizen(parent, position, id, place);
            AmbientNpcRoutine routine = citizen.AddComponent<AmbientNpcRoutine>();
            routine.Configure("Roman citizen", true, 3.5f, 800 + id);
            AddCitizenMetadata(citizen, id, place);
            id++;
        }
    }

    private static GameObject CreateCitizen(Transform parent, Vector3 position, int id, string place)
    {
        GameObject root = new GameObject("RomanCitizen_" + id.ToString("00"));
        root.transform.SetParent(parent, true);
        root.transform.position = position;
        Vector3 toward = Quaternion.Euler(0f, (id * 61) % 360, 0f) * Vector3.forward;
        root.transform.rotation = Quaternion.LookRotation(toward);

        Color tunic = TunicColors[id % TunicColors.Length];
        Material cloth = Mat(tunic);
        Material skin = Mat(new Color(0.68f + (id % 3) * 0.055f, 0.47f, 0.34f));
        Material leather = Mat(new Color(0.25f, 0.16f, 0.10f));
        Material hair = Mat(id % 2 == 0 ? new Color(0.18f, 0.13f, 0.10f) : new Color(0.40f, 0.25f, 0.14f));

        MakeBody(root.transform, "Wool tunic", PrimitiveType.Capsule, new Vector3(0f, 1.03f, 0f), new Vector3(0.45f, 0.53f, 0.35f), cloth);
        MakeBody(root.transform, "Leather belt", PrimitiveType.Cube, new Vector3(0f, 0.85f, 0.01f), new Vector3(0.50f, 0.09f, 0.37f), leather);
        MakeBody(root.transform, "Roman citizen head", PrimitiveType.Sphere, new Vector3(0f, 1.67f, 0f), new Vector3(0.36f, 0.39f, 0.35f), skin);
        MakeBody(root.transform, "Short hair", PrimitiveType.Sphere, new Vector3(0f, 1.82f, -0.015f), new Vector3(0.37f, 0.18f, 0.36f), hair);
        MakeBody(root.transform, "Left sandal leg", PrimitiveType.Capsule, new Vector3(-0.13f, 0.34f, 0f), new Vector3(0.15f, 0.37f, 0.16f), skin);
        MakeBody(root.transform, "Right sandal leg", PrimitiveType.Capsule, new Vector3(0.13f, 0.34f, 0f), new Vector3(0.15f, 0.37f, 0.16f), skin);

        MakeArm(root.transform, "Left_Arm", -1, skin, cloth);
        MakeArm(root.transform, "Right_Arm", 1, skin, cloth);
        GameObject satchel = MakeBody(root.transform, "Market satchel", PrimitiveType.Cube, new Vector3(-0.36f, 0.79f, -0.08f), new Vector3(0.19f, 0.24f, 0.17f), leather);
        satchel.transform.localRotation = Quaternion.Euler(0f, 0f, -10f);
        foreach (Collider collider in root.GetComponentsInChildren<Collider>()) Object.Destroy(collider);
        CapsuleCollider select = root.AddComponent<CapsuleCollider>();
        select.center = new Vector3(0f, 0.86f, 0f);
        select.height = 1.86f;
        select.radius = 0.38f;
        select.isTrigger = true;
        return root;
    }

    private static void MakeArm(Transform parent, string name, float side, Material skin, Material tunic)
    {
        GameObject shoulder = new GameObject(name);
        shoulder.transform.SetParent(parent, false);
        shoulder.transform.localPosition = new Vector3(side * 0.29f, 1.32f, 0f);
        MakeBody(shoulder.transform, "Sleeve", PrimitiveType.Capsule, new Vector3(0f, -0.21f, 0f), new Vector3(0.13f, 0.29f, 0.14f), tunic);
        MakeBody(shoulder.transform, "Forearm", PrimitiveType.Capsule, new Vector3(0f, -0.54f, 0.015f), new Vector3(0.11f, 0.23f, 0.11f), skin);
        MakeBody(shoulder.transform, "Hand", PrimitiveType.Sphere, new Vector3(0f, -0.77f, 0.025f), new Vector3(0.14f, 0.14f, 0.12f), skin);
    }

    private static GameObject MakeBody(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }

    private static Transform FindMarker(params string[] candidates)
    {
        foreach (string candidate in candidates)
        {
            GameObject exact = GameObject.Find(candidate);
            if (exact != null) return exact.transform;
        }
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        foreach (string candidate in candidates)
        foreach (Transform item in transforms)
            if (item.name.IndexOf(candidate, System.StringComparison.OrdinalIgnoreCase) >= 0) return item;
        return null;
    }

    private static Vector3 GroundPosition(Transform marker, Vector3 fallback)
    {
        Vector3 point = marker != null ? marker.position : fallback;
        if (NavMesh.SamplePosition(point, out NavMeshHit walkable, 16f, NavMesh.AllAreas)) return walkable.position;
        point.y = Mathf.Max(point.y, 0f);
        if (Physics.Raycast(point + Vector3.up * 14f, Vector3.down, out RaycastHit hit, 40f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            point.y = hit.point.y;
        return point;
    }

    private static void AddCitizenMetadata(GameObject citizen, int id, string place)
    {
        ObjectMetadata data = citizen.AddComponent<ObjectMetadata>();
        data.objectId = "rome_citizen_" + id;
        data.displayName = id % 4 == 0 ? "Market storyteller" : id % 4 == 1 ? "Roman neighbor" : id % 4 == 2 ? "Street vendor" : "Arena visitor";
        data.objectName = data.displayName;
        data.category = "Person";
        data.descriptionSummary = "A resident visiting the " + place + " in this Ancient Rome learning world. Say hello, ask what daily life is like, or ask Lumen about the nearby buildings.";
        data.description = data.descriptionSummary;
        data.contextualTags = "ancient rome, person, daily life, " + place + ", conversation";
        data.isInteractable = true;
        data.interactionHint = "Press E to say hello";
        data.scenario = "ancient_rome";
    }

    private static Material Mat(Color color)
    {
        if (materials.TryGetValue(color, out Material material)) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader) { color = color };
        materials[color] = material;
        return material;
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Places the inspected Blender collection using each world's walkable floor.</summary>
public sealed class BlenderWorldCollection : MonoBehaviour
{
    private string sceneName;
    public static void Install(string scene)
    {
        if (GameObject.Find("BlenderWorldCollection") != null) return;
        var host = new GameObject("BlenderWorldCollection");
        host.AddComponent<BlenderWorldCollection>().sceneName = scene;
    }
    private IEnumerator Start()
    {
        yield return null;
        yield return null;
        if (sceneName == "Classroom") yield break; // Classroom stays empty until a model is requested.
        string resource = sceneName == "Cafestore" ? "EspressoStation" : sceneName == "HistoricalPlace" ? "ForumTable" : "DiscoveryOrrery";
        string label = sceneName == "Cafestore" ? "Espresso station" : sceneName == "HistoricalPlace" ? "Roman architecture miniature" : "Mechanical orrery";
        string description = sceneName == "Cafestore" ? "An espresso machine with two brew groups, portafilters, a pressure gauge, drip tray and steam wand. Ask Maya how espresso is made. Practice orders use the menu."
            : sceneName == "HistoricalPlace" ? "An original tabletop architectural study of colonnaded temples, terracotta roofs, Roman houses and an arcaded amphitheatre. This is an interpretive composition, not an accurate map or scan of ancient Rome."
            : "An original mechanical-style Solar System teaching model with eight planets. Planet sizes, spacing and positions are schematic, not to scale. It is a modern teaching model, not a historical artifact.";
        Camera view = Camera.main;
        if (view == null) yield break;
        Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 feet = view.transform.position - Vector3.up * 1.62f;
        Vector3 candidate = feet + forward * 4f - right * 2.8f;
        if (sceneName.StartsWith("Home")) candidate = new Vector3(0f,0.03f,39.7f);
        else if (sceneName == "Museum") candidate = new Vector3(52.5f,0f,54f); // Interior astronomy corner; keep the arrival path clear.
        if (NavMesh.SamplePosition(candidate,out NavMeshHit nav,2f,NavMesh.AllAreas) && Mathf.Abs(nav.position.y-feet.y)<1f)
            candidate=nav.position;
        if (Physics.Raycast(candidate+Vector3.up*.5f,Vector3.down,out RaycastHit floor,1.8f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            candidate.y=floor.point.y;
        GameObject prefab = Resources.Load<GameObject>("LumenCollection/" + resource);
        if (prefab == null) { Debug.LogWarning("Blender collection prefab missing: " + resource); yield break; }
        GameObject prop = Instantiate(prefab, candidate, Quaternion.LookRotation(-forward), transform);
        prop.name = label;
        var metadata=prop.AddComponent<ObjectMetadata>();
        metadata.objectName=label; metadata.displayName=label; metadata.description=description; metadata.descriptionSummary=description;
        metadata.category="Interactive exhibit"; metadata.scenario=ScenarioRuntimeManager.GetScenarioForScene(sceneName);
        metadata.isInteractable=true; metadata.interactionHint="Ask about " + label;
        var obstacle=prop.AddComponent<NavMeshObstacle>();
        obstacle.shape=NavMeshObstacleShape.Box; obstacle.carving=true;
        var box=prop.GetComponent<BoxCollider>(); if(box!=null) {obstacle.center=box.center;obstacle.size=box.size;}
    }

    public static bool ShowOrrery(Transform parent,Vector3 position)
    {
        GameObject prefab=Resources.Load<GameObject>("LumenCollection/DiscoveryOrrery");
        if(prefab==null) return false;
        GameObject model=Instantiate(prefab,position,Quaternion.identity,parent);
        model.name="Solar System Orrery";
        var metadata=model.AddComponent<ObjectMetadata>(); metadata.displayName="Solar System";metadata.objectName="Solar System";
        metadata.scenario="classroom";metadata.category="Astronomy model";metadata.isInteractable=true;
        metadata.description="A modern orrery: the Sun and eight planets, with deliberately compressed sizes and distances. Earth is third from the Sun. The rods and rings are model mechanisms, not physical tracks in space.";
        metadata.descriptionSummary=metadata.description;metadata.interactionHint="Ask Professor Theo how the Solar System works";
        return true;
    }
}

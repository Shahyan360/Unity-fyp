using UnityEngine;
using UnityEngine.AI;

public static class ScenarioPopulation
{
    public static GameObject Spawn(Transform parent,string label,Vector3 position,string style,string role,bool wander,string scenario)
    {
        GameObject source=Resources.Load<GameObject>("ScenarioCharacters/"+(style=="CafeWoman"?"Ada":style=="Modern"?"Kai":CharacterWardrobe.Resource(style)));
        if(source==null)return null;
        var root=new GameObject(label);root.transform.SetParent(parent,true);
        if(NavMesh.SamplePosition(position,out NavMeshHit hit,2f,NavMesh.AllAreas))position=hit.position;
        root.transform.position=position;
        GameObject visual=Object.Instantiate(source,root.transform,false);visual.name="CitizenVisual";
        foreach(Collider collider in visual.GetComponentsInChildren<Collider>(true))collider.enabled=false;
        if(style!="Modern"&&style!="CafeWoman")CharacterWardrobe.Apply(visual.transform,style);
        var capsule=root.AddComponent<CapsuleCollider>();capsule.center=Vector3.up*.85f;capsule.height=1.7f;capsule.radius=.3f;capsule.isTrigger=true;
        var data=root.AddComponent<ObjectMetadata>();data.objectName=label;data.displayName=label;data.category="Person";
        data.scenario=scenario;data.isInteractable=true;data.descriptionSummary=label+" is a fictional "+role+" in this interactive scene.";
        data.objectId=scenario+"_"+label.Replace(" ","_").ToLowerInvariant();
        data.description=data.descriptionSummary;data.contextualTags=scenario+", person, "+role;data.interactionHint="Press E to greet "+label;
        var grounding=root.AddComponent<CharacterGrounding>();grounding.Initialize(visual.transform);
        var routine=root.AddComponent<AmbientNpcRoutine>();routine.Configure(role,wander,wander?3f:.6f,label.GetHashCode());
        root.AddComponent<AgentAnimator>();
        return root;
    }
}

using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Resolve visitor destinations to reachable floor, rather than the centre of a solid exhibit.</summary>
public static class WorldDestination
{
    public static bool TryResolve(string name,Vector3 origin,out Vector3 destination)
    {
        destination=origin;float best=float.PositiveInfinity;bool found=false;
        string query=name.ToLowerInvariant().Replace("the ","").Trim();
        if(query.Contains("coliseum")||query.Contains("colosseum")||query.Contains("arena")||query.Contains("fight"))query="arena viewing";
        foreach(var item in UnityEngine.Object.FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Exclude))
        {
            if(!item.enabled||!item.isInteractable||item.category=="Person")continue;
            string label=item.GetDisplayName().ToLowerInvariant();
            if(!label.Contains(query)&&!query.Contains(label))continue;
            if(label.Contains("walls")||label.Contains("waypoint"))continue;
            for(int i=0;i<(item.category=="Destination"?1:17);i++)
            {
                float angle=(i-1)*Mathf.PI/4;float radius=i==0?0:i<9?1.6f:3f;
                var candidate=item.transform.position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                if(!NavMesh.SamplePosition(candidate,out var hit,1.5f,NavMesh.AllAreas))continue;
                if(!NavMesh.SamplePosition(origin,out var from,3,NavMesh.AllAreas))continue;
                var path=new NavMeshPath();
                if(!NavMesh.CalculatePath(from.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                float score=0;for(int j=1;j<path.corners.Length;j++)score+=Vector3.Distance(path.corners[j-1],path.corners[j]);
                if(score>=best)continue;best=score;destination=hit.position;found=true;
            }
        }
        return found;
    }
}

using UnityEngine;
using UnityEngine.AI;

/// <summary>Tracks the actual motor and destination; no outcome inferred from a parsed sentence.</summary>
public sealed class WorldActionExecutionMonitor : MonoBehaviour
{
    public WorldActionExecution Execution {get;private set;}
    private Vector3 target,previous;
    private float started,lastProgress;
    private VoicePlayerNavigation playerMotor;
    private NavMeshAgent guideMotor;
    public void Begin(WorldActionExecution execution,Vector3 destination,bool visitor)
    {
        if(Execution!=null&&!Execution.Terminal)Execution.Set("cancelled","A newer route replaced this walk.");
        Execution=execution;target=destination;previous=transform.position;started=lastProgress=Time.unscaledTime;
        playerMotor=visitor?GetComponent<VoicePlayerNavigation>():null;
        guideMotor=visitor?null:GetComponent<NavMeshAgent>();
        execution.Set("moving","A complete route was accepted; arrival is still pending.");
    }
    private void Update()
    {
        if(Execution==null||Execution.Terminal)return;
        Vector3 difference=target-transform.position;difference.y=0;
        if(difference.magnitude < (playerMotor != null ? .5f : guideMotor != null ? guideMotor.stoppingDistance+.25f : .5f))
        {Execution.Set("completed","Arrived at "+Execution.target+".");return;}
        if(playerMotor!=null&&!playerMotor.IsWalking)
        {Execution.Set("cancelled","Walking stopped before arrival.");return;}
        if(guideMotor!=null&&(!guideMotor.enabled||!guideMotor.isOnNavMesh||(!guideMotor.pathPending&&guideMotor.pathStatus!=NavMeshPathStatus.PathComplete)))
        {Execution.Set("failed","The route is no longer traversable.");return;}
        if(Vector3.Distance(previous,transform.position)>.08f){lastProgress=Time.unscaledTime;previous=transform.position;}
        if(Time.unscaledTime-lastProgress>5||Time.unscaledTime-started>90)
        {
            playerMotor?.Cancel();
            if(guideMotor!=null&&guideMotor.enabled&&guideMotor.isOnNavMesh)guideMotor.ResetPath();
            Execution.Set("failed","Walking could not make progress; the route may be obstructed.");
        }
    }
    public void Cancel(string reason)
    {
        if(Execution!=null&&!Execution.Terminal)Execution.Set("cancelled",reason);
    }
    private void OnDisable()=>Cancel("The world or conversation route changed before arrival.");
}

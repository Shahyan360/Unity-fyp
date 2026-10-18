using UnityEngine;
using UnityEngine.AI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Optional spoken player walking along a complete NavMesh path; manual input cancels.</summary>
public sealed class VoicePlayerNavigation:MonoBehaviour
{
    private CafeWalkController walk;private CharacterController body;
    private Vector3[] corners;private int corner;private float stuckAt;private Vector3 previous;
    public bool IsWalking=>corners!=null;
    public bool Go(Vector3 destination)
    {
        walk=GetComponent<CafeWalkController>();body=GetComponent<CharacterController>();
        if(walk==null||body==null||!NavMesh.SamplePosition(transform.position,out NavMeshHit from,3,NavMesh.AllAreas)||!NavMesh.SamplePosition(destination,out NavMeshHit to,2.5f,NavMesh.AllAreas))return false;
        var path=new NavMeshPath();if(!NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)return false;
        corners=path.corners;corner=1;walk.enabled=false;previous=transform.position;stuckAt=Time.unscaledTime;return true;
    }
    public void Cancel(){corners=null;if(walk!=null)walk.enabled=true;}
    private void Update()
    {
        if(corners==null)return;
#if ENABLE_INPUT_SYSTEM
        var k=Keyboard.current;if(k!=null&&(k.wKey.isPressed||k.aKey.isPressed||k.sKey.isPressed||k.dKey.isPressed||k.escapeKey.wasPressedThisFrame)){Cancel();return;}
#endif
        if(corner>=corners.Length){Cancel();return;}
        var delta=corners[corner]-transform.position;delta.y=0;
        if(delta.magnitude<.25f){corner++;return;}
        transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),Time.deltaTime*3f);
        body.Move((delta.normalized*walk.walkSpeed+Vector3.down*2)*Time.deltaTime);
        if(Vector3.Distance(previous,transform.position)>.1f){previous=transform.position;stuckAt=Time.unscaledTime;}
        if(Time.unscaledTime-stuckAt>3)Cancel();
    }
    private void OnDisable(){Cancel();}
}

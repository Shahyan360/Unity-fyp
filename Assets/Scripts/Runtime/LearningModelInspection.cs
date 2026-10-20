using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Reversible camera inspection and bounded model rotation; the visitor capsule never teleports.</summary>
public sealed class LearningModelInspection:MonoBehaviour
{
    public static LearningModelInspection Current {get;private set;}
    public bool IsInspecting {get;private set;}
    private Transform view;
    private CafeWalkController motor;
    private Vector3 savedPosition,direction;
    private Quaternion savedRotation;
    private bool wasEnabled;
    private float distance=2.8f;
    private Vector3 localFocus;
    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private float minimumDistance=1.8f,maximumDistance=4.2f,initialDistance=2.8f;
    private Vector3 Focus=>transform.TransformPoint(localFocus);
    public static void MakeSelectable(GameObject exhibit)
    {
        var renderers=exhibit.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;
        Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
        var inspection=exhibit.GetComponent<LearningModelInspection>()??exhibit.AddComponent<LearningModelInspection>();
        inspection.localFocus=exhibit.transform.InverseTransformPoint(bounds.center);inspection.initialRotation=exhibit.transform.rotation;inspection.initialPosition=exhibit.transform.position;
        inspection.minimumDistance=Mathf.Max(1.2f,bounds.extents.magnitude*.50f);inspection.maximumDistance=Mathf.Max(4.2f,bounds.extents.magnitude*2.2f);
        inspection.initialDistance=inspection.distance=Mathf.Max(2.8f,bounds.extents.magnitude*1.3f);
    }
    public void Select(){if(Current==null||!Current.IsInspecting)Current=this;}
    public static void Attach(Transform model,Vector3 focus)
    {
        var pivot=new GameObject("Learning model inspection pivot");pivot.transform.SetParent(model.parent,false);pivot.transform.position=focus;
        model.SetParent(pivot.transform,true);Current=pivot.AddComponent<LearningModelInspection>();
        Current.initialPosition=pivot.transform.position;Current.initialRotation=pivot.transform.rotation;
    }
    public void Toggle(){if(IsInspecting)Exit();else Enter();}
    public void Enter()
    {
        if(IsInspecting||Camera.main==null)return;
        view=Camera.main.transform;motor=FindAnyObjectByType<CafeWalkController>();wasEnabled=motor!=null&&motor.enabled;
        savedPosition=view.localPosition;savedRotation=view.localRotation;
        direction=view.position-Focus;direction.y=0;if(direction.sqrMagnitude<.1f)direction=Vector3.back;direction.Normalize();
        if(motor!=null)motor.enabled=false;IsInspecting=true;ApplyView();
        var metadata=GetComponent<ObjectMetadata>()??GetComponentInChildren<ObjectMetadata>();if(metadata!=null)ExplorationNotebook.RecordExhibit(metadata.GetDisplayName());
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public void Rotate(){transform.RotateAround(Focus,Vector3.up,30);if(IsInspecting)ApplyView();}
    public void Zoom(){if(!IsInspecting)Enter();distance=distance<minimumDistance+.2f?maximumDistance:Mathf.Max(minimumDistance,distance-.6f);ApplyView();}
    public void ResetView(){transform.SetPositionAndRotation(initialPosition,initialRotation==default?Quaternion.identity:initialRotation);distance=initialDistance;if(IsInspecting)ApplyView();}
    private void ApplyView()
    {
        if(!IsInspecting||view==null)return;
        view.position=Focus+direction*Mathf.Clamp(distance,minimumDistance,maximumDistance)+Vector3.up*.25f;
        view.LookAt(Focus);
    }
    public void Exit()
    {
        if(!IsInspecting)return;
        IsInspecting=false;if(view!=null){view.localPosition=savedPosition;view.localRotation=savedRotation;}
        if(motor!=null)motor.enabled=wasEnabled;
    }
    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if(IsInspecting&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Exit();
#endif
    }
    private void OnDisable()=>Exit();
    private void OnDestroy(){Exit();if(Current==this)Current=null;}
}

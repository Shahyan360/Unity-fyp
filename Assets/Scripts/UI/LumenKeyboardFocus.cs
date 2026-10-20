using UnityEngine;
using UnityEngine.EventSystems;

public sealed class LumenKeyboardFocus : MonoBehaviour,ISelectHandler,IDeselectHandler
{
    private UnityEngine.UI.Outline outline;
    private void Awake()
    {
        outline=gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(1,.79f,.39f);outline.effectDistance=new Vector2(2,-2);outline.enabled=false;
    }
    public void OnSelect(BaseEventData data){if(outline!=null)outline.enabled=true;}
    public void OnDeselect(BaseEventData data){if(outline!=null)outline.enabled=false;}
}

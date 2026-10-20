using TMPro;
using UnityEngine;

public sealed class GuideTargetLabel:MonoBehaviour
{
    private TMP_Text label;
    private float next;
    private void Awake()=>label=GetComponentInChildren<TMP_Text>();
    private void Update()
    {
        if(Time.unscaledTime<next||label==null)return;next=Time.unscaledTime+.2f;
        var live=GeminiLiveConversation.Instance;
        label.text=live?.Partner!=null?live.Partner.GetDisplayName()+" / selected":"Guide: "+(FindAnyObjectByType<ScenarioCharacterIdentity>()?.DisplayName??"Kai");
    }
}

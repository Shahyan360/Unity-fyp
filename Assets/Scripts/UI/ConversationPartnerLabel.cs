using TMPro;
using UnityEngine;
public sealed class ConversationPartnerLabel:MonoBehaviour
{
    private TMP_Text label;
    private void Awake(){label=GetComponentInChildren<TMP_Text>();}
    private void Update(){if(label!=null)label.text=GeminiLiveConversation.Instance?.Partner!=null?"Return to companion":GeminiLiveConversation.Instance?.Ready==true?"Companion selected":"Retry assistant";}
}

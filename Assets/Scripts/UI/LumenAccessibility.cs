using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Scene-owned readable UI, focus navigation and explicit visitor presentation preferences.</summary>
public sealed class LumenAccessibility : MonoBehaviour
{
    public static bool MenuOpen {get;set;}
    public static bool ReducedMotion=>LumenPreferences.GetInt("Lumen.ReducedMotion",0)!=0;
    public static int MovementLayout=>LumenPreferences.GetInt("Lumen.MovementLayout",0);
    public static int TextTier=>LumenPreferences.GetInt("Lumen.TextTier",0);
    public static string MovementLabel=>MovementLayout==1?"Arrow keys":MovementLayout==2?"I J K L":"W A S D";
    private readonly Dictionary<TMP_Text,float> baseSizes=new Dictionary<TMP_Text,float>();
    private float nextRefresh;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded-=OnScene;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded+=OnScene;
        Ensure();
    }
    private static void OnScene(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode)=>Ensure();
#if ENABLE_INPUT_SYSTEM
    public static Vector2 ReadMovement(Keyboard k)
    {
        if(k==null)return Vector2.zero;
        if(MovementLayout==1)return new Vector2((k.rightArrowKey.isPressed?1:0)-(k.leftArrowKey.isPressed?1:0),(k.upArrowKey.isPressed?1:0)-(k.downArrowKey.isPressed?1:0));
        if(MovementLayout==2)return new Vector2((k.lKey.isPressed?1:0)-(k.jKey.isPressed?1:0),(k.iKey.isPressed?1:0)-(k.kKey.isPressed?1:0));
        return new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
    }
#endif
    private static void Ensure()
    {
        MenuOpen=false;
        if(FindAnyObjectByType<LumenAccessibility>()==null)new GameObject("Lumen accessibility").AddComponent<LumenAccessibility>();
    }
    public static void Refresh(){var current=FindAnyObjectByType<LumenAccessibility>();if(current!=null)current.nextRefresh=0;}
    private void Update()
    {
        if(Time.unscaledTime>=nextRefresh)
        {
            nextRefresh=Time.unscaledTime+1;
            foreach(var label in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
            {
                if(!baseSizes.TryGetValue(label,out float size)){size=label.fontSize;baseSizes[label]=size;}
                var canvas=label.canvas;if(canvas==null)continue;
                float scale=TextTier==1?1.18f:TextTier==2?1.35f:1;
                label.enableAutoSizing=false;
                label.fontSize=Mathf.Max(size,16/Mathf.Max(.4f,canvas.scaleFactor))*scale;
            }
            FindAnyObjectByType<HomeUIManager>()?.RefreshReadability();
            foreach(var button in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include))
            {
                var colors=button.colors;colors.highlightedColor=new Color(.42f,.68f,.67f);colors.selectedColor=new Color(.46f,.72f,.70f);colors.pressedColor=new Color(.24f,.45f,.44f);colors.disabledColor=new Color(.25f,.29f,.29f,.55f);button.colors=colors;
                if(button.GetComponent<LumenKeyboardFocus>()==null)button.gameObject.AddComponent<LumenKeyboardFocus>();
            }
            foreach(var dead in baseSizes.Keys.Where(t=>t==null).ToArray())baseSizes.Remove(dead);
        }
#if ENABLE_INPUT_SYSTEM
        var keyboard=Keyboard.current;
        if(keyboard!=null&&keyboard.tabKey.wasPressedThisFrame)
        {
            var system=EventSystem.current;if(system==null)return;
            var items=Selectable.allSelectablesArray.Where(s=>s.IsActive()&&s.IsInteractable()&&s.gameObject.activeInHierarchy)
                .Where(s=>s.GetComponentsInParent<CanvasGroup>().All(g=>g.interactable&&g.alpha>.05f))
                .OrderByDescending(s=>s.transform.position.y).ThenBy(s=>s.transform.position.x).ToArray();
            if(items.Length==0)return;
            int index=System.Array.FindIndex(items,s=>s.gameObject==system.currentSelectedGameObject);
            bool reverse=keyboard.leftShiftKey.isPressed;
            index=index<0?(reverse?items.Length-1:0):(index+(reverse?-1:1)+items.Length)%items.Length;
            system.SetSelectedGameObject(items[index].gameObject);
        }
        if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame&&MenuOpen)
        {
            GameObject.Find("Guide choices")?.SetActive(false);GameObject.Find("VisitSettings")?.SetActive(false);MenuOpen=false;
        }
#endif
    }
}

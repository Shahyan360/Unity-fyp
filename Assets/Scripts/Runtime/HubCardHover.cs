using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Adds a restrained lift when a scenario card receives pointer focus.</summary>
public sealed class HubCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rect;
    private Vector3 baseScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;
    private float phase;
    private RectTransform illustration;

    private void Awake() => rect = transform as RectTransform;
    private void OnEnable()
    {
        if (rect == null) rect = transform as RectTransform;
        baseScale = rect != null ? rect.localScale : Vector3.one;
        targetScale = baseScale;
        illustration = transform.Find("IllustrationWash") as RectTransform;
    }
    private void Update()
    {
        if (rect != null) rect.localScale = Vector3.Lerp(rect.localScale, targetScale, Time.unscaledDeltaTime * 12f);
        phase += Time.unscaledDeltaTime;
        if (illustration != null)
        {
            float pulse = 1f + Mathf.Sin(phase * 1.8f) * 0.018f;
            illustration.localScale = Vector3.one * pulse;
        }
    }
    public void OnPointerEnter(PointerEventData eventData) => targetScale = baseScale * 1.025f;
    public void OnPointerExit(PointerEventData eventData) => targetScale = baseScale;
}

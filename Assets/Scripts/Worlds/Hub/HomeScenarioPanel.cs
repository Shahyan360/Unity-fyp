using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the home-screen world picker quiet until the learner asks for it,
/// then reveals it with a short, staggered motion that feels intentional.
/// </summary>
public sealed class HomeScenarioPanel : MonoBehaviour
{
    private readonly List<RectTransform> cards = new List<RectTransform>();
    private readonly List<Vector3> cardScales = new List<Vector3>();
    private CanvasGroup panelGroup;
    private RectTransform panelRect;
    private RectTransform revealRect;
    private TMP_Text revealLabel;
    private bool isOpen;
    private float animation;
    private float targetAnimation;

    public bool IsOpen => isOpen;

    public void Initialize(RectTransform panel, Button revealButton)
    {
        panelRect = panel;
        panelGroup = panel != null ? panel.GetComponent<CanvasGroup>() : null;
        revealRect = revealButton != null ? revealButton.transform as RectTransform : null;
        revealLabel = revealButton != null ? revealButton.GetComponentInChildren<TMP_Text>() : null;

        if (revealButton != null)
        {
            revealButton.onClick.RemoveListener(Toggle);
            revealButton.onClick.AddListener(Toggle);
        }

        SetClosedImmediate();
    }

    public void RegisterCard(RectTransform card)
    {
        if (card == null || cards.Contains(card)) return;
        cards.Add(card);
        cardScales.Add(card.localScale);
        card.localScale = Vector3.one * 0.84f;
    }

    public void Toggle()
    {
        if (isOpen) Hide();
        else Reveal();
    }

    public void Reveal()
    {
        isOpen = true;
        targetAnimation = 1f;
        if (panelGroup != null)
        {
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
        }
        if (revealLabel != null) revealLabel.text = "WORLDS  /  CLOSE";
    }

    public void Hide()
    {
        isOpen = false;
        targetAnimation = 0f;
        if (revealLabel != null) revealLabel.text = "CHOOSE A WORLD";
    }

    private void SetClosedImmediate()
    {
        isOpen = false;
        animation = 0f;
        targetAnimation = 0f;
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
        if (panelRect != null) panelRect.localScale = Vector3.one * 0.965f;
        if (revealLabel != null) revealLabel.text = "CHOOSE A WORLD";
        for (int i = 0; i < cards.Count; i++) cards[i].localScale = Vector3.one * 0.84f;
    }

    private void Update()
    {
        float speed = isOpen ? 5.4f : 7.4f;
        animation = LumenAccessibility.ReducedMotion?targetAnimation:Mathf.MoveTowards(animation, targetAnimation, Time.unscaledDeltaTime * speed);
        float eased = animation * animation * (3f - 2f * animation);

        if (panelGroup != null)
        {
            panelGroup.alpha = eased;
            panelGroup.interactable = isOpen && eased > 0.92f;
            panelGroup.blocksRaycasts = isOpen || eased > 0.02f;
        }
        if (panelRect != null) panelRect.localScale = Vector3.Lerp(Vector3.one * 0.965f, Vector3.one, eased);
        if (revealRect != null)
        {
            float pulse = LumenAccessibility.ReducedMotion?1f:1f + Mathf.Sin(Time.unscaledTime * 2.1f) * (isOpen ? 0.006f : 0.018f);
            revealRect.localScale = Vector3.one * pulse;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            float cardProgress = Mathf.Clamp01((eased - i * 0.08f) / 0.84f);
            cardProgress = cardProgress * cardProgress * (3f - 2f * cardProgress);
            cards[i].localScale = Vector3.Lerp(Vector3.one * 0.84f, cardScales[i], cardProgress);
        }
    }
}

using UnityEngine;

public class UIGradientPulse : MonoBehaviour
{
    [Header("Pulse")]
    public float pulseAmount = 0.04f;
    public float pulseSpeed = 2f;

    private Vector3 startScale;

    private void Start()
    {
        startScale = transform.localScale;
    }

    private void Update()
    {
        // This creates a soft glow-like pulse by scaling the UI panel slightly.
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = startScale * pulse;
    }
}


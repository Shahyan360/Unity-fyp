using UnityEngine;

/// <summary>Temporarily highlights a learning exhibit when the assistant brings it up.</summary>
public sealed class LearningModelPulse : MonoBehaviour
{
    private Vector3 baseScale;
    private float pulseUntil;

    private void Awake() => baseScale = transform.localScale;
    public void Play() { baseScale = transform.localScale; pulseUntil = Time.time + 3f; }
    private void Update()
    {
        if (Time.time > pulseUntil)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * 7f);
            return;
        }
        float wave = 1f + Mathf.Sin(Time.time * 8f) * 0.055f;
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * wave, Time.deltaTime * 9f);
    }
}

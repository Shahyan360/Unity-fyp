using UnityEngine;
using UnityEngine.AI;

/// <summary>Small, allocation-free floor probes shared by character recovery and visual alignment.</summary>
public static class WalkableFloorProbe
{
    // Queries run sequentially on Unity's main thread. Saturated results are rejected
    // rather than treating an arbitrary truncated hit list as reliable support.
    private static readonly RaycastHit[] Hits = new RaycastHit[48];

    public static bool TryFind(Transform characterRoot, Vector3 feet, float above, float below, out RaycastHit floor)
    {
        floor = default;
        int count = Physics.RaycastNonAlloc(feet + Vector3.up * above, Vector3.down, Hits,
            above + below, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (count == Hits.Length) return false;
        float highest = float.NegativeInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = Hits[i];
            Collider collider = hit.collider;
            if (collider == null || hit.normal.y < 0.6f || hit.point.y > feet.y + above) continue;
            Transform surface = collider.transform;
            if (characterRoot != null && (surface == characterRoot || surface.IsChildOf(characterRoot))) continue;
            if (collider is CharacterController || collider.GetComponentInParent<NavMeshAgent>() != null) continue;
            Rigidbody body = collider.attachedRigidbody;
            if (body != null && !body.isKinematic) continue;
            if (hit.point.y <= highest) continue;
            highest = hit.point.y;
            floor = hit;
        }
        return !float.IsNegativeInfinity(highest);
    }
}

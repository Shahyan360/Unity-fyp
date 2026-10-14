using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Keeps a character's feet resting exactly on the real physics floor.
///
/// Built-in <see cref="NavMeshAgent"/>s (and runtime-seated guests) anchor the
/// container's origin to the NavMesh/seat height, which can sit a little above
/// or below the visible floor collider — and some imported rigs have their pivot
/// slightly off the feet. The result is characters that float a little or sink
/// through the floor (worst in the two-floor café). This component leaves the
/// NavMeshAgent in charge of horizontal movement and only nudges the VISUAL mesh
/// vertically so its lowest point sits on the floor. It never moves the
/// NavMeshAgent, so it cannot fight navigation, and it is a no-op when no floor
/// is found — normal walking, stairs, and upper floors are unaffected.
/// </summary>
[DisallowMultipleComponent]
public sealed class CharacterGrounding : MonoBehaviour
{
    [Tooltip("The child mesh root whose feet should touch the floor. Defaults to this transform.")]
    [SerializeField] private Transform visual;
    [Tooltip("Extra lift applied on top of the measured foot height (metres).")]
    [SerializeField] private float extraLift = 0.01f;

    // How far below the visual pivot the feet/lowest point sit (metres), measured once.
    private float pivotToFeet;
    private bool measured;
    private Transform visualRoot;
    private NavMeshAgent cachedAgent;

    // Smoothing prevents oscillation when the floor raycast or NavMesh height
    // fluctuates slightly between frames (the main cause of the bobbing bug).
    private float smoothedFloorY = float.NaN;
    private const float SmoothSpeed = 12f;

    public void Initialize(Transform visualRoot)
    {
        this.visualRoot = visualRoot != null ? visualRoot : transform;
        visual = this.visualRoot;
        measured = false;
        smoothedFloorY = float.NaN;
    }

    private void OnEnable()
    {
        if (visual == null) visual = transform;
        cachedAgent = GetComponent<NavMeshAgent>();
        StartCoroutine(MeasureAfterPose());
    }

    // Wait a frame so the Animator has posed the rig before measuring the feet.
    private IEnumerator MeasureAfterPose()
    {
        yield return null;
        if (visual == null || !this) yield break;
        float lowest = float.PositiveInfinity;
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;
            lowest = Mathf.Min(lowest, renderer.bounds.min.y);
        }
        if (float.IsInfinity(lowest)) lowest = visual.position.y; // no mesh yet; assume pivot = feet
        pivotToFeet = visual.position.y - lowest;               // >= 0 for feet-pivot and hips-pivot rigs
        if (pivotToFeet < 0f) pivotToFeet = 0f;
        measured = true;
    }

    private void LateUpdate()
    {
        if (visual == null || !measured) return;

        // If the NavMeshAgent is active and on the mesh, trust its Y position
        // completely. The NavMesh already handles floor following; adding a
        // raycast-based correction on top causes the two systems to fight each
        // other, producing the sink-and-recover bobbing loop.
        if (cachedAgent == null) cachedAgent = GetComponent<NavMeshAgent>();
        if (cachedAgent != null && cachedAgent.enabled && cachedAgent.isOnNavMesh)
        {
            // Only correct the visual child's LOCAL offset relative to the
            // agent root — never fight the NavMeshAgent's world position.
            if (visual != transform)
            {
                Vector3 localPos = visual.localPosition;
                // Gently zero out any vertical drift in the visual child.
                if (Mathf.Abs(localPos.y) > 0.002f)
                    visual.localPosition = new Vector3(localPos.x, Mathf.Lerp(localPos.y, 0f, Time.deltaTime * SmoothSpeed), localPos.z);
            }
            return;
        }

        // For characters WITHOUT a NavMeshAgent, use the floor raycast.
        if (!TryFindFloorY(transform.position, out float floorY)) return;

        // If the detected floor is more than 1.2m above or below the character's authored
        // position (e.g. upper floor vs ground floor in the cafe), ignore it to avoid
        // teleporting between stories.
        if (Mathf.Abs(floorY - transform.position.y) > 1.2f) return;

        // Smooth the floor height to prevent frame-to-frame jitter / bobbing.
        if (float.IsNaN(smoothedFloorY))
            smoothedFloorY = floorY;
        else
            smoothedFloorY = Mathf.Lerp(smoothedFloorY, floorY, Time.deltaTime * SmoothSpeed);

        float targetY = smoothedFloorY + pivotToFeet + extraLift;
        Vector3 current = visual.position;
        if (Mathf.Abs(current.y - targetY) < 0.002f) return;
        visual.position = new Vector3(current.x, Mathf.Lerp(current.y, targetY, Time.deltaTime * SmoothSpeed), current.z);
    }

    /// <summary> Finds the floor a character is standing on: the lowest solid
    /// surface at or just below foot level, ignoring triggers, furniture above
    /// the body, and other NPCs. Works with or without a NavMesh. </summary>
    private static bool TryFindFloorY(Vector3 feetPosition, out float floorY)
    {
        // Cast from well above the character to catch floors even when the
        // character has sunk below the surface.
        float feetPlane = feetPosition.y + 0.25f;
        Vector3 origin = new Vector3(feetPosition.x, feetPosition.y + 2.5f, feetPosition.z);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 5f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float best = float.NegativeInfinity;
        for (int i = 0; i < hits.Length; i++)
        {
            // Accept only surfaces at or below the feet plane so a table/desk
            // above the character is never treated as the floor.
            // Also require the surface normal to be mostly upward (floor-like).
            if (hits[i].point.y <= feetPlane && hits[i].point.y > best && hits[i].normal.y >= 0.5f)
                best = hits[i].point.y;
        }
        if (best == float.NegativeInfinity && NavMesh.SamplePosition(feetPosition, out NavMeshHit nav, 2.5f, NavMesh.AllAreas))
            best = nav.position.y; // physics floor not found (e.g. mid-transition); fall back to NavMesh
        floorY = best;
        return best != float.NegativeInfinity;
    }
}

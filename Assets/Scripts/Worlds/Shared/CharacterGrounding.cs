using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Aligns only a character's visual child with the nearby floor, preserving navigation ownership.</summary>
[DisallowMultipleComponent]
public sealed class CharacterGrounding : MonoBehaviour
{
    [Tooltip("The child mesh root whose feet should touch the floor. The movement root is never repositioned.")]
    [SerializeField] private Transform visual;
    [Tooltip("Extra lift applied on top of the measured foot height (metres).")]
    [SerializeField] private float extraLift = 0.01f;

    private float pivotToFeet;
    private bool measured;
    private NavMeshAgent cachedAgent;
    private Coroutine measurement;
    private float nextFloorProbe;
    private float floorOffset;
    private bool hasFloor;
    private const float SmoothSpeed = 12f;
    private const float ProbeInterval = 0.1f;

    public void Initialize(Transform visualRoot)
    {
        visual = visualRoot;
        measured = false;
        hasFloor = false;
        if (isActiveAndEnabled) BeginMeasurement();
    }

    private void OnEnable()
    {
        cachedAgent = GetComponent<NavMeshAgent>();
        BeginMeasurement();
    }

    private void OnDisable()
    {
        if (measurement != null) StopCoroutine(measurement);
        measurement = null;
    }

    private void BeginMeasurement()
    {
        if (measurement != null) StopCoroutine(measurement);
        measured = false;
        if (visual != null && visual != transform && visual.IsChildOf(transform))
            measurement = StartCoroutine(MeasureAfterPose());
    }

    private IEnumerator MeasureAfterPose()
    {
        yield return null;
        if (visual == null) yield break;
        float lowest = float.PositiveInfinity;
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
        {
            // Disabled placeholders, particles and world-space labels must not
            // change the avatar's calibrated foot height.
            if (!renderer.enabled || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer)) continue;
            if(renderer.GetComponent<WardrobeMeshLifetime>()!=null)continue; // Cloth bounds change during posing; source feet remain the calibration anchor.
            if(renderer.GetComponent<TailoredClothingMesh>()!=null)continue;
            lowest = Mathf.Min(lowest, renderer.bounds.min.y);
        }
        if (float.IsPositiveInfinity(lowest)) lowest = visual.position.y;
        // Negative offsets are valid for imported rigs with feet above the pivot.
        pivotToFeet = visual.position.y - lowest;
        measured = true;
        nextFloorProbe = 0f;
        measurement = null;
    }

    private void LateUpdate()
    {
        if (visual == null || visual == transform || !measured) return;
        if (Time.time >= nextFloorProbe)
        {
            nextFloorProbe = Time.time + ProbeInterval;
            if (cachedAgent == null) cachedAgent = GetComponent<NavMeshAgent>();
            Vector3 anchor = transform.position;
            if (cachedAgent != null && cachedAgent.enabled && cachedAgent.isOnNavMesh)
                anchor.y -= cachedAgent.baseOffset;
            hasFloor = WalkableFloorProbe.TryFind(transform, anchor, 0.35f, 0.65f, out RaycastHit floor);
            floorOffset = hasFloor ? floor.point.y - transform.position.y : 0f;
        }
        if (!hasFloor) return;
        float targetY = transform.position.y + floorOffset + pivotToFeet + extraLift;
        Vector3 current = visual.position;
        if (Mathf.Abs(current.y - targetY) < 0.002f) return;
        float blend = 1f - Mathf.Exp(-SmoothSpeed * Time.deltaTime);
        visual.position = new Vector3(current.x, Mathf.Lerp(current.y, targetY, blend), current.z);
    }
}

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene-wide, last-resort grounding safety net (Priority 1).
/// The per-character controllers/agents already snap on spawn and recover on
/// their own (<see cref="CafeWalkController"/>, <see cref="AgentCompanionController"/>,
/// <see cref="AgentFollow"/>, <see cref="AmbientNpcRoutine"/>, and the gentle
/// <see cref="CharacterGrounding"/> feet-on-floor pass). This guard only ever
/// rescues an object that has fallen into a genuine void (NO walkable surface
/// under its feet) — it deliberately IGNORES anything overhead (upper decks,
/// ceilings, tables, awnings), so it can never teleport a correctly-grounded
/// player upward in the two-floor café. It runs rarely, never moves an object
/// that already has a floor under it, and logs each recovery.
/// </summary>
[DisallowMultipleComponent]
public sealed class GroundingGuard : MonoBehaviour
{
    private static GroundingGuard active;
    private static bool registering;

    private float nextScan;
    private const float ScanInterval = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        if (registering) return;
        registering = true;
        SceneManager.sceneLoaded += (_, __) => EnsureInstance();
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (active != null) return;
        GameObject host = new GameObject("GroundingGuard");
        DontDestroyOnLoad(host);
        active = host.AddComponent<GroundingGuard>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + ScanInterval;
        ScanPlayers();
        ScanAgents();
    }

    private static void ScanPlayers()
    {
        foreach (CafeWalkController player in FindObjectsByType<CafeWalkController>(FindObjectsInactive.Exclude))
        {
            if (player == null) continue;
            CharacterController body = player.GetComponent<CharacterController>();
            if (body == null || !body.enabled) continue;
            if (body.isGrounded) continue;
            Vector3 feet = player.transform.position;
            // There is a floor right under us: not a fall, leave it alone.
            if (HasFloorUnderFeet(feet)) continue;
            if (!TryFloorToRecoverTo(feet, out float floorY)) continue;

            body.enabled = false;
            player.transform.position = new Vector3(feet.x, floorY + 0.05f, feet.z);
            body.enabled = true;
            Debug.Log("[GroundingGuard] Rescued first-person player from Y=" +
                feet.y.ToString("0.00") + " to floor Y=" + floorY.ToString("0.00") + ".");
        }
    }

    private static void ScanAgents()
    {
        foreach (NavMeshAgent agent in FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Exclude))
        {
            if (agent == null || !agent.enabled) continue;
            Transform t = agent.transform;
            if (HasFloorUnderFeet(t.position)) continue;
            Vector3 target;
            if (NavMesh.SamplePosition(t.position, out NavMeshHit nav, 10f, NavMesh.AllAreas))
                target = nav.position;
            else if (TryFloorToRecoverTo(t.position, out float floorY))
                target = new Vector3(t.position.x, floorY, t.position.z);
            else continue;

            agent.Warp(target);
            Debug.Log("[GroundingGuard] Rescued NavMeshAgent '" + t.name + "' from Y=" +
                t.position.y.ToString("0.00") + " to floor Y=" + target.y.ToString("0.00") + ".");
        }
    }

    /// <summary>True when a walkable surface (physics collider or NavMesh) sits
    /// at or just below the feet. Overhead furniture/decks/ceilings are ignored
    /// because the search starts just above the feet and only accepts hits that
    /// are not above the feet plane.</summary>
    private static bool HasFloorUnderFeet(Vector3 feetPosition)
    {
        float feetPlane = feetPosition.y + 0.12f;
        Vector3 origin = new Vector3(feetPosition.x, feetPosition.y + 1.1f, feetPosition.z);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 1.1f + 0.65f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
            if (hits[i].point.y <= feetPlane) return true;
        if (NavMesh.SamplePosition(feetPosition, out NavMeshHit nav, 0.75f, NavMesh.AllAreas))
            return true;
        return false;
    }

    /// <summary>For a genuine void fall, finds the nearest floor surface ABOVE
    /// the current position to recover onto (the underside of the floor the
    /// object fell through). Prefers the NavMesh, then the lowest solid surface
    /// above the feet. Never returns something at/under the feet.</summary>
    private static bool TryFloorToRecoverTo(Vector3 feetPosition, out float floorY)
    {
        floorY = 0f;
        if (NavMesh.SamplePosition(feetPosition, out NavMeshHit nav, 10f, NavMesh.AllAreas)
            && nav.position.y > feetPosition.y + 0.2f)
        {
            floorY = nav.position.y;
            return true;
        }
        Vector3 origin = new Vector3(feetPosition.x, feetPosition.y + 30f, feetPosition.z);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 60f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float best = float.PositiveInfinity;
        foreach (RaycastHit hit in hits)
            if (hit.point.y > feetPosition.y + 0.2f && hit.point.y < best)
                best = hit.point.y;
        if (float.IsInfinity(best)) return false;
        floorY = best;
        return true;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>Recovers genuinely fallen characters to their last verified support, never an overhead deck.</summary>
[DisallowMultipleComponent]
public sealed class GroundingGuard : MonoBehaviour
{
    private struct SafePosition
    {
        public Vector3 position;
        public float unsupportedSince;
    }

    private static GroundingGuard active;
    private readonly Dictionary<NavMeshAgent, SafePosition> safeAgents = new Dictionary<NavMeshAgent, SafePosition>();
    private CafeWalkController[] players = new CafeWalkController[0];
    private NavMeshAgent[] agents = new NavMeshAgent[0];
    private float nextScan;
    private float nextDiscovery;
    private const float ScanInterval = 0.25f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        active = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstance();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
        active.safeAgents.Clear();
        active.nextDiscovery = 0f;
    }

    private static void EnsureInstance()
    {
        if (active != null) return;
        active = FindAnyObjectByType<GroundingGuard>();
        if (active != null) return;
        GameObject host = new GameObject("GroundingGuard");
        DontDestroyOnLoad(host);
        active = host.AddComponent<GroundingGuard>();
    }

    private void Update()
    {
        if (Time.time < nextScan) return;
        nextScan = Time.time + ScanInterval;
        if (Time.time >= nextDiscovery)
        {
            nextDiscovery = Time.time + 3f;
            players = FindObjectsByType<CafeWalkController>(FindObjectsInactive.Exclude);
            agents = FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Exclude);
        }
        foreach (CafeWalkController player in players)
            if (player != null && player.isActiveAndEnabled) player.RecoverIfFallen();
        foreach (NavMeshAgent agent in agents) CheckAgent(agent);
    }

    private void CheckAgent(NavMeshAgent agent)
    {
        if (agent == null || !agent.isActiveAndEnabled || agent.isOnOffMeshLink) return;
        Vector3 position = agent.transform.position;
        Vector3 feet = position - Vector3.up * agent.baseOffset;
        bool supported = WalkableFloorProbe.TryFind(agent.transform, feet, 0.2f, 0.6f, out _);
        if (supported && agent.isOnNavMesh)
        {
            safeAgents[agent] = new SafePosition { position = position, unsupportedSince = -1f };
            return;
        }
        if (!safeAgents.TryGetValue(agent, out SafePosition safe)) return;
        if (safe.unsupportedSince < 0f)
        {
            safe.unsupportedSince = Time.time;
            safeAgents[agent] = safe;
        }
        // Brief losses of support on stairs are normal. Rescue requires a real
        // downward fall and a previously visited floor still present in physics.
        if (Time.time - safe.unsupportedSince < 0.75f || position.y >= safe.position.y - 2f) return;
        Vector3 safeFeet = safe.position - Vector3.up * agent.baseOffset;
        if (!WalkableFloorProbe.TryFind(agent.transform, safeFeet, 0.25f, 0.65f, out RaycastHit floor)) return;
        NavMeshQueryFilter filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(floor.point, out NavMeshHit nav, 0.6f, filter)) return;
        if (Mathf.Abs(nav.position.y - floor.point.y) > 0.35f) return;
        if (!agent.Warp(nav.position)) return;
        safe.unsupportedSince = -1f;
        safeAgents[agent] = safe;
        Debug.Log("[GroundingGuard] Recovered '" + agent.name + "' from Y=" + position.y.ToString("0.00") +
            " to its last supported floor at Y=" + nav.position.y.ToString("0.00") + ".", agent);
    }

    private void OnDestroy()
    {
        if (active != this) return;
        active = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

using UnityEngine;
using UnityEngine.AI;

/// <summary>Demo visitor that follows the first-person cafe player across the NavMesh.</summary>
[RequireComponent(typeof(NavMeshAgent))]
public sealed class CafeFollowPlayer : MonoBehaviour
{
    public Transform target;
    public float followDistance = 1.5f;
    public float repathInterval = 0.25f;

    private NavMeshAgent agent;
    private float nextRepathTime;
    private float nextNavMeshRecovery;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.updateUpAxis = true;
            agent.baseOffset = 0f;
        }
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("Cafe_Player");
            if (player != null) target = player.transform;
        }
        RecoverToNavMesh();
        nextRepathTime = 0f;
    }

    private void Update()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("Cafe_Player");
            if (player != null) target = player.transform;
        }
        if (agent == null || !agent.enabled) return;
        if (!agent.isOnNavMesh)
        {
            if (Time.time >= nextNavMeshRecovery)
            {
                nextNavMeshRecovery = Time.time + 0.75f;
                RecoverToNavMesh();
            }
            return;
        }
        if (target == null || Time.time < nextRepathTime) return;

        Vector3 destination = target.position - target.forward * followDistance;
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 1.5f, agent.areaMask))
            agent.SetDestination(hit.position);
        nextRepathTime = Time.time + Mathf.Max(0.1f, repathInterval);
    }

    private bool RecoverToNavMesh()
    {
        if (agent == null || !agent.enabled || agent.isOnNavMesh) return agent != null && agent.isOnNavMesh;
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 4f, agent.areaMask)) return false;
        return agent.Warp(hit.position);
    }
}

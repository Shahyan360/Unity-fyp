using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentFollow : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform player;

    [Header("Distances")]
    [SerializeField] private float followDistance = 3f;
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float repathInterval = 0.35f;
    [SerializeField] private Vector3 preferredLocalOffset = new Vector3(1.2f, 0f, -1.6f);

    private NavMeshAgent navMeshAgent;
    private float nextRepathTime;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (player == null || navMeshAgent == null)
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > followDistance && Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + repathInterval;
            navMeshAgent.isStopped = false;
            Vector3 targetPosition = player.TransformPoint(preferredLocalOffset);
            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(hit.position);
            }
        }
        else if (distanceToPlayer <= stopDistance)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
        }
    }
}

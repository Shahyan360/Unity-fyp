using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentCompanionController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private float arrivalDistance = 0.7f;

    private void Awake()
    {
        agent ??= GetComponent<NavMeshAgent>();
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Player");
            player = playerObject != null ? playerObject.transform : null;
        }
    }

    public bool TryNavigateToNamedTarget(string targetName)
    {
        if (agent == null || string.IsNullOrWhiteSpace(targetName)) return false;
        foreach (ObjectMetadata item in FindObjectsByType<ObjectMetadata>(FindObjectsSortMode.None))
        {
            if (item.GetDisplayName().IndexOf(targetName, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                targetName.IndexOf(item.GetDisplayName(), System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (NavMesh.SamplePosition(item.transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.stoppingDistance = arrivalDistance;
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                return true;
            }
        }
        return false;
    }
}

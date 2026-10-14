using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentCompanionController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private float arrivalDistance = 0.7f;
    [SerializeField] private float followDistance = 2.8f;
    [SerializeField] private float sideOffset = -0.85f;
    [SerializeField] private float repathInterval = 0.55f;
    private float nextRepath;
    private float nextNavMeshRecovery;
    private bool navigatingToRequestedTarget;
    private Transform requestedLookTarget;
    private float holdAtRequestedTargetUntil;

    private void Awake()
    {
        agent ??= GetComponent<NavMeshAgent>();
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Player") ?? GameObject.Find("WorldExplorer") ?? GameObject.Find("Cafe_Player");
            player = playerObject != null ? playerObject.transform : null;
        }
    }

    private void Start()
    {
        if (agent == null || !agent.enabled) return;

        // NavMeshAgent owns the guide's world position. Keep it anchored to
        // the baked floor and recover scene-authored spawn points that landed
        // just outside the NavMesh after a scene transition.
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.updateUpAxis = true;
        agent.baseOffset = 0f;
        RecoverToNavMesh();
    }

    private void Update()
    {
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
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Player") ?? GameObject.Find("WorldExplorer") ?? GameObject.Find("Cafe_Player");
            if (playerObject != null) player = playerObject.transform;
        }
        if (player == null || Time.time < nextRepath) return;

        if (navigatingToRequestedTarget)
        {
            if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.2f) return;
            navigatingToRequestedTarget = false;
            // Stay with the exhibit long enough for the response, gesture and
            // caption to be useful. Without this short hold the normal follow
            // loop immediately pulls the guide back toward the learner.
            holdAtRequestedTargetUntil = Time.time + 8f;
            FaceRequestedTarget();
        }

        if (Time.time < holdAtRequestedTargetUntil)
        {
            agent.isStopped = true;
            agent.ResetPath();
            return;
        }

        // Stay just ahead and to the player's right so the guide reads as a
        // companion leading the exploration rather than a camera-following tail.
        Vector3 desired = player.position + player.forward * followDistance + player.right * sideOffset;
        desired.y = player.position.y;
        if (Vector3.Distance(transform.position, desired) > 1.15f
            && NavMesh.SamplePosition(desired, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
        nextRepath = Time.time + Mathf.Max(0.25f, repathInterval);
    }

    private bool RecoverToNavMesh()
    {
        if (agent == null || !agent.enabled || agent.isOnNavMesh) return agent != null && agent.isOnNavMesh;
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 6f, agent.areaMask)) return false;
        return agent.Warp(hit.position);
    }

    public bool TryNavigateToNamedTarget(string targetName)
    {
        if (agent == null || !agent.enabled || string.IsNullOrWhiteSpace(targetName))
            return false;
        // Scene-authored agents can load a little above or beside the baked
        // navigation surface. Snap them onto the nearest walkable point before
        // issuing a destination, otherwise the request silently does nothing.
        if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit startHit, 4f, NavMesh.AllAreas))
            agent.Warp(startHit.position);
        if (!agent.isOnNavMesh)
            return false;
        foreach (ObjectMetadata item in FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Exclude))
        {
            if (item.GetDisplayName().IndexOf(targetName, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                targetName.IndexOf(item.GetDisplayName(), System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            Vector3 towardLearner = player != null ? player.position - item.transform.position : transform.position - item.transform.position;
            towardLearner.y = 0f;
            if (towardLearner.sqrMagnitude < 0.05f) towardLearner = Vector3.back;
            towardLearner.Normalize();
            Vector3 screenRight = Camera.main != null ? Camera.main.transform.right : Vector3.Cross(towardLearner, Vector3.up);
            screenRight.y = 0f;
            if (screenRight.sqrMagnitude < 0.05f) screenRight = Vector3.Cross(towardLearner, Vector3.up);
            screenRight.Normalize();
            // Bring the guide into the learner's clear aisle beside the model.
            // A point based on the exhibit itself can land on the far side of
            // classroom desks, leaving the guide hidden behind the plinth.
            Vector3 guideStandPoint;
            if (item.scenario == "museum" || item.scenario == "classroom")
                guideStandPoint = item.transform.position + towardLearner * 3.4f - screenRight * 0.9f;
            else if (player != null)
            {
                Vector3 facing = player.forward;
                facing.y = 0f;
                if (facing.sqrMagnitude < 0.05f) facing = -towardLearner;
                facing.Normalize();
                Vector3 learnerRight = player.right;
                learnerRight.y = 0f;
                if (learnerRight.sqrMagnitude < 0.05f) learnerRight = screenRight;
                learnerRight.Normalize();
                guideStandPoint = player.position + facing * 1.2f - learnerRight * 5.4f;
            }
            else guideStandPoint = item.transform.position + towardLearner * 3.8f + screenRight * 4.0f;

            if (NavMesh.SamplePosition(guideStandPoint, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            {
                ClassroomWaypointNavigator waypointNavigator = GetComponent<ClassroomWaypointNavigator>();
                if (waypointNavigator != null) waypointNavigator.StopNavigation();
                agent.stoppingDistance = arrivalDistance;
                agent.isStopped = false;
                requestedLookTarget = item.transform;
                holdAtRequestedTargetUntil = 0f;
                if (!agent.SetDestination(hit.position)) return false;
                navigatingToRequestedTarget = true;
                nextRepath = Time.time + repathInterval;
                return true;
            }
        }
        return false;
    }

    private void FaceRequestedTarget()
    {
        if (requestedLookTarget == null) return;
        Vector3 direction = requestedLookTarget.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.05f) transform.rotation = Quaternion.LookRotation(direction);
        requestedLookTarget = null;
    }
}

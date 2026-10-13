using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Moves a NavMeshAgent along the connected classroom waypoint graph.</summary>
[RequireComponent(typeof(NavMeshAgent))]
public sealed class ClassroomWaypointNavigator : MonoBehaviour
{
    [SerializeField] private ClassroomWaypointGraph graph;
    [SerializeField] private bool autoStart;
    [SerializeField] private string autoStartDestination = "teacher_approach";

    private NavMeshAgent agent;
    private List<ClassroomWaypoint> activeRoute;
    private int nextRouteIndex;
    private bool navigating;

    public bool IsNavigating => navigating;
    public string CurrentDestinationId => activeRoute != null && activeRoute.Count > 0 ? activeRoute[activeRoute.Count - 1].waypointId : string.Empty;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (graph == null) graph = FindAnyObjectByType<ClassroomWaypointGraph>();
    }

    private IEnumerator Start()
    {
        if (!autoStart) yield break;
        yield return null;
        if (agent == null || !agent.enabled) yield break;
        if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, agent.areaMask))
            agent.Warp(hit.position);
        NavigateToWaypoint(autoStartDestination);
    }

    private void Update()
    {
        if (!navigating || agent == null || agent.pathPending) return;
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid) { navigating = false; return; }
        if (agent.remainingDistance > agent.stoppingDistance + 0.1f) return;
        if (activeRoute != null && nextRouteIndex < activeRoute.Count)
        {
            agent.SetDestination(activeRoute[nextRouteIndex++].transform.position);
            return;
        }
        navigating = false;
    }

    public void Configure(ClassroomWaypointGraph waypointGraph, bool startAutomatically, string destinationId)
    {
        graph = waypointGraph;
        autoStart = startAutomatically;
        autoStartDestination = destinationId;
    }

    public bool NavigateToWaypoint(string waypointId)
    {
        if (graph == null) graph = FindAnyObjectByType<ClassroomWaypointGraph>();
        if (graph == null || agent == null || string.IsNullOrWhiteSpace(waypointId)) return false;
        ClassroomWaypoint start = graph.FindNearestWaypoint(transform.position);
        ClassroomWaypoint destination = graph.FindWaypoint(waypointId);
        activeRoute = graph.FindRoute(start, destination);
        if (activeRoute.Count == 0 || !agent.isOnNavMesh) return false;
        navigating = true;
        if (activeRoute.Count == 1)
        {
            nextRouteIndex = 1;
            agent.SetDestination(activeRoute[0].transform.position);
        }
        else
        {
            nextRouteIndex = 2;
            agent.SetDestination(activeRoute[1].transform.position);
        }
        return true;
    }

    public void StopNavigation()
    {
        navigating = false;
        activeRoute = null;
        if (agent != null && agent.isOnNavMesh) agent.ResetPath();
    }
}

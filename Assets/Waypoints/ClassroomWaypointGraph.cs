using System.Collections.Generic;
using UnityEngine;

/// <summary>Stores classroom waypoints and finds connected routes between them.</summary>
[DisallowMultipleComponent]
public sealed class ClassroomWaypointGraph : MonoBehaviour
{
    [SerializeField] private List<ClassroomWaypoint> waypoints = new List<ClassroomWaypoint>();
    public IReadOnlyList<ClassroomWaypoint> Waypoints => waypoints;

    public void SetWaypoints(IEnumerable<ClassroomWaypoint> values)
    {
        waypoints.Clear();
        if (values == null) return;
        foreach (ClassroomWaypoint value in values)
            if (value != null && !waypoints.Contains(value)) waypoints.Add(value);
    }

    public ClassroomWaypoint FindWaypoint(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        foreach (ClassroomWaypoint waypoint in waypoints)
            if (waypoint != null && string.Equals(waypoint.waypointId, id, System.StringComparison.OrdinalIgnoreCase)) return waypoint;
        return null;
    }

    public ClassroomWaypoint FindNearestWaypoint(Vector3 position)
    {
        ClassroomWaypoint best = null;
        float bestDistance = float.PositiveInfinity;
        foreach (ClassroomWaypoint waypoint in waypoints)
        {
            if (waypoint == null) continue;
            float distance = (waypoint.transform.position - position).sqrMagnitude;
            if (distance < bestDistance) { bestDistance = distance; best = waypoint; }
        }
        return best;
    }

    public List<ClassroomWaypoint> FindRoute(string fromId, string toId)
    {
        return FindRoute(FindWaypoint(fromId), FindWaypoint(toId));
    }

    public List<ClassroomWaypoint> FindRoute(ClassroomWaypoint from, ClassroomWaypoint to)
    {
        var route = new List<ClassroomWaypoint>();
        if (from == null || to == null) return route;
        if (from == to) { route.Add(from); return route; }

        var parents = new Dictionary<ClassroomWaypoint, ClassroomWaypoint>();
        var visited = new HashSet<ClassroomWaypoint>();
        var queue = new Queue<ClassroomWaypoint>();
        visited.Add(from);
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            ClassroomWaypoint current = queue.Dequeue();
            foreach (ClassroomWaypoint neighbor in current.Connections)
            {
                if (neighbor == null || !visited.Add(neighbor)) continue;
                parents[neighbor] = current;
                if (neighbor == to)
                {
                    var cursor = to;
                    route.Add(cursor);
                    while (cursor != from) { cursor = parents[cursor]; route.Add(cursor); }
                    route.Reverse();
                    return route;
                }
                queue.Enqueue(neighbor);
            }
        }
        return route;
    }
}

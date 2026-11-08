using System.Collections.Generic;
using UnityEngine;

/// <summary>A named point along the classroom's walkable route.</summary>
[DisallowMultipleComponent]
public sealed class ClassroomWaypoint : MonoBehaviour
{
    public string waypointId;
    public string displayName;
    [SerializeField] private List<ClassroomWaypoint> connections = new List<ClassroomWaypoint>();

    public IReadOnlyList<ClassroomWaypoint> Connections => connections;

    public void ClearConnections() => connections.Clear();

    public void ConnectBidirectionally(ClassroomWaypoint other)
    {
        if (other == null || other == this) return;
        if (!connections.Contains(other)) connections.Add(other);
        if (!other.connections.Contains(this)) other.connections.Add(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.75f, 1f);
        Gizmos.DrawSphere(transform.position, 0.12f);
        foreach (ClassroomWaypoint next in connections)
        {
            if (next == null) continue;
            Gizmos.DrawLine(transform.position, next.transform.position);
        }
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(displayName))
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.2f, displayName);
#endif
    }
}

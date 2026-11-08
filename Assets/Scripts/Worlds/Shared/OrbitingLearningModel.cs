using System.Collections.Generic;
using UnityEngine;

/// <summary>Moves teaching-model bodies on simple illustrative paths; it is not a scientific simulation.</summary>
public sealed class OrbitingLearningModel : MonoBehaviour
{
    private struct Orbiter
    {
        public Transform transform;
        public Vector3 center;
        public float radius;
        public Vector3 axis;
        public float speed;
        public float phase;
    }

    private readonly List<Orbiter> orbiters = new List<Orbiter>();

    public void AddOrbiter(Transform item, Vector3 center, float radius, Vector3 axis, float speed, float phase)
    {
        orbiters.Add(new Orbiter { transform = item, center = center, radius = radius, axis = axis.normalized, speed = speed, phase = phase });
    }

    private void Update()
    {
        foreach (Orbiter orbiter in orbiters)
        {
            if (orbiter.transform == null) continue;
            Vector3 reference = Mathf.Abs(Vector3.Dot(orbiter.axis, Vector3.up)) > 0.96f ? Vector3.right : Vector3.up;
            Vector3 first = Vector3.Cross(orbiter.axis, reference).normalized;
            Vector3 second = Vector3.Cross(orbiter.axis, first).normalized;
            float angle = Time.time * orbiter.speed + orbiter.phase;
            orbiter.transform.localPosition = orbiter.center + (first * Mathf.Cos(angle) + second * Mathf.Sin(angle)) * orbiter.radius;
        }
    }
}

using UnityEngine;

public class FloatingAgentIdle : MonoBehaviour
{
    [Header("Floating")]
    public float floatHeight = 0.25f;
    public float floatSpeed = 1.5f;

    [Header("Rotation")]
    public float rotationSpeed = 15f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        // Gently move the agent up and down around its starting position.
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = startPosition + new Vector3(0f, yOffset, 0f);

        // Slowly rotate the agent so the landing screen feels alive.
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}


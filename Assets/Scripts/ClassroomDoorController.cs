using UnityEngine;

/// <summary>
/// Click the door in Play mode to swing it open or closed.
/// Open, Close, and Toggle can also be called by future AI interaction scripts.
/// </summary>
public sealed class ClassroomDoorController : MonoBehaviour
{
    [SerializeField] private float openAngle = 100f;
    [SerializeField] private float rotationSpeed = 180f;

    private Transform hinge;
    private Quaternion closedRotation;
    private Quaternion targetRotation;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        hinge = transform.parent;
        if (hinge == null)
        {
            Debug.LogError("ClassroomDoorController needs to be a child of the door hinge.", this);
            enabled = false;
            return;
        }

        closedRotation = hinge.localRotation;
        targetRotation = closedRotation;
    }

    private void Update()
    {
        hinge.localRotation = Quaternion.RotateTowards(
            hinge.localRotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    private void OnMouseDown()
    {
        Toggle();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        IsOpen = true;
        targetRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    public void Close()
    {
        IsOpen = false;
        targetRotation = closedRotation;
    }
}

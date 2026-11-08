using UnityEngine;

/// <summary>Turns small exhibit captions toward the learner.</summary>
public sealed class WorldLabelBillboard : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        Vector3 towardsCamera = camera.transform.position - transform.position;
        towardsCamera.y = 0f;
        // TextMeshPro's 3D text mesh faces local -Z. Aim that face at the camera;
        // aiming the transform's +Z axis at the camera makes every label mirrored.
        if (towardsCamera.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(-towardsCamera.normalized, Vector3.up);
    }
}

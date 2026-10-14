using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Shared first-person WASD navigation used by the café and learning worlds.</summary>
[RequireComponent(typeof(CharacterController))]
public sealed class CafeWalkController : MonoBehaviour
{
    public Transform view;
    public float walkSpeed = 3.6f;
    public float runSpeed = 5.2f;
    public float lookSensitivity = 0.12f;

    private CharacterController character;
    private float pitch;
    private float verticalSpeed;
    private Vector3 lastSafeGroundPosition;
    private bool hasSafeGroundPosition;
    private const float GroundRecoveryDepth = 3f;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
    }

    private void Start()
    {
        // The authored café camera was parented to this controller with an old
        // aerial-review offset (-22.5, 11, -9). Keep the authored ground-level
        // spawn, but normalize the camera to a human first-person eye position.
        if (view != null && view.parent == transform)
        {
            view.localPosition = new Vector3(0f, 1.62f, 0f);
            view.localRotation = Quaternion.identity;
            pitch = 0f;
        }

        // Scene-authored spawn points can sit a few centimetres above/below
        // their baked floor. Prefer the walkable surface so the capsule starts
        // grounded instead of accumulating gravity from its first frame.
        if (TryFindGround(transform.position, out Vector3 groundPosition))
        {
            Vector3 groundedPosition = groundPosition + Vector3.up * Mathf.Max(0.04f, character.skinWidth * 0.5f);
            if (Mathf.Abs(transform.position.y - groundedPosition.y) > 0.025f)
            {
                character.enabled = false;
                transform.position = groundedPosition;
                character.enabled = true;
            }
            lastSafeGroundPosition = transform.position;
            hasSafeGroundPosition = true;
        }

        // Let the player use the on-screen guide and world controls immediately.
        // Clicking the 3D world (rather than a UI element) enters mouse-look.
        SetCursorLocked(false);
    }

    public void InitializeView(Transform cameraTransform, float initialPitch = 0f)
    {
        view = cameraTransform;
        pitch = Mathf.Clamp(initialPitch, -80f, 80f);
    }

    public void FaceTarget(Transform target)
    {
        if (target == null) return;
        Vector3 eyePosition = view != null ? view.position : transform.position + Vector3.up * 1.62f;
        Vector3 direction = target.position + Vector3.up * 1.5f - eyePosition;
        Vector3 horizontal = new Vector3(direction.x, 0f, direction.z);
        if (horizontal.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.LookRotation(horizontal.normalized, Vector3.up);
        if (view == null) return;
        pitch = Mathf.Clamp(-Mathf.Atan2(direction.y, horizontal.magnitude) * Mathf.Rad2Deg, -60f, 60f);
        view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Update()
    {
#if UNITY_EDITOR
        // The embedded Game view can remain in Play mode while the user moves
        // the pointer through the Hierarchy, Inspector, or Project panels. Do
        // not turn or move the explorer from editor-panel mouse/key input.
        EditorWindow focusedWindow = EditorWindow.focusedWindow;
        if (focusedWindow == null || focusedWindow.GetType().Name != "GameView")
        {
            SetCursorLocked(false);
            return;
        }
#endif

        // A click on an input can arrive one frame before TMP marks it focused.
        // Keep keyboard locomotion available while the cursor merely hovers over
        // the HUD; stop only when the learner is actually typing in a field.
        if (IsTypingIntoUi())
        {
            SetCursorLocked(false);
            return;
        }
        Vector2 moveInput = Vector2.zero;
        Vector2 lookInput = Vector2.zero;
        bool sprint = false;
        bool unlock = false;
        bool relock = false;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            moveInput.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            moveInput.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            unlock = keyboard.escapeKey.wasPressedThisFrame;
        }
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            if (Cursor.lockState == CursorLockMode.Locked) lookInput = mouse.delta.ReadValue();
            bool pointerOverUi = IsPointerOverUI(mouse.deviceId);
            // Recover from the Editor or a scene leaving the cursor captured:
            // a left click is always the user's route back to the on-screen UI.
            // Right-clicking the world is the explicit gesture for mouse-look.
            if (Cursor.lockState == CursorLockMode.Locked && mouse.leftButton.wasPressedThisFrame)
                unlock = true;
            relock = mouse.rightButton.wasPressedThisFrame && !pointerOverUi;
        }
#else
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        sprint = Input.GetKey(KeyCode.LeftShift);
        unlock = Input.GetKeyDown(KeyCode.Escape);
        relock = Input.GetMouseButtonDown(1) && !IsPointerOverUI(-1);
        lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
#endif

        if (unlock) SetCursorLocked(false);
        if (relock) SetCursorLocked(true);

        if (Cursor.lockState == CursorLockMode.Locked && view != null)
        {
            lookInput = Vector2.ClampMagnitude(lookInput, 40f);
            transform.Rotate(Vector3.up, lookInput.x * lookSensitivity, Space.World);
            pitch = Mathf.Clamp(pitch - lookInput.y * lookSensitivity, -80f, 80f);
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        Vector3 horizontal = (transform.right * moveInput.x + transform.forward * moveInput.y) * (sprint ? runSpeed : walkSpeed);
        bool grounded = character.isGrounded || (verticalSpeed <= 0f && CheckGroundedFallback());
        if (grounded && verticalSpeed < 0f) verticalSpeed = -0.5f;
        verticalSpeed -= 9.81f * Time.deltaTime;
        character.Move((horizontal + Vector3.up * verticalSpeed) * Time.deltaTime);

        if (grounded)
        {
            lastSafeGroundPosition = transform.position;
            hasSafeGroundPosition = true;
        }
        else if (hasSafeGroundPosition && transform.position.y < lastSafeGroundPosition.y - GroundRecoveryDepth)
        {
            // A safety net for a missing/incorrect collider in a downloaded
            // environment: recover at the last valid floor point instead of
            // leaving the learner in an endless fall.
            character.enabled = false;
            transform.position = lastSafeGroundPosition;
            character.enabled = true;
            verticalSpeed = 0f;
        }
    }

    private bool TryFindGround(Vector3 reference, out Vector3 groundPosition)
    {
        if (NavMesh.SamplePosition(reference, out NavMeshHit walkable, 1.5f, NavMesh.AllAreas))
        {
            groundPosition = walkable.position;
            return true;
        }

        bool wasEnabled = character != null && character.enabled;
        if (wasEnabled) character.enabled = false;
        Vector3 rayStart = reference + Vector3.up * 2f;
        bool found = Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 12f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.normal.y >= 0.55f;
        if (wasEnabled) character.enabled = true;
        groundPosition = found ? hit.point : reference;
        return found;
    }

    private bool CheckGroundedFallback()
    {
        if (character == null) return false;
        Vector3 bottom = transform.position + character.center - Vector3.up * Mathf.Max(0.1f, character.height * 0.5f - character.radius);
        return Physics.SphereCast(bottom + Vector3.up * 0.08f, Mathf.Max(0.1f, character.radius * 0.85f), Vector3.down, out _, 0.14f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private static bool IsPointerOverUI(int pointerId)
    {
        EventSystem events = EventSystem.current;
        if (events == null) return false;
        if (pointerId >= 0 && events.IsPointerOverGameObject(pointerId)) return true;
        if (events.IsPointerOverGameObject()) return true;

#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            PointerEventData pointer = new PointerEventData(events) { position = mouse.position.ReadValue() };
            var results = new List<RaycastResult>();
            events.RaycastAll(pointer, results);
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].module is GraphicRaycaster) return true;
            }
        }
#endif
        return false;
    }

    private static bool IsTypingIntoUi()
    {
        EventSystem events = EventSystem.current;
        if (events == null || events.currentSelectedGameObject == null) return false;
        GameObject selected = events.currentSelectedGameObject;
        TMPro.TMP_InputField tmpInput = selected.GetComponentInParent<TMPro.TMP_InputField>();
        if (tmpInput != null) return tmpInput.isFocused;
        UnityEngine.UI.InputField legacyInput = selected.GetComponentInParent<UnityEngine.UI.InputField>();
        return legacyInput != null && legacyInput.isFocused;
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) SetCursorLocked(false);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SetCursorLocked(false);
    }
}

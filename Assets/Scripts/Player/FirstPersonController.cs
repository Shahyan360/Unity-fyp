using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float mouseSensitivity = 2f;
    public float gravity = -9.81f;
    public float jumpHeight = 1f;

    [Header("References")]
    public Transform cameraTransform;

    [Header("Cursor Settings")]
    public KeyCode cursorToggleKey = KeyCode.Escape;

    private CharacterController characterController;
    private float cameraPitch = 0f;
    private float verticalVelocity = 0f;
    private bool isCursorLocked = true;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Start()
    {
        LockCursor();
    }

    private void Update()
    {
        bool isTypingInChat = IsTypingInInputField();

        // Press Esc to switch between player-control mode and UI-click mode.
        if (Input.GetKeyDown(cursorToggleKey))
        {
            if (isCursorLocked)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        }

        if (isTypingInChat)
        {
            // Do not let WASD, Space, or mouse movement control the player while typing.
            HandleGravityOnly();
            return;
        }

        HandleMovement();

        // Only rotate camera when cursor is locked.
        // When cursor is unlocked, user can click UI input field and Send button.
        if (isCursorLocked)
        {
            HandleMouseLook();
        }
    }

    private bool IsTypingInInputField()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
        {
            return false;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        return selectedObject.GetComponent<TMP_InputField>() != null
            || selectedObject.GetComponent<InputField>() != null
            || selectedObject.GetComponentInParent<TMP_InputField>() != null
            || selectedObject.GetComponentInParent<InputField>() != null;
    }

    private void HandleMovement()
    {
        if (characterController == null)
        {
            return;
        }

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 moveDirection = transform.right * moveX + transform.forward * moveZ;
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        bool grounded = characterController.isGrounded || (verticalVelocity <= 0f && CheckGroundedFallback());
        if (grounded && verticalVelocity < 0)
        {
            verticalVelocity = -0.5f;
        }

        if (Input.GetButtonDown("Jump") && grounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 gravityMove = new Vector3(0f, verticalVelocity, 0f);
        characterController.Move(gravityMove * Time.deltaTime);
    }

    private void HandleGravityOnly()
    {
        if (characterController == null)
        {
            return;
        }

        bool grounded = characterController.isGrounded || (verticalVelocity <= 0f && CheckGroundedFallback());
        if (grounded && verticalVelocity < 0)
        {
            verticalVelocity = -0.5f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
    }

    private bool CheckGroundedFallback()
    {
        if (characterController == null) return false;
        Vector3 bottom = transform.position + characterController.center - Vector3.up * Mathf.Max(0.1f, characterController.height * 0.5f - characterController.radius);
        return Physics.SphereCast(bottom + Vector3.up * 0.08f, Mathf.Max(0.1f, characterController.radius * 0.85f), Vector3.down, out _, 0.14f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    private void HandleMouseLook()
    {
        if (cameraTransform == null)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);

        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void LockCursor()
    {
        isCursorLocked = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        isCursorLocked = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}

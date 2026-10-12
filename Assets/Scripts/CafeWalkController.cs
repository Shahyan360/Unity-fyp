using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Simple first-person WASD controller for the cafe stair-follow demo.</summary>
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

    private void Awake()
    {
        character = GetComponent<CharacterController>();
    }

    private void Start()
    {
        SetCursorLocked(true);
    }

    private void Update()
    {
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
            lookInput = mouse.delta.ReadValue();
            relock = mouse.leftButton.wasPressedThisFrame;
        }
#else
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        sprint = Input.GetKey(KeyCode.LeftShift);
        unlock = Input.GetKeyDown(KeyCode.Escape);
        relock = Input.GetMouseButtonDown(0);
        lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
#endif

        if (unlock) SetCursorLocked(false);
        if (relock) SetCursorLocked(true);

        if (Cursor.lockState == CursorLockMode.Locked && view != null)
        {
            transform.Rotate(Vector3.up, lookInput.x * lookSensitivity, Space.World);
            pitch = Mathf.Clamp(pitch - lookInput.y * lookSensitivity, -80f, 80f);
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        Vector3 horizontal = (transform.right * moveInput.x + transform.forward * moveInput.y) * (sprint ? runSpeed : walkSpeed);
        if (character.isGrounded && verticalSpeed < 0f) verticalSpeed = -1.5f;
        verticalSpeed -= 20f * Time.deltaTime;
        character.Move((horizontal + Vector3.up * verticalSpeed) * Time.deltaTime);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }
}

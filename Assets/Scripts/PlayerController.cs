using UnityEngine;
using UnityEngine.InputSystem;

// Third-person player driven by the project-wide "Player" input actions
// (Move, Look, Jump, Sprint). Mouse orbits the camera; the character turns to face
// the direction it moves.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float walkSpeed = 20f;
    [SerializeField] float sprintSpeed = 40f;
    [SerializeField] float turnSpeed = 720f;
    [SerializeField] float jumpHeight = 1.2f;
    [SerializeField] float gravity = -20f;

    [Header("Camera")]
    [Tooltip("Defaults to Camera.main.")]
    [SerializeField] Transform cam;
    [SerializeField] float cameraDistance = 5f;
    [SerializeField] float cameraHeight = 1.5f;
    [SerializeField] float lookSensitivity = 0.15f;
    [SerializeField] float minPitch = -30f, maxPitch = 70f;

    CharacterController controller;
    InputAction moveAction, lookAction, jumpAction, sprintAction;
    float verticalVelocity;
    float yaw, pitch = 15f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cam == null && Camera.main != null) cam = Camera.main.transform;

        moveAction = InputSystem.actions.FindAction("Player/Move", true);
        lookAction = InputSystem.actions.FindAction("Player/Look", true);
        jumpAction = InputSystem.actions.FindAction("Player/Jump", true);
        sprintAction = InputSystem.actions.FindAction("Player/Sprint", true);

        yaw = transform.eulerAngles.y;
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        // CharacterController overrides transform changes while enabled.
        controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        controller.enabled = true;
        yaw = rotation.eulerAngles.y;
        verticalVelocity = 0f;
    }

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        Vector2 look = lookAction.ReadValue<Vector2>() * lookSensitivity;
        yaw += look.x;
        pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);

        // Movement is relative to the camera's facing, flattened onto the ground.
        Vector2 input = moveAction.ReadValue<Vector2>();
        Quaternion camYaw = Quaternion.Euler(0f, yaw, 0f);
        Vector3 moveDir = camYaw * new Vector3(input.x, 0f, input.y);
        float speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        if (controller.isGrounded)
        {
            // Small downward push keeps isGrounded stable on slopes.
            verticalVelocity = -2f;

            if (jumpAction.WasPressedThisFrame())
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDir * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (cam == null) return;

        Vector3 pivot = transform.position + Vector3.up * cameraHeight;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        float dist = cameraDistance;

        // Pull the camera in when something (e.g. a shelf) is between it and the player.
        if (Physics.SphereCast(pivot, 0.2f, rot * Vector3.back, out RaycastHit hit, cameraDistance,
                ~0, QueryTriggerInteraction.Ignore) && hit.transform != transform)
            dist = hit.distance;

        cam.SetPositionAndRotation(pivot - rot * Vector3.forward * dist, rot);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

// Third-person player driven by the project-wide "Player" input actions
// (Move, Look, Jump, Sprint). Mouse orbits the camera; the character turns to face
// the direction it moves. Cutscenes can take over movement and the camera, and the player
// respawns at the last checkpoint (R, or falling out of the world).
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float walkSpeed = 20f;
    [SerializeField] float sprintSpeed = 40f;
    [SerializeField] float turnSpeed = 720f;
    [SerializeField] float jumpHeight = 1.8f;
    [SerializeField] float gravity = -20f;

    [Header("Camera")]
    [Tooltip("Defaults to Camera.main.")]
    [SerializeField] Transform cam;
    [SerializeField] float cameraDistance = 5f;
    [SerializeField] float cameraHeight = 1.5f;
    [SerializeField] float lookSensitivity = 0.15f;
    [SerializeField] float minPitch = -40f, maxPitch = 70f;

    [Header("Respawn")]
    [SerializeField] float killHeight = -20f;

    CharacterController controller;
    InputAction moveAction, lookAction, jumpAction, sprintAction;
    float verticalVelocity;
    float yaw, pitch = 15f;
    Vector3 checkpointPos;
    Quaternion checkpointRot;

    // While a cutscene runs, player input is ignored and `scriptedMove` drives the character.
    public bool InCutscene { get; private set; }
    Vector3 scriptedMove;

    public bool IsGrounded => controller != null && controller.isGrounded;
    public float Yaw => yaw;
    public float Pitch => pitch;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cam == null && Camera.main != null) cam = Camera.main.transform;

        moveAction = InputSystem.actions.FindAction("Player/Move", true);
        lookAction = InputSystem.actions.FindAction("Player/Look", true);
        jumpAction = InputSystem.actions.FindAction("Player/Jump", true);
        sprintAction = InputSystem.actions.FindAction("Player/Sprint", true);

        yaw = transform.eulerAngles.y;
        SetCheckpoint(transform.position, transform.rotation);
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

    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        checkpointPos = position;
        checkpointRot = rotation;
    }

    public void Respawn() => Teleport(checkpointPos + Vector3.up * 0.5f, checkpointRot);

    public void BeginCutscene()
    {
        InCutscene = true;
        scriptedMove = Vector3.zero;
    }

    public void EndCutscene() => InCutscene = false;

    // Cutscene helpers: walk in a direction (world space, units/sec) and aim the camera.
    public void ScriptedMove(Vector3 velocity) => scriptedMove = velocity;

    public void SetLook(float newYaw, float newPitch)
    {
        yaw = newYaw;
        pitch = newPitch;
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
        if (transform.position.y < killHeight || (!InCutscene && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame))
        {
            Respawn();
            return;
        }

        Vector3 moveDir;
        float speed;
        if (InCutscene)
        {
            moveDir = scriptedMove;
            speed = 1f;
        }
        else
        {
            Vector2 look = lookAction.ReadValue<Vector2>() * lookSensitivity;
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);

            // Movement is relative to the camera's facing, flattened onto the ground.
            Vector2 input = moveAction.ReadValue<Vector2>();
            moveDir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
            speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
        }

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(new Vector3(moveDir.x, 0f, moveDir.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        if (controller.isGrounded)
        {
            // Small downward push keeps isGrounded stable on slopes.
            verticalVelocity = -2f;

            if (!InCutscene && jumpAction.WasPressedThisFrame())
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
                ~0, QueryTriggerInteraction.Ignore) && hit.transform != transform && !hit.transform.IsChildOf(transform))
            dist = hit.distance;

        cam.SetPositionAndRotation(pivot - rot * Vector3.forward * dist, rot);
    }
}

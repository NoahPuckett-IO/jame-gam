using UnityEngine;
using UnityEngine.InputSystem;

// Third-person player driven by the project-wide "Player" input actions
// (Move, Look, Jump, Sprint). Mouse orbits the camera; the character turns to face
// the direction it moves. Cutscenes can take over movement and the camera, and the player
// respawns at the last checkpoint (R, or falling out of the world).
// Runs after the things it can stand on (bobbing ice, moving plates, tipping planks) have moved.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float walkSpeed = 14f;
    [SerializeField] float sprintSpeed = 28f;
    [SerializeField] float turnSpeed = 720f;
    [SerializeField] float jumpHeight = 1.8f;
    [SerializeField] float gravity = -20f;

    [Header("Camera")]
    [Tooltip("Defaults to Camera.main.")]
    [SerializeField] Transform cam;
    [SerializeField] float cameraDistance = 7.5f;
    [SerializeField] float cameraHeight = 2.6f;
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
    public float WalkSpeed => walkSpeed;
    public float SprintSpeed => sprintSpeed;
    public event System.Action Jumped;

    // Briefly holds the player still, e.g. while bending down to pick something up.
    float lockedUntil;
    public void LockMovement(float seconds) => lockedUntil = Time.time + seconds;
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
        planarVelocity = knockback = Vector3.zero;
        iceZones = 0;
        platform = null;
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
            Vector2 input = Time.time < lockedUntil ? Vector2.zero : moveAction.ReadValue<Vector2>();
            moveDir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
            speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
        }

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(new Vector3(moveDir.x, 0f, moveDir.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        if (controller.isGrounded && verticalVelocity <= 0f)
        {
            // Small downward push keeps isGrounded stable on slopes.
            verticalVelocity = -2f;

            if (!InCutscene && Time.time >= lockedUntil && jumpAction.WasPressedThisFrame())
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                Jumped?.Invoke();
            }
        }

        verticalVelocity += gravity * Time.deltaTime;

        // Horizontal velocity chases the input. Normally almost instantly; on ice (low traction)
        // Henry keeps sliding and takes a while to turn or stop. Knockbacks add on top.
        float t = InCutscene ? 30f : (iceZones > 0 ? IceTraction : traction);
        planarVelocity = Vector3.Lerp(planarVelocity, moveDir * speed, 1f - Mathf.Exp(-t * Time.deltaTime));
        knockback = Vector3.Lerp(knockback, Vector3.zero, 1f - Mathf.Exp(-4f * Time.deltaTime));

        // Ride whatever moving thing he's standing on: carry him along by however far the spot under
        // his feet moved (and turned) since last frame.
        if (platform != null && platform.gameObject.activeInHierarchy)
        {
            var carried = platform.TransformPoint(platformLocal) - transform.position;
            float turn = Mathf.DeltaAngle(platformYaw, platform.eulerAngles.y);
            if (carried.sqrMagnitude < 4f) controller.Move(carried);
            if (Mathf.Abs(turn) < 45f) { transform.Rotate(0f, turn, 0f); yaw += turn; }
        }

        Vector3 velocity = planarVelocity + knockback + Vector3.up * verticalVelocity;
        standingOn = null;
        controller.Move(velocity * Time.deltaTime);

        platform = controller.isGrounded ? standingOn : null;
        if (platform != null)
        {
            platformLocal = platform.InverseTransformPoint(transform.position);
            platformYaw = platform.eulerAngles.y;
        }
    }

    // Moving platforms are anything with a kinematic Rigidbody (bobbing ice cubes, moving plates,
    // tipping planks, lifting ramps).
    Transform platform, standingOn;
    Vector3 platformLocal;
    float platformYaw;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y < 0.5f) return;
        var body = hit.collider.attachedRigidbody;
        standingOn = body != null && body.isKinematic ? hit.collider.transform : null;
    }

    [Header("Traction")]
    [SerializeField] float traction = 22f;
    const float IceTraction = 1.6f;
    Vector3 planarVelocity, knockback;
    int iceZones;

    public bool OnIce => iceZones > 0;
    public void EnterIce() => iceZones++;
    public void ExitIce() => iceZones = Mathf.Max(0, iceZones - 1);

    // Bounce pads and the like: throw Henry straight up at this speed.
    public void Launch(float upSpeed)
    {
        verticalVelocity = upSpeed;
        Jumped?.Invoke();
    }

    // Shoved by something (a falling item): a horizontal push plus a little hop.
    public void Knock(Vector3 push)
    {
        knockback = new Vector3(push.x, 0, push.z);
        verticalVelocity = Mathf.Max(verticalVelocity, push.y);
    }

    // Set by cutscenes that drive the camera themselves.
    public bool CameraLocked { get; set; }

    void LateUpdate()
    {
        if (cam == null || CameraLocked) return;

        Vector3 pivot = transform.position + Vector3.up * cameraHeight;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        float dist = cameraDistance;

        // Pull the camera in when something (e.g. a shelf) is between it and the player. Invisible
        // seals sit on the Ignore Raycast layer, which DefaultRaycastLayers leaves out.
        if (Physics.SphereCast(pivot, 0.2f, rot * Vector3.back, out RaycastHit hit, cameraDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.transform != transform && !hit.transform.IsChildOf(transform))
            dist = hit.distance;

        cam.SetPositionAndRotation(pivot - rot * Vector3.forward * dist, rot);
    }
}

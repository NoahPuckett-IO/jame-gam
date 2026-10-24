using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float walkSpeed = 11.5f;
    [SerializeField] float dashSpeed = 36f;
    [SerializeField] float dashTime = 0.22f;
    [SerializeField] float dashCooldown = 5f;
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

    public bool InCutscene { get; private set; }
    Vector3 scriptedMove;

    public bool IsGrounded => controller != null && controller.isGrounded;
    public float WalkSpeed => walkSpeed;
    public float DashReady => Mathf.Clamp01(1f - (dashReadyAt - Time.time) / dashCooldown);
    public event System.Action Dashed;
    float dashUntil, dashReadyAt;
    Vector3 dashDir;
    bool airControl;
    public event System.Action Jumped;

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

    public event System.Action Respawned;

    public void Respawn()
    {
        Teleport(checkpointPos + Vector3.up * 0.5f, checkpointRot);
        Respawned?.Invoke();
    }

    static float shake;
    public static void Shake(float amount) => shake = Mathf.Max(shake, amount);

    public void BeginCutscene()
    {
        InCutscene = true;
        scriptedMove = Vector3.zero;
    }

    public void EndCutscene() => InCutscene = false;

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

            Vector2 input = Time.time < lockedUntil ? Vector2.zero : moveAction.ReadValue<Vector2>();
            moveDir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
            speed = walkSpeed;
            if (sprintAction.WasPressedThisFrame() && Time.time >= dashReadyAt && Time.time >= lockedUntil)
            {
                dashDir = controller.isGrounded && moveDir.sqrMagnitude > 0.01f ? moveDir.normalized : transform.forward;
                dashDir.y = 0f;
                dashUntil = Time.time + dashTime;
                dashReadyAt = Time.time + dashCooldown;
                Dashed?.Invoke();
            }
        }

        bool grounded = controller.isGrounded;
        if (grounded && verticalVelocity <= 0f) airControl = false;
        bool steer = InCutscene || grounded || airControl;
        if (steer && moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(new Vector3(moveDir.x, 0f, moveDir.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        if (controller.isGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = -2f;

            if (!InCutscene && Time.time >= lockedUntil && jumpAction.WasPressedThisFrame())
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                Jumped?.Invoke();
            }
        }

        verticalVelocity += gravity * Time.deltaTime;

        float t = InCutscene ? 30f : (iceZones > 0 ? IceTraction : traction);
        if (!InCutscene && Time.time < dashUntil)
        {
            planarVelocity = dashDir.normalized * dashSpeed;
            transform.rotation = Quaternion.LookRotation(dashDir);
        }
        else if (steer)
            planarVelocity = Vector3.Lerp(planarVelocity, moveDir * speed, 1f - Mathf.Exp(-t * Time.deltaTime));
        else if (planarVelocity.magnitude > walkSpeed)
            planarVelocity = Vector3.MoveTowards(planarVelocity, planarVelocity.normalized * walkSpeed, 40f * Time.deltaTime);
        knockback = Vector3.Lerp(knockback, Vector3.zero, 1f - Mathf.Exp(-4f * Time.deltaTime));

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

    public void Launch(float upSpeed)
    {
        verticalVelocity = upSpeed;
        airControl = true;
        Jumped?.Invoke();
    }

    public void Knock(Vector3 push)
    {
        knockback = new Vector3(push.x, 0, push.z);
        verticalVelocity = Mathf.Max(verticalVelocity, push.y);
    }

    public bool CameraLocked { get; set; }

    void LateUpdate()
    {
        if (cam == null || CameraLocked) return;

        Vector3 pivot = transform.position + Vector3.up * cameraHeight;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        float dist = cameraDistance;

        if (Physics.SphereCast(pivot, 0.2f, rot * Vector3.back, out RaycastHit hit, cameraDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.transform != transform && !hit.transform.IsChildOf(transform))
            dist = hit.distance;

        var shakeOffset = Random.insideUnitSphere * shake;
        shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 2.5f);
        cam.SetPositionAndRotation(pivot - rot * Vector3.forward * dist + shakeOffset, rot);
    }
}

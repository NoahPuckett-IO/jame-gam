using UnityEngine;

// Drives Henry's Animator from the PlayerController: walk/sprint blend from ground speed, jump and
// fall, the pick-up motion, and the arms layer while carrying something.
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] float walkSpeed = 20f, sprintSpeed = 40f;

    PlayerController player;
    Vector3 lastPos;
    float speedParam, carryWeight, carryTarget;

    static readonly int SpeedId = Animator.StringToHash("Speed");
    static readonly int GroundedId = Animator.StringToHash("Grounded");
    static readonly int VertId = Animator.StringToHash("VerticalSpeed");
    static readonly int JumpId = Animator.StringToHash("Jump");
    static readonly int PickId = Animator.StringToHash("PickUp");

    void Awake()
    {
        player = GetComponent<PlayerController>();
        lastPos = transform.position;
        if (player != null) player.Jumped += () => animator.SetTrigger(JumpId);
    }

    void Update()
    {
        if (animator == null) return;
        var delta = transform.position - lastPos;
        lastPos = transform.position;
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        var v = delta / dt;
        float ground = new Vector2(v.x, v.z).magnitude;

        // 0 = idle, 1 = walking pace, 2 = sprinting pace.
        float target = ground < 0.5f ? 0f : ground <= walkSpeed ? ground / walkSpeed : 1f + (ground - walkSpeed) / (sprintSpeed - walkSpeed);
        speedParam = Mathf.MoveTowards(speedParam, Mathf.Clamp(target, 0f, 2f), dt * 6f);
        animator.SetFloat(SpeedId, speedParam);
        animator.SetBool(GroundedId, player == null || player.IsGrounded || Mathf.Abs(v.y) < 0.5f);
        animator.SetFloat(VertId, v.y);
        // Walk/run cycles play faster than real time when Henry hurries.
        animator.speed = speedParam > 1.2f ? 1.25f : 1f;

        carryWeight = Mathf.MoveTowards(carryWeight, carryTarget, dt * 5f);
        if (animator.layerCount > 1) animator.SetLayerWeight(1, carryWeight);
    }

    public void PlayPickUp() => animator.SetTrigger(PickId);

    public void SetCarrying(bool carrying) => carryTarget = carrying ? 1f : 0f;

    public void Setup(Animator anim, float walk, float sprint)
    {
        animator = anim;
        walkSpeed = walk;
        sprintSpeed = sprint;
    }
}

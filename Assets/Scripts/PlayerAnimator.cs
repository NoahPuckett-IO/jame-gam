using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] Animator animator;

    PlayerController player;
    Vector3 lastPos;
    float speedParam, carryWeight, carryTarget;
    bool scripted;

    static readonly int SpeedId = Animator.StringToHash("Speed");
    static readonly int GroundedId = Animator.StringToHash("Grounded");
    static readonly int JumpId = Animator.StringToHash("Jump");
    static readonly int PickId = Animator.StringToHash("PickUp");
    static readonly int HitId = Animator.StringToHash("Hit");

    public Animator Animator => animator;

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
        float ground = new Vector2(delta.x, delta.z).magnitude / dt;
        if (ground > 200f) ground = 0f;

        speedParam = Mathf.Lerp(speedParam, ground, 1f - Mathf.Exp(-12f * dt));
        animator.SetFloat(SpeedId, speedParam);
        animator.SetBool(GroundedId, player == null || player.IsGrounded);
        animator.speed = scripted ? 1f : Mathf.Clamp(speedParam / 22f, 1f, 1.6f);

        carryWeight = Mathf.MoveTowards(carryWeight, carryTarget, dt * 5f);
        if (animator.layerCount > 1) animator.SetLayerWeight(1, carryWeight);
    }

    public void PlayPickUp() => animator.SetTrigger(PickId);
    public void PlayHit() => animator.SetTrigger(HitId);
    public void SetCarrying(bool carrying) => carryTarget = carrying ? 1f : 0f;

    public void Play(string state, float fade = 0.2f)
    {
        scripted = true;
        animator.CrossFadeInFixedTime(state, fade);
    }

    public void Resume()
    {
        scripted = false;
        animator.CrossFadeInFixedTime("Locomotion", 0.3f);
    }

    public Transform Bone(HumanBodyBones bone) => animator != null ? animator.GetBoneTransform(bone) : null;

    public void Setup(Animator anim) => animator = anim;
}

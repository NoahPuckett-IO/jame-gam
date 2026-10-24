using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FootGrounder : MonoBehaviour
{
    [SerializeField] float sole;
    [SerializeField] float groundOffset;

    Animator animator;
    PlayerController player;
    Transform hips, leftFoot, rightFoot, groundRef;
    float applied;

    public float Offset => applied;

    void Awake()
    {
        animator = GetComponent<Animator>();
        player = GetComponentInParent<PlayerController>();
        groundRef = player != null ? player.transform : transform;
    }

    void Start()
    {
        hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
    }

    void LateUpdate()
    {
        if (hips == null || leftFoot == null || rightFoot == null) return;
        if (player == null || player.IsGrounded)
        {
            float ground = groundRef.position.y + groundOffset;
            float low = Mathf.Min(leftFoot.position.y, rightFoot.position.y) - sole;
            float target = Mathf.Clamp(ground - low, -1f, 1f);
            applied = Mathf.Lerp(applied, target, 1f - Mathf.Exp(-20f * Time.deltaTime));
        }
        hips.position += Vector3.up * applied;
    }

    public void Setup(float soleHeight, float offset)
    {
        sole = soleHeight;
        groundOffset = offset;
    }
}

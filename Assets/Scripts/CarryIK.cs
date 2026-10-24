using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CarryIK : MonoBehaviour
{
    Animator animator;
    Transform item;
    Vector3 size, centerOffset;
    float weight, targetWeight;

    FootGrounder grounder;

    void Awake()
    {
        animator = GetComponent<Animator>();
        grounder = GetComponent<FootGrounder>();
    }

    public void Hold(Transform held, Vector3 localSize, Vector3 localCenterOffset)
    {
        item = held;
        size = localSize;
        centerOffset = localCenterOffset;
        targetWeight = 1f;
    }

    public void Release() => targetWeight = 0f;

    void Update()
    {
        weight = Mathf.MoveTowards(weight, targetWeight, Time.deltaTime * 4f);
        if (weight <= 0f && targetWeight <= 0f) item = null;
    }

    void OnAnimatorIK(int layer)
    {
        if (item == null || weight <= 0.001f) return;
        var center = item.position + item.TransformVector(centerOffset);
        float lift = grounder != null ? grounder.Offset : 0f;
        var low = center - item.up * (size.y * 0.3f) - Vector3.up * lift;
        Place(AvatarIKGoal.LeftHand, AvatarIKHint.LeftElbow, low - item.right * (size.x / 2 + 0.03f), -item.right);
        Place(AvatarIKGoal.RightHand, AvatarIKHint.RightElbow, low + item.right * (size.x / 2 + 0.03f), item.right);
    }

    void Place(AvatarIKGoal goal, AvatarIKHint elbow, Vector3 position, Vector3 outward)
    {
        animator.SetIKPositionWeight(goal, weight);
        animator.SetIKRotationWeight(goal, 0f);
        animator.SetIKPosition(goal, position);
        animator.SetIKHintPositionWeight(elbow, weight);
        animator.SetIKHintPosition(elbow, transform.position + Vector3.up * 1.2f + outward * 0.9f);
    }
}

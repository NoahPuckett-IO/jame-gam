using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CarryIK : MonoBehaviour
{
    Animator animator;
    Transform item;
    Vector3 size, centerOffset;
    float weight, targetWeight;

    void Awake() => animator = GetComponent<Animator>();

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
        float grip = Mathf.Min(0.35f, size.z * 0.35f);
        var back = center - item.forward * (size.z / 2 - grip);
        float up = -size.y * 0.1f;
        Place(AvatarIKGoal.LeftHand, back - item.right * (size.x / 2 + 0.03f) + item.up * up, item.right);
        Place(AvatarIKGoal.RightHand, back + item.right * (size.x / 2 + 0.03f) + item.up * up, -item.right);
    }

    void Place(AvatarIKGoal goal, Vector3 position, Vector3 inward)
    {
        animator.SetIKPositionWeight(goal, weight);
        animator.SetIKRotationWeight(goal, weight * 0.4f);
        animator.SetIKPosition(goal, position);
        animator.SetIKRotation(goal, Quaternion.LookRotation(item.forward, -inward));
    }
}

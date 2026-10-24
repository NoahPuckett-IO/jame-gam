using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HeadLook : MonoBehaviour
{
    public Transform target;
    public Vector3 point;
    public float weight;

    Animator animator;

    void Awake() => animator = GetComponent<Animator>();

    void OnAnimatorIK(int layer)
    {
        if (weight <= 0.001f) return;
        animator.SetLookAtWeight(weight, 0.35f, 0.9f, 1f, 0.6f);
        animator.SetLookAtPosition(target != null ? target.position : point);
    }
}

using UnityEngine;

// Turns a humanoid's head and upper body toward a target (Animator look-at IK). Used in cutscenes
// so Henry can look up at the UFO, or around the store when he wakes up.
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

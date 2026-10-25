using UnityEngine;

// hand and foot IK goals that rides (zipline, ladder, slide) set every frame
[RequireComponent(typeof(Animator))]
public class LimbTargets : MonoBehaviour
{
    struct Goal
    {
        public bool on;
        public Vector3 position, hint;
    }

    static readonly AvatarIKHint[] Hints = { AvatarIKHint.LeftKnee, AvatarIKHint.RightKnee, AvatarIKHint.LeftElbow, AvatarIKHint.RightElbow };

    readonly Goal[] goals = new Goal[4];
    Animator animator;
    float weight, targetWeight;

    void Awake() => animator = GetComponent<Animator>();

    public void Set(AvatarIKGoal goal, Vector3 position, Vector3 hint)
    {
        goals[(int)goal] = new Goal { on = true, position = position, hint = hint };
        targetWeight = 1f;
    }

    public void Release(AvatarIKGoal goal) => goals[(int)goal].on = false;

    public void ReleaseAll() => targetWeight = 0f;

    public void Snap() => weight = targetWeight;

    void Update()
    {
        weight = Mathf.MoveTowards(weight, targetWeight, Time.deltaTime * 6f);
        if (weight <= 0f && targetWeight <= 0f)
            for (int i = 0; i < goals.Length; i++) goals[i].on = false;
    }

    void OnAnimatorIK(int layer)
    {
        if (weight <= 0.001f) return;
        for (int i = 0; i < goals.Length; i++)
        {
            if (!goals[i].on) continue;
            var goal = (AvatarIKGoal)i;
            animator.SetIKPositionWeight(goal, weight);
            animator.SetIKRotationWeight(goal, 0f);
            animator.SetIKPosition(goal, goals[i].position);
            animator.SetIKHintPositionWeight(Hints[i], weight);
            animator.SetIKHintPosition(Hints[i], goals[i].hint);
        }
    }
}

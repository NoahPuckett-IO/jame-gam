using UnityEngine;
using UnityEngine.InputSystem;

// shared plumbing for things that take over the player for a while: a zipline, a ladder, a slide
[DefaultExecutionOrder(50)]
public abstract class PlayerRide : MonoBehaviour
{
    protected PlayerController player;
    protected PlayerAnimator anim;
    protected LimbTargets limbs;
    protected Transform model;
    protected bool riding;
    InputAction interact;
    FootGrounder grounder;
    Vector3 modelPos;
    Quaternion modelRot;
    float cooldownUntil;

    // Henry's proportions, read once from his rig so the hang, climb and lying poses fit him
    protected static float ShoulderHeight = 1.29f, ShoulderHalfWidth = 0.17f, ArmLength = 0.47f, HeadHalfWidth = 0.33f, LieClearance = 0.12f;
    protected const float LieTilt = 10f;
    static bool measured;

    protected virtual void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;
        anim = player.GetComponent<PlayerAnimator>();
        if (anim != null && anim.Animator != null)
        {
            model = anim.Animator.transform;
            limbs = model.GetComponent<LimbTargets>();
            if (limbs == null) limbs = model.gameObject.AddComponent<LimbTargets>();
            grounder = model.GetComponent<FootGrounder>();
            modelPos = model.localPosition;
            modelRot = model.localRotation;
            Measure();
        }
        interact = InputSystem.actions.FindAction("Player/Interact", false);
    }

    void Measure()
    {
        if (measured) return;
        measured = true;
        var a = anim.Animator;
        var upper = a.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        var lower = a.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        var hand = a.GetBoneTransform(HumanBodyBones.LeftHand);
        var inv = Quaternion.Inverse(model.rotation);
        float feet = player.transform.position.y - 1f;
        if (upper != null && lower != null && hand != null)
        {
            ShoulderHeight = upper.position.y - feet;
            ShoulderHalfWidth = Mathf.Abs((inv * (upper.position - model.position)).x);
            ArmLength = Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position);
        }
        float headFrom = ShoulderHeight + 0.2f, head = 0f;

        // lying on his back with the upper body raised a little: how far the pivot must sit off the surface so nothing sinks in
        float sin = Mathf.Sin(LieTilt * Mathf.Deg2Rad), cos = Mathf.Cos(LieTilt * Mathf.Deg2Rad), clear = 0f;
        foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            foreach (var w in WorldVertices(smr))
            {
                var local = inv * (w - model.position);
                // feet and shins are left out: their pose changes, and lying flat they rest on the tray anyway
                if (local.y < 0.45f) continue;
                clear = Mathf.Max(clear, -local.y * sin + Mathf.Abs(local.z) * cos);
                if (local.y > headFrom && Mathf.Abs(local.x) < 0.6f && Mathf.Abs(local.z) < 0.5f) head = Mathf.Max(head, Mathf.Abs(local.x));
            }
        }
        if (clear > 0.01f) LieClearance = clear + 0.02f;
        if (head > 0.1f) HeadHalfWidth = head;
    }

    // the skinned mesh in world space; some rigs bake in another unit scale, so fit the result to the renderer's bounds
    static Vector3[] WorldVertices(SkinnedMeshRenderer smr)
    {
        var baked = new Mesh();
        smr.BakeMesh(baked, true);
        var v = baked.vertices;
        Destroy(baked);
        var t = smr.transform;
        var min = Vector3.one * float.MaxValue;
        var max = -min;
        for (int i = 0; i < v.Length; i++)
        {
            v[i] = t.position + t.rotation * v[i];
            min = Vector3.Min(min, v[i]);
            max = Vector3.Max(max, v[i]);
        }
        float want = smr.bounds.size.y, got = max.y - min.y;
        if (got > 1e-5f && want > 1e-5f && Mathf.Abs(got / want - 1f) > 0.1f)
        {
            float k = want / got;
            var c = (min + max) / 2f;
            for (int i = 0; i < v.Length; i++) v[i] = smr.bounds.center + (v[i] - c) * k;
        }
        return v;
    }

    protected bool InteractPressed => interact != null && interact.WasPressedThisFrame();

    // free to start: not in a cutscene, not already riding something, not carrying anything
    protected bool CanBoard
    {
        get
        {
            if (player == null || riding || player.Attached || player.InCutscene || Time.time < cooldownUntil) return false;
            var hands = player.GetComponent<PlayerInteractor>();
            return hands == null || !hands.Busy;
        }
    }

    protected void Board(string state)
    {
        riding = true;
        player.Attach();
        player.Respawned += OnRespawned;
        if (anim != null) anim.Play(state, 0.15f);
        if (grounder != null) grounder.enabled = false;
    }

    protected void Leave(Vector3 velocity, float cooldown = 0.6f)
    {
        if (!riding) return;
        riding = false;
        cooldownUntil = Time.time + cooldown;
        player.Respawned -= OnRespawned;
        RestoreModel();
        if (limbs != null) limbs.ReleaseAll();
        if (grounder != null) grounder.enabled = true;
        if (anim != null) anim.Resume();
        var f = player.transform.forward;
        f.y = 0f;
        if (f.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.LookRotation(f);
        player.Detach(velocity);
        OnLeft();
    }

    void OnRespawned() => Abandon();

    // the player got teleported or respawned mid-ride: let go without touching its velocity
    protected void Abandon()
    {
        if (!riding) return;
        riding = false;
        cooldownUntil = Time.time + 0.6f;
        player.Respawned -= OnRespawned;
        RestoreModel();
        if (limbs != null) limbs.ReleaseAll();
        if (grounder != null) grounder.enabled = true;
        if (anim != null) anim.Resume();
        OnLeft();
    }

    protected virtual void OnLeft() { }

    protected bool StillRiding()
    {
        if (riding && !player.Attached) Abandon();
        return riding;
    }

    protected void RestoreModel()
    {
        if (model == null) return;
        model.localPosition = modelPos;
        model.localRotation = modelRot;
    }

    protected void Place(Vector3 position, Quaternion rotation) => player.transform.SetPositionAndRotation(position, rotation);

    protected void OnDisable()
    {
        if (riding && player != null) Leave(Vector3.zero);
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] float reach = 4f;
    [SerializeField] float dropDistance = 1.7f;
    const float BendTime = 0.55f, GrabAt = 0.3f;

    PlayerController player;
    PlayerAnimator anim;
    InputAction interact;
    Carryable held;
    bool busy;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        anim = GetComponent<PlayerAnimator>();
        ik = GetComponentInChildren<CarryIK>();
        grounder = GetComponentInChildren<FootGrounder>();
        interact = InputSystem.actions.FindAction("Player/Interact", true);
    }

    void Update()
    {
        if (player.InCutscene || busy || !player.IsGrounded) return;
        bool pressed = interact.WasPressedThisFrame() || (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);
        if (!pressed) return;

        if (held != null)
        {
            StartCoroutine(PutDown());
            return;
        }

        Carryable best = null;
        float bestScore = float.MaxValue;
        foreach (var c in FindObjectsByType<Carryable>())
        {
            if (c.IsHeld) continue;
            var to = c.transform.position - transform.position;
            to.y = 0;
            float d = to.magnitude;
            if (d > reach + c.Height) continue;
            float score = d - Vector3.Dot(to.normalized, transform.forward);
            if (score < bestScore) { bestScore = score; best = c; }
        }
        if (best != null) StartCoroutine(PickUp(best));
    }

    IEnumerator PickUp(Carryable item)
    {
        busy = true;
        var to = item.transform.position - transform.position;
        to.y = 0;
        if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
        player.LockMovement(BendTime);
        if (anim != null) anim.PlayPickUp();
        yield return new WaitForSeconds(GrabAt);
        held = item;
        var fromPos = item.transform.position;
        var fromRot = item.transform.rotation;
        held.PickUp(transform, Vector3.zero);
        heldCenterOffset = held.CenterOffsetLocal();
        heldSize = held.LocalSize();
        grabFrom = fromPos;
        grabFromRot = fromRot;
        grabStart = Time.time;

        if (ik != null) ik.Hold(held.transform, heldSize, heldCenterOffset);
        yield return new WaitForSeconds(BendTime - GrabAt);
        busy = false;
    }

    void LateUpdate()
    {
        if (held == null) return;
        float headTop = transform.position.y + 1.08f + (grounder != null ? grounder.Offset : 0f);
        var center = transform.position + transform.forward * 0.05f;
        center.y = headTop + heldSize.y * 0.5f;

        var rot = transform.rotation;
        var pos = center - rot * heldCenterOffset;
        float k = Mathf.SmoothStep(0f, 1f, (Time.time - grabStart) / GlideTime);
        held.transform.SetPositionAndRotation(Vector3.Lerp(grabFrom, pos, k), Quaternion.Slerp(grabFromRot, rot, k));
    }

    const float GlideTime = 0.35f;
    Vector3 heldCenterOffset, heldSize, grabFrom;
    Quaternion grabFromRot;
    float grabStart;
    CarryIK ik;
    FootGrounder grounder;

    IEnumerator PutDown()
    {
        busy = true;
        player.LockMovement(BendTime);
        if (anim != null)
        {
            anim.PlayPickUp();
            anim.SetCarrying(false);
        }
        if (ik != null) ik.Release();
        yield return new WaitForSeconds(GrabAt);
        var forward = transform.forward;
        held.PutDown(transform.position + forward * dropDistance, Quaternion.LookRotation(forward));
        held = null;
        yield return new WaitForSeconds(BendTime - GrabAt);
        busy = false;
    }
}

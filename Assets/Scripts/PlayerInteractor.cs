using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Lets the player pick up a nearby Carryable with Interact (E) and put it down in front of them.
// Henry bends down for both; the item attaches (or lets go) partway through the motion, and he
// carries it in both arms in front of his chest.
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

        // Nearest carryable in reach, roughly in front of the player.
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
        // Turn to face it, then bend down and lift.
        var to = item.transform.position - transform.position;
        to.y = 0;
        if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
        player.LockMovement(BendTime);
        if (anim != null) anim.PlayPickUp();
        yield return new WaitForSeconds(GrabAt);
        held = item;
        float h = item.Height;
        held.PickUp(transform, new Vector3(0, 0.15f + h * 0.5f, 0.7f + h * 0.35f));
        if (anim != null) anim.SetCarrying(true);
        yield return new WaitForSeconds(BendTime - GrabAt);
        busy = false;
    }

    IEnumerator PutDown()
    {
        busy = true;
        player.LockMovement(BendTime);
        if (anim != null)
        {
            anim.PlayPickUp();
            anim.SetCarrying(false);
        }
        yield return new WaitForSeconds(GrabAt);
        var forward = transform.forward;
        held.PutDown(transform.position + forward * dropDistance, Quaternion.LookRotation(forward));
        held = null;
        yield return new WaitForSeconds(BendTime - GrabAt);
        busy = false;
    }
}

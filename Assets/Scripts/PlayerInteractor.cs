using UnityEngine;
using UnityEngine.InputSystem;

// Lets the player pick up a nearby Carryable with Interact (E) and put it down in front of them.
[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] float reach = 4f;
    [SerializeField] float dropDistance = 1.7f;

    PlayerController player;
    InputAction interact;
    Carryable held;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        interact = InputSystem.actions.FindAction("Player/Interact", true);
    }

    void Update()
    {
        if (player.InCutscene) return;
        bool pressed = interact.WasPressedThisFrame() || (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);
        if (!pressed) return;

        if (held != null)
        {
            var forward = transform.forward;
            held.PutDown(transform.position + forward * dropDistance, Quaternion.LookRotation(forward));
            held = null;
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
        if (best == null) return;

        held = best;
        held.PickUp(transform, new Vector3(0, 1.1f + held.Height / 2, 0));
    }
}

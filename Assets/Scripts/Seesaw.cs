using UnityEngine;
using UnityEngine.Events;

// A ruler balanced on a can. It rests with its long far end down, tucked under a fallen cereal box.
// When the player stands on the short near end it tips the other way (a set animation, not
// physics); the long end swings up and lifts the box (`onTipped`). After that it stays tipped.
public class Seesaw : MonoBehaviour
{
    [Tooltip("Rotates about its local X axis. The plank is a child; local +Z is the far (lifting) end.")]
    [SerializeField] Transform pivot;
    [SerializeField] float restAngle = 6f;    // far end down
    [SerializeField] float tippedAngle = -10f; // near end down
    [SerializeField] float tipTime = 0.45f;
    [SerializeField] UnityEvent onTipped = new UnityEvent();
    [SerializeField] LiftedGate gate;

    PlayerController player;
    float angle, timer = -1f;
    bool tipped;

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        angle = restAngle;
        Apply();
    }

    void Update()
    {
        if (!tipped && PlayerOnNearEnd())
        {
            tipped = true;
            timer = 0f;
        }
        if (timer < 0f) return;

        timer += Time.deltaTime;
        float k = Mathf.Clamp01(timer / tipTime);
        // Ease in-out with a small settle at the end.
        float e = k * k * (3f - 2f * k) + Mathf.Sin(k * Mathf.PI * 2f) * (1f - k) * 0.06f;
        angle = Mathf.LerpUnclamped(restAngle, tippedAngle, e);
        Apply();

        if (k >= 0.55f && gate != null)
        {
            gate.Lift();
            onTipped.Invoke();
            gate = null;
        }
        if (k >= 1f) timer = -1f;
    }

    // Standing on the plank = a short ray down from the player hits it, on the near side of the pivot.
    bool PlayerOnNearEnd()
    {
        if (player == null) return false;
        if (!Physics.Raycast(player.transform.position, Vector3.down, out var hit, 2.5f, ~0, QueryTriggerInteraction.Ignore)) return false;
        if (!hit.transform.IsChildOf(pivot)) return false;
        return pivot.InverseTransformPoint(hit.point).z < -0.3f;
    }

    void Apply() => pivot.localRotation = Quaternion.Euler(angle, 0, 0);

    public void Setup(Transform pivotTransform, float rest, float tippedTo, LiftedGate liftedGate)
    {
        pivot = pivotTransform;
        restAngle = rest;
        tippedAngle = tippedTo;
        gate = liftedGate;
    }
}

using System.Collections.Generic;
using UnityEngine;

public class Seesaw : MonoBehaviour
{
    [Tooltip("Rotates about its local X axis. The plank is a child; local +Z is the far (lifting) end.")]
    [SerializeField] Transform pivot;
    [SerializeField] float restAngle = 6f;
    [SerializeField] float tippedAngle = -10f;
    [SerializeField] float tipTime = 0.6f;
    [SerializeField] float farArm = 14f, thickness = 0.3f, yaw;
    [SerializeField] LeverLoad[] loads;
    [Header("Balance")]
    [Tooltip("Swing back up when nothing holds the near end down. Off: it stays tipped once stepped on.")]
    [SerializeField] bool springsBack;
    [Tooltip("A level pan carried on the near end; weights (carryables) resting on it hold the lever down.")]
    [SerializeField] Transform pan;
    [SerializeField] float nearArm = 4f;

    PlayerController player;
    float angle, from, timer = -1f;
    bool tipped;

    public bool Tipped => tipped;

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        angle = restAngle;
        Apply();
    }

    void Update()
    {
        bool held = PlayerOnNearEnd() || Weighted();
        bool want = springsBack ? held : tipped || held;
        if (want != tipped)
        {
            tipped = want;
            from = angle;
            timer = 0f;
        }
        if (timer < 0f) return;

        timer += Time.deltaTime;
        float k = Mathf.Clamp01(timer / tipTime);
        float e = k * k * (3f - 2f * k) + Mathf.Sin(k * Mathf.PI * 2f) * (1f - k) * 0.05f;
        angle = Mathf.LerpUnclamped(from, tipped ? tippedAngle : restAngle, e);
        Apply();
        if (k >= 1f) timer = -1f;
    }

    bool OnNearEnd(Vector3 from, float reach)
    {
        foreach (var hit in Physics.RaycastAll(from, Vector3.down, reach, ~0, QueryTriggerInteraction.Ignore))
        {
            if (pan != null && hit.transform.IsChildOf(pan)) return true;
            if (hit.transform.IsChildOf(pivot) && pivot.InverseTransformPoint(hit.point).z < -0.3f) return true;
        }
        return false;
    }

    bool PlayerOnNearEnd() =>
        player != null && player.IsGrounded && OnNearEnd(player.transform.position, 2.5f);

    // weights left on the pan; they ride up and down with it
    readonly List<Transform> riders = new List<Transform>();

    bool Weighted()
    {
        riders.Clear();
        if (pan == null) return false;
        foreach (var c in FindObjectsByType<Carryable>())
            if (!c.IsHeld && OnNearEnd(c.transform.position + Vector3.up * 0.6f, 1.2f)) riders.Add(c.transform);
        return riders.Count > 0;
    }

    void Apply()
    {
        pivot.rotation = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(angle, 0, 0);
        if (pan != null)
        {
            // the pan hangs level under the near tip, like a balance scale
            var tip = pivot.TransformPoint(new Vector3(0, thickness / 2, -nearArm));
            var moved = tip - pan.position;
            pan.SetPositionAndRotation(tip, Quaternion.Euler(0, yaw, 0));
            foreach (var r in riders)
                if (r != null) r.position += moved;
        }
        var tipTop = pivot.TransformPoint(new Vector3(0, thickness / 2, farArm));
        if (loads != null)
            foreach (var l in loads)
                if (l != null) l.OnTip(tipTop);
    }

    public void Setup(Transform pivotTransform, float rest, float tippedTo, float far, float thick, float yawDegrees, params LeverLoad[] leverLoads)
    {
        pivot = pivotTransform;
        restAngle = rest;
        tippedAngle = tippedTo;
        farArm = far;
        thickness = thick;
        yaw = yawDegrees;
        loads = leverLoads;
    }

    public void SetupBalance(Transform nearPan, float near)
    {
        springsBack = true;
        pan = nearPan;
        nearArm = near;
    }
}

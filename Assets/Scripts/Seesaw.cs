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

    PlayerController player;
    float angle, timer = -1f;
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
        if (!tipped && PlayerOnNearEnd())
        {
            tipped = true;
            timer = 0f;
        }
        if (timer < 0f) return;

        timer += Time.deltaTime;
        float k = Mathf.Clamp01(timer / tipTime);
        float e = k * k * (3f - 2f * k) + Mathf.Sin(k * Mathf.PI * 2f) * (1f - k) * 0.05f;
        angle = Mathf.LerpUnclamped(restAngle, tippedAngle, e);
        Apply();
        if (k >= 1f) timer = -1f;
    }

    bool PlayerOnNearEnd()
    {
        if (player == null) return false;
        if (!Physics.Raycast(player.transform.position, Vector3.down, out var hit, 2.5f, ~0, QueryTriggerInteraction.Ignore)) return false;
        if (!hit.transform.IsChildOf(pivot)) return false;
        return pivot.InverseTransformPoint(hit.point).z < -0.3f;
    }

    void Apply()
    {
        pivot.rotation = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(angle, 0, 0);
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
}

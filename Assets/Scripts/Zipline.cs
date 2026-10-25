using UnityEngine;

// press E under the handle to grab it and ride the line down to the far end
public class Zipline : PlayerRide
{
    [SerializeField] Vector3 start, end;
    [SerializeField] Transform handle;
    [SerializeField] float grabRadius = 3.2f;
    [SerializeField] float topSpeed = 20f;

    const float HandDrop = 0.3f;
    // hands wide enough on the bar that the arms pass outside his head, body an arm's length (nearly straight) below
    static float Spread => Mathf.Max(0.3f, HeadHalfWidth + 0.1f);
    static float BodyBelow
    {
        get
        {
            float arm = ArmLength * 0.95f, side = Spread - ShoulderHalfWidth;
            return ShoulderHeight - 1f + Mathf.Sqrt(Mathf.Max(0.01f, arm * arm - side * side)) + 0.06f;
        }
    }
    float t, speed, swing, swingVel, returnT = 1f, mountT;
    Vector3 mountFrom;

    Vector3 Dir => (end - start).normalized;
    Vector3 HandleAt(float k) => Vector3.Lerp(start, end, k) + Vector3.down * HandDrop;

    void Update()
    {
        if (player == null) return;
        if (riding)
        {
            if (StillRiding()) Ride();
            return;
        }

        if (returnT < 1f)
        {
            returnT = Mathf.Min(1f, returnT + Time.deltaTime / 1.6f);
            if (handle != null) handle.position = HandleAt(1f - Mathf.SmoothStep(0f, 1f, returnT));
        }
        if (!CanBoard || returnT < 1f || !player.IsGrounded || !InteractPressed) return;
        var h = HandleAt(0f);
        var feet = player.transform.position + Vector3.down;
        var flat = new Vector3(h.x - feet.x, 0f, h.z - feet.z);
        if (flat.magnitude > grabRadius || h.y - feet.y > 3.4f || h.y < feet.y) return;

        t = 0f;
        speed = 2f;
        mountT = 0f;
        mountFrom = player.transform.position;
        swing = 0f;
        swingVel = -40f;
        Board("Hang");
        Ride();
        if (limbs != null) limbs.Snap();
    }

    void Ride()
    {
        float len = Vector3.Distance(start, end);
        float slope = Mathf.Max(0.05f, -Dir.y);
        speed = Mathf.MoveTowards(speed, topSpeed, (9f + 20f * slope) * Time.deltaTime);
        t += speed * Time.deltaTime / len;

        swingVel += (-swing * 30f - swingVel * 2.2f) * Time.deltaTime;
        swing += swingVel * Time.deltaTime;

        var grip = HandleAt(Mathf.Min(t, 1f));
        if (handle != null) handle.position = grip;
        var flatDir = new Vector3(Dir.x, 0f, Dir.z).normalized;
        var right = Vector3.Cross(Vector3.up, flatDir);
        var tilt = Quaternion.AngleAxis(swing, right);
        var hang = grip + tilt * (Vector3.down * BodyBelow);
        // a quick hop up to the bar instead of snapping onto it
        mountT = Mathf.Min(1f, mountT + Time.deltaTime / 0.22f);
        Place(Vector3.Lerp(mountFrom, hang, Mathf.SmoothStep(0f, 1f, mountT)), tilt * Quaternion.LookRotation(flatDir));

        if (limbs != null)
        {
            var outR = right;
            var wrist = grip + Vector3.down * 0.06f;
            limbs.Set(AvatarIKGoal.LeftHand, wrist - outR * Spread, grip - outR * (Spread + 0.6f) + Vector3.down * 0.4f);
            limbs.Set(AvatarIKGoal.RightHand, wrist + outR * Spread, grip + outR * (Spread + 0.6f) + Vector3.down * 0.4f);
        }

        if (player.JumpPressed && t > 0.1f) { Leave(flatDir * speed * 0.6f); return; }
        if (t >= 1f) Leave(flatDir * Mathf.Min(speed, 9f) + Vector3.up * 2f);
    }

    protected override void OnLeft() => returnT = 0f;

    public void Setup(Vector3 from, Vector3 to, Transform grip)
    {
        start = from;
        end = to;
        handle = grip;
    }
}

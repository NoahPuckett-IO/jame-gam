using UnityEngine;

// walk into the ladder (or press E) to grab it, W/S to climb, jump to let go; climbing past the top steps off onto the ledge
public class Ladder : PlayerRide
{
    [SerializeField] Vector3 foot;          // bottom of the ladder, on the floor, in its plane
    [SerializeField] Vector3 outward;       // points from the ladder toward the climber
    [SerializeField] float top;             // height of the ledge it leads to
    [SerializeField] float rungSpacing = 0.42f;
    [SerializeField] float firstRung = 0.42f;
    [SerializeField] float halfWidth = 0.5f;
    [SerializeField] Vector3 stepOff;       // where the climber's feet land on the ledge
    [SerializeField] bool fromTop;          // can also be climbed down from the ledge

    const float StandOff = 0.42f, ClimbSpeed = 3.4f;
    float height;                           // how far the climber's feet are above the floor
    float mountT, exitT = -1f;
    Vector3 mountFrom, exitFrom;
    float lastRung;
    int tapRung;
    bool descendHeld;

    protected override string BoardSound => "rung";

    // the climber's right while facing the ladder (they face -outward)
    Vector3 Right => Vector3.Cross(outward, Vector3.up).normalized;
    Vector3 BodyAt(float feetHeight) => foot + outward * StandOff + Vector3.up * (feetHeight + 1f);
    float RungY(int i) => foot.y + firstRung + i * rungSpacing;

    protected override void Start()
    {
        base.Start();
        lastRung = Mathf.Floor((top - foot.y - firstRung) / rungSpacing);
    }

    void Update()
    {
        if (player == null) return;
        if (riding)
        {
            if (StillRiding()) Climb();
            return;
        }
        if (!CanBoard || !player.IsGrounded) return;
        if (fromTop && TryMountFromTop()) return;

        var p = player.transform.position;
        var rel = p - (foot + outward * StandOff);
        float along = Vector3.Dot(rel, Right), away = Vector3.Dot(rel, outward);
        float feet = p.y - 1f - foot.y;
        if (Mathf.Abs(along) > halfWidth + 1f || away < -0.6f || away > 2.4f || feet > 0.8f || feet < -0.5f) return;
        Hud.Hint(this, "CLIMB");

        var input = player.MoveInput;
        var wish = Quaternion.Euler(0f, player.Yaw, 0f) * new Vector3(input.x, 0f, input.y);
        bool pushing = wish.sqrMagnitude > 0.25f && Vector3.Dot(wish.normalized, -outward) > 0.6f && away < 1.4f;
        if (!pushing && !InteractPressed) return;

        height = Mathf.Max(0f, feet);
        descendHeld = false;
        mountFrom = p;
        mountT = 0f;
        exitT = -1f;
        Board("Climb");
        tapRung = Mathf.FloorToInt(height / rungSpacing);
    }

    // standing on the ledge at the top: walk to the edge over the ladder (or press E) to swing on and climb down
    bool TryMountFromTop()
    {
        var p = player.transform.position;
        float feet = p.y - 1f;
        if (Mathf.Abs(feet - top) > 0.4f) return false;
        var fromLanding = p - stepOff;
        fromLanding.y = 0f;
        var toLadder = foot - stepOff;
        toLadder.y = 0f;
        float along = Vector3.Dot(p - foot, Right);
        if (fromLanding.magnitude > 2.4f || Mathf.Abs(along) > halfWidth + 0.6f) return false;
        Hud.Hint(this, "CLIMB DOWN");

        var input = player.MoveInput;
        var wish = Quaternion.Euler(0f, player.Yaw, 0f) * new Vector3(input.x, 0f, input.y);
        bool pushing = wish.sqrMagnitude > 0.25f && Vector3.Dot(wish.normalized, toLadder.normalized) > 0.6f
                       && Vector3.Dot(fromLanding, toLadder.normalized) > 0.3f;
        if (!pushing && !InteractPressed) return false;

        height = top - foot.y - 1.25f;
        descendHeld = pushing;
        mountFrom = p;
        mountT = 0f;
        exitT = -1f;
        Board("Climb");
        tapRung = Mathf.FloorToInt(height / rungSpacing);
        return true;
    }

    void Climb()
    {
        var face = Quaternion.LookRotation(-outward);
        if (exitT >= 0f)
        {
            exitT += Time.deltaTime / 0.55f;
            float k = Mathf.Clamp01(exitT);
            var land = stepOff + Vector3.up;
            var over = new Vector3(exitFrom.x, land.y + 0.6f, exitFrom.z);
            var pos = k < 0.5f ? Vector3.Lerp(exitFrom, over, Mathf.SmoothStep(0f, 1f, k * 2f))
                               : Vector3.Lerp(over, land, Mathf.SmoothStep(0f, 1f, k * 2f - 1f));
            Place(pos, face);
            SetLimbs(height, true);
            if (exitT >= 1f) Leave(Vector3.zero);
            return;
        }

        float v = player.MoveInput.y;
        // swung on from the top while walking at it: keep walking that way to climb down, until the stick is let go
        if (descendHeld) { if (v > 0.2f) v = -v; else descendHeld = false; }
        if (Mathf.Abs(v) > 0.2f) height += Mathf.Sign(v) * ClimbSpeed * Time.deltaTime;
        // a tap each time a hand reaches the next rung
        int rung = Mathf.FloorToInt(height / rungSpacing);
        if (rung != tapRung) { tapRung = rung; Sfx.Play("rung", 0.5f, Random.Range(0.9f, 1.15f)); }

        if (height <= 0f && v < -0.2f) { Leave(outward * 2f); return; }
        height = Mathf.Max(0f, height);
        if (player.JumpPressed) { Leave(outward * 5f + Vector3.up * 4f); return; }

        mountT = Mathf.Min(1f, mountT + Time.deltaTime / 0.2f);
        var target = BodyAt(height);
        Place(Vector3.Lerp(mountFrom, target, Mathf.SmoothStep(0f, 1f, mountT)), face);
        SetLimbs(height, false);

        if (foot.y + height + 1.1f >= top)
        {
            exitT = 0f;
            exitFrom = player.transform.position;
        }
    }

    // hands and feet each grab every other rung, swapping sides, so climbing reads as hand-over-hand
    void SetLimbs(float feetHeight, bool topping)
    {
        if (limbs == null) return;
        var r = Right;
        var front = foot + outward * 0.06f;
        Vector3 Grip(float nominal, int phase, float side, float reach, out float lift)
        {
            float idx = (nominal - foot.y - firstRung) / rungSpacing;
            float q = (idx - phase) / 2f;
            float cycle = Mathf.Floor(q), f = q - cycle;
            int held = Mathf.Clamp((int)cycle * 2 + phase, 0, (int)lastRung);
            float k = f > 0.6f ? Mathf.SmoothStep(0f, 1f, (f - 0.6f) / 0.4f) : 0f;
            int next = Mathf.Min(held + 2, (int)lastRung);
            float y = Mathf.Lerp(RungY(held), RungY(next), k);
            lift = Mathf.Sin(k * Mathf.PI);
            return new Vector3(front.x, y, front.z) + r * side + outward * (reach + lift * 0.25f);
        }

        float baseY = foot.y + feetHeight;
        // highest a hand can grip while reaching forward to the rungs, so the arms never stretch past the rig
        float reach = StandOff - 0.06f, arm = ArmLength * 0.96f;
        float handTop = ShoulderHeight + Mathf.Sqrt(Mathf.Max(0.01f, arm * arm - reach * reach));
        float handNominal = topping ? top + 0.2f : baseY + handTop;
        var lh = Grip(handNominal, 0, -0.25f, 0f, out _);
        var rh = Grip(handNominal, 1, 0.25f, 0f, out _);
        if (topping) { lh.y = rh.y = Mathf.Max(lh.y, top + 0.05f); }
        // ankles sit just behind the rung so the toes rest on it, between the soles' height and a short step up
        float footNominal = baseY + 0.45f;
        var lf = Grip(footNominal, 1, -0.14f, 0.12f, out _);
        var rf = Grip(footNominal, 0, 0.14f, 0.12f, out _);
        var body = player.transform.position;
        limbs.Set(AvatarIKGoal.LeftHand, lh, body + Vector3.up * 0.2f - r * 0.7f + outward * 0.25f);
        limbs.Set(AvatarIKGoal.RightHand, rh, body + Vector3.up * 0.2f + r * 0.7f + outward * 0.25f);
        if (topping)
        {
            limbs.Release(AvatarIKGoal.LeftFoot);
            limbs.Release(AvatarIKGoal.RightFoot);
            return;
        }
        // knees bend forward, toward the ladder
        limbs.Set(AvatarIKGoal.LeftFoot, lf, body + Vector3.down * 0.3f - r * 0.2f - outward * 0.6f);
        limbs.Set(AvatarIKGoal.RightFoot, rf, body + Vector3.down * 0.3f + r * 0.2f - outward * 0.6f);
    }

    public void Setup(Vector3 bottom, Vector3 facing, float ledge, float spacing, float first, float width, Vector3 landing, bool down = false)
    {
        fromTop = down;
        foot = bottom;
        outward = new Vector3(facing.x, 0f, facing.z).normalized;
        top = ledge;
        rungSpacing = spacing;
        firstRung = first;
        halfWidth = width;
        stepOff = landing;
    }
}

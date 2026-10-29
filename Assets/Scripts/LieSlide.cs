using UnityEngine;

// step onto the slide and Henry drops onto his back and rides it feet first to the bottom
[RequireComponent(typeof(BoxCollider))]
public class LieSlide : PlayerRide
{
    [SerializeField] Vector3 top, bottom;   // centre line of the sliding surface
    [SerializeField] float exitSpeed = 12f;

    float t, speed, side;

    Vector3 Down => (bottom - top).normalized;
    Vector3 Normal
    {
        get
        {
            var across = Vector3.Cross(Vector3.up, Down).normalized;
            return Vector3.Cross(Down, across).normalized;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (riding || !CanBoard || other.GetComponent<PlayerController>() == null) return;
        var feet = player.transform.position + Vector3.down;
        var line = bottom - top;
        float k = Mathf.Clamp01(Vector3.Dot(feet - top, line) / line.sqrMagnitude);
        if (k > 0.92f) return;
        var onLine = top + line * k;
        if (Vector3.Dot(feet - onLine, Normal) > 1.2f) return;

        t = k;
        var across = Vector3.Cross(Vector3.up, Down).normalized;
        side = Vector3.Dot(feet - onLine, across);
        var flat = player.transform.forward;
        speed = Mathf.Max(6f, Vector3.Dot(new Vector3(flat.x, 0, flat.z), new Vector3(Down.x, 0, Down.z)) * player.WalkSpeed);
        Board("Lie");
        Slide();
        if (limbs != null) limbs.Snap();
    }

    protected override string BoardSound => "drop";
    protected override string LoopSound => "scrape";
    protected override float RideLoudness => Mathf.Lerp(0.2f, 0.7f, speed / 34f);
    protected override float RidePitch => Mathf.Lerp(0.8f, 1.4f, speed / 34f);

    void Update()
    {
        if (player == null || !riding) return;
        if (StillRiding()) Slide();
    }

    void Slide()
    {
        var line = bottom - top;
        float len = line.magnitude;
        float slope = Mathf.Clamp01(-Down.y);
        speed = Mathf.Min(speed + (8f + 26f * slope) * Time.deltaTime, 34f);
        t += speed * Time.deltaTime / len;
        side = Mathf.MoveTowards(side, 0f, Time.deltaTime * 1.5f);

        var n = Normal;
        var across = Vector3.Cross(Vector3.up, Down).normalized;
        var surface = top + line * Mathf.Min(t, 1f) + across * side;
        var flatDown = new Vector3(Down.x, 0f, Down.z).normalized;
        Place(surface + Vector3.up, Quaternion.LookRotation(flatDown));

        if (model != null)
        {
            // lying on his back, feet leading, head uphill and raised a little off the tray; the idle arms rest along his sides
            var uphill = -Down;
            var up = Quaternion.AngleAxis(LieTilt, across) * uphill;
            model.rotation = Quaternion.LookRotation(Vector3.Cross(across, up), up);
            model.position = surface + n * LieClearance;
        }
        if (limbs != null)
        {
            // legs out together along the tray, heels resting on it, knees easing up
            var heel = surface + n * 0.1f - Down * 0.12f;
            var knee = surface + n * 0.9f - Down * 0.55f;
            limbs.Set(AvatarIKGoal.LeftFoot, heel - across * 0.12f, knee - across * 0.15f);
            limbs.Set(AvatarIKGoal.RightFoot, heel + across * 0.12f, knee + across * 0.15f);
        }

        if (t >= 1f) Leave(flatDown * Mathf.Min(speed, exitSpeed), 1.2f);
    }

    public void Setup(Vector3 from, Vector3 to, float leaveSpeed)
    {
        top = from;
        bottom = to;
        exitSpeed = leaveSpeed;
    }
}

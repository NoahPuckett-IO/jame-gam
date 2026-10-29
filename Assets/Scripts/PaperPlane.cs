using System.Collections.Generic;
using UnityEngine;

// a big folded paper plane parked on top of the shelf. Step onto it and it glides out over the aisle, banks round
// in a circle on the way down, and sets Henry down on the shelf by the slide. It flies itself back home once he's
// wandered off (or straight away if he falls off mid-flight and respawns)
public class PaperPlane : PlayerRide
{
    [SerializeField] Transform plane;           // the paper and its deck collider; this object stays put as the boarding trigger
    [SerializeField] Vector3[] points;          // the route, which a Catmull-Rom curve runs through
    [SerializeField] float speed = 16f;
    [SerializeField] float deck = 0.6f;         // height of the walkable fold above the plane's origin
    [SerializeField] Vector3 exitFacing = Vector3.forward;

    const float Step = 0.25f;
    readonly List<Vector3> path = new List<Vector3>();
    Vector3 homePos;
    Quaternion homeRot;
    Collider[] solids;
    float s, roll, length;
    bool landed;

    protected override void Start()
    {
        base.Start();
        homePos = plane.position;
        homeRot = plane.rotation;
        solids = plane.GetComponentsInChildren<Collider>();
        Bake();
    }

    // the curve resampled at even spacing, so distance along it is just an index
    void Bake()
    {
        path.Clear();
        if (points == null || points.Length < 2) return;
        var dense = new List<Vector3>();
        for (int i = 0; i < points.Length - 1; i++)
        {
            var p0 = points[Mathf.Max(0, i - 1)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Mathf.Min(points.Length - 1, i + 2)];
            for (int k = 0; k < 24; k++)
            {
                float t = k / 24f;
                dense.Add(0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (3f * p1 - p0 - 3f * p2 + p3) * t * t * t));
            }
        }
        dense.Add(points[points.Length - 1]);
        path.Add(dense[0]);
        float carried = 0f;
        for (int i = 1; i < dense.Count; i++)
        {
            var a = dense[i - 1];
            float seg = Vector3.Distance(a, dense[i]);
            while (carried + seg >= Step)
            {
                float f = (Step - carried) / seg;
                a = Vector3.Lerp(a, dense[i], f);
                seg -= Step - carried;
                carried = 0f;
                path.Add(a);
            }
            carried += seg;
        }
        path.Add(dense[dense.Count - 1]);
        length = (path.Count - 1) * Step;
    }

    Vector3 At(float d)
    {
        float f = Mathf.Clamp(d, 0f, length) / Step;
        int i = Mathf.Min(path.Count - 2, Mathf.FloorToInt(f));
        return Vector3.Lerp(path[i], path[i + 1], f - i);
    }

    void OnTriggerStay(Collider other)
    {
        if (riding || landed || path.Count < 2 || !CanBoard || other.GetComponent<PlayerController>() == null) return;
        if (!player.IsGrounded) return;
        Board("CrouchIdle");
        foreach (var c in solids) c.enabled = false;
        s = 0f;
        roll = 0f;
    }

    void Update()
    {
        if (player == null) return;
        if (riding)
        {
            if (StillRiding()) Fly();
            return;
        }
        if (landed && (player.transform.position - plane.position).sqrMagnitude > 25f * 25f) Home();
    }

    void Fly()
    {
        // eases off the shelf, cruises, and slows right down for the landing
        float k = Mathf.Min(1f, Mathf.Min((s + 1f) / 10f, (length - s + 0.5f) / 12f));
        s = Mathf.Min(length, s + Mathf.Lerp(2.5f, speed, Mathf.SmoothStep(0f, 1f, k)) * Time.deltaTime);

        var pos = At(s);
        var ahead = At(s + 2f) - pos;
        var later = At(s + 6f) - At(s + 2f);
        if (ahead.sqrMagnitude < 1e-4f) ahead = plane.forward;
        // bank into the turn, levelling out for the touchdown
        float turn = Vector3.SignedAngle(new Vector3(ahead.x, 0, ahead.z), new Vector3(later.x, 0, later.z), Vector3.up);
        float level = Mathf.Clamp01((length - s) / 8f);
        roll = Mathf.Lerp(roll, Mathf.Clamp(-turn * 1.4f, -38f, 38f) * level, Time.deltaTime * 3f);
        var heading = Quaternion.LookRotation(Vector3.Lerp(new Vector3(ahead.x, 0, ahead.z), ahead, level).normalized);
        // a gentle bob so it feels like it's riding the air
        float bob = Mathf.Sin(Time.time * 2.3f) * 0.12f * level;
        plane.SetPositionAndRotation(pos + Vector3.up * bob, heading * Quaternion.Euler(0, 0, roll));

        Place(plane.TransformPoint(new Vector3(0, deck, -0.8f)) + plane.up * 1f, plane.rotation);

        if (s >= length)
        {
            landed = true;
            plane.rotation = heading;
            foreach (var c in solids) c.enabled = true;
            Place(plane.TransformPoint(new Vector3(0, deck + 0.05f, -0.8f)) + Vector3.up, Quaternion.LookRotation(exitFacing));
            Leave(Vector3.zero, 1f);
        }
    }

    protected override string BoardSound => "whoosh";
    protected override string LoopSound => "zip";
    protected override float RideLoudness => 0.3f;
    protected override float RidePitch => 0.6f;

    protected override void OnLeft()
    {
        if (!landed) Home();
    }

    void Home()
    {
        landed = false;
        plane.SetPositionAndRotation(homePos, homeRot);
        foreach (var c in solids) c.enabled = true;
    }

    public void Setup(Transform paper, Vector3[] route, float deckHeight, Vector3 facing)
    {
        plane = paper;
        points = route;
        deck = deckHeight;
        exitFacing = new Vector3(facing.x, 0, facing.z).normalized;
    }
}

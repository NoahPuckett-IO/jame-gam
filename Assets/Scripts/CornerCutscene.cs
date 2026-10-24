using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CornerCutscene : MonoBehaviour
{
    [SerializeField] Transform[] waypoints;
    [SerializeField] float walkSpeed = 11f;
    [SerializeField] float lookUpPitch = -32f;
    [SerializeField] float lookUpTime = 3f;
    [SerializeField] RectTransform barTop, barBottom;

    bool played;

    void OnTriggerEnter(Collider other)
    {
        if (played) return;
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        played = true;
        StartCoroutine(Play(player));
    }

    List<Vector3> BuildPath(Vector3 from)
    {
        var pts = new List<Vector3> { from };
        foreach (var w in waypoints) pts.Add(w.position);
        var path = new List<Vector3>();
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 p0 = pts[Mathf.Max(i - 1, 0)], p1 = pts[i], p2 = pts[i + 1], p3 = pts[Mathf.Min(i + 2, pts.Count - 1)];
            for (int s = 0; s < 24; s++)
            {
                float t = s / 24f, t2 = t * t, t3 = t2 * t;
                path.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
            }
        }
        path.Add(pts[pts.Count - 1]);
        return path;
    }

    IEnumerator Play(PlayerController player)
    {
        player.BeginCutscene();
        StartCoroutine(Bars(true));

        var path = BuildPath(player.transform.position);
        float camYaw = player.Yaw, camPitch = player.Pitch, yawVel = 0f, pitchVel = 0f, speed = 0f;
        int i = 1;
        while (i < path.Count)
        {
            var pos = player.transform.position;
            while (i < path.Count - 1 && Flat(path[i] - pos).magnitude < 2.5f) i++;
            var to = Flat(path[i] - pos);
            bool last = i == path.Count - 1;
            if (last && to.magnitude < 0.4f) break;

            float target = last ? Mathf.Min(walkSpeed, to.magnitude * 2.5f) : walkSpeed;
            speed = Mathf.MoveTowards(speed, target, walkSpeed * 2f * Time.deltaTime);
            player.ScriptedMove(to.normalized * speed);

            float targetYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            camYaw = Mathf.SmoothDampAngle(camYaw, targetYaw, ref yawVel, 0.45f);
            camPitch = Mathf.SmoothDamp(camPitch, 8f, ref pitchVel, 0.6f);
            player.SetLook(camYaw, camPitch);
            yield return null;
        }
        player.ScriptedMove(Vector3.zero);

        var end = waypoints[waypoints.Length - 1];
        float hallYaw = end.eulerAngles.y;
        for (float t = 0; t < lookUpTime; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0, 1, t / lookUpTime);
            camYaw = Mathf.SmoothDampAngle(camYaw, hallYaw, ref yawVel, 0.5f);
            player.SetLook(camYaw, Mathf.Lerp(camPitch, lookUpPitch, k));
            yield return null;
        }
        yield return new WaitForSeconds(0.8f);
        for (float t = 0; t < 1.2f; t += Time.deltaTime)
        {
            player.SetLook(hallYaw, Mathf.Lerp(lookUpPitch, 15f, Mathf.SmoothStep(0, 1, t / 1.2f)));
            yield return null;
        }

        player.SetCheckpoint(end.position, end.rotation);
        StartCoroutine(Bars(false));
        player.EndCutscene();
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

    IEnumerator Bars(bool show)
    {
        if (barTop == null || barBottom == null) yield break;
        barTop.gameObject.SetActive(true);
        barBottom.gameObject.SetActive(true);
        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            float h = Mathf.Lerp(show ? 0 : 1, show ? 1 : 0, Mathf.SmoothStep(0, 1, t / 0.5f)) * 0.12f;
            barTop.anchorMin = new Vector2(0, 1 - h);
            barBottom.anchorMax = new Vector2(1, h);
            yield return null;
        }
        if (!show)
        {
            barTop.gameObject.SetActive(false);
            barBottom.gameObject.SetActive(false);
        }
    }

    public void Setup(Transform[] points, RectTransform top, RectTransform bottom)
    {
        waypoints = points;
        barTop = top;
        barBottom = bottom;
    }
}

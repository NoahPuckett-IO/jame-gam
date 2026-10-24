using System.Collections;
using UnityEngine;

// Plays once when the player reaches the end of a hall: letterbox bars slide in, the character
// walks itself round the corner along the waypoints, stops, and the camera tilts up at the next
// towering aisle before handing control back. The last waypoint becomes the checkpoint.
[RequireComponent(typeof(BoxCollider))]
public class CornerCutscene : MonoBehaviour
{
    [SerializeField] Transform[] waypoints;
    [SerializeField] float walkSpeed = 10f;
    [SerializeField] float lookUpPitch = -35f;
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

    IEnumerator Play(PlayerController player)
    {
        player.BeginCutscene();
        StartCoroutine(Bars(true));

        float camYaw = player.Yaw, camPitch = player.Pitch;
        foreach (var wp in waypoints)
        {
            while (true)
            {
                var to = wp.position - player.transform.position;
                to.y = 0;
                if (to.magnitude < 0.6f) break;
                player.ScriptedMove(to.normalized * walkSpeed);
                // Camera swings round behind the character as it walks.
                float targetYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                camYaw = Mathf.MoveTowardsAngle(camYaw, targetYaw, 90f * Time.deltaTime);
                camPitch = Mathf.MoveTowards(camPitch, 10f, 20f * Time.deltaTime);
                player.SetLook(camYaw, camPitch);
                yield return null;
            }
        }
        player.ScriptedMove(Vector3.zero);

        // Face down the new hall and slowly look up at it.
        var last = waypoints[waypoints.Length - 1];
        float hallYaw = last.eulerAngles.y;
        for (float t = 0; t < lookUpTime; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0, 1, t / lookUpTime);
            camYaw = Mathf.MoveTowardsAngle(camYaw, hallYaw, 120f * Time.deltaTime);
            player.SetLook(camYaw, Mathf.Lerp(10f, lookUpPitch, k));
            yield return null;
        }
        yield return new WaitForSeconds(0.8f);
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            player.SetLook(hallYaw, Mathf.Lerp(lookUpPitch, 15f, Mathf.SmoothStep(0, 1, t)));
            yield return null;
        }

        player.SetCheckpoint(last.position, last.rotation);
        StartCoroutine(Bars(false));
        player.EndCutscene();
    }

    IEnumerator Bars(bool show)
    {
        if (barTop == null || barBottom == null) yield break;
        barTop.gameObject.SetActive(true);
        barBottom.gameObject.SetActive(true);
        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            float h = Mathf.Lerp(show ? 0 : 1, show ? 1 : 0, t / 0.5f) * 0.12f;
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

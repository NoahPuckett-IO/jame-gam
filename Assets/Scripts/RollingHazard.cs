using UnityEngine;

public class RollingHazard : MonoBehaviour
{
    [SerializeField] Vector3 offset = new Vector3(6f, 0f, 0f);
    [SerializeField] float travelSeconds = 2f;
    [SerializeField] float pauseSeconds = 0.6f;
    [SerializeField] float phase;
    [SerializeField] float radius = 1f;
    [SerializeField] Transform visual;

    Vector3 start, last, velocity;
    float hitCooldown;

    void Start()
    {
        start = last = transform.position;
        if (visual == null) visual = transform;
    }

    void Update()
    {
        float cycle = 2f * (travelSeconds + pauseSeconds);
        float t = Mathf.Repeat(Time.time + phase * cycle, cycle);
        float k;
        if (t < pauseSeconds) k = 0f;
        else if (t < pauseSeconds + travelSeconds) k = Mathf.SmoothStep(0f, 1f, (t - pauseSeconds) / travelSeconds);
        else if (t < 2f * pauseSeconds + travelSeconds) k = 1f;
        else k = 1f - Mathf.SmoothStep(0f, 1f, (t - 2f * pauseSeconds - travelSeconds) / travelSeconds);
        transform.position = start + offset * k;

        var moved = transform.position - last;
        last = transform.position;
        velocity = moved / Mathf.Max(Time.deltaTime, 1e-4f);
        if (moved.sqrMagnitude > 1e-6f)
        {
            var axis = Vector3.Cross(Vector3.up, moved.normalized);
            visual.Rotate(axis, moved.magnitude / Mathf.Max(radius, 0.1f) * Mathf.Rad2Deg, Space.World);
        }
        hitCooldown -= Time.deltaTime;
    }

    void OnTriggerStay(Collider other)
    {
        if (hitCooldown > 0f) return;
        var player = other.GetComponent<PlayerController>();
        if (player == null || player.InCutscene) return;
        var away = player.transform.position - transform.position;
        away.y = 0;
        var dir = velocity.sqrMagnitude > 1f ? velocity.normalized : away.normalized;
        player.Knock(dir * 20f + away.normalized * 6f + Vector3.up * 7f);
        player.LockMovement(0.35f);
        player.GetComponent<PlayerAnimator>()?.PlayHit();
        hitCooldown = 0.8f;
    }

    public void Setup(Vector3 move, float travel, float pause, float startPhase, float rollRadius, Transform rollVisual)
    {
        offset = move;
        travelSeconds = travel;
        pauseSeconds = pause;
        phase = startPhase;
        radius = rollRadius;
        visual = rollVisual;
    }
}

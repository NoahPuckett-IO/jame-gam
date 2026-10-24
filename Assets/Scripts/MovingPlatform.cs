using UnityEngine;

[DefaultExecutionOrder(-50)]
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] Vector3 offset = new Vector3(8f, 0f, 0f);
    [SerializeField] float travelSeconds = 3f;
    [SerializeField] float pauseSeconds = 0.8f;
    [SerializeField] float phase;

    Vector3 start;

    void Start() => start = transform.position;

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
    }

    public void Setup(Vector3 move, float travel, float pause, float startPhase)
    {
        offset = move;
        travelSeconds = travel;
        pauseSeconds = pause;
        phase = startPhase;
    }
}

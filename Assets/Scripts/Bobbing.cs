using UnityEngine;

// Gentle up-and-down drift (and an optional slow spin) for floating platforms like ice cubes.
public class Bobbing : MonoBehaviour
{
    [SerializeField] float amplitude = 0.25f, period = 3f, spin;

    Vector3 start;
    float phase;

    void Start()
    {
        start = transform.position;
        phase = (transform.position.x * 0.37f + transform.position.z * 0.21f) % (Mathf.PI * 2f);
    }

    void Update()
    {
        transform.position = start + Vector3.up * Mathf.Sin(Time.time * Mathf.PI * 2f / period + phase) * amplitude;
        if (spin != 0f) transform.Rotate(0, spin * Time.deltaTime, 0, Space.World);
    }

    public void Setup(float amp, float seconds, float spinSpeed)
    {
        amplitude = amp;
        period = seconds;
        spin = spinSpeed;
    }
}

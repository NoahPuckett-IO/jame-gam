using System.Collections;
using UnityEngine;

// The fallen cereal box blocking the gap. When the seesaw tips, the ruler's long end jacks it up:
// a fixed, eased animation (no physics) that leaves enough room to walk underneath.
public class LiftedGate : MonoBehaviour
{
    [SerializeField] float liftHeight = 3.8f;
    [SerializeField] float tiltBack = -9f;
    [SerializeField] float duration = 0.9f;

    bool lifted;

    public void Lift()
    {
        if (lifted) return;
        lifted = true;
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        var startPos = transform.position;
        var startRot = transform.rotation;
        var endPos = startPos + Vector3.up * liftHeight;
        var endRot = Quaternion.AngleAxis(tiltBack, Vector3.right) * startRot;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            // Ease out with a small overshoot, like it was flicked up and settled on the ruler.
            float k = t / duration;
            float e = 1f - Mathf.Pow(1f - k, 3f);
            float bounce = Mathf.Sin(k * Mathf.PI) * 0.08f;
            transform.SetPositionAndRotation(Vector3.LerpUnclamped(startPos, endPos, e + bounce),
                Quaternion.SlerpUnclamped(startRot, endRot, e));
            yield return null;
        }
        transform.SetPositionAndRotation(endPos, endRot);
    }

    public void Setup(float height) => liftHeight = height;
}

using UnityEngine;

// The fallen cereal box blocking the gap. It rests on the ruler's long end: as the ruler swings up,
// the box rides up on the tip (tilting back a little), leaving room to walk underneath.
public class LiftedGate : LeverLoad
{
    [SerializeField] float restBottom;  // world Y of the box's underside at rest
    [SerializeField] float tiltPerUnit = -1.6f;

    Vector3 startPos;
    Quaternion startRot;

    void Awake()
    {
        startPos = transform.position;
        startRot = transform.rotation;
    }

    public override void OnTip(Vector3 tipTop)
    {
        float lift = Mathf.Max(0f, tipTop.y - restBottom);
        transform.SetPositionAndRotation(startPos + Vector3.up * lift,
            Quaternion.AngleAxis(lift * tiltPerUnit, Vector3.right) * startRot);
    }

    public void Setup(float bottomAtRest) => restBottom = bottomAtRest;
}

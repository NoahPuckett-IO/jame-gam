using UnityEngine;

public class LiftedGate : LeverLoad
{
    [SerializeField] float restBottom;
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

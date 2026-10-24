using UnityEngine;

public class LiftedRamp : LeverLoad
{
    [SerializeField] float length = 14f;
    [SerializeField] float maxRise = 5.4f;
    [SerializeField] float baseYaw;

    public override void OnTip(Vector3 tipTop)
    {
        float rise = Mathf.Clamp(tipTop.y - transform.position.y, 0f, maxRise);
        float angle = Mathf.Asin(Mathf.Clamp01(rise / length)) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(-angle, baseYaw, 0);
    }

    public void Setup(float boardLength, float rise, float yaw)
    {
        length = boardLength;
        maxRise = rise;
        baseYaw = yaw;
    }
}

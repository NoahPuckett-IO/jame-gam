using UnityEngine;

// A plank balanced on a fulcrum. It rests with its far end down, wedged under a CanWall. When the
// player stands on the near half it tips the other way; the far end rises like a wedge and the
// can wall topples. After that it stays tipped.
public class Seesaw : MonoBehaviour
{
    [Tooltip("Rotates about its local X axis. The plank is a child; local +Z is the far (wedge) end.")]
    [SerializeField] Transform pivot;
    [SerializeField] float halfLength = 11f;
    [SerializeField] float halfWidth = 3f;
    [SerializeField] float tilt = 6f;
    [SerializeField] float tipSpeed = 60f;
    [SerializeField] CanWall wall;

    PlayerController player;
    float angle;
    bool tripped;

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        angle = tilt; // far end down
        Apply();
    }

    void Update()
    {
        bool weighted = tripped || PlayerOnNearHalf();
        float target = weighted ? -tilt : tilt;
        angle = Mathf.MoveTowards(angle, target, tipSpeed * Time.deltaTime);
        Apply();

        if (!tripped && angle <= -tilt * 0.6f)
        {
            tripped = true;
            if (wall != null) wall.Topple(pivot.forward);
        }
    }

    // Standing on the plank = a short ray down from the player hits it, on the near side of the pivot.
    bool PlayerOnNearHalf()
    {
        if (player == null) return false;
        var origin = player.transform.position;
        if (!Physics.Raycast(origin, Vector3.down, out var hit, 2.5f, ~0, QueryTriggerInteraction.Ignore)) return false;
        if (!hit.transform.IsChildOf(pivot)) return false;
        return pivot.InverseTransformPoint(hit.point).z < -0.3f;
    }

    void Apply()
    {
        var e = pivot.localEulerAngles;
        pivot.localRotation = Quaternion.Euler(angle, e.y, 0);
    }

    public void Setup(Transform pivotTransform, float half, float width, float tiltDegrees, CanWall canWall)
    {
        pivot = pivotTransform;
        halfLength = half;
        halfWidth = width;
        tilt = tiltDegrees;
        wall = canWall;
    }
}

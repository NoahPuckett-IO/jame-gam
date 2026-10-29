using UnityEngine;

public class Carryable : MonoBehaviour
{
    Collider[] colliders;

    public bool IsHeld { get; private set; }

    public static readonly System.Collections.Generic.List<Carryable> All = new System.Collections.Generic.List<Carryable>();

    void Awake() => colliders = GetComponentsInChildren<Collider>();
    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    float height = -1f;

    // measured once: the hands look this up every frame for every item to decide what's in reach
    public float Height
    {
        get
        {
            if (height >= 0f) return height;
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return height = 1f;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return height = b.size.y;
        }
    }

    public Vector3 CenterOffsetLocal()
    {
        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return Vector3.zero;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return transform.InverseTransformVector(b.center - transform.position);
    }

    public Vector3 LocalSize()
    {
        var mf = GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return Vector3.one * Height;
        var s = Vector3.Scale(mf.sharedMesh.bounds.size, mf.transform.lossyScale);
        var inv = Quaternion.Inverse(transform.rotation) * mf.transform.rotation;
        return Abs(inv * s);
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    public void PickUp(Transform holder, Vector3 localOffset)
    {
        IsHeld = true;
        foreach (var c in colliders) c.enabled = false;
        transform.SetParent(holder, true);
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;
    }

    public void PutDown(Vector3 point, Quaternion rotation)
    {
        transform.SetParent(null, true);
        transform.rotation = rotation;
        float lift = transform.position.y - Bottom();
        float ground = point.y - 1f;
        if (Physics.Raycast(point + Vector3.up * 1.5f, Vector3.down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore))
            ground = hit.point.y;
        transform.position = new Vector3(point.x, ground + lift, point.z);

        var back = -(rotation * Vector3.forward);
        back.y = 0;
        var size = Size();
        for (int i = 0; i < 20 && Blocked(size); i++)
            transform.position += back.normalized * 0.25f;

        foreach (var c in colliders) c.enabled = true;
        IsHeld = false;
    }

    Vector3 Size()
    {
        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return Vector3.one;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b.size;
    }

    bool Blocked(Vector3 size)
    {
        var center = transform.position + Vector3.up * (size.y / 2 + 0.05f);
        foreach (var hit in Physics.OverlapBox(center, size * 0.45f, transform.rotation, ~0, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(transform) && hit.GetComponent<CharacterController>() == null)
                return true;
        return false;
    }

    float Bottom()
    {
        var rs = GetComponentsInChildren<Renderer>();
        float min = transform.position.y;
        bool any = false;
        foreach (var r in rs)
        {
            min = any ? Mathf.Min(min, r.bounds.min.y) : r.bounds.min.y;
            any = true;
        }
        return min;
    }
}

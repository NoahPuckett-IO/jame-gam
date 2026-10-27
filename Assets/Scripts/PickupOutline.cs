using System.Collections.Generic;
using UnityEngine;

// draws a thin outline round a carryable while it's lying around to be picked up. Each mesh gets a twin with
// smoothed normals, so the pushed-out hull stays closed at hard corners instead of splitting open
[RequireComponent(typeof(Carryable))]
public class PickupOutline : MonoBehaviour
{
    [SerializeField] Material material;

    static readonly Dictionary<Mesh, Mesh> smoothed = new Dictionary<Mesh, Mesh>();
    readonly List<Renderer> lines = new List<Renderer>();
    Carryable item;

    void Start()
    {
        item = GetComponent<Carryable>();
        if (material == null) return;
        foreach (var mf in GetComponentsInChildren<MeshFilter>())
        {
            var source = mf.GetComponent<MeshRenderer>();
            if (source == null || mf.sharedMesh == null || !source.enabled) continue;
            var go = new GameObject("Outline");
            go.transform.SetParent(mf.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = Smooth(mf.sharedMesh);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            lines.Add(r);
        }
    }

    void LateUpdate()
    {
        bool show = item != null && !item.IsHeld;
        foreach (var r in lines)
            if (r != null && r.enabled != show) r.enabled = show;
    }

    static Mesh Smooth(Mesh mesh)
    {
        if (smoothed.TryGetValue(mesh, out var done)) return done;
        if (!mesh.isReadable) return smoothed[mesh] = mesh;
        var copy = Instantiate(mesh);
        copy.name = mesh.name + " Outline";
        var verts = copy.vertices;
        var normals = copy.normals;
        var sum = new Dictionary<Vector3Int, Vector3>();
        Vector3Int Key(Vector3 v) => new Vector3Int(Mathf.RoundToInt(v.x * 1000f), Mathf.RoundToInt(v.y * 1000f), Mathf.RoundToInt(v.z * 1000f));
        for (int i = 0; i < verts.Length; i++)
        {
            var k = Key(verts[i]);
            sum.TryGetValue(k, out var n);
            sum[k] = n + normals[i];
        }
        for (int i = 0; i < verts.Length; i++) normals[i] = sum[Key(verts[i])].normalized;
        copy.normals = normals;
        return smoothed[mesh] = copy;
    }

    public void Setup(Material outline) => material = outline;
}

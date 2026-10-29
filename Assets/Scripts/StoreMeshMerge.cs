using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

// The store holds ~30,000 separate renderers, most of them small products that never move. Every frame Unity
// culls, sorts and submits each one on its own for the camera, the SSAO prepass and the door light's shadow, and
// in a browser (one thread, no jobs) that per-object work is what makes busy views crawl. When the game starts,
// this merges those products into one mesh per material per patch of floor and hides the originals. Their
// colliders stay, and the merged meshes use the same materials at the same world positions, so nothing looks or
// plays differently.
//
// Left alone: anything a script references or might move (rigidbodies, animators, scripted objects and
// everything below them), transparent or cutout materials, meshes with LODs or many vertices, and mirrored objects.
public static class StoreMeshMerge
{
    const float Cell = 48f;
    const int MaxSourceVerts = 300;

    // what the merge hid and what it made, for the Editor checks
    public static readonly List<MeshRenderer> Hidden = new List<MeshRenderer>();
    public static GameObject Root;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Run()
    {
        var store = GameObject.Find("Store");
        if (store == null) return;
        var referenced = ReferencedByScripts();
        var batches = new Dictionary<(Material, int, ShadowCastingMode, bool, uint, Vector3Int), List<CombineInstance>>();
        var merged = new List<MeshRenderer>();

        foreach (Transform group in store.transform)
        {
            foreach (var r in group.GetComponentsInChildren<MeshRenderer>())
            {
                if (!Mergeable(r, group, referenced)) continue;
                var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                var p = r.bounds.center;
                var cell = new Vector3Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell), Mathf.FloorToInt(p.z / Cell));
                var mats = r.sharedMaterials;
                for (int s = 0; s < mesh.subMeshCount && s < mats.Length; s++)
                {
                    var key = (mats[s], r.gameObject.layer, r.shadowCastingMode, r.receiveShadows, r.renderingLayerMask, cell);
                    if (!batches.TryGetValue(key, out var list)) batches[key] = list = new List<CombineInstance>();
                    list.Add(new CombineInstance { mesh = mesh, subMeshIndex = s, transform = r.localToWorldMatrix });
                }
                merged.Add(r);
            }
        }
        if (merged.Count == 0) return;

        var root = new GameObject("Merged Products").transform;
        root.SetParent(store.transform, false);
        foreach (var kv in batches)
        {
            var (mat, layer, shadows, receive, renderingLayers, _) = kv.Key;
            // split so each merged mesh stays within 16-bit indices
            int start = 0;
            var list = kv.Value;
            while (start < list.Count)
            {
                int verts = 0, end = start;
                while (end < list.Count && (end == start || verts + list[end].mesh.vertexCount <= 65000)) verts += list[end++].mesh.vertexCount;
                var mesh = new Mesh { name = "Merged " + mat.name, indexFormat = IndexFormat.UInt16 };
                mesh.CombineMeshes(list.GetRange(start, end - start).ToArray(), true, true, false);
                mesh.UploadMeshData(true);
                var go = new GameObject(mesh.name) { layer = layer };
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = shadows;
                mr.receiveShadows = receive;
                mr.renderingLayerMask = renderingLayers;
                start = end;
            }
        }
        foreach (var r in merged) r.enabled = false;
        Hidden.Clear();
        Hidden.AddRange(merged);
        Root = root.gameObject;
        Debug.Log($"[StoreMeshMerge] merged {merged.Count} renderers into {root.childCount}");
    }

    static bool Mergeable(MeshRenderer r, Transform group, HashSet<Transform> referenced)
    {
        if (!r.enabled || !r.gameObject.activeInHierarchy || r.HasPropertyBlock()) return false;
        var mf = r.GetComponent<MeshFilter>();
        var mesh = mf != null ? mf.sharedMesh : null;
        if (mesh == null || !mesh.isReadable || mesh.vertexCount > MaxSourceVerts || mesh.lodCount > 1) return false;
        if (r.GetComponent<TextMesh>() != null) return false;
        if (r.transform.localToWorldMatrix.determinant < 0) return false;
        foreach (var m in r.sharedMaterials)
            if (m == null || m.renderQueue >= (int)RenderQueue.AlphaTest) return false;
        for (var t = r.transform; t != group.parent; t = t.parent)
        {
            if (referenced.Contains(t)) return false;
            if (t.GetComponent<Rigidbody>() != null || t.GetComponent<Animator>() != null) return false;
            foreach (var b in t.GetComponents<MonoBehaviour>())
                if (!(b is SignText)) return false;
        }
        return true;
    }

    // every object some script holds a reference to, so anything a script could move, hide or recolour stays its own renderer
    static HashSet<Transform> ReferencedByScripts()
    {
        var found = new HashSet<Transform>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            if (b == null || b is SignText) continue;
            for (var type = b.GetType(); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
                foreach (var f in type.GetFields(flags | BindingFlags.DeclaredOnly))
                {
                    object value;
                    try { value = f.GetValue(b); } catch { continue; }
                    Collect(value, found);
                }
        }
        return found;
    }

    static void Collect(object value, HashSet<Transform> found)
    {
        switch (value)
        {
            // before the list case: a Transform is also enumerable (its children), and an unassigned one throws
            case Object o:
                if (o is Component c && c != null) found.Add(c.transform);
                else if (o is GameObject g && g != null) found.Add(g.transform);
                break;
            case IEnumerable list when !(value is string):
                foreach (var item in list) if (item is Component || item is GameObject) Collect(item, found);
                break;
        }
    }
}

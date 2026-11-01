using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Lists solid props that sink into one another (a box half inside a shelf, a can through a crate), deepest first.
// Things touching or resting on each other only overlap by a hair, so anything past a few centimetres is a real clip.
static class ClippingCheck
{
    const float MinDepth = 0.2f;

    [MenuItem("JameGam/Debug/Find Clipping Props")]
    static void Run()
    {
        Physics.SyncTransforms();
        var store = GameObject.Find("Store");
        if (store == null) return;
        // invisible blockers and seals overlap things on purpose; only visible props count
        var all = store.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger && Visible(c)).ToArray();
        var seen = new HashSet<(Collider, Collider)>();
        var found = new List<(float depth, string a, string b, Vector3 at)>();
        foreach (var a in all)
        {
            // only small props: walls, floors and shelf frames are what everything else rests on
            var size = a.bounds.size;
            if (Mathf.Max(size.x, size.y, size.z) > 12f) continue;
            foreach (var b in Physics.OverlapBox(a.bounds.center, a.bounds.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (b == a || !b.enabled || !Visible(b) || b.transform.IsChildOf(a.transform) || a.transform.IsChildOf(b.transform)) continue;
                if (Owner(a) == Owner(b)) continue;
                if (seen.Contains((b, a))) continue;
                seen.Add((a, b));
                if (!Physics.ComputePenetration(a, a.transform.position, a.transform.rotation, b, b.transform.position, b.transform.rotation, out _, out float depth)) continue;
                if (depth < MinDepth) continue;
                found.Add((depth, Path(a.transform), Path(b.transform), a.bounds.center));
            }
        }
        var report = $"{found.Count} clipping pairs\n" + string.Join("\n", found.OrderByDescending(f => f.depth).Take(800)
            .Select(f => $"{f.depth:F2}  {f.a}  <>  {f.b}  @{f.at:F1}"));
        System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "..", "DebugOut", "clipping.txt"), report);
        Debug.Log("[ClippingCheck] " + found.Count + " clipping pairs");
    }

    // a prop and its own colliders count as one thing: the outermost model it belongs to (a product, a box, a cart),
    // or for built-up props (a cereal box, a sign) the object holding its parts. Group objects (Products, Aisle Mess,
    // a level) don't make their contents one thing, so two products or two pieces of clutter in one group still count
    static Transform Owner(Collider c)
    {
        var root = PrefabUtility.GetOutermostPrefabInstanceRoot(c.gameObject);
        if (root != null) return root.transform;
        var t = c.transform;
        if (t.parent != null && t.parent.childCount <= 12 && t.parent.parent != null && t.parent.parent.name != "Store") return t.parent;
        return t;
    }

    static bool Visible(Collider c)
    {
        var r = c.GetComponent<Renderer>() ?? c.GetComponentInChildren<Renderer>();
        return r != null && r.enabled;
    }

    static string Path(Transform t)
    {
        var parts = new List<string>();
        for (; t != null; t = t.parent) parts.Add(t.name);
        parts.Reverse();
        return string.Join("/", parts);
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

static class FloatingCheck
{
    static readonly string[] Hanging = { "Light", "Sign", "Wire", "String", "Cracker", "Plate", "Saucer", "Ceiling", "Slide", "Mop", "Snow", "Icicle",
        "Grime", "Stain", "Mold", "Text", "Lane", "Talker", "Strip", "Banner", "Fixture", "Ice Cube", "Thin Ice", "Ice Block", "Ice Wall", "Frost", "Door",
        "Henry", "Player", "VAC", "Vision", "Fill", "Decal", "Tutorial", "Rolling", "Puddle", "Spill", "Melt", "Wake-up", "Templates", "Woods", "Ship", "Intro",
        "Hinge", "Ruler", "Spatula", "Popsicle", "Gate", "Pivot", "Freezer", "Cooler", "Checkout", "Shelf", "Aisle Sign" };

    [MenuItem("JameGam/Debug/Find Floating Props %#&4")]
    static void Run()
    {
        Physics.SyncTransforms();
        var store = GameObject.Find("Store");
        if (store == null) return;
        var found = new List<(string path, float gap)>();
        foreach (var r in store.GetComponentsInChildren<MeshRenderer>())
        {
            var b = r.bounds;
            if (b.size.y > 9f || b.min.y < 0.25f) continue;
            string path = Path(r.transform);
            if (Hanging.Any(h => path.Contains(h))) continue;
            bool supported = false;
            float gap = 99f;
            foreach (var o in new[] { Vector3.zero, new Vector3(0.3f, 0, 0.3f), new Vector3(-0.3f, 0, -0.3f), new Vector3(0.3f, 0, -0.3f), new Vector3(-0.3f, 0, 0.3f) })
            {
                var p = new Vector3(b.center.x + o.x * b.size.x, b.min.y + 0.05f, b.center.z + o.z * b.size.z);
                foreach (var h in Physics.RaycastAll(p + Vector3.up * 0.3f, Vector3.down, 1.0f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.collider.transform.IsChildOf(r.transform)) continue;
                    gap = Mathf.Min(gap, h.distance - 0.35f);
                    supported = true;
                }
                foreach (var other in Physics.OverlapBox(new Vector3(p.x, b.min.y, p.z), new Vector3(0.05f, 0.12f, 0.05f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    if (!other.transform.IsChildOf(r.transform)) supported = true;
                if (supported) break;
            }
            if (!supported) found.Add((path + $" @{b.center:F1} gap {gap:F2}", gap));
        }
        var groups = found.GroupBy(f => f.path.Substring(0, Mathf.Max(0, f.path.Split(" @")[0].LastIndexOf('/'))))
            .OrderByDescending(g => g.Count()).Take(40);
        var report = $"{found.Count} unsupported props\n" + string.Join("\n", groups.Select(g => $"{g.Count(),4}  {g.Key}  e.g. {g.First().path.Substring(g.Key.Length)}"))
                     + "\n\nall:\n" + string.Join("\n", found.Select(f => f.path));
        System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TinyHenrySnapshots", "floating.txt"), report);
        Debug.Log("[FloatingCheck] " + report);
    }

    static string Path(Transform t)
    {
        var parts = new List<string>();
        for (; t != null; t = t.parent) parts.Add(t.name);
        parts.Reverse();
        return string.Join("/", parts);
    }
}

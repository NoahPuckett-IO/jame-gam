using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

static partial class GroceryStoreBuilder
{
    const string PlatformerFolder = "Assets/ThirdParty/KenneyPlatformer";

    // a real wooden ladder (Kenney platformer kit) stood against a shelf face, its rails stretched to reach from
    // bottomY to a little above the ledge at topY. bottomY above the floor makes a ladder that hangs from the
    // board, out of reach until Henry stands on something. Falls back to the toothpick ladder if the model's missing.
    static Transform KitLadder(Transform parent, Vector3 faceAt, Vector3 outward, float bottomY, float topY, Vector3 landing, bool down = false)
    {
        string path = $"{PlatformerFolder}/ladder-long.fbx";
        var prefab = Usable(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
        if (prefab == null)
        {
            ToothpickLadder(parent, faceAt, outward, topY, landing);
            return null;
        }
        var root = new GameObject("Ladder").transform;
        root.SetParent(parent);
        outward = new Vector3(outward.x, 0, outward.z).normalized;
        var plane = new Vector3(faceAt.x, bottomY, faceAt.z) + outward * 0.18f;

        // rung heights, as fractions of the model's height, from the vertices near the middle (only rungs are there)
        var mf = prefab.GetComponentInChildren<MeshFilter>();
        var mesh = mf.sharedMesh;
        var mb = mesh.bounds;
        var ys = mesh.isReadable
            ? mesh.vertices.Where(v => Mathf.Abs(v.x - mb.center.x) < mb.size.x * 0.15f).Select(v => (v.y - mb.min.y) / mb.size.y).OrderBy(y => y).ToList()
            : new List<float>();
        var rungs = new List<float>();
        foreach (var y in ys)
            if (rungs.Count == 0 || y - rungs[rungs.Count - 1] > 0.04f) rungs.Add(y); else rungs[rungs.Count - 1] = (rungs[rungs.Count - 1] + y) / 2;
        float firstFrac = rungs.Count > 1 ? rungs[0] : 0.1f;
        float stepFrac = rungs.Count > 1 ? (rungs[rungs.Count - 1] - rungs[0]) / (rungs.Count - 1) : 0.1f;

        var probe = Spawn(prefab, null, 0, 1);
        var pb = GetBounds(probe);
        Object.DestroyImmediate(probe);
        float wide = 1.2f, s = wide / Mathf.Max(pb.size.x, 0.01f);
        float total = topY + 1.3f - bottomY;
        int n = Mathf.Max(1, Mathf.RoundToInt(total / (pb.size.y * s)));
        float segH = total / n;
        for (int i = 0; i < n; i++)
        {
            var seg = Spawn(prefab, root, 0, 1);
            seg.name = "Ladder Section";
            seg.transform.rotation = Quaternion.LookRotation(outward) * prefab.transform.rotation;
            var ls = seg.transform.localScale;
            seg.transform.localScale = new Vector3(ls.x * s, ls.y * segH / pb.size.y, ls.z * s);
            var b = GetBounds(seg);
            seg.transform.position += plane + Vector3.up * (i * segH) - new Vector3(b.center.x, b.min.y, b.center.z);
        }
        if (bottomY > 0.5f)
        {
            // hooks over the edge it hangs from, reaching back over the ledge it leads to
            var iron = GetMaterial("Ladder Hook", new Color(0.18f, 0.18f, 0.2f));
            var across = Vector3.Cross(Vector3.up, outward);
            var toLedge = new Vector3(landing.x - plane.x, 0, landing.z - plane.z).normalized;
            foreach (int side in new[] { -1, 1 })
            {
                var hook = Box2("Hook", root, new Vector3(plane.x, topY + 0.15f, plane.z) + across * side * 0.5f + toLedge * 0.35f,
                    new Vector3(0.16f, 0.3f, 0.16f), iron);
                hook.transform.rotation = Quaternion.LookRotation(toLedge);
                hook.transform.localScale = new Vector3(0.16f, 0.12f, 0.9f);
                Object.DestroyImmediate(hook.GetComponent<Collider>());
            }
        }
        root.gameObject.AddComponent<Ladder>().Setup(plane, outward, topY, segH * stepFrac, segH * firstFrac, 0.55f, landing, down);
        return root;
    }

    static GameObject Box2(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        Box(name, parent, pos, size, mat);
        return parent.GetChild(parent.childCount - 1).gameObject;
    }

    static void Seal(Transform parent, string name, Vector3 center, Vector3 size)
    {
        Solid(parent, name, center, size);
        parent.GetChild(parent.childCount - 1).gameObject.layer = 2;
    }

    static void ShelfGuards(Transform parent, float xc, float z0, float z1)
    {
        float zc = (z0 + z1) / 2, len = Mathf.Abs(z1 - z0);
        foreach (int side in new[] { -1, 1 })
        {
            float face = xc + side * WalkwayWidth / 2;
            Seal(parent, side < 0 ? "Shelf Guard West" : "Shelf Guard East",
                new Vector3(face + side * 0.1f, WallHeight / 2, zc), new Vector3(0.8f, WallHeight, len));
        }
    }

    // invisible walls in every shelf cubby Henry isn't meant to walk into, flush with the front edge of the boards
    // so there's no lip to stand on; built after SettleProps so stocked products don't get shoved out. The freezer
    // hall is skipped (solid freezers); the two climbing halls (levels one and two) leave open just the boards
    // their routes use, from the NoStockZones.
    static void SealShelves(Transform store, List<float> walkways, ShelfInfo shelf, float aisleLength)
    {
        var root = new GameObject("Shelf Seals").transform;
        root.SetParent(store);
        float half = WalkwayWidth / 2, depth = shelf.SideDepth * 0.95f, top = ShelfHeight - 0.4f;
        float zS = -aisleLength / 2, zN = aisleLength / 2;

        void Face(float face, int dir, float y0, float y1, float z0, float z1)
        {
            if (y1 - y0 < 0.2f || z1 - z0 < 0.5f) return;
            float x0 = face - dir * 0.05f, x1 = face + dir * depth;
            Seal(root, "Shelf Seal", new Vector3((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2),
                new Vector3(Mathf.Abs(x1 - x0), y1 - y0, z1 - z0));
        }

        for (int w = 0; w < walkways.Count; w++)
        {
            if (w == 2) continue;
            var faces = new List<(float face, int dir)> { (walkways[w] + half, 1) };
            if (w < walkways.Count - 1) faces.Add((walkways[w] - half, -1));
            bool route = w == 0 || w == 1;
            var hall = w == 0 ? levelOneHall : levelTwoHall;
            foreach (var (face, dir) in faces)
            {
                if (!route)
                {
                    Face(face, dir, 0f, top, zS, zN);
                    continue;
                }
                Face(face, dir, 0f, hall.h1 - 0.15f, zS, zN);
                var bands = new[] { (hall.h1, hall.h2), (hall.h2, hall.h3), (hall.h3, top) };
                foreach (var (h, next) in bands)
                {
                    var open = NoStockZones
                        .Where(b => Mathf.Abs(b.min.y - (h - 0.5f)) < 0.1f && Mathf.Abs(b.center.x - (face + dir * hall.boardDepth / 2)) < hall.boardDepth)
                        .Select(b => (b.min.z - 4f, b.max.z + 4f)).OrderBy(r => r.Item1).ToList();
                    float from = zS;
                    foreach (var (a, b) in open)
                    {
                        Face(face, dir, h + 0.05f, next - 0.15f, from, Mathf.Min(a, zN));
                        from = Mathf.Max(from, b);
                    }
                    Face(face, dir, h + 0.05f, next - 0.15f, from, zN);
                }
            }
        }
    }

    static readonly List<GameObject> settleQueue = new List<GameObject>();
    static readonly HashSet<GameObject> settleForced = new HashSet<GameObject>();

    static void SettleLater(GameObject go, bool force = false)
    {
        if (go == null) return;
        settleQueue.Add(go);
        if (force) settleForced.Add(go);
    }

    static void SettleProps()
    {
        var items = settleQueue.Where(g => g != null).Distinct().ToList();
        settleQueue.Clear();
        if (items.Count == 0) return;

        var tempColliders = new List<Collider>();
        var madeConvex = new List<MeshCollider>();
        var bodies = new List<Rigidbody>();
        var start = new Dictionary<GameObject, (Vector3 pos, Quaternion rot)>();
        var rng = new System.Random(5150);
        Physics.SyncTransforms();

        foreach (var go in items)
        {
            var b = GetBounds(go);
            bool stuck = !settleForced.Contains(go) && Physics.OverlapBox(b.center + Vector3.up * 0.3f, b.extents * 0.8f,
                    Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                .Any(c => !c.transform.IsChildOf(go.transform));
            if (stuck) continue;

            start[go] = (go.transform.position, go.transform.rotation);
            if (go.GetComponentsInChildren<Collider>().Length == 0)
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null) continue;
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = true;
                    tempColliders.Add(mc);
                }
            foreach (var mc in go.GetComponentsInChildren<MeshCollider>())
                if (!mc.convex) { mc.convex = true; madeConvex.Add(mc); }

            go.transform.position += Vector3.up * 0.3f;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0.3f;
            rb.angularDamping = 1.2f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.angularVelocity = new Vector3((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f) * 3f;
            bodies.Add(rb);
        }

        var seals = Object.FindObjectsByType<Collider>()
            .Where(c => c.enabled && !c.isTrigger && c.gameObject.layer == 2).ToList();
        foreach (var c in seals) c.enabled = false;
        Physics.SyncTransforms();
        var mode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        for (int i = 0; i < 250; i++) Physics.Simulate(0.02f);
        Physics.simulationMode = mode;
        foreach (var c in seals) c.enabled = true;

        foreach (var rb in bodies) Object.DestroyImmediate(rb);
        foreach (var c in tempColliders) Object.DestroyImmediate(c);
        foreach (var mc in madeConvex) if (mc != null) mc.convex = false;
        int kept = 0;
        foreach (var kv in start)
        {
            var go = kv.Key;
            var moved = go.transform.position - kv.Value.pos;
            bool lost = go.transform.position.y < -0.5f;
            if (lost || (!settleForced.Contains(go) && (new Vector2(moved.x, moved.z).magnitude > 6f || moved.y > 1f)))
                go.transform.SetPositionAndRotation(kv.Value.pos, kv.Value.rot);
            else kept++;
        }
        settleForced.Clear();
        Debug.Log($"[GroceryStoreBuilder] Settled {kept} of {items.Count} scattered items with physics.");
    }

    static Rigidbody Kinematic(GameObject go)
    {
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        return rb;
    }

    static Transform PaperPlate(Transform parent, Vector3 top, Vector3 move, float travel, float phase, float diameter = 6f)
    {
        var root = new GameObject("Paper Plate Platform").transform;
        root.SetParent(parent);
        root.position = top;
        var paper = GetMaterial("Paper Plate", new Color(0.96f, 0.95f, 0.9f));
        var rim = GetMaterial("Paper Plate Rim", new Color(0.85f, 0.83f, 0.78f));
        var plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        plate.name = "Plate";
        Object.DestroyImmediate(plate.GetComponent<Collider>());
        plate.transform.SetParent(root, false);
        plate.transform.localPosition = Vector3.down * 0.15f;
        plate.transform.localScale = new Vector3(diameter, 0.15f, diameter);
        plate.GetComponent<Renderer>().sharedMaterial = paper;
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Rim";
        Object.DestroyImmediate(ring.GetComponent<Collider>());
        ring.transform.SetParent(root, false);
        ring.transform.localPosition = Vector3.down * 0.05f;
        ring.transform.localScale = new Vector3(diameter + 0.5f, 0.12f, diameter + 0.5f);
        ring.GetComponent<Renderer>().sharedMaterial = rim;
        var col = root.gameObject.AddComponent<BoxCollider>();
        col.center = Vector3.down * 0.15f;
        col.size = new Vector3(diameter * 0.85f, 0.3f, diameter * 0.85f);
        var wire = GetMaterial("Wire", new Color(0.2f, 0.2f, 0.2f));
        float h = WallHeight - top.y;
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI / 2 + Mathf.PI / 4;
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = "String";
            Object.DestroyImmediate(w.GetComponent<Collider>());
            w.transform.SetParent(root, false);
            w.transform.localPosition = new Vector3(Mathf.Cos(a) * diameter * 0.42f, h / 2, Mathf.Sin(a) * diameter * 0.42f);
            w.transform.localScale = new Vector3(0.06f, h, 0.06f);
            w.GetComponent<Renderer>().sharedMaterial = wire;
        }
        foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
        Kinematic(root.gameObject);
        root.gameObject.AddComponent<MovingPlatform>().Setup(move, travel, 0.9f, phase);
        return root;
    }

    static Transform CrumbleCracker(Transform parent, Vector3 top, float size = 4.4f)
    {
        var root = new GameObject("Crumbling Cracker").transform;
        root.SetParent(parent);
        root.position = top;
        var cracker = GetMaterial("Cracker", new Color(0.86f, 0.66f, 0.35f));
        var dots = GetMaterial("Cracker Dots", new Color(0.55f, 0.38f, 0.18f));
        Box("Cracker", root, top + Vector3.down * 0.25f, new Vector3(size, 0.5f, size), cracker);
        for (int i = 0; i < 9; i++)
            Box("Dock Hole", root, top + new Vector3((i % 3 - 1) * size * 0.28f, 0.01f, (i / 3 - 1) * size * 0.28f), new Vector3(0.22f, 0.04f, 0.22f), dots);
        foreach (var c in root.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        var col = root.gameObject.AddComponent<BoxCollider>();
        col.center = Vector3.down * 0.25f;
        col.size = new Vector3(size, 0.5f, size);
        var trig = root.gameObject.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.center = Vector3.up * 0.5f;
        trig.size = new Vector3(size * 0.9f, 1f, size * 0.9f);
        foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
        Kinematic(root.gameObject);
        root.gameObject.AddComponent<CrumblePlatform>();
        var wire = GetMaterial("Wire", new Color(0.2f, 0.2f, 0.2f));
        float h = WallHeight - top.y;
        foreach (int s in new[] { -1, 1 })
            Object.DestroyImmediate(Box2("Cracker String", parent, top + new Vector3(s * size * 0.4f, h / 2, s * size * 0.4f), new Vector3(0.06f, h, 0.06f), wire).GetComponent<Collider>());
        return root;
    }

    static Transform RollingCan(Transform parent, Vector3 floorAt, Vector3 move, float travel, float phase, float diameter = 2.6f)
    {
        var root = new GameObject("Rolling Can").transform;
        root.SetParent(parent);
        root.position = floorAt + Vector3.up * diameter / 2;
        var visual = new GameObject("Can").transform;
        visual.SetParent(root, false);
        var can = BeanCanBuilder.GetOrCreate();
        var c = Spawn(can, visual, 0, 1);
        var b = GetBounds(c);
        c.transform.localScale *= diameter / Mathf.Max(b.size.x, b.size.z);
        var across = Vector3.Cross(Vector3.up, move.normalized);
        c.transform.rotation = Quaternion.FromToRotation(Vector3.up, across) * c.transform.rotation;
        b = GetBounds(c);
        c.transform.position += root.position - b.center;
        foreach (var col in c.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        var trig = root.gameObject.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.size = Quaternion.Inverse(root.rotation) * (b.size + Vector3.one * 0.3f);
        trig.size = new Vector3(Mathf.Abs(trig.size.x), Mathf.Abs(trig.size.y), Mathf.Abs(trig.size.z));
        foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
        Kinematic(root.gameObject);
        root.gameObject.AddComponent<RollingHazard>().Setup(move, travel, 0.5f, phase, diameter / 2, visual);
        return root;
    }
}

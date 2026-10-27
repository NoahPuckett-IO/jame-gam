using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

static partial class GroceryStoreBuilder
{
    const string SurvivalFolder = "Assets/ThirdParty/KenneySurvival";

    // the cardboard boxes, all in one colour: Sealed (taped shut), Ajar (flaps up a little), Open (flaps folded out)
    enum BoxKind { Sealed, SealedLong, Ajar, Open, OpenWide }

    static GameObject BoxPrefab(BoxKind kind)
    {
        string path = kind switch
        {
            BoxKind.Sealed => $"{SurvivalFolder}/box.fbx",
            BoxKind.SealedLong => $"{SurvivalFolder}/box-large.fbx",
            BoxKind.Open => $"{SurvivalFolder}/box-open.fbx",
            BoxKind.Ajar => $"{KenneyFolder}/cardboardBoxClosed.fbx",
            _ => $"{KenneyFolder}/cardboardBoxOpen.fbx",
        };
        var prefab = Usable(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
        if (prefab == null && kind != BoxKind.Ajar && kind != BoxKind.OpenWide)
            return BoxPrefab(kind == BoxKind.Open ? BoxKind.OpenWide : BoxKind.Ajar);
        return prefab;
    }

    // the furniture-kit boxes are flat colours; tint them to match the taped survival-kit boxes
    static void Recolor(GameObject box, BoxKind kind)
    {
        if (kind != BoxKind.Ajar && kind != BoxKind.OpenWide) return;
        var light = GetMaterial("Cardboard Box", new Color(0.93f, 0.6f, 0.43f));
        var dark = GetMaterial("Cardboard Box Inside", new Color(0.62f, 0.38f, 0.26f));
        foreach (var r in box.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = mats[i] != null && mats[i].name.Contains("Dark") ? dark : light;
            r.sharedMaterials = mats;
        }
    }

    // a box whose largest side is `size`, with a box collider, sitting with its bottom at `floorAt`
    static GameObject CardboardBox(Transform parent, BoxKind kind, float size, float yaw, Vector3 floorAt, bool collider = true)
    {
        var prefab = BoxPrefab(kind);
        if (prefab == null) return null;
        var go = SpawnSized(prefab, parent, size, yaw);
        go.name = kind.ToString() + " Box";
        Recolor(go, kind);
        PlaceOnFloor(go, floorAt);
        if (collider) AddBoxColliders(go);
        return go;
    }

    // a barricade of boxes: a tight bottom layer of sealed boxes (no gaps to see through), a second sealed layer
    // over most of it, then a few ajar or open boxes on top, all dropped with physics so nothing floats
    static void BoxPile(Transform parent, float x0, float x1, float z0, float z1, System.Random rng, List<ProductType> goods)
    {
        if (BoxPrefab(BoxKind.Sealed) == null) return;
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        float box = 0.45f * Scale;
        int nx = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / box)), nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / box));
        float cx = (x1 - x0) / nx, cz = (z1 - z0) / nz, cell = Mathf.Min(cx, cz);
        var heights = new float[nx, nz];
        Vector3 Cell(int i, int j) => new Vector3(x0 + (i + 0.5f) * cx, 0, z0 + (j + 0.5f) * cz);

        for (int i = 0; i < nx; i++)
        for (int j = 0; j < nz; j++)
        {
            var b = CardboardBox(parent, BoxKind.Sealed, cell * 0.99f, R(-2f, 2f), Cell(i, j));
            heights[i, j] = GetBounds(b).max.y;
        }

        for (int i = 0; i < nx; i++)
        for (int j = 0; j < nz; j++)
        {
            if (rng.NextDouble() < 0.3) continue;
            var b = CardboardBox(parent, BoxKind.Sealed, cell * R(0.8f, 0.95f), R(-12f, 12f),
                Cell(i, j) + new Vector3(R(-0.06f, 0.06f) * cell, heights[i, j] + 0.05f, R(-0.06f, 0.06f) * cell));
            SettleLater(b, force: true);
            heights[i, j] = GetBounds(b).max.y;
        }

        var filled = new List<(int i, int j)>();
        int loose = Mathf.RoundToInt(nx * nz * R(0.35f, 0.55f));
        for (int k = 0; k < loose; k++)
        {
            int i = rng.Next(nx), j = rng.Next(nz);
            if (goods != null && goods.Count > 0 && rng.NextDouble() < 0.35)
            {
                if (!filled.Contains((i, j))) filled.Add((i, j));
                continue;
            }
            var kind = rng.NextDouble() < 0.55 ? BoxKind.Ajar : BoxKind.Open;
            var b = CardboardBox(parent, kind, cell * R(0.6f, 0.8f), R(0f, 360f),
                Cell(i, j) + new Vector3(R(-0.15f, 0.15f) * cell, heights[i, j] + 0.3f, R(-0.15f, 0.15f) * cell));
            b.transform.rotation = Quaternion.Euler(R(-8f, 8f), 0, R(-8f, 8f)) * b.transform.rotation;
            SettleLater(b, force: true);
            heights[i, j] = GetBounds(b).max.y;
        }

        // boxes with groceries in them go on last and nearly upright, so nothing lands in them
        foreach (var (i, j) in filled)
        {
            var b = FilledBox(parent, BoxPrefab(BoxKind.OpenWide), cell * R(0.7f, 0.85f), goods, rng);
            b.transform.rotation = Quaternion.Euler(R(-5f, 5f), R(0f, 360f), R(-5f, 5f));
            PlaceOnFloor(b, Cell(i, j) + new Vector3(0, heights[i, j] + 0.3f, 0));
            SettleLater(b, force: true);
            heights[i, j] = GetBounds(b).max.y;
        }
    }

    // an open box with a few groceries standing inside it, kept as one piece for the physics drop
    static GameObject FilledBox(Transform parent, GameObject open, float size, List<ProductType> goods, System.Random rng)
    {
        var root = new GameObject("Open Box");
        root.transform.SetParent(parent);
        var b = SpawnSized(open, root.transform, size, 0f);
        Recolor(b, BoxKind.OpenWide);
        AddBoxColliders(b);
        FillOpenBox(root.transform, b, open, goods, rng, rng.Next(2, 5), raiseToRim: false);
        return root;
    }

    struct Cavity
    {
        public Vector3 min, max;   // inside of the box, in the box instance's local space
        public float rim;          // local height where the walls stop and the flaps start
    }

    static readonly Dictionary<GameObject, Cavity> cavities = new Dictionary<GameObject, Cavity>();

    // probes an open box with rays to find the floor, the inner walls and the top of the walls, so goods
    // can sit inside it instead of in the flaps
    static Cavity MeasureCavity(GameObject prefab)
    {
        if (cavities.TryGetValue(prefab, out var known)) return known;
        var go = Spawn(prefab, null, 0, 1);
        go.transform.position = new Vector3(0, -5000, 0);
        var cols = new List<Collider>();
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; cols.Add(mc); }
        Physics.SyncTransforms();
        var b = GetBounds(go);
        bool backfaces = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;

        bool Hit(Vector3 from, Vector3 dir, float dist, out float d)
        {
            d = float.MaxValue;
            foreach (var h in Physics.RaycastAll(from, dir, dist, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                if (cols.Contains(h.collider) && h.distance < d) d = h.distance;
            return d < float.MaxValue;
        }

        var c = b.center;
        var cavity = new Cavity();
        float floor = b.min.y + b.size.y * 0.08f;
        if (Hit(new Vector3(c.x, b.max.y + 1f, c.z), Vector3.down, b.size.y + 2f, out float down)) floor = b.max.y + 1f - down;
        float probeY = floor + b.size.y * 0.1f;
        var dirs = new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        var walls = new float[4];
        for (int i = 0; i < 4; i++)
            walls[i] = Hit(new Vector3(c.x, probeY, c.z), dirs[i], b.size.x, out float w) ? w : Mathf.Abs(Vector3.Dot(b.extents, dirs[i])) * 0.7f;

        float rim = b.max.y;
        for (float y = probeY; y < b.max.y; y += b.size.y * 0.02f)
        {
            bool open = false;
            for (int i = 0; i < 4 && !open; i++)
                open = !Hit(new Vector3(c.x, y, c.z), dirs[i], b.size.x, out float w) || w > walls[i] * 1.12f;
            if (open) { rim = y; break; }
        }
        Physics.queriesHitBackfaces = backfaces;

        var lo = new Vector3(c.x - walls[1], floor, c.z - walls[3]);
        var hi = new Vector3(c.x + walls[0], rim, c.z + walls[2]);
        cavity.min = go.transform.InverseTransformPoint(lo);
        cavity.max = go.transform.InverseTransformPoint(hi);
        cavity.rim = cavity.max.y;
        Object.DestroyImmediate(go);
        cavities[prefab] = cavity;
        return cavity;
    }

    // one layer of goods standing on the box floor (or on a cardboard insert near the rim), each in its own cell
    // so nothing overlaps, and nothing taller than the walls by much
    static void FillOpenBox(Transform parent, GameObject box, GameObject openPrefab, List<ProductType> goods, System.Random rng, int maxItems, bool raiseToRim)
    {
        if (goods == null || goods.Count == 0) return;
        var cav = MeasureCavity(openPrefab);
        var tr = box.transform;
        var lo = tr.TransformPoint(cav.min);
        var hi = tr.TransformPoint(cav.max);
        var right = tr.right;
        var fwd = tr.forward;
        var center = (lo + hi) / 2;
        float innerX = Mathf.Abs(Vector3.Dot(hi - lo, right)) * 0.86f;
        float innerZ = Mathf.Abs(Vector3.Dot(hi - lo, fwd)) * 0.86f;
        float floorY = Mathf.Min(lo.y, hi.y), rimY = Mathf.Max(lo.y, hi.y);
        float depth = rimY - floorY;
        if (innerX <= 0.01f || innerZ <= 0.01f || depth <= 0.01f) return;

        var t = goods[rng.Next(goods.Count)];
        float foot = Mathf.Max(t.bounds.x, t.bounds.z);
        int perSide = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(maxItems)), 1, 3);
        float cell = Mathf.Min(innerX, innerZ) / perSide;
        float s = Mathf.Min(t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z), cell * 0.82f / foot);
        float itemH = t.bounds.y * s;
        if (!raiseToRim) s = Mathf.Min(s, depth * 1.15f / t.bounds.y);
        itemH = t.bounds.y * s;
        int nx = Mathf.Max(1, Mathf.FloorToInt(innerX / (foot * s * 1.12f)));
        int nz = Mathf.Max(1, Mathf.FloorToInt(innerZ / (foot * s * 1.12f)));
        float baseY = floorY;
        if (raiseToRim && itemH < depth)
        {
            // a cardboard insert holds a single layer up near the top, the way display bins are packed
            baseY = rimY - itemH * 0.85f;
            var insert = GameObject.CreatePrimitive(PrimitiveType.Cube);
            insert.name = "Box Insert";
            insert.transform.SetParent(parent);
            insert.transform.SetPositionAndRotation(new Vector3(center.x, baseY - 0.06f, center.z), tr.rotation);
            insert.transform.localScale = new Vector3(innerX / 0.86f * 0.98f, 0.1f, innerZ / 0.86f * 0.98f);
            insert.GetComponent<Renderer>().sharedMaterial = GetMaterial("Cardboard Insert", new Color(0.62f, 0.45f, 0.28f));
            Object.DestroyImmediate(insert.GetComponent<Collider>());
        }

        int placed = 0;
        float stepX = innerX / nx, stepZ = innerZ / nz;
        for (int i = 0; i < nx; i++)
        for (int j = 0; j < nz; j++)
        {
            if (!raiseToRim && placed >= maxItems) return;
            var at = center + right * ((i + 0.5f) * stepX - innerX / 2) + fwd * ((j + 0.5f) * stepZ - innerZ / 2);
            var p = Spawn(t.prefab, parent, tr.eulerAngles.y + t.yaw + (rng.Next(2) * 90) + rng.Next(-8, 8), s);
            PlaceOnFloor(p, new Vector3(at.x, baseY + 0.02f, at.z));
            placed++;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

static partial class GroceryStoreBuilder
{
    static readonly List<Bounds> NoStockZones = new List<Bounds>();

    static bool InNoStockZone(Vector3 p) => NoStockZones.Any(b => b.Contains(p));

    static List<float> BoardHeights(Transform aisle, float x, float z)
    {
        var up = UpwardTriangles(aisle.gameObject);
        return SurfacesAt(up, x, z).Where(h => h > 4f && h < ShelfHeight * 0.95f).ToList();
    }

    struct Hall
    {
        public float x, xW, xE;
        public float h1, h2, h3;
        public float boardDepth;
    }

    static Hall levelTwoHall;

    static void PlanLevelTwo(List<Transform> aisles, List<float> walkways, ShelfInfo shelf)
    {
        NoStockZones.Clear();
        var east = aisles[0];
        float x = walkways[1];
        var hall = new Hall { x = x, xW = x - WalkwayWidth / 2, xE = x + WalkwayWidth / 2, boardDepth = 7f };
        var hs = BoardHeights(east, hall.xE + 3f, east.position.z);
        hall.h1 = hs.Count > 0 ? hs[0] : 8.6f;
        hall.h2 = hs.Count > 1 ? hs[1] : 17.3f;
        hall.h3 = hs.Count > 2 ? hs[2] : 25.9f;
        levelTwoHall = hall;

        void Zone(bool westSide, float h, float zA, float zB)
        {
            float x0 = westSide ? hall.xW - hall.boardDepth - 1f : hall.xE - 1f;
            float x1 = westSide ? hall.xW + 1f : hall.xE + hall.boardDepth + 1f;
            var b = new Bounds();
            b.SetMinMax(new Vector3(x0, h - 0.5f, Mathf.Min(zA, zB)), new Vector3(x1, h + 7f, Mathf.Max(zA, zB)));
            NoStockZones.Add(b);
        }
        Zone(true, hall.h1, 66f, 100f);
        Zone(true, hall.h2, 22f, 56f);
        Zone(false, hall.h2, -16f, 30f);
        Zone(false, hall.h3, -46f, -20f);
        Zone(true, hall.h3, -66f, -52f);
        Zone(false, hall.h1, -108f, -92f);
    }

    static void BuildLevelTwo(Transform store, float aisleLength)
    {
        var hall = levelTwoHall;
        var root = new GameObject("Level Two - Shelf Climb").transform;
        root.SetParent(store);
        float zN = aisleLength / 2;
        var rng = new System.Random(202);


        BoxWall(root, hall.x, WalkwayWidth, zN - 15f, 2, 0);
        var spill = new GameObject("Sticky Soda Spill").AddComponent<BoxCollider>();
        spill.transform.SetParent(root);
        spill.isTrigger = true;
        spill.transform.position = new Vector3(hall.x, 0.4f, (zN - 17f + -zN + 6f) / 2);
        spill.size = new Vector3(WalkwayWidth + 1f, 0.8f, (zN - 17f) - (-zN + 6f));
        spill.gameObject.AddComponent<Hazard>();
        SodaSpill(root, hall.x, WalkwayWidth, zN - 17f, -zN + 6f, rng);

        CanStairs(root, new Vector3(hall.xW + 1.4f, 0, zN - 0.5f), Vector3.back, 5, hall.h1);
        CheckpointAt(root, "Checkpoint L2-A", new Vector3(hall.xW - 3.5f, hall.h1, 95f), new Vector3(6, 4, 4), 180);
        RollingCan(root, new Vector3(hall.xW - 5.6f, hall.h1, 86f), new Vector3(4.4f, 0, 0), 1.3f, 0f);
        RollingCan(root, new Vector3(hall.xW - 5.6f, hall.h1, 77f), new Vector3(4.4f, 0, 0), 1.1f, 0.45f);

        Ramp(root, new Vector3(hall.xW + 2.2f, hall.h1, 70f), new Vector3(hall.xW + 2.2f, hall.h2, 52f), 5.4f, true);
        CheckpointAt(root, "Checkpoint L2-B", new Vector3(hall.xW - 3.5f, hall.h2, 50f), new Vector3(6, 4, 4), 180);
        RollingCan(root, new Vector3(hall.xW - 5.6f, hall.h2, 47.5f), new Vector3(4.4f, 0, 0), 1.0f, 0.2f);
        CarryBox(root, new Vector3(hall.xW - 3.5f, hall.h2, 42.5f));
        JarWall(root, hall.xW - hall.boardDepth - 0.5f, hall.xW + 0.6f, 36f, hall.h2, 3f);
        devWarps.Add(("Jar Wall", new Vector3(hall.xW - 3.5f, hall.h2 + 1f, 50f), 180));

        Mop(root, new Vector3(hall.xW - 1f, hall.h2, 26f), new Vector3(hall.xE + 1f, hall.h2, 26f));

        Pillar(root, new Vector3(hall.xE - 3f, 0, 19.5f), hall.h2, lyingAlongZ: true);
        CheckpointAt(root, "Checkpoint L2-C", new Vector3(hall.xE + 3.5f, hall.h2, 12f), new Vector3(6, 4, 4), 180);

        JarBlock(root, new Vector3(hall.xE, hall.h2, -6.5f), new Vector3(hall.xE + hall.boardDepth + 1f, hall.h3, 1f));
        CrumbleCracker(root, new Vector3(hall.xE - 4.6f, hall.h2, 3.6f));
        CrumbleCracker(root, new Vector3(hall.xE - 5.4f, hall.h2 + 1.3f, -2.2f));
        CrumbleCracker(root, new Vector3(hall.xE - 4.4f, hall.h2 + 0.4f, -8.2f));
        devWarps.Add(("Cracker Detour", new Vector3(hall.xE + 3.5f, hall.h2 + 1f, 9f), 180));

        Pillar(root, new Vector3(hall.xE - 4f, 0, -13f), hall.h2, lyingAlongZ: true);
        Sponge(root, new Vector3(hall.xE - 4f, hall.h2, -13f), 23f);
        Pillar(root, new Vector3(hall.xE - 4f, 0, -21.5f), hall.h3, lyingAlongZ: true);
        CheckpointAt(root, "Checkpoint L2-D", new Vector3(hall.xE + 3.5f, hall.h3, -26f), new Vector3(6, 4, 4), 180);
        RollingCan(root, new Vector3(hall.xE + 5.6f, hall.h3, -35f), new Vector3(-4.4f, 0, 0), 1.0f, 0.3f);

        Pillar(root, new Vector3(hall.x + 10f, 0, -44f), hall.h3, lyingAlongZ: false);
        PaperPlate(root, new Vector3(hall.x + 3.5f, hall.h3, -50f), new Vector3(-9f, 0, 0), 2.4f, 0f);
        devWarps.Add(("Paper Plate", new Vector3(hall.xE + 3.5f, hall.h3 + 1f, -38f), 180));
        Pillar(root, new Vector3(hall.x - 10f, 0, -56f), hall.h3, lyingAlongZ: false);
        CheckpointAt(root, "Checkpoint L2-E", new Vector3(hall.xW - 3.5f, hall.h3, -58f), new Vector3(6, 4, 4), 180);

        Slide(root, new Vector3(hall.xW - 1f, hall.h3, -61f), new Vector3(hall.xE + 1.5f, hall.h1, -96f), 4.5f);

        CanStairs(root, new Vector3(hall.xE - 1.4f, 0, -zN + 2.5f), Vector3.left, 5, hall.h1, descendingFromBoard: true);

        WetFloorSign(root, new Vector3(hall.x - 6f, 0, zN - 6f), 200f);
        WetFloorSign(root, new Vector3(hall.x + 9f, 0, -zN + 3f), -20f);
    }

    // a row of jars across a shelf board, too tall to jump: carry a box over to step up
    static void JarWall(Transform parent, float x0, float x1, float z, float y, float height)
    {
        var root = new GameObject("Jar Wall").transform;
        root.SetParent(parent);
        var jar = LoadModel("k:peanut-butter") ?? LoadModel("k:honey");
        if (jar != null)
            for (float x = x0 + 1.9f; x < x1 - 1.2f; x += 2.9f)
            {
                var j = Spawn(jar, root, (x * 37f) % 360f, 1f);
                var b = GetBounds(j);
                j.transform.localScale *= height / b.size.y;
                PlaceOnFloor(j, new Vector3(x, y, z));
            }
        Seal(root, "Jar Wall Collider", new Vector3((x0 + x1) / 2, y + height / 2, z), new Vector3(x1 - x0, height, 2.8f));
    }

    static void JarBlock(Transform parent, Vector3 min, Vector3 max)
    {
        var root = new GameObject("Jar Block").transform;
        root.SetParent(parent);
        var size = max - min;
        var center = (min + max) / 2;
        var jar = LoadModel("k:honey") ?? LoadModel("k:peanut-butter");
        if (jar != null)
        {
            var j = FitInto(jar, root, new Vector3(Mathf.Min(size.x, size.z) * 0.95f, size.y * 0.97f, Mathf.Min(size.x, size.z) * 0.95f));
            j.position = new Vector3(min.x + size.x * 0.55f, j.position.y, center.z);
            PlaceOnFloor(j.gameObject, new Vector3(j.position.x, min.y, center.z));
        }
        Seal(root, "Jar Block Collider", center, size);
    }

    static void BoxWall(Transform parent, float xc, float width, float z, int rows, int styleOffset)
    {
        var root = new GameObject("Box Wall").transform;
        root.SetParent(parent);
        int cols = Mathf.CeilToInt(width / CerealW);
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            var b = CerealBox(root, (r * 3 + c + styleOffset) % Cereals.Length);
            b.transform.localScale = new Vector3(width / cols / CerealW * 0.97f, 1f, 1f);
            b.transform.rotation = Quaternion.Euler(0, (c % 2) * 180f + ((r * 7 + c * 13) % 3 - 1) * 0.8f, 0);
            PlaceOnFloor(b, new Vector3(xc - width / 2 + (c + 0.5f) * width / cols, r * CerealH, z));
        }
        Solid(root, "Wall Collider", new Vector3(xc, rows * CerealH / 2, z), new Vector3(width + 2f, rows * CerealH, CerealD + 0.5f));
    }

    static void CanStairs(Transform parent, Vector3 start, Vector3 dir, int steps, float topY, bool descendingFromBoard = false)
    {
        var root = new GameObject("Can Stairs").transform;
        root.SetParent(parent);
        var can = BeanCanBuilder.GetOrCreate();
        var probe = Spawn(can, null, 0, 1);
        var pb = GetBounds(probe);
        Object.DestroyImmediate(probe);
        float canH = (topY - 0.5f) / steps;
        float s = canH / pb.size.y;
        float d = Mathf.Max(pb.size.x, pb.size.z) * s;
        var across = Vector3.Cross(Vector3.up, dir).normalized;
        for (int i = 0; i < steps; i++)
        {
            int n = descendingFromBoard ? steps - i : i + 1;
            var at = start + dir * (i * (2f * d + 0.3f));
            for (int q = 0; q < 4; q++)
            {
                var offset = dir * ((q / 2) - 0.5f) * (d + 0.05f) + across * ((q % 2) - 0.5f) * (d + 0.05f);
                for (int k = 0; k < n; k++)
                {
                    var c = Spawn(can, root, (i * 37 + k * 71 + q * 53) % 360, s);
                    PlaceOnFloor(c, at + offset + Vector3.up * k * canH);
                    AddBoxColliders(c);
                }
            }
        }
    }

    static void Pillar(Transform parent, Vector3 floorAt, float topY, bool lyingAlongZ)
    {
        var root = new GameObject("Box Pillar").transform;
        root.SetParent(parent);
        float y = 0;
        int i = 0;
        while (y + CerealH <= topY - CerealD + 0.01f)
        {
            var b = CerealBox(root, (i + (int)floorAt.z) % Cereals.Length);
            b.transform.rotation = Quaternion.Euler(0, (i * 47) % 360 + (lyingAlongZ ? 90 : 0), 0);
            PlaceOnFloor(b, new Vector3(floorAt.x, y, floorAt.z));
            y += CerealH;
            i++;
        }
        while (y < topY - CerealD - 0.01f)
        {
            float h = Mathf.Min(CerealD, topY - CerealD - y);
            var filler = CerealBox(root, (i++) % Cereals.Length);
            filler.transform.rotation = Quaternion.Euler(90, lyingAlongZ ? 0 : 90, 0);
            filler.transform.localScale = new Vector3(1, 1, h / CerealD);
            PlaceOnFloor(filler, new Vector3(floorAt.x, y, floorAt.z));
            y += h;
        }
        var cap = CerealBox(root, (i + 2) % Cereals.Length);
        cap.transform.rotation = Quaternion.Euler(90, lyingAlongZ ? 0 : 90, 0);
        PlaceOnFloor(cap, new Vector3(floorAt.x, topY - CerealD, floorAt.z));
        foreach (var b in root.GetComponentsInChildren<BoxCollider>()) b.enabled = true;
    }

    static void Ramp(Transform parent, Vector3 low, Vector3 high, float width, bool propped)
    {
        var root = new GameObject("Ramp").transform;
        root.SetParent(parent);
        var dir = high - low;
        root.position = (low + high) / 2;
        root.rotation = Quaternion.LookRotation(dir);
        var board = LoadModel("k:cutting-board");
        if (board != null)
        {
            var b = FitInto(board, root, new Vector3(width, 0.5f, dir.magnitude + 1f));
            b.localPosition = Vector3.down * 0.25f;
            b.localRotation = Quaternion.identity;
        }
        Solid(root, "Ramp Collider", root.position + root.up * -0.25f, Vector3.one);
        var col = root.GetChild(root.childCount - 1).GetComponent<BoxCollider>();
        col.transform.localPosition = Vector3.down * 0.25f;
        col.transform.localRotation = Quaternion.identity;
        col.size = new Vector3(width, 0.5f, dir.magnitude + 1f);
        if (propped) Pillar(parent, new Vector3(high.x, 0, high.z - 1.5f), high.y - 0.6f, lyingAlongZ: true);
    }

    static void Mop(Transform parent, Vector3 a, Vector3 b)
    {
        var root = new GameObject("Mop").transform;
        root.SetParent(parent);
        var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Mop Handle";
        handle.transform.SetParent(root);
        var mid = (a + b) / 2 + Vector3.up * 0.45f;
        handle.transform.SetPositionAndRotation(mid, Quaternion.FromToRotation(Vector3.up, b - a));
        handle.transform.localScale = new Vector3(0.9f, (b - a).magnitude / 2, 0.9f);
        handle.GetComponent<Renderer>().sharedMaterial = GetMaterial("Mop Handle", new Color(0.2f, 0.45f, 0.85f));
        Object.DestroyImmediate(handle.GetComponent<Collider>());
        var col = new GameObject("Handle Collider").AddComponent<BoxCollider>();
        col.transform.SetParent(root);
        col.transform.SetPositionAndRotation(mid, Quaternion.LookRotation(b - a));
        col.size = new Vector3(2f, 0.9f, (b - a).magnitude);
        var head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        head.name = "Mop Head";
        head.transform.SetParent(root);
        head.transform.SetPositionAndRotation(b + Vector3.down * 1.5f, Quaternion.Euler(0, 0, 8));
        head.transform.localScale = new Vector3(3.2f, 1.6f, 3.2f);
        head.GetComponent<Renderer>().sharedMaterial = GetMaterial("Mop Strands", new Color(0.85f, 0.85f, 0.78f));
        Object.DestroyImmediate(head.GetComponent<Collider>());
    }

    static void Sponge(Transform parent, Vector3 topAt, float launch)
    {
        var root = new GameObject("Sponge").transform;
        root.SetParent(parent);
        root.position = topAt;
        var size = new Vector3(3.4f, 1f, 2.2f);
        var visual = new GameObject("Sponge Visual").transform;
        visual.SetParent(root, false);
        Box("Sponge Body", visual, topAt + Vector3.up * size.y * 0.35f, new Vector3(size.x, size.y * 0.7f, size.z), GetMaterial("Sponge Yellow", new Color(1f, 0.85f, 0.2f)));
        Box("Scrub Layer", visual, topAt + Vector3.up * size.y * 0.85f, new Vector3(size.x, size.y * 0.3f, size.z), GetMaterial("Sponge Green", new Color(0.15f, 0.6f, 0.25f)));
        foreach (var c in visual.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        var trig = root.gameObject.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.center = Vector3.up * (size.y + 0.4f);
        trig.size = new Vector3(size.x, 1.2f, size.z);
        Solid(root, "Sponge Collider", topAt + Vector3.up * size.y / 2, size);
        root.gameObject.AddComponent<BouncePad>().Setup(launch, visual);
    }

    static void Slide(Transform parent, Vector3 top, Vector3 bottom, float width)
    {
        var root = new GameObject("Baking Tray Slide").transform;
        root.SetParent(parent);
        var dir = bottom - top;
        root.SetPositionAndRotation((top + bottom) / 2, Quaternion.LookRotation(dir));
        float len = dir.magnitude + 2f;
        var tray = GetMaterial("Baking Tray", new Color(0.62f, 0.64f, 0.66f));
        tray.SetFloat("_Metallic", 0.7f);
        tray.SetFloat("_Smoothness", 0.55f);
        void Part(string name, Vector3 local, Vector3 size)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = name;
            p.transform.SetParent(root, false);
            p.transform.localPosition = local;
            p.transform.localScale = size;
            p.GetComponent<Renderer>().sharedMaterial = tray;
        }
        Part("Tray", new Vector3(0, -0.2f, 0), new Vector3(width, 0.25f, len));
        Part("Rim Left", new Vector3(-width / 2, 0.4f, 0), new Vector3(0.25f, 1.2f, len));
        Part("Rim Right", new Vector3(width / 2, 0.4f, 0), new Vector3(0.25f, 1.2f, len));
        var wire = GetMaterial("Wire", new Color(0.2f, 0.2f, 0.2f));
        foreach (float f in new[] { -0.35f, 0.35f })
        {
            var at = root.TransformPoint(new Vector3(0, 0, f * len));
            float h = WallHeight - at.y;
            Box("Wire", parent, at + Vector3.up * h / 2, new Vector3(0.12f, h, 0.12f), wire);
        }
    }

    static void SodaSpill(Transform parent, float xc, float width, float z0, float z1, System.Random rng)
    {
        var root = new GameObject("Soda Spill").transform;
        root.SetParent(parent);
        var puddle = StoreAtmosphere.Glass("Soda Puddle", new Color(0.85f, 0.35f, 0.05f, 0.6f));
        puddle.SetFloat("_Smoothness", 0.95f);
        for (int i = 0; i < 26; i++)
        {
            float z = Mathf.Lerp(z0, z1, (i + (float)rng.NextDouble()) / 26f);
            float s = 8f + (float)rng.NextDouble() * 10f;
            var q = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            q.name = "Puddle";
            q.transform.SetParent(root);
            q.transform.position = new Vector3(xc + ((float)rng.NextDouble() - 0.5f) * width * 0.7f, 0.03f, z);
            q.transform.localScale = new Vector3(s, 0.01f, s * (0.6f + (float)rng.NextDouble() * 0.6f));
            q.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
            q.GetComponent<Renderer>().sharedMaterial = puddle;
            Object.DestroyImmediate(q.GetComponent<Collider>());
        }
        var bottles = Measure(new[] { ("k:soda-bottle", 0.3f), ("k:soda-can", 0.12f), ("k:soda", 0.2f), ("k:soda-can-crushed", 0.1f) });
        for (int i = 0; i < 18 && bottles.Count > 0; i++)
        {
            var t = bottles[rng.Next(bottles.Count)];
            var p = Spawn(t.prefab, root, (float)rng.NextDouble() * 360f, t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z));
            p.transform.rotation = Quaternion.Euler(90, (float)rng.NextDouble() * 360f, 0) * p.transform.rotation;
            PlaceOnFloor(p, new Vector3(xc + ((float)rng.NextDouble() - 0.5f) * width * 0.8f, 0, Mathf.Lerp(z0, z1, (float)rng.NextDouble())));
            SettleLater(p);
        }
    }

    static void WetFloorSign(Transform parent, Vector3 floorAt, float yaw)
    {
        var root = new GameObject("Wet Floor Sign").transform;
        root.SetParent(parent);
        root.SetPositionAndRotation(floorAt, Quaternion.Euler(0, yaw, 0));
        var yellow = GetMaterial("Caution Yellow", new Color(1f, 0.82f, 0.05f));
        float h = 0.6f * Scale, w = 0.3f * Scale;
        foreach (int s in new[] { -1, 1 })
        {
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel";
            panel.transform.SetParent(root, false);
            panel.transform.localPosition = new Vector3(0, h / 2 * 0.97f, s * h * 0.12f);
            panel.transform.localRotation = Quaternion.Euler(s * -14f, 0, 0);
            panel.transform.localScale = new Vector3(w, h, 0.15f);
            panel.GetComponent<Renderer>().sharedMaterial = yellow;
            float textYaw = yaw + (s < 0 ? 0 : 180);
            MakeText(root, root.TransformPoint(new Vector3(0, h * 0.55f, s * (h * 0.12f + 0.25f))), textYaw, "CAUTION\nWET FLOOR", w * 0.8f, h * 0.3f, Color.black);
            MakeIcon(root, root.TransformPoint(new Vector3(0, h * 0.25f, s * (h * 0.12f + 0.2f))), textYaw, "walk", w * 0.45f, Color.black);
        }
    }
}

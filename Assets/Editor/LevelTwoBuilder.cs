using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
        public float top, back;     // the shelf's top surface, and how far behind the edge its back panel is (level one only)
    }

    static Hall levelOneHall, levelTwoHall;

    // keeps the shelf products off a board the route walks along (and leaves its cubby unsealed)
    static void RouteBoard(Hall hall, bool westSide, float h, float zA, float zB)
    {
        float x0 = westSide ? hall.xW - hall.boardDepth - 1f : hall.xE - 1f;
        float x1 = westSide ? hall.xW + 1f : hall.xE + hall.boardDepth + 1f;
        var b = new Bounds();
        b.SetMinMax(new Vector3(x0, h - 0.5f, Mathf.Min(zA, zB)), new Vector3(x1, h + 7f, Mathf.Max(zA, zB)));
        NoStockZones.Add(b);
    }

    static Hall MakeHall(Transform eastAisle, float x)
    {
        var hall = new Hall { x = x, xW = x - WalkwayWidth / 2, xE = x + WalkwayWidth / 2, boardDepth = 7f };
        var hs = BoardHeights(eastAisle, hall.xE + 3f, eastAisle.position.z);
        hall.h1 = hs.Count > 0 ? hs[0] : 8.6f;
        hall.h2 = hs.Count > 1 ? hs[1] : 17.3f;
        hall.h3 = hs.Count > 2 ? hs[2] : 25.9f;
        return hall;
    }

    static void PlanLevelTwo(List<Transform> aisles, List<float> walkways, ShelfInfo shelf)
    {
        NoStockZones.Clear();
        var hall = MakeHall(aisles[0], walkways[1]);
        levelTwoHall = hall;
        RouteBoard(hall, true, hall.h1, 66f, 112f);
        RouteBoard(hall, true, hall.h2, 22f, 56f);
        RouteBoard(hall, false, hall.h2, -16f, 30f);
        RouteBoard(hall, false, hall.h3, -46f, -20f);
        RouteBoard(hall, true, hall.h3, -66f, -52f);
        RouteBoard(hall, false, hall.h1, -108f, -92f);
        PlanLevelOne(aisles, walkways);
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

        float ladderZ = Mathf.Min(zN - 9f, 100f);
        KitLadder(root, new Vector3(hall.xW, 0, ladderZ), Vector3.right, 0f, hall.h1, new Vector3(hall.xW - 2.2f, hall.h1, ladderZ));
        CheckpointAt(root, "Checkpoint L2-A", new Vector3(hall.xW - 3.5f, hall.h1, 95f), new Vector3(6, 4, 4), 180);
        RollingCan(root, new Vector3(hall.xW - 5.6f, hall.h1, 86f), new Vector3(4.4f, 0, 0), 1.3f, 0f);
        RollingCan(root, new Vector3(hall.xW - 5.6f, hall.h1, 77f), new Vector3(4.4f, 0, 0), 1.1f, 0.45f);

        Ramp(root, hall, westSide: true, hall.h1, 70f, hall.h2, 52f);
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
        MopBucket(root, new Vector3(hall.x + 12f, 0, zN - 4f), 35f);
        TrafficCone(root, new Vector3(hall.x + 4f, 0, zN - 10.5f), 15f);
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

    // toothpicks lashed end to end for the rails, single toothpicks for the rungs
    static void ToothpickLadder(Transform parent, Vector3 faceAt, Vector3 outward, float topY, Vector3 landing)
    {
        var root = new GameObject("Toothpick Ladder").transform;
        root.SetParent(parent);
        const float spacing = 0.42f, first = 0.42f, half = 0.55f, pick = 1.17f, thick = 0.09f;
        var wood = GetMaterial("Toothpick Wood", new Color(0.93f, 0.82f, 0.6f));
        var tip = ConeMesh();
        var across = Vector3.Cross(Vector3.up, outward).normalized;
        var plane = faceAt + outward * 0.18f;
        void Stick(Vector3 a, Vector3 b, float r)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            c.name = "Toothpick";
            c.transform.SetParent(root);
            var d = b - a;
            float body = d.magnitude - r * 4f;
            c.transform.SetPositionAndRotation((a + b) / 2, Quaternion.FromToRotation(Vector3.up, d));
            c.transform.localScale = new Vector3(r, body / 2, r);
            c.GetComponent<Renderer>().sharedMaterial = wood;
            Object.DestroyImmediate(c.GetComponent<Collider>());
            foreach (int end in new[] { -1, 1 })
            {
                var cone = new GameObject("Point", typeof(MeshFilter), typeof(MeshRenderer));
                cone.transform.SetParent(c.transform.parent);
                cone.transform.position = (a + b) / 2 + d.normalized * end * body / 2;
                cone.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized * end);
                cone.transform.localScale = new Vector3(r, r * 2f, r);
                cone.GetComponent<MeshFilter>().sharedMesh = tip;
                cone.GetComponent<MeshRenderer>().sharedMaterial = wood;
            }
        }

        float railTop = topY + 1.3f;
        foreach (int side in new[] { -1, 1 })
        {
            var x = plane + across * side * half;
            int n = Mathf.CeilToInt(railTop / (pick * 0.85f));
            for (int i = 0; i < n; i++)
            {
                float y0 = i * pick * 0.85f, y1 = Mathf.Min(y0 + pick, railTop + 0.1f);
                var wobble = outward * ((i % 2) * 2 - 1) * thick * 0.6f;
                Stick(x + Vector3.up * y0 + wobble, x + Vector3.up * y1 + wobble, thick * 1.15f);
            }
        }
        for (float y = first; y < topY + 0.01f; y += spacing)
        {
            var c = plane + Vector3.up * y;
            Stick(c - across * pick / 2, c + across * pick / 2, thick);
        }

        root.gameObject.AddComponent<Ladder>().Setup(plane, outward, topY, spacing, first, half, landing);
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

    // a plank from one shelf board up to the next, out in the walkway beside the shelf face. Each end gets a flat
    // landing that reaches back over the board edge, propped on a stack of cereal boxes, so Henry walks board ->
    // landing -> plank -> landing -> board with no gap, lip or floating end, and the planks are plain boxes so
    // what you see is exactly what you stand on
    static void Ramp(Transform parent, Hall hall, bool westSide, float lowY, float lowZ, float highY, float highZ)
    {
        var root = new GameObject("Ramp").transform;
        root.SetParent(parent);
        const float width = 5.4f, landingLen = 6f, thick = 0.4f, overlap = 1.2f;
        int out_ = westSide ? 1 : -1;          // from the shelf face out into the walkway
        float face = westSide ? hall.xW : hall.xE;
        float cx = face + out_ * (width / 2 + 0.3f);
        var wood = PlankMaterial();
        int along = highZ > lowZ ? 1 : -1;

        void Landing(float y, float zEnd, int awayFromRamp)
        {
            float z0 = zEnd, z1 = zEnd + awayFromRamp * landingLen;
            float xa = face - out_ * overlap, xb = face + out_ * (width + 0.3f);
            var center = new Vector3((xa + xb) / 2, y - thick / 2 + 0.04f, (z0 + z1) / 2);
            var size = new Vector3(Mathf.Abs(xb - xa), thick, landingLen);
            var go = Box2("Landing", root, center, size, wood);
            go.isStatic = true;
            Pillar(root, new Vector3(cx, 0, (z0 + z1) / 2), y - thick + 0.04f, lyingAlongZ: true);
        }
        Landing(lowY, lowZ, -along);
        Landing(highY, highZ, along);

        var a = new Vector3(cx, lowY + 0.04f, lowZ);
        var b = new Vector3(cx, highY + 0.04f, highZ);
        var dir = b - a;
        var plank = Box2("Ramp Plank", root, (a + b) / 2, new Vector3(width, thick, dir.magnitude), wood);
        plank.transform.rotation = Quaternion.LookRotation(dir);
        plank.transform.position = (a + b) / 2 - plank.transform.up * thick / 2;
        // cleats across the plank: grip for a tiny climber, and they read as a ramp from a distance
        for (float t = 0.12f; t < 0.95f; t += 0.16f)
        {
            var cleat = Box2("Cleat", plank.transform.parent, Vector3.Lerp(a, b, t) + plank.transform.up * 0.06f,
                new Vector3(width * 0.92f, 0.12f, 0.35f), PlankMaterial(dark: true));
            cleat.transform.rotation = plank.transform.rotation;
            Object.DestroyImmediate(cleat.GetComponent<Collider>());
        }
    }

    static Material PlankMaterial(bool dark = false)
    {
        var mat = GetMaterial(dark ? "Plank Wood Dark" : "Plank Wood", dark ? new Color(0.55f, 0.36f, 0.2f) : new Color(0.78f, 0.58f, 0.36f));
        mat.SetFloat("_Smoothness", 0.2f);
        return mat;
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
        // the head lies on the far board: a plastic socket on the end of the handle and the strands fanned out
        // flat from it (it used to be a drum sunk halfway through the board)
        var dir = (b - a).normalized;
        var across = Vector3.Cross(Vector3.up, dir);
        var socketAt = b + dir * 0.5f + Vector3.up * 0.45f;
        var socket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        socket.name = "Mop Socket";
        socket.transform.SetParent(root);
        socket.transform.SetPositionAndRotation(socketAt, Quaternion.FromToRotation(Vector3.up, dir));
        socket.transform.localScale = new Vector3(1.4f, 0.7f, 1.4f);
        socket.GetComponent<Renderer>().sharedMaterial = GetMaterial("Mop Socket", new Color(0.85f, 0.2f, 0.15f));
        Object.DestroyImmediate(socket.GetComponent<Collider>());
        var strands = GetMaterial("Mop Strands", new Color(0.85f, 0.85f, 0.78f));
        for (int i = 0; i < 18; i++)
        {
            float k = i / 17f;
            var fan = Quaternion.AngleAxis(Mathf.Lerp(-38f, 38f, k) + Mathf.Sin(i * 2.3f) * 5f, Vector3.up) * dir;
            float len = 3.2f + Mathf.Abs(Mathf.Sin(i * 1.7f)) * 1.2f;
            var start = socketAt + dir * 0.6f + across * (k - 0.5f) * 0.9f;
            var end = new Vector3(start.x, b.y + 0.18f + (i % 3) * 0.1f, start.z) + fan * len;
            var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
            s.name = "Strand";
            s.transform.SetParent(root);
            s.transform.SetPositionAndRotation((start + end) / 2, Quaternion.LookRotation(end - start));
            s.transform.localScale = new Vector3(0.36f, 0.28f, (end - start).magnitude);
            s.GetComponent<Renderer>().sharedMaterial = strands;
            Object.DestroyImmediate(s.GetComponent<Collider>());
        }
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

    static void Slide(Transform parent, Vector3 top, Vector3 bottom, float width, float exitSpeed = 10f, float ceiling = WallHeight)
    {
        var root = new GameObject("Baking Tray Slide").transform;
        root.SetParent(parent);
        var dir = bottom - top;
        root.SetPositionAndRotation((top + bottom) / 2, Quaternion.LookRotation(dir));
        float len = dir.magnitude + 2f;
        var tray = GetMaterial("Baking Tray", new Color(0.62f, 0.64f, 0.66f));
        tray.SetFloat("_Metallic", 0.65f);
        tray.SetFloat("_Smoothness", 0.42f);
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
        // the hanging wires come down to the rims so Henry rides between them
        var wire = GetMaterial("Wire", new Color(0.2f, 0.2f, 0.2f));
        foreach (float f in new[] { -0.35f, 0.35f })
        foreach (float rim in new[] { -1f, 1f })
        {
            var at = root.TransformPoint(new Vector3(rim * width / 2, 1f, f * len));
            float h = ceiling - at.y;
            Box("Wire", parent, at + Vector3.up * h / 2, new Vector3(0.12f, h, 0.12f), wire);
        }

        var ride = root.gameObject.AddComponent<BoxCollider>();
        ride.isTrigger = true;
        ride.center = new Vector3(0, 1.2f, 0);
        ride.size = new Vector3(width, 2.4f, len);
        float surface = -0.2f + 0.125f;
        root.gameObject.AddComponent<LieSlide>().Setup(top + root.up * surface, bottom + root.up * surface, exitSpeed);
    }

    static void SodaSpill(Transform parent, float xc, float width, float z0, float z1, System.Random rng)
    {
        var root = new GameObject("Soda Spill").transform;
        root.SetParent(parent);
        var splats = new Material[3];
        for (int v = 0; v < splats.Length; v++) splats[v] = SplatMaterial("Soda", new Color(0.86f, 0.42f, 0.08f), new Color(0.5f, 0.2f, 0.04f), 0.74f, v);
        // irregular puddles with a dried sugary rim, half as many and bigger than the old discs so they read as one
        // sticky mess instead of a stack of see-through circles (the rolls stay the same so the rest of the level
        // lays out as before)
        for (int i = 0; i < 26; i++)
        {
            float z = Mathf.Lerp(z0, z1, (i + (float)rng.NextDouble()) / 26f);
            float s = 8f + (float)rng.NextDouble() * 10f;
            float x = xc + ((float)rng.NextDouble() - 0.5f) * width * 0.7f;
            float stretch = 0.6f + (float)rng.NextDouble() * 0.6f;
            float yaw = (float)rng.NextDouble() * 360f;
            if (i % 2 == 1) continue;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Puddle";
            q.transform.SetParent(root);
            q.transform.SetPositionAndRotation(new Vector3(x, 0.025f + i * 0.0015f, z), Quaternion.Euler(90, yaw, 0));
            q.transform.localScale = new Vector3(s * 1.7f, s * 1.7f * stretch, 1f);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = splats[i / 2 % splats.Length];
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(q.GetComponent<Collider>());
        }
        var bottles = Measure(new[] { ("k:soda-bottle", 0.3f), ("k:soda-can", 0.12f), ("k:soda", 0.2f), ("k:soda-can-crushed", 0.1f) });
        for (int i = 0; i < 18 && bottles.Count > 0; i++)
        {
            var t = bottles[rng.Next(bottles.Count)];
            var p = SpawnProduct(t, root, (float)rng.NextDouble() * 360f, t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z));
            p.transform.rotation = Quaternion.Euler(90, (float)rng.NextDouble() * 360f, 0) * p.transform.rotation;
            PlaceOnFloor(p, new Vector3(xc + ((float)rng.NextDouble() - 0.5f) * width * 0.8f, 0, Mathf.Lerp(z0, z1, (float)rng.NextDouble())));
            SettleLater(p);
        }
    }

    // a spilled-drink puddle: a lumpy blob with a darker dried edge and a few drips thrown off it
    static Material SplatMaterial(string kind, Color body, Color rim, float opacity, int variant)
    {
        if (!AssetDatabase.IsValidFolder(GrimeFolder)) AssetDatabase.CreateFolder(MatFolder, "Grime");
        string texPath = $"{GrimeFolder}/{kind}_{variant}.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
        {
            const int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var rng = new System.Random(variant * 541 + 9);
            int seed = variant * 77 + 21;
            var drops = new List<Vector3>();
            for (int i = 0; i < 7 + variant * 2; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2, d = 0.72f + (float)rng.NextDouble() * 0.22f;
                drops.Add(new Vector3(Mathf.Cos(a) * d, Mathf.Sin(a) * d, 0.03f + (float)rng.NextDouble() * 0.06f));
            }
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x / (float)size - 0.5f) * 2f, v = (y / (float)size - 0.5f) * 2f;
                float wu = u + (Fbm(x / 90f, y / 90f, seed) - 0.5f) * 0.7f;
                float wv = v + (Fbm(x / 90f + 7f, y / 90f + 3f, seed) - 0.5f) * 0.7f;
                float r = Mathf.Sqrt(wu * wu + wv * wv) / 0.62f;
                float inside = Mathf.Clamp01((1f - r) * 14f);
                float edge = Mathf.Exp(-Mathf.Pow((r - 0.96f) * 16f, 2f));
                float drop = 0f;
                foreach (var dr in drops)
                {
                    float dd = Mathf.Sqrt((u - dr.x) * (u - dr.x) + (v - dr.y) * (v - dr.y)) / dr.z;
                    drop = Mathf.Max(drop, Mathf.Clamp01((1f - dd) * 6f));
                }
                float fine = Fbm(x / 14f, y / 14f, seed + 5);
                float a = Mathf.Max(inside * (opacity - 0.06f + 0.12f * fine), drop * opacity);
                a = Mathf.Max(a, edge * 0.85f * Mathf.Clamp01(inside + 0.4f));
                var c = Color.Lerp(body * (0.9f + 0.2f * fine), rim, Mathf.Clamp01(edge * 1.2f));
                c.a = Mathf.Clamp01(a);
                tex.SetPixel(x, y, c);
            }
            System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath);
            if (AssetImporter.GetAtPath(texPath) is TextureImporter ti)
            {
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.SaveAndReimport();
            }
        }
        var mat = StoreAtmosphere.Glass($"{kind} Splat {variant}", Color.white);
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        mat.SetFloat("_Smoothness", 0.9f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // a yellow plastic A-frame caution sign: two tapered boards hinged under a carry handle, black rubber feet, and the
    // print on both outer faces (a black CAUTION band, the warning triangle, WET FLOOR in English and Spanish). It
    // used to be two flat slabs with a line of text; one box collider covers the whole sign
    static void WetFloorSign(Transform parent, Vector3 floorAt, float yaw)
    {
        var root = new GameObject("Wet Floor Sign").transform;
        root.SetParent(parent);
        root.SetPositionAndRotation(floorAt, Quaternion.Euler(0, yaw, 0));
        var yellow = GetMaterial("Caution Yellow", new Color(1f, 0.82f, 0.05f));
        var black = GetMaterial("Caution Black", new Color(0.07f, 0.07f, 0.08f));
        const float h = 0.6f * Scale, bw = 0.3f * Scale, tw = bw * 0.74f, t = 0.22f, spread = 13f;
        float hinge = h * Mathf.Cos(spread * Mathf.Deg2Rad) + 0.2f;
        var mesh = CautionBoardMesh(bw, tw, h, t);
        float Width(float y) => tw + (bw - tw) * (-y / h);
        void Part(Transform p, string name, PrimitiveType type, Vector3 local, Vector3 scale, Material m, Vector3 euler = default)
        {
            var g = GameObject.CreatePrimitive(type);
            g.name = name;
            g.transform.SetParent(p, false);
            g.transform.localPosition = local;
            g.transform.localRotation = Quaternion.Euler(euler);
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            Object.DestroyImmediate(g.GetComponent<Collider>());
        }
        // text and icons are made in world space; pin each to its spot on the board's outer face
        void Pin(Transform board, Vector3 local)
        {
            var c = board.GetChild(board.childCount - 1);
            c.localPosition = local;
            c.localRotation = Quaternion.identity;
        }

        foreach (int s in new[] { -1, 1 })
        {
            var board = new GameObject(s < 0 ? "Front Board" : "Back Board").transform;
            board.SetParent(root, false);
            board.localPosition = new Vector3(0, hinge, 0);
            board.localRotation = Quaternion.Euler(spread, s < 0 ? 0 : 180, 0);
            board.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            board.gameObject.AddComponent<MeshRenderer>().sharedMaterial = yellow;
            float face = -t / 2;
            Part(board, "Caution Band", PrimitiveType.Cube, new Vector3(0, -0.17f * h, face - 0.02f), new Vector3(Width(-0.17f * h) * 0.9f, 0.2f * h, 0.04f), black);
            MakeText(board, board.position, 0, "CAUTION", Width(-0.17f * h) * 0.78f, 0.15f * h, new Color(1f, 0.82f, 0.05f), lit: true);
            Pin(board, new Vector3(0, -0.17f * h, face - 0.05f));
            MakeIcon(board, board.position, 0, "warning", 0.27f * h, Color.black, lit: true);
            Pin(board, new Vector3(0, -0.45f * h, face - 0.03f));
            MakeText(board, board.position, 0, "WET FLOOR", Width(-0.68f * h) * 0.82f, 0.12f * h, Color.black, lit: true);
            Pin(board, new Vector3(0, -0.68f * h, face - 0.03f));
            MakeText(board, board.position, 0, "PISO MOJADO", Width(-0.81f * h) * 0.6f, 0.06f * h, Color.black, lit: true);
            Pin(board, new Vector3(0, -0.81f * h, face - 0.03f));
            foreach (int side in new[] { -1, 1 })
                Part(board, "Foot", PrimitiveType.Cube, new Vector3(side * (bw / 2 - 0.4f), -h + 0.2f, 0), new Vector3(0.8f, 0.4f, t + 0.14f), black);
        }
        // the hinge rod and the carry handle on top
        Part(root, "Hinge", PrimitiveType.Cylinder, new Vector3(0, hinge, 0), new Vector3(0.45f, tw / 2, 0.45f), black, new Vector3(0, 0, 90));
        foreach (int side in new[] { -1, 1 })
            Part(root, "Handle Post", PrimitiveType.Cube, new Vector3(side * tw * 0.26f, hinge + 0.8f, 0), new Vector3(0.4f, 1.6f, 0.4f), yellow);
        Part(root, "Handle Grip", PrimitiveType.Cube, new Vector3(0, hinge + 1.6f, 0), new Vector3(tw * 0.52f + 0.4f, 0.45f, 0.45f), yellow);
        foreach (var r in root.GetComponentsInChildren<Renderer>()) r.gameObject.isStatic = true;
        var col = root.gameObject.AddComponent<BoxCollider>();
        float foot = h * Mathf.Sin(spread * Mathf.Deg2Rad) + t;
        col.center = new Vector3(0, (hinge + 1.8f) / 2, 0);
        col.size = new Vector3(bw, hinge + 1.8f, foot * 2);
    }

    static Mesh cautionBoard;

    // one board of the caution sign: a tapered slab hanging from its top edge (the hinge, at the origin) down to y = -h,
    // its outer face towards -z, hard edges. Made fresh once per editor session and shared by every sign (remaking it
    // for each sign deleted the mesh out from under the ones already built)
    static Mesh CautionBoardMesh(float bottomWidth, float topWidth, float h, float thick)
    {
        if (cautionBoard != null) return cautionBoard;
        const string path = "Assets/Models/CautionBoard.asset";
        AssetDatabase.DeleteAsset(path);
        var verts = new List<Vector3>();
        var tris = new List<int>();
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0) (a, b, c, d) = (d, c, b, a);
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            tris.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }
        float z = thick / 2;
        Vector3 TL(float s) => new Vector3(-topWidth / 2, 0, s * z);
        Vector3 TR(float s) => new Vector3(topWidth / 2, 0, s * z);
        Vector3 BL(float s) => new Vector3(-bottomWidth / 2, -h, s * z);
        Vector3 BR(float s) => new Vector3(bottomWidth / 2, -h, s * z);
        Quad(TL(-1), TR(-1), BR(-1), BL(-1), Vector3.back);
        Quad(TR(1), TL(1), BL(1), BR(1), Vector3.forward);
        Quad(TL(1), TR(1), TR(-1), TL(-1), Vector3.up);
        Quad(BL(-1), BR(-1), BR(1), BL(1), Vector3.down);
        Quad(TL(-1), BL(-1), BL(1), TL(1), Vector3.left);
        Quad(TR(1), BR(1), BR(-1), TR(-1), Vector3.right);
        var mesh = new Mesh { name = "Caution Board" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return cautionBoard = mesh;
    }

    // the cleaner's bucket (Kenney survival kit), left out by the spill
    static void MopBucket(Transform parent, Vector3 floorAt, float yaw)
    {
        var bucket = KitProp($"{SurvivalFolder}/bucket.fbx", parent, 0.36f, floorAt, yaw);
        if (bucket != null) bucket.name = "Mop Bucket";
    }

    // a push broom dropped flat on the floor, bristles down, from its head at `head` along `along`
    static void Broom(Transform parent, Vector3 head, Vector3 along)
    {
        var root = new GameObject("Broom").transform;
        root.SetParent(parent);
        along = new Vector3(along.x, 0, along.z).normalized;
        var across = Vector3.Cross(Vector3.up, along);
        var wood = GetMaterial("Broom Handle", new Color(0.72f, 0.52f, 0.3f));
        var block = GetMaterial("Broom Block", new Color(0.2f, 0.35f, 0.6f));
        var bristle = GetMaterial("Vac Bristle", new Color(0.95f, 0.8f, 0.25f));
        const float len = 22f, radius = 0.4f;
        Box("Broom Bristles", root, head + Vector3.up * 0.7f, new Vector3(1f, 1.4f, 1f), bristle);
        var b = root.GetChild(root.childCount - 1);
        b.rotation = Quaternion.LookRotation(across);
        b.localScale = new Vector3(1.6f, 1.4f, 7f);
        Box("Broom Head", root, head + Vector3.up * 1.9f, Vector3.one, block);
        var h = root.GetChild(root.childCount - 1);
        h.rotation = Quaternion.LookRotation(across);
        h.localScale = new Vector3(2f, 1f, 7.4f);
        // out of the top of the block and down to the floor
        var from = head + along * 0.9f + Vector3.up * 1.9f;
        var to = head + along * len + Vector3.up * radius;
        var stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stick.name = "Broom Handle";
        stick.transform.SetParent(root);
        stick.transform.SetPositionAndRotation((from + to) / 2, Quaternion.FromToRotation(Vector3.up, to - from));
        stick.transform.localScale = new Vector3(radius * 2, (to - from).magnitude / 2, radius * 2);
        stick.GetComponent<Renderer>().sharedMaterial = wood;
        Object.DestroyImmediate(stick.GetComponent<Collider>());
        stick.isStatic = true;
    }
}

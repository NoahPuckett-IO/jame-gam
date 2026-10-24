using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

static partial class GroceryStoreBuilder
{
    const float FreezerHeight = 2.0f * Scale;

    static void BuildLevelThree(Transform store, List<Transform> aisles, List<float> walkways, ShelfInfo shelf, float aisleLength)
    {
        var root = new GameObject("Level Three - Freezer Aisle").transform;
        root.SetParent(store);
        float x = walkways[2], half = WalkwayWidth / 2;
        float xE = x + half, xW = x - half;
        float fDepth = shelf.gondolaDepth / 2;
        float zS = -aisleLength / 2, zN = aisleLength / 2;
        var rng = new System.Random(303);

        FreezerWall(root, xE, -1, zS, zN, fDepth, rng);
        FreezerWall(root, xW, +1, zS, zN, fDepth, rng);

        var ice = IceMaterial();
        float poolA = zS + 14f, poolB = zS + 32f;
        float wallZ = poolB + 34f, bz = wallZ + 14f, crossZ = bz + 76f;
        float waterA = bz + 18f, finishZ = zN - 11f;
        IceSlab(root, new Vector3(x, 0.08f, (zS + poolA) / 2), new Vector3(WalkwayWidth, 0.16f, poolA - zS));
        IceSlab(root, new Vector3(x, 0.08f, (poolB + waterA) / 2), new Vector3(WalkwayWidth, 0.16f, waterA - poolB));
        IceSlab(root, new Vector3(x, 0.08f, (finishZ + zN) / 2), new Vector3(WalkwayWidth, 0.16f, zN - finishZ));
        IceZoneBox(root, new Vector3(x, 1.5f, 0), new Vector3(WalkwayWidth, 3f, aisleLength + 10f));
        IceZoneBox(root, new Vector3(xE + fDepth / 2, FreezerHeight + 1.5f, 0), new Vector3(fDepth, 3f, aisleLength));
        IceZoneBox(root, new Vector3(xW - fDepth / 2, FreezerHeight + 1.5f, 0), new Vector3(fDepth, 3f, aisleLength));
        Snowfall(root, new Vector3(x, WallHeight - 4f, 0), new Vector3(WalkwayWidth, 1f, aisleLength));
        for (int i = 0; i < 6; i++)
        {
            var l = new GameObject("Cold Light").AddComponent<Light>();
            l.transform.SetParent(root);
            l.transform.position = new Vector3(x, 14f, zS + (i + 0.5f) * aisleLength / 6);
            l.type = LightType.Point;
            l.color = new Color(0.6f, 0.85f, 1f);
            l.range = 40f;
            l.intensity = 120f;
        }


        FreezingWater(root, x, poolA, poolB);
        int cubes = 5;
        for (int i = 0; i < cubes; i++)
        {
            float z = Mathf.Lerp(poolA + 2.5f, poolB - 2.5f, i / (cubes - 1f));
            var at = new Vector3(x + (i % 2 == 0 ? -4f : 4f), 0.4f, z);
            if (i % 2 == 1)
            {
                var floe = IceCube(root, at + Vector3.left * 5f, new Vector3(3.2f, 2f, 3.2f), bob: 0);
                floe.isStatic = false;
                Kinematic(floe);
                floe.AddComponent<MovingPlatform>().Setup(new Vector3(10f, 0, 0), 2f, 0.6f, i * 0.3f);
            }
            else IceCube(root, at, new Vector3(3.2f, 2f, 3.2f), bob: 0.2f);
        }
        CheckpointAt(root, "Checkpoint L3-A", new Vector3(x, 0, poolB + 3f), new Vector3(WalkwayWidth, 4, 3), 0);

        IceGatePuzzle(root, x, wallZ, rng);
        CheckpointAt(root, "Checkpoint L3-B", new Vector3(x, 0, wallZ + 5f), new Vector3(WalkwayWidth, 4, 3), 0);

        WhippedCream(root, new Vector3(x + 7f, 0.15f, bz), 24f);
        IceTower(root, new Vector3(x + 11f, 0, bz + 7f), 12f);
        WhippedCream(root, new Vector3(x + 11f, 12f, bz + 7f), 24f);
        IceTower(root, new Vector3(x + 14.5f, 0, bz + 14f), 24f);
        WhippedCream(root, new Vector3(x + 14.5f, 24f, bz + 14f), 24f);
        CheckpointAt(root, "Checkpoint L3-C", new Vector3(xE + fDepth / 2, FreezerHeight, bz + 22f), new Vector3(fDepth, 4, 4), 0);

        var cube = IceCube(root, new Vector3(xE + fDepth * 0.5f, FreezerHeight + 0.95f, bz + 27f), new Vector3(1.9f, 1.9f, 1.9f), bob: 0);
        cube.name = "Carry Ice Cube";
        cube.isStatic = false;
        cube.AddComponent<Carryable>();
        LowIceWall(root, xE, xE + fDepth + 0.5f, bz + 33f, FreezerHeight, 3f);
        devWarps.Add(("Ice Wall", new Vector3(xE + fDepth * 0.5f, FreezerHeight + 1f, bz + 23f), 0));

        var bags = Measure(new[] { ("k:bag", 0.25f), ("k:ice-cream-cup", 0.12f), ("IceCream", 0.15f), ("k:pizza-box", 0.35f) });
        for (int i = 0; i < 6 && bags.Count > 0; i++)
        {
            var t = bags[rng.Next(bags.Count)];
            var p = Spawn(t.prefab, root, rng.Next(360), t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z));
            PlaceOnFloor(p, new Vector3(xE + fDepth * (0.3f + 0.4f * (float)rng.NextDouble()), FreezerHeight, bz + 39f + i * 6f));
            AddBoxColliders(p);
        }

        RollingCan(root, new Vector3(xE + fDepth - 2.2f, FreezerHeight, bz + 41f), new Vector3(-(fDepth - 4f), 0, 0), 1.1f, 0f, 2.4f);
        RollingCan(root, new Vector3(xE + 2f, FreezerHeight, bz + 55f), new Vector3(fDepth - 4f, 0, 0), 0.9f, 0.5f, 2.4f);

        FreezingWater(root, x, waterA, finishZ);
        devWarps.Add(("Ice Crossing", new Vector3(xE + fDepth / 2, FreezerHeight + 1f, crossZ - 16f), 0));
        float[] xs = { x + 13f, x + 5f, x - 3f, x - 11f };
        for (int i = 0; i < xs.Length; i++)
        {
            var at = new Vector3(xs[i], FreezerHeight - 0.4f, crossZ + (xs[i] - x) * 0.15f);
            var size = new Vector3(3.6f, 1.6f, 3.6f);
            if (i == 1)
            {
                var drift = IceCube(root, at + Vector3.back * 3f, size, bob: 0);
                drift.isStatic = false;
                Kinematic(drift);
                drift.AddComponent<MovingPlatform>().Setup(new Vector3(0, 0, 6f), 2.2f, 0.7f, 0f);
            }
            else if (i == 3)
            {
                var thin = IceCube(root, at + Vector3.up * 0.5f, new Vector3(4f, 0.6f, 4f), bob: 0);
                thin.name = "Thin Ice";
                thin.isStatic = false;
                var trig = thin.AddComponent<BoxCollider>();
                trig.isTrigger = true;
                trig.center = new Vector3(0, 0.5f + 0.5f / 0.6f, 0);
                trig.size = new Vector3(0.9f, 1f / 0.6f, 0.9f);
                Kinematic(thin);
                thin.AddComponent<CrumblePlatform>();
            }
            else IceCube(root, at, size, bob: 0.35f);
        }
        foreach (float dz in new[] { -6.5f, 6.5f })
            Seal(root, "Crossing Wall", new Vector3(x, WallHeight / 2, crossZ + dz), new Vector3(WalkwayWidth, WallHeight, 1f));
        IceBarrier(root, xE, xE + fDepth, crossZ + 6.5f);
        IceBarrier(root, xW - fDepth, xW, crossZ - 6.5f);
        IceBarrier(root, xW - fDepth, xW, crossZ + 17f);
        Seal(root, "Freezer Back East", new Vector3(xE + fDepth + 0.5f, (FreezerHeight + WallHeight) / 2, 0), new Vector3(1f, WallHeight - FreezerHeight, aisleLength));
        Seal(root, "Freezer Back West", new Vector3(xW - fDepth - 0.5f, (FreezerHeight + WallHeight) / 2, 0), new Vector3(1f, WallHeight - FreezerHeight, aisleLength));
        CheckpointAt(root, "Checkpoint L3-D", new Vector3(xW - fDepth / 2, FreezerHeight, crossZ + 4f), new Vector3(fDepth, 4, 4), 0);

        Slide(root, new Vector3(xW - 2f, FreezerHeight, crossZ + 12f), new Vector3(x + 10f, 0.4f, zN - 6f), 5f);
        foreach (var r in root.Find("Baking Tray Slide").GetComponentsInChildren<Renderer>()) r.sharedMaterial = ice;
        root.Find("Baking Tray Slide").name = "Ice Slide";
    }

    // a wall of ice blocks across a freezer top, just too tall to jump
    static void LowIceWall(Transform root, float x0, float x1, float z, float y, float height)
    {
        float w = x1 - x0;
        int n = Mathf.Max(1, Mathf.RoundToInt(w / 3.5f));
        for (int i = 0; i < n; i++)
        {
            var c = IceCube(root, new Vector3(x0 + (i + 0.5f) * w / n, y + height / 2, z), new Vector3(w / n * 0.97f, height, 2.6f), bob: 0);
            c.name = "Ice Wall Block";
            c.transform.rotation = Quaternion.identity;
        }
        Seal(root, "Ice Wall Collider", new Vector3((x0 + x1) / 2, y + height / 2, z), new Vector3(w, height, 2.6f));
    }

    static void FreezingWater(Transform root, float x, float zA, float zB)
    {
        var water = StoreAtmosphere.Glass("Melt Water", new Color(0.25f, 0.55f, 0.85f, 0.7f));
        water.SetFloat("_Smoothness", 0.97f);
        var pool = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pool.name = "Melt Water";
        pool.transform.SetParent(root);
        pool.transform.position = new Vector3(x, 0.05f, (zA + zB) / 2);
        pool.transform.localScale = new Vector3(WalkwayWidth, 0.1f, zB - zA);
        pool.GetComponent<Renderer>().sharedMaterial = water;
        Object.DestroyImmediate(pool.GetComponent<Collider>());
        var hazard = new GameObject("Melt Water Hazard").AddComponent<BoxCollider>();
        hazard.transform.SetParent(root);
        hazard.isTrigger = true;
        hazard.transform.position = new Vector3(x, 0.3f, (zA + zB) / 2);
        hazard.size = new Vector3(WalkwayWidth + 1f, 0.6f, zB - zA);
        hazard.gameObject.AddComponent<Hazard>();
    }

    static void IceBarrier(Transform root, float x0, float x1, float z)
    {
        float w = x1 - x0;
        int n = Mathf.Max(1, Mathf.RoundToInt(w / 4f));
        for (int r = 0; r < 3; r++)
        for (int i = 0; i < n - (r == 2 ? 1 : 0); i++)
        {
            float bx = x0 + (i + 0.5f + (r == 1 ? 0.25f : r == 2 ? 0.5f : 0f)) * w / n;
            var c = IceCube(root, new Vector3(Mathf.Min(bx, x1 - 2f), FreezerHeight + 2f + r * 3.8f, z), new Vector3(w / n * 0.98f, 3.8f, 3.4f), bob: 0);
            c.name = "Ice Block";
        }
        Seal(root, "Ice Barrier Seal", new Vector3((x0 + x1) / 2, (FreezerHeight + WallHeight) / 2, z), new Vector3(w + 1f, WallHeight - FreezerHeight, 3.4f));
    }

    static Material IceMaterial()
    {
        var m = StoreAtmosphere.Glass("Ice", new Color(0.75f, 0.92f, 1f, 0.55f));
        m.SetFloat("_Smoothness", 0.96f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", new Color(0.05f, 0.12f, 0.18f));
        EditorUtility.SetDirty(m);
        return m;
    }

    static void IceSlab(Transform parent, Vector3 center, Vector3 size)
    {
        var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
        s.name = "Ice Sheet";
        s.transform.SetParent(parent);
        s.transform.position = center;
        s.transform.localScale = size;
        s.GetComponent<Renderer>().sharedMaterial = IceMaterial();
        Object.DestroyImmediate(s.GetComponent<Collider>());
    }

    static void IceZoneBox(Transform parent, Vector3 center, Vector3 size)
    {
        var z = new GameObject("Ice Zone").AddComponent<BoxCollider>();
        z.transform.SetParent(parent);
        z.transform.position = center;
        z.isTrigger = true;
        z.size = size;
        z.gameObject.AddComponent<IceZone>();
    }

    static GameObject IceCube(Transform parent, Vector3 center, Vector3 size, float bob)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.name = "Ice Cube";
        c.transform.SetParent(parent);
        c.transform.position = center;
        c.transform.localScale = size;
        c.transform.rotation = Quaternion.Euler(0, center.x * 7f % 30f, 0);
        c.GetComponent<Renderer>().sharedMaterial = IceMaterial();
        if (bob > 0)
        {
            c.AddComponent<Rigidbody>().isKinematic = true;
            c.AddComponent<Bobbing>().Setup(bob, 2.5f + Mathf.Abs(center.z) % 1.5f, 0f);
        }
        return c;
    }

    static void IceTower(Transform parent, Vector3 floorAt, float topY)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(topY / 4f));
        float h = topY / n;
        for (int i = 0; i < n; i++)
        {
            var c = IceCube(parent, floorAt + Vector3.up * (h * i + h / 2), new Vector3(4.2f - i * 0.15f, h, 4.2f - i * 0.15f), bob: 0);
            c.transform.rotation = Quaternion.Euler(0, i * 17f, 0);
        }
    }

    static void WhippedCream(Transform parent, Vector3 baseAt, float launch)
    {
        var root = new GameObject("Whipped Cream Pad").transform;
        root.SetParent(parent);
        root.position = baseAt;
        var visual = new GameObject("Cream").transform;
        visual.SetParent(root, false);
        var model = LoadModel("k:whipped-cream");
        if (model != null)
        {
            var m = Spawn(model, visual, 0, 1);
            var b = GetBounds(m);
            m.transform.localScale *= 3.4f / Mathf.Max(b.size.x, b.size.z);
            PlaceOnFloor(m, baseAt);
        }
        else Box("Cream", visual, baseAt + Vector3.up * 0.6f, new Vector3(3f, 1.2f, 3f), GetMaterial("Cream", Color.white));
        foreach (var c in visual.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        Solid(root, "Pad Collider", baseAt + Vector3.up * 0.5f, new Vector3(3f, 1f, 3f));
        var trig = root.gameObject.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.center = Vector3.up * 1.4f;
        trig.size = new Vector3(3f, 1.2f, 3f);
        root.gameObject.AddComponent<BouncePad>().Setup(launch, visual);
    }

    static void IceGatePuzzle(Transform parent, float x, float wallZ, System.Random rng)
    {
        var root = new GameObject("Ice Gate Puzzle").transform;
        root.SetParent(parent);
        float half = WalkwayWidth / 2;

        float gw = 8.4f, gh = 6f, gd = 1.6f;
        var pizza = LoadModel("k:pizza-box");
        Transform gate;
        if (pizza != null)
        {
            gate = FitInto(pizza, root, new Vector3(gw, gh, gd));
            gate.position = new Vector3(x, gh / 2, wallZ);
            AddBoxColliders(gate.gameObject);
        }
        else
        {
            gate = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            gate.SetParent(root);
            gate.position = new Vector3(x, gh / 2, wallZ);
            gate.localScale = new Vector3(gw, gh, gd);
        }
        gate.name = "Frozen Pizza Gate";
        foreach (var t in gate.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
        gate.gameObject.AddComponent<Rigidbody>().isKinematic = true;
        var lifted = gate.gameObject.AddComponent<LiftedGate>();
        lifted.Setup(0.25f);

        for (float wx = x - half; wx < x + half - 0.1f; wx += 4f)
        {
            if (Mathf.Abs(wx + 2f - x) < gw / 2 + 0.5f) continue;
            for (int r = 0; r < 3; r++)
                IceCube(root, new Vector3(wx + 2f, 2f + r * 4f, wallZ), new Vector3(4f, 4f, 3f), bob: 0);
        }
        Seal(root, "Seal West", new Vector3((x - half - 3f + x - gw / 2) / 2, WallHeight / 2, wallZ), new Vector3(half + 3f - gw / 2, WallHeight, 3.2f));
        Seal(root, "Seal East", new Vector3((x + gw / 2 + x + half + 3f) / 2, WallHeight / 2, wallZ), new Vector3(half + 3f - gw / 2, WallHeight, 3.2f));
        float liftedTop = gh + 7.5f;
        Seal(root, "Seal Above", new Vector3(x, (liftedTop + WallHeight) / 2, wallZ), new Vector3(gw + 0.4f, WallHeight - liftedTop, 3.2f));

        const float nearArm = 8f, farArm = 14f, plankW = 2.4f, thick = 0.3f;
        float tubH = 1.8f;
        var tubPrefab = LoadModel("k:ice-cream-cup");
        float pivotY = tubH + thick / 2;
        float rest = Mathf.Asin(pivotY / farArm) * Mathf.Rad2Deg;
        float tipped = -Mathf.Asin(pivotY / nearArm) * Mathf.Rad2Deg;
        float pivotZ = wallZ - farArm * Mathf.Cos(rest * Mathf.Deg2Rad);
        if (tubPrefab != null)
        {
            var tub = Spawn(tubPrefab, root, 0, 1);
            var tb = GetBounds(tub);
            tub.transform.localScale *= tubH / tb.size.y;
            PlaceOnFloor(tub, new Vector3(x, 0, pivotZ));
            AddBoxColliders(tub);
        }
        var pivot = Plank(root, "Popsicle Stick", new Vector3(x, pivotY, pivotZ), nearArm, farArm, plankW, thick,
            GetMaterial("Popsicle Wood", new Color(0.9f, 0.78f, 0.55f)), ticks: false);
        pivot.gameObject.AddComponent<Seesaw>().Setup(pivot, rest, tipped, farArm, thick, 0f, lifted);
        pivot.rotation = Quaternion.Euler(rest, 0, 0);

        var step = IceCube(root, new Vector3(x - 9f, 0.95f, pivotZ - 14f), new Vector3(1.9f, 1.9f, 1.9f), bob: 0);
        step.name = "Carry Ice Cube";
        step.isStatic = false;
        step.AddComponent<Carryable>();

    }

    static void FreezerWall(Transform parent, float xFront, int dir, float z0, float z1, float depth, System.Random rng)
    {
        var root = new GameObject("Freezer Wall").transform;
        root.SetParent(parent);
        float len = z1 - z0, zc = (z0 + z1) / 2;
        float xc = xFront - dir * depth / 2;
        var body = GetMaterial("Freezer Body", new Color(0.82f, 0.86f, 0.9f));
        var trim = GetMaterial("Freezer Trim", new Color(0.12f, 0.13f, 0.15f));
        var glow = StoreAtmosphere.Emissive("Freezer Glow", new Color(0.65f, 0.88f, 1f), 1.5f);
        var glass = StoreAtmosphere.Glass("Freezer Glass", new Color(0.8f, 0.93f, 1f, 0.16f));
        var frost = FrostMaterial();
        var chrome = GetMaterial("Chrome", new Color(0.8f, 0.82f, 0.85f));
        chrome.SetFloat("_Metallic", 0.9f);
        chrome.SetFloat("_Smoothness", 0.8f);

        float baseH = 0.12f * Scale, topH = 0.12f * Scale;
        Box("Back Glow", root, new Vector3(xFront - dir * (depth - 0.4f), FreezerHeight / 2, zc), new Vector3(0.3f, FreezerHeight, len), glow);
        Box("Base", root, new Vector3(xc, baseH / 2, zc), new Vector3(depth, baseH, len), trim);
        Box("Top", root, new Vector3(xc, FreezerHeight - topH / 2, zc), new Vector3(depth, topH, len), body);
        var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.name = "Frost Top";
        top.transform.SetParent(root);
        top.transform.position = new Vector3(xc, FreezerHeight + 0.03f, zc);
        top.transform.localScale = new Vector3(depth, 0.06f, len);
        top.GetComponent<Renderer>().sharedMaterial = IceMaterial();
        Object.DestroyImmediate(top.GetComponent<Collider>());
        Solid(root, "Freezer Collider", new Vector3(xc, FreezerHeight / 2, zc), new Vector3(depth, FreezerHeight, len));

        float doorW = 0.75f * Scale;
        int doors = Mathf.Max(1, Mathf.RoundToInt(len / doorW));
        doorW = len / doors;
        float fx = xFront - dir * 0.05f;
        var products = Measure(CoolerFrozen);
        var shelfMat = GetMaterial("CoolerShelf", new Color(0.7f, 0.72f, 0.74f));
        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        const int levels = 5;
        float levelH = (FreezerHeight - baseH - topH) / levels;
        for (int k = 1; k < levels; k++)
            Box("Shelf", root, new Vector3(xFront - dir * depth * 0.45f, baseH + k * levelH, zc), new Vector3(depth * 0.85f, 0.25f, len), shelfMat);

        for (int d = 0; d <= doors; d++)
        {
            float z = z0 + d * doorW;
            Box("Door Frame", root, new Vector3(fx, FreezerHeight / 2, z), new Vector3(0.5f, FreezerHeight, 0.6f), trim);
            if (d == doors) break;
            float zm = z + doorW / 2;
            var pane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pane.name = "Door Glass";
            pane.transform.SetParent(root);
            pane.transform.SetPositionAndRotation(new Vector3(fx + dir * 0.02f, (baseH + FreezerHeight - topH) / 2, zm), Quaternion.Euler(0, dir > 0 ? -90 : 90, 0));
            pane.transform.localScale = new Vector3(doorW - 0.6f, FreezerHeight - baseH - topH, 1);
            pane.GetComponent<Renderer>().sharedMaterial = glass;
            Object.DestroyImmediate(pane.GetComponent<Collider>());
            var fr = Object.Instantiate(pane, root);
            fr.name = "Frost";
            fr.transform.position += Vector3.right * dir * 0.03f;
            fr.transform.rotation = pane.transform.rotation * Quaternion.Euler(0, 0, d % 2 * 180);
            fr.GetComponent<Renderer>().sharedMaterial = frost;
            Box("Handle", root, new Vector3(fx + dir * 0.6f, FreezerHeight * 0.48f, zm + doorW * 0.36f * (d % 2 == 0 ? 1 : -1)), new Vector3(0.35f, FreezerHeight * 0.32f, 0.35f), chrome);
            if (d % 3 == 1)
            {
                var l = new GameObject("Freezer Light").AddComponent<Light>();
                l.transform.SetParent(root);
                l.transform.position = new Vector3(xFront - dir * depth * 0.4f, FreezerHeight * 0.85f, zm);
                l.type = LightType.Point;
                l.color = new Color(0.7f, 0.9f, 1f);
                l.range = 16f;
                l.intensity = 40f;
            }
            if (products.Count > 0)
                for (int k = 0; k < levels; k++)
                    FillRowAlongZ(root, products, rng, Rand, z + 0.5f, z + doorW - 0.5f, baseH + k * levelH + 0.13f, levelH - 0.5f, xFront - dir * 0.8f, depth * 0.75f, -dir);
        }

        var cone = ConeMesh();
        var ice = IceMaterial();
        for (float z = z0 + 1f; z < z1; z += 1.6f + (float)rng.NextDouble() * 2.5f)
        {
            var ic = new GameObject("Icicle", typeof(MeshFilter), typeof(MeshRenderer));
            ic.transform.SetParent(root);
            float h = 1.5f + (float)rng.NextDouble() * 3.5f;
            ic.transform.position = new Vector3(xFront + dir * 0.2f, FreezerHeight, z);
            ic.transform.rotation = Quaternion.Euler(180, 0, 0);
            ic.transform.localScale = new Vector3(0.5f + h * 0.08f, h, 0.5f + h * 0.08f);
            ic.GetComponent<MeshFilter>().sharedMesh = cone;
            ic.GetComponent<MeshRenderer>().sharedMaterial = ice;
        }
    }

    static void FillRowAlongZ(Transform parent, List<ProductType> types, System.Random rng, System.Func<float, float, float> rand,
        float z0, float z1, float y, float room, float xFront, float depth, int inward)
    {
        float gap = 0.006f * Scale;
        float z = z0;
        while (z < z1)
        {
            var t = types[rng.Next(types.Count)];
            var b = t.bounds;
            float s = Mathf.Min(t.size / Mathf.Max(b.x, b.y, b.z), 0.85f * room / b.y, 0.95f * depth / b.x);
            float w = b.z * s, dx = b.x * s;
            if (z + w > z1) break;
            int rows = Mathf.Clamp(Mathf.FloorToInt(depth / (dx + gap)), 1, 4);
            int facings = rng.Next(2, 5);
            for (int f = 0; f < facings && z + w <= z1; f++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (rng.NextDouble() < 0.04) continue;
                    var p = Spawn(t.prefab, parent, t.yaw + (inward > 0 ? 180 : 0) + rand(-4f, 4f), s);
                    PlaceOnFloor(p, new Vector3(xFront + inward * (dx / 2 + r * (dx + gap)), y, z + w / 2));
                }
                z += w + gap;
            }
            z += gap;
        }
    }

    static Material FrostMaterial()
    {
        const string texPath = MatFolder + "/Frost.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float edge = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v)) * 2f;
                float n = Fbm(x / 18f, y / 18f, 41);
                float crystals = Mathf.Pow(Fbm(x / 5f, y / 5f, 77), 3f);
                float a = Mathf.Clamp01((0.75f - edge) * 2.2f + (n - 0.5f) * 1.2f) * 0.75f + crystals * 0.25f + (v < 0.25f ? (0.25f - v) * 1.5f : 0);
                tex.SetPixel(x, y, new Color(0.92f, 0.97f, 1f, Mathf.Clamp01(a)));
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
        var mat = StoreAtmosphere.Glass("Frost", Color.white);
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        mat.SetFloat("_Smoothness", 0.4f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static Mesh ConeMesh()
    {
        const string path = "Assets/Models/Cone.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        if (!AssetDatabase.IsValidFolder("Assets/Models")) AssetDatabase.CreateFolder("Assets", "Models");
        const int seg = 16;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i < seg; i++)
        {
            float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
            var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f);
            var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f);
            int b = verts.Count;
            verts.Add(p0); verts.Add(Vector3.up); verts.Add(p1);
            tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            b = verts.Count;
            verts.Add(Vector3.zero); verts.Add(p0); verts.Add(p1);
            tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
        }
        var mesh = new Mesh { name = "Cone" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    static Material ParticleMaterial(string name, Color color)
    {
        const string texPath = MatFolder + "/SoftDot.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1 - d) * Mathf.Clamp01(1 - d)));
            }
            System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath);
            if (AssetImporter.GetAtPath(texPath) is TextureImporter ti) { ti.alphaIsTransparency = true; ti.SaveAndReimport(); }
        }
        string path = $"{MatFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void Snowfall(Transform parent, Vector3 center, Vector3 size)
    {
        var go = new GameObject("Snowfall");
        go.transform.SetParent(parent);
        go.transform.position = center;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 14f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
        main.maxParticles = 3000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.prewarm = true;
        main.gravityModifier = 0.03f;
        var emission = ps.emission;
        emission.rateOverTime = 180f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = size;
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 0.2f;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial("Snow", new Color(1f, 1f, 1f, 0.9f));
    }
}

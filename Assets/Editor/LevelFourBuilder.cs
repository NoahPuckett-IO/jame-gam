using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

static partial class GroceryStoreBuilder
{
    static void BuildLevelFour(Transform store, List<float> walkways, float aisleLength)
    {
        var root = new GameObject("Level Four - Falling Aisle").transform;
        root.SetParent(store);
        float x = walkways[3], half = WalkwayWidth / 2;
        float zN = aisleLength / 2, zS = -aisleLength / 2;

        var templates = new GameObject("Falling Item Templates").transform;
        templates.SetParent(root);
        var list = new List<GameObject>();
        var seen = new HashSet<string>();
        foreach (var (model, size) in Stock.SelectMany(s => s).Concat(CoolerDrinks).Concat(CoolerFrozen))
        {
            if (!seen.Add(model)) continue;
            var prefab = LoadModel(model);
            if (prefab == null) continue;
            var t = MeasureProduct(prefab, size);
            var go = SpawnProduct(t, templates, t.yaw, t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z) * 1.3f);
            go.name = model;
            list.Add(go);
        }
        for (int i = 0; i < Cereals.Length; i++)
        {
            var box = CerealBox(templates, i, 0.8f);
            list.Add(box);
        }
        foreach (var go in list)
        {
            go.transform.position = new Vector3(x, -50f, 0);
            foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
            go.SetActive(false);
        }

        var director = root.gameObject.AddComponent<FallingItemsDirector>();
        director.Setup(list.ToArray(), x, half, zN - 8f, zS + 8f, DashMaterial());

        CheckpointAt(root, "Checkpoint L4-A", new Vector3(x, 0, zN * 0.35f), new Vector3(WalkwayWidth, 4, 3), 180);
        CheckpointAt(root, "Checkpoint L4-B", new Vector3(x, 0, zS * 0.35f), new Vector3(WalkwayWidth, 4, 3), 180);
        ShelfGuards(root, x, zS, zN);
        DressFallingAisle(root, x, half);

        var rng = new System.Random(404);
        var debris = Measure(Stock[2].Concat(Stock[3]).ToArray());
        Physics.SyncTransforms();
        for (int i = 0; i < 20 && debris.Count > 0; i++)
        {
            var t = debris[rng.Next(debris.Count)];
            var p = SpawnProduct(t, root, rng.Next(360), t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z));
            p.transform.rotation = Quaternion.Euler(50 + rng.Next(40), rng.Next(360), rng.Next(30)) * p.transform.rotation;
            float side = rng.Next(2) == 0 ? -1 : 1;
            var at = new Vector3(x + side * (half - 2.5f - (float)rng.NextDouble() * 4f), 0, Mathf.Lerp(zS + 6f, zN - 6f, (float)rng.NextDouble()));
            // not inside the cart, a crate or a box pile
            if (Physics.OverlapSphere(at + Vector3.up * 1.2f, 1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).Length > 0)
            {
                Object.DestroyImmediate(p);
                continue;
            }
            PlaceOnFloor(p, at);
            SettleLater(p);
        }
    }

    // the aisle used to be an empty run under the falling groceries. Now there's a mess to weave through on the way
    // down it, side to side: a cart tipped over against the shelf with its shopping spilled, produce crates, whole
    // watermelons, piles of boxes, and two slippery spills (milk, then juice). The checkpoint spots stay clear
    static void DressFallingAisle(Transform root, float x, float half)
    {
        var mess = new GameObject("Aisle Mess").transform;
        mess.SetParent(root);
        var rng = new System.Random(4040);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        float west = x - half, east = x + half;
        var produce = Measure(new[] { ("Apple", 0.09f), ("k:orange", 0.08f), ("k:lemon", 0.07f), ("tomate", 0.09f), ("k:pear", 0.1f),
            ("k:onion", 0.08f), ("k:paprika", 0.1f), ("banana", 0.2f), ("lettuce", 0.2f), ("k:cabbage", 0.2f), ("k:broccoli", 0.15f) });

        void Spill(Vector3 center, float spreadX, float spreadZ, int count)
        {
            for (int i = 0; i < count && produce.Count > 0; i++)
            {
                var t = produce[rng.Next(produce.Count)];
                var p = SpawnProduct(t, mess, R(0f, 360f), t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z));
                if (rng.NextDouble() < 0.5) p.transform.rotation = Quaternion.Euler(R(40f, 100f), 0, 0) * p.transform.rotation;
                AddBoxColliders(p);
                if (PlaceClear(p, () => center + new Vector3(R(-spreadX, spreadX), 0, R(-spreadZ, spreadZ)))) SettleLater(p);
            }
        }

        // a cart on its side against the west shelf, the basket open to the aisle and its shopping rolled out of it
        var cart = LoadModel("ShopingCar");
        if (cart != null)
        {
            var c = PlaceProp(cart, mess, 1.05f, 1.1f, alongX: false, new Vector3(x - 8f, 0, 78f));
            c.name = "Tipped Cart";
            FitCartColliders(c);
            c.transform.RotateAround(GetBounds(c).center, Vector3.forward, -90f);
            c.transform.Rotate(0, 8f, 0, Space.World);
            var b = GetBounds(c);
            PlaceOnFloor(c, new Vector3(west + b.size.x / 2 + 2.6f, 0, 78f));
            Spill(new Vector3(GetBounds(c).max.x + 4f, 0, 78f), 3.5f, 8f, 9);
        }

        // produce crates on the east side, one stacked, with fruit heaped on top
        var crate = Usable(AssetDatabase.LoadAssetAtPath<GameObject>($"{PlatformerFolder}/crate.fbx"), $"{PlatformerFolder}/crate.fbx");
        GameObject Crate(Vector3 at, float size)
        {
            var g = SpawnSized(crate, mess, size, R(-12f, 12f));
            g.name = "Produce Crate";
            PlaceOnFloor(g, at);
            AddBoxColliders(g);
            return g;
        }
        // fruit on a crate's lid, one to each quarter so no two pieces sit inside each other
        void Heap(GameObject under, int count)
        {
            var b = GetBounds(under);
            for (int i = 0; i < count && i < 4 && produce.Count > 0; i++)
            {
                var t = produce[rng.Next(Mathf.Min(7, produce.Count))];
                var p = SpawnProduct(t, mess, R(0f, 360f), t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z));
                var cell = new Vector3((i % 2 - 0.5f) * b.size.x * 0.5f, 0, (i / 2 - 0.5f) * b.size.z * 0.5f);
                PlaceOnFloor(p, new Vector3(b.center.x + cell.x + R(-0.1f, 0.1f), b.max.y, b.center.z + cell.z + R(-0.1f, 0.1f)));
                AddBoxColliders(p);
            }
        }
        if (crate != null)
        {
            var a = Crate(new Vector3(east - 5.4f, 0, 60f), 5.6f);
            var b = Crate(new Vector3(east - 5.6f, 0, 54f), 5.2f);
            var top = Crate(new Vector3(east - 5.4f, GetBounds(a).max.y, 58.6f), 4.6f);
            Heap(top, 6);
            Heap(b, 4);
            Spill(new Vector3(east - 11f, 0, 57f), 2.5f, 4f, 3);

            var lone = Crate(new Vector3(west + 5.6f, 0, -72f), 5.4f);
            Heap(lone, 5);
            Crate(new Vector3(west + 11f, 0, -75f), 4.8f).transform.Rotate(0, 30f, 0, Space.World);
        }

        // whole watermelons rolled into the middle
        var melon = Measure(new[] { ("k:watermelon", 0.32f) });
        if (melon.Count > 0)
            foreach (var at in new[] { new Vector3(x - 4f, 0, 48f), new Vector3(west + 15f, 0, -69f) })
            {
                var t = melon[0];
                var m = SpawnProduct(t, mess, R(0f, 360f), t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z) * R(0.9f, 1.1f));
                m.transform.rotation = Quaternion.Euler(R(-20f, 20f), 0, R(70f, 110f)) * m.transform.rotation;
                PlaceOnFloor(m, at);
                AddBoxColliders(m);
            }

        // piles of stock boxes, west then east
        BoxPile(mess, west + 2.6f, west + 16.6f, -4f, 7f, rng, produce);
        BoxPile(mess, east - 16.6f, east - 2.6f, -63f, -51f, rng, produce);

        // a display bin knocked on its side, oranges rolled out of it
        var bin = CardboardBox(mess, BoxKind.OpenWide, 6.5f, 0f, new Vector3(east - 6f, 0, -16f));
        if (bin != null)
        {
            bin.name = "Tipped Display Bin";
            bin.transform.RotateAround(GetBounds(bin).center, Vector3.forward, 90f);
            bin.transform.Rotate(0, -25f, 0, Space.World);
            PlaceOnFloor(bin, new Vector3(east - 6.8f, 0, -16f));
            var oranges = Measure(new[] { ("k:orange", 0.08f) });
            for (int i = 0; i < 8 && oranges.Count > 0; i++)
            {
                var t = oranges[0];
                var o = SpawnProduct(t, mess, R(0f, 360f), t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z));
                AddBoxColliders(o);
                if (PlaceClear(o, () => new Vector3(east - 12.5f - R(0f, 7f), 0, -16f + R(-5f, 5f)))) SettleLater(o);
            }
        }

        // slippery spills from a burst carton, with a sign by each
        DrinkSpill(mess, "Milk", new Color(0.97f, 0.97f, 0.95f), new Color(0.84f, 0.85f, 0.83f), "Milk", new Vector3(x + 2f, 0, 19f), 22f, 17f, rng);
        WetFloorSign(mess, new Vector3(west + 7f, 0, 30f), 150f);
        TrafficCone(mess, new Vector3(x + 13f, 0, 28f), 10f);
        TrafficCone(mess, new Vector3(x + 4f, 0, 8.5f), -25f);
        DrinkSpill(mess, "Juice", new Color(1f, 0.62f, 0.12f), new Color(0.82f, 0.42f, 0.05f), "Juice", new Vector3(x + 5f, 0, -88f), 16f, 12f, rng);
        WetFloorSign(mess, new Vector3(east - 7f, 0, -78f), -30f);
        TrafficCone(mess, new Vector3(x - 5f, 0, -79f), 40f);
    }

    // collision that follows the shopping cart instead of one box per part (the basket's box was solid all the way
    // through and the wheels' box filled the space under the cart). The frame tubes, chassis, wheels and handle
    // collide as their own meshes; the wire basket is the biggest mesh and gets one thin box per wall, leaning with
    // the wall, open at the top. Run on the cart standing upright, before it's tipped over
    static void FitCartColliders(GameObject cart)
    {
        foreach (var c in cart.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        var parts = cart.GetComponentsInChildren<MeshFilter>().Where(m => m.sharedMesh != null).ToList();
        if (parts.Count == 0) return;
        var basket = parts.OrderByDescending(m => m.sharedMesh.vertexCount).First();
        foreach (var mf in parts)
            if (mf != basket) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;

        var verts = basket.sharedMesh.vertices.Select(v => basket.transform.TransformPoint(v)).ToList();
        var b = new Bounds(verts[0], Vector3.zero);
        foreach (var v in verts) b.Encapsulate(v);
        float t = Mathf.Max(0.3f, b.size.y * 0.04f);
        var walls = new GameObject("Basket Walls").transform;
        walls.SetParent(cart.transform, true);

        void Wall(Vector3 center, Vector3 size, Quaternion rot)
        {
            var w = new GameObject("Basket Wall").transform;
            w.SetPositionAndRotation(center, rot);
            w.SetParent(walls, true);
            var box = w.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(size.x / w.lossyScale.x, size.y / w.lossyScale.y, size.z / w.lossyScale.z);
        }

        // the floor of the basket
        var floor = verts.Where(v => v.y < b.min.y + b.size.y * 0.12f).ToList();
        Wall(new Vector3((floor.Min(v => v.x) + floor.Max(v => v.x)) / 2, b.min.y + t / 2, (floor.Min(v => v.z) + floor.Max(v => v.z)) / 2),
            new Vector3(floor.Max(v => v.x) - floor.Min(v => v.x), t, floor.Max(v => v.z) - floor.Min(v => v.z)), Quaternion.identity);

        // the four sides: find each one's lean from the outermost wires low down and high up
        foreach (int axis in new[] { 0, 2 })
        foreach (int s in new[] { -1, 1 })
        {
            float edge = s > 0 ? b.max[axis] : b.min[axis];
            var band = verts.Where(v => s * (v[axis] - edge) > -b.size[axis] * 0.12f).ToList();
            var low = band.Where(v => v.y < b.center.y).ToList();
            var high = band.Where(v => v.y >= b.center.y).ToList();
            float lean = 0f, at = edge;
            if (low.Count > 0 && high.Count > 0)
            {
                float a0 = s > 0 ? low.Max(v => v[axis]) : low.Min(v => v[axis]);
                float a1 = s > 0 ? high.Max(v => v[axis]) : high.Min(v => v[axis]);
                float y0 = low.Average(v => v.y), y1 = high.Average(v => v.y);
                lean = (a1 - a0) / Mathf.Max(y1 - y0, 0.01f);
                at = a0 + lean * (b.center.y - y0);
            }
            int across = axis == 0 ? 2 : 0;
            float w0 = band.Min(v => v[across]), w1 = band.Max(v => v[across]);
            var center = new Vector3();
            center[axis] = at - s * t / 2;
            center.y = b.center.y;
            center[across] = (w0 + w1) / 2;
            var size = new Vector3();
            size[axis] = t;
            size.y = b.size.y * Mathf.Sqrt(1 + lean * lean);
            size[across] = w1 - w0;
            float angle = Mathf.Atan(lean) * Mathf.Rad2Deg;
            var rot = axis == 0 ? Quaternion.AngleAxis(-angle, Vector3.forward) : Quaternion.AngleAxis(angle, Vector3.right);
            Wall(center, size, rot);
        }
    }

    // a carton lying where it burst, a puddle of its drink spreading from it, and an ice zone under the puddle
    static void DrinkSpill(Transform parent, string kind, Color body, Color rim, string carton, Vector3 center, float width, float length, System.Random rng)
    {
        var root = new GameObject($"{kind} Spill").transform;
        root.SetParent(parent);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        IceZoneBox(root, center + Vector3.up * 1.5f, new Vector3(width * 0.8f, 3f, length * 0.8f));
        for (int i = 0; i < 4; i++)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Puddle";
            q.transform.SetParent(root);
            float s = i == 0 ? Mathf.Max(width, length) : Mathf.Max(width, length) * R(0.4f, 0.6f);
            var at = center + (i == 0 ? Vector3.zero : new Vector3(R(-0.35f, 0.35f) * width, 0, R(-0.35f, 0.35f) * length));
            q.transform.SetPositionAndRotation(at + Vector3.up * (0.025f + i * 0.002f), Quaternion.Euler(90, R(0f, 360f), 0));
            q.transform.localScale = new Vector3(s, s * (i == 0 ? length / width : R(0.7f, 1.2f)), 1f);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = SplatMaterial(kind, body, rim, 0.9f, i % 3);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(q.GetComponent<Collider>());
        }
        var prefab = LoadModel(carton);
        if (prefab == null) return;
        var t = MeasureProduct(prefab, 0.25f);
        var c = SpawnProduct(t, root, R(0f, 360f), t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z));
        c.name = $"Burst {carton}";
        c.transform.rotation = Quaternion.Euler(0, 0, 90) * c.transform.rotation;
        PlaceOnFloor(c, center + new Vector3(-width * 0.45f, 0, length * 0.3f));
        AddBoxColliders(c);
    }

    static Material DashMaterial()
    {
        const string texPath = MatFolder + "/Dash.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
        {
            const int w = 64, h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float a = x < w * 0.6f ? 1f : 0f;
                float edge = Mathf.Min(y, h - 1 - y) / 3f;
                tex.SetPixel(x, y, new Color(1, 1, 1, a * Mathf.Clamp01(edge)));
            }
            System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath);
            if (AssetImporter.GetAtPath(texPath) is TextureImporter ti)
            {
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
        }
        var mat = ParticleMaterial("Dashed Warning", new Color(1f, 0.12f, 0.1f, 1f));
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        EditorUtility.SetDirty(mat);
        return mat;
    }
}

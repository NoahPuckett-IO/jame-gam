using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Store dressing for GroceryStoreBuilder: glowing refrigerated coolers along the end walls,
// backlit signs, a drop ceiling, and endcap displays at the open end of each aisle.
static partial class GroceryStoreBuilder
{
    const float CoolerDepth = 0.85f * Scale;
    const float CoolerHeight = 2.0f * Scale;
    const float CoolerDoorWidth = 0.75f * Scale;

    static readonly (string model, float size)[] CoolerDrinks =
    {
        ("Milk", 0.25f), ("k:carton", 0.25f), ("Juice", 0.25f), ("k:soda-bottle", 0.3f), ("k:soda-can", 0.12f),
        ("k:carton-small", 0.15f), ("k:soda", 0.2f), ("Eggs", 0.3f), ("cheese", 0.15f), ("k:cheese", 0.15f),
    };

    static readonly (string model, float size)[] CoolerFrozen =
    {
        ("IceCream", 0.15f), ("k:ice-cream", 0.15f), ("k:pizza-box", 0.35f), ("k:popsicle", 0.15f), ("meat", 0.25f),
        ("fish", 0.3f), ("k:meat-raw", 0.2f), ("k:whole-ham", 0.25f), ("ChickenLeg", 0.15f),
    };

    static List<ProductType> Measure((string model, float size)[] stock) =>
        stock.Select(s => (prefab: LoadModel(s.model), s.size)).Where(t => t.prefab != null)
            .Select(t => MeasureProduct(t.prefab, t.size)).ToList();

    // ---------- Text & signs ----------

    static Material SignTextMaterial()
    {
        const string path = MatFolder + "/SignText.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("JameGam/Sign Text"));
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static Font SignFont() =>
        AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BebasNeue-Regular.ttf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    // Depth-tested 3D text, sized to fit inside maxWidth × maxHeight. Yaw 0 reads correctly when
    // looking along +Z.
    static void MakeText(Transform parent, Vector3 pos, float yaw, string text, float maxWidth, float maxHeight, Color color)
    {
        const int fontSize = 96;
        var font = SignFont();
        var t = new GameObject("Text").AddComponent<TextMesh>();
        t.transform.SetParent(parent);
        t.transform.position = pos;
        t.transform.rotation = Quaternion.Euler(0, yaw, 0);
        t.font = font;
        t.text = text;
        t.fontSize = fontSize;
        t.characterSize = 1f;
        t.lineSpacing = 0.9f;
        t.anchor = TextAnchor.MiddleCenter;
        t.alignment = TextAlignment.Center;
        t.color = color;
        t.GetComponent<MeshRenderer>().sharedMaterial = SignTextMaterial();
        t.gameObject.AddComponent<SignText>();

        // Size of the text at characterSize 1, from the font's glyph metrics. TextMesh draws
        // 1 font pixel as 0.1 units × characterSize.
        var lines = text.Split('\n');
        font.RequestCharactersInTexture(text, fontSize);
        float widest = 0;
        foreach (var line in lines)
        {
            float w = 0;
            foreach (char ch in line)
                if (font.GetCharacterInfo(ch, out var info, fontSize)) w += info.advance;
            widest = Mathf.Max(widest, w);
        }
        float textW = widest * 0.1f;
        float textH = lines.Length * fontSize * 0.9f * 0.1f;
        if (textW > 0 && textH > 0)
            t.characterSize = Mathf.Min(maxWidth / textW, maxHeight / textH);
    }

    // A glowing lightbox panel. `normal` is the direction the readable face points (±X or ±Z).
    static void BacklitSign(Transform parent, Vector3 center, Vector3 normal, float width, float height, string text, Color color, bool twoSided = false)
    {
        var sign = new GameObject($"Sign {text.Replace('\n', ' ')}").transform;
        sign.SetParent(parent);
        sign.position = center;

        bool alongX = Mathf.Abs(normal.z) > 0.5f; // panel spans X when it faces ±Z
        float thick = 0.06f * Scale;
        Vector3 Size(float w, float h, float d) => alongX ? new Vector3(w, h, d) : new Vector3(d, h, w);

        Box("Frame", sign, center, Size(width + 0.06f * Scale, height + 0.06f * Scale, thick), GetMaterial("SignFrame", new Color(0.08f, 0.08f, 0.09f)));
        var glow = StoreAtmosphere.Emissive($"SignGlow_{ColorUtility.ToHtmlStringRGB(color)}", color, 1.8f);

        foreach (var n in twoSided ? new[] { normal, -normal } : new[] { normal })
        {
            Box("Panel", sign, center + n * (thick / 2 + 0.005f * Scale), Size(width, height, 0.01f * Scale), glow);
            // Text yaw so it reads when viewed from the side `n` points to.
            float yaw = Mathf.Atan2(-n.x, -n.z) * Mathf.Rad2Deg;
            MakeText(sign, center + n * (thick / 2 + 0.02f * Scale), yaw, text, width * 0.88f, height * 0.72f, Color.white);
        }
    }

    // ---------- Drop ceiling ----------

    static Material CeilingTiles(float width, float depth)
    {
        const string texPath = MatFolder + "/CeilingTiles.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
        {
            const int size = 256, half = size / 2;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            var rng = new System.Random(5);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Fissured mineral-fibre tiles in a dark metal T-grid.
                float n = 0.74f + 0.05f * (float)rng.NextDouble() - ((float)rng.NextDouble() > 0.96f ? 0.12f : 0f);
                var c = new Color(n, n, n * 0.98f);
                int gx = x % half, gy = y % half;
                if (gx < 3 || gy < 3) c = new Color(0.32f, 0.33f, 0.33f);
                tex.SetPixel(x, y, c);
            }
            System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath);
        }
        var mat = GetMaterial("CeilingTiles", Color.white);
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        float tile = 0.6f * Scale; // 60 cm tiles, 2×2 per texture
        mat.SetTextureScale("_BaseMap", new Vector2(width / (2 * tile), depth / (2 * tile)));
        mat.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------- Coolers ----------

    // A run of glass-door coolers against a wall, glowing from inside and packed with stock.
    // `dir` = +1 for a wall at +Z (coolers face -Z), -1 for a wall at -Z.
    static void BuildCoolerRun(Transform parent, float x0, float x1, float wallZ, int dir,
        (string model, float size)[] stock, string label, Color signColor, int seed)
    {
        var run = new GameObject($"Coolers {label}").transform;
        run.SetParent(parent);
        var rng = new System.Random(seed);
        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        var body = GetMaterial("CoolerBody", new Color(0.12f, 0.13f, 0.14f));
        var shelfMat = GetMaterial("CoolerShelf", new Color(0.7f, 0.72f, 0.74f));
        var glow = StoreAtmosphere.Emissive("CoolerGlow", new Color(0.8f, 0.95f, 1f), 1.3f);
        var glass = StoreAtmosphere.Glass("CoolerGlass", new Color(0.75f, 0.9f, 1f, 0.12f));

        float len = x1 - x0, mid = (x0 + x1) / 2;
        float back = wallZ - dir * 0.02f * Scale;
        float front = wallZ - dir * CoolerDepth;
        float zc = (back + front) / 2;
        float baseH = 0.16f * Scale, topH = 0.1f * Scale;

        Box("Back Glow", run, new Vector3(mid, CoolerHeight / 2, back - dir * 0.02f * Scale), new Vector3(len, CoolerHeight, 0.04f * Scale), glow);
        Box("Base", run, new Vector3(mid, baseH / 2, zc), new Vector3(len, baseH, CoolerDepth), body);
        Box("Top", run, new Vector3(mid, CoolerHeight - topH / 2, zc), new Vector3(len, topH, CoolerDepth), body);
        Box("Header", run, new Vector3(mid, CoolerHeight + 0.2f * Scale, zc), new Vector3(len, 0.4f * Scale, CoolerDepth), body);
        BacklitSign(run, new Vector3(mid, CoolerHeight + 0.2f * Scale, front - dir * 0.04f * Scale), new Vector3(0, 0, -dir),
            Mathf.Min(len * 0.6f, 8f * Scale), 0.3f * Scale, label, signColor);
        Box("Glass", run, new Vector3(mid, (baseH + CoolerHeight - topH) / 2, front), new Vector3(len, CoolerHeight - baseH - topH, 0.01f * Scale), glass);

        int doors = Mathf.Max(1, Mathf.RoundToInt(len / CoolerDoorWidth));
        for (int d = 0; d <= doors; d++)
        {
            float x = x0 + d * len / doors;
            Box("Mullion", run, new Vector3(x, CoolerHeight / 2, zc), new Vector3(0.04f * Scale, CoolerHeight, CoolerDepth), body);
            if (d < doors && d % 3 == 1)
            {
                var l = new GameObject("Cooler Light").AddComponent<Light>();
                l.transform.SetParent(run);
                l.transform.position = new Vector3(x + len / doors / 2, CoolerHeight * 0.85f, zc);
                l.type = LightType.Point;
                l.color = new Color(0.85f, 0.95f, 1f);
                l.range = 1.6f * Scale;
                l.intensity = 0.25f * l.range * l.range;
            }
        }

        var types = Measure(stock);
        if (types.Count == 0) return;
        var products = new GameObject("Products").transform;
        products.SetParent(run);

        const int levels = 5;
        float levelH = (CoolerHeight - baseH - topH) / levels;
        float depth = CoolerDepth * 0.85f;
        for (int k = 0; k < levels; k++)
        {
            float y = baseH + k * levelH;
            if (k > 0)
                Box("Shelf", run, new Vector3(mid, y - 0.008f * Scale, zc), new Vector3(len, 0.016f * Scale, depth), shelfMat);
            for (int d = 0; d < doors; d++)
            {
                float dx0 = x0 + d * len / doors + 0.03f * Scale, dx1 = x0 + (d + 1) * len / doors - 0.03f * Scale;
                FillRowAlongX(products, types, rng, Rand, dx0, dx1, y, levelH - 0.02f * Scale, front + dir * 0.04f * Scale, depth, dir);
            }
        }
    }

    // Packs one shelf that runs along X: facings side by side, 1–4 rows deep going towards +dir Z.
    static void FillRowAlongX(Transform parent, List<ProductType> types, System.Random rng, System.Func<float, float, float> rand,
        float x0, float x1, float y, float room, float zFront, float depth, int dir)
    {
        float gap = 0.008f * Scale;
        float x = x0;
        while (x < x1)
        {
            var t = types[rng.Next(types.Count)];
            var b = t.bounds; // long side along Z at t.yaw; turned 90° below so it runs along X
            float s = Mathf.Min(t.size / Mathf.Max(b.x, b.y, b.z), 0.85f * room / b.y, 0.95f * depth / b.x);
            float w = b.z * s, dz = b.x * s;
            if (x + w > x1) break;
            int rows = Mathf.Clamp(Mathf.FloorToInt(depth / (dz + gap)), 1, 4);
            int facings = rng.Next(2, 6);
            for (int f = 0; f < facings && x + w <= x1; f++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (rng.NextDouble() < 0.04) continue;
                    var p = Spawn(t.prefab, parent, t.yaw + 90 + (dir > 0 ? 180 : 0) + rand(-4f, 4f), s);
                    PlaceOnFloor(p, new Vector3(x + w / 2, y, zFront + dir * (dz / 2 + r * (dz + gap))));
                }
                x += w + gap;
            }
            x += gap * 2;
        }
    }

    // ---------- Endcaps & displays ----------

    // Stack of boxes with an open box on top filled with one product, like a promo bin.
    static void DisplayBin(Transform parent, Vector3 at, List<ProductType> types, System.Random rng, int stackHeight)
    {
        var closed = AssetDatabase.LoadAssetAtPath<GameObject>($"{KenneyFolder}/cardboardBoxClosed.fbx");
        var open = AssetDatabase.LoadAssetAtPath<GameObject>($"{KenneyFolder}/cardboardBoxOpen.fbx");
        if (closed == null || open == null || types.Count == 0) return;
        float box = 0.5f * Scale;
        float y = at.y;
        for (int h = 0; h < stackHeight; h++)
        {
            var b = SpawnSized(h == stackHeight - 1 ? open : closed, parent, box, (float)rng.NextDouble() * 6f - 3f);
            PlaceOnFloor(b, new Vector3(at.x, y, at.z));
            AddBoxColliders(b);
            var bb = GetBounds(b);
            y = bb.max.y;
            if (h < stackHeight - 1) continue;

            // Heap of product in the open box: a packed layer sunk into the box, a smaller layer on top.
            var t = types[rng.Next(types.Count)];
            float inner = bb.size.x * 0.8f;
            float s = Mathf.Min(t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z), inner / 2.2f / Mathf.Max(t.bounds.x, t.bounds.z));
            float step = Mathf.Max(t.bounds.x, t.bounds.z) * s * 1.05f;
            int n = Mathf.Max(1, Mathf.FloorToInt(inner / step));
            float baseY = bb.max.y - bb.size.y * 0.35f;
            for (int layer = 0; layer < 2; layer++)
            for (int i = 0; i < n - layer; i++)
            for (int j = 0; j < n - layer; j++)
            {
                float ox = (i - (n - layer - 1) / 2f) * step, oz = (j - (n - layer - 1) / 2f) * step;
                var p = Spawn(t.prefab, parent, t.yaw + (float)rng.NextDouble() * 360f, s);
                PlaceOnFloor(p, new Vector3(bb.center.x + ox, baseY + layer * t.bounds.y * s * 0.8f, bb.center.z + oz));
            }
        }
    }

    // Promo bins against the open end of every aisle (the end without a blockade).
    static void BuildEndcaps(Transform store, List<Transform> aisles, ShelfInfo shelf, float aisleLength)
    {
        var root = new GameObject("Endcaps").transform;
        root.SetParent(store);
        var rng = new System.Random(23);
        for (int i = 0; i < aisles.Count; i++)
        {
            bool blockedSouth = i % 2 == 0;
            float zEnd = blockedSouth ? aisleLength / 2 + 0.32f * Scale : -aisleLength / 2 - 0.32f * Scale;
            var types = Measure(Stock[Mathf.Min(i, Stock.Length - 1)]);
            foreach (float off in new[] { -0.25f, 0.25f })
                DisplayBin(root, new Vector3(aisles[i].position.x + off * shelf.gondolaDepth, 0, zEnd), types, rng, rng.Next(1, 3));
        }
    }

    static readonly List<GameObject> checkoutCounters = new List<GameObject>();

    static GameObject LoadFurniture(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{KenneyFolder}/{name}.fbx");

    // Puts `count` products on whatever upward surface `target` has near (x, z), e.g. a counter top.
    static void SetOnTop(GameObject target, Transform parent, List<ProductType> types, System.Random rng, Vector3 around, float spread, int count)
    {
        var up = UpwardTriangles(target);
        for (int i = 0; i < count; i++)
        {
            float x = around.x + ((float)rng.NextDouble() - 0.5f) * spread;
            float z = around.z + ((float)rng.NextDouble() - 0.5f) * spread * 0.5f;
            var tops = SurfacesAt(up, x, z);
            if (tops.Count == 0) continue;
            var t = types[rng.Next(types.Count)];
            var p = Spawn(t.prefab, parent, t.yaw + (float)rng.NextDouble() * 360f, t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z));
            PlaceOnFloor(p, new Vector3(x, tops[tops.Count - 1], z));
            AddBoxColliders(p);
        }
    }

    // Fills every board of a piece of furniture (a rack, a bookcase) with products along X.
    static void StockFurniture(GameObject rack, Transform parent, List<ProductType> types, System.Random rng)
    {
        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        var b = GetBounds(rack);
        var levels = SurfacesAt(UpwardTriangles(rack), b.center.x, b.center.z);
        if (levels.Count == 0) return;
        levels.Add(levels[levels.Count - 1] + 0.25f * Scale); // the top surface gets a short layer too
        for (int i = 0; i < levels.Count - 1; i++)
            FillRowAlongX(parent, types, rng, Rand, b.min.x + 0.03f * Scale, b.max.x - 0.03f * Scale,
                levels[i], levels[i + 1] - levels[i] - 0.02f * Scale, b.min.z + 0.02f * Scale, b.size.z * 0.85f, +1);
    }

    // Big lightbox menu board like the ones over a deli counter: a title across the top and a row of
    // real products standing on a ledge underneath, lit by the panel.
    static void MenuBoard(Transform parent, Vector3 center, Vector3 normal, float width, float height, string title, Color color, List<ProductType> items, System.Random rng)
    {
        BacklitSign(parent, center + Vector3.up * height * 0.25f, normal, width, height * 0.45f, title, color);
        var shelfMat = GetMaterial("SignFrame", new Color(0.08f, 0.08f, 0.09f));
        var tint = StoreAtmosphere.Emissive($"MenuGlow_{ColorUtility.ToHtmlStringRGB(color)}", Color.Lerp(color, Color.white, 0.6f), 1.2f);
        bool alongX = Mathf.Abs(normal.z) > 0.5f;
        Vector3 Span(float w, float h, float d) => alongX ? new Vector3(w, h, d) : new Vector3(d, h, w);
        Vector3 along = alongX ? Vector3.right : Vector3.forward;

        // Back panel for the product row and a ledge for them to stand on.
        Vector3 lowCenter = center - Vector3.up * height * 0.23f;
        Box("Menu Panel", parent, lowCenter, Span(width, height * 0.5f, 0.04f * Scale), tint);
        float ledgeY = lowCenter.y - height * 0.25f;
        Box("Menu Ledge", parent, new Vector3(center.x, ledgeY - 0.01f * Scale, center.z) + normal * 0.12f * Scale,
            Span(width, 0.02f * Scale, 0.22f * Scale), shelfMat);
        if (items.Count == 0) return;
        int n = Mathf.Max(3, Mathf.RoundToInt(width / (0.6f * Scale)));
        for (int i = 0; i < n; i++)
        {
            var t = items[i % items.Count];
            float slot = width / n;
            float s = Mathf.Min(slot * 0.7f / Mathf.Max(t.bounds.x, t.bounds.z), height * 0.4f / t.bounds.y);
            float yaw = Mathf.Atan2(normal.x, normal.z) * Mathf.Rad2Deg + (alongX ? 0 : 90);
            var p = Spawn(t.prefab, parent, t.yaw + 90 + yaw, s);
            PlaceOnFloor(p, new Vector3(center.x, ledgeY, center.z) + along * (-width / 2 + (i + 0.5f) * slot) + normal * 0.12f * Scale);
        }
    }

    // The store front: lane furniture, impulse racks, bags and groceries on the belts, plants,
    // bins, a doormat, a bench, big menu boards and posters.
    static void DressCheckout(Transform store, float left, float right, float depth)
    {
        var root = new GameObject("Checkout Displays").transform;
        root.SetParent(store);
        var rng = new System.Random(31);
        var produce = Measure(Stock[3]);
        var snacks = Measure(new[] { ("k:candy-bar", 0.15f), ("k:chocolate", 0.15f), ("chocolate", 0.15f), ("k:cookie", 0.08f),
            ("k:donut-sprinkles", 0.1f), ("k:soda-can", 0.12f), ("k:candy-bar-wrapper", 0.15f) });
        var groceries = Measure(Stock[0].Concat(Stock[2]).ToArray());
        var bagTypes = Measure(new[] { ("k:bag", 0.45f) });
        var pole = GetMaterial("SignFrame", new Color(0.08f, 0.08f, 0.09f));

        // Each lane: candy rack at its entrance, lane light on the counter end, groceries on the belt,
        // a couple of filled bags at the bagging end.
        var rack = LoadFurniture("bookcaseOpenLow");
        int lane = 1;
        foreach (var counter in checkoutCounters)
        {
            var b = GetBounds(counter);
            if (rack != null)
            {
                var r = PlaceProp(rack, root, 1.2f, 1.1f, alongX: true, new Vector3(b.max.x + 0.75f * Scale, 0, b.center.z));
                StockFurniture(r, root, snacks, rng);
            }
            float poleH = CoolerHeight * 0.85f;
            var poleAt = new Vector3(b.max.x - 0.08f * Scale, b.max.y, b.center.z);
            Box("Lane Pole", root, poleAt + Vector3.up * (poleH - b.max.y) / 2, new Vector3(0.04f * Scale, poleH - b.max.y, 0.04f * Scale), pole);
            BacklitSign(root, new Vector3(poleAt.x, poleH + 0.15f * Scale, poleAt.z), Vector3.right, 0.35f * Scale, 0.3f * Scale,
                (lane++).ToString(), new Color(0.1f, 0.55f, 0.3f), twoSided: true);
            if (groceries.Count > 0) SetOnTop(counter, root, groceries, rng, new Vector3(b.center.x + b.size.x * 0.15f, 0, b.center.z), b.size.x * 0.4f, rng.Next(3, 7));
            if (bagTypes.Count > 0) SetOnTop(counter, root, bagTypes, rng, new Vector3(b.min.x + b.size.x * 0.12f, 0, b.center.z), b.size.x * 0.15f, rng.Next(1, 3));
        }

        float laneX = left + CheckoutWidth * 0.6f;
        BacklitSign(root, new Vector3(laneX, WallHeight - 1.2f * Scale, 0), Vector3.right, 4f * Scale, 0.55f * Scale, "CHECKOUT", new Color(0.85f, 0.15f, 0.12f), twoSided: true);

        // Produce bins north of the door, snack bins at the ends of the lane block.
        for (int i = 0; i < 3; i++)
            DisplayBin(root, new Vector3(left + CheckoutWidth * 0.25f, 0, (1.4f + i * 0.7f) * Scale + DoorWidth / 2), produce, rng, 1);
        foreach (float z in new[] { -4.4f, 4.4f })
            DisplayBin(root, new Vector3(left + CheckoutWidth * 0.75f, 0, z * Scale), snacks, rng, 2);

        // Furniture around the door.
        void Prop(string name, float height, float maxLen, bool alongX, Vector3 at)
        {
            var prefab = LoadFurniture(name);
            if (prefab != null) PlaceProp(prefab, root, height, maxLen, alongX, at);
        }
        Prop("rugDoormat", 10f, 2.6f, alongX: false, new Vector3(left + 0.9f * Scale, 0, 0));
        Prop("trashcan", 0.8f, 0.6f, alongX: true, new Vector3(left + 0.45f * Scale, 0, DoorWidth / 2 + 0.5f * Scale));
        Prop("trashcan", 0.8f, 0.6f, alongX: true, new Vector3(left + CheckoutWidth * 0.95f, 0, -6f * Scale));
        Prop("bench", 0.45f, 1.6f, alongX: false, new Vector3(left + 0.4f * Scale, 0, DoorWidth / 2 + 3.6f * Scale));
        Prop("pottedPlant", 1.4f, 0.8f, alongX: true, new Vector3(left + 0.5f * Scale, 0, DoorWidth / 2 + 1.2f * Scale));
        Prop("pottedPlant", 1.4f, 0.8f, alongX: true, new Vector3(left + 0.5f * Scale, 0, depth / 2 - 0.6f * Scale));
        Prop("plantSmall2", 0.8f, 0.6f, alongX: true, new Vector3(left + CheckoutWidth * 0.95f, 0, 6.2f * Scale));
        Prop("plantSmall1", 0.8f, 0.6f, alongX: true, new Vector3(left + CheckoutWidth * 0.95f, 0, -6.8f * Scale));

        // Menu boards: deli/bakery over the door, drinks and frozen high on the end walls.
        var bakery = Measure(new[] { ("Bread", 0.28f), ("k:croissant", 0.14f), ("k:cake", 0.25f), ("coffee", 0.18f), ("k:donut-chocolate", 0.1f), ("k:pie", 0.25f) });
        MenuBoard(root, new Vector3(left + 0.08f * Scale, DoorHeight + 0.85f * Scale, 0), Vector3.right, 6f * Scale, 1.4f * Scale,
            "BAKERY  •  DELI  •  HOT COFFEE", new Color(0.9f, 0.5f, 0.1f), bakery, rng);
        float midAisles = (left + CheckoutWidth + right) / 2;
        MenuBoard(root, new Vector3(midAisles, CoolerHeight + 1.35f * Scale, depth / 2 - 0.08f * Scale), Vector3.back, 9f * Scale, 1.3f * Scale,
            "ICE COLD DRINKS", new Color(0.1f, 0.4f, 0.9f), Measure(CoolerDrinks), rng);
        MenuBoard(root, new Vector3(midAisles, CoolerHeight + 1.35f * Scale, -depth / 2 + 0.08f * Scale), Vector3.forward, 9f * Scale, 1.3f * Scale,
            "FROZEN TREATS", new Color(0.85f, 0.12f, 0.2f), Measure(CoolerFrozen), rng);

        // Sale posters on the east wall, under the FRESH MARKET sign.
        string[] deals = { "SALE", "2 FOR $5", "NEW!", "FRESH DAILY" };
        Color[] colors = { new Color(0.9f, 0.1f, 0.1f), new Color(0.95f, 0.75f, 0.1f), new Color(0.1f, 0.6f, 0.3f), new Color(0.2f, 0.4f, 0.9f) };
        for (int i = 0; i < deals.Length; i++)
            BacklitSign(root, new Vector3(right - 0.05f * Scale, 1.9f * Scale, (-4.5f + i * 3f) * Scale), Vector3.left,
                1.6f * Scale, 0.9f * Scale, deals[i], colors[i]);
    }
}

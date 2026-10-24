using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Level One for GroceryStoreBuilder: the first hallway (walkway 0). Henry wakes up in a cereal box
// lying on its side, then works through three puzzles built from store items, with tutorial cards
// along the way, and a cutscene at the far end where he rounds the corner into the next hall.
// Also sets up the HUD pieces every level uses (tutorial card, letterbox bars, fader) and the
// checkpoints at each turning point of the S-shaped route.
//
//   z ─────────────────────────────────────────────────────────────────────────────────────►
//   [wake-up box] ... [matchbox → ruler on a can → fallen cereal boxes] ... [carry box → box ledge]
//   ... [carry box + spatula lever on a jar → cutting-board ramp → cracker-box wall] ... corner
static partial class GroceryStoreBuilder
{
    static (RectTransform top, RectTransform bottom) cinematicBars;

    static void BuildLevelOne(Transform store, List<float> walkways, float aisleLength, float depth, Transform spawn,
        out Transform wakeInside, out Transform wakeOutside)
    {
        var level = new GameObject("Level One").transform;
        level.SetParent(store);

        float xc = walkways[0], w = WalkwayWidth;
        float start = -aisleLength / 2;

        BuildTutorialHud();
        cinematicBars = BuildCinematicBars();

        WakeUpBox(level, spawn.position, out wakeInside, out wakeOutside);
        TutorialZoneAt(level, new Vector3(xc, 5, spawn.position.z + 14f), new Vector3(w, 10, 24f), "walk", "WHERE AM I?",
            "Walk with WASD, look with the mouse. Hold Shift to run and press Space to jump.", "WASD", "SHIFT", "SPACE");

        BuildSeesawPuzzle(level, xc, w, start + aisleLength * 0.3f);
        BuildStepPuzzle(level, xc, w, start + aisleLength * 0.52f);
        BuildRampPuzzle(level, xc, w, start + aisleLength * 0.84f);
        ScatterLitter(level, xc, w, start + 12f, start + aisleLength - 6f, 21);
        ShelfGuards(level, xc, start, start + aisleLength);

        float northCorridor = (aisleLength / 2 + depth / 2 - CoolerDepth) / 2;
        float southCorridor = -northCorridor;

        // End of the first hall: a smooth curve round the corner into walkway 1, ending facing south.
        var cut = new GameObject("Corner Cutscene").transform;
        cut.SetParent(level);
        cut.position = new Vector3(xc, 5, aisleLength / 2 + 1f);
        var trigger = cut.gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(w, 10, 4);
        float mid = (xc + walkways[1]) / 2;
        var points = new[]
        {
            Waypoint(cut, "Into Corridor", new Vector3(xc - w * 0.1f, 1, northCorridor - 2f), 0),
            Waypoint(cut, "Across", new Vector3(mid, 1, northCorridor + 1f), -90),
            Waypoint(cut, "Round", new Vector3(walkways[1] + w * 0.1f, 1, northCorridor - 3f), -150),
            Waypoint(cut, "Next Hall", new Vector3(walkways[1], 1, aisleLength / 2 + 2f), 180),
        };
        cut.gameObject.AddComponent<CornerCutscene>().Setup(points, cinematicBars.top, cinematicBars.bottom);

        // Plain checkpoints at the later turns: walkway 1 turns at the south end, 2 at the north, ...
        for (int k = 1; k < walkways.Count - 1; k++)
        {
            bool south = k % 2 == 1;
            float endZ = south ? -aisleLength / 2 : aisleLength / 2;
            var cp = new GameObject($"Checkpoint {k}").transform;
            cp.SetParent(level);
            cp.position = new Vector3(walkways[k], 5, endZ + (south ? -1f : 1f));
            var box = cp.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(WalkwayWidth, 10, 4);
            var respawn = Waypoint(cp, "Respawn", new Vector3(walkways[k], 1, south ? southCorridor : northCorridor), -90);
            cp.gameObject.AddComponent<Checkpoint>().Setup(respawn);
        }
    }

    static Transform Waypoint(Transform parent, string name, Vector3 pos, float yaw)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent);
        t.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        return t;
    }

    static void CheckpointAt(Transform parent, string name, Vector3 at, Vector3 size, float yaw)
    {
        var cp = new GameObject(name).transform;
        cp.SetParent(parent);
        cp.position = at + Vector3.up * size.y / 2;
        var box = cp.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        cp.gameObject.AddComponent<Checkpoint>().Setup(Waypoint(cp, "Respawn", at + Vector3.up, yaw));
    }

    static void Solid(Transform parent, string name, Vector3 center, Vector3 size)
    {
        var s = new GameObject(name).AddComponent<BoxCollider>();
        s.transform.SetParent(parent);
        s.transform.position = center;
        s.size = size;
    }

    // ---------- Cereal boxes ----------

    const float CerealW = 0.32f * Scale, CerealH = 0.45f * Scale, CerealD = 0.1f * Scale;

    static readonly (string brand, string tag, CerealArt.Brand art, string icon)[] Cereals =
    {
        ("CRUNCHY\nO'S", "WHOLE GRAIN", new CerealArt.Brand { main = new Color(0.9f, 0.18f, 0.12f), accent = new Color(1f, 0.82f, 0.15f), deep = new Color(0.55f, 0.05f, 0.05f), piece = CerealArt.Piece.Ring, pieceColor = new Color(0.95f, 0.72f, 0.3f) }, "breakfast"),
        ("HONEY\nFLAKES", "GOLDEN TOASTED", new CerealArt.Brand { main = new Color(1f, 0.68f, 0.15f), accent = new Color(0.45f, 0.22f, 0.08f), deep = new Color(0.75f, 0.35f, 0.05f), piece = CerealArt.Piece.Flake, pieceColor = new Color(0.98f, 0.78f, 0.35f) }, "bakery"),
        ("CHOCO\nPUFFS", "DOUBLE CHOCOLATE", new CerealArt.Brand { main = new Color(0.45f, 0.24f, 0.12f), accent = new Color(1f, 0.85f, 0.55f), deep = new Color(0.2f, 0.09f, 0.04f), piece = CerealArt.Piece.Puff, pieceColor = new Color(0.32f, 0.16f, 0.08f) }, "cookie"),
        ("FROSTY\nSTARS", "FROSTED", new CerealArt.Brand { main = new Color(0.2f, 0.45f, 0.9f), accent = new Color(0.95f, 0.97f, 1f), deep = new Color(0.06f, 0.15f, 0.45f), piece = CerealArt.Piece.Star, pieceColor = new Color(0.98f, 0.92f, 0.55f) }, "star"),
        ("FRUITY\nRINGS", "NATURAL FRUIT FLAVORS", new CerealArt.Brand { main = new Color(0.58f, 0.25f, 0.8f), accent = new Color(0.45f, 0.92f, 0.45f), deep = new Color(0.25f, 0.08f, 0.45f), piece = CerealArt.Piece.Loop, pieceColor = new Color(1f, 0.45f, 0.5f) }, "apple"),
        ("CRISPY\nCRACKERS", "BAKED NOT FRIED", new CerealArt.Brand { main = new Color(0.95f, 0.85f, 0.55f), accent = new Color(0.8f, 0.15f, 0.15f), deep = new Color(0.7f, 0.55f, 0.25f), piece = CerealArt.Piece.Flake, pieceColor = new Color(0.9f, 0.7f, 0.35f) }, "bakery"),
    };

    enum CerealPart { Front, Side, SideB, Top, Body, Cardboard }

    static Material CerealMat(int style, CerealPart part)
    {
        var s = Cereals[((style % Cereals.Length) + Cereals.Length) % Cereals.Length];
        if (part == CerealPart.Body)
            return GetMaterial($"Cereal {style} Body Color", s.art.main);
        string path = part == CerealPart.Cardboard
            ? $"{MatFolder}/CerealArt/Cardboard.mat"
            : $"{MatFolder}/CerealArt/Cereal_{style}_{part}.mat";
        var tex = part switch
        {
            CerealPart.Front => CerealArt.Front(style, s.art),
            CerealPart.Side => CerealArt.Side(style, s.art),
            CerealPart.SideB => CerealArt.SideB(style, s.art),
            CerealPart.Top => CerealArt.Top(style, s.art),
            _ => CerealArt.Cardboard(),
        };
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetTexture("_BaseMap", tex);
        mat.color = Color.white;
        mat.SetFloat("_Smoothness", 0.28f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // A family-size cereal box, built upright at the origin (front facing -Z): a printed body plus
    // painted front and back art panels with the brand name in crisp 3D type over the logo panel.
    static GameObject CerealBox(Transform parent, int style, float sizeScale = 1f)
    {
        style = ((style % Cereals.Length) + Cereals.Length) % Cereals.Length;
        var s = Cereals[style];
        var root = new GameObject($"Cereal Box ({s.brand.Replace('\n', ' ')})");
        root.transform.SetParent(parent);
        float W = CerealW * sizeScale, H = CerealH * sizeScale, D = CerealD * sizeScale;
        Box("Box", root.transform, Vector3.zero, new Vector3(W, H, D), CerealMat(style, CerealPart.Body));
        var frontMat = CerealMat(style, CerealPart.Front);
        // Narrow sides: nutrition facts + barcode on one, ingredients on the other; flaps top and bottom.
        ArtQuad(root.transform, "Nutrition Side", new Vector3(W / 2 + 0.02f, 0, 0), Quaternion.Euler(0, -90, 0), new Vector2(D, H), CerealMat(style, CerealPart.Side));
        ArtQuad(root.transform, "Ingredients Side", new Vector3(-W / 2 - 0.02f, 0, 0), Quaternion.Euler(0, 90, 0), new Vector2(D, H), CerealMat(style, CerealPart.SideB));
        ArtQuad(root.transform, "Top Flaps", new Vector3(0, H / 2 + 0.02f, 0), Quaternion.Euler(90, 0, 0), new Vector2(W, D), CerealMat(style, CerealPart.Top));
        ArtQuad(root.transform, "Bottom Flaps", new Vector3(0, -H / 2 - 0.02f, 0), Quaternion.Euler(-90, 0, 0), new Vector2(W, D), CerealMat(style, CerealPart.Top));
        foreach (int face in new[] { -1, 1 })
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Front Art";
            q.transform.SetParent(root.transform);
            q.transform.SetPositionAndRotation(new Vector3(0, 0, face * (D / 2 + 0.02f)), Quaternion.Euler(0, face < 0 ? 0 : 180, 0));
            q.transform.localScale = new Vector3(W, H, 1);
            q.GetComponent<Renderer>().sharedMaterial = frontMat;
            Object.DestroyImmediate(q.GetComponent<Collider>());

            float yaw = face < 0 ? 0 : 180;
            var outward = new Vector3(0, 0, face * (D / 2 + 0.06f));
            var right = face < 0 ? 1f : -1f; // the art's +x as seen from this side
            MakeText(root.transform, new Vector3(0, H * 0.3f, 0) + outward, yaw, s.brand, W * 0.8f, H * 0.2f, Color.white);
            MakeText(root.transform, new Vector3(0, H * 0.165f, 0) + outward, yaw, s.tag, W * 0.6f, H * 0.035f, new Color(1f, 1f, 0.85f));
            MakeText(root.transform, new Vector3(right * W * 0.324f, H * 0.083f, 0) + outward, yaw, "NEW!", W * 0.16f, H * 0.04f, Color.white);
            MakeText(root.transform, new Vector3(0, -H * 0.462f, 0) + outward, yaw, "FAMILY SIZE  •  NET WT 18 OZ (510g)", W * 0.8f, H * 0.03f, Color.white);
        }
        foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
        return root;
    }

    static void ArtQuad(Transform parent, string name, Vector3 pos, Quaternion rot, Vector2 size, Material mat)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name;
        q.transform.SetParent(parent);
        q.transform.SetPositionAndRotation(pos, rot);
        q.transform.localScale = new Vector3(size.x, size.y, 1);
        q.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(q.GetComponent<Collider>());
    }

    static GameObject Fallen(Transform parent, int style, Quaternion rotation, Vector3 at)
    {
        var box = CerealBox(parent, style);
        box.transform.rotation = rotation;
        PlaceOnFloor(box, at);
        SettleLater(box, force: true);
        return box;
    }

    // ---------- Wake-up box ----------

    // The box Henry wakes up in: a cereal box lying on its side, open end facing up the aisle.
    // Built as five thin panels so he can actually be inside it.
    static void WakeUpBox(Transform parent, Vector3 spawn, out Transform inside, out Transform outside)
    {
        var root = new GameObject("Wake-up Cereal Box").transform;
        root.SetParent(parent);
        const int style = 0;
        float W = CerealW, H = CerealH, D = CerealD * 1.4f, t = 0.1f; // a little roomier so he fits
        // Final orientation: length H along +Z (open end north), height W, width D.
        var c = new Vector3(spawn.x, W / 2, spawn.z - H / 2 - 1f);
        // Panels are plain cardboard (what you see inside); printed art covers their outer faces.
        var card = CerealMat(style, CerealPart.Cardboard);
        card.SetTextureScale("_BaseMap", new Vector2(3, 3));
        var front = CerealMat(style, CerealPart.Front);
        Box("Floor Panel", root, c + Vector3.down * (W / 2 - t / 2), new Vector3(D, t, H), card);
        Box("Roof Panel", root, c + Vector3.up * (W / 2 - t / 2), new Vector3(D, t, H), card);
        Box("Bottom Panel", root, c + Vector3.back * (H / 2 - t / 2), new Vector3(D, W, t), card);
        // Outside of the roof is the box's nutrition side; outside of the closed end is its bottom flaps.
        ArtQuad(root, "Nutrition Side", c + Vector3.up * (W / 2 + 0.01f), Quaternion.Euler(90, 0, 0), new Vector2(D, H), CerealMat(style, CerealPart.Side));
        ArtQuad(root, "Bottom Flaps", c + Vector3.back * (H / 2 + 0.01f), Quaternion.Euler(0, 0, 90), new Vector2(W, D), CerealMat(style, CerealPart.Top));
        foreach (int s in new[] { -1, 1 })
        {
            Box(s < 0 ? "West Panel" : "East Panel", root, c + Vector3.right * s * (D / 2 - t / 2), new Vector3(t, W, H), card);
            // The printed front art, turned on its side like the box.
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Front Art";
            q.transform.SetParent(root);
            q.transform.position = c + Vector3.right * s * (D / 2 + 0.01f);
            q.transform.rotation = Quaternion.Euler(0, s < 0 ? 90 : -90, 0) * Quaternion.Euler(0, 0, s < 0 ? 90 : -90);
            q.transform.localScale = new Vector3(W, H, 1);
            q.GetComponent<Renderer>().sharedMaterial = front;
            Object.DestroyImmediate(q.GetComponent<Collider>());
        }
        // Open flaps sagging at the open end.
        var flapMat = card;
        var flapL = Box2("Flap", root, c + new Vector3(0, W / 2 - 0.2f, H / 2 + 1.2f), new Vector3(D * 0.9f, t, 2.4f), flapMat);
        flapL.transform.RotateAround(c + new Vector3(0, W / 2, H / 2), Vector3.right, 35f);
        var flapB = Box2("Flap", root, c + new Vector3(0, -W / 2 + 0.05f, H / 2 + 1.2f), new Vector3(D * 0.9f, t, 2.4f), flapMat);
        flapB.transform.RotateAround(c + new Vector3(0, -W / 2, H / 2), Vector3.right, -8f);
        // Spilled cereal around the opening.
        SpillPieces(root, c + new Vector3(0, -W / 2, H / 2 + 3f), 4f, 40);

        inside = Waypoint(root, "Inside", new Vector3(c.x, 1f, c.z - H * 0.15f), 0);
        // Far enough out that the follow camera behind him clears the box.
        outside = Waypoint(root, "Outside", new Vector3(c.x, 1f, c.z + H / 2 + 9f), 0);
    }

    static GameObject Box2(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        Box(name, parent, pos, size, mat);
        return parent.GetChild(parent.childCount - 1).gameObject;
    }

    // Little cereal pieces (tiny donut rings) scattered on the floor. Visual only.
    static void SpillPieces(Transform parent, Vector3 center, float radius, int count)
    {
        var ring = LoadModel("k:donut");
        if (ring == null) return;
        var rng = new System.Random((int)(center.x * 13 + center.z * 7));
        var probe = Spawn(ring, null, 0, 1);
        var pb = GetBounds(probe);
        Object.DestroyImmediate(probe);
        float s = 0.018f * Scale / Mathf.Max(pb.size.x, pb.size.z);
        for (int i = 0; i < count; i++)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2, r = radius * Mathf.Sqrt((float)rng.NextDouble());
            var p = Spawn(ring, parent, (float)rng.NextDouble() * 360f, s);
            if (rng.NextDouble() < 0.3) p.transform.rotation = Quaternion.Euler(70, 0, 0) * p.transform.rotation;
            PlaceOnFloor(p, center + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            SettleLater(p);
        }
    }

    // ---------- Litter ----------

    // Wrappers, crushed cans, cups, broken plates, chopsticks... strewn along the hall, mostly near
    // the shelves so the middle stays clear. Visual only (no colliders).
    static void ScatterLitter(Transform parent, float xc, float w, float z0, float z1, int seed)
    {
        var root = new GameObject("Litter").transform;
        root.SetParent(parent);
        var items = Measure(new[]
        {
            ("k:soda-can-crushed", 0.1f), ("k:candy-bar-wrapper", 0.15f), ("k:chocolate-wrapper", 0.15f), ("k:styrofoam", 0.1f),
            ("k:fries-empty", 0.12f), ("k:plate-broken", 0.2f), ("k:chopstick", 0.22f), ("k:popsicle-stick", 0.1f),
            ("k:bag-flat", 0.16f), ("k:cup", 0.1f), ("k:soda-can", 0.12f), ("BeanCan", 0.11f), ("k:cookie", 0.07f),
        });
        if (items.Count == 0) return;
        var rng = new System.Random(seed);
        var receipt = GetMaterial("Receipt", new Color(0.96f, 0.95f, 0.9f));
        for (int i = 0; i < 70; i++)
        {
            float side = rng.Next(2) == 0 ? -1 : 1;
            float edge = (float)rng.NextDouble();
            float x = xc + side * (w / 2 - 1f - edge * edge * w * 0.4f);
            float z = Mathf.Lerp(z0, z1, (float)rng.NextDouble());
            if (rng.NextDouble() < 0.18)
            {
                // A crumpled receipt.
                var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
                r.name = "Receipt";
                r.transform.SetParent(root);
                r.transform.localScale = new Vector3(0.06f * Scale, 0.004f * Scale, 0.16f * Scale);
                r.transform.SetPositionAndRotation(new Vector3(x, 0.05f, z), Quaternion.Euler((float)rng.NextDouble() * 8f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 8f));
                r.GetComponent<Renderer>().sharedMaterial = receipt;
                Object.DestroyImmediate(r.GetComponent<Collider>());
                continue;
            }
            var t = items[rng.Next(items.Count)];
            var p = Spawn(t.prefab, root, (float)rng.NextDouble() * 360f, t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z));
            // Only round things (cans, cups, sticks) roll onto their sides; plates, bags and wrappers lie flat.
            string n = t.prefab.name;
            bool rolls = n.Contains("can") || n.Contains("Can") || n.Contains("cup") || n.Contains("stick") || n.Contains("chopstick");
            if (rolls && rng.NextDouble() < 0.7) p.transform.rotation = Quaternion.Euler(90, (float)rng.NextDouble() * 360f, 0) * p.transform.rotation;
            PlaceOnFloor(p, new Vector3(x, 0, z));
            SettleLater(p);
        }
    }

    // ---------- Puzzle 1: the ruler and the cereal boxes ----------

    // A handful of family-size cereal boxes have toppled across the hall. Too tall to jump, and
    // every gap is sealed up to the ceiling. A wooden ruler balances on a can, its long end tucked
    // right under the middle box. Hop up via a matchbox onto the short end; Henry's weight tips the
    // ruler and the box rides up on its tip, high enough to walk underneath.
    static void BuildSeesawPuzzle(Transform level, float xc, float w, float barrierZ)
    {
        var root = new GameObject("Puzzle 1 - Ruler").transform;
        root.SetParent(level);
        float xL = xc - w / 2, xR = xc + w / 2;

        // The gate box lies on its side, square to the hall, so the ruler fits under it.
        var gate = CerealBox(root, 0);
        gate.name = "Gate Cereal Box";
        gate.transform.rotation = Quaternion.Euler(0, 0, 90);
        PlaceOnFloor(gate, new Vector3(xc, 0, barrierZ));
        gate.AddComponent<Rigidbody>().isKinematic = true;
        var gb = GetBounds(gate);
        var lifted = gate.AddComponent<LiftedGate>();

        // Its neighbours: one on its side either side of it, one toppling against each shelf, and one
        // dropped on top. Physics settles them (SettleProps) so they lean and lie where they would.
        var a = Fallen(root, 1, Quaternion.Euler(0, 7, 90), new Vector3(gb.min.x - CerealH / 2 - 0.5f, 0, barrierZ + 0.6f));
        Fallen(root, 2, Quaternion.Euler(0, -12, 14), new Vector3(xL + CerealW / 2 + 1.2f, 0, barrierZ - 0.4f));
        Fallen(root, 3, Quaternion.Euler(0, -5, 90), new Vector3(gb.max.x + CerealH / 2 + 0.5f, 0, barrierZ + 0.4f));
        Fallen(root, 4, Quaternion.Euler(0, 14, -15), new Vector3(xR - CerealW / 2 - 1.2f, 0, barrierZ - 0.6f));
        var ab = GetBounds(a);
        Fallen(root, 5, Quaternion.Euler(90, 22, 0), new Vector3(ab.center.x, ab.max.y + 0.6f, barrierZ + 0.3f));
        SpillPieces(root, new Vector3(xc - 8f, 0, barrierZ - 4f), 5f, 50);

        // Seals: either side of the gate floor to ceiling, and above the gate's highest point, so the
        // only way through is underneath it.
        float sealD = 4f;
        Seal(root, "Seal West", new Vector3((xL - 3f + gb.min.x) / 2, WallHeight / 2, barrierZ), new Vector3(gb.min.x - xL + 3f, WallHeight, sealD));
        Seal(root, "Seal East", new Vector3((gb.max.x + xR + 3f) / 2, WallHeight / 2, barrierZ), new Vector3(xR + 3f - gb.max.x, WallHeight, sealD));
        float liftedTop = gb.max.y + 6.5f;
        Seal(root, "Seal Above", new Vector3(xc, (liftedTop + WallHeight) / 2, barrierZ), new Vector3(gb.size.x + 0.4f, WallHeight - liftedTop, sealD));

        // The ruler on its can: short near arm, long far arm, long end's tip under the box's middle.
        const float nearArm = 8f, farArm = 14f, plankW = 4f, thick = 0.3f;
        var canPrefab = BeanCanBuilder.GetOrCreate();
        var probe = Spawn(canPrefab, null, 0, 1);
        var pb = GetBounds(probe);
        Object.DestroyImmediate(probe);
        float canScale = 0.13f * Scale / pb.size.y;
        float canDiameter = Mathf.Max(pb.size.x, pb.size.z) * canScale;
        float pivotY = canDiameter + thick / 2;
        float rest = Mathf.Asin(pivotY / farArm) * Mathf.Rad2Deg;     // long end on the floor
        float tipped = -Mathf.Asin(pivotY / nearArm) * Mathf.Rad2Deg; // short end on the floor
        float tipZ = gb.center.z;
        float pivotZ = tipZ - farArm * Mathf.Cos(rest * Mathf.Deg2Rad);

        var can = Spawn(canPrefab, root, 0, canScale);
        can.name = "Pivot Can";
        can.transform.rotation = Quaternion.Euler(0, 0, 90) * can.transform.rotation;
        PlaceOnFloor(can, new Vector3(xc, 0, pivotZ));
        AddBoxColliders(can);

        var pivot = Plank(root, "Ruler", new Vector3(xc, pivotY, pivotZ), nearArm, farArm, plankW, thick,
            GetMaterial("Ruler", new Color(0.86f, 0.72f, 0.38f)), ticks: true);
        lifted.Setup(gb.min.y + 0.25f);
        pivot.gameObject.AddComponent<Seesaw>().Setup(pivot, rest, tipped, farArm, thick, 0f, lifted);

        // A matchbox to climb up onto the raised short end.
        float nearTipZ = pivotZ - nearArm * Mathf.Cos(rest * Mathf.Deg2Rad);
        Matchbox(root, new Vector3(xc, 0, nearTipZ - 1.4f));

        TutorialZoneAt(level, new Vector3(xc, 5, nearTipZ - 6f), new Vector3(w, 10, 16f), "balance", "TIP THE SCALES",
            "Climb the matchbox and jump onto the short end of the ruler. Your weight will lever the box up.", "SPACE");
    }

    // A plank on a pivot transform (the transform the Seesaw rotates). Local +Z is the far arm.
    static Transform Plank(Transform parent, string name, Vector3 pivotAt, float nearArm, float farArm, float width, float thick, Material mat, bool ticks)
    {
        var pivot = new GameObject($"{name} Pivot").transform;
        pivot.SetParent(parent);
        pivot.position = pivotAt;
        var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plank.name = name;
        plank.transform.SetParent(pivot, false);
        plank.transform.localPosition = new Vector3(0, 0, (farArm - nearArm) / 2);
        plank.transform.localScale = new Vector3(width, thick, nearArm + farArm);
        plank.GetComponent<Renderer>().sharedMaterial = mat;
        plank.AddComponent<Rigidbody>().isKinematic = true;
        if (ticks)
        {
            var ink = GetMaterial("Ink", new Color(0.12f, 0.1f, 0.08f));
            int n = 30;
            for (int i = 0; i <= n; i++)
            {
                var tick = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tick.name = "Tick";
                tick.transform.SetParent(pivot, false);
                float len = i % 5 == 0 ? 1.2f : 0.6f;
                tick.transform.localPosition = new Vector3(-width / 2 + len / 2 + 0.1f, thick / 2 + 0.01f, -nearArm + 0.5f + i * (nearArm + farArm - 1f) / n);
                tick.transform.localScale = new Vector3(len, 0.02f, 0.1f);
                tick.GetComponent<Renderer>().sharedMaterial = ink;
                Object.DestroyImmediate(tick.GetComponent<Collider>());
            }
        }
        return pivot;
    }

    static void Matchbox(Transform parent, Vector3 floorAt)
    {
        var root = new GameObject("Matchbox").transform;
        root.SetParent(parent);
        var size = new Vector3(2.8f, 1.6f, 1.8f); // low enough to hop onto, high enough to reach the ruler from
        Box("Matchbox", root, floorAt + Vector3.up * size.y / 2, size, GetMaterial("Matchbox", new Color(0.15f, 0.3f, 0.7f)));
        Box("Strike Strip", root, floorAt + new Vector3(size.x / 2 + 0.01f, size.y / 2, 0), new Vector3(0.03f, size.y * 0.7f, size.z * 0.9f),
            GetMaterial("Strike Strip", new Color(0.35f, 0.2f, 0.12f)));
        Box("Label", root, floorAt + new Vector3(0, size.y + 0.01f, 0), new Vector3(size.x * 0.8f, 0.02f, size.z * 0.7f),
            GetMaterial("Matchbox Label", new Color(0.95f, 0.85f, 0.2f)));
        MakeText(root, floorAt + new Vector3(0, size.y / 2, -size.z / 2 - 0.02f), 0, "SAFETY MATCHES", size.x * 0.85f, size.y * 0.4f, Color.white);
    }

    static GameObject CarryBox(Transform parent, Vector3 at, float size = 1.5f)
    {
        var closed = LoadKenney("cardboardBoxClosed");
        if (closed == null) return null;
        var step = SpawnSized(closed, parent, size, 20f);
        step.name = "Carry Box";
        step.isStatic = false;
        PlaceOnFloor(step, at);
        AddBoxColliders(step);
        step.AddComponent<Carryable>();
        return step;
    }

    // ---------- Puzzle 2: the step ----------

    // A ledge of flattened boxes is just too high to jump. A small box nearby can be carried (E)
    // and dropped against it as a step.
    static void BuildStepPuzzle(Transform level, float xc, float w, float ledgeZ)
    {
        var root = new GameObject("Puzzle 2 - Step").transform;
        root.SetParent(level);
        const float ledgeH = 3f, ledgeD = 10f;

        var closed = LoadKenney("cardboardBoxClosed");
        if (closed != null)
            for (int i = 0; i < 3; i++)
            {
                var slab = FitInto(closed, root, new Vector3(w / 3, ledgeH, ledgeD));
                slab.name = "Ledge Box";
                slab.position = new Vector3(xc - w / 2 + (i + 0.5f) * w / 3, slab.position.y, ledgeZ + ledgeD / 2);
                AddBoxColliders(slab.gameObject);
            }
        Solid(root, "Ledge Collider", new Vector3(xc, ledgeH / 2, ledgeZ + ledgeD / 2), new Vector3(w + 2f, ledgeH, ledgeD));

        CarryBox(root, new Vector3(xc + w * 0.25f, 0, ledgeZ - 14f));
        devWarps.Add(("Puzzle 2", new Vector3(xc + w * 0.25f - 4f, 1, ledgeZ - 20f), 0));

        TutorialZoneAt(level, new Vector3(xc, 5, ledgeZ - 9f), new Vector3(w, 10, 16f), "hand", "NEED A BOOST?",
            "Pick up the small box, carry it to the ledge and put it down to use as a step.", "E");
    }

    // ---------- Puzzle 3: carry + lever + ramp ----------

    // A wall of flattened cracker boxes, too high to climb even with a step. In front of it a
    // cutting board lies flat, its far end resting on the tip of a wooden spatula balanced on a jar
    // of peanut butter that sits off to one side. The spatula's short end is too high to jump onto,
    // so: carry the box over as a step, climb onto the spatula's short end, and the long end swings
    // up, lifting the cutting board into a ramp. Walk up it and over the wall.
    static void BuildRampPuzzle(Transform level, float xc, float w, float wallZ)
    {
        var root = new GameObject("Puzzle 3 - Ramp").transform;
        root.SetParent(level);
        float xL = xc - w / 2, xR = xc + w / 2;

        // The wall: three layers of cracker boxes lying flat (long side across the hall, face up),
        // staggered like bricks. Boxes at the ends of a layer are shorter ones cut to fit, so no two
        // boxes overlap.
        float layerH = CerealD, boxLen = CerealH, wallD = CerealW;
        int layers = 3;
        float wallTop = layers * layerH;
        for (int layer = 0; layer < layers; layer++)
        {
            float offset = layer % 2 == 0 ? 0 : boxLen / 2;
            for (float x = xL - offset; x < xR - 0.5f; x += boxLen)
            {
                float a = Mathf.Max(x, xL), b = Mathf.Min(x + boxLen, xR);
                if (b - a < 1f) continue;
                float len = b - a - 0.08f; // hairline gap between neighbours
                var bx = CerealBox(root, 5);
                bx.transform.localScale = new Vector3(1f, len / boxLen, 1f); // shortened along its length (local Y) only
                bx.transform.rotation = Quaternion.Euler(90, 90, 0) * Quaternion.Euler(0, (layer + (int)(x * 0.1f)) % 2 == 0 ? 0 : 180, 0);
                PlaceOnFloor(bx, new Vector3((a + b) / 2, layer * layerH, wallZ + wallD / 2));
            }
        }
        Solid(root, "Wall Collider", new Vector3(xc, wallTop / 2, wallZ + wallD / 2), new Vector3(w + 2f, wallTop, wallD));

        // The ramp: a cutting board lying flat, hinged at its near (south) end.
        const float rampLen = 15f, rampW = 6f;
        float rampFarZ = wallZ - 0.6f;
        var hinge = new GameObject("Ramp Hinge").transform;
        hinge.SetParent(root);
        hinge.position = new Vector3(xc, 0.3f, rampFarZ - rampLen);
        var boardPrefab = LoadModel("k:cutting-board");
        Transform board;
        if (boardPrefab != null)
        {
            board = FitInto(boardPrefab, hinge, new Vector3(rampW, 0.6f, rampLen));
            board.localPosition = new Vector3(0, 0, rampLen / 2);
            AddBoxColliders(board.gameObject);
        }
        else
        {
            board = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            board.SetParent(hinge, false);
            board.localScale = new Vector3(rampW, 0.6f, rampLen);
            board.localPosition = new Vector3(0, 0, rampLen / 2);
        }
        foreach (var t in hinge.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
        hinge.gameObject.AddComponent<Rigidbody>().isKinematic = true;
        var ramp = hinge.gameObject.AddComponent<LiftedRamp>();
        ramp.Setup(rampLen, wallTop - 0.3f, 0f);

        // The lever runs across the hall: pivot (a peanut butter jar on its side) to the west of the
        // ramp, short arm further west, long arm east so its tip ends under the ramp's far end.
        // Short end rests ~2.8 up: out of jumping reach, but reachable from the carried box.
        const float nearArm = 4f, farArm = 7f, plankW = 3f, thick = 0.4f, pivotH = 1.6f;
        float tipX = xc + 1f;
        float pivotX = tipX - farArm;
        float rest = Mathf.Asin((pivotH + thick / 2) / farArm) * Mathf.Rad2Deg;
        float tippedTo = -Mathf.Asin((pivotH + thick / 2) / nearArm) * Mathf.Rad2Deg;
        var jarPrefab = LoadModel("k:peanut-butter");
        if (jarPrefab != null)
        {
            var jar = Spawn(jarPrefab, root, 0, 1);
            var jb = GetBounds(jar);
            jar.transform.localScale *= pivotH / Mathf.Max(jb.size.x, jb.size.z);
            jar.transform.rotation = Quaternion.Euler(90, 0, 0) * jar.transform.rotation; // on its side, axis along the hall
            PlaceOnFloor(jar, new Vector3(pivotX, 0, rampFarZ));
            AddBoxColliders(jar);
        }
        var pivot = Plank(root, "Spatula", new Vector3(pivotX, pivotH + thick / 2, rampFarZ), nearArm, farArm, plankW, thick,
            GetMaterial("Spatula Wood", new Color(0.72f, 0.52f, 0.3f)), ticks: false);
        pivot.gameObject.AddComponent<Seesaw>().Setup(pivot, rest, tippedTo, farArm, thick, 90f, ramp);
        pivot.rotation = Quaternion.Euler(0, 90, 0) * Quaternion.Euler(rest, 0, 0);

        CarryBox(root, new Vector3(xR - 5f, 0, rampFarZ - rampLen - 6f));
        devWarps.Add(("Puzzle 3", new Vector3(xR - 9f, 1, rampFarZ - rampLen - 12f), 0));
        SpillPieces(root, new Vector3(xc + 6f, 0, wallZ - 3f), 4f, 30);

        TutorialZoneAt(level, new Vector3(xc, 5, rampFarZ - rampLen - 4f), new Vector3(w, 10, 22f), "stairs", "BUILD A RAMP",
            "Bring the box over to the spatula's raised end, climb on, and your weight will lift the cutting board into a ramp.", "E", "SPACE");
    }

    // ---------- Tutorial cards, cinematic bars, fader ----------

    static void TutorialZoneAt(Transform parent, Vector3 center, Vector3 size, string icon, string title, string body, params string[] keys)
    {
        var z = new GameObject($"Tutorial - {title}");
        z.transform.SetParent(parent);
        z.transform.position = center;
        var box = z.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        z.AddComponent<TutorialZone>().Setup(Glyph(icon), title, body, keys);
    }

    static Text skipHint;

    // Top-of-screen card: rounded dark panel, teal accent, big icon, title, help line, key chips.
    // The same canvas carries the screen fader and the intro's "skip" hint.
    static void BuildTutorialHud()
    {
        var old = GameObject.Find("Tutorial HUD");
        if (old != null) Object.DestroyImmediate(old);
        var canvasGo = new GameObject("Tutorial HUD", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var titleFont = SignFont();
        var bodyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Oswald.ttf") ?? titleFont;

        RectTransform Rect(string name, Transform p, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(p, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }
        Image Img(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = rounded;
            img.type = Image.Type.Sliced;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }
        Text Txt(RectTransform rt, Font f, int size, Color c, TextAnchor a)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = f;
            t.fontSize = size;
            t.color = c;
            t.alignment = a;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        var accent = new Color(0.2f, 0.85f, 0.75f);
        var cardGroupGo = new GameObject("Card Group", typeof(RectTransform), typeof(CanvasGroup));
        var cardGroupRt = cardGroupGo.GetComponent<RectTransform>();
        cardGroupRt.SetParent(canvasGo.transform, false);
        cardGroupRt.anchorMin = Vector2.zero;
        cardGroupRt.anchorMax = Vector2.one;
        cardGroupRt.offsetMin = cardGroupRt.offsetMax = Vector2.zero;
        var group = cardGroupGo.GetComponent<CanvasGroup>();
        group.interactable = group.blocksRaycasts = false;

        // Top of the screen, so it never covers Henry.
        var card = Rect("Card", cardGroupRt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(980, 190));
        Img(card, new Color(0.04f, 0.06f, 0.07f, 0.9f));
        card.gameObject.AddComponent<Outline>().effectColor = new Color(accent.r, accent.g, accent.b, 0.35f);
        Img(Rect("Accent", card, new Vector2(0, 0), new Vector2(0, 1), new Vector2(6, 0), new Vector2(10, -24)), accent);
        var iconBg = Rect("Icon Circle", card, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(95, 0), new Vector2(120, 120));
        Img(iconBg, new Color(accent.r, accent.g, accent.b, 0.15f));
        var icon = Txt(Rect("Icon", iconBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), IconFont() ?? titleFont, 78, accent, TextAnchor.MiddleCenter);
        var title = Txt(Rect("Title", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-40, -42), new Vector2(-420, 56)), titleFont, 48, Color.white, TextAnchor.MiddleLeft);
        title.rectTransform.pivot = new Vector2(0, 0.5f);
        title.rectTransform.anchoredPosition = new Vector2(180, -42);
        var body = Txt(Rect("Body", card, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero), bodyFont, 26, new Color(0.82f, 0.86f, 0.86f), TextAnchor.UpperLeft);
        body.rectTransform.offsetMin = new Vector2(180, 18);
        body.rectTransform.offsetMax = new Vector2(-250, -78);
        var chips = new GameObject[3];
        var labels = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            var chip = Rect($"Key {i + 1}", card, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-125, -38 - i * 52), new Vector2(190, 44));
            Img(chip, new Color(0.93f, 0.95f, 0.95f));
            var shadow = chip.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(0, -4);
            labels[i] = Txt(Rect("Label", chip, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), titleFont, 30, new Color(0.08f, 0.1f, 0.1f), TextAnchor.MiddleCenter);
            chips[i] = chip.gameObject;
        }
        canvasGo.AddComponent<TutorialPopup>().Setup(group, card, icon, title, body, chips, labels);

        // Skip hint for the intro, bottom right.
        skipHint = Txt(Rect("Skip Hint", canvasGo.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-180, 60), new Vector2(320, 50)),
            titleFont, 34, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleRight);
        skipHint.text = "SPACE  TO SKIP";
        skipHint.gameObject.SetActive(false);

        // Full-screen fader on top of everything.
        var fadeRt = Rect("Fader", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fadeRt.offsetMin = fadeRt.offsetMax = Vector2.zero;
        var fade = fadeRt.gameObject.AddComponent<Image>();
        fade.color = new Color(0, 0, 0, 0);
        fade.raycastTarget = false;
        fade.enabled = false;
        canvasGo.AddComponent<ScreenFader>().Setup(fade);
    }

    // Letterbox bars for cutscenes, hidden until one plays.
    static (RectTransform top, RectTransform bottom) BuildCinematicBars()
    {
        var old = GameObject.Find("Cinematic Bars");
        if (old != null) Object.DestroyImmediate(old);
        var canvasGo = new GameObject("Cinematic Bars", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        RectTransform Bar(string name, Vector2 min, Vector2 max)
        {
            var rt = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rt.SetParent(canvasGo.transform, false);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.GetComponent<Image>().color = Color.black;
            rt.gameObject.SetActive(false);
            return rt;
        }
        return (Bar("Top Bar", new Vector2(0, 1), new Vector2(1, 1)), Bar("Bottom Bar", new Vector2(0, 0), new Vector2(1, 0)));
    }
}

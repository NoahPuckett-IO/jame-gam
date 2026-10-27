using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

static partial class GroceryStoreBuilder
{
    static (RectTransform top, RectTransform bottom) cinematicBars;

    // Level one climbs all over the bakery aisle. The walkway floor is cut into rooms by box barricades, and every
    // way forward is up a shelf and back down again:
    //   A  floor: carry the bean can onto the ruler lever's lid to hold it down, walk up it to the east first shelf
    //      east shelf 1: hop the jars, dodge the rolling can, then the plank ramp up to east shelf 2
    //      east shelf 2: a giant wooden spoon slopes up across the aisle to west shelf 3 (the floor below is a sticky
    //      soda spill)
    //      west shelf 3: a cereal box blocks the way, so up a ladder onto the top of the shelf and down another one
    //      past it; three cans stood on three spots drop the ladder back up to the paper plane, which loops round
    //      over the aisle and lands past the second blocked cubby, where the baking tray slide carries Henry down
    //      three shelves, over a barricade, to room C
    //   C  floor: the ladder up the east side hangs out of reach; carry a box under it to climb to east shelf 1
    //      east shelf 1: past the rolling cans to a second slide back down to room D and the way out
    static void PlanLevelOne(List<Transform> aisles, List<float> walkways)
    {
        var t = levelTwoHall;
        var hall = new Hall
        {
            x = walkways[0], xW = walkways[0] - WalkwayWidth / 2, xE = walkways[0] + WalkwayWidth / 2,
            boardDepth = t.boardDepth, h1 = t.h1, h2 = t.h2, h3 = t.h3,
        };
        MeasureShelfTop(aisles[0], ref hall);
        levelOneHall = hall;
        RouteBoard(hall, false, hall.h1, -88f, -46f);
        RouteBoard(hall, false, hall.h2, -34f, -12f);
        RouteBoard(hall, true, hall.h3, -18f, 35f);
        RouteBoard(hall, false, hall.h1, 60f, 100f);
    }

    const float L1BarrierA = -72f, L1BarrierB = 34f, L1BarrierC = 76f;

    static void BuildLevelOne(Transform store, List<float> walkways, float aisleLength, float depth, Transform spawn,
        out Transform wakeInside, out Transform wakeOutside)
    {
        var level = new GameObject("Level One").transform;
        level.SetParent(store);

        var hall = levelOneHall;
        float xc = hall.x, w = WalkwayWidth, xW = hall.xW, xE = hall.xE;
        float zS = -aisleLength / 2, zN = aisleLength / 2;
        var rng = new System.Random(101);

        BuildTutorialHud();
        cinematicBars = BuildCinematicBars();

        WakeUpBox(level, spawn.position, out wakeInside, out wakeOutside);
        CheckpointAt(level, "Checkpoint L1-A", new Vector3(xc, 0, zS + 4f), new Vector3(w, 4, 3), 0);

        // room A
        ScatterLitter(level, xc, w, zS + 10f, L1BarrierA - 8f, 21, 28, onlySide: -1);
        ScatterLitter(level, xc, w, zS + 10f, -104f, 22, 8, onlySide: 1);
        LeverLift(level, hall, -92f);
        Barricade(level, xc, w, L1BarrierA, 2, true, WallHeight, rng);

        // east shelf 1 -> 2
        CheckpointAt(level, "Checkpoint L1-B", new Vector3(xE + 3.5f, hall.h1, -76f), new Vector3(6, 4, 4), 0);
        ShelfJar(level, new Vector3(xE + 2.5f, hall.h1, -69f), 1.3f);
        ShelfJar(level, new Vector3(xE + 4.8f, hall.h1, -69.6f), 1.1f);
        RollingCan(level, new Vector3(xE + 5.6f, hall.h1, -61f), new Vector3(-4.4f, 0, 0), 1.2f, 0.25f);
        Ramp(level, hall, westSide: false, hall.h1, -50f, hall.h2, -32f);
        CheckpointAt(level, "Checkpoint L1-C", new Vector3(xE + 3.5f, hall.h2, -24f), new Vector3(6, 4, 4), 0);
        devWarps.Add(("L1 Ramp", new Vector3(xE + 3.5f, hall.h1 + 1f, -72f), 0));

        // the sticky floor under the high part of the climb
        var spill = new GameObject("Sticky Soda Spill").AddComponent<BoxCollider>();
        spill.transform.SetParent(level);
        spill.isTrigger = true;
        spill.transform.position = new Vector3(xc, 0.4f, (L1BarrierA + L1BarrierB) / 2);
        spill.size = new Vector3(w + 1f, 0.8f, L1BarrierB - L1BarrierA - 9f);
        spill.gameObject.AddComponent<Hazard>();
        SodaSpill(level, xc, w, L1BarrierA + 5f, L1BarrierB - 5f, rng);

        // up across the aisle to the west
        SpoonBridge(level, new Vector3(xE + 6.3f, hall.h2, -16f), new Vector3(xW - 3f, hall.h3, -16f));
        devWarps.Add(("L1 Spoon", new Vector3(xE + 3.5f, hall.h2 + 1f, -24f), -90));
        CheckpointAt(level, "Checkpoint L1-D", new Vector3(xW - 3.5f, hall.h3, -11f), new Vector3(6, 4, 4), 0);

        // west shelf 3: over the top of the shelf, the can spots, the plane
        ShelfTopRoute(level, hall, rng);
        devWarps.Add(("L1 Top", new Vector3(xW - 3.5f, hall.h3 + 1f, -10f), 0));
        CheckpointAt(level, "Checkpoint L1-E", new Vector3(xW - 3.5f, hall.h3, 3f), new Vector3(6, 4, 3), 0);
        CanPuzzle(level, hall);
        devWarps.Add(("L1 Cans", new Vector3(xW - 3.5f, hall.h3 + 1f, 4f), 0));
        var landing = new Vector3(xW - 3f, hall.h3, 23f);
        PaperPlaneRide(level, hall, landing);
        devWarps.Add(("L1 Plane", new Vector3(xW - hall.back - 2.4f, hall.top + 1f, 14f), 0));

        // the long slide down
        CheckpointAt(level, "Checkpoint L1-F", new Vector3(xW - 3.5f, hall.h3, 27.5f), new Vector3(6, 4, 2.5f), 0);
        Slide(level, new Vector3(xW - 1.5f, hall.h3, 29f), new Vector3(xc + 2f, 0.4f, 62f), 5f, exitSpeed: 11f);
        Barricade(level, xc, w, L1BarrierB, 2, false, 17f, rng);
        devWarps.Add(("L1 Slide", new Vector3(xW - 3.5f, hall.h3 + 1f, 26f), 0));

        // room C: the ladder that hangs out of reach
        CheckpointAt(level, "Checkpoint L1-G", new Vector3(xc + 4f, 0, 68f), new Vector3(w, 4, 4), 0);
        // low enough that standing on one box puts its bottom rung at Henry's feet
        KitLadder(level, new Vector3(xE, 0, 68f), Vector3.left, 1.6f, hall.h1, new Vector3(xE + 2.2f, hall.h1, 68f));
        CarryBox(level, new Vector3(xc - 9f, 0, 47f), 1.6f);
        CarryBox(level, new Vector3(xc - 12f, 0, 57f), 1.6f);
        ScatterLitter(level, xc, w, L1BarrierB + 6f, L1BarrierC - 8f, 23, 26, onlySide: -1);
        for (int i = 0; i < 3; i++)
            Fallen(level, i + 2, Quaternion.Euler(0, rng.Next(360), 90), new Vector3(xc - 13f + i * 1.5f, 0, 40f + i * 7f));
        Barricade(level, xc, w, L1BarrierC, 2, true, WallHeight, rng);
        devWarps.Add(("L1 Ladder", new Vector3(xc, 1, 56f), 90));

        // east shelf 1, the cans, the slide to the exit
        CheckpointAt(level, "Checkpoint L1-H", new Vector3(xE + 3.5f, hall.h1, 72f), new Vector3(6, 4, 4), 0);
        RollingCan(level, new Vector3(xE + 1.2f, hall.h1, 81f), new Vector3(4.4f, 0, 0), 1.0f, 0f);
        RollingCan(level, new Vector3(xE + 5.6f, hall.h1, 87f), new Vector3(-4.4f, 0, 0), 0.9f, 0.5f);
        Slide(level, new Vector3(xE + 2.5f, hall.h1, 95f), new Vector3(xc - 6f, 0.4f, 103f), 4.5f, exitSpeed: 9f);

        // room D
        ScatterLitter(level, xc, w, L1BarrierC + 6f, zN - 3f, 37, 24, onlySide: 1);
        WetFloorSign(level, new Vector3(xc - 14f, 0, 90f), 160f);

        float northCorridor = (aisleLength / 2 + depth / 2 - CoolerDepth) / 2;
        float southCorridor = -northCorridor;

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

    // a ruler balanced on a bean can beside the east shelf, with a jar lid hung level under its near (south) end.
    // Standing on the lid tips the ruler's far end up to the first shelf, but it swings back the moment the lid is
    // empty, so Henry has to bring the heavy can along and leave it on the lid before walking up
    static void LeverLift(Transform level, Hall hall, float pivotZ)
    {
        var root = new GameObject("Puzzle - Lever Lift").transform;
        root.SetParent(level);
        const float near = 4.5f, far = 12f, width = 3f, thick = 0.4f, panRadius = 2.7f;
        float x = hall.xE - width / 2 - 0.25f;

        var canPrefab = BeanCanBuilder.GetOrCreate();
        var probe = Spawn(canPrefab, null, 0, 1);
        var pb = GetBounds(probe);
        Object.DestroyImmediate(probe);
        float canD = 2.4f;
        float canScale = canD / Mathf.Max(pb.size.x, pb.size.z);
        float pivotY = canD + thick / 2;
        var can = Spawn(canPrefab, root, 0, canScale);
        can.name = "Fulcrum Can";
        can.transform.localScale = new Vector3(can.transform.localScale.x, can.transform.localScale.y * width * 1.6f / (pb.size.y * canScale), can.transform.localScale.z);
        can.transform.rotation = Quaternion.Euler(0, 0, 90) * can.transform.rotation;
        PlaceOnFloor(can, new Vector3(x, 0, pivotZ));
        AddBoxColliders(can);

        float rest = Mathf.Asin((pivotY - thick / 2) / far) * Mathf.Rad2Deg;
        float tipped = -Mathf.Asin((pivotY - thick / 2) / near) * Mathf.Rad2Deg;
        var pivot = Plank(root, "Lever Ruler", new Vector3(x, pivotY, pivotZ), near, far, width, thick,
            GetMaterial("Ruler", new Color(0.86f, 0.72f, 0.38f)), ticks: true);

        var pan = new GameObject("Jar Lid").transform;
        pan.SetParent(root);
        var lidMat = GetMaterial("Jar Lid Gold", new Color(0.85f, 0.65f, 0.2f));
        lidMat.SetFloat("_Metallic", 0.75f);
        lidMat.SetFloat("_Smoothness", 0.55f);
        var lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lid.name = "Lid";
        Object.DestroyImmediate(lid.GetComponent<Collider>());
        lid.transform.SetParent(pan, false);
        lid.transform.localPosition = new Vector3(0, -0.15f, -panRadius + 0.3f);
        lid.transform.localScale = new Vector3(panRadius * 2, 0.15f, panRadius * 2);
        lid.GetComponent<Renderer>().sharedMaterial = lidMat;
        var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "Lid Rim";
        Object.DestroyImmediate(rim.GetComponent<Collider>());
        rim.transform.SetParent(pan, false);
        rim.transform.localPosition = new Vector3(0, -0.05f, -panRadius + 0.3f);
        rim.transform.localScale = new Vector3(panRadius * 2 + 0.3f, 0.12f, panRadius * 2 + 0.3f);
        rim.GetComponent<Renderer>().sharedMaterial = GetMaterial("Jar Lid Rim", new Color(0.55f, 0.4f, 0.12f));
        var col = pan.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0, -0.15f, -panRadius + 0.3f);
        col.size = new Vector3(panRadius * 1.8f, 0.3f, panRadius * 1.8f);
        foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
        Kinematic(pan.gameObject);

        var seesaw = pivot.gameObject.AddComponent<Seesaw>();
        seesaw.Setup(pivot, rest, tipped, far, thick, 0f);
        seesaw.SetupBalance(pan, near);
        pivot.rotation = Quaternion.Euler(rest, 0, 0);
        pan.position = pivot.TransformPoint(new Vector3(0, thick / 2, -near));

        // the weight: a big can of beans by the start
        var weight = Spawn(canPrefab, root, 25f, 2.8f / (pb.size.y));
        weight.name = "Heavy Can";
        PlaceOnFloor(weight, new Vector3(hall.x + 7f, 0, pivotZ - 10f));
        AddBoxColliders(weight);
        weight.isStatic = false;
        weight.AddComponent<Carryable>();
        devWarps.Add(("L1 Lever", new Vector3(hall.x + 4f, 1, pivotZ - 14f), 0));
    }

    // a wall of boxes across the walkway, taped boxes below, with an invisible seal up to sealTop
    static void Barricade(Transform parent, float xc, float width, float z, int layers, bool looseTop, float sealTop, System.Random rng)
    {
        var root = new GameObject("Barricade").transform;
        root.SetParent(parent);
        float box = 0.45f * Scale;
        int cols = Mathf.CeilToInt(width / box);
        float cell = width / cols;
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        float top = 0;
        for (int l = 0; l < layers; l++)
            for (int c = 0; c < cols; c++)
            {
                float size = l == 0 ? cell * 0.99f : cell * R(0.86f, 0.95f);
                var at = new Vector3(xc - width / 2 + (c + 0.5f) * cell + (l == 0 ? 0 : R(-0.4f, 0.4f)), l * cell * 0.99f, z + (l == 0 ? 0 : R(-0.5f, 0.5f)));
                var b = CardboardBox(root, l == 0 || rng.NextDouble() < 0.7 ? BoxKind.Sealed : BoxKind.SealedLong, size, l == 0 ? R(-1.5f, 1.5f) : R(-8f, 8f), at);
                if (l > 0) SettleLater(b, force: true);
                top = Mathf.Max(top, GetBounds(b).max.y);
            }
        if (looseTop)
            for (int c = 0; c < cols; c++)
            {
                if (rng.NextDouble() < 0.45) continue;
                var b = CardboardBox(root, rng.NextDouble() < 0.5 ? BoxKind.Ajar : BoxKind.Open, cell * R(0.55f, 0.75f), R(0f, 360f),
                    new Vector3(xc - width / 2 + (c + 0.5f) * cell, top + 0.4f, z + R(-1f, 1f)));
                SettleLater(b, force: true);
            }
        Seal(root, "Barricade Seal", new Vector3(xc, sealTop / 2, z), new Vector3(width + 1f, sealTop, cell));
        // switched on after the physics settle, or the loose boxes get shoved out of it and left hanging
        var seal = root.GetChild(root.childCount - 1).gameObject;
        seal.SetActive(false);
        lateSeals.Add(seal);
    }

    static readonly List<GameObject> lateSeals = new List<GameObject>();

    // a stack of taped boxes from the floor up to exactly topY
    static void BoxTower(Transform parent, Vector3 floorAt, float topY, float size, System.Random rng)
    {
        var root = new GameObject("Box Tower").transform;
        root.SetParent(parent);
        int n = Mathf.Max(1, Mathf.RoundToInt(topY / size));
        float h = topY / n;
        for (int i = 0; i < n; i++)
        {
            var b = CardboardBox(root, BoxKind.Sealed, h, (float)rng.NextDouble() * 10f - 5f, floorAt + Vector3.up * i * h);
            b.transform.localScale = Vector3.Scale(b.transform.localScale, new Vector3(size / h, 1f, size / h));
            PlaceOnFloor(b, floorAt + Vector3.up * i * h);
        }
        Solid(root, "Tower Collider", floorAt + Vector3.up * topY / 2, new Vector3(size, topY, size));
    }

    // a jar standing on a shelf board, small enough to hop over
    static void ShelfJar(Transform parent, Vector3 at, float height)
    {
        var jar = LoadModel("k:honey") ?? LoadModel("k:peanut-butter");
        if (jar == null) return;
        var j = Spawn(jar, parent, (at.x * 31f + at.z * 17f) % 360f, 1f);
        j.name = "Jar";
        var b = GetBounds(j);
        j.transform.localScale *= height / b.size.y;
        PlaceOnFloor(j, at);
        AddBoxColliders(j);
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

    static GameObject CerealBox(Transform parent, int style, float sizeScale = 1f)
    {
        style = ((style % Cereals.Length) + Cereals.Length) % Cereals.Length;
        var s = Cereals[style];
        var root = new GameObject($"Cereal Box ({s.brand.Replace('\n', ' ')})");
        root.transform.SetParent(parent);
        float W = CerealW * sizeScale, H = CerealH * sizeScale, D = CerealD * sizeScale;
        Box("Box", root.transform, Vector3.zero, new Vector3(W, H, D), CerealMat(style, CerealPart.Body));
        var frontMat = CerealMat(style, CerealPart.Front);
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
            var right = face < 0 ? 1f : -1f;
            MakeText(root.transform, new Vector3(0, H * 0.3f, 0) + outward, yaw, s.brand, W * 0.8f, H * 0.2f, Color.white, lit: true);
            MakeText(root.transform, new Vector3(0, H * 0.165f, 0) + outward, yaw, s.tag, W * 0.6f, H * 0.035f, new Color(1f, 1f, 0.85f), lit: true);
            MakeText(root.transform, new Vector3(right * W * 0.324f, H * 0.083f, 0) + outward, yaw, "NEW!", W * 0.16f, H * 0.04f, Color.white, lit: true);
            MakeText(root.transform, new Vector3(0, -H * 0.462f, 0) + outward, yaw, "FAMILY SIZE  •  NET WT 18 OZ (510g)", W * 0.8f, H * 0.03f, Color.white, lit: true);
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

    static void WakeUpBox(Transform parent, Vector3 spawn, out Transform inside, out Transform outside)
    {
        var root = new GameObject("Wake-up Cereal Box").transform;
        root.SetParent(parent);
        const int style = 0;
        float W = CerealW, H = CerealH, D = CerealD * 1.4f, t = 0.1f;
        var c = new Vector3(spawn.x, W / 2, spawn.z - H / 2 - 1f);
        var card = CerealMat(style, CerealPart.Cardboard);
        card.SetTextureScale("_BaseMap", new Vector2(3, 3));
        var front = CerealMat(style, CerealPart.Front);
        Box("Floor Panel", root, c + Vector3.down * (W / 2 - t / 2), new Vector3(D, t, H), card);
        Box("Roof Panel", root, c + Vector3.up * (W / 2 - t / 2), new Vector3(D, t, H), card);
        Box("Bottom Panel", root, c + Vector3.back * (H / 2 - t / 2), new Vector3(D, W, t), card);
        ArtQuad(root, "Nutrition Side", c + Vector3.up * (W / 2 + 0.01f), Quaternion.Euler(90, 0, 0), new Vector2(D, H), CerealMat(style, CerealPart.Side));
        ArtQuad(root, "Bottom Flaps", c + Vector3.back * (H / 2 + 0.01f), Quaternion.Euler(0, 0, 90), new Vector2(W, D), CerealMat(style, CerealPart.Top));
        foreach (int s in new[] { -1, 1 })
        {
            Box(s < 0 ? "West Panel" : "East Panel", root, c + Vector3.right * s * (D / 2 - t / 2), new Vector3(t, W, H), card);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Front Art";
            q.transform.SetParent(root);
            q.transform.position = c + Vector3.right * s * (D / 2 + 0.01f);
            q.transform.rotation = Quaternion.Euler(0, s < 0 ? 90 : -90, 0) * Quaternion.Euler(0, 0, s < 0 ? 90 : -90);
            q.transform.localScale = new Vector3(W, H, 1);
            q.GetComponent<Renderer>().sharedMaterial = front;
            Object.DestroyImmediate(q.GetComponent<Collider>());
        }
        var flapMat = card;
        var flapL = Box2("Flap", root, c + new Vector3(0, W / 2 - 0.2f, H / 2 + 1.2f), new Vector3(D * 0.9f, t, 2.4f), flapMat);
        flapL.transform.RotateAround(c + new Vector3(0, W / 2, H / 2), Vector3.right, 35f);
        var flapB = Box2("Flap", root, c + new Vector3(0, -W / 2 + 0.05f, H / 2 + 1.2f), new Vector3(D * 0.9f, t, 2.4f), flapMat);
        flapB.transform.RotateAround(c + new Vector3(0, -W / 2, H / 2), Vector3.right, -8f);
        SpillPieces(root, c + new Vector3(0, -W / 2, H / 2 + 3f), 4f, 40);

        inside = Waypoint(root, "Inside", new Vector3(c.x, 1f, c.z - H * 0.15f), 0);
        outside = Waypoint(root, "Outside", new Vector3(c.x, 1f, c.z + H / 2 + 9f), 0);
    }

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

    static void ScatterLitter(Transform parent, float xc, float w, float z0, float z1, int seed, int count = 70, int onlySide = 0)
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
        for (int i = 0; i < count; i++)
        {
            float side = onlySide != 0 ? onlySide : rng.Next(2) == 0 ? -1 : 1;
            float edge = (float)rng.NextDouble();
            float x = xc + side * (w / 2 - 1f - edge * edge * w * 0.4f);
            float z = Mathf.Lerp(z0, z1, (float)rng.NextDouble());
            if (rng.NextDouble() < 0.18)
            {
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
            string n = t.prefab.name;
            bool rolls = n.Contains("can") || n.Contains("Can") || n.Contains("cup") || n.Contains("stick") || n.Contains("chopstick");
            if (rolls && rng.NextDouble() < 0.7) p.transform.rotation = Quaternion.Euler(90, (float)rng.NextDouble() * 360f, 0) * p.transform.rotation;
            PlaceOnFloor(p, new Vector3(x, 0, z));
            SettleLater(p);
        }
    }

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
        var size = new Vector3(2.8f, 1.6f, 1.8f);
        Box("Matchbox", root, floorAt + Vector3.up * size.y / 2, size, GetMaterial("Matchbox", new Color(0.15f, 0.3f, 0.7f)));
        Box("Strike Strip", root, floorAt + new Vector3(size.x / 2 + 0.01f, size.y / 2, 0), new Vector3(0.03f, size.y * 0.7f, size.z * 0.9f),
            GetMaterial("Strike Strip", new Color(0.35f, 0.2f, 0.12f)));
        Box("Label", root, floorAt + new Vector3(0, size.y + 0.01f, 0), new Vector3(size.x * 0.8f, 0.02f, size.z * 0.7f),
            GetMaterial("Matchbox Label", new Color(0.95f, 0.85f, 0.2f)));
        MakeText(root, floorAt + new Vector3(0, size.y / 2, -size.z / 2 - 0.02f), 0, "SAFETY MATCHES", size.x * 0.85f, size.y * 0.4f, Color.white, lit: true);
    }

    static GameObject CarryBox(Transform parent, Vector3 at, float size = 1.5f)
    {
        var step = CardboardBox(parent, BoxKind.Sealed, size, 20f, at);
        if (step == null) return null;
        step.name = "Carry Box";
        foreach (var t in step.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
        step.AddComponent<Carryable>();
        return step;
    }

    static Text skipHint;

    // white rectangle, square on top, rounded on the bottom two corners; nine-sliced
    static Sprite BannerSprite()
    {
        const string path = "Assets/Settings/TutorialBanner.png";
        if (!System.IO.File.Exists(path))
        {
            const int size = 96, r = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = 1f;
                if (y < r && (x < r || x >= size - r))
                {
                    float cx = x < r ? r : size - r - 1, d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, r));
                    a = Mathf.Clamp01(r - d + 0.5f);
                }
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }
        if (AssetImporter.GetAtPath(path) is TextureImporter ti && (ti.textureType != TextureImporterType.Sprite || ti.spriteBorder.x < 1f))
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteBorder = new Vector4(32, 32, 32, 32);
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

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

        var card = Rect("Card", cardGroupRt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -95), new Vector2(980, 190));
        var banner = Img(card, new Color(0.06f, 0.09f, 0.1f, 0.94f));
        banner.sprite = BannerSprite();
        banner.pixelsPerUnitMultiplier = 1f;
        var iconBg = Rect("Icon", card, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(95, 0), new Vector2(120, 120));
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

        skipHint = Txt(Rect("Skip Hint", canvasGo.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-180, 60), new Vector2(320, 50)),
            titleFont, 34, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleRight);
        skipHint.text = "SPACE  TO SKIP";
        skipHint.gameObject.SetActive(false);

        var fadeRt = Rect("Fader", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fadeRt.offsetMin = fadeRt.offsetMax = Vector2.zero;
        var fade = fadeRt.gameObject.AddComponent<Image>();
        fade.color = new Color(0, 0, 0, 0);
        fade.raycastTarget = false;
        fade.enabled = false;
        canvasGo.AddComponent<ScreenFader>().Setup(fade);
    }

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

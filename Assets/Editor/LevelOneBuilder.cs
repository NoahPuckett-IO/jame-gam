using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Level One for GroceryStoreBuilder: puzzles along the first hallway (walkway 0), tutorial cards,
// a cutscene at its far end where Henry rounds the corner and looks up at the next hall, and
// checkpoints at every turning point of the S-shaped route.
//
//   z ───────────────────────────────────────────────────────────────►  (north, away from spawn)
//   spawn ... [matchbox → ruler on a can → fallen cereal boxes] ... [carry box → box ledge] ... corner
static partial class GroceryStoreBuilder
{
    static void BuildLevelOne(Transform store, List<float> walkways, float aisleLength, float depth, Vector3 spawn)
    {
        var level = new GameObject("Level One").transform;
        level.SetParent(store);

        float xc = walkways[0], w = WalkwayWidth;
        float start = -aisleLength / 2;

        BuildTutorialHud();
        TutorialZoneAt(level, new Vector3(xc, 5, spawn.z + 6f), new Vector3(w, 10, 30f), "walk", "LOOK AROUND, HENRY",
            "Walk with WASD, look with the mouse. Hold Shift to run and press Space to jump.", "WASD", "SHIFT", "SPACE");

        float barrierZ = start + aisleLength * 0.3f;
        BuildSeesawPuzzle(level, xc, w, barrierZ);
        BuildStepPuzzle(level, xc, w, start + aisleLength * 0.55f);

        var bars = BuildCinematicBars();
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
            Waypoint(cut, "Next Hall", new Vector3(walkways[1], 1, aisleLength / 2 - 0.6f * Scale), 180),
        };
        cut.gameObject.AddComponent<CornerCutscene>().Setup(points, bars.top, bars.bottom);

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
            box.size = new Vector3(w, 10, 4);
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

    // ---------- Puzzle 1: the ruler and the cereal boxes ----------

    // A handful of family-size cereal boxes have toppled across the hall. Too tall to jump, and the
    // gaps are sealed. A wooden ruler balances on a can, its long end tucked under the middle box.
    // Hop up via a matchbox onto the short end; Henry's weight tips the ruler (a set animation) and
    // its long end jacks the box up high enough to walk underneath.
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
        foreach (var t in gate.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
        gate.AddComponent<Rigidbody>().isKinematic = true;
        var gb = GetBounds(gate);
        var lifted = gate.AddComponent<LiftedGate>();

        // Its neighbours: tumbled at odd angles, one leaning on the shelf, one on top of the others.
        var a = Fallen(root, 1, Quaternion.Euler(0, 7, 94), new Vector3(gb.min.x - CerealH / 2 - 0.3f, 0, barrierZ + 0.6f));
        var b = Fallen(root, 2, Quaternion.Euler(0, -12, 72), new Vector3(xL + CerealH * 0.42f, 0, barrierZ - 0.4f));
        var c = Fallen(root, 3, Quaternion.Euler(0, -5, 85), new Vector3(gb.max.x + CerealH / 2 + 0.3f, 0, barrierZ + 0.4f));
        var d = Fallen(root, 4, Quaternion.Euler(0, 14, 103), new Vector3(xR - CerealH * 0.45f, 0, barrierZ - 0.6f));
        float topY = Mathf.Max(GetBounds(a).max.y, GetBounds(b).max.y);
        Fallen(root, 0, Quaternion.Euler(84, 22, 0), new Vector3((xL + gb.min.x) / 2, topY, barrierZ + 0.8f));

        // Invisible seals either side of the gate so nothing slips between boxes or past the shelves.
        void Seal(float x0, float x1)
        {
            var s = new GameObject("Barrier Seal").AddComponent<BoxCollider>();
            s.transform.SetParent(root);
            s.transform.position = new Vector3((x0 + x1) / 2, 7f, barrierZ);
            s.size = new Vector3(x1 - x0, 14f, 3f);
        }
        Seal(xL - 2f, gb.min.x - 0.1f);
        Seal(gb.max.x + 0.1f, xR + 2f);

        // The ruler on its can: short near arm, long far arm.
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
        float tipZ = gb.min.z + 0.6f; // long end tucked just under the gate box
        float pivotZ = tipZ - farArm * Mathf.Cos(rest * Mathf.Deg2Rad);

        var can = Spawn(canPrefab, root, 0, canScale);
        can.name = "Pivot Can";
        can.transform.rotation = Quaternion.Euler(0, 0, 90) * can.transform.rotation; // on its side, rolling axis across the hall
        PlaceOnFloor(can, new Vector3(xc, 0, pivotZ));
        AddBoxColliders(can);

        var pivot = new GameObject("Ruler Pivot").transform;
        pivot.SetParent(root);
        pivot.position = new Vector3(xc, pivotY, pivotZ);
        var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plank.name = "Ruler";
        plank.transform.SetParent(pivot, false);
        plank.transform.localPosition = new Vector3(0, 0, (farArm - nearArm) / 2);
        plank.transform.localScale = new Vector3(plankW, thick, nearArm + farArm);
        plank.GetComponent<Renderer>().sharedMaterial = GetMaterial("Ruler", new Color(0.86f, 0.72f, 0.38f));
        plank.AddComponent<Rigidbody>().isKinematic = true;
        var ink = GetMaterial("Ink", new Color(0.12f, 0.1f, 0.08f));
        int ticks = 30;
        for (int i = 0; i <= ticks; i++)
        {
            var tick = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tick.name = "Tick";
            tick.transform.SetParent(pivot, false);
            float len = i % 5 == 0 ? 1.2f : 0.6f;
            tick.transform.localPosition = new Vector3(-plankW / 2 + len / 2 + 0.1f, thick / 2 + 0.01f, -nearArm + 0.5f + i * (nearArm + farArm - 1f) / ticks);
            tick.transform.localScale = new Vector3(len, 0.02f, 0.1f);
            tick.GetComponent<Renderer>().sharedMaterial = ink;
            Object.DestroyImmediate(tick.GetComponent<Collider>());
        }
        pivot.localRotation = Quaternion.Euler(rest, 0, 0);
        lifted.Setup(farArm * Mathf.Sin(-tipped * Mathf.Deg2Rad) + pivotY - 0.4f);
        pivot.gameObject.AddComponent<Seesaw>().Setup(pivot, rest, tipped, lifted);

        // A matchbox to climb up onto the raised short end.
        float nearTipZ = pivotZ - nearArm * Mathf.Cos(rest * Mathf.Deg2Rad);
        Matchbox(root, new Vector3(xc, 0, nearTipZ - 1.4f));

        TutorialZoneAt(level, new Vector3(xc, 5, nearTipZ - 6f), new Vector3(w, 10, 16f), "balance", "TIP THE SCALES",
            "Climb the matchbox and jump onto the short end of the ruler. Your weight will lever the box up.", "SPACE");
    }

    const float CerealW = 0.32f * Scale, CerealH = 0.45f * Scale, CerealD = 0.1f * Scale;

    static readonly (string brand, Color body, Color band, Color text, string icon)[] Cereals =
    {
        ("CRUNCHY\nO'S", new Color(0.85f, 0.15f, 0.1f), new Color(1f, 0.85f, 0.2f), new Color(0.6f, 0.05f, 0.05f), "breakfast"),
        ("HONEY\nFLAKES", new Color(0.95f, 0.62f, 0.12f), new Color(0.4f, 0.22f, 0.08f), Color.white, "bakery"),
        ("CHOCO\nPUFFS", new Color(0.35f, 0.18f, 0.1f), new Color(0.95f, 0.8f, 0.5f), new Color(0.3f, 0.12f, 0.05f), "cookie"),
        ("FROSTY\nSTARS", new Color(0.15f, 0.4f, 0.85f), Color.white, new Color(0.1f, 0.25f, 0.6f), "star"),
        ("FRUITY\nRINGS", new Color(0.5f, 0.2f, 0.7f), new Color(0.4f, 0.9f, 0.4f), new Color(0.2f, 0.4f, 0.1f), "apple"),
    };

    // A family-size cereal box, built upright at the origin (front facing -Z), label on both big faces.
    static GameObject CerealBox(Transform parent, int style)
    {
        var s = Cereals[style % Cereals.Length];
        var root = new GameObject("Cereal Box");
        root.transform.SetParent(parent);
        var bodyMat = GetMaterial($"Cereal {style} Body", s.body);
        var bandMat = GetMaterial($"Cereal {style} Band", s.band);
        Box("Box", root.transform, Vector3.zero, new Vector3(CerealW, CerealH, CerealD), bodyMat);
        foreach (int face in new[] { -1, 1 })
        {
            float z = face * (CerealD / 2 + 0.02f);
            Box("Band", root.transform, new Vector3(0, CerealH * 0.14f, z), new Vector3(CerealW * 0.92f, CerealH * 0.3f, 0.03f), bandMat);
            Box("Top Stripe", root.transform, new Vector3(0, CerealH * 0.44f, z), new Vector3(CerealW, CerealH * 0.06f, 0.03f), bandMat);
            float yaw = face < 0 ? 0 : 180;
            var outward = new Vector3(0, 0, face * 0.05f);
            MakeText(root.transform, new Vector3(0, CerealH * 0.14f, z) + outward, yaw, s.brand, CerealW * 0.82f, CerealH * 0.25f, s.text);
            MakeIcon(root.transform, new Vector3(0, -CerealH * 0.2f, z) + outward, yaw, s.icon, CerealW * 0.55f, s.band);
            MakeText(root.transform, new Vector3(0, -CerealH * 0.43f, z) + outward, yaw, "FAMILY SIZE", CerealW * 0.6f, CerealH * 0.05f, Color.white);
        }
        foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
        return root;
    }

    static GameObject Fallen(Transform parent, int style, Quaternion rotation, Vector3 at)
    {
        var box = CerealBox(parent, style);
        box.transform.rotation = rotation;
        PlaceOnFloor(box, at);
        return box;
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
        // Solid block matching the ledge, slightly wider than the hall, so there's no gap at the shelves.
        var block = new GameObject("Ledge Collider").AddComponent<BoxCollider>();
        block.transform.SetParent(root);
        block.transform.position = new Vector3(xc, ledgeH / 2, ledgeZ + ledgeD / 2);
        block.size = new Vector3(w + 2f, ledgeH, ledgeD);

        if (closed != null)
        {
            var step = SpawnSized(closed, root, 1.9f, 20f);
            step.name = "Carry Box";
            PlaceOnFloor(step, new Vector3(xc + w * 0.25f, 0, ledgeZ - 14f));
            AddBoxColliders(step);
            step.AddComponent<Carryable>();
        }

        // A few cans and jars tumbled off the pile.
        var rng = new System.Random(5);
        var junk = Measure(Stock[1]);
        for (int i = 0; i < 8 && junk.Count > 0; i++)
        {
            var t = junk[rng.Next(junk.Count)];
            var p = Spawn(t.prefab, root, (float)rng.NextDouble() * 360f, t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z));
            p.transform.rotation = Quaternion.Euler(90, (float)rng.NextDouble() * 360f, 0) * p.transform.rotation;
            // Only along the left shelf, well clear of where the step box gets carried and dropped.
            PlaceOnFloor(p, new Vector3(xc - (w / 2 - 1.5f - (float)rng.NextDouble() * 3f), 0, ledgeZ - (float)rng.NextDouble() * 6f));
            AddBoxColliders(p);
        }

        TutorialZoneAt(level, new Vector3(xc, 5, ledgeZ - 9f), new Vector3(w, 10, 16f), "hand", "NEED A BOOST?",
            "Pick up the small box, carry it to the ledge and put it down to use as a step.", "E");
    }

    // ---------- Tutorial cards ----------

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

    // Bottom-of-screen card: rounded dark panel, teal accent, big icon, title, help line, key chips.
    static void BuildTutorialHud()
    {
        var old = GameObject.Find("Tutorial HUD");
        if (old != null) Object.DestroyImmediate(old);
        var canvasGo = new GameObject("Tutorial HUD", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var group = canvasGo.GetComponent<CanvasGroup>();
        group.interactable = group.blocksRaycasts = false;

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
            return t;
        }

        var accent = new Color(0.2f, 0.85f, 0.75f);
        // Top of the screen, so it never covers Henry.
        var card = Rect("Card", canvasGo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(980, 190));
        Img(card, new Color(0.04f, 0.06f, 0.07f, 0.9f));
        card.gameObject.AddComponent<Outline>().effectColor = new Color(accent.r, accent.g, accent.b, 0.35f);
        var bar = Rect("Accent", card, new Vector2(0, 0), new Vector2(0, 1), new Vector2(6, 0), new Vector2(10, -24));
        Img(bar, accent);

        var iconBg = Rect("Icon Circle", card, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(95, 0), new Vector2(120, 120));
        Img(iconBg, new Color(accent.r, accent.g, accent.b, 0.15f));
        var icon = Txt(Rect("Icon", iconBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), IconFont() ?? titleFont, 78, accent, TextAnchor.MiddleCenter);

        var title = Txt(Rect("Title", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-40, -42), new Vector2(-420, 56)), titleFont, 48, Color.white, TextAnchor.MiddleLeft);
        title.rectTransform.pivot = new Vector2(0, 0.5f);
        title.rectTransform.anchoredPosition = new Vector2(180, -42);
        var body = Txt(Rect("Body", card, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero), bodyFont, 26, new Color(0.82f, 0.86f, 0.86f), TextAnchor.UpperLeft);
        body.rectTransform.offsetMin = new Vector2(180, 18);
        body.rectTransform.offsetMax = new Vector2(-250, -78);

        // Up to three key chips stacked on the right.
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

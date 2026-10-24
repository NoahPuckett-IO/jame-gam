using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Level One for GroceryStoreBuilder: puzzles along the first hallway (walkway 0), a cutscene at
// its far end where the character rounds the corner and looks up at the next hall, and
// checkpoints at every turning point of the S-shaped route.
//
//   z ─────────────────────────────────────────────────────────────►  (north, away from spawn)
//   spawn ... [seesaw → can wall] ... [carry box → box ledge] ... (free space for later) ... corner
static partial class GroceryStoreBuilder
{
    static void BuildLevelOne(Transform store, List<float> walkways, float aisleLength, float depth)
    {
        var level = new GameObject("Level One").transform;
        level.SetParent(store);

        float xc = walkways[0], w = WalkwayWidth;
        float start = -aisleLength / 2;

        BuildSeesawPuzzle(level, xc, w, start + aisleLength * 0.28f);
        BuildStepPuzzle(level, xc, w, start + aisleLength * 0.52f);

        var bars = BuildCinematicBars();
        float northCorridor = (aisleLength / 2 + depth / 2 - CoolerDepth) / 2;
        float southCorridor = -northCorridor;

        // End of the first hall: cutscene round the corner into walkway 1, looking south down it.
        var cut = new GameObject("Corner Cutscene").transform;
        cut.SetParent(level);
        cut.position = new Vector3(xc, 5, aisleLength / 2 + 1f);
        var trigger = cut.gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(w, 10, 4);
        var points = new[]
        {
            Waypoint(cut, "Turn", new Vector3(xc, 1, northCorridor), 0),
            Waypoint(cut, "Corner", new Vector3(walkways[1], 1, northCorridor), -90),
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
            var spawn = Waypoint(cp, "Respawn", new Vector3(walkways[k], 1, south ? southCorridor : northCorridor), -90);
            cp.gameObject.AddComponent<Checkpoint>().Setup(spawn);
        }
    }

    static Transform Waypoint(Transform parent, string name, Vector3 pos, float yaw)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent);
        t.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        return t;
    }

    // Puzzle 1: a wall of cans too tall to jump blocks the hall. In front of it lies a wooden ruler
    // balanced on a bottle cap, its far end wedged against the cans. Jump onto the raised near end,
    // your weight tips it, the far end kicks up and the wall comes down.
    static void BuildSeesawPuzzle(Transform level, float xc, float w, float wallZ)
    {
        var root = new GameObject("Puzzle 1 - Seesaw").transform;
        root.SetParent(level);

        // Can wall: one row across the full hall, seven cans high.
        var canPrefab = BeanCanBuilder.GetOrCreate();
        var wallGo = new GameObject("Can Wall");
        wallGo.transform.SetParent(root);
        wallGo.transform.position = new Vector3(xc, 0, wallZ);
        var wall = wallGo.AddComponent<CanWall>();
        var probe = Spawn(canPrefab, null, 0, 1);
        var pb = GetBounds(probe);
        Object.DestroyImmediate(probe);
        float s = 0.11f * Scale / pb.size.y;
        float canH = pb.size.y * s, canD = Mathf.Max(pb.size.x, pb.size.z) * s;
        int cols = Mathf.FloorToInt(w / canD);
        float x0 = xc - cols * canD / 2 + canD / 2;
        for (int row = 0; row < 7; row++)
        for (int c = 0; c < cols; c++)
        {
            var can = Spawn(canPrefab, wallGo.transform, Random.Range(0f, 360f), s);
            PlaceOnFloor(can, new Vector3(x0 + c * canD, row * canH, wallZ));
            can.isStatic = false;
            AddBoxColliders(can);
            var rb = can.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.mass = 0.3f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        // The shelves' edges aren't perfectly straight; an invisible strip makes sure nothing slips past.
        var seal = wallGo.AddComponent<BoxCollider>();
        seal.center = new Vector3(0, canH * 3.5f, 0);
        seal.size = new Vector3(w + 2f, canH * 7f, canD * 0.5f); // CanWall switches it off when it topples

        // Seesaw: a ruler on a bottle cap.
        const float halfL = 11f, plankW = 6f, thick = 0.3f, capH = 0.6f;
        float pivotY = capH + thick / 2;
        float pivotZ = wallZ - canD / 2 - 0.4f - halfL;
        var pivot = new GameObject("Seesaw Pivot").transform;
        pivot.SetParent(root);
        pivot.position = new Vector3(xc, pivotY, pivotZ);
        var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plank.name = "Ruler";
        plank.transform.SetParent(pivot, false);
        plank.transform.localScale = new Vector3(plankW, thick, halfL * 2);
        plank.GetComponent<Renderer>().sharedMaterial = GetMaterial("Ruler", new Color(0.86f, 0.72f, 0.38f));
        var plankRb = plank.AddComponent<Rigidbody>();
        plankRb.isKinematic = true;
        // Ruler markings.
        var ink = GetMaterial("Ink", new Color(0.12f, 0.1f, 0.08f));
        for (int i = 0; i <= 20; i++)
        {
            var tick = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tick.name = "Tick";
            tick.transform.SetParent(pivot, false);
            float len = i % 5 == 0 ? 1.6f : 0.8f;
            tick.transform.localPosition = new Vector3(-plankW / 2 + len / 2 + 0.1f, thick / 2 + 0.01f, -halfL + 1f + i * (halfL * 2 - 2f) / 20f);
            tick.transform.localScale = new Vector3(len, 0.02f, 0.12f);
            tick.GetComponent<Renderer>().sharedMaterial = ink;
            Object.DestroyImmediate(tick.GetComponent<Collider>());
        }

        var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.name = "Bottle Cap";
        cap.transform.SetParent(root);
        cap.transform.SetPositionAndRotation(new Vector3(xc, capH / 2, pivotZ), Quaternion.Euler(0, 0, 90));
        cap.transform.localScale = new Vector3(capH, plankW * 0.55f, capH);
        cap.GetComponent<Renderer>().sharedMaterial = GetMaterial("BottleCap", new Color(0.8f, 0.1f, 0.1f));

        float tilt = Mathf.Asin(pivotY / halfL) * Mathf.Rad2Deg;
        pivot.localRotation = Quaternion.Euler(tilt, 0, 0); // far end down, as it starts in play
        pivot.gameObject.AddComponent<Seesaw>().Setup(pivot, halfL, plankW / 2, tilt, wall);

        HintCard(root, new Vector3(xc - plankW / 2 - 4f, 0, pivotZ - halfL + 1f), "TIP THE SCALES\nJUMP ON THE END");
    }

    // Puzzle 2: a ledge of flattened boxes is just too high to jump. A small box nearby can be
    // carried (E) and dropped against it as a step.
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
            float side = rng.Next(2) == 0 ? -1 : 1;
            PlaceOnFloor(p, new Vector3(xc + side * (w / 2 - 2f - (float)rng.NextDouble() * 4f), 0, ledgeZ - (float)rng.NextDouble() * 6f));
            AddBoxColliders(p);
        }

        HintCard(root, new Vector3(xc - w * 0.3f, 0, ledgeZ - 8f), "NEED A BOOST?\nPRESS E TO CARRY");
    }

    // Little cardboard tent card with a handwritten-style hint, readable walking north.
    static void HintCard(Transform parent, Vector3 floorAt, string text)
    {
        var card = new GameObject("Hint Card").transform;
        card.SetParent(parent);
        card.position = floorAt;
        Box("Card", card, floorAt + Vector3.up * 1.7f, new Vector3(5.5f, 3.4f, 0.1f), GetMaterial("Cardboard", new Color(0.72f, 0.56f, 0.36f)));
        MakeText(card, floorAt + new Vector3(0, 1.7f, -0.08f), 0, text, 5f, 2.8f, new Color(0.15f, 0.1f, 0.06f));
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

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// the high middle of level one: the wooden spoon up to west shelf 3, over the top of the shelf and back down, the
// three cans that drop the last ladder, and the paper plane that loops down to the long slide
static partial class GroceryStoreBuilder
{
    // the open patch on top of the west shelf, from the shelf's front edge back one board deep
    const float L1TopFrom = -8f, L1TopTo = 24f, L1TopSplit = 11f;
    // walls across west shelf 3 that make the route go up and over
    const float L1WallA = 0f, L1WallB = 18f;

    // the shelf's top and how far back from the aisle its back panel stands above the third board, off the model.
    // The boards are open above; the top of the gondola starts behind them, one panel's height over board 3
    static void MeasureShelfTop(Transform westAisle, ref Hall hall)
    {
        var up = UpwardTriangles(westAisle.gameObject);
        float top = 0f, back = 0f;
        foreach (float z in new[] { L1TopFrom + 3f, (L1TopFrom + L1TopTo) / 2, L1TopTo - 3f })
            for (float dx = 4f; dx < 14f; dx += 0.25f)
            {
                if (!SurfacesAt(up, hall.xW - dx, z).Any(h => h > ShelfHeight * 0.9f && h < ShelfHeight + 2f)) continue;
                // the top's front edge is bevelled, so read its height a little further back
                var tops = SurfacesAt(up, hall.xW - dx - 2f, z).Where(h => h > ShelfHeight * 0.9f && h < ShelfHeight + 2f).ToList();
                if (tops.Count > 0) top = Mathf.Max(top, tops.Max());
                back = Mathf.Max(back, dx - 0.25f);
                break;
            }
        hall.top = top > 0f ? top : ShelfHeight;
        hall.back = back > 0f ? back : hall.boardDepth + 1f;
        Debug.Log($"[LevelOne] west shelf top {hall.top:0.00}, back panel {hall.back:0.00} behind the edge, board 3 at {hall.h3:0.00}");
    }

    // the part of the shelf top that's walkable, from the back panel over board 3 to about the gondola's middle
    static float L1TopBack(Hall hall) => hall.xW - hall.back - 6.5f;

    // the seal over the first gondola, leaving a strip of its top open along level one's west side
    static void SealAboveLevelOne(Transform bounds, string name, float x0, float x1, float y, float h, float aisleLength)
    {
        var hall = levelOneHall;
        float hx = L1TopBack(hall), zS = -aisleLength / 2, zN = aisleLength / 2;
        Seal(bounds, name, new Vector3((x0 + x1) / 2, y, (zS + L1TopFrom) / 2), new Vector3(x1 - x0, h, L1TopFrom - zS));
        Seal(bounds, name, new Vector3((x0 + x1) / 2, y, (L1TopTo + zN) / 2), new Vector3(x1 - x0, h, zN - L1TopTo));
        Seal(bounds, name, new Vector3((x0 + hx) / 2, y, (L1TopFrom + L1TopTo) / 2), new Vector3(hx - x0, h, L1TopTo - L1TopFrom));
    }

    static void ShelfTopRoute(Transform level, Hall hall, System.Random rng)
    {
        float xW = hall.xW, T = hall.top, hx = L1TopBack(hall), face = xW - hall.back;
        var root = new GameObject("Shelf Top").transform;
        root.SetParent(level);

        // no dropping off the front of the top back onto board 3, and no walking from the first patch to the plane's
        Seal(root, "Shelf Top Guard", new Vector3(face + 0.1f, T + 1.7f, (L1TopFrom + L1TopTo) / 2), new Vector3(0.6f, 4f, L1TopTo - L1TopFrom));
        Seal(root, "Shelf Top Divider", new Vector3((hx + face + 0.4f) / 2, T + 3f, L1TopSplit), new Vector3(face + 0.4f - hx, 6.6f, 1f));
        // a tipped-over cereal box shows where the divider is
        var divider = CerealBox(root, 3, 0.62f);
        divider.transform.rotation = Quaternion.Euler(0, 0, 90);
        PlaceOnFloor(divider, new Vector3((hx + face) / 2, T, L1TopSplit));

        CubbyWall(root, hall, L1WallA, 1);
        CubbyWall(root, hall, L1WallB, 4);

        // a few boxes left up on top of the shelf
        CardboardBox(root, BoxKind.Sealed, 1.4f, 12f, new Vector3(face - 4.6f, T, -2f));
        CardboardBox(root, BoxKind.Ajar, 1.2f, -25f, new Vector3(face - 1.8f, T, 2.4f));
        CardboardBox(root, BoxKind.SealedLong, 1.3f, 70f, new Vector3(face - 5f, T, 6.4f));

        // against the back panel: up from the first part of board 3, back down to the middle part
        ShelfLadder(root, hall, -5f);
        ShelfLadder(root, hall, 7f);
    }

    // a ladder stood on board 3 against the back panel, up to the shelf top (and back down)
    static Transform ShelfLadder(Transform parent, Hall hall, float z)
    {
        float face = hall.xW - hall.back;
        return KitLadder(parent, new Vector3(face, 0, z), Vector3.right, hall.h3, hall.top, new Vector3(face - 2.4f, hall.top, z), down: true);
    }

    // a cereal box tipped over across board 3, from the back panel to the edge and nearly up to the shelf top, with an
    // invisible wall that reaches well out over the aisle so it can't be jumped round
    static void CubbyWall(Transform parent, Hall hall, float z, int style)
    {
        float h = (hall.top - hall.h3) * 0.92f, face = hall.xW - hall.back;
        var box = CerealBox(parent, style);
        box.name = "Cubby Wall";
        box.transform.rotation = Quaternion.Euler(0, 0, 90);
        box.transform.localScale = new Vector3(h / CerealW, (hall.back - 0.3f) / CerealH, 1f);
        PlaceOnFloor(box, new Vector3(hall.xW - hall.back / 2, hall.h3, z));
        float x0 = face - 0.3f, x1 = hall.xW + 7f, y0 = hall.h3 - 1f, y1 = hall.top - 0.3f;
        Seal(parent, "Cubby Wall Seal", new Vector3((x0 + x1) / 2, (y0 + y1) / 2, z), new Vector3(x1 - x0, y1 - y0, CerealD + 0.2f));
    }

    // a giant wooden spoon resting with its bowl on east shelf 2 and its handle on west shelf 3
    static void SpoonBridge(Transform parent, Vector3 bowlTip, Vector3 handleTip)
    {
        var root = new GameObject("Spoon Bridge").transform;
        root.SetParent(parent);
        var dir = handleTip - bowlTip;
        float len = dir.magnitude, thick = 0.45f;
        var frame = Quaternion.LookRotation(dir);
        var up = frame * Vector3.up;
        float handleW = 1.5f, bowlW = 4f, bowlLen = len * 0.3f;

        var prefab = LoadModel("k:cooking-spoon");
        if (prefab != null)
        {
            var holder = new GameObject("Spoon").transform;
            holder.SetParent(root, false);
            var probe = Spawn(prefab, holder, 0, 1);
            var mf = probe.GetComponentInChildren<MeshFilter>();
            var b = GetBounds(probe);
            bool alongX = b.size.x >= b.size.z;
            float bowlSide = 1f, modelHandle = 0f, modelBowl = 0f, modelBowlLen = 0.3f;
            if (mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable)
            {
                var m = mf.transform.localToWorldMatrix;
                var vs = mf.sharedMesh.vertices.Select(v => m.MultiplyPoint3x4(v)).ToList();
                float L(Vector3 v) => alongX ? v.x - b.center.x : v.z - b.center.z;
                float W(Vector3 v) => Mathf.Abs(alongX ? v.z - b.center.z : v.x - b.center.x);
                float half = (alongX ? b.size.x : b.size.z) / 2;
                float plus = vs.Where(v => L(v) > half * 0.5f).Select(W).DefaultIfEmpty(0).Max();
                float minus = vs.Where(v => L(v) < -half * 0.5f).Select(W).DefaultIfEmpty(0).Max();
                bowlSide = plus >= minus ? 1f : -1f;
                modelBowl = Mathf.Max(plus, minus) * 2;
                modelHandle = vs.Where(v => Mathf.Abs(L(v)) < half * 0.15f).Select(W).DefaultIfEmpty(0).Max() * 2;
                // where the bowl starts: the first slice from the bowl end narrower than most of the bowl
                float bowlStart = half;
                for (float f = 0.95f; f > -0.9f; f -= 0.05f)
                {
                    float w = vs.Where(v => Mathf.Abs(bowlSide * L(v) - f * half) < half * 0.05f).Select(W).DefaultIfEmpty(0).Max() * 2;
                    if (w < modelBowl * 0.45f && f < 0.8f) { bowlStart = f * half; break; }
                }
                modelBowlLen = (half - bowlStart) / (2 * half);
            }
            // turn the model so its handle points along +z, then stretch it to the span
            float yaw = alongX ? (bowlSide > 0 ? 90f : -90f) : (bowlSide > 0 ? 180f : 0f);
            Object.DestroyImmediate(probe);
            probe = Spawn(prefab, holder, yaw, 1);
            b = GetBounds(probe);
            probe.transform.position -= b.center;
            float sx = modelHandle > 0.001f ? Mathf.Min(handleW / modelHandle, 7.5f / Mathf.Max(modelBowl, 0.001f)) : 4f / b.size.x;
            holder.localScale = new Vector3(sx, thick / b.size.y, len / b.size.z);
            holder.SetPositionAndRotation((bowlTip + handleTip) / 2 + up * thick / 2, frame);
            handleW = modelHandle > 0.001f ? modelHandle * sx : 1.5f;
            bowlW = modelBowl > 0.001f ? modelBowl * sx : 4f;
            bowlLen = len * modelBowlLen;
            Debug.Log($"[LevelOne] spoon bridge: handle {handleW:0.00} wide, bowl {bowlW:0.00} wide x {bowlLen:0.0} long, span {len:0.0}");
            foreach (var r in holder.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = GetMaterial("Wooden Spoon", new Color(0.78f, 0.55f, 0.32f));
        }
        else
        {
            Box("Spoon Handle", root, (bowlTip + handleTip) / 2 + up * thick / 2, new Vector3(handleW, thick, len), GetMaterial("Wooden Spoon", new Color(0.78f, 0.55f, 0.32f)));
            root.GetChild(0).rotation = frame;
            Object.DestroyImmediate(root.GetChild(0).GetComponent<Collider>());
        }

        void Walk(string name, Vector3 center, Vector3 size)
        {
            var c = new GameObject(name).AddComponent<BoxCollider>();
            c.transform.SetParent(root);
            c.transform.SetPositionAndRotation(center, frame);
            c.size = size;
        }
        Walk("Handle Collider", (bowlTip + handleTip) / 2 + up * thick / 2, new Vector3(Mathf.Max(1.6f, handleW + 0.3f), thick, len));
        Walk("Bowl Collider", bowlTip + dir.normalized * bowlLen / 2 + up * thick / 2, new Vector3(bowlW * 0.85f, thick, bowlLen));
    }

    // three cans to stand on three painted spots on the middle part of west shelf 3; that drops the ladder up to
    // the shelf top where the paper plane waits
    static void CanPuzzle(Transform level, Hall hall)
    {
        float xW = hall.xW, h3 = hall.h3;
        var root = new GameObject("Puzzle - Can Spots").transform;
        root.SetParent(level);
        var empty = StoreAtmosphere.Emissive("Spot Empty", new Color(0.85f, 0.2f, 0.15f), 0.9f);
        var filled = StoreAtmosphere.Emissive("Spot Filled", new Color(0.2f, 0.85f, 0.3f), 0.9f);
        var ring = GetMaterial("Spot Ring", new Color(0.95f, 0.93f, 0.85f));

        var spots = new List<Transform>();
        var marks = new List<Renderer>();
        foreach (float z in new[] { 9f, 12f, 15f })
        {
            var at = new Vector3(xW - 4.6f, h3, z);
            var spot = Waypoint(root, "Can Spot", at, 0);
            var outer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outer.name = "Spot Ring";
            outer.transform.SetParent(spot, false);
            outer.transform.localPosition = Vector3.up * 0.02f;
            outer.transform.localScale = new Vector3(2.3f, 0.02f, 2.3f);
            outer.GetComponent<Renderer>().sharedMaterial = ring;
            Object.DestroyImmediate(outer.GetComponent<Collider>());
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Spot";
            disc.transform.SetParent(spot, false);
            disc.transform.localPosition = Vector3.up * 0.045f;
            disc.transform.localScale = new Vector3(1.9f, 0.02f, 1.9f);
            disc.GetComponent<Renderer>().sharedMaterial = empty;
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            spots.Add(spot);
            marks.Add(disc.GetComponent<Renderer>());
        }

        var canPrefab = BeanCanBuilder.GetOrCreate();
        var probe = Spawn(canPrefab, null, 0, 1);
        var pb = GetBounds(probe);
        Object.DestroyImmediate(probe);
        var cans = new List<Carryable>();
        foreach (var (at, yaw) in new[] { (new Vector3(xW - 1.8f, h3, 2.6f), 30f), (new Vector3(xW - 5.6f, h3, 4.4f), 140f), (new Vector3(xW - 1.6f, h3, 11.5f), 260f) })
        {
            var can = Spawn(canPrefab, root, yaw, 1.3f / pb.size.y);
            can.name = "Puzzle Can";
            PlaceOnFloor(can, at);
            AddBoxColliders(can);
            foreach (var t in can.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
            cans.Add(can.AddComponent<Carryable>());
        }

        var ladder = ShelfLadder(root, hall, 15f);
        if (ladder != null) foreach (var t in ladder.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
        root.gameObject.AddComponent<CanSpots>().Setup(spots.ToArray(), marks.ToArray(), empty, filled, cans.ToArray(), ladder, h3 - 3f);
    }

    // the paper plane on top of the shelf, and its flight: out over the aisle, one and a half turns round to the
    // left while it sinks, then back in to land on west shelf 3 beside the slide
    static void PaperPlaneRide(Transform level, Hall hall, Vector3 landing)
    {
        float xW = hall.xW, T = hall.top, xc = hall.x;
        float face = xW - hall.back;
        var home = new Vector3(face - 3.8f, T, 18.5f);
        var root = new GameObject("Paper Plane").transform;
        root.SetParent(level);
        root.SetPositionAndRotation(home, Quaternion.LookRotation(Vector3.right));

        var paper = new GameObject("Plane").transform;
        paper.SetParent(root, false);
        var visual = new GameObject("Paper");
        visual.transform.SetParent(paper, false);
        visual.AddComponent<MeshFilter>().sharedMesh = PaperPlaneMesh();
        var r = visual.AddComponent<MeshRenderer>();
        var mat = GetMaterial("Paper", new Color(0.95f, 0.95f, 0.91f));
        mat.SetFloat("_Smoothness", 0.1f);
        r.sharedMaterial = mat;
        const float deck = 0.62f;
        var col = paper.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0, deck - 0.15f, -0.4f);
        col.size = new Vector3(2.6f, 0.3f, 5.6f);
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = false;

        var trigger = root.gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0, deck + 1f, -0.4f);
        trigger.size = new Vector3(2.4f, 2f, 5.2f);

        // the loop: centre over the aisle, clear of the slide's hanging wires and the east shelves
        var centre = new Vector3(xc + 1f, 0, 39f);
        const float radius = 10.5f;
        var route = new List<Vector3>
        {
            home,
            new Vector3(face + 2f, T + 0.5f, 18.8f),
            new Vector3(xW + 2.5f, T + 0.4f, 19.3f),
            new Vector3(xW + 9f, T + 0.2f, 21.5f),
        };
        const float a0 = -45f, a1 = 540f;
        for (float a = a0; a <= a1 + 0.1f; a += 22.5f)
        {
            float k = (a - a0) / (a1 - a0);
            float rad = a * Mathf.Deg2Rad;
            route.Add(new Vector3(centre.x + Mathf.Cos(rad) * radius, Mathf.Lerp(T - 1.5f, hall.h3 + 3f, k), centre.z + Mathf.Sin(rad) * radius));
        }
        route.Add(new Vector3(xW + 9f, hall.h3 + 2.2f, landing.z + 3f));
        route.Add(new Vector3(xW + 3f, hall.h3 + 0.9f, landing.z + 0.5f));
        route.Add(new Vector3(landing.x, landing.y + 0.15f, landing.z));
        root.gameObject.AddComponent<PaperPlane>().Setup(paper, route.ToArray(), deck, Vector3.forward);
    }

    // a folded paper dart, nose along +z: two wings meeting at a raised spine and a keel folded down under it,
    // every face doubled so it reads from both sides
    static Mesh PaperPlaneMesh()
    {
        const string path = "Assets/Models/PaperPlane.asset";
        AssetDatabase.DeleteAsset(path);
        const float len = 7f, span = 5.2f;
        var nose = new Vector3(0, 0.55f, len / 2);
        var tail = new Vector3(0, 0.6f, -len / 2);
        var keelTail = new Vector3(0, -0.15f, -len / 2 + 0.3f);
        var keelNose = new Vector3(0, 0.45f, len / 2 - 1.2f);
        var verts = new List<Vector3>();
        var tris = new List<int>();
        void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            foreach (var (p, q, s) in new[] { (a, b, c), (a, c, b) })
            {
                int i = verts.Count;
                verts.Add(p); verts.Add(q); verts.Add(s);
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            }
        }
        foreach (int side in new[] { -1, 1 })
        {
            var tip = new Vector3(side * span / 2, 0.85f, -len / 2);
            var fold = new Vector3(side * span * 0.22f, 0.66f, -len / 2);
            var spine = new Vector3(side * 0.04f, 0, 0);
            Tri(nose + spine, tail + spine, fold);
            Tri(nose + spine, fold, tip);
            Tri(keelNose + spine, keelTail + spine, tail + spine);
            Tri(nose + spine, keelNose + spine, tail + spine);
        }
        var mesh = new Mesh { name = "Paper Plane" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    static void AddPickupOutlines(Transform store)
    {
        const string path = "Assets/Materials/Pickup Outline.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("JameGam/Pickup Outline");
            if (shader == null)
            {
                Debug.LogWarning("[GroceryStoreBuilder] Pickup outline shader not found; no outlines.");
                return;
            }
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_Color", Color.white);
        mat.SetFloat("_Width", 1.6f);
        EditorUtility.SetDirty(mat);
        foreach (var c in store.GetComponentsInChildren<Carryable>(true))
            if (c.GetComponent<PickupOutline>() == null) c.gameObject.AddComponent<PickupOutline>().Setup(mat);
    }
}

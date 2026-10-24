using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

static partial class GroceryStoreBuilder
{
    static void BuildBoss(Transform store, List<float> walkways, float aisleLength, float depth, GameObject shelfPrefab, ShelfInfo shelf)
    {
        var root = new GameObject("Boss - VAC-3000").transform;
        root.SetParent(store);
        float x = walkways[4], half = WalkwayWidth / 2;
        float zS = -aisleLength / 2, zN = aisleLength / 2;
        float edge = x - half;
        float wallZ = depth / 2 - CoolerDepth;

        int units = Mathf.Max(1, Mathf.RoundToInt(aisleLength / shelf.length));
        var aisle = BuildAisle(5, store, edge - shelf.gondolaDepth / 2, units, aisleLength, shelfPrefab, shelf);
        StockAisle(aisle, shelf, aisleLength, Stock[Stock.Length - 1], sides: new[] { 1 });
        HangAisleSign(store, x, zS, 5, "CLEANING SUPPLIES", 0);

        EndBlock(root, "South Block", edge - shelf.gondolaDepth, edge, -wallZ, zS, out _);
        var gate = EndBlock(root, "Exit Gate", edge - shelf.gondolaDepth, edge, zN, wallZ, out var gateSeal);

        CheckpointAt(root, "Checkpoint Boss", new Vector3(x, 0, zS + 6f), new Vector3(WalkwayWidth, 4, 3), 0);
        devWarps.Add(("Boss", new Vector3(x, 1, zS + 6f), 0));

        var rng = new System.Random(808);
        var spots = new List<Vector2>();
        float zMid = (zS + zN) / 2;
        var can = BeanCanBuilder.GetOrCreate();
        var crate = LoadKenney("cardboardBoxClosed");
        for (int tries = 0; spots.Count < 16 && tries < 400; tries++)
        {
            var p = new Vector2(x + ((float)rng.NextDouble() - 0.5f) * (WalkwayWidth - 10f), Mathf.Lerp(zS + 22f, zN - 12f, (float)rng.NextDouble()));
            if (Vector2.Distance(p, new Vector2(x, zMid)) < 12f) continue;
            bool clear = true;
            foreach (var s in spots) if (Vector2.Distance(s, p) < 11f) clear = false;
            if (!clear) continue;
            spots.Add(p);
            var at = new Vector3(p.x, 0, p.y);
            int kind = spots.Count % 3;
            if (kind == 0)
            {
                var b = CerealBox(root, rng.Next(6));
                b.transform.rotation = Quaternion.Euler(0, rng.Next(360), 0);
                PlaceOnFloor(b, at);
            }
            else if (kind == 1 && can != null)
            {
                for (int k = 0; k < 2; k++)
                {
                    var c = Spawn(can, root, rng.Next(360), 1);
                    var cb = GetBounds(c);
                    c.transform.localScale *= 3.6f / cb.size.y;
                    PlaceOnFloor(c, at + Vector3.up * (k * 3.6f));
                    AddBoxColliders(c);
                }
            }
            else if (crate != null)
            {
                float y = 0;
                for (int k = 0; k < 2; k++)
                {
                    var c = SpawnSized(crate, root, 4.2f, rng.Next(360));
                    PlaceOnFloor(c, at + Vector3.up * y);
                    AddBoxColliders(c);
                    y = GetBounds(c).max.y;
                }
            }
        }

        var vac = BuildVacuum(root, new Vector3(x, 0, zMid), out var parts);
        float r = 3.1f;
        var boss = vac.gameObject.AddComponent<RoombaBoss>();
        boss.Setup(parts.eye, parts.fan, parts.button, parts.led, parts.light, parts.brushes, r, 1.46f,
            new Vector2(x - half + r + 0.4f, x + half - r - 0.4f), new Vector2(zS + 16f, zN - 8f), zS + 18f,
            x - half - 3f, x + half + 3f, gate.gameObject, gateSeal, new[] { Glyph("game"), Glyph("run"), Glyph("bulb"), Glyph("star") });

        var door = GameObject.Find("Sliding Door");
        if (door != null)
        {
            var end = new GameObject("End Credits Trigger");
            end.transform.SetParent(root);
            end.transform.position = door.transform.position + new Vector3(4f, 6f, 0f);
            var box = end.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(8f, 12f, DoorWidth);
            var titleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BebasNeue-Regular.ttf");
            var bodyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Oswald.ttf") ?? titleFont;
            end.AddComponent<EndCredits>().Setup(titleFont, bodyFont, Vector3.left);
            devWarps.Add(("Exit", door.transform.position + new Vector3(24f, 1f, 0f), -90));
        }
    }

    static Transform EndBlock(Transform parent, string name, float x0, float x1, float z0, float z1, out Collider seal)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent);
        float zLo = Mathf.Min(z0, z1), zHi = Mathf.Max(z0, z1);
        Seal(root, name + " Seal", new Vector3((x0 + x1) / 2, WallHeight / 2, (zLo + zHi) / 2), new Vector3(x1 - x0, WallHeight, zHi - zLo));
        seal = root.GetChild(root.childCount - 1).GetComponent<Collider>();
        var crate = LoadKenney("cardboardBoxClosed");
        if (crate == null) return root;
        var rng = new System.Random(name.Length * 31);
        float size = 0.45f * Scale;
        int nx = Mathf.Max(1, Mathf.FloorToInt((x1 - x0) / size)), nz = Mathf.Max(1, Mathf.FloorToInt((zHi - zLo) / size));
        for (int i = 0; i < nx; i++)
        for (int j = 0; j < nz; j++)
        {
            float y = 0;
            int h = rng.Next(2, 5);
            for (int k = 0; k < h; k++)
            {
                var b = SpawnSized(crate, root, size * 0.95f, rng.Next(-12, 12));
                PlaceOnFloor(b, new Vector3(x0 + (i + 0.5f) * (x1 - x0) / nx, y, zLo + (j + 0.5f) * (zHi - zLo) / nz));
                AddBoxColliders(b);
                y = GetBounds(b).max.y;
            }
        }
        return root;
    }

    struct VacParts
    {
        public Transform eye, fan;
        public Renderer button, led;
        public Light light;
        public Transform[] brushes;
    }

    static Transform BuildVacuum(Transform parent, Vector3 at, out VacParts parts)
    {
        var root = new GameObject("VAC-3000").transform;
        root.SetParent(parent);
        root.SetPositionAndRotation(at, Quaternion.Euler(0, 180, 0));
        var body = GetMaterial("Vac Body", new Color(0.13f, 0.14f, 0.16f));
        body.SetFloat("_Smoothness", 0.7f);
        var bumper = GetMaterial("Vac Bumper", new Color(0.32f, 0.33f, 0.36f));
        var plate = GetMaterial("Vac Plate", new Color(0.72f, 0.74f, 0.78f));
        plate.SetFloat("_Metallic", 0.8f);
        plate.SetFloat("_Smoothness", 0.65f);
        var dark = GetMaterial("Vac Panel", new Color(0.06f, 0.06f, 0.07f));
        dark.SetFloat("_Smoothness", 0.9f);
        var bristle = GetMaterial("Vac Bristle", new Color(0.95f, 0.8f, 0.25f));
        var buttonMat = GetMaterial("Vac Button", new Color(0.2f, 0.2f, 0.22f));
        buttonMat.EnableKeyword("_EMISSION");
        var ledMat = GetMaterial("Vac LED", new Color(1f, 0.3f, 0.2f));
        ledMat.EnableKeyword("_EMISSION");

        Transform P(PrimitiveType type, string name, Transform p, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default)
        {
            var g = GameObject.CreatePrimitive(type);
            g.name = name;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(p, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(euler);
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }
        var Y = PrimitiveType.Cylinder;
        P(Y, "Body", root, new Vector3(0, 0.72f, 0), new Vector3(6.2f, 0.6f, 6.2f), body);
        P(Y, "Bumper", root, new Vector3(0, 0.55f, 0), new Vector3(6.45f, 0.28f, 6.45f), bumper);
        P(Y, "Top Plate", root, new Vector3(0, 1.34f, 0), new Vector3(5.7f, 0.04f, 5.7f), plate);
        P(Y, "Button Ring", root, new Vector3(0, 1.38f, 0), new Vector3(2.6f, 0.04f, 2.6f), dark);
        var button = P(Y, "Power Button", root, new Vector3(0, 1.42f, 0), new Vector3(1.4f, 0.06f, 1.4f), buttonMat);
        P(PrimitiveType.Cube, "Power Mark", root, new Vector3(0, 1.49f, 0.18f), new Vector3(0.12f, 0.02f, 0.5f), plate);
        P(PrimitiveType.Cube, "Sensor Window", root, new Vector3(0, 0.98f, 2.92f), new Vector3(2.8f, 0.38f, 0.4f), dark);
        var led = P(PrimitiveType.Sphere, "Eye LED", root, new Vector3(0, 1.0f, 3.12f), Vector3.one * 0.32f, ledMat);
        var brushes = new List<Transform>();
        foreach (int s in new[] { -1, 1 })
        {
            var hub = P(Y, "Side Brush", root, new Vector3(s * 2.1f, 0.1f, 2.3f), new Vector3(0.45f, 0.05f, 0.45f), dark);
            for (int i = 0; i < 3; i++)
            {
                var arm = new GameObject("Bristles").transform;
                arm.SetParent(hub, false);
                arm.localRotation = Quaternion.Euler(0, i * 120f, 0);
                P(PrimitiveType.Cube, "Tuft", arm, new Vector3(0, 0, 1.6f), new Vector3(0.12f, 0.6f, 3.2f), bristle);
            }
            brushes.Add(hub);
        }

        var solid = new GameObject("Collider").transform;
        solid.SetParent(root, false);
        solid.localPosition = new Vector3(0, 0.73f, 0);
        solid.localScale = new Vector3(6.2f, 0.73f, 6.2f);
        var mc = solid.gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
        mc.convex = true;

        var eye = new GameObject("Eye").transform;
        eye.SetParent(root, false);
        eye.localPosition = new Vector3(0, 1.15f, 3.35f);

        var fan = new GameObject("Vision Beam", typeof(MeshFilter), typeof(MeshRenderer)).transform;
        fan.SetParent(root, false);
        var fr = fan.GetComponent<MeshRenderer>();
        fr.sharedMaterial = UnlitTransparent("Vision Beam", new Color(1f, 0.85f, 0.2f, 0.32f));
        fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fr.receiveShadows = false;

        var light = new GameObject("Status Light").AddComponent<Light>();
        light.transform.SetParent(root, false);
        light.transform.localPosition = new Vector3(0, 2.4f, 1.5f);
        light.type = LightType.Point;
        light.range = 12f;
        light.intensity = 18f;
        light.shadows = LightShadows.None;

        foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
        Kinematic(root.gameObject);
        parts = new VacParts
        {
            eye = eye, fan = fan, button = button.GetComponent<Renderer>(), led = led.GetComponent<Renderer>(),
            light = light, brushes = brushes.ToArray(),
        };
        return root;
    }

    static Material UnlitTransparent(string name, Color color)
    {
        string path = $"{MatFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0);
        mat.SetFloat("_Cull", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
        mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

static partial class GroceryStoreBuilder
{
    const string GrimeFolder = MatFolder + "/Grime";

    enum GrimeKind { Dirt, Mold, Stain }

    static void MakeProductsMatte(Transform root)
    {
        const string folder = MatFolder + "/Matte";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(MatFolder, "Matte");
        var copies = new Dictionary<Material, Material>();

        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;
                string path = AssetDatabase.GetAssetPath(m);
                // a model part that lost its material (the toaster, the door frame) falls back to URP's default
                if (path.StartsWith("Packages/") && m.name == "Lit")
                {
                    var steel = GetMaterial("Brushed Steel", new Color(0.62f, 0.64f, 0.67f));
                    steel.SetFloat("_Metallic", 0.75f);
                    steel.SetFloat("_Smoothness", 0.42f);
                    mats[i] = steel;
                    changed = true;
                    continue;
                }
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;

                if (!copies.TryGetValue(m, out var matte))
                {
                    string model = System.IO.Path.GetFileNameWithoutExtension(path);
                    string copyPath = $"{folder}/{model}_{m.name}.mat";
                    matte = AssetDatabase.LoadAssetAtPath<Material>(copyPath);
                    if (matte == null)
                    {
                        matte = new Material(m);
                        AssetDatabase.CreateAsset(matte, copyPath);
                    }
                    matte.SetFloat("_Smoothness", 0.12f);
                    matte.SetFloat("_Metallic", 0f);
                    matte.SetFloat("_EnvironmentReflections", 0.3f);
                    matte.SetFloat("_Cull", 0f);
                    // the supermarket pack uses the 3ds Max physical shader, which ignores _Smoothness: its
                    // reflections came in at roughness 0, so shelves, counters and carts shone like wet plastic
                    // and the few metal parts were mirrors
                    if (matte.HasProperty("_REFLECTIONS_ROUGHNESS"))
                    {
                        bool metal = matte.HasProperty("_METALNESS") && matte.GetFloat("_METALNESS") > 0.5f;
                        matte.SetFloat("_REFLECTIONS_ROUGHNESS", metal ? 0.5f : 0.72f);
                        matte.SetFloat("_REFLECTIONS_WEIGHT", metal ? 0.8f : 0.45f);
                    }
                    matte.doubleSidedGI = true;
                    EditorUtility.SetDirty(matte);
                    copies[m] = matte;
                }
                mats[i] = matte;
                changed = true;
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    // URP's default smoothness of 0.5 gives every plain material a hard white highlight under the store's lights.
    // Paper, card, wood, sponge and painted walls get a proper dull finish; plastics and metal keep a little sheen
    static readonly (string name, float smooth, float metal)[] Finishes =
    {
        ("Wall", 0.12f, 0f), ("Sidewalk", 0.08f, 0f), ("LightFixture", 0.3f, 0f),
        ("Cardboard Box", 0.1f, 0f), ("Cardboard Box Inside", 0.05f, 0f), ("Cardboard Insert", 0.05f, 0f),
        ("Receipt", 0.06f, 0f), ("Paper Plate", 0.08f, 0f), ("Paper Plate Rim", 0.08f, 0f), ("Ink", 0.12f, 0f),
        ("PriceTag", 0.22f, 0f), ("PriceRail", 0.3f, 0f), ("SignFrame", 0.3f, 0f), ("SignDisc", 0.25f, 0f),
        ("Talker Red", 0.2f, 0f), ("Talker Yellow", 0.2f, 0f), ("Talker Green", 0.2f, 0f), ("Talker Orange", 0.2f, 0f),
        ("Cereal 0 Body Color", 0.2f, 0f), ("Cereal 1 Body Color", 0.2f, 0f), ("Cereal 2 Body Color", 0.2f, 0f),
        ("Cereal 3 Body Color", 0.2f, 0f), ("Cereal 4 Body Color", 0.2f, 0f), ("Cereal 5 Body Color", 0.2f, 0f),
        ("Cracker", 0.06f, 0f), ("Cracker Dots", 0.04f, 0f), ("Sponge Yellow", 0.03f, 0f), ("Sponge Green", 0.03f, 0f),
        ("Mop Strands", 0.03f, 0f), ("Mop Handle", 0.4f, 0f), ("Mop Socket", 0.35f, 0f), ("Bucket Grey", 0.3f, 0f),
        ("Broom Handle", 0.2f, 0f), ("Broom Block", 0.35f, 0f), ("Ruler", 0.28f, 0f), ("Caution Yellow", 0.35f, 0f),
        ("Spot Ring", 0.15f, 0f), ("Popsicle Wood", 0.1f, 0f), ("Chopstick Wood", 0.15f, 0f), ("Toothpick Wood", 0.1f, 0f),
        ("Wooden Spoon", 0.18f, 0f), ("Floss", 0.3f, 0f), ("Wire", 0.35f, 0f), ("Ladder Hook", 0.35f, 0.3f),
        ("Freezer Body", 0.3f, 0f), ("Freezer Trim", 0.3f, 0f), ("CoolerBody", 0.3f, 0f), ("CoolerShelf", 0.35f, 0.4f),
        ("Vac Bumper", 0.25f, 0f), ("Vac Bristle", 0.05f, 0f), ("Bread Clip Blue", 0.4f, 0f),
        ("Jar Lid Rim", 0.35f, 0.4f), ("Matchbox", 0.15f, 0f), ("Strike Strip", 0.05f, 0f),
        ("Matchbox Label", 0.15f, 0f), ("Dirt Path", 0.03f, 0f), ("Forest Floor", 0.03f, 0f), ("Tutorial Light Panel", 0.3f, 0f),
    };

    static void TuneFinishes()
    {
        foreach (var (name, smooth, metal) in Finishes)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>($"{MatFolder}/{name}.mat");
            if (m == null) continue;
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            EditorUtility.SetDirty(m);
        }
    }

    static void AddGrime(Transform store, float left, float right, float depth)
    {
        var root = new GameObject("Grime").transform;
        root.SetParent(store);
        var rng = new System.Random(77);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        var dirt = GrimeMaterial(GrimeKind.Dirt);
        var mold = GrimeMaterial(GrimeKind.Mold);
        int layer = 0;

        for (int i = 0; i < 90; i++)
        {
            float s = R(0.6f, 2.8f) * Scale;
            Decal(root, dirt, new Vector3(R(left, right), 0.02f + (layer++ % 40) * 0.002f, R(-depth / 2, depth / 2)),
                Quaternion.Euler(90, R(0, 360), 0), new Vector2(s, s * R(0.6f, 1.4f)));
        }
        var stains = new Material[StainVariants];
        for (int v = 0; v < StainVariants; v++) stains[v] = StainMaterial(v);
        for (int c = 0; c < 14; c++)
        {
            var centre = new Vector2(R(left, right), R(-depth / 2, depth / 2));
            int count = rng.Next(1, 5);
            for (int i = 0; i < count; i++)
            {
                float s = (i == 0 ? R(1.6f, 3.6f) : R(0.5f, 1.6f)) * Scale;
                var at = centre + new Vector2(R(-1f, 1f), R(-1f, 1f)) * (i == 0 ? 0 : 1.6f * Scale);
                Decal(root, stains[rng.Next(StainVariants)], new Vector3(at.x, WallHeight - 0.02f - (layer++ % 40) * 0.002f, at.y),
                    Quaternion.Euler(-90, R(0, 360), 0), new Vector2(s, s * R(0.65f, 1.35f)));
            }
        }
        var walls = new[]
        {
            (from: new Vector3(left, 0, depth / 2), to: new Vector3(right, 0, depth / 2), yaw: 0f),
            (from: new Vector3(left, 0, -depth / 2), to: new Vector3(right, 0, -depth / 2), yaw: 180f),
            (from: new Vector3(right, 0, -depth / 2), to: new Vector3(right, 0, depth / 2), yaw: 90f),
            (from: new Vector3(left, 0, -depth / 2), to: new Vector3(left, 0, depth / 2), yaw: -90f),
        };
        foreach (var w in walls)
        {
            var inward = Quaternion.Euler(0, w.yaw, 0) * Vector3.back;
            for (int i = 0; i < 14; i++)
            {
                bool high = rng.NextDouble() < 0.45;
                float s = R(0.7f, 2.4f) * Scale;
                var at = Vector3.Lerp(w.from, w.to, R(0.02f, 0.98f));
                at.y = high ? WallHeight - s * R(0.2f, 0.45f) : s * R(0.2f, 0.45f);
                Decal(root, mold, at + inward * (0.03f + (layer++ % 40) * 0.002f), Quaternion.Euler(0, w.yaw, R(0, 360)), new Vector2(s, s * R(0.7f, 1.2f)));
            }
        }
    }

    static void Decal(Transform parent, Material mat, Vector3 pos, Quaternion rot, Vector2 size)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = mat.name;
        q.transform.SetParent(parent);
        q.transform.SetPositionAndRotation(pos, rot);
        q.transform.localScale = new Vector3(size.x, size.y, 1);
        Object.DestroyImmediate(q.GetComponent<Collider>());
        var r = q.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static Material GrimeMaterial(GrimeKind kind)
    {
        if (!AssetDatabase.IsValidFolder(GrimeFolder)) AssetDatabase.CreateFolder(MatFolder, "Grime");
        string texPath = $"{GrimeFolder}/{kind}.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            int seed = (int)kind * 101 + 7;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size - 0.5f, v = y / (float)size - 0.5f;
                float r = Mathf.Sqrt(u * u + v * v) * 2f;
                float n = Fbm(x / 40f, y / 40f, seed);
                float fine = Fbm(x / 9f, y / 9f, seed + 3);
                float a;
                Color c;
                switch (kind)
                {
                    case GrimeKind.Dirt:
                        a = Mathf.Clamp01((1f - r) * 1.6f + (n - 0.5f) * 1.4f - 0.25f) * (0.55f + 0.45f * fine) * 0.75f;
                        c = Color.Lerp(new Color(0.16f, 0.13f, 0.1f), new Color(0.3f, 0.25f, 0.18f), fine);
                        break;
                    case GrimeKind.Mold:
                        float spots = Mathf.Clamp01((fine - 0.45f) * 4f);
                        a = Mathf.Clamp01((1f - r) * 1.3f + (n - 0.5f) * 1.6f - 0.15f) * Mathf.Lerp(0.35f, 1f, spots) * 0.85f;
                        c = Color.Lerp(new Color(0.12f, 0.16f, 0.08f), new Color(0.03f, 0.05f, 0.03f), spots);
                        break;
                    default:
                        float ring = Mathf.Exp(-Mathf.Pow((r + (n - 0.5f) * 0.35f - 0.8f) * 7f, 2f));
                        a = Mathf.Clamp01(ring * 0.8f + Mathf.Clamp01(0.8f - r) * 0.25f) * (0.7f + 0.3f * fine);
                        c = Color.Lerp(new Color(0.55f, 0.42f, 0.22f), new Color(0.35f, 0.24f, 0.1f), ring);
                        break;
                }
                c.a = a;
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

        var mat = StoreAtmosphere.Glass($"Grime_{kind}", Color.white);
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        mat.SetFloat("_Smoothness", kind == GrimeKind.Mold ? 0.05f : 0.15f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    const int StainVariants = 6;

    static Material StainMaterial(int variant)
    {
        if (!AssetDatabase.IsValidFolder(GrimeFolder)) AssetDatabase.CreateFolder(MatFolder, "Grime");
        string texPath = $"{GrimeFolder}/Stain_{variant}.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
        {
            const int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var rng = new System.Random(variant * 7919 + 13);
            int seed = variant * 131 + 5;
            Color[] tints =
            {
                new Color(0.55f, 0.42f, 0.2f), new Color(0.48f, 0.3f, 0.14f), new Color(0.42f, 0.38f, 0.3f),
                new Color(0.6f, 0.48f, 0.25f), new Color(0.38f, 0.26f, 0.16f), new Color(0.5f, 0.4f, 0.32f),
            };
            var tint = tints[variant % tints.Length];
            int rings = 2 + variant % 3;
            float lumpiness = 0.25f + 0.08f * (variant % 4);
            var blots = new List<Vector3>();
            for (int i = 0; i < 3 + variant % 3; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2, d = 0.55f + (float)rng.NextDouble() * 0.25f;
                blots.Add(new Vector3(Mathf.Cos(a) * d, Mathf.Sin(a) * d, 0.08f + (float)rng.NextDouble() * 0.1f));
            }
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x / (float)size - 0.5f) * 2f, v = (y / (float)size - 0.5f) * 2f;
                float wu = u + (Fbm(x / 70f, y / 70f, seed) - 0.5f) * lumpiness * 2f;
                float wv = v + (Fbm(x / 70f + 9f, y / 70f + 4f, seed) - 0.5f) * lumpiness * 2f;
                float r = Mathf.Sqrt(wu * wu + wv * wv) / 0.75f;
                float fine = Fbm(x / 12f, y / 12f, seed + 3);

                float fill = Mathf.Clamp01((1f - r) * 3f) * 0.22f;
                float ringA = 0f;
                for (int k = 0; k < rings; k++)
                {
                    float rr = 1f - k * (0.55f / rings);
                    ringA = Mathf.Max(ringA, Mathf.Exp(-Mathf.Pow((r - rr) * (18f + k * 6f), 2f)) * (1f - k * 0.18f));
                }
                float blot = 0f;
                foreach (var bl in blots)
                {
                    float br = Mathf.Sqrt((u - bl.x) * (u - bl.x) + (v - bl.y) * (v - bl.y)) / bl.z;
                    blot = Mathf.Max(blot, Mathf.Clamp01(1f - br) * 0.5f + Mathf.Exp(-Mathf.Pow((br - 1f) * 6f, 2f)) * 0.4f);
                }
                float a = Mathf.Clamp01(fill + ringA * 0.85f + blot) * (0.75f + 0.25f * fine);
                var c = Color.Lerp(tint * 1.15f, tint * 0.6f, Mathf.Clamp01(ringA + blot * 0.5f));
                c.a = a;
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
        var mat = StoreAtmosphere.Glass($"Grime_Stain_{variant}", Color.white);
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        mat.SetFloat("_Smoothness", 0.05f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static float Fbm(float x, float y, int seed)
    {
        float sum = 0, amp = 0.5f;
        for (int o = 0; o < 4; o++)
        {
            sum += amp * ValueNoise(x, y, seed + o * 17);
            x *= 2f; y *= 2f; amp *= 0.5f;
        }
        return sum / 0.9375f;
    }

    static float ValueNoise(float x, float y, int seed)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float xf = x - xi, yf = y - yi;
        float Hash(int a, int b)
        {
            unchecked
            {
                int h = a * 374761393 + b * 668265263 + seed * 982451653;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }
        float sx = xf * xf * (3 - 2 * xf), sy = yf * yf * (3 - 2 * yf);
        float top = Mathf.Lerp(Hash(xi, yi), Hash(xi + 1, yi), sx);
        float bottom = Mathf.Lerp(Hash(xi, yi + 1), Hash(xi + 1, yi + 1), sx);
        return Mathf.Lerp(top, bottom, sy);
    }
}

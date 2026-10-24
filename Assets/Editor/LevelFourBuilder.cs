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
            var go = Spawn(prefab, templates, t.yaw, t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z) * 1.3f);
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

        TutorialZoneAt(root, new Vector3(x, 5, zN - 4f), new Vector3(WalkwayWidth, 10, 10f), "run", "RUN!",
            "The shelves are giving way! Red outlines show where things are about to land. Keep moving, and save your dash for close calls.", "SHIFT", "WASD");
        CheckpointAt(root, "Checkpoint L4-A", new Vector3(x, 0, zN * 0.35f), new Vector3(WalkwayWidth, 4, 3), 180);
        CheckpointAt(root, "Checkpoint L4-B", new Vector3(x, 0, zS * 0.35f), new Vector3(WalkwayWidth, 4, 3), 180);
        ShelfGuards(root, x, zS, zN);

        var rng = new System.Random(404);
        var debris = Measure(Stock[2].Concat(Stock[3]).ToArray());
        for (int i = 0; i < 40 && debris.Count > 0; i++)
        {
            var t = debris[rng.Next(debris.Count)];
            var p = Spawn(t.prefab, root, rng.Next(360), t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z));
            p.transform.rotation = Quaternion.Euler(50 + rng.Next(40), rng.Next(360), rng.Next(30)) * p.transform.rotation;
            float side = rng.Next(2) == 0 ? -1 : 1;
            PlaceOnFloor(p, new Vector3(x + side * (half - 1f - (float)rng.NextDouble() * 4f), 0, Mathf.Lerp(zS + 6f, zN - 6f, (float)rng.NextDouble())));
            SettleLater(p);
        }
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

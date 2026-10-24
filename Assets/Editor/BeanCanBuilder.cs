using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Generates a flat-shaded low-poly can of beans (matching the Supermarket pack's style) and saves
// it as Assets/Models/BeanCan.prefab. Menu: JameGam > Create Bean Can; the store builder also
// calls GetOrCreate() when it needs one.
static class BeanCanBuilder
{
    const string Folder = "Assets/Models";
    const string PrefabPath = Folder + "/BeanCan.prefab";
    const int Sides = 12;

    [MenuItem("JameGam/Create Bean Can")]
    static void CreateMenu()
    {
        AssetDatabase.DeleteAsset(PrefabPath);
        Selection.activeObject = GetOrCreate();
    }

    public static GameObject GetOrCreate()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets", "Models");

        var mesh = BuildMesh();
        AssetDatabase.CreateAsset(mesh, $"{Folder}/BeanCan_Mesh.asset");

        var mats = new[]
        {
            Mat("BeanCan_Metal", new Color(0.72f, 0.74f, 0.78f), 0.6f, 0.7f),
            Mat("BeanCan_Label", new Color(0.85f, 0.32f, 0.12f), 0f, 0.2f),
            Mat("BeanCan_Stripe", new Color(0.98f, 0.9f, 0.7f), 0f, 0.2f),
        };

        var go = new GameObject("BeanCan");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = mats;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // Profile from bottom to top as (radius, height, submesh). Real can ≈ 7.5 cm wide × 11 cm tall;
    // units here are arbitrary since the store builder rescales everything.
    static readonly (float r, float y, int sub)[] Profile =
    {
        (0.44f, 0.00f, 0), // bottom rim bevel
        (0.50f, 0.08f, 0), // rim
        (0.50f, 0.14f, 1), // label
        (0.50f, 0.58f, 2), // cream stripe
        (0.50f, 0.86f, 1), // label
        (0.50f, 1.32f, 0), // top rim bevel
        (0.44f, 1.40f, 0), // (top edge; its submesh is unused)
    };

    static Mesh BuildMesh()
    {
        var verts = new List<Vector3>();
        var subs = new[] { new List<int>(), new List<int>(), new List<int>() };

        Vector3 Ring(float r, float y, int i)
        {
            float a = i * Mathf.PI * 2 / Sides;
            return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
        }

        // Unshared vertices per triangle give flat shading after RecalculateNormals.
        void Tri(int sub, Vector3 a, Vector3 b, Vector3 c)
        {
            subs[sub].Add(verts.Count); verts.Add(a);
            subs[sub].Add(verts.Count); verts.Add(b);
            subs[sub].Add(verts.Count); verts.Add(c);
        }

        // Each band takes the submesh of the profile point at its bottom.
        for (int p = 0; p < Profile.Length - 1; p++)
        {
            var lo = Profile[p];
            var hi = Profile[p + 1];
            int sub = lo.sub;
            for (int i = 0; i < Sides; i++)
            {
                Vector3 a = Ring(lo.r, lo.y, i), b = Ring(lo.r, lo.y, i + 1);
                Vector3 c = Ring(hi.r, hi.y, i + 1), d = Ring(hi.r, hi.y, i);
                Tri(sub, a, d, c);
                Tri(sub, a, c, b);
            }
        }

        // Lids
        var bottom = Profile[0];
        var top = Profile[Profile.Length - 1];
        for (int i = 0; i < Sides; i++)
        {
            Tri(0, new Vector3(0, top.y, 0), Ring(top.r, top.y, i + 1), Ring(top.r, top.y, i));
            Tri(0, new Vector3(0, bottom.y, 0), Ring(bottom.r, bottom.y, i), Ring(bottom.r, bottom.y, i + 1));
        }

        var mesh = new Mesh { name = "BeanCan" };
        mesh.SetVertices(verts);
        mesh.subMeshCount = subs.Length;
        for (int s = 0; s < subs.Length; s++) mesh.SetTriangles(subs[s], s);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Material Mat(string name, Color color, float metallic, float smoothness)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(mat, $"{Folder}/{name}.mat");
        return mat;
    }
}

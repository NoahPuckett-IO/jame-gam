using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
        if (existing != null)
        {
            // an older can built every triangle from its own three corners (504 vertices); rebuild it in place
            var old = existing.GetComponent<MeshFilter>().sharedMesh;
            var fresh = BuildMesh();
            if (old != null && old.vertexCount != fresh.vertexCount)
            {
                old.Clear();
                old.SetVertices(fresh.vertices);
                old.subMeshCount = fresh.subMeshCount;
                for (int s = 0; s < fresh.subMeshCount; s++) old.SetTriangles(fresh.GetTriangles(s), s);
                old.SetNormals(fresh.normals);
                old.RecalculateBounds();
                EditorUtility.SetDirty(old);
            }
            Object.DestroyImmediate(fresh);
            return existing;
        }

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

    static readonly (float r, float y, int sub)[] Profile =
    {
        (0.44f, 0.00f, 0),
        (0.50f, 0.08f, 0),
        (0.50f, 0.14f, 1),
        (0.50f, 0.58f, 2),
        (0.50f, 0.86f, 1),
        (0.50f, 1.32f, 0),
        (0.44f, 1.40f, 0),
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

        // flat shaded: each side panel and each end cap is flat, so its corners are shared within it but never with
        // a neighbour (314 vertices, under the store merge's limit, where one set of corners per triangle made 504)
        for (int p = 0; p < Profile.Length - 1; p++)
        {
            var lo = Profile[p];
            var hi = Profile[p + 1];
            for (int i = 0; i < Sides; i++)
            {
                int v = verts.Count;
                verts.Add(Ring(lo.r, lo.y, i)); verts.Add(Ring(lo.r, lo.y, i + 1));
                verts.Add(Ring(hi.r, hi.y, i + 1)); verts.Add(Ring(hi.r, hi.y, i));
                subs[lo.sub].AddRange(new[] { v, v + 3, v + 2, v, v + 2, v + 1 });
            }
        }

        foreach (var (end, up) in new[] { (Profile[Profile.Length - 1], true), (Profile[0], false) })
        {
            int centre = verts.Count;
            verts.Add(new Vector3(0, end.y, 0));
            for (int i = 0; i < Sides; i++) verts.Add(Ring(end.r, end.y, i));
            for (int i = 0; i < Sides; i++)
            {
                int a = centre + 1 + i, b = centre + 1 + (i + 1) % Sides;
                subs[0].AddRange(up ? new[] { centre, b, a } : new[] { centre, a, b });
            }
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

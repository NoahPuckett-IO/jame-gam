using System.Collections.Generic;
using UnityEngine;

static partial class GroceryStoreBuilder
{
    static void BoxPile(Transform parent, float x0, float x1, float z0, float z1, System.Random rng, List<ProductType> goods)
    {
        var closed = LoadKenney("cardboardBoxClosed");
        var open = LoadKenney("cardboardBoxOpen");
        if (closed == null || open == null) return;
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        float box = 0.45f * Scale;
        int nx = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / box)), nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / box));
        var heights = new float[nx, nz];
        Vector3 Cell(int i, int j) => new Vector3(x0 + (i + 0.5f) * (x1 - x0) / nx, 0, z0 + (j + 0.5f) * (z1 - z0) / nz);

        for (int i = 0; i < nx; i++)
        for (int j = 0; j < nz; j++)
        {
            var b = SpawnSized(closed, parent, box * R(0.85f, 1.02f), R(0f, 360f));
            PlaceOnFloor(b, Cell(i, j) + new Vector3(R(-0.1f, 0.1f) * box, 0, R(-0.1f, 0.1f) * box));
            AddBoxColliders(b);
            SettleLater(b, force: true);
            heights[i, j] = GetBounds(b).max.y;
        }

        int loose = Mathf.RoundToInt(nx * nz * R(1.1f, 1.6f));
        for (int k = 0; k < loose; k++)
        {
            int i = rng.Next(nx), j = rng.Next(nz);
            var at = Cell(i, j) + new Vector3(R(-0.3f, 0.3f) * box, heights[i, j] + R(0.4f, 2f), R(-0.3f, 0.3f) * box);
            GameObject b;
            if (goods != null && goods.Count > 0 && rng.NextDouble() < 0.35)
                b = FilledBox(parent, open, box * R(0.8f, 0.95f), goods, rng);
            else
            {
                b = SpawnSized(rng.NextDouble() < 0.2 ? open : closed, parent, box * R(0.7f, 1f), R(0f, 360f));
                AddBoxColliders(b);
            }
            b.transform.rotation = Quaternion.Euler(R(-35f, 35f), R(0f, 360f), R(-35f, 35f)) * b.transform.rotation;
            PlaceOnFloor(b, at);
            SettleLater(b, force: true);
            heights[i, j] = GetBounds(b).max.y;
        }
    }

    // an open box with a few groceries standing inside it, kept as one piece for the physics drop
    static GameObject FilledBox(Transform parent, GameObject open, float size, List<ProductType> goods, System.Random rng)
    {
        var root = new GameObject("Open Box");
        root.transform.SetParent(parent);
        var b = SpawnSized(open, root.transform, size, 0f);
        AddBoxColliders(b);
        var bb = GetBounds(b);
        int count = rng.Next(1, 4);
        float slot = Mathf.Min(bb.size.x, bb.size.z) * 0.36f;
        for (int k = 0; k < count; k++)
        {
            var t = goods[rng.Next(goods.Count)];
            float s = Mathf.Min(t.size / Mathf.Max(t.bounds.x, t.bounds.y, t.bounds.z), slot / Mathf.Max(t.bounds.x, t.bounds.z));
            var p = Spawn(t.prefab, root.transform, t.yaw + rng.Next(-20, 20), s);
            var off = count == 1 ? Vector3.zero : new Vector3((k % 2 == 0 ? -1 : 1) * slot * 0.6f, 0, (k < 2 ? -1 : 1) * slot * 0.6f);
            PlaceOnFloor(p, new Vector3(bb.center.x + off.x, bb.min.y + bb.size.y * 0.08f, bb.center.z + off.z));
        }
        return root;
    }
}

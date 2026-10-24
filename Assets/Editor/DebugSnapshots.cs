using System.IO;
using UnityEditor;
using UnityEngine;

static class DebugSnapshots
{
    static readonly string OutFolder = Path.Combine(Path.GetTempPath(), "TinyHenrySnapshots");

    [MenuItem("JameGam/Debug/Take Snapshots %#&1")]
    static void TakeAll()
    {
        Directory.CreateDirectory(OutFolder);
        int n = 0;
        foreach (var (name, eye, target) in Shots())
        {
            Render(name, eye, target);
            n++;
        }
        Debug.Log($"[DebugSnapshots] Saved {n} snapshots to {OutFolder}");
    }

    static System.Collections.Generic.IEnumerable<(string, Vector3, Vector3)> Shots()
    {
        var blocker = Find("Blocker");
        if (blocker != null)
        {
            var p = blocker.position;
            p.y = 0f;
            yield return ("pile_a", p + new Vector3(26f, 10f, p.z < 0 ? 30f : -30f), p + Vector3.up * 5f);
            yield return ("pile_b", p + new Vector3(-26f, 8f, p.z < 0 ? 22f : -22f), p + Vector3.up * 5f);
        }
        var lane = Find("Lane Pole");
        if (lane != null) yield return ("lanes", lane.position + new Vector3(30f, 4f, -40f), lane.position + Vector3.up * 6f);
        var vac = Find("VAC-3000");
        if (vac != null)
        {
            yield return ("vac", vac.position + new Vector3(7f, 5f, -9f), vac.position + Vector3.up);
            yield return ("boss_hall", vac.position + new Vector3(0f, 22f, -60f), vac.position);
        }
        var portrait = Find("Portrait Camera");
        if (portrait != null)
        {
            var look = portrait.position + portrait.forward * 5f;
            yield return ("menu_henry", portrait.position, look);
        }
        var alien = Find("Alien");
        if (alien != null)
        {
            yield return ("ship", Find("Cam Ship").position, alien.position + Vector3.up * 1.4f + Vector3.left * 3.5f);
            yield return ("alien_close", alien.position + alien.forward * 4.5f + Vector3.up * 2.2f + alien.right * 1.5f, alien.position + Vector3.up * 1.9f);
        }
        var level3 = Find("Level Three - Freezer Aisle");
        if (level3 != null)
        {
            var crossing = Find("Crossing Wall");
            if (crossing != null)
            {
                var c = crossing.position;
                yield return ("ice_crossing", c + new Vector3(30f, 30f, -40f), c + new Vector3(0, -20f, 10f));
                yield return ("ice_overview", c + new Vector3(0f, 45f, -110f), c + new Vector3(0, 0, 0));
            }
        }
        var l2 = Find("Level Two - Shelf Climb");
        if (l2 != null)
        {
            var plate = Find("Paper Plate Platform");
            if (plate != null) yield return ("l2_plate", plate.position + new Vector3(18f, 6f, 14f), plate.position);
            var jar = Find("Jar Block");
            if (jar != null)
            {
                var col = jar.GetComponentInChildren<BoxCollider>();
                var p = col != null ? col.bounds.center : jar.position;
                yield return ("l2_crackers", p + new Vector3(-22f, 6f, 16f), p + new Vector3(-4f, -3f, -2f));
            }
        }
        var l1 = Find("Puzzle 3 - Ramp");
        if (l1 != null)
        {
            var wall = Find("Wall Collider");
            var p = wall != null ? wall.position : l1.position;
            yield return ("l1_wall", p + new Vector3(10f, 8f, -26f), p);
        }
        var litter = Find("Litter");
        if (litter != null)
        {
            var pl = Find("Spawn Point");
            if (pl != null) yield return ("l1_litter", pl.position + new Vector3(-8f, 4f, 20f), pl.position + new Vector3(4f, 0f, 45f));
        }
    }

    static Transform Find(string name)
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            if (t.name == name && t.gameObject.scene.IsValid()) return t;
        return null;
    }

    static void Render(string name, Vector3 eye, Vector3 target)
    {
        var go = new GameObject("Snapshot Camera") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = name == "menu_henry" ? 30f : 55f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 3000f;
        go.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye));
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(OutFolder, name + ".jpg"), tex.EncodeToJPG(85));
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
    }
}

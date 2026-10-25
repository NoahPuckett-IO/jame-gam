using System.IO;
using UnityEditor;
using UnityEngine;

// dev helpers for checking Henry's ride poses: jump to a ride, then save close-up renders of him from four sides
static class PosePreview
{
    const string OutFolder = "Temp/PosePreview";

    [MenuItem("JameGam/Debug/Capture Henry Pose %#&F9")]
    static void Capture()
    {
        Transform henry = null;
        if (Application.isPlaying)
        {
            var pa = Object.FindAnyObjectByType<PlayerAnimator>();
            if (pa != null && pa.Animator != null) henry = pa.Animator.transform;
        }
        else
        {
            var stage = GameObject.Find("Menu Henry Stage");
            if (stage != null) henry = stage.transform.Find("Henry");
        }
        if (henry == null) { Debug.LogWarning("[PosePreview] Henry not found"); return; }

        var rs = henry.GetComponentsInChildren<SkinnedMeshRenderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        float dist = b.extents.magnitude / Mathf.Tan(15f * Mathf.Deg2Rad) * 1.05f;

        var go = new GameObject("Pose Preview Camera");
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.2f, 0.25f, 0.28f);
        cam.fieldOfView = 30f;
        cam.nearClipPlane = Mathf.Max(0.05f, dist - b.extents.magnitude * 1.3f);
        cam.farClipPlane = dist + b.extents.magnitude * 2f;
        const int size = 700;
        var rt = new RenderTexture(size, size, 24) { antiAliasing = 4 };
        cam.targetTexture = rt;

        var f = henry.forward;
        var right = henry.right;
        var up = henry.up;
        (Vector3 dir, Vector3 camUp)[] views =
        {
            (f + right * 0.45f + up * 0.15f, up),
            (right, up),
            (-f + right * 0.4f + up * 0.15f, up),
            (-right + f * 0.25f, up),
        };
        var sheet = new Texture2D(size * 2, size * 2, TextureFormat.RGB24, false);
        var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
        for (int i = 0; i < views.Length; i++)
        {
            go.transform.position = b.center + views[i].dir.normalized * dist;
            go.transform.LookAt(b.center, views[i].camUp);
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex.Apply();
            sheet.SetPixels((i % 2) * size, (1 - i / 2) * size, size, size, tex.GetPixels());
        }
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(go);
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);

        Directory.CreateDirectory(OutFolder);
        var path = Path.GetFullPath(Path.Combine(OutFolder, $"henry_{System.DateTime.Now:HHmmss}.png"));
        File.WriteAllBytes(path, sheet.EncodeToPNG());
        Object.DestroyImmediate(sheet);
        Debug.Log("[PosePreview] saved " + path);
    }

    static PlayerController Player() => Application.isPlaying ? Object.FindAnyObjectByType<PlayerController>() : null;

    static void Put(PlayerController p, Vector3 pos, Vector3 facing)
    {
        facing.y = 0f;
        var rot = Quaternion.LookRotation(facing.normalized);
        p.Teleport(pos, rot);
        p.SetCheckpoint(pos, rot);
        p.SetLook(rot.eulerAngles.y, 15f);
    }

    static Vector3 V(Object o, string field) => new SerializedObject(o).FindProperty(field).vector3Value;

    [MenuItem("JameGam/Debug/Go To Ladder %#&F5")]
    static void GoLadder()
    {
        var p = Player();
        var l = Object.FindAnyObjectByType<Ladder>();
        if (p == null || l == null) return;
        var outward = V(l, "outward");
        Put(p, V(l, "foot") + outward * 1.6f + Vector3.up * 1.05f, -outward);
    }

    [MenuItem("JameGam/Debug/Go To Zipline %#&F6")]
    static void GoZipline()
    {
        var p = Player();
        var z = Object.FindAnyObjectByType<Zipline>();
        if (p == null || z == null) return;
        var start = V(z, "start");
        var end = V(z, "end");
        var launch = GameObject.Find("Zipline Launch");
        float top = launch != null ? launch.GetComponent<Collider>().bounds.max.y : start.y - 2.3f;
        Put(p, new Vector3(start.x, top + 1.05f, start.z), end - start);
    }

    [MenuItem("JameGam/Debug/Go To Slide 1 %#&F7")]
    static void GoSlide1() => GoSlide(0);

    [MenuItem("JameGam/Debug/Go To Slide 2 %#&F8")]
    static void GoSlide2() => GoSlide(1);

    static void GoSlide(int index)
    {
        var p = Player();
        var slides = System.Array.FindAll(Resources.FindObjectsOfTypeAll<LieSlide>(), x => x.gameObject.scene.IsValid());
        if (p == null || slides.Length <= index) return;
        System.Array.Sort(slides, (a, c) => a.transform.position.x.CompareTo(c.transform.position.x));
        var s = slides[index];
        var top = V(s, "top");
        var down = (V(s, "bottom") - top).normalized;
        var flat = new Vector3(down.x, 0f, down.z).normalized;
        Put(p, top + down * 1.5f + Vector3.up * 1.15f, flat);
    }
}

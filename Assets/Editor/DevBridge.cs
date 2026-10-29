using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

// Lets the editor be driven from a text file: write commands into DebugOut/cmd.txt (one per line) and the editor
// runs them on its next tick, writing results, renders and the console into DebugOut/. DebugOut sits next to
// Assets, so nothing in it is imported or shipped.
[InitializeOnLoad]
static class DevBridge
{
    static readonly string Root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "DebugOut"));
    static string CmdPath => Path.Combine(Root, "cmd.txt");
    static string LogPath => Path.Combine(Root, "log.txt");
    static string StatusPath => Path.Combine(Root, "status.txt");
    static System.DateTime lastCmd;
    static readonly Queue<string> pending = new Queue<string>();
    static double waitUntil;

    static DevBridge()
    {
        Directory.CreateDirectory(Root);
        if (File.Exists(CmdPath)) lastCmd = File.GetLastWriteTimeUtc(CmdPath);
        Application.logMessageReceived += OnLog;
        CompilationPipeline.compilationStarted += _ => File.WriteAllText(Path.Combine(Root, "compile.txt"), "compiling\n");
        CompilationPipeline.assemblyCompilationFinished += (asm, msgs) =>
        {
            var sb = new StringBuilder();
            foreach (var m in msgs)
                if (m.type == CompilerMessageType.Error) sb.AppendLine($"ERROR {m.file}:{m.line} {m.message}");
            File.AppendAllText(Path.Combine(Root, "compile.txt"), $"{Path.GetFileName(asm)}: {(sb.Length == 0 ? "ok" : "\n" + sb)}\n");
        };
        EditorApplication.update += Tick;
        Status("ready " + System.DateTime.Now.ToString("HH:mm:ss"));
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Log && !message.StartsWith("[")) return;
        try
        {
            if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 2_000_000) File.Delete(LogPath);
            string trace = type == LogType.Exception || type == LogType.Error ? "\n" + string.Join("\n", stack.Split('\n').Take(6)) : "";
            File.AppendAllText(LogPath, $"{System.DateTime.Now:HH:mm:ss} {type}: {message}{trace}\n");
        }
        catch (IOException) { }
    }

    static void Status(string line)
    {
        try { File.AppendAllText(StatusPath, line + "\n"); } catch (IOException) { }
    }

    static void Tick()
    {
        if (File.Exists(CmdPath))
        {
            var t = File.GetLastWriteTimeUtc(CmdPath);
            if (t != lastCmd)
            {
                lastCmd = t;
                foreach (var line in File.ReadAllLines(CmdPath))
                    if (!string.IsNullOrWhiteSpace(line)) pending.Enqueue(line.Trim());
            }
        }
        if (pending.Count == 0 || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.timeSinceStartup < waitUntil) return;
        if (PerfProbe.Busy) return;
        var cmd = pending.Dequeue();
        try
        {
            Status("run " + cmd);
            Run(cmd);
            Status("done " + cmd);
        }
        catch (System.Exception e)
        {
            Status($"fail {cmd}: {e.Message}");
            Debug.LogException(e);
        }
    }

    static void Run(string cmd)
    {
        var parts = cmd.Split(new[] { ' ' }, 2);
        string arg = parts.Length > 1 ? parts[1] : "";
        switch (parts[0])
        {
            case "refresh": AssetDatabase.Refresh(); break;
            case "menu": if (!EditorApplication.ExecuteMenuItem(arg)) throw new System.Exception("no menu " + arg); break;
            case "build": EditorApplication.ExecuteMenuItem("JameGam/Build Grocery Store"); break;
            case "save": EditorSceneManager.SaveOpenScenes(); break;
            case "play": EditorApplication.isPlaying = true; break;
            case "stop": EditorApplication.isPlaying = false; break;
            case "wait": waitUntil = EditorApplication.timeSinceStartup + float.Parse(arg); break;
            case "clearlog": File.WriteAllText(LogPath, ""); File.WriteAllText(StatusPath, ""); break;
            case "shots": Shots(); break;
            case "find": Find(arg); break;
            case "models": ModelSheet(arg.Split(' ')); break;
            case "warp": Warp(arg); break;
            case "matinfo": MatInfo(arg.Split(' ')); break;
            case "tutorial": PlayingOnly(); Object.FindAnyObjectByType<MainMenu>().Tutorial(); break;
            case "tp": Teleport(arg.Split(' ').Select(F).ToArray()); break;
            case "grab": Grab(); break;
            case "drop": Drop(); break;
            case "pos": Pos(); break;
            case "start": PlayingOnly(); Object.FindAnyObjectByType<MainMenu>().Play(); break;
            case "skip": PlayingOnly(); SkipIntro(); break;
            case "reopen": UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/JameGamGame.unity"); break;
            case "census": PerfProbe.Census(arg); break;
            case "perf": PerfProbe.Perf(arg); break;
            case "looks": PerfProbe.Looks(arg); break;
            case "diff": PerfProbe.Diff(arg); break;
            case "tweak": PerfProbe.Tweak(arg); break;
            case "benchbuild": PerfProbe.BenchBuild(arg); break;
            case "matkw": PerfProbe.MatKeywords(arg); break;
            case "combinecensus": PerfProbe.CombineCensus(arg); break;
            case "mergecheck": PerfProbe.MergeCheck(); break;
            default: throw new System.Exception("unknown command");
        }
    }

    // menu and intro calls made outside play mode would switch the menu off in the saved scene
    static void PlayingOnly()
    {
        if (!Application.isPlaying) throw new System.Exception("only in play mode");
    }

    // shots.txt: "name ex ey ez tx ty tz [fov]" with world positions, or "name @Object dx dy dz [fov]" to look at
    // an object's bounds centre from that offset
    static void Shots()
    {
        var path = Path.Combine(Root, "shots.txt");
        if (!File.Exists(path)) return;
        foreach (var raw in File.ReadAllLines(path))
        {
            var p = raw.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (p.Length < 5 || p[0].StartsWith("#")) continue;
            Vector3 eye, target;
            float fov = 55f;
            if (p[1].StartsWith("@"))
            {
                var t = FindObject(p[1].Substring(1).Replace('_', ' '));
                if (t == null) { Status("no object " + p[1]); continue; }
                target = BoundsOf(t.gameObject).center;
                eye = target + new Vector3(F(p[2]), F(p[3]), F(p[4]));
                if (p.Length > 5) fov = F(p[5]);
            }
            else
            {
                eye = new Vector3(F(p[1]), F(p[2]), F(p[3]));
                target = new Vector3(F(p[4]), F(p[5]), F(p[6]));
                if (p.Length > 7) fov = F(p[7]);
            }
            // the tutorial room switches fog off and the ambient to flat white when it starts; preview it that way
            bool tut = p[0].StartsWith("tut");
            var (fog, mode, amb) = (RenderSettings.fog, RenderSettings.ambientMode, RenderSettings.ambientLight);
            if (tut)
            {
                RenderSettings.fog = false;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.86f, 0.87f, 0.9f);
            }
            Render(p[0], eye, target, fov);
            if (tut) (RenderSettings.fog, RenderSettings.ambientMode, RenderSettings.ambientLight) = (fog, mode, amb);
        }
    }

    static float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

    static Transform FindObject(string name)
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            if (t.name == name && t.gameObject.scene.IsValid()) return t;
        return null;
    }

    static Bounds BoundsOf(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var cs = go.GetComponentsInChildren<Collider>();
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        bool any = false;
        foreach (var r in rs) { if (!any) b = r.bounds; else b.Encapsulate(r.bounds); any = true; }
        foreach (var c in cs) { if (!any) b = c.bounds; else b.Encapsulate(c.bounds); any = true; }
        return b;
    }

    // find Name: logs where every object with that name is and what it spans
    static void Find(string name)
    {
        var sb = new StringBuilder();
        foreach (var one in name.Split('|'))
        {
            int n = 0;
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (!t.gameObject.scene.IsValid() || !t.name.Contains(one)) continue;
                var b = BoundsOf(t.gameObject);
                sb.AppendLine($"{PathOf(t)} pos={t.position} bounds min={b.min} max={b.max}");
                if (++n > 60) break;
            }
        }
        File.WriteAllText(System.IO.Path.Combine(Root, "find.txt"), sb.ToString());
    }

    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

    static void Warp(string arg)
    {
        var t = FindObject(arg);
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (t == null || player == null) throw new System.Exception("can't warp to " + arg);
        player.Teleport(t.position + Vector3.up * 0.5f, t.rotation);
    }

    static void Teleport(float[] p)
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        player.Teleport(new Vector3(p[0], p[1], p[2]), Quaternion.Euler(0, p.Length > 3 ? p[3] : 0, 0));
    }

    // picks up the nearest carryable the way pressing E does
    static void Grab()
    {
        var hands = Object.FindAnyObjectByType<PlayerInteractor>();
        var at = hands.transform.position;
        var item = Object.FindObjectsByType<Carryable>().OrderBy(c => Vector3.Distance(c.transform.position, at)).First();
        var routine = (System.Collections.IEnumerator)typeof(PlayerInteractor)
            .GetMethod("PickUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(hands, new object[] { item });
        hands.StartCoroutine(routine);
    }

    // puts down whatever's held, the way pressing E again does
    static void Drop()
    {
        var hands = Object.FindAnyObjectByType<PlayerInteractor>();
        var routine = (System.Collections.IEnumerator)typeof(PlayerInteractor)
            .GetMethod("PutDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(hands, null);
        hands.StartCoroutine(routine);
    }

    // what pressing space during the intro does
    static void SkipIntro()
    {
        var intro = Object.FindAnyObjectByType<IntroCutscene>();
        if (intro == null || !intro.Running) return;
        var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var type = typeof(IntroCutscene);
        type.GetField("skipping", f).SetValue(intro, true);
        intro.StopAllCoroutines();
        type.GetMethod("StopSounds", f)?.Invoke(intro, null);
        intro.StartCoroutine((System.Collections.IEnumerator)type.GetMethod("Finish", f).Invoke(intro, new object[] { true }));
    }

    static void Pos()
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        Status($"pos {player.transform.position} yaw {player.transform.eulerAngles.y:0} grounded {player.IsGrounded} attached {player.Attached}");
    }

    static void MatInfo(string[] paths)
    {
        var sb = new StringBuilder();
        foreach (var path in paths)
            foreach (var r in AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponentsInChildren<Renderer>() ?? new Renderer[0])
                foreach (var m in r.sharedMaterials)
                    if (m != null)
                        sb.AppendLine($"{path} {r.name}: {m.name} shader={m.shader.name} color={(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color)} tex={(m.mainTexture != null ? AssetDatabase.GetAssetPath(m.mainTexture) : "-")}");
        File.WriteAllText(System.IO.Path.Combine(Root, "matinfo.txt"), sb.ToString());
    }

    static void ModelSheet(string[] paths)
    {
        var root = new GameObject("Model Sheet") { hideFlags = HideFlags.DontSave };
        root.transform.position = new Vector3(0, -3000, 0);
        var sb = new StringBuilder();
        float x = 0;
        foreach (var path in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { sb.AppendLine("missing " + path); continue; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            var b = BoundsOf(go);
            float s = 4f / Mathf.Max(b.size.x, b.size.y, b.size.z, 0.001f);
            sb.AppendLine($"{path} size={b.size} center-pos={b.center - go.transform.position} rot={go.transform.rotation.eulerAngles} meshes={go.GetComponentsInChildren<MeshFilter>().Length}");
            go.transform.localScale *= s;
            b = BoundsOf(go);
            go.transform.position += new Vector3(root.transform.position.x + x + 2.5f - b.center.x, root.transform.position.y - b.min.y, root.transform.position.z - b.center.z);
            x += 5.5f;
        }
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.SetParent(root.transform);
        floor.transform.position = root.transform.position + new Vector3(x / 2, -0.05f, 0);
        floor.transform.localScale = new Vector3(x + 4, 0.1f, 12);
        var light = new GameObject("Sheet Light").AddComponent<Light>();
        light.transform.SetParent(root.transform);
        light.type = LightType.Directional;
        light.intensity = 1.6f;
        light.transform.rotation = Quaternion.Euler(40, -30, 0);
        var c = root.transform.position + new Vector3(x / 2, 2f, 0);
        Render("models_front", c + new Vector3(0, 2.5f, -x * 0.62f - 3f), c, 40f);
        Render("models_top", c + new Vector3(0, x * 0.5f + 4f, -x * 0.45f), c, 40f);
        Render("models_low", c + new Vector3(0, 0.5f, -x * 0.62f - 3f) + Vector3.down * 1.5f, c + Vector3.down * 1.5f, 40f);
        File.WriteAllText(System.IO.Path.Combine(Root, "models.txt"), sb.ToString());
        Object.DestroyImmediate(root);
    }

    static void Render(string name, Vector3 eye, Vector3 target, float fov)
    {
        var go = new GameObject("Bridge Camera") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 3000f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.2f, 0.22f, 0.25f);
        go.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye));
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(System.IO.Path.Combine(Root, name + ".jpg"), tex.EncodeToJPG(82));
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
    }
}

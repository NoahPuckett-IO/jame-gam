using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Rendering measurements driven from DevBridge. "census" counts what the scene asks the renderer to do; "perf"
// (play mode) parks a camera at each view in DebugOut/perfviews.txt and averages the frame stats there; "looks"
// renders the same views to PNG so before/after images can be compared with "diff".
static class PerfProbe
{
    static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "DebugOut"));

    static readonly string[] Counters =
    {
        "Batches Count", "Draw Calls Count", "SetPass Calls Count", "Triangles Count", "Vertices Count",
        "Shadow Casters Count", "Static Batched Draw Calls Count", "Instanced Batched Draw Calls Count",
    };

    struct View { public string name; public Vector3 eye, target; public float fov; }

    static List<View> LoadViews()
    {
        var views = new List<View>();
        var path = Path.Combine(Root, "perfviews.txt");
        foreach (var raw in File.ReadAllLines(path))
        {
            var p = raw.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (p.Length < 7 || p[0].StartsWith("#")) continue;
            views.Add(new View
            {
                name = p[0],
                eye = new Vector3(F(p[1]), F(p[2]), F(p[3])),
                target = new Vector3(F(p[4]), F(p[5]), F(p[6])),
                fov = p.Length > 7 ? F(p[7]) : 60f,
            });
        }
        return views;
    }

    static float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

    public static void Census(string tag)
    {
        var sb = new StringBuilder();
        var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        var active = renderers.Where(r => r.enabled).ToArray();
        sb.AppendLine($"mesh renderers {renderers.Length} enabled {active.Length}");
        sb.AppendLine($"  static-batching flagged {active.Count(r => GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.BatchingStatic))}");
        sb.AppendLine($"  shadow casting {active.Count(r => r.shadowCastingMode != ShadowCastingMode.Off)}");
        sb.AppendLine($"  with property block {active.Count(r => r.HasPropertyBlock())}");
        long tris = 0;
        var pairs = new Dictionary<(Mesh, Material), int>();
        var mats = new HashSet<Material>();
        foreach (var r in active)
        {
            var mf = r.GetComponent<MeshFilter>();
            var mesh = mf != null ? mf.sharedMesh : null;
            if (mesh != null) for (int i = 0; i < mesh.subMeshCount; i++) tris += mesh.GetIndexCount(i) / 3;
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                mats.Add(m);
                pairs.TryGetValue((mesh, m), out int n);
                pairs[(mesh, m)] = n + 1;
            }
        }
        sb.AppendLine($"  triangles {tris}  materials {mats.Count}  instancing-enabled materials {mats.Count(m => m.enableInstancing)}");
        sb.AppendLine($"  mesh+material pairs {pairs.Count}, pairs used 10+ times {pairs.Count(p => p.Value >= 10)} covering {pairs.Where(p => p.Value >= 10).Sum(p => p.Value)} draws");
        foreach (var p in pairs.OrderByDescending(p => p.Value).Take(15))
            sb.AppendLine($"    {p.Value}x {(p.Key.Item1 ? p.Key.Item1.name : "-")} / {p.Key.Item2.name} ({p.Key.Item2.shader.name})");
        var byMesh = active.Select(r => r.GetComponent<MeshFilter>()).Where(mf => mf != null && mf.sharedMesh != null)
            .GroupBy(mf => mf.sharedMesh).Select(g => (mesh: g.Key, n: g.Count(), tris: g.Key.triangles.Length / 3))
            .OrderByDescending(x => (long)x.n * x.tris).Take(25);
        sb.AppendLine("  heaviest meshes (instances x triangles):");
        foreach (var x in byMesh)
            sb.AppendLine($"    {x.mesh.name} ({AssetDatabase.GetAssetPath(x.mesh)}): {x.n} x {x.tris} = {(long)x.n * x.tris / 1000}k  bounds {x.mesh.bounds.size}");
        var shaders = mats.GroupBy(m => m.shader.name).OrderByDescending(g => g.Count());
        foreach (var g in shaders) sb.AppendLine($"  shader {g.Key}: {g.Count()} materials");
        foreach (var g in mats.GroupBy(m => System.IO.Path.GetExtension(AssetDatabase.GetAssetPath(m))))
            sb.AppendLine($"  materials from '{g.Key}': {g.Count()}  e.g. {string.Join(", ", g.Take(4).Select(m => AssetDatabase.GetAssetPath(m) + ":" + m.name))}");
        var transparent = mats.Where(m => m.renderQueue >= 3000).ToList();
        sb.AppendLine($"  transparent materials {transparent.Count}");

        var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude).Where(l => l.enabled).ToArray();
        sb.AppendLine($"lights {lights.Length}");
        foreach (var g in lights.GroupBy(l => $"{l.type} shadows={l.shadows} range={l.range:0.#}").OrderByDescending(g => g.Count()))
            sb.AppendLine($"  {g.Count()}x {g.Key}  e.g. {PathOf(g.First().transform)}");

        sb.AppendLine($"colliders {Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude).Length}  rigidbodies {Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude).Length}");
        sb.AppendLine($"behaviours {Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude).Length}");
        foreach (var g in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude).GroupBy(b => b.GetType().Name).OrderByDescending(g => g.Count()).Take(12))
            sb.AppendLine($"  {g.Count()}x {g.Key}");

        // where the renderers live, by the store's top groups
        var store = GameObject.Find("Store");
        if (store != null)
            foreach (Transform child in store.transform)
            {
                var rs = child.GetComponentsInChildren<MeshRenderer>();
                if (rs.Length < 20) continue;
                int st = rs.Count(r => GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.BatchingStatic));
                int moving = child.GetComponentsInChildren<Rigidbody>().Length;
                sb.AppendLine($"  group {child.name}: {rs.Length} renderers, {st} static, {moving} rigidbodies, active={child.gameObject.activeInHierarchy}");
            }

        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
            sb.AppendLine($"urp {urp.name}: shadowDistance {urp.shadowDistance} cascades {urp.shadowCascadeCount} mainShadowRes {urp.mainLightShadowmapResolution} addShadowRes {urp.additionalLightsShadowmapResolution} msaa {urp.msaaSampleCount} hdr {urp.supportsHDR} opaqueTex {urp.supportsCameraOpaqueTexture} depthTex {urp.supportsCameraDepthTexture}");
        File.WriteAllText(Path.Combine(Root, $"census_{tag}.txt"), sb.ToString());
    }

    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

    // combinecensus: how much mesh data merging the never-moving store renderers would produce
    public static void CombineCensus(string arg)
    {
        int maxVerts = string.IsNullOrEmpty(arg) ? int.MaxValue : int.Parse(arg);
        var sb = new StringBuilder();
        var store = GameObject.Find("Store");
        var reasons = new Dictionary<string, int>();
        void Skip(string why) { reasons.TryGetValue(why, out int n); reasons[why] = n + 1; }
        long verts = 0, indices = 0, bytes = 0, ok = 0;
        var mats = new HashSet<Material>();
        var behaviours = new Dictionary<string, int>();
        foreach (var r in store.GetComponentsInChildren<MeshRenderer>())
        {
            if (!r.enabled) { Skip("disabled"); continue; }
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) { Skip("no mesh"); continue; }
            if (mf.sharedMesh.vertexCount > maxVerts) { Skip("over vertex limit"); continue; }
            if (r.GetComponent<TextMesh>() != null) { Skip("text"); continue; }
            if (r.sharedMaterials.Any(m => m == null || m.renderQueue >= 2450)) { Skip("transparent/cutout"); continue; }
            if (r.GetComponentInParent<Rigidbody>() != null) { Skip("rigidbody"); continue; }
            var mb = r.GetComponentsInParent<MonoBehaviour>().Where(b => !(b is SignText)).ToArray();
            if (mb.Length > 0) { Skip("script " + mb[0].GetType().Name); continue; }
            var top = r.transform; while (top.parent != store.transform) top = top.parent;
            if (top.name.StartsWith("Level") || top.name.StartsWith("Boss") || top.name.StartsWith("Tutorial")) { Skip("level group " + top.name); continue; }
            ok++;
            var mesh = mf.sharedMesh;
            verts += mesh.vertexCount;
            for (int i = 0; i < mesh.subMeshCount; i++) indices += mesh.GetIndexCount(i);
            int stride = 0; for (int s = 0; s < mesh.vertexBufferCount; s++) stride += mesh.GetVertexBufferStride(s);
            bytes += (long)mesh.vertexCount * stride;
            foreach (var m in r.sharedMaterials) mats.Add(m);
        }
        sb.AppendLine($"combinable {ok}: verts {verts}, indices {indices}, vertex bytes {bytes / 1048576}MB + index bytes {indices * 4 / 1048576}MB, materials {mats.Count}");
        foreach (var kv in reasons.OrderByDescending(k => k.Value)) sb.AppendLine($"  skipped {kv.Value}: {kv.Key}");
        File.WriteAllText(Path.Combine(Root, $"combine_{arg}.txt"), sb.ToString());
    }

    // matkw <path> [path...]: what the loaded copy of each material holds, to compare against the file on disk
    public static void MatKeywords(string arg)
    {
        var sb = new StringBuilder();
        foreach (var path in arg.Split(' '))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { sb.AppendLine($"{path}: missing"); continue; }
            sb.AppendLine($"{path}: keywords [{string.Join(",", m.shaderKeywords)}] srcBlend {(m.HasProperty("_SrcBlend") ? m.GetFloat("_SrcBlend") : -1)} instancing {m.enableInstancing} dirty {EditorUtility.IsDirty(m)}");
        }
        File.WriteAllText(Path.Combine(Root, "matkw.txt"), sb.ToString());
    }

    // benchbuild <name>: development WebGL build (so the number-key level warps work) into DebugOut/bench/<name>
    public static void BenchBuild(string arg)
    {
        var parts = arg.Split(' ');
        string name = parts[0];
        bool instancing = parts.Length > 1 && parts[1] == "inst";
        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        bool wasSrpb = urp.useSRPBatcher;
        urp.useSRPBatcher = !instancing;
        StripWebGLInstancing.KeepForBench = instancing;
        string folder = Path.Combine(Root, "bench", name);
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        var report = UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/JameGamGame.unity" },
            locationPathName = folder,
            target = BuildTarget.WebGL,
            options = BuildOptions.Development,
        });
        urp.useSRPBatcher = wasSrpb;
        StripWebGLInstancing.KeepForBench = false;
        Debug.Log($"[BenchBuild] {arg} {report.summary.result} in {report.summary.totalTime}");
    }

    // tweak <setting> <on|off>: flips one rendering switch for an experiment
    public static void Tweak(string arg)
    {
        var p = arg.Split(' ');
        bool on = p.Length > 1 && p[1] == "on";
        switch (p[0])
        {
            case "srpb": GraphicsSettings.useScriptableRenderPipelineBatching = on; break;
            case "instancing":
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
                    foreach (var m in r.sharedMaterials)
                        if (m != null) m.enableInstancing = on;
                break;
            case "shadows":
                foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                    if (l.type == LightType.Spot) l.shadows = on ? LightShadows.Soft : LightShadows.None;
                break;
            case "lamps":
                foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                    if (l.type == LightType.Point && l.name == "Lamp") l.enabled = on;
                break;
            case "ssao":
                {
                    var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
                    foreach (var f in data.rendererFeatures) if (f.name.Contains("Ambient") || f.GetType().Name.Contains("ScreenSpaceAmbient")) f.SetActive(on);
                }
                break;
            case "priming":
                {
                    var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
                    data.depthPrimingMode = on ? DepthPrimingMode.Forced : DepthPrimingMode.Disabled;
                }
                break;
            default: throw new System.Exception("unknown tweak " + p[0]);
        }
    }

    // ---- play-mode frame stats ----

    static List<View> views;
    static int viewIndex, phaseStart;
    static bool measuring;
    static string runTag;
    static Camera probe;
    static readonly List<Camera> muted = new List<Camera>();
    static ProfilerRecorder[] recorders;
    static ProfilerRecorder mainThread;
    static readonly string[] Markers =
    {
        "PlayerLoop", "BehaviourUpdate", "FixedBehaviourUpdate", "LateBehaviourUpdate", "Physics.Simulate", "FixedUpdate.PhysicsFixedUpdate",
        "CullScriptable", "Culling", "Shadows.Culling", "DoRenderLoop_Internal", "RenderPipelineManager.DoRenderLoop_Internal()",
        "Gfx.WaitForPresentOnGfxThread", "Gfx.WaitForRenderThread", "WaitForTargetFPS", "UpdateRendererBoundingVolumes", "Animators.Update",
    };
    static List<(string, ProfilerRecorder)> markers;
    static readonly List<double> gpuTimes = new List<double>(), cpuTimes = new List<double>();
    static StringBuilder report;
    const int Warmup = 30, Frames = 120;

    public static bool Busy => views != null;

    public static void Perf(string tag)
    {
        if (!EditorApplication.isPlaying) throw new System.Exception("perf needs play mode");
        runTag = string.IsNullOrEmpty(tag) ? "run" : tag;
        views = LoadViews();
        viewIndex = 0;
        report = new StringBuilder();
        report.AppendLine($"perf {runTag} {System.DateTime.Now:HH:mm:ss} screen {Screen.width}x{Screen.height}");
        report.AppendLine("view | ms(main) | gpu ms | batches | draws | setpass | tris | shadowCasters | staticDraws | instancedDraws");

        var main = Camera.main;
        foreach (var c in Camera.allCameras)
            if (c.enabled && c.targetTexture == null) { muted.Add(c); c.enabled = false; }
        var go = new GameObject("Perf Probe Camera") { hideFlags = HideFlags.DontSave };
        probe = go.AddComponent<Camera>();
        if (main != null)
        {
            probe.CopyFrom(main);
            var src = main.GetUniversalAdditionalCameraData();
            var dst = probe.GetUniversalAdditionalCameraData();
            dst.renderPostProcessing = src.renderPostProcessing;
            dst.antialiasing = src.antialiasing;
            dst.renderShadows = src.renderShadows;
        }
        probe.enabled = true;
        probe.targetTexture = null;
        StartView();
        EditorApplication.update += Step;
    }

    static void StartView()
    {
        var v = views[viewIndex];
        probe.transform.SetPositionAndRotation(v.eye, Quaternion.LookRotation(v.target - v.eye));
        probe.fieldOfView = v.fov;
        phaseStart = Time.frameCount;
        measuring = false;
    }

    static void Step()
    {
        if (!EditorApplication.isPlaying) { Finish(); return; }
        int f = Time.frameCount - phaseStart;
        if (!measuring && f >= Warmup)
        {
            recorders = Counters.Select(n => ProfilerRecorder.StartNew(ProfilerCategory.Render, n, Frames + 10)).ToArray();
            mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", Frames + 10);
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            markers = new List<(string, ProfilerRecorder)>();
            foreach (var h in handles)
            {
                var d = ProfilerRecorderHandle.GetDescription(h);
                if (Markers.Contains(d.Name) && !markers.Any(m => m.Item1 == d.Name)) markers.Add((d.Name, new ProfilerRecorder(h, Frames + 10, ProfilerRecorderOptions.Default)));
            }
            foreach (var m in markers) m.Item2.Start();
            gpuTimes.Clear();
            cpuTimes.Clear();
            measuring = true;
            phaseStart = Time.frameCount;
            return;
        }
        if (measuring)
        {
            FrameTimingManager.CaptureFrameTimings();
            var t = new FrameTiming[1];
            if (FrameTimingManager.GetLatestTimings(1, t) > 0)
            {
                if (t[0].gpuFrameTime > 0) gpuTimes.Add(t[0].gpuFrameTime);
                cpuTimes.Add(t[0].cpuFrameTime);
            }
            if (f < Frames) return;
            var v = views[viewIndex];
            string Avg(ProfilerRecorder r) => r.Valid && r.Count > 0 ? Average(r).ToString("0") : "-";
            double ms = mainThread.Valid && mainThread.Count > 0 ? Average(mainThread) / 1e6 : 0;
            double gpu = gpuTimes.Count > 0 ? gpuTimes.Average() : 0;
            report.AppendLine($"{v.name} | {ms:0.00} | {gpu:0.00} | {string.Join(" | ", recorders.Select(Avg))}");
            foreach (var r in recorders) r.Dispose();
            mainThread.Dispose();
            report.AppendLine("    " + string.Join("  ", markers.Select(m => $"{m.Item1}={(m.Item2.Count > 0 ? Average(m.Item2) / 1e6 : 0):0.00}")));
            foreach (var m in markers) m.Item2.Dispose();
            viewIndex++;
            if (viewIndex >= views.Count) { Finish(); return; }
            StartView();
        }
    }

    static double Average(ProfilerRecorder r)
    {
        double sum = 0;
        int n = r.Count;
        for (int i = 0; i < n; i++) sum += r.GetSample(i).Value;
        return n > 0 ? sum / n : 0;
    }

    static void Finish()
    {
        EditorApplication.update -= Step;
        if (report != null) File.WriteAllText(Path.Combine(Root, $"perf_{runTag}.txt"), report.ToString());
        if (probe != null) Object.Destroy(probe.gameObject);
        foreach (var c in muted) if (c != null) c.enabled = true;
        muted.Clear();
        views = null;
        report = null;
    }

    // ---- reference images ----

    public static void Looks(string tag)
    {
        foreach (var v in LoadViews())
        {
            var go = new GameObject("Looks Camera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            var main = Camera.main;
            if (main != null)
            {
                cam.CopyFrom(main);
                var dst = cam.GetUniversalAdditionalCameraData();
                var src = main.GetUniversalAdditionalCameraData();
                dst.renderPostProcessing = src.renderPostProcessing;
                dst.antialiasing = src.antialiasing;
            }
            cam.enabled = false;
            cam.fieldOfView = v.fov;
            go.transform.SetPositionAndRotation(v.eye, Quaternion.LookRotation(v.target - v.eye));
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Root, $"look_{tag}_{v.name}.png"), tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
        }
    }

    // mergecheck (play mode): renders every view with the merged products, then with the originals, in the same
    // frame so flickering lamps can't differ, and diffs the two
    public static void MergeCheck()
    {
        if (StoreMeshMerge.Root == null) throw new System.Exception("nothing merged (needs play mode)");
        Looks("merged");
        StoreMeshMerge.Root.SetActive(false);
        foreach (var r in StoreMeshMerge.Hidden) r.enabled = true;
        Looks("unmerged");
        foreach (var r in StoreMeshMerge.Hidden) r.enabled = false;
        StoreMeshMerge.Root.SetActive(true);
        Diff("unmerged merged");
    }

    // diff a b: compares look_a_* with look_b_* and writes how far apart each pair is, plus an amplified diff image
    public static void Diff(string arg)
    {
        var p = arg.Split(' ');
        var sb = new StringBuilder();
        foreach (var v in LoadViews())
        {
            string pa = Path.Combine(Root, $"look_{p[0]}_{v.name}.png"), pb = Path.Combine(Root, $"look_{p[1]}_{v.name}.png");
            if (!File.Exists(pa) || !File.Exists(pb)) { sb.AppendLine($"{v.name}: missing"); continue; }
            var a = new Texture2D(2, 2); a.LoadImage(File.ReadAllBytes(pa));
            var b = new Texture2D(2, 2); b.LoadImage(File.ReadAllBytes(pb));
            var ca = a.GetPixels32(); var cb = b.GetPixels32();
            var d = new Color32[ca.Length];
            double sum = 0; int max = 0, over8 = 0;
            for (int i = 0; i < ca.Length; i++)
            {
                int dr = Mathf.Abs(ca[i].r - cb[i].r), dg = Mathf.Abs(ca[i].g - cb[i].g), db = Mathf.Abs(ca[i].b - cb[i].b);
                int m = Mathf.Max(dr, Mathf.Max(dg, db));
                sum += (dr + dg + db) / 3.0;
                max = Mathf.Max(max, m);
                if (m > 8) over8++;
                byte k = (byte)Mathf.Min(255, m * 8);
                d[i] = new Color32(k, k, k, 255);
            }
            sb.AppendLine($"{v.name}: mean {sum / ca.Length:0.00}/255  max {max}  pixels off by >8: {100.0 * over8 / ca.Length:0.00}%");
            var dt = new Texture2D(a.width, a.height, TextureFormat.RGB24, false);
            dt.SetPixels32(d); dt.Apply();
            File.WriteAllBytes(Path.Combine(Root, $"diff_{p[0]}_{p[1]}_{v.name}.png"), dt.EncodeToPNG());
            Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(dt);
        }
        File.WriteAllText(Path.Combine(Root, $"diff_{p[0]}_{p[1]}.txt"), sb.ToString());
    }
}

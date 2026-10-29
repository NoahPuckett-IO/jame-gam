#if DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Development builds only: opening a web build with "bench" in its URL parks a camera at fixed spots in the store
// and logs frame times there, so different builds can be compared on the same views. Release builds don't have it.
public class PerfBench : MonoBehaviour
{
    static readonly (string name, Vector3 eye, Vector3 target)[] Views =
    {
        ("spawn", new Vector3(253.4f, 9, -128), new Vector3(253.4f, 5, -60)),
        ("aisle_l3", new Vector3(124.96f, 9, -40), new Vector3(124.96f, 5, 30)),
        ("aisle_l2", new Vector3(189.2f, 9, 125), new Vector3(189.2f, 5, 60)),
        ("to_door", new Vector3(240, 9, 0), new Vector3(-280, 8, 0)),
        ("coolers", new Vector3(100, 10, 118), new Vector3(250, 6, 140)),
        ("overview", new Vector3(230, 48, -135), new Vector3(20, 15, 60)),
        ("boss", new Vector3(-3.5f, 10, -125), new Vector3(-3.5f, 3, -80)),
    };

    Camera cam;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!Application.absoluteURL.Contains("bench")) return;
        new GameObject("Perf Bench").AddComponent<PerfBench>();
    }

    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(5f);
        var main = Camera.main;
        foreach (var c in Camera.allCameras) if (c.targetTexture == null) c.enabled = false;
        cam = new GameObject("Bench Camera").AddComponent<Camera>();
        if (main != null)
        {
            cam.CopyFrom(main);
            var src = main.GetUniversalAdditionalCameraData();
            var dst = cam.GetUniversalAdditionalCameraData();
            dst.renderPostProcessing = src.renderPostProcessing;
            dst.antialiasing = src.antialiasing;
        }
        cam.targetTexture = null;
        cam.fieldOfView = 60f;
        cam.enabled = true;
        foreach (var v in Views) yield return Measure(v, "");

        // what each costs on the heaviest views, one at a time
        var heavy = Views.Where(v => v.name == "spawn" || v.name == "overview").ToArray();
        var urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        var data = cam.GetUniversalAdditionalCameraData();
        var ssao = FindFeature("ScreenSpaceAmbientOcclusion");
        var spots = FindObjectsByType<Light>().Where(l => l.type == LightType.Spot && l.shadows != LightShadows.None).ToArray();
        var lamps = FindObjectsByType<Light>().Where(l => l.type == LightType.Point && l.enabled).ToArray();
        var tests = new (string name, System.Action on, System.Action off)[]
        {
            ("no-ssao", () => ssao?.SetActive(false), () => ssao?.SetActive(true)),
            ("no-spot-shadow", () => { foreach (var l in spots) l.shadows = LightShadows.None; }, () => { foreach (var l in spots) l.shadows = LightShadows.Soft; }),
            ("no-point-lights", () => { foreach (var l in lamps) l.enabled = false; }, () => { foreach (var l in lamps) l.enabled = true; }),
            ("no-post", () => data.renderPostProcessing = false, () => data.renderPostProcessing = true),
            ("half-res", () => urp.renderScale = 0.5f, () => urp.renderScale = 1f),
            ("no-hdr", () => urp.supportsHDR = false, () => urp.supportsHDR = true),
        };
        foreach (var t in tests)
        {
            t.on();
            foreach (var v in heavy) yield return Measure(v, t.name);
            t.off();
        }
        Debug.Log("BENCH DONE");
    }

    static ScriptableRendererFeature FindFeature(string typeName)
    {
        var asset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        var list = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(asset) as ScriptableRendererData[];
        return list?.SelectMany(d => d.rendererFeatures).FirstOrDefault(f => f != null && f.GetType().Name.Contains(typeName));
    }

    IEnumerator Measure((string name, Vector3 eye, Vector3 target) v, string variant)
    {
        cam.transform.SetPositionAndRotation(v.eye, Quaternion.LookRotation(v.target - v.eye));
        yield return new WaitForSecondsRealtime(2f);
        var times = new List<float>();
        float end = Time.realtimeSinceStartup + 4f;
        while (Time.realtimeSinceStartup < end)
        {
            yield return null;
            times.Add(Time.unscaledDeltaTime * 1000f);
        }
        times.Sort();
        Debug.Log($"BENCH {v.name}{(variant == "" ? "" : " [" + variant + "]")} fps={1000f / times.Average():0.0} avg={times.Average():0.0}ms med={times[times.Count / 2]:0.0}ms p95={times[(int)(times.Count * 0.95f)]:0.0}ms worst={times[times.Count - 1]:0}ms");
    }
}
#endif

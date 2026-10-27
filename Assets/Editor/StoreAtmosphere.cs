using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

static class StoreAtmosphere
{
    const string ProfilePath = "Assets/Settings/StoreVolumeProfile.asset";
    static readonly Color LampColor = new Color(0.92f, 0.96f, 1f);

    public static void BuildCeilingLights(Transform parent, List<Vector2> spots, float ceilingY, float scale, Material fixtureMat)
    {
        var root = new GameObject("Ceiling Lights").transform;
        root.SetParent(parent);
        var rng = new System.Random(7);

        foreach (var p in spots)
        {
            var fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fixture.name = "Light Fixture";
            fixture.transform.SetParent(root);
            fixture.transform.position = new Vector3(p.x, ceilingY - 0.05f * scale, p.y);
            fixture.transform.localScale = new Vector3(0.3f * scale, 0.06f * scale, 1.2f * scale);
            fixture.GetComponent<Renderer>().sharedMaterial = fixtureMat;
            Object.DestroyImmediate(fixture.GetComponent<Collider>());

            var lightGo = new GameObject("Lamp");
            lightGo.transform.SetParent(fixture.transform, false);
            lightGo.transform.position = fixture.transform.position + Vector3.down * 0.3f * scale;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = LampColor;
            float drop = lightGo.transform.position.y;
            light.range = drop * 2f;
            light.intensity = 0.27f * drop * drop;
            light.shadows = LightShadows.None;

            bool flicker = rng.NextDouble() < 0.18;
            fixture.AddComponent<FlickerLight>().Setup(light, fixture.GetComponent<Renderer>(), LampColor * 1.7f, flicker);
        }
    }

    public static Material FixtureMaterial()
    {
        const string path = "Assets/Materials/LightFixture.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = Color.white;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", LampColor * 1.7f);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static Material Emissive(string name, Color color, float intensity)
    {
        string path = $"Assets/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color * intensity);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static Material Glass(string name, Color color)
    {
        string path = $"Assets/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 0.92f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static void ApplySceneLook(float scale)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.3f, 0.32f, 0.33f);
        RenderSettings.ambientEquatorColor = new Color(0.25f, 0.26f, 0.27f);
        RenderSettings.ambientGroundColor = new Color(0.17f, 0.17f, 0.17f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.08f, 0.12f, 0.11f);
        RenderSettings.fogDensity = 0.0018f;

        var sun = GameObject.Find("Directional Light");
        if (sun != null)
        {
            Undo.RecordObject(sun, "Store Atmosphere");
            sun.SetActive(false);
        }

        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
        {
            Undo.RecordObject(urp, "Store Atmosphere");
            urp.shadowDistance = 12f * scale;
            EditorUtility.SetDirty(urp);
        }

        var cam = Camera.main;
        if (cam != null)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            Undo.RecordObject(data, "Store Atmosphere");
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cam.backgroundColor = RenderSettings.fogColor;
        }

        var volumeGo = GameObject.Find("Global Volume");
        if (volumeGo == null)
        {
            volumeGo = new GameObject("Global Volume");
            Undo.RegisterCreatedObjectUndo(volumeGo, "Store Atmosphere");
        }
        var volume = volumeGo.GetComponent<Volume>();
        if (volume == null) volume = volumeGo.AddComponent<Volume>();
        Undo.RecordObject(volume, "Store Atmosphere");
        volume.isGlobal = true;
        volume.sharedProfile = BuildProfile(scale);
    }

    static VolumeProfile BuildProfile(float scale)
    {
        AssetDatabase.DeleteAsset(ProfilePath);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, ProfilePath);

        var tone = Add<Tonemapping>(profile);
        tone.mode.Override(TonemappingMode.ACES);

        var bloom = Add<Bloom>(profile);
        bloom.threshold.Override(1f);
        bloom.intensity.Override(0.6f);
        bloom.scatter.Override(0.7f);

        var color = Add<ColorAdjustments>(profile);
        color.postExposure.Override(0.1f);
        color.contrast.Override(18f);
        color.saturation.Override(0f);
        color.colorFilter.Override(new Color(0.93f, 1f, 0.97f));

        var split = Add<SplitToning>(profile);
        split.shadows.Override(new Color(0.3f, 0.5f, 0.48f));
        split.highlights.Override(new Color(0.85f, 0.9f, 0.88f));
        split.balance.Override(-40f);

        var lgg = Add<LiftGammaGain>(profile);
        lgg.lift.Override(new Vector4(0.95f, 1.02f, 1f, -0.03f));

        var wb = Add<WhiteBalance>(profile);
        wb.temperature.Override(-10f);
        wb.tint.Override(-4f);

        var vignette = Add<Vignette>(profile);
        vignette.intensity.Override(0.32f);
        vignette.smoothness.Override(0.5f);
        vignette.color.Override(Color.black);

        var dof = Add<DepthOfField>(profile);
        dof.mode.Override(DepthOfFieldMode.Gaussian);
        dof.gaussianStart.Override(4f * scale);
        dof.gaussianEnd.Override(16f * scale);
        dof.gaussianMaxRadius.Override(1.2f);
        dof.highQualitySampling.Override(true);

        AssetDatabase.SaveAssets();
        return profile;
    }

    static T Add<T>(VolumeProfile profile) where T : VolumeComponent
    {
        var c = profile.Add<T>();
        c.name = typeof(T).Name;
        AssetDatabase.AddObjectToAsset(c, profile);
        return c;
    }
}

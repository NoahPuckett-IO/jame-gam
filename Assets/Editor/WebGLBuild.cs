using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

static class WebGLBuild
{
    [MenuItem("JameGam/Build WebGL Game")]
    static void Build() => Build("TinyHenryWebGL");

    // a build to try out before release, kept apart from the folder that gets uploaded to itch
    [MenuItem("JameGam/Build WebGL Preview")]
    static void BuildPreview() => Build("TinyHenryWebGLPreview");

    static void Build(string folderName)
    {
        PerformanceSettings.Apply();
        PerformanceSettings.EnsureOcclusion();
        // the editor sometimes revalidates Henry.mat and drops its _EMISSION keyword; put his glow back before every build
        CharacterSetup.HenryMaterial();
        AssetDatabase.SaveAssets();
        // itch serves files without Content-Encoding headers, so Gzip needs the decompression fallback.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        // compile the game's code for speed rather than for a quicker build
        UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.RuntimeSpeed;

        string folder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory), folderName);
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/JameGamGame.unity" },
            locationPathName = folder,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        // GPU instancing stutters in browsers (every instanced shader is recompiled through ANGLE, and the batches
        // are re-uploaded whenever the view changes), so the web build keeps the SRP Batcher and leaves the
        // instanced shader variants out; Windows keeps instancing.
        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        urp.useSRPBatcher = true;
        BuildReport report;
        try { report = BuildPipeline.BuildPlayer(options); }
        finally
        {
            urp.useSRPBatcher = false;
            EditorUtility.SetDirty(urp);
            AssetDatabase.SaveAssetIfDirty(urp);
        }
        Debug.Log($"[WebGLBuild] {report.summary.result} in {report.summary.totalTime}, {report.summary.totalSize / (1024 * 1024)} MB -> {folder}");
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}

class StripWebGLInstancing : IPreprocessShaders
{
    static readonly ShaderKeyword Instancing = new ShaderKeyword("INSTANCING_ON");
    public static bool KeepForBench;

    public int callbackOrder => 0;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL || KeepForBench) return;
        for (int i = data.Count - 1; i >= 0; i--)
            if (data[i].shaderKeywordSet.IsEnabled(Instancing)) data.RemoveAt(i);
    }
}

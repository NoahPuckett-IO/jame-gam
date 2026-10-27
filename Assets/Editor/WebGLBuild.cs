using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

static class WebGLBuild
{
    [MenuItem("JameGam/Build WebGL Game")]
    static void Build()
    {
        // itch serves files without Content-Encoding headers, so Gzip needs the decompression fallback.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;

        string folder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory), "TinyHenryWebGL");
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/JameGamGame.unity" },
            locationPathName = folder,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[WebGLBuild] {report.summary.result} in {report.summary.totalTime}, {report.summary.totalSize / (1024 * 1024)} MB -> {folder}");
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}

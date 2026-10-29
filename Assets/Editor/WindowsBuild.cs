using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

static class WindowsBuild
{
    [MenuItem("JameGam/Build Windows Game %#&w")]
    static void Build() => Build("TinyHenryBuild");

    // a build to try out before release, kept apart from the folder that gets uploaded to itch
    [MenuItem("JameGam/Build Windows Preview")]
    static void BuildPreview() => Build("TinyHenryPreview");

    static void Build(string folderName)
    {
        PerformanceSettings.Apply();
        PerformanceSettings.EnsureOcclusion();
        string folder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory), folderName);
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/JameGamGame.unity" },
            locationPathName = Path.Combine(folder, "Tiny Henry.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[WindowsBuild] {report.summary.result} in {report.summary.totalTime}, {report.summary.totalSize / (1024 * 1024)} MB -> {folder}");
    }
}

using System.Linq;
using UnityEditor;

[InitializeOnLoad]
static class AddGameSceneToBuild
{
    const string ScenePath = "Assets/JameGamGame.unity";

    static AddGameSceneToBuild()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == ScenePath))
            return;

        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}

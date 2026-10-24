using System.Linq;
using UnityEditor;

// Makes sure JameGamGame.unity is the first scene in Build Settings.
// Runs whenever scripts reload; does nothing once the scene is already listed.
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

using UnityEditor;
using UnityEngine;

/// <summary>
/// Automatically sets MainMenu.unity as the first scene in the build settings when the project loads.
/// </summary>
[InitializeOnLoad]
public static class SetStartupScene
{
    static SetStartupScene()
    {
        SetMainMenuAsStartup();
    }

    [MenuItem("Tools/Set Main Menu as Startup Scene")]
    public static void SetMainMenuAsStartup()
    {
        var mainMenuPath = "Assets/Scenes/MainMenu.unity";
        if (!System.IO.File.Exists(mainMenuPath))
        {
            Debug.LogError($"MainMenu scene not found at {mainMenuPath}");
            return;
        }

        var scenes = EditorBuildSettings.scenes;
        var newScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
        // Remove any existing MainMenu entries
        newScenes.RemoveAll(s => s.path.EndsWith("MainMenu.unity"));
        // Insert at index 0 (first)
        newScenes.Insert(0, new EditorBuildSettingsScene(mainMenuPath, true));
        EditorBuildSettings.scenes = newScenes.ToArray();
        Debug.Log($"Set {mainMenuPath} as first scene in build settings.");
    }
}
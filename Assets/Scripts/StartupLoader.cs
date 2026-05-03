using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Forces MainMenu.unity to load on application start, before any other scene.
/// </summary>
public static class StartupLoader
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnBeforeSceneLoad()
    {
        Debug.Log("[StartupLoader] BeforeSceneLoad called.");
        // Ensure MainMenu is loaded
        string mainMenuPath = "Assets/Scenes/MainMenu.unity";
        // If the scene is already loaded, do nothing
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).path.EndsWith("MainMenu.unity"))
            {
                Debug.Log("[StartupLoader] MainMenu already loaded.");
                return;
            }
        }
        // Load MainMenu
        Debug.Log("[StartupLoader] Loading MainMenu.");
        SceneManager.LoadScene(mainMenuPath, LoadSceneMode.Single);
    }
}
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreateMainMenuScene : MonoBehaviour
{
    [MenuItem("Tools/Create/MainMenu Scene")]
    public static void CreateMainMenu()
    {
        // Create new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // Create MainMenu GameObject
        GameObject mainMenuObj = new GameObject("MainMenu");
        mainMenuObj.AddComponent<MainMenu>();
        
        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(mainMenuObj.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Save scene
        string path = "Assets/Scenes/MainMenu.unity";
        if (System.IO.File.Exists(path))
        {
            int i = 1;
            while (System.IO.File.Exists($"{path}.bak{i}"))
                i++;
            System.IO.File.Move(path, $"{path}.bak{i}");
        }
        EditorSceneManager.SaveScene(newScene, path);
        
        Debug.Log($"[CreateMainMenuScene] Created and saved scene at {path}");
    }
}
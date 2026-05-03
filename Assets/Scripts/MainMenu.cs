using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    void Awake()
    {
        // Ensure GameInitializer exists
        if (FindObjectOfType<GameInitializer>() == null)
        {
            new GameObject("GameInitializer").AddComponent<GameInitializer>();
        }
    }

    void OnGUI()
    {
        // Simple GUI layout
        GUI.skin.button.fontSize = 24;
        GUI.skin.toggle.fontSize = 20;

        float buttonWidth = 200;
        float buttonHeight = 50;
        float startX = (Screen.width - buttonWidth) / 2f;
        float startY = (Screen.height - (buttonHeight * 5 + 20 * 4)) / 2f;

        if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "Start Game"))
        {
            StartGame();
        }

        if (GUI.Button(new Rect(startX, startY + buttonHeight + 10, buttonWidth, buttonHeight), "Settings"))
        {
            OpenSettings();
        }

        if (GUI.Button(new Rect(startX, startY + 2 * (buttonHeight + 10), buttonWidth, buttonHeight), "Skin Shop"))
        {
            OpenSkinShop();
        }

        // Music toggle
        GUI.Label(new Rect(startX, startY + 3 * (buttonHeight + 10), 100, buttonHeight), "Music");
        bool newMusicOn = GUI.Toggle(new Rect(startX + 110, startY + 3 * (buttonHeight + 10), 50, buttonHeight), 
            PlayerPrefs.GetInt("MusicOn", 1) == 1, "");
        if (newMusicOn != (PlayerPrefs.GetInt("MusicOn", 1) == 1))
        {
            PlayerPrefs.SetInt("MusicOn", newMusicOn ? 1 : 0);
        }

        // SFX toggle
        GUI.Label(new Rect(startX, startY + 4 * (buttonHeight + 10), 100, buttonHeight), "SFX");
        bool newSfxOn = GUI.Toggle(new Rect(startX + 110, startY + 4 * (buttonHeight + 10), 50, buttonHeight), 
            PlayerPrefs.GetInt("SfxOn", 1) == 1, "");
        if (newSfxOn != (PlayerPrefs.GetInt("SfxOn", 1) == 1))
        {
            PlayerPrefs.SetInt("SfxOn", newSfxOn ? 1 : 0);
        }

        if (GUI.Button(new Rect(startX, startY + 5 * (buttonHeight + 10), buttonWidth, buttonHeight), "Quit Game"))
        {
            QuitGame();
        }
    }

    void StartGame()
    {
        SceneManager.LoadScene("Level2");
    }

    void OpenSettings()
    {
        // Placeholder
        Debug.Log("Settings panel not implemented in GUI version");
    }

    void OpenSkinShop()
    {
        // Placeholder
        Debug.Log("Skin shop panel not implemented in GUI version");
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
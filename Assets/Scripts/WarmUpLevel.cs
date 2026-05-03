using UnityEngine;
using UnityEngine.SceneManagement;

public class WarmUpLevel : MonoBehaviour
{
    public bool Completed { get; private set; } = false;

    void Awake()
    {
        // Mark as completed immediately since warm-up is not implemented
        Completed = true;
        // Optionally, load the main level after a short delay
        Invoke(nameof(LoadMainLevel), 0.5f);
    }

    void LoadMainLevel()
    {
        SceneManager.LoadScene("Level2");
    }
}

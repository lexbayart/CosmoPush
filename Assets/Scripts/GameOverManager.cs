
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Управляет меню проигрыша и перезагрузкой уровня (v17.0).
/// </summary>
public class GameOverManager : MonoBehaviour
{
    /// <summary>
    /// Перезапускает текущую сцену (привязать к кнопке Restart).
    /// </summary>
    public void RestartGame()
    {
        // Возвращаем время в норму
        Time.timeScale = 1.0f;
        // Перезагружаем текущую сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Возможность выйти из игры (опционально).
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }
}

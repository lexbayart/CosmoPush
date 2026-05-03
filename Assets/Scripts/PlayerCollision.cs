
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Обрабатывает физическое столкновение Космонавта с печеньками и препятствиями (v15.1).
/// </summary>
public class PlayerCollision : MonoBehaviour
{
    public PushupVerifier verifier; // Поле для Editor
    public Text eventText;        // Поле для Editor

    private Image _playerImg;
    private RectTransform _playerRt; 
    private PlayerCharacter _playerScript; // Референс для получения текущей линии (v15.0)
    
    public static int CookiesCollected = 0; // Для подсчёта рейтинга (v121.0)
    public static int StarsCollected = 0;   // Для звездного рейтинга (v122.0)
    public static int AsteroidHitsTaken = 0; // Удары кометой (v122.2)
    public static int RockHitsTaken = 0;     // Удары камнем (v122.2)

    void Start()
    {
        CookiesCollected = 0;
        StarsCollected = 0;
        AsteroidHitsTaken = 0; // Сброс (v122.2)
        RockHitsTaken = 0;     // Сброс (v122.2)
        if (verifier == null)
            verifier = GameObject.FindFirstObjectByType<PushupVerifier>();
        
        _playerScript = GameObject.FindFirstObjectByType<PlayerCharacter>();
        _playerRt = GetComponent<RectTransform>();
        _playerImg = GetComponent<Image>();
    }

    void Update()
    {
        if (Time.timeScale == 0) return; // ПАУЗА (v116.4): Заморозка логики столкновений

        // 🪲 BUGFIX #2: Используем статический список вместо FindObjectsByType каждый кадр
        var items = MoverItem.ActiveItems;
        
        // ВАЖНО: Итерируемся в обратном порядке на случай удаления объекта во время цикла
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];
            if (item == null) continue;
            
            RectTransform itemRt = item.GetComponent<RectTransform>();
            if (itemRt == null) continue;

            // Простая проверка пересечения прямоугольников (AABB)
            if (IsOverlapping(_playerRt, itemRt))
            {
                if (item.isAsteroid)
                {
                    if (_playerScript != null && item.laneIndex == _playerScript.verifier.CurrentLane)
                    {
                        // 🪲 BUGFIX #16: Счётчик ударов для рейтинга работает всегда
                        AsteroidHitsTaken++; 
                        HandleObstacleHit(item.gameObject);
                    }
                }
                else if (item.isObstacle)
                {
                    // 🪲 BUGFIX #16: Счётчик ударов для рейтинга работает всегда
                    RockHitsTaken++;
                    HandleObstacleHit(item.gameObject);
                }
                else if (item.isSpeedup)
                {
                    HandleSpeedupCollect(item.gameObject);
                }
                else if (item.isGoal)
                {
                    // Игнорируем: Спутник обрабатывается в PlayerCharacter для красивой стыковки по центру (v120.0)
                }
                else
                {
                    HandleCookieCollect(item.gameObject);
                }
            }
        }
    }

    bool IsOverlapping(RectTransform rt1, RectTransform rt2)
    {
        // ХИТБОКС (v22.5): Немного расширяем зону для печенек (1.2), чтобы их было легче собирать
        
        Vector3[] corners1 = new Vector3[4];
        rt1.GetWorldCorners(corners1);
        Rect r1 = new Rect(corners1[0].x, corners1[0].y, corners1[2].x - corners1[0].x, corners1[2].y - corners1[0].y);
        
        Vector3[] corners2 = new Vector3[4];
        rt2.GetWorldCorners(corners2);
        Rect r2 = new Rect(corners2[0].x, corners2[0].y, corners2[2].x - corners2[0].x, corners2[2].y - corners2[0].y);

        // Для печенек делаем хитбокс чуть больше визуального размера
        float hitSizeFactor = 1.0f;
        if (rt2.GetComponent<MoverItem>() != null)
        {
            MoverItem m = rt2.GetComponent<MoverItem>();
            if (m.isObstacle || m.isAsteroid) hitSizeFactor = 0.7f; // Уменьшаем хитбокс опасности (Честный уворот)
            else if (!m.isSpeedup) hitSizeFactor = 1.2f; // Бонус для сбора печенек
        }

        Rect r2Final = new Rect(
            r2.x - r2.width * (hitSizeFactor - 1) / 2,
            r2.y - r2.height * (hitSizeFactor - 1) / 2,
            r2.width * hitSizeFactor,
            r2.height * hitSizeFactor
        );

        return r1.Overlaps(r2Final);
    }

    void HandleCookieCollect(GameObject cookie)
    {
        // 1. Начисляем очки
        if (verifier != null)
        {
            verifier.AddExternalScore(10); 
        }
        
        // 2. СПАВНИМ ЭФФЕКТ (v2.9): Только искры к счетчику, без текста в центре
        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.SpawnParticleBurst(cookie.transform.position, Color.yellow, PushupUIHandler.ScoreTarget);
        }

        CookiesCollected++; // Учитываем сбор для звездного рейтинга (v121.0)
        Destroy(cookie);
    }

    void HandleSpeedupCollect(GameObject speedup)
    {
        if (verifier != null && verifier.rhythmManager != null)
        {
            verifier.rhythmManager.TriggerSpeedup(1.0f, 1.5f); // 1 секунда ускорения, 1.5x
            // Очки не добавляются, только ускорение
        }

        if (JuiceManager.Instance != null)
        {
            // Эффект ускорения (разлет горизонтальных линий)
            JuiceManager.Instance.SpawnSpeedLines(speedup.transform.position);
        }

        Destroy(speedup);
        StarsCollected++; // Учитываем сбор звязд для рейтинга (v122.0)
    }

    void HandleObstacleHit(GameObject obstacle)
    {
        // 🪲 BUGFIX #18: Неуязвимость полностью удалена. Урон наносится всегда.
        if (verifier != null)
        {
            verifier.TakeDamage();
        }

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.Shake(0.3f, 20f);
            JuiceManager.Instance.Flash(new Color(1f, 0.2f, 0f, 0.4f), 0.2f); 
            JuiceManager.Instance.SpawnParticleBurst(obstacle.transform.position, Color.red);
        }

        // ВАЖНО (v28.0): Уничтожаем препятствие сразу после удара.
        Destroy(obstacle);
    }
}


using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DebugGridOverlay v86.0 - Глобальная система разметки экрана для Pair Programming.🦾📊🏁
/// Позволяет точно указывать координаты элементов интерфейса по сетке 4x4.
/// Управление: Клавиша "Q" - Вкл/Выкл.
/// </summary>
public class DebugGridOverlay : MonoBehaviour
{
    private static DebugGridOverlay _instance;
    private Canvas _canvas;
    private bool _isVisible = false;

    // Авто-инициализация при старте игры (v86.0)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("DebugGridOverlay");
            _instance = go.AddComponent<DebugGridOverlay>();
            DontDestroyOnLoad(go);
            Debug.Log("DebugGridOverlay: Глобальная сетка инициализирована! Нажми 'Q' для вкл/выкл. 🏁");
        }
    }

    void Awake()
    {
        CreateGrid();
    }

    void Update()
    {
        // Переключение сетки (Q)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleVisibility();
        }

        // СИСТЕМНАЯ ПАУЗА (Space) - v94.0
        // Используем GetKeyDown, но добавляем лог для проверки
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("DebugPause: Нажат ПРОБЕЛ. Пытаюсь переключить TimeScale...");
            TogglePause();
        }
    }

    private void TogglePause()
    {
        if (Time.timeScale > 0f)
        {
            Time.timeScale = 0f;
            AudioListener.pause = true; // ГЛОБАЛЬНАЯ ОСТАНОВКА ЗВУКА И РИТМА (v95.0)
            Debug.Log("DebugPause: ИГРА ОСТАНОВЛЕНА (TimeScale = 0, Audio Paused).");
        }
        else
        {
            Time.timeScale = 1f;
            AudioListener.pause = false; // ГЛОБАЛЬНЫЙ ЗАПУСК (v95.0)
            Debug.Log("DebugPause: ИГРА ЗАПУЩЕНА (TimeScale = 1, Audio Resumed).");
        }
    }

    private void ToggleVisibility()
    {
        _isVisible = !_isVisible;
        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(_isVisible);
        }
    }

    private void CreateGrid()
    {
        // 1. СОЗДАНИЕ КАНВАСА НА ВЕРХНЕМ УРОВНЕ
        GameObject canvasObj = new GameObject("GridCanvas");
        canvasObj.transform.SetParent(transform);
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 32767; // МАКСИМАЛЬНЫЙ УРОВЕНЬ (v86.0)
        
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>(); // Хотя он не нужен для визуала

        // По умолчанию выключено
        canvasObj.SetActive(_isVisible);

        // 2. ОТРИСОВКА ЛИНИЙ И МЕТОК
        Color lineColor = new Color(0f, 1f, 1f, 0.4f); // Более четкий Циан
        Color axisColor = Color.yellow; // Желтый для краев (v88.0)
        Color labelColor = new Color(1f, 1f, 1f, 0.4f); // Бледный белый для центров

        string[] cols = { "A", "B", "C", "D" };
        string[] rows = { "1", "2", "3", "4" };

        // --- ВЕРТИКАЛЬНЫЕ ОСИ (A, B, C, D) СВЕРХУ ---
        for (int i = 0; i < 4; i++)
        {
            CreateLabel(canvasObj.transform, cols[i], 
                new Vector2(i * 0.25f + 0.125f, 0.97f), // По самому верху
                axisColor, 40, true);
        }

        // --- ГОРИЗОНТАЛЬНЫЕ ОСИ (1, 2, 3, 4) СЛЕВА ---
        for (int i = 0; i < 4; i++)
        {
            CreateLabel(canvasObj.transform, rows[i], 
                new Vector2(0.02f, 1f - (i * 0.25f + 0.125f)), // По левому краю
                axisColor, 40, true);
        }

        // Вертикальные линии (разделители между колонками)
        for (int i = 1; i < 4; i++)
        {
            CreateLine(canvasObj.transform, 
                new Vector2(i * 0.25f, 0), new Vector2(i * 0.25f, 1), 
                new Vector2(2f, 0), lineColor);
        }

        // Горизонтальные линии (разделители между рядами)
        for (int i = 1; i < 4; i++)
        {
            CreateLine(canvasObj.transform, 
                new Vector2(0, i * 0.25f), new Vector2(1, i * 0.25f), 
                new Vector2(0, 2f), lineColor);
        }

        // 3. СОЗДАНИЕ МЕТОК В ЦЕНТРАХ ЯЧЕЕК
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                CreateLabel(canvasObj.transform, cols[c] + rows[r], 
                    new Vector2(c * 0.25f + 0.125f, 1f - (r * 0.25f + 0.125f)), 
                    labelColor, 24, false);
            }
        }
    }

    private void CreateLine(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Color color)
    {
        GameObject line = new GameObject("GridLine", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(parent, false);
        
        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;

        Image img = line.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private void CreateLabel(Transform parent, string text, Vector2 anchor, Color color, int fontSize, bool isBold)
    {
        GameObject labelObj = new GameObject("Label_" + text, typeof(RectTransform), typeof(Text));
        labelObj.transform.SetParent(parent, false);

        RectTransform rt = labelObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(150, 100);

        Text txt = labelObj.GetComponent<Text>();
        txt.text = text;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = fontSize;
        if (isBold) txt.fontStyle = FontStyle.Bold;
        
        // В новых версиях Unity Arial.ttf заменен на LegacyRuntime.ttf (v87.0)
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        
        txt.raycastTarget = false;
        
        // Тень для читаемости
        Shadow shadow = labelObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.7f);
        shadow.effectDistance = new Vector2(2, -2);
    }
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Система дорожек для Cosmo Push (v20.1 STABLE).
/// Поддерживает 2 активные линии (Верх/Низ).
/// </summary>
public class LaneSystem : MonoBehaviour
{
    [Header("Lane Positions (Visual Editor v29.0)")]
    public float HighY = 135f; 
    public float LowY = -270f; // Сдвинуто на стык 3 и 4 ряда сетки (v119.0)

    [HideInInspector] public Image HighLine;
    [HideInInspector] public Image LowLine;

    private Color normalColor = new Color(0f, 1f, 1f, 0.2f);
    private Color activeColor = new Color(1f, 0.9f, 0f, 0.8f);

    public void Setup(float canvasHeight)
    {
        // Теперь настраивается через Инспектор! (v29.0)
    }

    void Awake()
    {
        // ПРИНУДИТЕЛЬНО ПЕРЕЗАПИСЫВАЕМ ЗНАЧЕНИЯ ИНСПЕКТОРА
        HighY = 135f;  // Точно по центру ячеек A2, B2, C2, D2
        LowY = -270f;  // v119.0: Точно на стыке 3-го и 4-го ряда (между ячейками x3 и x4)

        // СИНХРОНИЗАЦИЯ: двигаем сами картинки (визуальные линии) к космонавту
        if (HighLine != null)
        {
            RectTransform rtHigh = HighLine.GetComponent<RectTransform>();
            if (rtHigh != null) rtHigh.anchoredPosition = new Vector2(rtHigh.anchoredPosition.x, HighY);
        }
        if (LowLine != null)
        {
            RectTransform rtLow = LowLine.GetComponent<RectTransform>();
            if (rtLow != null) rtLow.anchoredPosition = new Vector2(rtLow.anchoredPosition.x, LowY);
        }
    }

    public float GetLaneY(int laneIndex)
    {
        if (laneIndex == 0) return HighY;
        return LowY; // Всего 2 линии (v18.0)
    }

    public void HighlightLane(int laneIndex)
    {
        if (HighLine != null) HighLine.color = (laneIndex == 0) ? activeColor : normalColor;
        if (LowLine != null) LowLine.color = (laneIndex == 1) ? activeColor : normalColor;
    }
    void Update()
    {
        // СОВМЕСТНОЕ СКРЫТИЕ С ХИТБОКСАМИ (v28.0)
        bool show = MoverItem.ShowDebug;
        if (HighLine != null && HighLine.enabled != show) HighLine.enabled = show;
        if (LowLine != null && LowLine.enabled != show) LowLine.enabled = show;
    }
}

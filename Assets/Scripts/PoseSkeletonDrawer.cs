
using UnityEngine;
using UnityEngine.UI;

public class PoseSkeletonDrawer : MonoBehaviour
{
    public WebcamPoseDetection tracker;
    private GameObject[] _lines;
    private GameObject[] _dots;
    private RectTransform _rect;

    private int[,] segments = new int[,] {
        {11, 12}, {11, 23}, {12, 24}, {23, 24}, // Торс
        {11, 13}, {13, 15}, // Левая рука
        {12, 14}, {14, 16}, // Правая рука
        {23, 25}, {25, 27}, // Левая нога
        {24, 26}, {26, 28}, // Правая нога
        {0, 1}, {1, 2}, {2, 3}, {3, 7}, // Левый глаз/ухо
        {0, 4}, {4, 5}, {5, 6}, {6, 8}, // Правый глаз/ухо
        {9, 10} // Рот
    };

    void Start()
    {
        _rect = GetComponent<RectTransform>();
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        _rect.sizeDelta = Vector2.zero;
        _rect.anchoredPosition = Vector2.zero;

        _lines = new GameObject[segments.GetLength(0)];
        for (int i = 0; i < _lines.Length; i++)
        {
            _lines[i] = CreateUIElement("Line_" + i, new Color(0, 1f, 1f, 0.7f), new Vector2(0, 6)); 
            _lines[i].GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
        }

        _dots = new GameObject[33];
        for (int i = 0; i < 33; i++)
        {
            // ПЛЕЧИ (11, 12): КРАСНЫЕ И КРУПНЫЕ (v7.0)
            bool isShoulder = (i == 11 || i == 12);
            Color c = isShoulder ? Color.red : Color.yellow;
            Vector2 size = isShoulder ? new Vector2(30, 30) : new Vector2(15, 15);
            _dots[i] = CreateUIElement("Landmark_" + i, c, size);
        }
    }

    GameObject CreateUIElement(string n, Color c, Vector2 size)
    {
        GameObject go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(transform, false); 
        Image img = go.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; 
        rt.anchorMax = Vector2.zero; 
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        return go;
    }

    void Update()
    {
        // v136.7: Авто-подхват синглтона при смене сцены
        if (tracker == null && WebcamPoseDetection.Instance != null)
            tracker = WebcamPoseDetection.Instance;

        // СОВМЕСТНОЕ СКРЫТИЕ С ХИТБОКСАМИ (v27.0)
        if (!MoverItem.ShowDebug || tracker == null || !tracker.IsTracking || _rect == null)
        {
            foreach (var l in _lines) if (l != null) l.SetActive(false);
            foreach (var d in _dots) if (d != null) d.SetActive(false);
            return;
        }

        float w = _rect.rect.width;
        float h = _rect.rect.height;
        if (w == 0 || h == 0) return;

        for (int i = 0; i < _lines.Length; i++) DrawLine(_lines[i], segments[i, 0], segments[i, 1], w, h);
        for (int i = 0; i < 33; i++) DrawDot(_dots[i], i, w, h);
    }

    void DrawDot(GameObject dot, int idx, float w, float h)
    {
        if (tracker.GetKeypointVisibility(idx) < 0.2f) { dot.SetActive(false); return; }
        dot.SetActive(true);
        dot.GetComponent<RectTransform>().anchoredPosition = new Vector2(tracker.GetKeypointX(idx) * w, tracker.GetKeypointY(idx) * h);
    }

    void DrawLine(GameObject line, int idx1, int idx2, float w, float h)
    {
        if (tracker.GetKeypointVisibility(idx1) < 0.2f || tracker.GetKeypointVisibility(idx2) < 0.2f) { line.SetActive(false); return; }
        line.SetActive(true);
        var rt = line.GetComponent<RectTransform>();
        Vector2 start = new Vector2(tracker.GetKeypointX(idx1) * w, tracker.GetKeypointY(idx1) * h);
        Vector2 end = new Vector2(tracker.GetKeypointX(idx2) * w, tracker.GetKeypointY(idx2) * h);
        Vector2 dir = (end - start);
        rt.sizeDelta = new Vector2(dir.magnitude, 6f);
        rt.anchoredPosition = start;
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }
}

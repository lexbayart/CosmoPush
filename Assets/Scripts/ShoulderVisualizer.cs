
using UnityEngine;
using UnityEngine.UI;

public class ShoulderVisualizer : MonoBehaviour
{
    public WebcamPoseDetection poseDetection;
    public RectTransform leftMarker;
    public RectTransform rightMarker;
    public RectTransform canvasRect;

    void Start()
    {
        if (leftMarker == null || rightMarker == null)
        {
            GameObject l = new GameObject("LeftShoulderMarker", typeof(Image));
            l.transform.SetParent(this.transform);
            leftMarker = l.GetComponent<RectTransform>();
            leftMarker.sizeDelta = new Vector2(40, 40);
            l.GetComponent<Image>().color = Color.red;

            GameObject r = new GameObject("RightShoulderMarker", typeof(Image));
            r.transform.SetParent(this.transform);
            rightMarker = r.GetComponent<RectTransform>();
            rightMarker.sizeDelta = new Vector2(40, 40);
            r.GetComponent<Image>().color = Color.red;
        }
    }

    void Update()
    {
        // v136.7: Авто-подхват синглтона при смене сцены
        if (poseDetection == null && WebcamPoseDetection.Instance != null)
            poseDetection = WebcamPoseDetection.Instance;

        if (poseDetection == null || !poseDetection.IsTracking || canvasRect == null) return;

        UpdateMarker(leftMarker, 11);
        UpdateMarker(rightMarker, 12);
    }

    void UpdateMarker(RectTransform marker, int index)
    {
        float x = poseDetection.GetKeypointX(index);
        float y = poseDetection.GetKeypointY(index);

        // Масштабируем до размеров канваса
        float screenX = (x - 0.5f) * canvasRect.sizeDelta.x;
        float screenY = (0.5f - y) * canvasRect.sizeDelta.y;

        marker.anchoredPosition = new Vector2(screenX, screenY);
    }
}

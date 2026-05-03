
using UnityEngine;
using UnityEngine.UI;

public class CameraPreviewHandler : MonoBehaviour
{
    public WebcamPoseDetection tracker;
    public RawImage rawImage;

    void Start()
    {
        // v136.7: Немедленно подхватить синглтон если он уже есть (Level 2+)
        if (tracker == null && WebcamPoseDetection.Instance != null)
            tracker = WebcamPoseDetection.Instance;

        if (rawImage != null)
        {
            rawImage.color = new Color(1f, 1f, 1f, 0.2f); // 20% прозрачности

            // Если камера уже работает — сразу кидаем текстуру
            if (tracker != null && tracker.CamTexture != null && tracker.CamTexture.isPlaying)
            {
                rawImage.texture = tracker.CamTexture;
                Debug.Log("[Camera] Texture pre-assigned in Start() for new level.");
            }

            // СОЗДАЕМ КОСМИЧЕСКИЙ ФОН ПРЯМО ЗА КАМЕРОЙ
            // Проверяем: не дублируем если фон уже создан
            if (rawImage.transform.parent.Find("SpaceBackground") == null)
            {
                GameObject spaceBg = new GameObject("SpaceBackground", typeof(RectTransform), typeof(Image));
                spaceBg.transform.SetParent(rawImage.transform.parent, false);
                spaceBg.transform.SetSiblingIndex(rawImage.transform.GetSiblingIndex());
                
                RectTransform bgRt = spaceBg.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
                
                Image bgImg = spaceBg.GetComponent<Image>();
                bgImg.color = new Color(0.02f, 0.05f, 0.15f, 1f);

                Texture2D[] sparkleTexs = Resources.LoadAll<Texture2D>("Emojis/Background");
                if (sparkleTexs.Length > 0)
                {
                    Sprite sparkleObj = Sprite.Create(sparkleTexs[0], new Rect(0,0,sparkleTexs[0].width, sparkleTexs[0].height), new Vector2(0.5f, 0.5f));
                    for(int i = 0; i < 30; i++)
                    {
                        GameObject starObj = new GameObject("Sparkle", typeof(RectTransform), typeof(Image), typeof(CosmicSparkle));
                        starObj.transform.SetParent(spaceBg.transform, false);
                        RectTransform sRt = starObj.GetComponent<RectTransform>();
                        float sz = Random.Range(15f, 45f);
                        sRt.sizeDelta = new Vector2(sz, sz);
                        sRt.anchorMin = sRt.anchorMax = new Vector2(0.5f, 0.5f);
                        sRt.anchoredPosition = new Vector2(Random.Range(-900f, 900f), Random.Range(-500f, 500f));
                        starObj.GetComponent<Image>().sprite = sparkleObj;
                        CosmicSparkle spark = starObj.GetComponent<CosmicSparkle>();
                        spark.speed = Random.Range(0.5f, 2.5f);
                        spark.offset = Random.Range(0f, 5f);
                    }
                }
            }
        }
    }

    void Update()
    {
        // v136.7: Авто-подхват глобальной камеры синглтона
        if (tracker == null && WebcamPoseDetection.Instance != null)
        {
            tracker = WebcamPoseDetection.Instance;
            if (rawImage != null) rawImage.texture = null; // Сброс — принудим переназначение
        }

        // v136.8: Авто-поиск rawImage если ссылка потеряна
        if (rawImage == null)
            rawImage = GetComponentInChildren<RawImage>();
        if (rawImage == null)
            rawImage = FindFirstObjectByType<RawImage>();

        if (tracker == null || rawImage == null) return;

        if (tracker.CamTexture != null && tracker.CamTexture.isPlaying)
        {
            // Всегда назначаем — убрали проверку на равенство (v136.8)
            if (rawImage.texture != tracker.CamTexture)
            {
                rawImage.texture = tracker.CamTexture;
                rawImage.color = new Color(1f, 1f, 1f, 0.2f); // Гарантируем видимость
                Debug.Log("[Camera v136.8] Texture assigned to RawImage!");
            }

            // Поворот и отражение (для селфи-режима)
            float angle = tracker.CamTexture.videoRotationAngle;
            rawImage.rectTransform.localEulerAngles = new Vector3(0, 0, -angle);
            
            // Если на Mac камера "зеркальная", инвертируем X
            rawImage.rectTransform.localScale = new Vector3(tracker.CamTexture.videoVerticallyMirrored ? -1 : 1, 1, 1);
            
            // Сохраняем пропорции (Aspect Ratio)
            AspectRatioFitter fitter = rawImage.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = rawImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = tracker.CamTexture.width > 0
                ? (float)tracker.CamTexture.width / tracker.CamTexture.height
                : 16f / 9f;
        }
        else if (tracker.CamTexture != null && !tracker.CamTexture.isPlaying)
        {
            // Камера есть, но не воспроизводится — попробуем снова запустить
            Debug.LogWarning("[Camera] CamTexture not playing — restarting...");
            tracker.CamTexture.Play();
        }
    }
}

public class CosmicSparkle : MonoBehaviour
{
    public float speed = 1f;
    public float offset = 0f;
    private Image _img;

    void Start() { _img = GetComponent<Image>(); }

    void Update()
    {
        if (_img == null) return;
        Color c = _img.color;
        // Мерцание от 0.1 до 1.0 альфы
        c.a = 0.1f + 0.9f * Mathf.PingPong(Time.time * speed + offset, 1f);
        _img.color = c;
    }
}


using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Глобальный менеджер эффектов "Juice" (Встряска, Вспышка, Всплывающий текст).
/// v1.0 - Ориентирован на UI Canvas.
/// </summary>
public class JuiceManager : MonoBehaviour
{
    public static JuiceManager Instance;

    private Canvas _rootCanvas;
    private RectTransform _mainContainer;
    private Image _flashOverlay;

    void Awake()
    {
        Instance = this;
        // ГАРАНТИЯ КОНТЕЙНЕРА (v1.3)
        _mainContainer = GetComponent<RectTransform>(); 
        if (_mainContainer == null) _mainContainer = transform as RectTransform;
        
        // Создаем оверлей для вспышки внутри этого же контейнера
        GameObject flashObj = new GameObject("HitFlashOverlay", typeof(RectTransform), typeof(Image));
        flashObj.transform.SetParent(transform, false); 
        _flashOverlay = flashObj.GetComponent<Image>();
        _flashOverlay.color = new Color(1, 1, 1, 0);
        _flashOverlay.raycastTarget = false;
        
        RectTransform rt = flashObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public void Shake(float duration = 0.2f, float amount = 15f)
    {
        StopCoroutine("ShakeCoroutine");
        StartCoroutine(ShakeCoroutine(duration, amount));
    }

    private IEnumerator ShakeCoroutine(float duration, float amount)
    {
        Vector2 originalPos = _mainContainer.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * amount;
            float y = Random.Range(-1f, 1f) * amount;
            _mainContainer.anchoredPosition = originalPos + new Vector2(x, y);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        _mainContainer.anchoredPosition = originalPos;
    }

    public void Flash(Color color, float duration = 0.15f)
    {
        StopCoroutine("FlashCoroutine");
        StartCoroutine(FlashCoroutine(color, duration));
    }

    private IEnumerator FlashCoroutine(Color color, float duration)
    {
        _flashOverlay.color = color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(color.a, 0, elapsed / duration);
            _flashOverlay.color = new Color(color.r, color.g, color.b, alpha);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        _flashOverlay.color = new Color(0, 0, 0, 0);
    }

    public void SpawnFloatingText(string text, Vector2 screenPos, Color color)
    {
        // ГАРАНТИЯ ПРАВИЛЬНОГО РОДИТЕЛЯ (v1.3)
        // JuiceManager сам находится на Canvas, поэтому спавним внутри него.
        GameObject textObj = new GameObject("FloatingText", typeof(RectTransform), typeof(Text), typeof(Outline));
        textObj.transform.SetParent(transform, false); 
        
        Text t = textObj.GetComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 50;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        
        Outline ot = textObj.GetComponent<Outline>();
        ot.effectColor = Color.black; 
        
        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.position = screenPos;
        rt.sizeDelta = new Vector2(300, 100);

        StartCoroutine(FloatingTextCoroutine(textObj, rt));
    }

    public void SpawnFlyingScore(Vector3 worldPos, string text, RectTransform targetScore)
    {
        GameObject textObj = new GameObject("FlyingScore", typeof(RectTransform), typeof(Text), typeof(Outline));
        textObj.transform.SetParent(transform, false);
        
        Text t = textObj.GetComponent<Text>();
        t.text = text;
        
        // БЕЗОПАСНЫЙ ШРИФТ (берем из любой надписи на сцене, если Arial нет)
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) 
        {
            Text anyText = FindFirstObjectByType<Text>();
            if (anyText != null) font = anyText.font;
        }
        t.font = font;
        
        t.fontSize = 80; // УВЕЛИЧЕНО для заметности
        t.color = Color.yellow;
        t.alignment = TextAnchor.MiddleCenter;
        
        Outline ot = textObj.GetComponent<Outline>();
        ot.effectColor = Color.black;
        
        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.position = worldPos;
        rt.sizeDelta = new Vector2(300, 100);

        StartCoroutine(FlyingScoreCoroutine(textObj, rt, targetScore));
        SpawnParticleBurst(worldPos, Color.yellow, targetScore); // Желтые летят в счетчик
    }

    private IEnumerator FlyingScoreCoroutine(GameObject obj, RectTransform rt, RectTransform target)
    {
        float duration = 0.8f;
        float elapsed = 0f;
        
        // Используем АБСОЛЮТНЫЕ координаты экрана (position), а не локальные (anchoredPosition)
        Vector3 startPos = rt.position; 
        
        while (elapsed < duration && obj != null)
        {
            float p = elapsed / duration;
            // Плавное ускорение к цели
            float curve = p * p; 
            
            if (target != null && target.gameObject.activeInHierarchy)
            {
                rt.position = Vector3.Lerp(startPos, target.position, curve);
                rt.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.4f, curve);
            }
            else
            {
                // FALLBACK: Если счетчик не найден, просто летим вверх
                rt.position = startPos + new Vector3(0, curve * 400f, 0);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(obj);
    }

    public void SpawnParticleBurst(Vector3 worldPos, Color color, RectTransform target = null)
    {
        for (int i = 0; i < 15; i++) // Больше частиц
        {
            GameObject p = new GameObject("ScorePart", typeof(RectTransform), typeof(Image));
            p.transform.SetParent(transform, false);
            p.GetComponent<Image>().color = color;
            
            RectTransform rt = p.GetComponent<RectTransform>();
            rt.position = worldPos; // Старт из точки сбора
            rt.sizeDelta = new Vector2(40, 40); // КРУПНЫЕ кубики
            
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            float speed = UnityEngine.Random.Range(400f, 1000f); 
            StartCoroutine(ScoreParticleCoroutine(p, rt, dir, speed, target));
        }
    }

    private IEnumerator ScoreParticleCoroutine(GameObject obj, RectTransform rt, Vector2 dir, float speed, RectTransform target)
    {
        float duration = 0.6f;
        float elapsed = 0f;
        Vector3 startPos = rt.position; // Запоминаем изначальную абсолютную позицию
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            
            if (target != null && target.gameObject.activeInHierarchy)
            {
                // Логика "Взрыв + Магнит":
                // 1. Взрываются в стороны (t * (1 - t) создает параболу разлета)
                Vector3 explosionOffset = (Vector3)dir * speed * t * (1f - t); 
                // 2. Притягиваются к цели (t * t дает плавное ускорение к счетчику)
                rt.position = Vector3.Lerp(startPos, target.position, t * t) + explosionOffset;
            }
            else
            {
                // Используем локальное независимое смещение (просто разлетаются в стороны)
                rt.anchoredPosition += dir * speed * Time.deltaTime;
            }

            rt.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            
            // Вращение (сочности!)
            rt.Rotate(0, 0, speed * Time.deltaTime);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(obj);
    }

    public void SpawnSpeedLines(Vector3 worldPos)
    {
        for (int i = 0; i < 10; i++)
        {
            GameObject p = new GameObject("SpeedLine", typeof(RectTransform), typeof(Image));
            p.transform.SetParent(transform, false);
            p.GetComponent<Image>().color = new Color(0f, 1f, 1f, 0.8f); // Полупрозрачный Циан (голубой)
            
            RectTransform rt = p.GetComponent<RectTransform>();
            // Разбрасываем начальную высоту немного хаотично
            rt.position = worldPos + new Vector3(0, UnityEngine.Random.Range(-50f, 50f), 0); 
            // Линия: длинная и узкая
            rt.sizeDelta = new Vector2(UnityEngine.Random.Range(100f, 300f), UnityEngine.Random.Range(4f, 12f)); 
            
            // Вектор сильно влево с легким разбросом по высоте
            Vector2 dir = new Vector2(-1f, UnityEngine.Random.Range(-0.1f, 0.1f)).normalized;
            float speed = UnityEngine.Random.Range(2000f, 4000f); 
            StartCoroutine(SpeedLineCoroutine(p, rt, dir, speed));
        }
    }

    private IEnumerator SpeedLineCoroutine(GameObject obj, RectTransform rt, Vector2 dir, float speed)
    {
        float duration = 0.4f;
        float elapsed = 0f;
        Image img = obj.GetComponent<Image>();
        
        while (elapsed < duration)
        {
            rt.anchoredPosition += dir * speed * Time.deltaTime;
            
            float t = elapsed / duration;
            // Линии сильно растягиваются в длину и сплющиваются в высоту
            rt.localScale = new Vector3(1f + t * 2f, 1f - t, 1f); 
            
            // Исчезают со временем
            Color c = img.color;
            c.a = 0.8f * (1f - t);
            img.color = c;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(obj);
    }

    private IEnumerator FloatingTextCoroutine(GameObject obj, RectTransform rt)
    {
        float duration = 1.0f;
        float elapsed = 0f;
        Vector2 startPos = rt.anchoredPosition;
        Text t = obj.GetComponent<Text>();
        Outline s = obj.GetComponent<Outline>();

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            rt.anchoredPosition = startPos + new Vector2(0, progress * 150f);
            
            Color tc = t.color; tc.a = 1f - progress; t.color = tc;
            if(s) { Color sc = s.effectColor; sc.a = 1f - progress; s.effectColor = sc; }

            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(obj);
    }

    public void SpawnAsteroidTrail(Vector3 worldPos)
    {
        GameObject p = new GameObject("AsteroidTrail", typeof(RectTransform), typeof(Image));
        p.transform.SetParent(transform, false);
        p.GetComponent<Image>().color = new Color(1f, 0.4f, 0f, 0.8f); // Огненно-оранжевый
        
        RectTransform rt = p.GetComponent<RectTransform>();
        rt.position = worldPos + new Vector3(UnityEngine.Random.Range(-15f, 15f), UnityEngine.Random.Range(-15f, 15f), 0);
        rt.sizeDelta = new Vector2(40f, 40f); // Достаточно крупные кубики шлейфа
        
        // Хаотичное вращение
        rt.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));
        
        StartCoroutine(AsteroidTrailCoroutine(p, rt));
    }

    private IEnumerator AsteroidTrailCoroutine(GameObject obj, RectTransform rt)
    {
        float duration = 0.6f;
        float elapsed = 0f;
        Image img = obj.GetComponent<Image>();
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Уменьшаются и тают
            rt.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            
            Color c = img.color;
            c.a = 0.8f * (1f - t);
            img.color = c;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(obj);
    }
}


using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Реализует эффект реактивной турбины (квадратные частицы огня).
/// v1.0 - Ориентирован на UI Canvas.
/// </summary>
public class AstroTurbine : MonoBehaviour
{
    [Header("Настройки частиц")]
    public float baseSpawnRate = 0.05f; // Интервал спавна (меньше = чаще)
    public float driftSpeed = 300f;     // Базовая скорость улета частиц
    public Vector2 particleSizeRange = new Vector2(10, 25);
    
    [Header("Цвета огня")]
    public Color[] fireColors = new Color[] { 
        new Color(1, 0.5f, 0),    // Оранжевый
        new Color(1, 1, 0),       // Желтый
        new Color(1, 0.2f, 0),    // Красный
        new Color(1, 0.9f, 0.5f)  // Светло-желтый
    };

    private RectTransform _parentRt;
    private float _timer = 0f;
    private float _currentVelocityY = 0f;
    private Sprite _squareSprite;

    void Start()
    {
        _parentRt = transform.parent as RectTransform;
        // Создаем простой квадрат (v116.5), чтобы не вызывать ошибку поиска Background.psd в новых версиях Unity
        _squareSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }

    public void SetVelocity(float vy)
    {
        _currentVelocityY = vy;
    }

    void Update()
    {
        // Интенсивность зависит от вертикальной скорости (смена дорожки)
        float intensity = Mathf.Clamp01(Mathf.Abs(_currentVelocityY) / 1500f); 
        float currentRate = Mathf.Lerp(baseSpawnRate, baseSpawnRate * 0.2f, intensity);
        
        _timer += Time.deltaTime;
        if (_timer >= currentRate)
        {
            SpawnParticle(intensity);
            _timer = 0f;
        }
    }

    void SpawnParticle(float intensity)
    {
        GameObject p = new GameObject("TurbineParticle", typeof(RectTransform), typeof(Image));
        p.transform.SetParent(transform.parent, false);
        p.transform.SetSiblingIndex(transform.GetSiblingIndex()); // Сзади космонавта
        
        Image img = p.GetComponent<Image>();
        img.sprite = _squareSprite;
        img.color = fireColors[Random.Range(0, fireColors.Length)];
        
        RectTransform rt = p.GetComponent<RectTransform>();
        float size = Random.Range(particleSizeRange.x, particleSizeRange.y);
        // Если "газует" сильно, частицы чуть больше
        if (intensity > 0.5f) size *= 1.5f; 
        rt.sizeDelta = new Vector2(size, size);
        
        // Позиция: используем МИРОВЫЕ координаты (position) для точности (v1.6)
        rt.position = transform.position;
        // Смещение за спину (в локальных координатах UI)
        // Увеличено до -90f, так как персонаж стал крупнее (v2.1)
        rt.anchoredPosition += new Vector2(-90f, 0); 

        // Вектор движения: ИНВЕРТИРУЕМ (v1.6), чтобы летели НАЗАД
        Vector2 driftDir = new Vector2(-1.5f, -(_currentVelocityY / 500f)).normalized;
        float speed = driftSpeed + (intensity * 800f);

        StartCoroutine(ParticleLife(p, rt, driftDir, speed));
    }

    IEnumerator ParticleLife(GameObject obj, RectTransform rt, Vector2 dir, float speed)
    {
        float life = Random.Range(0.3f, 0.6f);
        float elapsed = 0f;
        Image img = obj.GetComponent<Image>();
        Vector2 startPos = rt.anchoredPosition;

        while (elapsed < life)
        {
            float p = elapsed / life;
            rt.anchoredPosition = startPos + (dir * speed * elapsed);
            
            // Затухание и изменение цвета к серому/прозрачному
            Color c = img.color;
            c.a = 1f - p;
            img.color = c;
            
            // Уменьшение размера
            rt.localScale = Vector3.one * (1f - p);

            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(obj);
    }
}

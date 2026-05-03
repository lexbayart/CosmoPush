
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BGStoneMover v47.0 - Легковесный скрипт для фонового декора.🦾🪐🌑
/// Полностью изолирован от геймплейных MoverItem.
/// </summary>
public class BGStoneMover : MonoBehaviour
{
    public float speed;
    public float rotationSpeed;
    public Vector2 moveDirection = Vector2.left; 
    public float destroyX = -4000f; 
    public bool enableFadeOut = false; // v73.0: Включение эффекта сгорания
    public float fadeStartX = 0f;      // Точка начала исчезновения
    
    private RectTransform _rt;
    private RhythmManager _rm;
    private Image _img;
    private Vector3 _baseScale;
    private AudioSource _rmAudio; // 🪲 BUGFIX #11: Кэш AudioSource

    void Start()
    {
        _rt = GetComponent<RectTransform>();
        _rm = GameObject.FindFirstObjectByType<RhythmManager>();
        _img = GetComponent<Image>();
        _baseScale = transform.localScale;
        if (_rm != null) _rmAudio = _rm.GetComponent<AudioSource>(); // 🪲 BUGFIX #11
    }

    void Update()
    {
        if (_rm == null || !_rm.IsPlaying) return;

        // 🪲 BUGFIX #11: Используем кэшированный AudioSource
        float dt = Time.deltaTime * ((_rmAudio != null) ? _rmAudio.pitch : 1f);
        _rt.anchoredPosition += moveDirection.normalized * (speed * dt);

        // ЭФФЕКТ СГОРАНИЯ (v73.0)
        if (enableFadeOut)
        {
            float curX = _rt.anchoredPosition.x;
            if (curX < fadeStartX)
            {
                // Рассчитываем прогресс исчезновения (1 -> 0)
                float range = fadeStartX - destroyX;
                float t = Mathf.Clamp01((curX - destroyX) / range);
                
                // Уменьшаем масштаб и прозрачность
                transform.localScale = _baseScale * t;
                if (_img != null)
                {
                    Color c = _img.color;
                    c.a = t;
                    _img.color = c;
                }
            }
        }

        // Вращение
        _rt.Rotate(0, 0, rotationSpeed * dt);

        // Удаление за границей (v72.0: Используем настраиваемую границу)
        if (_rt.anchoredPosition.x < destroyX)
        {
            Destroy(gameObject);
        }
    }
}

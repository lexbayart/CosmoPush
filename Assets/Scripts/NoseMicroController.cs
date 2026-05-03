
using UnityEngine;
using Unity.Mathematics;

/// <summary>
/// Добавляет эстетическое микродвижение персонажу на основе положения носа игрока.
/// Не меняет дорожку, только создает визуальный отклик.
/// </summary>
public class NoseMicroController : MonoBehaviour
{
    public WebcamPoseDetection tracker; // Работаем через наш трекер
    public float microRange = 0.5f;     // Насколько сильно персонаж "плавает" (±0.5 единицы Unity)
    public float smoothness = 10f;      // Плавность движения
    
    private float _targetOffset;
    private float _currentOffset;
    private Vector3 _baseLocalPosition;

    void Start()
    {
        // v136.16: Глобальная защита от конфликтов. 
        // Если есть PlayerCharacter, он сам рулит микро-движениями.
        if (GetComponent<PlayerCharacter>() != null)
        {
            Debug.Log("[Nose] PlayerCharacter detected. Disabling separate NoseMicroController to avoid Y-fighting.");
            this.enabled = false;
            return;
        }

        _baseLocalPosition = transform.localPosition;
    }

    void Update()
    {
        // v136.7: Авто-подхват синглтона при смене сцены
        if (tracker == null && WebcamPoseDetection.Instance != null)
            tracker = WebcamPoseDetection.Instance;

        if (tracker == null || !tracker.IsTracking)
        {
            _targetOffset = 0;
        }
        else
        {
            // Получаем нормализованный Y носа (0..1)
            // Инвертируем, чтобы движение соответствовало реальности (нос вверх - персонаж вверх)
            float noseY = 1f - tracker.GetNose().y;
            
            // Центрируем значение (-0.5 .. 0.5)
            _targetOffset = (noseY - 0.5f) * microRange;
        }

        // Плавное приближение к цели
        _currentOffset = Mathf.Lerp(_currentOffset, _targetOffset, Time.deltaTime * smoothness);
        
        // Применяем смещение только по Y к базовой позиции
        transform.localPosition = _baseLocalPosition + new Vector3(0, _currentOffset, 0);
    }
}

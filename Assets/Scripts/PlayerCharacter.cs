
using UnityEngine;
using System.Collections;

public class PlayerCharacter : MonoBehaviour
{
    private LaneSystem _lanesystem;
    private PushupVerifier _pushupverifier;
    public LaneSystem laneSystem;
    public PushupVerifier verifier;
    public RhythmManager rhythmManager;

    private RectTransform rectTransform;
    private int currentLaneIndex = 0; 
    private float targetY;
    private float velocityY = 0f;
    private float velocityX = 0f; // Для сглаживания по оси X
    
    [Header("v3.0 PHYSICAL SYNC")]
    public float minSmoothTime = 0.05f; // В движении
    public float maxSmoothTime = 0.40f; // При остановке
    public float velocitySensitivity = 8.0f; // Чувствительность к скорости тела

    [Header("Физика Растяжения")]
    public float stretchIntensity = 0.0003f;
    public float maxStretch = 1.45f;

    [Header("Turbo & Juice")]
    private AstroTurbine _turbine;
    private Vector3 _baseScale = Vector3.one;
    
    // 🪲 BUGFIX #11: Кэш AudioSource вместо GetComponent каждый кадр
    private LevelSpawner _cachedSpawner; // v136.13: Восстановлено
    private AudioSource _cachedAudio;
    private bool _isSubscribed = false; // v136.10: Флаг динамической подписки


    /// <summary>Текущая скорость по Y (для AstroSpriteController и AstroTurbine)</summary>
    public float CurrentVelocityY => velocityY;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        _baseScale = rectTransform.localScale;

        if (verifier != null)
        {
            verifier.OnLaneChanged += OnLaneChanged;
            currentLaneIndex = verifier.CurrentLane;
        }

        if (rhythmManager == null) rhythmManager = GameObject.FindFirstObjectByType<RhythmManager>();
        if (rhythmManager != null)
        {
            rhythmManager.OnBeat += HandleBeat;
            _cachedAudio = rhythmManager.GetComponent<AudioSource>(); // 🪲 BUGFIX #11
        }
        _cachedSpawner = GameObject.FindFirstObjectByType<LevelSpawner>(); // 🪲 BUGFIX #8

        // Добавляем турбину (v1.2)
        _turbine = gameObject.AddComponent<AstroTurbine>();

        // Добавляем контроллер спрайтов (v2.0)
        if (GetComponent<AstroSpriteController>() == null)
            gameObject.AddComponent<AstroSpriteController>();

        // v136.18: ЗАЩИТА ОТ КОНФЛИКТОВ С АНИМАЦИЕЙ
        // Если на объекте есть Animator, он может 'воровать' позицию Y обратно в 135
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.enabled = false;
            Debug.Log("<color=orange>[Player] Animator detected and DISABLED to prevent Y-fighting.</color>");
        }
        
        if (laneSystem != null)
        {
            targetY = laneSystem.GetLaneY(currentLaneIndex);
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            
            // УСТАНОВКА РАЗМЕРА (v2.1): Делаем космонавта заметным
            rectTransform.sizeDelta = new Vector2(180, 180);
            
            rectTransform.anchoredPosition = new Vector2(300f, targetY); 
            laneSystem.HighlightLane(currentLaneIndex);
        }
    }

    void OnDestroy()
    {
        if (rhythmManager != null) rhythmManager.OnBeat -= HandleBeat;
        // 🪲 BUGFIX #10: Отписка от события при уничтожении объекта
        if (verifier != null) verifier.OnLaneChanged -= OnLaneChanged;
    }

    private void HandleBeat(bool isDown)
    {
        StopCoroutine("PulseCoroutine");
        StartCoroutine(PulseCoroutine());
    }

    private float _pulseMultiplier = 1f;

    private IEnumerator PulseCoroutine()
    {
        float duration = 0.1f;
        float elapsed = 0f;
        float targetPulse = 1.15f; 

        while (elapsed < duration)
        {
            _pulseMultiplier = Mathf.Lerp(1f, targetPulse, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < duration)
        {
            _pulseMultiplier = Mathf.Lerp(targetPulse, 1f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _pulseMultiplier = 1f;
    }

    void OnLaneChanged(int newLane)
    {
        currentLaneIndex = newLane;
        if (laneSystem == null || verifier == null) return;
        targetY = laneSystem.GetLaneY(newLane);
        laneSystem.HighlightLane(newLane);
    }

    [Header("v4.0 MICRO-MOVEMENT")]
    public float noseMicroRange = 60f; // Диапазон смещения в пикселях
    private float _noseOffset = 0f;
    private float _noseVelocity = 0f;

    void Update()
    {
        // 0. АВТО-ПОДХВАТ КОМПОНЕНТОВ (v136.10)
        if (laneSystem == null) laneSystem = _lanesystem;
        if (verifier == null) verifier = _pushupverifier;

        if (verifier != null && !_isSubscribed)
        {
            verifier.OnLaneChanged -= OnLaneChanged; // Защита от дублей
            verifier.OnLaneChanged += OnLaneChanged;
            OnLaneChanged(verifier.CurrentLane); // Мгновенная синхронизация
            _isSubscribed = true;
            Debug.Log("<color=cyan>[Player] Dynamic subscription to Verifier SUCCESS!</color>");
        }

        if (laneSystem == null || rectTransform == null || verifier == null) return;
        if (Time.timeScale == 0) return; // ПАУЗА (v116.2)

        // 1. РАСЧЕТ МИКРО-СМЕЩЕНИЯ НОСОМ (v4.0)
        float targetNoseOffset = 0f;
        WebcamPoseDetection activeTracker = (verifier.poseDetection != null) ? verifier.poseDetection : WebcamPoseDetection.Instance;
        
        if (activeTracker != null && activeTracker.IsTracking)
        {
            float rawNoseY = 1f - activeTracker.GetNose().y;
            targetNoseOffset = (rawNoseY - 0.5f) * noseMicroRange;
        }
        _noseOffset = Mathf.SmoothDamp(_noseOffset, targetNoseOffset, ref _noseVelocity, 0.1f);

        float currentSmoothTime = Mathf.Lerp(maxSmoothTime, minSmoothTime, verifier.BodyVelocityY * velocitySensitivity);
        currentSmoothTime = Mathf.Clamp(currentSmoothTime, minSmoothTime, maxSmoothTime);

        // 2. РАСЧЕТ ПОЗИЦИИ X (Boost)
        float targetX = 300f; 
        if (_cachedAudio != null && _cachedAudio.pitch > 1.05f) 
        {
            float boostFactor = Mathf.InverseLerp(1.0f, 1.5f, _cachedAudio.pitch);
            targetX = 300f + (200f * boostFactor);
        }

        // 3. КИНЕМАТОГРАФИЧНЫЙ ФИНАЛ (v136.17): Защита от конфликта целей
        bool isGoalLock = false;
        float lockTargetY = targetY; // По дефолту целимся в текущую дорожку

        if (_cachedSpawner != null && _cachedSpawner.objectsContainer != null)
        {
            foreach (Transform child in _cachedSpawner.objectsContainer)
            {
                MoverItem item = child.GetComponent<MoverItem>();
                if (item != null && item.isGoal)
                {
                    float arrivalTime = item.startTime;
                    float songTime = (rhythmManager != null) ? rhythmManager.SongTime : 0f;
                    float timeRemaining = arrivalTime - songTime;
                    
                    // Блокируем, только если до цели меньше 2 сек (v136.17: используем локальную цель)
                    if (timeRemaining < 2.0f && timeRemaining > -5.0f && arrivalTime > 1.0f)
                    {
                        isGoalLock = true;
                        lockTargetY = item.GetComponent<RectTransform>().anchoredPosition.y; 
                        targetX = 960f; 
                        
                        if (timeRemaining <= 0.05f) 
                        {
                            if (PushupUIHandler.Instance != null && !PushupUIHandler.Instance.IsSuccessPanelActive()) 
                                PushupUIHandler.Instance.ShowSuccessPanel();
                        }
                    }
                    else if (timeRemaining < -5.0f)
                    {
                        Destroy(child.gameObject);
                    }
                    break;
                }
            }
        }

        float newX = Mathf.SmoothDamp(rectTransform.anchoredPosition.x, targetX, ref velocityX, 0.2f); 
        
        // 4. ФИНАЛЬНАЯ ПОЗИЦИЯ Y (Дорожка + Микро-смещение)
        float currentTargetY = isGoalLock ? lockTargetY : (targetY + _noseOffset);
        float newY = Mathf.SmoothDamp(rectTransform.anchoredPosition.y, currentTargetY, ref velocityY, currentSmoothTime);
        
        rectTransform.anchoredPosition = new Vector2(newX, newY);

        // ОБНОВЛЯЕМ ТУРБИНУ (v1.6)
        if(_turbine != null) _turbine.SetVelocity(velocityY);

        // ФИНАЛЬНЫЙ МАСШТАБ (v1.6): Добавлена защита от NaN
        if (float.IsNaN(velocityY)) velocityY = 0f;
        
        float stretch = 1f + (Mathf.Abs(velocityY) * stretchIntensity);
        stretch = Mathf.Clamp(stretch, 1f, maxStretch);
        float squash = 1f - (stretch - 1f) * 0.5f;

        Vector3 finalScale = new Vector3(squash * _pulseMultiplier, stretch * _pulseMultiplier, 1f);
        
        // ГАРАНТИЯ: игнорируем некорректные значения масштаба
        if (!float.IsNaN(finalScale.x) && !float.IsNaN(finalScale.y))
        {
            rectTransform.localScale = finalScale;
        }
    }
}

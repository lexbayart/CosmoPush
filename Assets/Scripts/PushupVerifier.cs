
using UnityEngine;
using UnityEngine.UI;
using System;

public class PushupVerifier : MonoBehaviour
{
    public WebcamPoseDetection poseDetection;
    public RhythmManager rhythmManager;

    [Header("Пороги локтей (градусы)")]
    public float angleUp = 145f;   
    public float angleDown = 110f;  
    
    [Header("Физика Растяжения")]
    public float cooldownTime = 0.5f; 

    // СОБЫТИЯ
    public event Action<string, int> OnHitVisuals;
    public event Action<int> OnPushupCounted;
    public event Action<string> OnCheatStatusChanged;
    public event Action<int> OnLaneChanged; 
    public event Action<int> OnOxygenChanged;
    
    public int PushupCount { get; private set; }
    public int Score { get; private set; }
    public int Oxygen { get; private set; } = 5;
    public int CurrentLane { get; private set; } = 0; 
    
    // ФИЗИЧЕСКАЯ СИНХРОНИЗАЦИЯ (v3.0 PHYSICAL SYNC)
    public float BodyVelocityY { get; private set; } 
    private float _filteredSY = 0.5f;
    private float _prevFilteredSY = 0.5f;
    private float _velocityFilter = 0f;

    enum Phase { Up, Down }
    Phase _phase = Phase.Up;
    float _lastPhaseChangeTime = 0f;
    string _currentCheatReason = ""; 

    void Start() 
    { 
        PushupVerifier.TotalHitsTaken = 0; // Сброс при каждом старте уровня (v122.0)
    }

    public static int TotalHitsTaken = 0; // Счетчик ударов о препятствия (v122.0)

    void Update()
    {
        // 🪲 BUGFIX #16: Авто-подхват глобальной камеры (v136.6)
        if (poseDetection == null && WebcamPoseDetection.Instance != null)
        {
            poseDetection = WebcamPoseDetection.Instance;
        }

        // ЗАЩИТА ФИНАЛЬНЫХ МЕНЮ (v119.1): Не тречим игрока, если игра окончена
        if (PushupUIHandler.Instance != null && 
            (PushupUIHandler.Instance.IsSuccessPanelActive() || 
            (PushupUIHandler.Instance.gameOverPanel != null && PushupUIHandler.Instance.gameOverPanel.activeSelf)))
        {
            if (!string.IsNullOrEmpty(_currentCheatReason) || _outOfFrameTimer > 0)
            {
                OnCheatStatusChanged?.Invoke(""); // Очищаем любые надписи
                _currentCheatReason = "";
                _outOfFrameTimer = 0;
            }
            return; // Полностью глушим логику
        }

        if (poseDetection == null) return;

        HandleVisibilityGuard();
        if (_outOfFrameTimer > 1.5f) return; // Замораживаем физику отжиманий, если пауза

        if (!poseDetection.IsTracking) return;

        // 1. ПОЛУЧЕНИЕ СЫРЫХ ДАННЫХ
        float leftY = poseDetection.GetKeypointY(11);
        float rightY = poseDetection.GetKeypointY(12);
        float rawSY = (leftY + rightY) / 2f;

        // 2. ФИЛЬТРАЦИЯ ВТОРОГО ПОРЯДКА (v3.0)
        _filteredSY = Mathf.Lerp(_filteredSY, rawSY, Time.deltaTime * 15f); // Первый порядок (позиция)
        float instantVelocity = Mathf.Abs(_filteredSY - _prevFilteredSY) / Time.deltaTime;
        BodyVelocityY = Mathf.SmoothDamp(BodyVelocityY, instantVelocity, ref _velocityFilter, 0.1f); // Второй порядок (скорость)
        _prevFilteredSY = _filteredSY;

        // 3. ЛОГИКА ДВУХ ЛИНИЙ (Строгая привязка)
        int detectedLane = (_filteredSY > 0.5f) ? 0 : 1; 

        if (detectedLane != CurrentLane)
        {
            CurrentLane = detectedLane;
            OnLaneChanged?.Invoke(CurrentLane);
        }

        // 4. ЛОГИКА ЧИТЕРСТВА (Движение не блокируется)
        string cheatReason = CheckIsCheating();
        if (cheatReason != _currentCheatReason)
        {
            _currentCheatReason = cheatReason;
            OnCheatStatusChanged?.Invoke(_currentCheatReason); 
        }
        
        if (!string.IsNullOrEmpty(_currentCheatReason)) return; 

        // 5. ПОДСЧЕТ ОТЖИМАНИЙ
        if (Time.time - _lastPhaseChangeTime > cooldownTime) 
        {
            if (_phase == Phase.Up && CurrentLane == 1) 
            {
                _phase = Phase.Down;
                _lastPhaseChangeTime = Time.time;
                RateRhythmHit(true); 
            }
            else if (_phase == Phase.Down && CurrentLane == 0) 
            {
                _phase = Phase.Up;
                _lastPhaseChangeTime = Time.time;
                PushupCount++;
                OnPushupCounted?.Invoke(PushupCount);
                RateRhythmHit(false); 
            }
        }
    }

    string CheckIsCheating()
    {
        if (poseDetection.GetKeypointVisibility(11) < 0.2f || poseDetection.GetKeypointVisibility(23) < 0.2f) return "";
        float lS_z = poseDetection.GetKeypointZ(11);
        float rS_z = poseDetection.GetKeypointZ(12);
        float lH_z = poseDetection.GetKeypointZ(23); 
        float rH_z = poseDetection.GetKeypointZ(24);
        float zDelta = ((lH_z + rH_z) / 2f) - ((lS_z + rS_z) / 2f); 

        if (zDelta < 80f) return "Вы стоите\nПРИМИТЕ УПОР ЛЁЖА!";
        if (zDelta > 260f) return "СЛИШКОМ ВЫСОКО!\nОТЖИМАЙТЕСЬ ОТ ПОЛА!";
        return ""; 
    }

    // 🪲🪲 BUGFIX #12: Переписано для синхронизации с beatMap (совместимость с BUGFIX #4)
    void RateRhythmHit(bool isGoingDown)
    {
        if (rhythmManager == null || rhythmManager.beatMap == null || rhythmManager.beatMap.Length == 0) return;
        
        float songTime = rhythmManager.SongTime;
        float[] beats = rhythmManager.beatMap;
        
        // Находим ближайший бит (предыдущий и следующий)
        float dtToLast = float.MaxValue;
        float dtToNext = float.MaxValue;
        
        for (int i = 0; i < beats.Length; i++)
        {
            float diff = songTime - beats[i];
            if (diff >= 0 && diff < dtToLast) dtToLast = diff;
            if (diff < 0 && Mathf.Abs(diff) < dtToNext) dtToNext = Mathf.Abs(diff);
        }
        
        float minDiff = Mathf.Min(dtToLast, dtToNext);
        bool isCloseToNext = dtToNext < dtToLast;
        bool expectedState = isCloseToNext ? !rhythmManager.CurrentExpectedStateDown : rhythmManager.CurrentExpectedStateDown;

        if (expectedState == isGoingDown)
        {
            if (minDiff < 0.2f) { OnHitVisuals?.Invoke("PERFECT!", 0); }
            else if (minDiff < 0.5f) { OnHitVisuals?.Invoke("GOOD", 0); }
            else { OnHitVisuals?.Invoke("OK!", 0); } 
        }
        else { OnHitVisuals?.Invoke("MISS!", 0); }
    }

    public void TakeDamage()
    {
        if (Oxygen <= 0) return;
        Oxygen--;
        TotalHitsTaken++; // Отслеживаем для рейтинга (v122.0)
        OnOxygenChanged?.Invoke(Oxygen);
    }

    public void AddExternalScore(int points)
    {
        Score += points;
        OnHitVisuals?.Invoke("+ " + points + "!", points); // Синхронизируем UI с новыми очками
    }

    [Header("Visibility Guard (v118.1)")]
    private float _outOfFrameTimer = 0f;
    private float _resumeTimer = 0f;
    private bool _isPausedByVisibility = false;
    private bool _hasTrackedOnce = false;

    void HandleVisibilityGuard()
    {
        bool visible = poseDetection.IsTracking && poseDetection.GetKeypointVisibility(0) > 0.5f;
        
        if (!visible) 
        {
            _resumeTimer = 0f; // Если снова пропал из кадра — сбрасываем таймер возврата
            _outOfFrameTimer += Time.unscaledDeltaTime;
            
            // На старте игры даем больше времени (3 секунды) на загрузку нейросети, чтобы избежать ложных пауз
            float dropThreshold = _hasTrackedOnce ? 1.5f : 3.0f;
            
            if (_outOfFrameTimer > dropThreshold && !_isPausedByVisibility) 
            { 
                _isPausedByVisibility = true;
                PauseGameObjects(); 
                PauseMusic(); 
                OnCheatStatusChanged?.Invoke("ВЫ ВНЕ КАДРА!\nИГРА НА ПАУЗЕ");
            }
        } 
        else 
        {
            _outOfFrameTimer = 0f;

            if (_isPausedByVisibility)
            {
                // Если игрока не было на старте, мы стартуем игру сразу без отсчета
                if (!_hasTrackedOnce)
                {
                    _resumeTimer = 3.1f; 
                }
                else
                {
                    _resumeTimer += Time.unscaledDeltaTime;
                    if (Input.GetKeyDown(KeyCode.Space)) _resumeTimer = 3.1f; 
                }

                if (_resumeTimer > 3f)
                {
                    _isPausedByVisibility = false;
                    _resumeTimer = 0f;
                    _hasTrackedOnce = true;
                    HideWarning(); 
                    ResumeGameObjects(); 
                    ResumeMusic(); 
                }
                else
                {
                    int secondsLeft = Mathf.CeilToInt(3f - _resumeTimer);
                    OnCheatStatusChanged?.Invoke($"ПРОДОЛЖАЕМ ЧЕРЕЗ {secondsLeft}...\n[ПРОБЕЛ ДЛЯ СТАРТА]");
                }
            }
            else
            {
                _hasTrackedOnce = true; // Мы просто стоим в кадре
            }
        }
    }

    void HideWarning()
    {
        if (_currentCheatReason == "") {
            OnCheatStatusChanged?.Invoke(""); 
        } else {
            OnCheatStatusChanged?.Invoke(_currentCheatReason); 
        }
    }

    void PauseGameObjects() { Time.timeScale = 0f; }
    void ResumeGameObjects() { Time.timeScale = 1f; }
    void PauseMusic() { AudioListener.pause = true; }
    void ResumeMusic() { AudioListener.pause = false; }
}


using UnityEngine;
using UnityEngine.UI;

public class MoverItem : MonoBehaviour
{
    public float startTime;
    public float spawnX;
    public float speed;
    public bool isObstacle;
    public bool isSpeedup;
    public bool isAsteroid;
    public bool isGoal;
    public int laneIndex;
    public float rotationSpeed = 0f;
    public bool asteroidTriggered = false; // v77.0: Чтобы не спамить проверками

    private RhythmManager _rm;
    private RectTransform _rt;
    private float _targetY;
    private PlayerCharacter _player; 
    private LevelSpawner _spawner;
    // 🪲 BUGFIX #9: Кэш Visual трансформа и Image (вместо Find каждый кадр)
    private Transform _visualTransform;
    private Image _visualImage;
    // 🪲 BUGFIX #11: Кэш AudioSource (вместо GetComponent каждый кадр)
    private AudioSource _rmAudio;

    // --- СИСТЕМА ОКАНТОВОК (НАСТОЯЩИЙ шейдер, v134.0) ---
    // 🪲 BUGFIX #1: Используем статический пул материалов для батчинга и избежания утечек
    private static Material _matNone, _matRed, _matGreen, _matWhite;
    
    // 🪲 BUGFIX #2: Статический реестр активных объектов для PlayerCollision
    public static System.Collections.Generic.List<MoverItem> ActiveItems = new System.Collections.Generic.List<MoverItem>();
    private static MoverItem _cookieCandidate = null;
    private static float _cookieCandidateDist = float.MaxValue;
    private static int _cookieCompeteFrame = -1;

    // --- ДАННЫЕ ОТЛАДКИ (v26.0) ---
    public static bool ShowDebug = true; // Глобальный переключатель на клавишу H
    private Image _debugImg; 
    
    void OnEnable() { ActiveItems.Add(this); }
    void OnDisable() { ActiveItems.Remove(this); }

    void Start()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        _rm = GameObject.FindFirstObjectByType<RhythmManager>();
        _spawner = GameObject.FindFirstObjectByType<LevelSpawner>();
        _player = GameObject.FindFirstObjectByType<PlayerCharacter>();
        if (_rm != null) _rmAudio = _rm.GetComponent<AudioSource>(); // 🪲 BUGFIX #11
        
        if (_targetY == 0f) _targetY = _rt.anchoredPosition.y; 
        
        SyncVisuals(); // Инициализация визуала (v111.4)

        // ИНИЦИАЛИЗАЦИЯ ШЕЙДЕРНОГО МАТЕРИАЛА КОНТУРА (v133.0)
        // 🪲 BUGFIX #9: Кэшируем Visual один раз
        _visualTransform = transform.Find("Visual");
        if (_visualTransform != null)
        {
            _visualImage = _visualTransform.GetComponent<Image>();
            Image visImg = _visualImage;
            if (visImg != null)
            {
                if (_matNone == null)
                {
                    Shader outlineShader = Shader.Find("UI/OutlineEffect");
                    if (outlineShader != null)
                    {
                        _matNone = new Material(outlineShader); _matNone.SetColor("_OutlineColor", Color.clear); _matNone.SetFloat("_OutlineWidth", 3f);
                        _matRed = new Material(outlineShader); _matRed.SetColor("_OutlineColor", Color.red); _matRed.SetFloat("_OutlineWidth", 3f);
                        _matGreen = new Material(outlineShader); _matGreen.SetColor("_OutlineColor", Color.green); _matGreen.SetFloat("_OutlineWidth", 3f);
                        _matWhite = new Material(outlineShader); _matWhite.SetColor("_OutlineColor", Color.white); _matWhite.SetFloat("_OutlineWidth", 3f);
                    }
                }
                if (_matNone != null) visImg.material = _matNone;
            }
        }
    }

    // МЕТОД ПРИНУДИТЕЛЬНОГО ПЕРЕНОСА (v38.0)
    public void ChangeLaneAndY(int newLane, float newY)
    {
        laneIndex = newLane;
        _targetY = newY;
        if (_rt == null) _rt = GetComponent<RectTransform>();
        _rt.anchoredPosition = new Vector2(_rt.anchoredPosition.x, newY);
    }

    // МЕТОД СИНХРОНИЗАЦИИ ВИЗУАЛА (v111.4)
    // Гарантирует правильный цвет хитбокса и эффекты после настройки флагов
    public void SyncVisuals()
    {
        // Очистка старых DEBUG-отрисовок
        Transform oldDebug = transform.Find("DebugHitbox");
        if (oldDebug != null) Destroy(oldDebug.gameObject);

        GameObject frame = new GameObject("DebugHitbox", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(transform, false);
        
        RectTransform frameRt = frame.GetComponent<RectTransform>();
        frameRt.anchorMin = Vector2.zero; frameRt.anchorMax = Vector2.one;
        frameRt.offsetMin = frameRt.offsetMax = Vector2.zero;

        Image img = frame.GetComponent<Image>();
        img.raycastTarget = false; 

        // Цвет и размер
        float hitFactor = 1.0f;
        Color debugColor = new Color(1f, 1f, 1f, 0.4f);

        if (isGoal)
        {
            debugColor = new Color(0.5f, 0.5f, 0.5f, 0.2f); // Едва заметный хитбокс для цели
            hitFactor = 1.0f;
        }
        else if (isObstacle || isAsteroid) 
        {
            debugColor = new Color(1f, 0f, 0f, 0.4f); // КРАСНЫЙ (Опасность)
            hitFactor = 0.8f; 
        }
        else if (isSpeedup)
        {
            debugColor = new Color(0f, 1f, 1f, 0.4f); // ГОЛУБОЙ (Звезда) - v133.0
            hitFactor = 1.0f;
        }
        else // Cookie
        {
            debugColor = new Color(1f, 0.9f, 0f, 0.4f); // ЖЕЛТЫЙ (Сбор)
            hitFactor = 1.0f;
        }

        img.color = debugColor;
        frameRt.localScale = new Vector3(hitFactor, hitFactor, 1f);
        _debugImg = img;
    }

    void Update()
    {
        if (Time.timeScale == 0) return; // ПАУЗА (v116.4): Полная заморозка объекта
        
        // Переключение видимости хитбоксов (v26.0)
        if (_debugImg != null) _debugImg.enabled = ShowDebug;
        
        if (_rm == null) return;
        
        // 🪲 BUGFIX #9: Используем кэшированный Visual
        if (_visualTransform != null)
        {
            if (isSpeedup) _visualTransform.Rotate(0, 0, 360f * Time.deltaTime);
            else if (rotationSpeed != 0) _visualTransform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
        
        float playerX = (_spawner != null) ? _spawner.playerXPosition : 300f;

        // --- ЛОГИКА АСТЕРОИДА (v16.1: БЕЗ ЗАМЕДЛЕНИЯ) ---
        if (isAsteroid)
        {
            float pitch = (_rmAudio != null) ? _rmAudio.pitch : 1f;
            float actualSpeed = speed * pitch;
            
            _rt.anchoredPosition += new Vector2(-actualSpeed, -actualSpeed) * Time.deltaTime;
            
            if (Random.value < 0.2f && JuiceManager.Instance != null)
                JuiceManager.Instance.SpawnAsteroidTrail(transform.position);

            if (_rt.anchoredPosition.x < -800f || _rt.anchoredPosition.y < -800f) 
                Destroy(gameObject);
                
            return; 
        }
        
        // --- ПРЕЦИЗИОННАЯ ФИЗИКА НОТ (v120.0) ---
        float timeToHit = startTime - _rm.SongTime;
        
        float targetHitX = isGoal ? 960f : playerX; // Спутник летит в центр (960f), а не к левому краю!
        if (isGoal) timeToHit = Mathf.Max(0f, timeToHit); // Спутник останавливается в центре и ждёт!

        float currentX = targetHitX + (speed * timeToHit);
        
        _rt.anchoredPosition = new Vector2(currentX, _targetY);

        // Уничтожаем, если улетел далеко за игрока (но НЕ спутник!)
        if (currentX < -500f && !isGoal) 
        {
            Destroy(gameObject);
        }

        // КОНКУРЕНЦИЯ ПЕЧЕНЕК (v123.0): Только ближайшая к игроку подсвечивается зелёным
        if (!isObstacle && !isSpeedup && !isAsteroid && !isGoal)
        {
            float pX = (_spawner != null) ? _spawner.playerXPosition : 300f;
            float cookieDist = _rt.anchoredPosition.x - pX;
            bool cookieSameLane = (_player != null) && (laneIndex == _player.verifier.CurrentLane);

            if (Time.frameCount != _cookieCompeteFrame)
            {
                _cookieCompeteFrame = Time.frameCount;
                _cookieCandidate = null;
                _cookieCandidateDist = float.MaxValue;
            }

            if (cookieSameLane && cookieDist > 0 && cookieDist < 600f && cookieDist < _cookieCandidateDist)
            {
                _cookieCandidate = this;
                _cookieCandidateDist = cookieDist;
            }
        }
    }

    void LateUpdate()
    {
        if (_matNone == null || _rm == null || Time.timeScale == 0) return;

        float playerX = (_spawner != null) ? _spawner.playerXPosition : 300f;
        float dist = _rt.anchoredPosition.x - playerX;
        bool inFront = dist > 0;
        bool sameLane = (_player != null) && (laneIndex == _player.verifier.CurrentLane);

        Material targetMat = _matNone;

        if (isAsteroid)
        {
            if (sameLane && inFront && dist < 600f) targetMat = _matRed;
        }
        else if (isObstacle)
        {
            if (sameLane && inFront && dist < 600f) targetMat = _matRed;
        }
        else if (isSpeedup)
        {
            if (sameLane && inFront && dist < 600f) targetMat = _matWhite;
        }
        else if (!isGoal) // Cookie
        {
            if (this == _cookieCandidate && dist < 600f) targetMat = _matGreen;
        }

        // 🪲 BUGFIX #9: Используем кэшированный _visualImage
        if (_visualImage != null && _visualImage.material != targetMat)
        {
            _visualImage.material = targetMat;
        }
    }
}

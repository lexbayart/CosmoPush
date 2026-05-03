
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LevelSpawner : MonoBehaviour
{
    public LaneSystem laneSystem;
    public RhythmManager rhythmManager;
    public Transform objectsContainer; 
    
    [Header("Global Layout")]
    public float scrollSpeed = 450f; 
    public float spawnXPosition = 2000f; 
    public float playerXPosition = 300f;  
    
    public static int TotalCookiesSpawned = 0; 
    public static int TotalStarsSpawned = 0;   
    
    private PlayerCharacter _playerScript; 
    private RectTransform _playerRect;
    private int _nextBeatIndex = 0;
    private int _lastCookieLane = 0;
    private int _cookieGroupCounter = 0; 
    private int _currentCookieGroupSize = 0;

    private float _rockTimer = 0f;
    private float _starTimer = 1.5f; 
    private float _nextRockInterval = 2.5f;
    private float _nextStarInterval = 2.5f;
    
    private int _lastRockLane = 0;
    private int _lastRockLaneL2 = 0; 
    private int _consecutiveRocksInLane = 0; 
    
    private float[] _lastObjectArrival = new float[2] { -10f, -10f }; 
    private float _asteroidTimer = 5f; 
    private float _asteroidImpactTime = -10f; 
    private bool _goalSpawned = false; 

    private const float DODGE_WINDOW = 1.1f;

    [Header("Level 2 Specialized Logic")]
    public bool rocksOnBeatMode = true; 
    private float _lastGroupSpawnTime = 0f;

    private Sprite[] _obstacleSprites;
    private Sprite[] _cookieSprites;
    private Sprite _asteroidSprite;
    private Sprite _starSprite;
    private Sprite _goalSprite;

    private float _currentLeadTime;

    void Awake() { TotalCookiesSpawned = 0; TotalStarsSpawned = 0; }

    void Start()
    {
        _playerScript = GameObject.FindFirstObjectByType<PlayerCharacter>();
        if (_playerScript != null)
            _playerRect = _playerScript.GetComponent<RectTransform>();
        if (rhythmManager == null) rhythmManager = GameObject.FindFirstObjectByType<RhythmManager>();
        _obstacleSprites = LoadSprites("Emojis/Obstacles");
        _cookieSprites = LoadSprites("Emojis/Cookies");
        _asteroidSprite = SafeLoadSprite("Emojis/Asteroid/emoji_u2604");
        _starSprite = SafeLoadSprite("Emojis/Speedup/emoji_u2b50");
        _goalSprite = SafeLoadSprite("Satellite");
        if (GameObject.FindFirstObjectByType<BackgroundStoneManager>() == null)
            new GameObject("BackgroundStoneManager", typeof(BackgroundStoneManager)).transform.SetParent(transform.parent);
    }

    private Sprite SafeLoadSprite(string path)
    {
        Sprite s = Resources.Load<Sprite>(path);
        if (s != null) return s;
        Texture2D t = Resources.Load<Texture2D>(path);
        if (t != null) return Sprite.Create(t, new Rect(0,0,t.width, t.height), new Vector2(0.5f, 0.5f));
        return null;
    }

    private Sprite[] LoadSprites(string folder)
    {
        Texture2D[] texs = Resources.LoadAll<Texture2D>(folder);
        if (texs == null || texs.Length == 0) return new Sprite[0];
        Sprite[] sprs = new Sprite[texs.Length];
        for(int i = 0; i < texs.Length; i++)
            sprs[i] = Sprite.Create(texs[i], new Rect(0,0,texs[i].width, texs[i].height), new Vector2(0.5f, 0.5f));
        return sprs;
    }

    void Update()
    {
        if (rhythmManager == null || Time.timeScale == 0) return;
        float songTime = rhythmManager.SongTime;
        if (PushupUIHandler.Instance != null && PushupUIHandler.Instance.IsSuccessPanelActive()) return;
        if (Input.GetKeyDown(KeyCode.H)) MoverItem.ShowDebug = !MoverItem.ShowDebug;
        if (!rhythmManager.IsPlaying) return;
        
        if (Input.GetKeyDown(KeyCode.RightArrow) && rhythmManager.SongDuration > 20f)
        {
            float skipTo = rhythmManager.SongDuration - 10f; 
            rhythmManager.SkipToTime(skipTo);
            foreach (Transform child in objectsContainer) if (child.GetComponent<MoverItem>() != null) Destroy(child.gameObject);
            while (_nextBeatIndex < rhythmManager.beatMap.Length && rhythmManager.beatMap[_nextBeatIndex] < skipTo) _nextBeatIndex++;
            _goalSpawned = false; 
        }

        float currentPX = (_playerRect != null) ? _playerRect.anchoredPosition.x : playerXPosition;
        _currentLeadTime = (spawnXPosition - currentPX) / scrollSpeed;

        HandleAsteroids(songTime);
        HandleRhythmSpawning(songTime);
        HandleAmbientSpawning(songTime);
        HandleGoalSpawning(songTime);
    }

    void HandleGoalSpawning(float songTime)
    {
        if (_goalSpawned) return;
        float arrivalTime = rhythmManager.SongDuration - 0.2f; 
        if (songTime >= (arrivalTime - _currentLeadTime) && rhythmManager.SongDuration > 5f)
        {
            _goalSpawned = true;
            SpawnObject(0, false, false, arrivalTime, true); 
        }
    }

    void HandleRhythmSpawning(float songTime)
    {
        if (_goalSpawned || _nextBeatIndex >= rhythmManager.beatMap.Length) return;
        if (songTime > rhythmManager.SongDuration - _currentLeadTime) return;

        bool isLevel2 = rhythmManager.IsLevel2;
        float nextBeatThreshold = (float)rhythmManager.beatMap[_nextBeatIndex];

        if (isLevel2 && rocksOnBeatMode)
        {
            if (songTime >= nextBeatThreshold - _currentLeadTime)
            {
                bool isFarFromGoal = (rhythmManager.SongDuration - songTime) > 5.0f;
                // Спавним строго на каждый 4-й бит, без искусственных задержек
                if (_nextBeatIndex % 4 == 0 && isFarFromGoal)
                {
                    int lane = _lastRockLaneL2;
                    if (_consecutiveRocksInLane >= 2) { lane = 1 - lane; _consecutiveRocksInLane = 1; }
                    else { if (Random.value > 0.5f) { lane = 1 - lane; _consecutiveRocksInLane = 1; } else { _consecutiveRocksInLane++; } }
                    _lastRockLaneL2 = lane;

                    float targetArrival = nextBeatThreshold + (100f / scrollSpeed);
                    PushCookiesAtSpawn(lane, targetArrival, 300f);
                    SpawnObject(lane, true, false, targetArrival, false);
                }
                _nextBeatIndex++;
                return;
            }

            if (songTime - _lastGroupSpawnTime > 1.2f) 
            {
                int lane = 1 - _lastCookieLane;
                float baseStart = songTime + _currentLeadTime + 0.2f;
                int spawnedCount = 0;
                for (int i = 0; i < 5; i++) 
                {
                    float arrival = baseStart + (i * 0.4f);
                    if (!IsAmbientOverlapping(lane, arrival, 220f)) { SpawnObject(lane, false, false, arrival, false); spawnedCount++; }
                }
                if (spawnedCount > 0) { _lastCookieLane = lane; _lastGroupSpawnTime = songTime; }
            }
        }
        else
        {
            if (songTime >= nextBeatThreshold - _currentLeadTime)
            {
                if (_cookieGroupCounter >= _currentCookieGroupSize || _currentCookieGroupSize == 0)
                {
                    _currentCookieGroupSize = Random.Range(3, 6); _cookieGroupCounter = 0;
                    _lastCookieLane = 1 - _lastCookieLane; 
                }
                int lane = _lastCookieLane;
                if (IsAmbientOverlapping(lane, nextBeatThreshold, 220f)) 
                {
                    lane = 1 - lane;
                    if (IsAmbientOverlapping(lane, nextBeatThreshold, 220f)) { _nextBeatIndex++; return; }
                    _lastCookieLane = lane;
                }
                _cookieGroupCounter++;
                SpawnObject(lane, false, false, nextBeatThreshold, false); 
                _nextBeatIndex++;
            }
        }
    }

    void HandleAmbientSpawning(float songTime)
    {
        if (_goalSpawned) return;
        bool isGoalNear = (rhythmManager.SongDuration - songTime) < 5.0f;
        if (isGoalNear) return;

        bool isL2 = (rhythmManager.IsLevel2 && rocksOnBeatMode);
        if (!isL2) _rockTimer += Time.deltaTime;
        _starTimer += Time.deltaTime;

        if (_rockTimer >= _nextRockInterval)
        {
            _rockTimer = 0f; _nextRockInterval = Random.Range(2.0f, 3.0f);
            float arrival = songTime + ((spawnXPosition - playerXPosition) / scrollSpeed);
            int lane = 1 - _lastRockLane;
            if (!IsAmbientOverlapping(lane, arrival, 400f)) {
                PushCookiesAtSpawn(lane, arrival, 300f);
                _lastRockLane = lane; SpawnObject(lane, true, false, arrival, false);
            }
        }

        if (_starTimer >= _nextStarInterval)
        {
            _starTimer = 0f; _nextStarInterval = Random.Range(4.0f, 6.0f);
            float arrival = songTime + ((spawnXPosition - playerXPosition) / scrollSpeed);
            int lane = Random.Range(0, 2);
            if (!IsAmbientOverlapping(lane, arrival, 400f)) {
                PushCookiesAtSpawn(lane, arrival, 150f);
                SpawnObject(lane, false, true, arrival, false);
            }
        }
    }

    private bool IsAmbientOverlapping(int lane, float targetStartTime, float width)
    {
        foreach (Transform child in objectsContainer)
        {
            MoverItem item = child.GetComponent<MoverItem>();
            if (item != null && item.laneIndex == lane && (item.isObstacle || item.isSpeedup || item.isAsteroid))
            {
                float otherWidth = item.isObstacle ? 300f : (item.isSpeedup ? 150f : 300f);
                float dist = Mathf.Abs(targetStartTime - item.startTime) * scrollSpeed;
                if (dist < (width + otherWidth) / 2f + 50f) return true; 
            }
        }
        return false;
    }

    private void PushCookiesAtSpawn(int fromLane, float targetStartTime, float newObjWidth)
    {
        foreach (Transform child in objectsContainer)
        {
            MoverItem item = child.GetComponent<MoverItem>();
            if (item != null && item.laneIndex == fromLane && !item.isObstacle && !item.isSpeedup && !item.isAsteroid)
            {
                float dist = Mathf.Abs(targetStartTime - item.startTime) * scrollSpeed;
                if (dist < (newObjWidth + 120f) / 2f + 50f) Destroy(child.gameObject);
            }
        }
    }

    void HandleAsteroids(float songTime)
    {
        if (_goalSpawned || _asteroidTimer > 0) { _asteroidTimer -= Time.deltaTime; return; }
        bool isGoalNear = (rhythmManager.SongDuration - songTime) < 5.0f;
        if (isGoalNear) return;

        float currentPX = (_playerRect != null) ? _playerRect.anchoredPosition.x : playerXPosition;
        foreach (Transform child in objectsContainer)
        {
            MoverItem item = child.GetComponent<MoverItem>();
            if (item != null && item.isObstacle && !item.asteroidTriggered && child.GetComponent<RectTransform>().anchoredPosition.x <= currentPX + 400f)
            {
                item.asteroidTriggered = true; SpawnAsteroid();
                _asteroidTimer = Random.Range(10f, 18f); break;
            }
        }
    }

    void SpawnObject(int laneIndex, bool isObstacle, bool isSpeedup, float targetArrivalSongTime, bool isGoal)
    {
        GameObject obj = new GameObject(isGoal ? "Goal" : (isObstacle ? "Obstacle" : (isSpeedup ? "Speedup" : "Cookie")), typeof(RectTransform), typeof(MoverItem));
        obj.transform.SetParent(objectsContainer, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(spawnXPosition, laneSystem.GetLaneY(laneIndex)); 
        GameObject visualObj = new GameObject("Visual", typeof(RectTransform), typeof(Image));
        visualObj.transform.SetParent(obj.transform, false);
        RectTransform vRt = visualObj.GetComponent<RectTransform>();
        vRt.anchorMin = Vector2.zero; vRt.anchorMax = Vector2.one; vRt.offsetMin = vRt.offsetMax = Vector2.zero;
        MoverItem mover = obj.GetComponent<MoverItem>();
        mover.startTime = targetArrivalSongTime; mover.spawnX = spawnXPosition; mover.speed = scrollSpeed;
        mover.isObstacle = isObstacle; mover.isSpeedup = isSpeedup; mover.isGoal = isGoal; mover.laneIndex = laneIndex;
        mover.SyncVisuals();
        Image img = visualObj.GetComponent<Image>();
        if (isSpeedup) { img.sprite = _starSprite; rt.sizeDelta = new Vector2(150, 150); TotalStarsSpawned++; mover.rotationSpeed = 180f; }
        else if (isObstacle) { img.sprite = _obstacleSprites[Random.Range(0, _obstacleSprites.Length)]; rt.sizeDelta = new Vector2(300, 300); mover.rotationSpeed = Random.Range(15f, 30f) * (Random.value > 0.5f ? 1 : -1); }
        else if (isGoal) { img.sprite = _goalSprite; rt.sizeDelta = new Vector2(800, 800); mover.rotationSpeed = 0; }
        else { img.sprite = _cookieSprites[Random.Range(0, _cookieSprites.Length)]; rt.sizeDelta = new Vector2(120, 120); TotalCookiesSpawned++; }
    }

    void SpawnAsteroid()
    {
        GameObject obj = new GameObject("Asteroid", typeof(RectTransform), typeof(MoverItem));
        obj.transform.SetParent(objectsContainer, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        bool deco = Random.value < 0.6f;
        int lane = deco ? -1 : Random.Range(0, 2);
        float destY = (lane == -1) ? Random.Range(-500f, 500f) : laneSystem.GetLaneY(lane);
        float startX = spawnXPosition + Random.Range(400f, 1200f);
        float dist = startX - ((_playerScript != null) ? _playerScript.GetComponent<RectTransform>().anchoredPosition.x : 300f);
        rt.anchoredPosition = new Vector2(startX, destY + dist);
        GameObject vis = new GameObject("Visual", typeof(RectTransform), typeof(Image));
        vis.transform.SetParent(obj.transform, false);
        RectTransform vRt = vis.GetComponent<RectTransform>();
        vRt.anchorMin = Vector2.zero; vRt.anchorMax = Vector2.one; vRt.offsetMin = vRt.offsetMax = Vector2.zero;
        Image img = vis.GetComponent<Image>(); 
        MoverItem mover = obj.GetComponent<MoverItem>();
        mover.isAsteroid = !deco; mover.laneIndex = lane; mover.speed = scrollSpeed * (deco ? 0.4f : 1.3f);
        if (deco) { img.sprite = _obstacleSprites[Random.Range(0, _obstacleSprites.Length)]; img.color = new Color(1,1,1,0.5f); rt.sizeDelta = new Vector2(Random.Range(400,700), Random.Range(400,700)); obj.transform.SetAsFirstSibling(); }
        else { img.sprite = _asteroidSprite; _asteroidImpactTime = rhythmManager.SongTime + (dist / mover.speed); rt.sizeDelta = new Vector2(300, 300); }
    }
}

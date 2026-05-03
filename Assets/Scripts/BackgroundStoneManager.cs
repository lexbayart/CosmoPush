
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// BackgroundStoneManager v47.0 - Режиссер фонового декора.🦾🚀🪐🌑
/// Спавнит гигантские камни за пределами геймплея.
/// </summary>
public class BackgroundStoneManager : MonoBehaviour
{
    private LevelSpawner _spawner;
    private Transform _bgContainer;
    private Sprite[] _stoneSprites;
    private Sprite[] _asteroidSprites; // v70.0: Настоящие астероиды для падения ☄️
    
    private float _spawnTimer = 5f;
    private float _nextSpawnInterval = 6f;
    private float _fallingTimer = 0f; // v69.0: Таймер для падающих звезд

    void Start()
    {
        _spawner = GameObject.FindFirstObjectByType<LevelSpawner>();
        if (_spawner == null) return;

        // ИДЕАЛЬНЫЙ СЭНДВИЧ СЛОЕВ (v56.0)
        GameObject spaceBg = GameObject.Find("SpaceBackground");
        
        // Надежный поиск видеотрансляции через компонент
        CameraPreviewHandler camHandler = GameObject.FindFirstObjectByType<CameraPreviewHandler>();
        Transform videoTransform = (camHandler != null && camHandler.rawImage != null) ? camHandler.rawImage.transform : null;
        
        Transform gameContainer = _spawner.objectsContainer;
        
        // Пытаемся найти общий дом
        GameObject hudCanvasObj = GameObject.Find("Oxygen_HUD_Canvas"); 
        Transform parent = (gameContainer != null) ? gameContainer.parent : (hudCanvasObj != null ? hudCanvasObj.transform : null);

        if (parent != null)
        {
            GameObject containerObj = new GameObject("Background_Scenery_Container", typeof(RectTransform));
            containerObj.transform.SetParent(parent, false);
            
            // ЛОГИКА v56.0: ВСТАТЬ ПОВЕРХ ВСЕГО ФОНА (ЗВЕЗД И ВИДЕО)
            int targetIndex = 0;
            if (spaceBg != null && spaceBg.transform.parent == parent) 
                targetIndex = Mathf.Max(targetIndex, spaceBg.transform.GetSiblingIndex() + 1);
            
            if (videoTransform != null && videoTransform.parent == parent)
                targetIndex = Mathf.Max(targetIndex, videoTransform.GetSiblingIndex() + 1);
            
            // Но при этом остаться под игрой (Cookies/Enemies)
            if (gameContainer != null && gameContainer.transform.parent == parent)
            {
                int gameIdx = gameContainer.GetSiblingIndex();
                // Ставим прямо перед игровыми объектами, чтобы быть НАД видео, но ПОД игрой
                targetIndex = gameIdx;
            }

            containerObj.transform.SetSiblingIndex(targetIndex);
            _bgContainer = containerObj.transform;
        }

        // КРИТИЧЕСКАЯ ИСПРАВЛЕННАЯ ЛОГИКА (v65.0): Сначала загружаем, потом спавним!
        _stoneSprites = LoadSprites("Emojis/Obstacles");
        _asteroidSprites = LoadSprites("Emojis/Asteroid"); // v70.0
        
        // МГНОВЕННОЕ ЗАПОЛНЕНИЕ (v67.0)
        // Возвращаем как было, раз это устраивает.
        SpawnHugeStone(0f);
    }

    private Sprite[] LoadSprites(string folder)
    {
        Texture2D[] texs = Resources.LoadAll<Texture2D>(folder);
        if (texs == null || texs.Length == 0) return new Sprite[0];
        Sprite[] sprs = new Sprite[texs.Length];
        for(int i = 0; i < texs.Length; i++)
        {
            sprs[i] = Sprite.Create(texs[i], new Rect(0,0,texs[i].width, texs[i].height), new Vector2(0.5f, 0.5f));
        }
        return sprs;
    }

    private int _patternStep = 0;

    void Update()
    {
        if (_spawner == null || _bgContainer == null) return;

        // 1. ОСНОВНОЙ СПАВН (Горизонтальный)
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _nextSpawnInterval)
        {
            _spawnTimer = 0f;
            _nextSpawnInterval = Random.Range(3.5f, 7f);
            SpawnHugeStone(_spawner.spawnXPosition + 1500f);
        }

        // 2. ПАДАЮЩИЕ АСТЕРОИДЫ (v74.0 - Серийный спавн при открытом небе)
        _fallingTimer += Time.deltaTime;
        if (_fallingTimer > 1.6f) // v78.0: Реже в 2 раза
        {
            _fallingTimer = 0f;
            float occupancy = GetSkyOccupancy(); 
            
            // Если экран в основном пуст (занято меньше 40%) - запускаем кометы
            if (occupancy < 0.4f)
            {
                // v78.0: Меньше комет в ряду (1 или 2)
                int burstSize = Random.Range(1, 3);
                StartCoroutine(SpawnCometBurst(burstSize));
            }
        }
    }

    private float GetSkyOccupancy()
    {
        float totalOccupiedWidth = 0f;
        float screenWidth = 1920f;
        float halfScreen = screenWidth / 2f;

        foreach (Transform child in _bgContainer)
        {
            // 🪲 BUGFIX #15: Избегаем тяжелого GetComponent в цикле (Все дети Canvas - RectTransform)
            RectTransform rt = (RectTransform)child;
            if (rt.sizeDelta.x < 1800f) continue; // Мелкие фоновые камни не блокируют небо

            float x = rt.anchoredPosition.x;
            float halfWidth = rt.sizeDelta.x / 2f;

            // Считаем проекцию на экран [-960, 960]
            float left = Mathf.Max(x - halfWidth, -halfScreen);
            float right = Mathf.Min(x + halfWidth, halfScreen);

            if (right > left)
            {
                totalOccupiedWidth += (right - left);
            }
        }

        return Mathf.Clamp01(totalOccupiedWidth / screenWidth);
    }

    private System.Collections.IEnumerator SpawnCometBurst(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // Перед каждым спавном в серии проверяем, не накрыло ли небо новым валуном
            if (GetSkyOccupancy() > 0.4f) yield break; 

            SpawnFallingStone();
            yield return new WaitForSeconds(Random.Range(0.25f, 0.6f));
        }
    }

    private void SpawnFallingStone()
    {
        if (_asteroidSprites == null || _asteroidSprites.Length == 0) return;

        GameObject stoneObj = new GameObject("FallingAsteroid", typeof(RectTransform), typeof(Image), typeof(BGStoneMover));
        stoneObj.transform.SetParent(_bgContainer, false);
        
        RectTransform rt = stoneObj.GetComponent<RectTransform>();
        
        // Размер: Маленькие и далекие (v71.0)
        float sizeFactor = Random.Range(0.4f, 0.7f);
        float size = 300f * sizeFactor;
        rt.sizeDelta = new Vector2(size, size);

        // Позиция: Смещаем вправо (v72.0), чтобы астероиды затихали до левой зоны
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        float spawnX = Random.Range(400f, 1500f); // Только центр и право
        float spawnY = 800f; 
        rt.anchoredPosition = new Vector2(spawnX, spawnY);

        // Визуал: Яркий астероид с хвостом (v71.0)
        Image img = stoneObj.GetComponent<Image>();
        img.sprite = _asteroidSprites[Random.Range(0, _asteroidSprites.Length)];
        img.color = Color.white;
        img.raycastTarget = false;

        // Движение: Под углом 45 градусов
        BGStoneMover mover = stoneObj.GetComponent<BGStoneMover>();
        mover.moveDirection = new Vector2(-1f, -1.0f);
        mover.speed = Random.Range(450f, 650f); 
        mover.rotationSpeed = 0f;
        
        // ЛОГИКА СГОРАНИЯ (v73.0: Плавное исчезновение)
        mover.destroyX = -200f; 
        mover.fadeStartX = 400f; 
        mover.enableFadeOut = true;

        // Поворот: СТРОГО 45 ГРАДУСОВ (v75.0)
        rt.localEulerAngles = Vector3.zero;
    }

    void SpawnHugeStone(float xPosition)
    {
        if (_stoneSprites == null || _stoneSprites.Length == 0) return;

        GameObject stoneObj = new GameObject("HugeBGStone", typeof(RectTransform), typeof(Image), typeof(BGStoneMover));
        stoneObj.transform.SetParent(_bgContainer, false);
        
        RectTransform rt = stoneObj.GetComponent<RectTransform>();
        
        // 1. ЛОГИКА ЧЕРЕДОВАНИЯ РАЗМЕРОВ (v51.0)
        // Паттерн: Маленький - Маленький - Огромный
        float sizeFactor;
        bool isTiny = (_patternStep % 3 != 0); // 1 и 2 шаги - поменьше
        if (isTiny) sizeFactor = Random.Range(3.5f, 5.0f);
        else sizeFactor = Random.Range(7.5f, 9.5f);
        _patternStep++;

        float size = 300f * sizeFactor;
        rt.sizeDelta = new Vector2(size, size);

        // 2. ПОЗИЦИЯ С УЧЕТОМ РАЗМЕРА (v68.0 - Центр всегда пустой!)
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        float spawnY;
        
        if (isTiny)
        {
            // Маленькие камни: теперь тоже избегают центральную зону (+/- 250)
            bool isTop = (Random.value > 0.5f);
            if (isTop) spawnY = Random.Range(250f, 500f);
            else spawnY = Random.Range(-500f, -250f);
        }
        else
        {
            // Гиганты: прижаты к краям еще сильнее
            bool isTop = (Random.value > 0.5f);
            if (isTop) spawnY = Random.Range(650f, 950f);
            else spawnY = Random.Range(-950f, -650f);
        }
        
        rt.anchoredPosition = new Vector2(xPosition, spawnY);

        // 3. ВНЕШНИЙ ВИД И ЦВЕТ (v68.0 - Еще темнее!)
        // Огромные камни: 0.12 (очень темные), Маленькие: 0.28
        float brightness = Mathf.Lerp(0.28f, 0.12f, (sizeFactor - 3.5f) / 6f);
        Image img = stoneObj.GetComponent<Image>();
        img.sprite = _stoneSprites[Random.Range(0, _stoneSprites.Length)];
        img.color = new Color(brightness, brightness, brightness + 0.02f, 1f); 
        img.raycastTarget = false;

        // 4. СЛОИ ВНУТРИ ФОНА (v51.0)
        if (!isTiny) stoneObj.transform.SetAsFirstSibling();
        else stoneObj.transform.SetAsLastSibling();

        // 5. ДВИЖЕНИЕ И ПАРАЛЛАКС
        BGStoneMover mover = stoneObj.GetComponent<BGStoneMover>();
        
        float speedNormalized = 1f - ((sizeFactor - 3.5f) / 6f); 
        mover.speed = 80f + (speedNormalized * 270f); 
        
        float rotBase = 1.5f + (speedNormalized * 10f);
        mover.rotationSpeed = (Random.value > 0.5f) ? rotBase : -rotBase;

        rt.localEulerAngles = new Vector3(0, 0, Random.Range(0f, 360f));
        rt.localScale = new Vector3(Random.value > 0.5f ? 1f : -1f, Random.value > 0.5f ? 1f : -1f, 1f);
    }
}

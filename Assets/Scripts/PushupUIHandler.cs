
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class PushupUIHandler : MonoBehaviour
{
    public PushupVerifier verifier;
    public RhythmManager rhythmManager;
    
    public Text counterText;
    public Text scoreText;
    public Text rhythmHitText;
    public GameObject cheatWarningObj;
    
    [Header("Pulse Engine (v11.0)")]
    public OxygenIndicator oxygenIndicator;
    public GameObject gameOverPanel;

    public RectTransform oxygenHUDRoot; // Перетяни сюда HeartsRoot из Unity, чтобы двигать его руками
    public RectTransform boostBarRoot;  // Перетяни сюда BoostBarRoot, чтобы двигать его руками

    public static PushupUIHandler Instance; // v113.9

    void Awake()
    {
        Instance = this;
        AudioListener.pause = false; // 🪲 BUGFIX #14: Защита статики звука
    }

    private float _hitTextTimer = 0f; // v114.3
    private Sprite _circleSprite;    // v114.3

    // --- ШКАЛА УСКОРЕНИЯ ---
    private GameObject _boostBarRoot;
    private RectTransform _boostBarFill;
    
    private GameObject _successCanvasObj; // v114.0
    private GameObject _successPanelInstance; // v114.2
    public static RectTransform ScoreTarget; // Для JuiceManager (v2.2)

    void OnDestroy() => Dispose();
    void Dispose() 
    { 
        if (verifier != null)
        {
            // 🪲 BUGFIX #13: Отписка от событий (Устранение утечки памяти)
            verifier.OnPushupCounted -= HandlePushupCounted;
            verifier.OnCheatStatusChanged -= HandleCheatStatus;
            verifier.OnHitVisuals -= HandleHitVisuals;
            verifier.OnOxygenChanged -= HandleOxygenChanged;
        }
    } 

    // 🪲 BUGFIX #13: Именованные методы вместо анонимных lambd
    private void HandlePushupCounted(int count) { if(counterText) counterText.text = count.ToString(); }
    private void HandleCheatStatus(string cheatReason) 
    {
        if (cheatWarningObj != null)
        {
            bool isCheating = !string.IsNullOrEmpty(cheatReason);
            cheatWarningObj.SetActive(isCheating);
            if (isCheating) { var t = cheatWarningObj.GetComponent<Text>(); if (t != null) t.text = cheatReason; }
        }
    }
    private void HandleHitVisuals(string txt, int scoreVal) 
    { 
        if (scoreText != null) 
        {
            scoreText.text = "Очки: " + verifier.Score;
            PulseScore();
        }
    }
    private void HandleOxygenChanged(int ox) 
    {
        if (oxygenIndicator != null) oxygenIndicator.UpdateOxygen(ox);
        if (ox <= 0) ShowGameOver(); 
    }

    // КОМАНДА ДЛЯ СОЗДАНИЯ ШАБЛОНА В РЕДАКТОРЕ (v30.0)
    // Нажми правый клик на компоненте в Инспекторе -> "Create HUD Template"
    [ContextMenu("Create HUD Template (v30.0)")]
    public void CreateHUDTemplate()
    {
        EnsureOxygenHUD(true); 
        Debug.Log("HUD Template Created! Теперь ты можешь двигать его в Unity.");
    }

    // ГАРАНТИРОВАННЫЙ HUD (v31.1): Полная авто-магия без ручной привязки
    private void EnsureOxygenHUD(bool forceCreateInEditor = false)
    {
        // 1. АВТО-ПОИСК КАНВАСА
        GameObject hudCanvasObj = GameObject.Find("Oxygen_HUD_Canvas");
        if (hudCanvasObj == null)
        {
            hudCanvasObj = new GameObject("Oxygen_HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas hudCanvas = hudCanvasObj.GetComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 30000; 
            CanvasScaler scaler = hudCanvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // 2. АВТО-ПОИСК КОРНЯ ДЛЯ ПЕРЕДНЕГО ПЛАНА (v66.0 - Растяжка на весь экран)
        Transform foregroundRoot = hudCanvasObj.transform.Find("Foreground_HUD_Root");
        if (foregroundRoot == null)
        {
            GameObject fgObj = new GameObject("Foreground_HUD_Root", typeof(RectTransform));
            fgObj.transform.SetParent(hudCanvasObj.transform, false);
            foregroundRoot = fgObj.transform;
            
            // Растягиваем на весь экран, чтобы дети могли липнуть к углам
            RectTransform fgRt = fgObj.GetComponent<RectTransform>();
            fgRt.anchorMin = Vector2.zero;
            fgRt.anchorMax = Vector2.one;
            fgRt.offsetMin = fgRt.offsetMax = Vector2.zero;
        }
        foregroundRoot.SetAsLastSibling();

        // 3. ПРИНУДИТЕЛЬНОЕ ВЫРАВНИВАНИЕ КОРНЯ OXYGEN (v65.0)
        if (oxygenHUDRoot == null)
        {
            Transform existingRoot = hudCanvasObj.transform.Find("HeartsRoot");
            if (existingRoot == null) existingRoot = foregroundRoot.Find("HeartsRoot"); 
            if (existingRoot != null) oxygenHUDRoot = existingRoot.GetComponent<RectTransform>();
            else
            {
                GameObject hudRoot = new GameObject("HeartsRoot", typeof(RectTransform));
                hudRoot.transform.SetParent(foregroundRoot, false);
                oxygenHUDRoot = hudRoot.GetComponent<RectTransform>();
            }
        }
        
        oxygenHUDRoot.SetParent(foregroundRoot, false);
        oxygenHUDRoot.anchorMin = oxygenHUDRoot.anchorMax = oxygenHUDRoot.pivot = new Vector2(1f, 1f); 
        oxygenHUDRoot.anchoredPosition = new Vector2(-50f, -50f); // В УГОЛ!
        oxygenHUDRoot.sizeDelta = new Vector2(900, 150);

        Font safeFont = (scoreText != null && scoreText.font != null) ? scoreText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 4. ПРИНУДИТЕЛЬНОЕ ВЫРАВНИВАНИЕ ТЕКСТА (v96.0: Скрыто по просьбе пользователя)
        Transform labelT = oxygenHUDRoot.Find("OxygenLabel");
        if (labelT != null) labelT.gameObject.SetActive(false); 
        
        /* Код создания удален для чистоты v96.0
        if (labelT == null)
        {
            GameObject txtObj = new GameObject("OxygenLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            txtObj.transform.SetParent(oxygenHUDRoot.transform, false);
            labelT = txtObj.transform;
        }
        Text label = labelT.GetComponent<Text>();
        label.text = "КИСЛОРОД";
        label.font = safeFont; label.fontSize = 42; label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleRight; label.color = Color.white;
        
        RectTransform rtLabel = labelT.GetComponent<RectTransform>();
        rtLabel.anchorMin = rtLabel.anchorMax = rtLabel.pivot = new Vector2(1, 0.5f);
        rtLabel.anchoredPosition = new Vector2(-480, -12); // Центровка
        rtLabel.sizeDelta = new Vector2(300, 80);
        */

        // 5. ПРИНУДИТЕЛЬНОЕ ВЫРАВНИВАНИЕ СЕРДЕЧЕК (v65.0)
        Transform existingIcons = oxygenHUDRoot.Find("HeartIcons");
        GameObject iconsRoot;
        if (existingIcons != null)
        {
            iconsRoot = existingIcons.gameObject;
            if (Application.isPlaying || forceCreateInEditor)
            {
                for (int i = iconsRoot.transform.childCount - 1; i >= 0; i--)
                    DestroyImmediate(iconsRoot.transform.GetChild(i).gameObject);
            }
        }
        else
        {
            iconsRoot = new GameObject("HeartIcons", typeof(RectTransform));
            iconsRoot.transform.SetParent(oxygenHUDRoot.transform, false);
        }
        
        RectTransform rtIcons = iconsRoot.GetComponent<RectTransform>();
        rtIcons.anchorMin = rtIcons.anchorMax = rtIcons.pivot = new Vector2(0.5f, 0.5f);
        rtIcons.anchoredPosition = Vector2.zero; 
        rtIcons.sizeDelta = new Vector2(450, 100);
        rtIcons.transform.SetAsLastSibling();

        // 5. ГЕНЕРАЦИЯ КАРТИНОК-СЕРДЕЧЕК
        if (Application.isPlaying || forceCreateInEditor)
        {
            if (oxygenIndicator == null) oxygenIndicator = oxygenHUDRoot.GetComponent<OxygenIndicator>();
            if (oxygenIndicator == null) oxygenIndicator = oxygenHUDRoot.gameObject.AddComponent<OxygenIndicator>();
            
            oxygenIndicator.normalColor = Color.white; 
            oxygenIndicator.lostColor = new Color(0,0,0,0);
            oxygenIndicator.circles = new Image[5];

            Sprite heartSprite = null;
            Texture2D tex = Resources.Load<Texture2D>("Emojis/Hearts/heart");
            if (tex != null) heartSprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f, 0.5f));

            for (int i = 0; i < 5; i++)
            {
                GameObject heartObj = new GameObject("Heart_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
                heartObj.transform.SetParent(iconsRoot.transform, false);
                
                Image img = heartObj.GetComponent<Image>();
                img.color = Color.white; 
                if (heartSprite != null) img.sprite = heartSprite;
                
                Outline outHeart = heartObj.GetComponent<Outline>();
                outHeart.effectColor = Color.black; 
                
                RectTransform rtC = heartObj.GetComponent<RectTransform>();
                rtC.sizeDelta = new Vector2(75, 75); 
                rtC.anchorMin = rtC.anchorMax = new Vector2(0.5f, 0.5f);
                rtC.anchoredPosition = new Vector2((2 - i) * 85f, 0);
                
                oxygenIndicator.circles[i] = img;
            }
        }

        // 6. АВТО-ПОИСК ШКАЛЫ ТУРБО И ЕЁ ЦЕНТРОВКА (v34.0)
        if (boostBarRoot == null)
        {
            Transform existingBoost = hudCanvasObj.transform.Find("BoostBarRoot");
            if (existingBoost == null) existingBoost = oxygenHUDRoot.Find("BoostBarRoot"); // Поиск старых версий

            if (existingBoost != null) boostBarRoot = existingBoost.GetComponent<RectTransform>();
            else
            {
                _boostBarRoot = new GameObject("BoostBarRoot", typeof(RectTransform), typeof(Image), typeof(Outline));
                boostBarRoot = _boostBarRoot.GetComponent<RectTransform>();
                
                GameObject fillObj = new GameObject("BoostBarFill", typeof(RectTransform), typeof(Image));
                fillObj.transform.SetParent(boostBarRoot.transform, false);
                fillObj.GetComponent<Image>().color = Color.cyan;
            }
        }

        if (boostBarRoot != null)
        {
            // ПРИНУДИТЕЛЬНО перемещаем в корень Канваса для идеальной центровки по экрану!
            boostBarRoot.SetParent(hudCanvasObj.transform, false); 
            // Жестко привязываем к центру секторов B1/C1 (v35.0)
            // Длина сокращена на 25% (было 0.25-0.75, стало 0.3125-0.6875)
            boostBarRoot.anchorMin = new Vector2(0.3125f, 0.875f); 
            boostBarRoot.anchorMax = new Vector2(0.6875f, 0.875f); 
            boostBarRoot.pivot = new Vector2(0.5f, 0.5f);
            boostBarRoot.anchoredPosition = Vector2.zero; 
            boostBarRoot.sizeDelta = new Vector2(0f, 60f); 
            
            _boostBarRoot = boostBarRoot.gameObject;
            _boostBarRoot.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f); // ЕДИНАЯ ТЁМНАЯ ПОДЛОЖКА (v34.0)

            if (_boostBarRoot.transform.childCount > 0) 
            {
                _boostBarFill = _boostBarRoot.transform.GetChild(0).GetComponent<RectTransform>();
                // Заливка будет растягиваться от левого края подложки до текущего процента
                _boostBarFill.anchorMin = Vector2.zero; 
                _boostBarFill.pivot = new Vector2(0, 0.5f); 
                _boostBarFill.anchoredPosition = Vector2.zero; 
                _boostBarFill.sizeDelta = Vector2.zero; 
            }
            _boostBarRoot.SetActive(false); 
        }

        if (hudCanvasObj.GetComponentInChildren<JuiceManager>() == null && Application.isPlaying)
            hudCanvasObj.AddComponent<JuiceManager>();

        // ГАРАНТИЯ ПЕРЕДНЕГО ПЛАНА И ЛЕЙАУТА (v98.0: Исправлены пивоты для центровки)
        if (scoreText != null) 
        {
            scoreText.transform.SetParent(foregroundRoot, false); // false, чтобы не наследовать старые координаты
            
            // ЦЕНТР СЕКТОРА A1 (v98.0)
            RectTransform scoreRt = scoreText.GetComponent<RectTransform>();
            scoreRt.anchorMin = scoreRt.anchorMax = new Vector2(0.125f, 0.875f); // Якорь в центре сектора
            scoreRt.pivot = new Vector2(0.5f, 0.5f); // ПИВОТ В ЦЕНТРЕ ТЕКСТА
            scoreRt.anchoredPosition = Vector2.zero;
            scoreRt.sizeDelta = new Vector2(500, 100);
            scoreText.alignment = TextAnchor.MiddleCenter; 
        }

        if (counterText != null) 
        {
            counterText.transform.SetParent(foregroundRoot, false);
        }

        if (rhythmHitText != null) rhythmHitText.transform.SetParent(foregroundRoot, false);
        
        if (cheatWarningObj != null) 
        {
            cheatWarningObj.transform.SetParent(foregroundRoot, false);
            
            // ПРИНУДИТЕЛЬНОЕ ПОЗИЦИОНИРОВАНИЕ (v91.0)
            RectTransform cwRt = cheatWarningObj.GetComponent<RectTransform>();
            cwRt.anchorMin = cwRt.anchorMax = new Vector2(0.5f, 0.5f); // СТРОГО ЦЕНТР
            cwRt.pivot = new Vector2(0.5f, 0.5f); 
            cwRt.anchoredPosition = new Vector2(0, 0); 
            cwRt.sizeDelta = new Vector2(1200, 300);

            Text cwText = cheatWarningObj.GetComponent<Text>();
            if (cwText != null)
            {
                cwText.fontStyle = FontStyle.Bold;
                cwText.alignment = TextAnchor.MiddleCenter;
                cwText.color = Color.yellow; // ЖЕЛТЫЙ (v91.0)
                cwText.fontSize = 80; // КРУПНО (v91.0)
                
                // ОБВОДКА (v92.0)
                Outline outline = cheatWarningObj.GetComponent<Outline>();
                if (outline == null) outline = cheatWarningObj.AddComponent<Outline>();
                outline.effectColor = Color.red;
                outline.effectDistance = new Vector2(6, -6); // В два раза толще (v92.0)
            }
        }

        // КОРРЕКЦИЯ ЖИЗНЕЙ (ЦЕНТР D1 - v98.0)
        if (oxygenHUDRoot != null)
        {
            oxygenHUDRoot.anchorMin = oxygenHUDRoot.anchorMax = new Vector2(0.875f, 0.875f);
            oxygenHUDRoot.pivot = new Vector2(0.5f, 0.5f); // ПИВОТ В ЦЕНТРЕ ГРУППЫ СЕРДЕЦ
            oxygenHUDRoot.anchoredPosition = Vector2.zero;
        }
    }

    void Start()
    {
        EnsureOxygenHUD(false); // Просто проверяем, всё ли на месте
        // Закрепляем ПЕРЕДНИЙ ПЛАН еще раз
        GameObject fg = GameObject.Find("Foreground_HUD_Root");
        if (fg != null) fg.transform.SetAsLastSibling();

        if (scoreText != null) ScoreTarget = scoreText.GetComponent<RectTransform>();
        
        if (verifier != null)
        {
            // 🪲 BUGFIX #13: Подписка через именованные методы
            verifier.OnPushupCounted += HandlePushupCounted;
            verifier.OnCheatStatusChanged += HandleCheatStatus;
            verifier.OnHitVisuals += HandleHitVisuals;
            verifier.OnOxygenChanged += HandleOxygenChanged;

            if (oxygenIndicator != null) oxygenIndicator.UpdateOxygen(5);
        }
    }

    public void ShowSuccessPanel()
    {
        try 
        {
            if (_successPanelInstance != null) return;
            if (gameOverPanel != null && gameOverPanel.activeSelf) return;

            // 1. СОЗДАЕМ ВЫДЕЛЕННЫЙ ХОЛСТ (v114.0 - Максимальный приоритет)
            _successCanvasObj = new GameObject("Final_Success_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas c = _successCanvasObj.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 999999; 
            
            CanvasScaler cs = _successCanvasObj.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);

            // 2. ГАРАНТИЯ КНОПОК
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // 3. СОЗДАЕМ ПАНЕЛЬ (v114.0)
            _successPanelInstance = new GameObject("SuccessPanel", typeof(RectTransform), typeof(Image));
            _successPanelInstance.transform.SetParent(_successCanvasObj.transform, false);
            
            RectTransform rt = _successPanelInstance.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; 
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            
            _successPanelInstance.GetComponent<Image>().color = new Color(0, 0, 0, 0.98f); 

            // Гарантия шрифта
            Font mainFont = (scoreText != null && scoreText.font != null) ? scoreText.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (mainFont == null) mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ЦЕНТРАЛЬНЫЙ ТЕКСТ
            GameObject title = new GameObject("Title", typeof(RectTransform), typeof(Text), typeof(Outline));
            title.transform.SetParent(_successPanelInstance.transform, false);
            Text t = title.GetComponent<Text>();
            t.text = "MISSION COMPLETE"; t.font = mainFont; t.fontSize = 120; t.color = Color.cyan; t.alignment = TextAnchor.MiddleCenter;
            title.GetComponent<Outline>().effectColor = Color.black;
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 240);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(1800, 400);

            // --- ЗВЕЗДНЫЙ РЕЙТИНГ ---
            int totalCookies = LevelSpawner.TotalCookiesSpawned;
            int collectedCookies = PlayerCollision.CookiesCollected;
            float cookiePct = totalCookies > 0 ? (float)collectedCookies / totalCookies : 0f;
            int totalStars = LevelSpawner.TotalStarsSpawned;
            int collectedStars = PlayerCollision.StarsCollected;
            int pushupsDone = GameObject.FindFirstObjectByType<PushupVerifier>()?.PushupCount ?? 0;

            int starCount = 0;
            if (cookiePct >= 0.5f) starCount++;
            if (PlayerCollision.AsteroidHitsTaken == 0) starCount++;
            if (PlayerCollision.RockHitsTaken == 0) starCount++;
            if (pushupsDone >= 10) starCount++;
            if (totalStars > 0 && collectedStars >= totalStars) starCount++;
            starCount = Mathf.Clamp(starCount, 0, 5);

            string starsString = "";
            for (int i = 0; i < 5; i++) starsString += (i < starCount) ? "★" : "☆";

            GameObject starsObj = new GameObject("StarsRating", typeof(RectTransform), typeof(Text), typeof(Outline));
            starsObj.transform.SetParent(_successPanelInstance.transform, false);
            Text stText = starsObj.GetComponent<Text>();
            stText.text = starsString; stText.font = mainFont; stText.fontSize = 150; stText.color = new Color(1f, 0.85f, 0f, 1f); stText.alignment = TextAnchor.MiddleCenter;
            starsObj.GetComponent<Outline>().effectColor = Color.black;
            starsObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);
            starsObj.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 200);

            // КНОПКА RESTART
            GameObject btnObj = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(_successPanelInstance.transform, false);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(500, 150); btnRt.anchoredPosition = new Vector2(-280, -200); 
            btnObj.GetComponent<Image>().color = Color.white;
            
            GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            Text btnT = btnTxtObj.GetComponent<Text>();
            btnT.text = "RESTART"; btnT.font = mainFont; btnT.fontSize = 60; btnT.color = Color.black; btnT.alignment = TextAnchor.MiddleCenter;
            btnTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 150);
            btnObj.GetComponent<Button>().onClick.AddListener(() => { ReloadGame(); });

            // КНОПКА NEXT LEVEL
            GameObject nextBtnObj = new GameObject("NextLevelButton", typeof(RectTransform), typeof(Image), typeof(Button));
            nextBtnObj.transform.SetParent(_successPanelInstance.transform, false);
            RectTransform nextBtnRt = nextBtnObj.GetComponent<RectTransform>();
            nextBtnRt.sizeDelta = new Vector2(500, 150); nextBtnRt.anchoredPosition = new Vector2(280, -200); 
            nextBtnObj.GetComponent<Image>().color = new Color(0.2f, 1f, 1f, 1f); 
            
            GameObject nextBtnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            nextBtnTxtObj.transform.SetParent(nextBtnObj.transform, false);
            Text nextBtnT = nextBtnTxtObj.GetComponent<Text>();
            nextBtnT.text = "NEXT LEVEL"; nextBtnT.font = mainFont; nextBtnT.fontSize = 60; nextBtnT.color = Color.black; nextBtnT.alignment = TextAnchor.MiddleCenter;
            nextBtnTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 150);
            nextBtnObj.GetComponent<Button>().onClick.AddListener(() => { LoadNextLevel(); });

            // ПАУЗА (v114.0)
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[v114.0 UI BUG] " + ex.Message);
            Time.timeScale = 0f;
        }
    }

    private void ReloadGame()
    {
        Time.timeScale = 1.0f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void LoadNextLevel()
    {
        Time.timeScale = 1.0f;
        AudioListener.pause = false;

        string currentScene = SceneManager.GetActiveScene().name;
        
        // v136.5: Жёстко переводим на Level2, если мы сейчас не там.
        if (!currentScene.ToLower().Contains("level2"))
        {
            Debug.Log("[NextLevel] Переход на Уровень 2");
            SceneManager.LoadScene("Level2");
        }
        else
        {
            // Если уже на Level2, возвращаемся в самое начало (Меню/Ур1)
            Debug.Log("[NextLevel] Возврат на Уровень 1");
            SceneManager.LoadScene(0);
        }
    }

    // РЕЗЕРВНЫЙ GUI (v114.0): Появится даже если Canvas умрет
    void OnGUI()
    {
        if (_successPanelInstance != null)
        {
            GUI.backgroundColor = Color.red;
            if (GUI.Button(new Rect(20, 20, 250, 100), "RESTART (SAFE)"))
            {
                ReloadGame();
            }
        }
    }

    public bool IsSuccessPanelActive()
    {
        return _successPanelInstance != null && _successPanelInstance.activeSelf;
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null && gameOverPanel.activeSelf) return;

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        Time.timeScale = 0f;

        if (gameOverPanel == null)
        {
            GameObject canvasObj = GameObject.Find("Oxygen_HUD_Canvas");
            gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
            gameOverPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rt = gameOverPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            
            gameOverPanel.GetComponent<Image>().color = Color.black; 

            GameObject goTxt = new GameObject("Title", typeof(RectTransform), typeof(Text));
            goTxt.transform.SetParent(gameOverPanel.transform, false);
            Text t = goTxt.GetComponent<Text>();
            t.text = "Game Over";
            t.font = (scoreText != null) ? scoreText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 80; t.color = Color.red; t.alignment = TextAnchor.MiddleCenter;
            goTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);
            goTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 300);

            GameObject btnObj = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(gameOverPanel.transform, false);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(400, 120); btnRt.anchoredPosition = new Vector2(0, -150);
            btnObj.GetComponent<Image>().color = Color.white;
            
            GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            Text btnT = btnTxtObj.GetComponent<Text>();
            btnT.text = "RESTART"; btnT.font = t.font; btnT.fontSize = 48; btnT.color = Color.black; btnT.alignment = TextAnchor.MiddleCenter;
            btnTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 120);

            btnObj.GetComponent<Button>().onClick.AddListener(() => {
                ReloadGame(); // 🪲 BUGFIX #14: Используем единый метод с разблокировкой аудио
            });
        }
        
        gameOverPanel.SetActive(true);
    }

    public void PulseScore()
    {
        if (scoreText == null) return;
        StopCoroutine("PulseScoreCoroutine");
        StartCoroutine(PulseScoreCoroutine());
    }

    private IEnumerator PulseScoreCoroutine()
    {
        RectTransform rt = scoreText.GetComponent<RectTransform>();
        float duration = 0.15f;
        Vector3 largeScale = Vector3.one * 1.3f;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            rt.localScale = Vector3.Lerp(Vector3.one, largeScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < duration)
        {
            rt.localScale = Vector3.Lerp(largeScale, Vector3.one, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    void Update()
    {
        if (_hitTextTimer > 0)
        {
            _hitTextTimer -= Time.deltaTime;
            if (_hitTextTimer <= 0 && rhythmHitText != null) rhythmHitText.gameObject.SetActive(false);
        }

        // --- ОБНОВЛЕНИЕ ШКАЛЫ ТУРБО ---
        RhythmManager rm = rhythmManager != null ? rhythmManager : (verifier != null ? verifier.rhythmManager : null);
        if (rm != null && _boostBarRoot != null)
        {
            float boostProgress = rm.GetSpeedupProgress();
            if (boostProgress > 0)
            {
                if (!_boostBarRoot.activeSelf) _boostBarRoot.SetActive(true);
                _boostBarFill.anchorMax = new Vector2(boostProgress, 1f);
                _boostBarFill.sizeDelta = Vector2.zero;
            }
            else
            {
                if (_boostBarRoot.activeSelf) _boostBarRoot.SetActive(false);
            }
        }
    }
}

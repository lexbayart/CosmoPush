using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CosmoPushSetupEditor : EditorWindow
{
    [MenuItem("Cosmo Push/ПОЛНЫЙ СБРОС И НАСТРОЙКА (Геймплей)")]
    public static void SetupScene()
    {
        // 1. ОЧИСТКА СЦЕНЫ (безопасное удаление только корневых объектов)
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            if (obj.name == "Main Camera" || obj.name == "Directional Light" || obj.name.Contains("CosmoPush")) continue;
            DestroyImmediate(obj);
        }

        // 2. КАМЕРА И СВЕТ
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            mainCam = camObj.GetComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = Color.black; 

        // 3. КАНВАС И UI
        GameObject canvasObj = new GameObject("UI_Canvas_Main", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // Горизонтальный (Landscape) формат

        // ПРЕВЬЮ КАМЕРЫ (ФОН)
        GameObject rawImageObj = new GameObject("Camera_Preview", typeof(RectTransform));
        rawImageObj.transform.SetParent(canvasObj.transform, false);
        RawImage rawImage = rawImageObj.AddComponent<RawImage>();
        var fitter = rawImageObj.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        rawImage.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Делаем камеру темнее, чтобы графику было лучше видно
        var camHandler = rawImageObj.AddComponent<CameraPreviewHandler>();

        // СКЕЛЕТ (Рентген)
        GameObject skeletonObj = new GameObject("Skeleton_Drawer", typeof(RectTransform));
        skeletonObj.transform.SetParent(rawImageObj.transform, false); 
        var drawer = skeletonObj.AddComponent<PoseSkeletonDrawer>();

        // СЧЕТЧИК ОТЖИМАНИЙ 
        GameObject textObj = new GameObject("Pushup_Counter_Text", typeof(RectTransform));
        textObj.transform.SetParent(canvasObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.green;
        text.fontSize = 150;
        text.text = "0";
        var rt = text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.8f);
        rt.anchorMax = new Vector2(0.5f, 0.8f);
        rt.sizeDelta = new Vector2(400, 200);

        // === НОВЫЕ ЭЛЕМЕНТЫ ===
        GameObject scoreObj = new GameObject("Score_Text", typeof(RectTransform));
        scoreObj.transform.SetParent(canvasObj.transform, false);
        Text scoreText = scoreObj.AddComponent<Text>();
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.alignment = TextAnchor.MiddleLeft;
        scoreText.color = Color.yellow;
        scoreText.fontSize = 60;
        scoreText.text = "СКОР: 0";
        var scRect = scoreText.GetComponent<RectTransform>();
        scRect.anchorMin = new Vector2(0.05f, 0.95f);
        scRect.anchorMax = new Vector2(0.05f, 0.95f);
        scRect.pivot = new Vector2(0, 1);
        scRect.sizeDelta = new Vector2(400, 100);

        GameObject hitObj = new GameObject("GameEvent_Text", typeof(RectTransform));
        hitObj.transform.SetParent(canvasObj.transform, false);
        Text hitText = hitObj.AddComponent<Text>();
        hitText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hitText.alignment = TextAnchor.MiddleCenter;
        hitText.fontSize = 120;
        hitText.text = "";
        var rhRect = hitText.GetComponent<RectTransform>();
        rhRect.anchorMin = new Vector2(0.5f, 0.5f);
        rhRect.anchorMax = new Vector2(0.5f, 0.5f);
        rhRect.sizeDelta = new Vector2(1000, 200);

        GameObject cheatObj = new GameObject("CheatWarning_Text", typeof(RectTransform));
        cheatObj.transform.SetParent(canvasObj.transform, false);
        Text cheatText = cheatObj.AddComponent<Text>();
        cheatText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cheatText.alignment = TextAnchor.MiddleCenter;
        cheatText.color = Color.red;
        cheatText.fontSize = 60;
        cheatText.text = "";
        cheatObj.SetActive(false);
        var cwRect = cheatText.GetComponent<RectTransform>();
        cwRect.anchorMin = new Vector2(0.5f, 0.7f);
        cwRect.anchorMax = new Vector2(0.5f, 0.7f);
        cwRect.sizeDelta = new Vector2(1000, 150);


        // 4. НЕЙРОСЕТЬ (BLAZEPOSE)
        GameObject trackerObj = new GameObject("BlazePose_Tracker");
        var tracker = trackerObj.AddComponent<WebcamPoseDetection>();
        tracker.poseDetectorAsset = AssetDatabase.LoadAssetAtPath<Unity.InferenceEngine.ModelAsset>("Assets/Models/pose_detection.onnx");
        tracker.poseLandmarkerAsset = AssetDatabase.LoadAssetAtPath<Unity.InferenceEngine.ModelAsset>("Assets/Models/pose_landmarks_full.onnx"); 
        tracker.anchorsCSV = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Models/anchors.csv");

        // 5. МЕНЕДЖЕР РИТМА
        GameObject rhythmObj = new GameObject("Rhythm_Manager");
        var rhythm = rhythmObj.AddComponent<RhythmManager>();

        // 6. ВЕРИФИКАТОР ОТЖИМАНИЙ
        GameObject verifierObj = new GameObject("Pushup_Verifier");
        var verifier = verifierObj.AddComponent<PushupVerifier>();
        verifier.poseDetection = tracker;
        verifier.rhythmManager = rhythm;
        
        // === ГЕЙМПЛЕЙНЫЙ СЛОЙ ===
        GameObject gameplayObj = new GameObject("Gameplay_Container", typeof(RectTransform));
        gameplayObj.transform.SetParent(canvasObj.transform, false);
        var gpRect = gameplayObj.GetComponent<RectTransform>();
        gpRect.anchorMin = Vector2.zero; gpRect.anchorMax = Vector2.one; gpRect.sizeDelta = Vector2.zero;

        var laneSys = gameplayObj.AddComponent<LaneSystem>();
        // Для Canvas высотой 1080 (у нас referenceResolution Y=1080)
        laneSys.Setup(1080f);
        
        // Визуализируем 2 полосы (Верх и Низ) (v20.0)
        for(int i=0; i<2; i++) {
            GameObject line = new GameObject(i == 0 ? "LaneLine_TOP" : "LaneLine_BOTTOM", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(gameplayObj.transform, false);
            var lRt = line.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0, 0.5f); lRt.anchorMax = new Vector2(1, 0.5f);
            lRt.sizeDelta = new Vector2(0, 15);
            Image img = line.GetComponent<Image>();
            img.color = new Color(0f, 1f, 1f, 0.4f); 
            lRt.anchoredPosition = new Vector2(0, i == 0 ? 190f : -180f);

            if (i == 0) laneSys.HighLine = img;
            if (i == 1) laneSys.LowLine = img;
        }

        // КОСМОНАВТ (Плеер)
        GameObject astroObj = new GameObject("Astronaut_Player", typeof(RectTransform), typeof(Image));
        astroObj.transform.SetParent(gameplayObj.transform, false);
        var astroRt = astroObj.GetComponent<RectTransform>();
        astroRt.sizeDelta = new Vector2(160, 160); // Круг
        astroObj.GetComponent<Image>().color = Color.white;
        
        var pChar = astroObj.AddComponent<PlayerCharacter>();
        pChar.laneSystem = laneSys;
        pChar.verifier = verifier;
        
        var pCol = astroObj.AddComponent<PlayerCollision>();
        pCol.eventText = hitText; 
        pCol.verifier = verifier; 

        // СПАВНЕР
        GameObject spawnerObj = new GameObject("Level_Spawner");
        var spawner = spawnerObj.AddComponent<LevelSpawner>();
        spawner.laneSystem = laneSys;
        spawner.rhythmManager = rhythm;
        spawner.objectsContainer = gameplayObj.transform;

        // 7. СВЯЗКА ВСЕГО
        camHandler.tracker = tracker;
        camHandler.rawImage = rawImage;
        drawer.tracker = tracker;

        var uiHandler = canvasObj.AddComponent<PushupUIHandler>();
        uiHandler.verifier = verifier;
        uiHandler.rhythmManager = rhythm;
        uiHandler.counterText = text;
        uiHandler.scoreText = scoreText;
        uiHandler.rhythmHitText = hitText; // текст об игре
        uiHandler.cheatWarningObj = cheatObj;

        Debug.Log("Сцена CosmoPush с Геймплеем (Фаза 3) собрана успешно!");
    }
}

using System;
using System.Threading;
using Unity.Mathematics;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.Analytics;

/// <summary>
/// Реализация трекинга через Unity Sentis (BlazePose).
/// </summary>
public class WebcamPoseDetection : MonoBehaviour, IBodyTracker
{
    [Header("Модели BlazePose")]
    public ModelAsset poseDetectorAsset;
    public ModelAsset poseLandmarkerAsset;
    public TextAsset anchorsCSV;

    [Header("Настройки")]
    [Range(0, 1)]
    public float scoreThreshold = 0.35f;
    public int cameraIndex = 0;
    public int requestedWidth = 640;
    public int requestedHeight = 480;

    public static WebcamPoseDetection Instance { get; private set; } // v136.6: Глобальный синглтон

    public float[] Landmarks { get; private set; }
    public bool IsTracking { get; private set; }
    public WebCamTexture CamTexture { get; private set; }

    private CancellationTokenSource m_Cts;
    const int k_NumAnchors = 2254;
    const int k_NumKeypoints = 33;
    const int detectorInputSize = 224;
    const int landmarkerInputSize = 256;

    float[,] m_Anchors;
    Worker m_PoseDetectorWorker, m_PoseLandmarkerWorker;
    Tensor<float> m_DetectorInput, m_LandmarkerInput;
    float m_TextureWidth, m_TextureHeight;

    // Реализация IBodyTracker
    public float2 GetNose() => (Landmarks == null || !IsTracking) ? new float2(0.5f, 0.5f) : new float2(Landmarks[5 * 0 + 0], Landmarks[5 * 0 + 1]);

    public float2 GetShoulderMidpoint()
    {
        if (Landmarks == null || !IsTracking) return new float2(0.5f, 0.5f);
        float x = (Landmarks[5 * 11 + 0] + Landmarks[5 * 12 + 0]) / 2f;
        float y = (Landmarks[5 * 11 + 1] + Landmarks[5 * 12 + 1]) / 2f;
        return new float2(x, y);
    }

    public float GetKeypointY(int index) => (Landmarks == null || !IsTracking) ? 0.5f : Landmarks[5 * index + 1];
    public float GetKeypointX(int index) => (Landmarks == null || !IsTracking) ? 0.5f : Landmarks[5 * index + 0];
    public float GetKeypointZ(int index) => (Landmarks == null || !IsTracking) ? 0f : Landmarks[5 * index + 2];
    public float GetKeypointVisibility(int index) => (Landmarks == null || !IsTracking) ? 0f : Landmarks[5 * index + 3];

    void Awake()
    {
        // 🪲 BUGFIX #15: Не убиваем камеру при смене сцен, иначе она зависнет (Mac/iOS баг)
        if (Instance != null && Instance != this)
        {
            Debug.Log("[Webcam] Камера уже работает, удаляем дубликат на новом уровне.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null); // Должен быть в корне для DontDestroyOnLoad
        DontDestroyOnLoad(gameObject);
    }

    public async void Start()
    {
        if (Instance != this) return; // Дубликаты не стартуют

        m_Cts = new CancellationTokenSource();
        Landmarks = new float[k_NumKeypoints * 5];

        var devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("[CosmoPush] Камера не найдена!");
            return;
        }

        CamTexture = new WebCamTexture(devices[cameraIndex].name, requestedWidth, requestedHeight);
        CamTexture.Play();

        while (CamTexture.width < 100)
        {
            if (m_Cts.IsCancellationRequested) return;
            await Awaitable.NextFrameAsync();
        }

        // Инициализация моделей (ваш оригинальный код настройки)
        m_Anchors = BlazeUtils.LoadAnchors(anchorsCSV.text, k_NumAnchors);
        var poseDetectorModel = ModelLoader.Load(poseDetectorAsset);
        var graph = new FunctionalGraph();
        var input = graph.AddInput(poseDetectorModel, 0);
        var outputs = Functional.Forward(poseDetectorModel, input);
        var idx_scores_boxes = BlazeUtils.ArgMaxFiltering(outputs[0], outputs[1]);
        poseDetectorModel = graph.Compile(idx_scores_boxes.Item1, idx_scores_boxes.Item2, idx_scores_boxes.Item3);

        m_PoseDetectorWorker = new Worker(poseDetectorModel, BackendType.GPUCompute);
        m_PoseLandmarkerWorker = new Worker(ModelLoader.Load(poseLandmarkerAsset), BackendType.GPUCompute);

        m_DetectorInput = new Tensor<float>(new TensorShape(1, detectorInputSize, detectorInputSize, 3));
        m_LandmarkerInput = new Tensor<float>(new TensorShape(1, landmarkerInputSize, landmarkerInputSize, 3));

        // Основной цикл обработки
        while (!m_Cts.IsCancellationRequested)
        {
            try
            {
                // Additional guard: if CamTexture destroyed, break
                if (CamTexture == null)
                {
                    Debug.Log("[Webcam] CamTexture is null, breaking loop.");
                    break;
                }

                if (CamTexture.isPlaying && CamTexture.didUpdateThisFrame)
                {
                    await Detect(m_Cts.Token);
                }
                else
                {
                    await Awaitable.NextFrameAsync();
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Pose] Ошибка кадра: {ex.Message}");
                await Awaitable.NextFrameAsync();
            }
        }
    }

    async Awaitable Detect(CancellationToken ct)
    {
        // Guard against destroyed CamTexture
        if (CamTexture == null) return;

        m_TextureWidth = CamTexture.width;
        m_TextureHeight = CamTexture.height;
        var size = Mathf.Max(m_TextureWidth, m_TextureHeight);
        var scale = size / (float)detectorInputSize;
        var M = BlazeUtils.mul(BlazeUtils.TranslationMatrix(0.5f * (new Vector2(m_TextureWidth, m_TextureHeight) + new Vector2(-size, size))), BlazeUtils.ScaleMatrix(new Vector2(scale, -scale)));

        BlazeUtils.SampleImageAffine(CamTexture, m_DetectorInput, M);
        m_PoseDetectorWorker.SetInput(0, m_DetectorInput);
        m_PoseDetectorWorker.Schedule();

        var outIdxTensor = m_PoseDetectorWorker.PeekOutput(0) as Tensor<int>;
        var outScoreTensor = m_PoseDetectorWorker.PeekOutput(1) as Tensor<float>;
        var outBoxTensor = m_PoseDetectorWorker.PeekOutput(2) as Tensor<float>;

        if (outIdxTensor == null || outScoreTensor == null || outBoxTensor == null) return;

        using var outputIdx = await outIdxTensor.ReadbackAndCloneAsync();
        using var outputScore = await outScoreTensor.ReadbackAndCloneAsync();
        using var outputBox = await outBoxTensor.ReadbackAndCloneAsync();

        if (outputScore[0] < scoreThreshold) { IsTracking = false; return; }

        IsTracking = true;
        var idx = outputIdx[0];
        var anchorPosition = detectorInputSize * new float2(m_Anchors[idx, 0], m_Anchors[idx, 1]);
        var kp1 = BlazeUtils.mul(M, anchorPosition + new float2(outputBox[0, 0, 4], outputBox[0, 0, 5]));
        var kp2 = BlazeUtils.mul(M, anchorPosition + new float2(outputBox[0, 0, 6], outputBox[0, 0, 7]));
        var delta = kp2 - kp1;
        var radius = 1.25f * math.length(delta);
        var theta = math.atan2(delta.y, delta.x);
        var M2 = BlazeUtils.mul(BlazeUtils.mul(BlazeUtils.mul(BlazeUtils.TranslationMatrix(kp1), BlazeUtils.ScaleMatrix(new float2(radius / (0.5f * landmarkerInputSize), -radius / (0.5f * landmarkerInputSize)))), BlazeUtils.RotationMatrix(0.5f * Mathf.PI - theta)), BlazeUtils.TranslationMatrix(-new float2(0.5f * landmarkerInputSize, 0.5f * landmarkerInputSize)));

        BlazeUtils.SampleImageAffine(CamTexture, m_LandmarkerInput, M2);
        m_PoseLandmarkerWorker.SetInput(0, m_LandmarkerInput);
        m_PoseLandmarkerWorker.Schedule();

        var landmarkTensor = m_PoseLandmarkerWorker.PeekOutput("Identity") as Tensor<float>;
        if (landmarkTensor == null) return;

        using var landmarks = await landmarkTensor.ReadbackAndCloneAsync();
        if (landmarks == null) return;

        for (var i = 0; i < k_NumKeypoints; i++)
        {
            var p = BlazeUtils.mul(M2, new float2(landmarks[5 * i + 0], landmarks[5 * i + 1]));
            Landmarks[5 * i + 0] = p.x / m_TextureWidth;
            Landmarks[5 * i + 1] = p.y / m_TextureHeight;
            Landmarks[5 * i + 2] = landmarks[5 * i + 2];
            Landmarks[5 * i + 3] = landmarks[5 * i + 3];
        }
    }

    void OnDisable()
    {
        m_Cts?.Cancel();
        Dispose();
    }

    void Dispose()
    {
        m_PoseDetectorWorker?.Dispose();
        m_PoseLandmarkerWorker?.Dispose();
        m_DetectorInput?.Dispose();
        m_LandmarkerInput?.Dispose();
        if (CamTexture != null && CamTexture.isPlaying) CamTexture.Stop();
    }

    void OnDestroy() => Dispose();
}

using UnityEngine;
using System;

[RequireComponent(typeof(AudioSource))]
public class RhythmManager : MonoBehaviour
{
    [Header("Level 1 Beats (Original)")]
    public float[] level1Map = new float[] { 
        9.2f, 9.6f, 10.3f, 10.9f, 11.2f, 11.5f, 12.0f, 12.3f, 12.6f, 12.9f, 13.2f, 13.4f, 13.8f, 14.3f, 14.9f, 15.2f, 15.5f, 15.7f, 16.1f, 16.3f, 16.6f, 16.9f, 17.2f, 17.7f, 18.2f, 18.9f, 19.2f, 19.5f, 20.0f, 20.3f, 20.6f, 21.2f, 21.8f, 22.3f, 22.5f, 22.9f, 23.3f, 23.5f, 23.7f, 24.1f, 24.6f, 24.9f, 25.2f, 25.7f, 26.0f, 26.3f, 26.8f, 27.2f, 27.5f, 28.3f, 28.6f, 28.9f, 29.2f, 29.5f, 29.8f, 30.0f, 30.3f, 30.5f, 30.9f, 31.2f, 31.4f, 31.7f, 32.1f, 32.3f, 32.6f, 32.9f, 33.2f, 33.6f, 34.0f, 34.3f, 34.6f, 34.9f, 35.2f, 35.5f, 35.7f, 35.9f, 36.3f, 36.6f, 36.9f, 37.2f, 37.5f, 37.8f, 38.3f, 38.6f, 38.9f, 39.2f, 39.5f, 39.8f, 40.1f, 40.3f, 40.5f, 40.9f, 41.2f, 41.4f, 41.7f, 42.0f, 42.4f, 42.6f, 42.9f, 43.2f, 43.5f, 43.7f, 44.0f, 44.6f, 45.0f, 45.5f, 45.8f, 46.0f, 48.3f, 49.5f, 49.8f, 50.1f, 52.6f, 53.0f, 53.8f, 54.0f, 54.2f, 54.6f, 54.9f, 55.1f, 55.5f, 56.1f, 56.6f, 56.9f, 57.2f, 57.4f, 58.3f, 58.7f, 58.9f, 59.1f, 59.5f, 59.7f, 60.0f, 60.6f, 61.2f, 61.8f, 62.0f, 62.3f, 62.5f, 62.9f, 64.1f, 66.4f, 66.6f, 66.9f, 67.5f, 68.0f, 68.3f, 68.6f, 68.9f, 69.2f, 69.4f, 69.8f, 70.0f, 70.2f, 70.6f, 70.9f, 71.2f, 71.5f, 71.7f, 72.1f, 72.5f, 72.9f, 73.2f, 73.7f, 74.1f, 74.3f, 74.6f, 74.9f, 75.2f, 75.5f, 76.0f, 76.3f, 76.6f, 77.2f, 77.5f, 77.8f, 78.0f, 78.3f, 78.6f, 78.9f, 79.2f, 79.4f, 79.7f, 79.9f, 80.1f, 80.3f, 80.6f, 81.2f, 81.6f, 82.0f, 82.3f, 82.6f, 82.9f, 83.2f, 83.5f, 83.7f, 83.9f, 84.3f, 84.6f, 85.2f, 85.5f, 85.8f, 86.2f, 86.6f, 86.9f, 87.2f, 87.5f, 87.7f, 88.1f, 88.3f, 88.5f, 88.9f, 89.2f, 89.5f, 89.7f, 90.3f, 90.6f, 90.9f, 91.2f, 91.5f, 92.0f, 92.3f, 92.6f, 92.9f, 93.1f, 93.5f, 93.8f, 94.0f, 94.3f, 94.6f, 94.9f, 95.2f, 95.7f, 96.1f, 96.3f, 96.6f, 96.9f, 97.2f, 97.6f, 98.0f, 98.3f, 98.6f, 98.9f, 99.1f, 99.5f, 99.9f, 100.3f, 100.6f, 101.0f, 101.2f, 101.6f, 101.8f, 102.2f, 102.6f, 102.9f, 104.1f, 104.6f, 105.2f, 105.7f, 106.3f, 106.9f, 107.5f, 108.0f, 108.6f, 109.2f, 109.8f, 110.3f, 110.9f, 111.5f, 112.1f, 112.6f, 113.2f, 114.4f, 114.6f, 114.9f, 115.5f, 116.0f, 116.6f, 117.2f, 117.8f, 118.3f, 118.9f, 119.4f, 119.9f, 120.1f, 120.6f, 121.0f, 121.2f, 121.5f, 122.5f, 122.9f, 123.2f, 123.5f, 123.7f, 124.8f, 125.2f, 125.5f, 125.8f, 126.3f, 127.1f, 127.5f, 127.8f, 128.1f, 128.3f, 128.8f, 129.2f, 129.5f, 129.9f, 130.1f, 130.3f, 130.6f, 130.9f, 131.5f, 132.0f, 132.3f, 132.6f, 132.9f, 133.2f, 133.8f, 134.3f, 134.7f, 134.9f, 135.4f, 136.1f, 136.4f, 136.6f, 136.9f, 137.2f, 137.4f, 137.7f, 138.0f, 138.3f, 138.6f, 138.9f, 139.2f, 139.5f, 140.1f, 140.6f, 141.2f, 141.8f, 142.4f, 142.9f, 143.5f, 143.8f, 144.1f, 144.6f, 145.2f, 145.8f, 146.4f, 146.9f, 147.2f, 147.5f, 147.8f, 148.0f, 148.2f, 148.6f, 149.0f, 149.2f, 149.6f, 149.8f, 150.3f, 150.9f, 151.4f, 152.1f, 152.6f, 152.9f, 153.2f, 153.7f, 154.3f, 154.9f, 155.5f, 155.7f, 156.0f, 156.6f, 156.9f, 157.1f, 157.5f, 157.8f, 159.1f, 160.1f, 160.3f, 160.6f, 160.9f, 161.2f, 161.7f, 162.0f, 162.3f, 162.6f, 162.9f, 163.1f, 163.5f, 163.9f, 164.3f, 164.6f, 164.9f, 165.2f, 165.8f, 166.6f, 166.9f, 167.2f, 167.4f, 167.8f, 168.1f, 168.3f, 168.5f, 168.9f, 169.2f, 169.5f, 169.7f, 170.0f, 170.3f, 170.6f, 170.8f, 171.2f, 171.5f, 171.8f, 172.0f, 172.2f, 172.6f, 172.9f, 173.1f, 173.4f, 173.8f, 174.3f, 174.5f, 174.9f, 175.4f, 175.8f, 176.1f, 176.3f, 176.5f, 176.9f, 177.2f, 177.4f, 177.6f, 178.1f, 178.3f, 178.9f, 179.2f, 179.5f, 179.7f, 179.9f, 180.2f, 180.5f, 180.9f, 181.2f, 181.4f, 181.8f, 182.0f, 182.2f, 182.6f, 182.9f, 183.2f, 183.4f, 183.8f, 184.1f, 184.3f, 184.6f, 184.9f, 185.2f, 185.5f, 185.7f, 186.3f, 186.8f, 187.2f, 187.5f, 187.8f, 188.0f, 188.3f, 188.6f, 188.9f, 189.1f, 189.8f, 190.1f, 190.3f, 190.6f, 190.9f, 191.2f, 191.4f, 191.7f, 192.1f, 192.4f, 192.6f, 192.9f, 193.2f, 193.5f, 193.9f, 194.3f, 194.6f, 194.8f, 195.5f, 195.9f, 196.2f, 196.6f, 203.5f, 204.3f, 204.6f, 205.2f, 205.5f, 205.8f, 213.1f, 214.6f, 214.8f
    };

    [Header("Level 2 Beats (66.6s trek2 - BASS KICKS)")]
    public float[] level2Map = new float[] {
        5.53f, 6.69f, 7.15f, 8.99f, 9.43f, 9.85f, 10.05f, 10.24f, 10.61f, 11.01f, 11.40f, 11.59f, 12.24f, 12.59f, 12.98f, 13.77f, 14.14f, 14.54f, 14.79f, 15.12f, 15.30f, 15.70f, 15.98f, 16.49f, 16.88f, 17.11f, 17.30f, 18.07f, 18.48f, 19.64f, 19.83f, 20.64f, 20.83f, 21.20f, 21.59f, 22.01f, 22.38f, 22.78f, 23.20f, 23.55f, 24.36f, 24.59f, 24.78f, 25.54f, 26.01f, 26.38f, 26.59f, 27.89f, 28.28f, 28.65f, 29.05f, 29.26f, 29.63f, 29.84f, 30.23f, 30.63f, 31.02f, 31.42f, 32.28f, 32.60f, 33.37f, 33.76f, 34.32f, 34.53f, 34.95f, 35.34f, 35.55f, 35.83f, 36.13f, 36.50f, 36.90f, 37.29f, 37.50f, 38.06f, 38.29f, 38.48f, 38.87f, 39.26f, 39.66f, 41.01f, 42.00f, 42.19f, 42.42f, 42.77f, 43.19f, 44.35f, 45.53f, 45.74f, 45.95f, 46.14f, 46.51f, 46.72f, 47.51f, 47.72f, 48.00f, 49.06f, 49.83f, 50.20f, 50.99f, 51.41f, 52.52f, 52.71f, 53.78f, 54.13f, 54.57f, 54.78f, 55.01f, 55.19f, 55.61f, 56.12f, 56.56f, 57.14f, 57.40f, 57.66f, 57.91f, 58.47f, 58.65f, 58.86f, 59.26f, 59.63f, 60.60f, 60.81f, 61.02f, 61.44f, 61.63f, 61.81f, 62.00f, 62.42f, 62.81f, 63.00f, 63.48f, 64.16f, 64.34f, 64.76f, 65.18f
    };

    [Header("Modular Level Settings (v136.6)")]
    [Tooltip("Включите это для новых уровней, чтобы использовать свой массив битов и музыку")]
    public bool useCustomMap = false;
    public float[] customMap;

    [HideInInspector] public float[] beatMap; // Текущая активная карта

    public float bpm = 100f; 
    public float beatOffset = 0.05f; 
    public event Action<bool> OnBeat; 

    public double DspTimeOfLastBeat { get; private set; }
    public bool CurrentExpectedStateDown { get; private set; } = true;

    private AudioSource _audioSource;
    private double _nextTickTime; 
    private bool _running = true;
    private float _speedupTimer = 0f;
    private float _speedupDuration = 1f;
    private int _currentBeatIndex = 0; 

    private float _postSongTime = 0f;
    public float SongTime => _audioSource.isPlaying ? _audioSource.time : (SongDuration + _postSongTime);
    public float SongDuration => (_audioSource.clip != null) ? _audioSource.clip.length : 0f;
    public bool IsPlaying => _audioSource.isPlaying;
    public bool IsLevel2 { get; private set; } // v136.22: Флаг для спавнера

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        IsLevel2 = sceneName.ToLower().Contains("level2") || sceneName.ToLower().Contains("level 2");
        Debug.Log($"<color=white>[Rhythm Init] Scene detected: {sceneName} | IsLevel2: {IsLevel2}</color>");

        _audioSource.Stop();

        // 🪲 BUGFIX #17: Модульная система ритмов (v136.6)
        if (useCustomMap && customMap != null && customMap.Length > 0)
        {
            beatMap = customMap;
            Debug.Log("<color=magenta>[Rhythm] Loading CUSTOM Modular Data for New Level!</color>");
        }
        else
        {
            // Старая логика для Уровня 1 и 2 (Обратная совместимость)
            bool isLevel2 = sceneName.ToLower().Contains("level2") || sceneName.ToLower().Contains("level 2");

            if (isLevel2)
            {
                beatMap = level2Map;
                _audioSource.clip = Resources.Load<AudioClip>("trek2");
                Debug.Log("<color=yellow>[Rhythm] Loading Level 2 Data (trek2)</color>");
            }
            else
            {
                beatMap = level1Map;
                _audioSource.clip = Resources.Load<AudioClip>("CustomGameMusic");
                Debug.Log("<color=yellow>[Rhythm] Loading Level 1 Data (CustomGameMusic)</color>");
            }
        }

        if (_audioSource.clip != null)
        {
            _audioSource.loop = false;
            _audioSource.playOnAwake = false; // Отключаем автостарт, чтобы не было наложения
            _audioSource.Play();
            Debug.Log($"<color=green>[Rhythm SUCCESS] Playing: {_audioSource.clip.name} ({_audioSource.clip.length}s)</color>");
        }
        else
        {
             Debug.LogError($"[Rhythm ERROR] Clip not found in Resources! CustomMapEnabled: {useCustomMap}");
        }

        _nextTickTime = AudioSettings.dspTime + 0.5 + beatOffset; 
    }

    public void TriggerSpeedup(float duration, float multiplier = 2f)
    {
        _speedupDuration = duration;
        _speedupTimer = duration;
        if (_audioSource != null) _audioSource.pitch = multiplier;
    }

    public void SkipToTime(float targetTime)
    {
        if (_audioSource == null || _audioSource.clip == null) return;
        _audioSource.time = Mathf.Clamp(targetTime, 0f, _audioSource.clip.length);
        _nextTickTime = AudioSettings.dspTime; 
        
        _currentBeatIndex = 0;
        while (_currentBeatIndex < beatMap.Length && beatMap[_currentBeatIndex] < targetTime) {
            _currentBeatIndex++;
        }
    }

    public float GetSpeedupProgress()
    {
        if (_speedupTimer <= 0) return 0f;
        return Mathf.Clamp01(_speedupTimer / _speedupDuration);
    }

    void Update()
    {
        if (!_running) return;

        // Отсчет времени после завершения трека
        if (!_audioSource.isPlaying) _postSongTime += Time.deltaTime;

        // Обработка рывков (speedup)
        if (_speedupTimer > 0) _speedupTimer -= Time.unscaledDeltaTime;
        else if (_audioSource != null && _audioSource.pitch > 1.0f)
            _audioSource.pitch = Mathf.MoveTowards(_audioSource.pitch, 1.0f, Time.unscaledDeltaTime * 1.5f);

        // ОБРАБОТКА БИТОВ (Синхронизация по beatMap)
        if (beatMap != null && _currentBeatIndex < beatMap.Length)
        {
            if (SongTime >= beatMap[_currentBeatIndex])
            {
                DspTimeOfLastBeat = AudioSettings.dspTime;
                CurrentExpectedStateDown = !CurrentExpectedStateDown;
                OnBeat?.Invoke(CurrentExpectedStateDown);
                
                // v136.21: Визуальный контроль битов в консоли
                Debug.Log($"<color=cyan>[Rhythm Beat]</color> Time: {SongTime:F2}s | Index: {_currentBeatIndex}");
                
                _currentBeatIndex++;
            }
        }
    }
}

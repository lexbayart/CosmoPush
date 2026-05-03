**COSMO PUSH**

Game Design Document

Version 4.1 | Unity 6.3 LTS (6000.3.11f1) | Android 8.0+ / iOS 14+ | 2026

Design Document — Integrated with v4.0 prototype code

# **Правила ведения этого документа**

1. Документ описывает игру в настоящем времени.
2. Никаких маркеров новизны в тексте. Документ — готовая спецификация, не журнал правок.
3. Версия документа — только в шапке.
4. Сравнения с другими проектами — не в тексте разделов.
5. Пометки «без изменений» запрещены.
6. История изменений — отдельно в changelog или VCS.

# **1. Обзор игры**

|**Параметр**|**Значение**|
| :- | :- |
|Название|Cosmo Push (рабочее название)|
|Жанр|Ритм-аркада / Фитнес-игра|
|Платформа|Android 8.0+ / iOS 14+|
|Движок|Unity 6.3 LTS (6000.3.11f1)|
|Ориентация|Landscape (горизонтально)|
|Трекинг|**WebcamPoseDetection.cs** — единый трекер на обе платформы через Unity Sentis + BlazePose. ARKit Body Tracking не реализован.|
|Основная точка управления|Нос (#0) — микросмещение через `PlayerCharacter.Update()` с Mathf.SmoothDamp|
|Верификатор отжимания|Плечи (#11/#12) — `PushupVerifier.Update()` по порогу Y (0.5 фильтрованных)|
|Уровни персонажа|**2 дорожки** (HIGH / LOW). MID определена в `LaneSystem.cs` но принудительно скрыта.|
|Механика движения|Нос — микросмещение ±60px. Смена дорожки — фильтрованная Y плеч > 0.5 (HIGH) или < 0.5 (LOW).|
|Размещение телефона|Стоит на ребре в landscape, прислонён под углом 30–45° к стене/подставке лицевым экраном к игроку. 50–80 см от лица игрока.|
|Камера|Фронтальная камера телефона. Задняя камера не используется.|
|Рендер-пайплайн|Universal Render Pipeline (URP).|
|Режимы сложности|**Заглушка:** Dropdown Easy / Normal / Hard (`DifficultySelector.cs`). Карточки Колени/Носки не реализованы.|
|Количество уровней|2 реализованных уровня (Level2.unity + WarmUpLevel-заглушка). Level3 не создан.|

Игрок кладёт телефон на пол в landscape, ложится в упор лёжа лицом к экрану. Камера отслеживает нос (для микросмещения персонажа) и плечи (для смены дорожки). Космонавт в киберпанк-скафандре летит в невесомости. По пути летят астероиды, космический мусор и печенья. Уклонение двухуровневое: лёгкое смещение носом внутри дорожки, полная смена дорожки — через изменение положения плеч.

## **1.1 Правила дизайна — неизменяемые**

**⚠ ПРАВИЛО: Единственные режимы сложности — «Колени» и «Носки». Никаких других вариантов (отжимания от стены, сидя, стоя и т.д.) не существует и не добавляется.**

**⚠ ПРАВИЛО: Коммуникация с игроком во время игры — только через звук и визуал. Тактильная обратная связь (вибрация телефона) не используется: телефон лежит на полу и игрок его не держит.**

**⚠ ПРАВИЛО: Смена дорожки возможна исключительно через верифицированное отжимание. Кивок головой, имитация движения без тела — игнорируются без штрафа.**

## **1.2 Лабораторное допущение — стабильность телефона**

Данный документ рассматривает лабораторный случай: телефон установлен надёжно на полу и не подвергается внешним механическим воздействиям в ходе игровой сессии. Любое внешнее воздействие на телефон во время игры выходит за рамки данной спецификации.

# **2. Игровой мир и визуальная концепция**

## **2.1 Сеттинг**

Открытый космос, киберпанк-эстетика. Тёмный фон со звёздами, туманностями и далёкими планетами (parallax, 3–4 слоя). Каждый уровень — отдельный визуальный трек: уровень 1 (CustomGameMusic, ~112 сек) и уровень 2 (trek2, ~66 сек). Фон реализован через `BackgroundStoneManager.cs` — процедурная генерация камней/звёзд, движущихся влево с эффектом параллакса.

## **2.2 Персонаж**

Космонавт в детализированном киберпанк-скафандре. Вид сбоку (профиль). Персонаж зафиксирован в левой трети экрана — движется по вертикали. Реализован через `PlayerCharacter.cs` с поддержкой:
- Микросмещения носом (±60px от базовой позиции дорожки)
- Физики растяжения (stretch/squash при движении)
- Пульсации на каждый бит
- Автоматического притяжения к цели (космическая станция) в финале уровня

|**Скин**|**Описание**|**Разблокировка**|
| :- | :- | :- |
|Космонавт|Киберпанк-скафандр, неоновые акценты|Бесплатно (текущий — спрайт `Skins/{skinName}`)|
|Инопланетянин|Серый, большие глаза, антенны|3 000 очков (не реализовано)|
|Кот|Котик в гермошлеме, хвост из скафандра|5 000 очков (не реализовано)|
|Рыба|Рыбка в шарообразном шлеме-аквариуме|8 000 очков (не реализовано)|

## **2.3 Объекты в пространстве**

|**Объект**|**Внешний вид**|**Эффект**|
| :- | :- | :- |
|Препятствие (Obstacle)|Случайный спрайт из `Resources/Emojis/Obstacles/`, 300×300, вращается|-1 кислород при столкновении|
|Астероид (Asteroid)|Спрайт `emoji_u2604` (комета), 300×300, летит по диагонали|-1 кислород при столкновении, оранжевый шлейф|
|Печенье (Cookie)|Случайный спрайт из `Resources/Emojis/Cookies/`, 120×120|+10 очков, эффект частиц|
|Звезда-ускорение (Speedup)|Спрайт `emoji_u2b50`, 150×150, вращается|×1.5 ускорение музыки на 1 сек|
|Спутник-цель (Goal)|Спрайт `Resources/Satellite`, 800×800|Финал уровня|

# **3. Механика дорожек**

## **3.1 Принцип**

**🟢 Реализовано (v4.0 prototype)**

Экран разделён на **две** горизонтальных дорожки. Персонаж всегда принадлежит одной из них, но внутри дорожки свободно плавает по Y, следуя за носом игрока (±60px). Смена дорожки — когда фильтрованная Y-позиция плеч пересекает порог 0.5.

Третья дорожка (MID) существует в `LaneSystem.cs` как Image-компонент (строка 12), но принудительно скрыта в `LaneSystem.Update()`:
```csharp
// LaneSystem.cs — MID принудительно скрыта
if (MidLine != null && MidLine.gameObject.activeSelf)
    MidLine.gameObject.SetActive(false);
```

|**Дорожка**|**Y на экране**|**Цвет полосы**|
| :- | :- | :- |
|HIGH|135|#00FFCC, alpha 20% (общий цвет для обеих линий)|
|LOW|-270|#00FFCC, alpha 20%|

Текущие значения позиций линий (HighY=135, LowY=-270) жёстко заданы в инспекторе `LaneSystem` и сдвинуты относительно сетки UI. Калибровка по камере/телу не реализована.

**🔜 План (GDD v4.0):** В будущем планируется 3 дорожки (HIGH/MID/LOW) с полной системой верификации. `LaneSystem.GetLaneY()` должен возвращать Y для 3 lane-индексов, спавнер должен учитывать 3 дорожки.

## **3.2 Двухслойная система управления**

**Слой 1 — Микродвижение носом (мгновенно, без смены дорожки):**

**🟢 Реализовано (v4.0 prototype)**
- Нос игрока двигает персонажа на `noseMicroRange = 60px` через `PlayerCharacter.Update()`
- Работает через `WebcamPoseDetection.GetNose()` → нормализованные координаты (0..1) → центрирование и SmoothDamp
- Не меняет дорожку, только создаёт визуальный отклик
- **Kalman-фильтрация не реализована.** Используется `Mathf.SmoothDamp` с параметром сглаживания `smoothTime = 0.1f`

**Слой 2 — Смена дорожки (через положение плеч):**

**🟢 Реализовано (v4.0 prototype)**
- `PushupVerifier.Update()` получает Y левого (индекс 11) и правого (индекс 12) плеча через `IBodyTracker.GetKeypointY()`
- Усредняет → фильтрует через `Mathf.Lerp` с `Time.deltaTime * 15f` → сравнивает с порогом 0.5
- Смена дорожки происходит мгновенно, **без временно́го гейта**
- **Elbow-верификация не реализована:** локти (#13/#14) не используются в коде
- **Temporal gating 150-200 мс не реализован**
- **Nose-вектор как подтверждение направления не реализован**
- **Гистерезис не реализован**

```csharp
// PushupVerifier.cs — текущая фильтрация плеч
_filteredSY = Mathf.Lerp(_filteredSY, rawSY, Time.deltaTime * 15f);
```

**Детекция читерства (Cheat Detection):**

**🟢 Реализовано (v4.0 prototype)**
Реализована в `PushupVerifier.CheckIsCheating()` через Z-координату бёдер (индексы 23/24) относительно плеч (11/12):
- Если `zDelta < 80` — игрок стоит, а не в упоре: сообщение "Вы стоите. ПРИМИТЕ УПОР ЛЁЖА!"
- Если `zDelta > 260` — игрок слишком высоко: сообщение "СЛИШКОМ ВЫСОКО! ОТЖИМАЙТЕСЬ ОТ ПОЛА!"
- При читерстве логика отжиманий блокируется (return в Update)

## **3.3 Подсчёт отжиманий**

**🟢 Реализовано (v4.0 prototype)**

Реализован конечный автомат с двумя фазами (`Phase.Up` / `Phase.Down`) и cooldown-таймером 0.5 сек:
- `Phase.Up` + дорожка LOW → переход в `Phase.Down`
- `Phase.Down` + дорожка HIGH → отжимание засчитано
- **Счётчик отжиманий** — целое число, отображается в HUD через событие `OnPushupCounted`

```csharp
// PushupVerifier.cs — конечный автомат отжиманий
enum Phase { Up, Down, Cooldown }
Phase _currentPhase = Phase.Up;
float _cooldownTimer = 0f;
const float COOLDOWN_TIME = 0.5f;
```

## **3.4 Логика препятствий на дорожках**

**🟢 Реализовано (v4.0 prototype)**

Реализована в `LevelSpawner.cs` (316 строк):
- **Level 1:** Спавн по beatMap level1Map (хардкоженный массив float[] временных меток). Чередование дорожек для печений. Препятствия (Obstacle) спавнятся по таймеру (2-3 сек). Астероиды — по таймеру 10-18 сек.
- **Level 2:** Строгий спавн на каждый 4-й бит. Препятствия гарантированно на биты, печенья группами между ними. Проверка на перекрытие по времени (`IsAmbientOverlapping`).
- **Speedup (звёзды):** спавнятся случайно каждые 4-6 сек.
- **Цель (спутник):** спавнится за 0.2 сек до конца трека на дорожке 0.
- **Статические счётчики:** `TotalCookiesSpawned`, `TotalStarsSpawned` — для звёздного рейтинга на экране успеха.
- **Двойные препятствия с интервалом < 1 сек:** не поддержаны спавнером.

```csharp
// LevelSpawner.cs — основные параметры
float scrollSpeed = 450f;
float spawnXPosition = 2000f;
float playerXPosition = 300f;
```

# **4. Система трекинга**

## **4.1 Текущая реализация — WebcamPoseDetection (BlazePose + Sentis)**

**🟢 Реализовано (v4.0 prototype)**

Вместо платформенного разделения через TrackerFactory, в игре реализован единый трекер `WebcamPoseDetection.cs` (213 строк), работающий на обеих платформах через Unity Sentis и BlazePose:

```csharp
// WebcamPoseDetection.cs — ключевые моменты
public class WebcamPoseDetection : MonoBehaviour, IBodyTracker
{
    // Две модели: poseDetector (детектор региона), poseLandmarker (ключевые точки)
    public ModelAsset poseDetectorAsset;
    public ModelAsset poseLandmarkerAsset;
    public TextAsset anchorsCSV;
    
    // BlazePose Full, 33 keypoints, вход 224×224 (детектор) / 256×256 (ландмаркер)
    // Использует FunctionalGraph для компиляции детектора с anchor-фильтрацией
    
    // DontDestroyOnLoad — не уничтожается при смене сцены
    // async Awaitable — асинхронный цикл считывания кадров
    
    // Реализация IBodyTracker:
    float2 GetNose() → Landmarks[0] / Landmarks[1]
    float2 GetShoulderMidpoint() → среднее Landmarks[11] и Landmarks[12]
    float GetKeypointY(int index) → Landmarks[5 * index + 1]
    float GetKeypointVisibility(int index) → Landmarks[5 * index + 3]
    float GetKeypointZ(int index) → Landmarks[5 * index + 2]
}
```

**Ключевые особенности:**
- Использует `Unity.InferenceEngine` и `FunctionalGraph` — API более ранних версий Sentis (до 2.0)
- Асинхронный цикл `Detect()` через `Awaitable.NextFrameAsync()`
- Нет разделения на ARKitBodyTracker / SentisBodyTracker — обе константы сборки (COSMO_ARKIT / COSMO_SENTIS) не заданы
- `IBodyTracker.cs` существует как интерфейс (20 строк), но не используется через TrackerFactory
- `CameraPreviewHandler.cs` (133 строк) — отображает камеру на RawImage с поворотом/отражением и AspectRatioFitter. Создаёт космический фон (SpaceBackground) с мерцающими звёздами (`CosmicSparkle` класс в том же файле).

## **4.2 Платформенная архитектура (план)**

|**Условие**|**Реализация**|
| :- | :- |
|iOS, Apple A12+|**НЕ РЕАЛИЗОВАНО.** Build Profile для iOS не создан.|
|Android / iOS < A12|WebcamPoseDetection через Unity Sentis (текущий)|
|Fallback (ошибка)|Возвращает нулевые данные, игра не стартует|

**🔜 План (GDD v4.0):** TrackerFactory → ARKitBodyTracker (iOS) / SentisBodyTracker (Android). `TrackerFactory.cs` не существует в проекте.

## **4.3 iOS — AR Foundation 6 / ARKit Body Tracking**

**🔜 План:** Build Profile для iOS не создан. Пакет AR Foundation 6 не подключен.

## **4.4 Android — Unity Sentis / BlazePose Full**

**🟢 Реализовано (v4.0 prototype)**

Текущий трекер (`WebcamPoseDetection.cs`) реализует BlazePose Full через Unity Sentis. Использует две модели:

1. **Pose Detector** — определяет bounding box тела на кадре (224×224 вход, 2254 anchor box через `BlazeUtils.LoadAnchors()`)
2. **Pose Landmarker** — вычисляет 33 ключевые точки в найденном регионе (256×256 вход)

Процесс детекции:
```
Детектор: SampleImageAffine(camera, 224×224) → PoseDetector.Schedule() → 
ArgMaxFiltering → выбор лучшего anchor → расчёт bounding box → 
Ландмаркер: SampleImageAffine(crop, 256×256) → PoseLandmarker.Schedule() →
Readback → нормализация в Landmarks[5*33]
```

**Используемые индексы (MediaPipe BlazePose 33-point):**
|**Индекс**|**Точка**|**Использование**|
| :- | :- | :- |
|#0|Нос|Микросмещение персонажа (PlayerCharacter)|
|#11|Левое плечо|Смена дорожки (PushupVerifier)|
|#12|Правое плечо|Смена дорожки (PushupVerifier)|
|#23|Левое бедро|Z-детекция читерства|
|#24|Правое бедро|Z-детекция читерства|
|Остальные 28|—|В игровой логике не используются|

## **4.5 Проверка освещения**

**🔜 План (GDD v4.0):** `LightCheck.cs` существует как пустая заглушка. Метод `Start()` пуст.

## **4.6 ReadyPlayScreen**

**🔜 План (GDD v4.0):** Не реализован. После MainMenu → WarmUpLevel (заглушка) → Level2.

## **4.7 Фильтрация данных трекинга**

**🟢 Реализовано (v4.0 prototype) — базовая фильтрация:**

В `PushupVerifier.cs`:
```csharp
_filteredSY = Mathf.Lerp(_filteredSY, rawSY, Time.deltaTime * 15f); // Экспоненциальное сглаживание
BodyVelocityY = Mathf.SmoothDamp(BodyVelocityY, instantVelocity, ref _velocityFilter, 0.1f); // Скорость
```

В `PlayerCharacter.cs`:
```csharp
_noseOffset = Mathf.SmoothDamp(_noseOffset, targetNoseOffset, ref _noseVelocity, 0.1f); // Микросмещение носа
```

**🔜 План (GDD v4.0):**
- `KalmanFilterJob [BurstCompile] IJob` — не существует в проекте. Файл `KalmanFilterJob.cs` отсутствует.
- Low-pass фильтр 2 Hz для дыхания — не реализован
- `NoseLevelMapper.cs` — не существует в проекте
- `AdaptiveCalibration.cs` — не существует в проекте

# **5. Ритм-система**

## **5.1 Текущая реализация (RhythmManager.cs)**

**🟢 Реализовано (v4.0 prototype)**

Использует `AudioSource.time` для синхронизации с предварительно размеченными beat-картами (149 строк):

```csharp
// RhythmManager — ключевые моменты
public float[] beatMap; // Хардкоженный массив float временных меток (секунды)
// level1Map — ~190 бит, трек CustomGameMusic (~112 сек)
// level2Map — ~110 бит, трек trek2 (~66.6 сек)

// SongTime = _audioSource.time (если играет) или SongDuration + _postSongTime (после конца)
// IsLevel2 — по имени сцены: sceneName.ToLower().Contains("level2")
// OnBeat(bool isDown) — событие при совпадении SongTime с beatMap[i]
// TriggerSpeedup(duration, speed) — ускорение AudioSource.pitch

// В Update():
if (SongTime >= beatMap[currentBeatIndex])
{
    OnBeat?.Invoke(CurrentExpectedStateDown);
    currentBeatIndex++;
}
```

**Отличия от DSP-clock спецификации:**
- Не использует `AudioSettings.dspTime` — синхронизация по `AudioSource.time` (может рассинхронизироваться на длинных треках)
- Нет `OnHalfBeat` / `OnBar` событий — только `OnBeat(bool isDown)`
- BeatMap — хардкоженные `float[]` в скрипте, не `LevelData ScriptableObject`
- Нет поддержки BPM-параметра

## **5.2 Визуальная синхронизация с ритмом**

**🟢 Реализовано (v4.0 prototype):**

1. **Пульсация персонажа** — `PlayerCharacter.PulseCoroutine()` (×1.15 на каждый OnBeat, 0.1 сек)
2. **Пульсация индикатора кислорода** — `OxygenIndicator.PulseCoroutine()` (×1.2, при 1 жизни ×1.5)
3. **Ритм-рейтинг (RateRhythmHit)** — оценка совпадения отжимания с ближайшим битом:
   - PERFECT: < 0.2 сек
   - GOOD: < 0.5 сек
   - OK: < 1.0 сек
   - MISS: > 1.0 сек

**🔜 План (GDD v4.0):**
- Пульсация виньетки Post Processing Volume — не реализована
- OnBeat пульс дорожек VFX Graph — не реализован
- 3D Audio stereoPan по дорожкам — не реализован
- Дыхание персонажа (покачивание в ритм BPM) — не реализовано

## **5.3 Треки уровней**

|**Уровень**|**Трек**|**Длительность**|**BPM (приблизительно)**|**Статус**|
| :- | :- | :- | :- | :- |
|1 — Пояс астероидов|CustomGameMusic|~112 сек|100|Реализован как Level2.unity|
|2 — Туманность|trek2|~66.6 сек|~105|Реализован как Level2.unity (вторая половина beatMap)|
|3 — Ледяная орбита|—|—|—|**Не создан**|

Фактически: основной геймплейный уровень — `Level2.unity`, загружаемый через `SceneManager.LoadScene("Level2")`. `WarmUpLevel.unity` — пустая сцена-переходник. Отдельный Level1.unity или Level3.unity не существуют.

# **6. Счёт, жизни и прогрессия**

## **6.1 Кислород как жизни**

**🟢 Реализовано (v4.0 prototype)**

|**Событие**|**Эффект**|
| :- | :- |
|Столкновение с препятствием / астероидом|-1 кислород. Красная вспышка (`JuiceManager.Flash`). Тряска экрана (`JuiceManager.Shake`).|
|0 кислорода|Game Over: панель через `PushupUIHandler.ShowGameOver()` с кнопками Restart|
|Старт уровня|Кислород = 5|

Кислород не сохраняется между уровнями — каждый раз сбрасывается до 5.

Счётчики ударов:
- `PlayerCollision.AsteroidHitsTaken` (static) — удары кометой
- `PlayerCollision.RockHitsTaken` (static) — удары камнями
- `PushupVerifier.TotalHitsTaken` — общее кол-во

## **6.2 Очки — печенья**

**🟢 Реализовано (v4.0 prototype) — базовая система:**

|**Параметр**|**Значение**|
| :- | :- |
|Очки за одно печенье|+10 (вызов `verifier.AddExternalScore(10)` в `PlayerCollision.HandleCookieCollect()`)|
|Комбо-множитель|**НЕ РЕАЛИЗОВАН** — ×1→×2→×3 отсутствует|
|Звёзды уровня|Подсчёт `CookiesCollected` / `StarsCollected` ведётся, отображаются звёзды ★☆☆☆☆ на экране успеха|
|Итог уровня|Звёздный рейтинг на экране успеха (5 критериев: ≥50% печений, 0 hits астероидами, 0 hits камнями, ≥10 отжиманий, все звёзды собраны)|

**🔜 План (GDD v4.0):** Комбо-множитель ×1→×2→×3 на основе последовательного сбора печений без пропусков.

## **6.3 Счётчик отжиманий**

**🟢 Реализовано (v4.0 prototype)**

`PushupCounter` интегрирован в `PushupVerifier`:
- LOW → HIGH → LOW цикл с cooldown 0.5 сек
- Счётчик отображается в HUD (`counterText`)
- При каждом отжимании вызывается `RateRhythmHit()` — оценка синхронизации с битом

## **6.4 Длина сессии и целевое количество отжиманий**

Текущие треки:
- Уровень 1 (CustomGameMusic): ~112 секунд, beatMap ~190 отметок времени
- Уровень 2 (trek2): ~66.6 секунд, beatMap ~110 отметок времени

Количество отжиманий за уровень зависит от расположения препятствий в спавнере.

# **7. Near-Miss и асимметрия**

## **7.1 Текущий статус**

**🟡 Частично: заглушка**

`AsymmetrySystem.cs` — пустая заглушка (10 строк). Near-miss механика не реализована. Смещение персонажа по X при асимметричных отжиманиях отсутствует.

## **7.2 Spec (к реализации)**

**🔜 План (GDD v4.0):**

AsymmetrySystem будет анализировать разницу Y между левым и правым плечом. При разнице > 6% высоты кадра — персонаж кренится по X в сторону более высокого плеча. Near-miss никогда не отнимает жизнь, только создаёт визуальный эффект.

# **8. Бонус за симметричные отжимания**

## **8.1 Текущий статус**

**🟡 Частично: заглушка**

`SymmetryBonusTracker.cs` — пустая заглушка (10 строк). Щит космонавта (3 симметричных отжимания) и бонус Perfect Symmetry не реализованы.

## **8.2 Spec (к реализации)**

**🔜 План (GDD v4.0):**
- 3 подряд симметричных отжимания → неоновый щит вокруг скафандра
- Щит поглощает одно столкновение
- Perfect Symmetry бонус за уровень — все отжимания < 4% асимметрии

# **9. VisibilityGuard — выход из кадра**

## **9.1 Текущая реализация**

**🟢 Реализовано (v4.0 prototype) — встроен в PushupVerifier:**

VisibilityGuard встроен в `PushupVerifier.HandleVisibilityGuard()`:

```csharp
void HandleVisibilityGuard()
{
    bool visible = poseDetection.IsTracking && poseDetection.GetKeypointVisibility(0) > 0.5f;
    
    if (!visible) {
        _outOfFrameTimer += Time.unscaledDeltaTime;
        float dropThreshold = _hasTrackedOnce ? 1.5f : 3.0f; // Первый раз 3 сек на загрузку сети
        
        if (_outOfFrameTimer > dropThreshold) {
            PauseGameObjects(); // Time.timeScale = 0
            PauseMusic();       // AudioListener.pause = true
        }
    } else {
        _outOfFrameTimer = 0f;
        _resumeTimer += Time.unscaledDeltaTime;
        if (_resumeTimer > 3f) {  // 3 сек после возврата в кадр
            ResumeGameObjects();
            ResumeMusic();
        }
    }
}
```

**ВАЖНО:** При выходе из кадра игра ставится на **паузу**, а не заканчивается. Таймер на LevelIncomplete (10 сек по GDD) — **НЕ РЕАЛИЗОВАН**.

Отдельный класс `VisibilityGuard.cs` существует, но является пустой заглушкой.

**🔜 План (GDD v4.0):** VisibilityGuard как отдельный класс с 1.5с паузой → 10с → EndLevelIncomplete. `VisibilityGuard.cs` — пустой файл, функциональность встроена в `PushupVerifier`.

# **10. WarmUpLevel**

## **10.1 Текущая реализация (заглушка)**

**🟡 Частично: заглушка**

```csharp
public class WarmUpLevel : MonoBehaviour
{
    public bool Completed { get; private set; } = false;
    
    void Awake()
    {
        Completed = true;  // Мгновенно помечается как пройденный
        Invoke(nameof(LoadMainLevel), 0.5f);  // Через 0.5 сек загружает Level2
    }
    
    void LoadMainLevel() => SceneManager.LoadScene("Level2");
}
```

После нажатия "Start Game" в MainMenu → загружается WarmUpLevel → через 0.5 сек → Level2. WarmUpLevel существует как сцена, но не содержит игрового процесса, калибровки или обучения.

## **10.2 Spec (к реализации)**

**🔜 План (GDD v4.0):** Полноценный уровень-разогрев:
- Только печенья (без астероидов)
- Шар на LOW → "Опустись — поймай!" → фиксация minY носа
- Шар на HIGH → "Поднимись — поймай!" → фиксация maxY носа
- Первый шар всегда MID (нос игрока в покое)
- Мягкий режим PushupVerifier (пониженные пороги на 40%)
- Полное игровое окружение (фон, музыка)

# **11. Экраны и состояния игры**

## **11.1 Текущие экраны**

|**Состояние**|**Статус**|**Описание реализации**|
| :- | :- | :- |
|**ConsentScreen**|**НЕ РЕАЛИЗОВАНО**|—|
|**MainMenu**|**РЕАЛИЗОВАНО**|`MainMenu.cs` (89 строк) через `MonoBehaviour.OnGUI()` — 5 кнопок + Music/SFX toggles|
|**LevelSelect**|**НЕ РЕАЛИЗОВАНО**|Start Game ведёт сразу в "Level2"|
|**DifficultySelect**|**ЗАГЛУШКА**|`DifficultySelector.cs` (24 строки) — Dropdown Easy/Normal/Hard|
|**ReadyPlayScreen**|**НЕ РЕАЛИЗОВАНО**|—|
|**WarmUpLevel**|**ЗАГЛУШКА**|Мгновенный переход в Level2|
|**Countdown**|**НЕ РЕАЛИЗОВАНО**|—|
|**Playing**|**РЕАЛИЗОВАНО**|2 уровня, HUD, спавн, коллизии, пауза|
|**OutOfFrame**|**РЕАЛИЗОВАНО**|Встроен в PushupVerifier, пауза вместо|**LevelIncomplete** (пауза при выходе из кадра), 10s таймер не реализован|
|**Paused**|**РЕАЛИЗОВАНО**|`PauseManager.cs` (31 строка) — Escape → Time.timeScale = 0|
|**LevelComplete**|**РЕАЛИЗОВАНО ЧАСТИЧНО**|`PushupUIHandler.ShowSuccessPanel()` — панель с миссия-комплит, звёздный рейтинг, кнопки Restart / Next Level. Нет личного рекорда, нет Perfect Symmetry бонуса.|
|**GameOver**|**РЕАЛИЗОВАНО**|`PushupUIHandler.ShowGameOver()` — панель с кнопкой Restart.|
|**SkinShop**|**ЗАГЛУШКА**|`SkinShop.cs` (32 строки) + `SkinShopPanel.cs` (27 строк) — пустые Buy/Equip|
|**Settings**|**РЕАЛИЗОВАНО (базово)**|`MainMenu.OnGUI()` — Music/SFX toggles через PlayerPrefs. `SettingsManager.cs` (57 строк) работает.|

## **11.2 MainMenu (существующая реализация)**

Реализован через `MonoBehaviour.OnGUI()` (Immediate Mode GUI):
```csharp
void OnGUI()
{
    GUI.skin.button.fontSize = 24;
    float buttonWidth = 200;
    float buttonHeight = 50;
    float startX = (Screen.width - buttonWidth) / 2f;
    float startY = (Screen.height - (buttonHeight * 5 + 20 * 4)) / 2f;
    
    // 5 элементов: Start Game, Settings, Skin Shop, Music/SFX toggles, Quit Game
    // StartGame() → SceneManager.LoadScene("Level2") — минует промежуточные экраны
    // Music/SFX toggles: PlayerPrefs.GetInt("MusicOn", 1) и PlayerPrefs.GetInt("SfxOn", 1)
}
```

**Отличие от GDD:** UI Toolkit / UGUI не используются. Меню — onGUI (Immediate Mode).

## **11.3 DifficultySelect (существующая реализация)**

Вместо двух карточек (Колени/Носки) с иллюстрациями — простой Dropdown через `DifficultySelector.cs`:
```csharp
dropdown.AddOptions(new List<string>{ "Easy", "Normal", "Hard" });
// Режим сохраняется в PlayerPrefs["Difficulty"]
// **Не влияет на геймплей** — игнорируется в коде спавнера и Verifier
```

## **11.4 LevelComplete (существующая реализация)**

После стыковки со спутником (Goal) `PlayerCharacter` вызывает `PushupUIHandler.ShowSuccessPanel()`:

**Звёздный рейтинг (5 критериев):**
1. ≥50% печений собрано
2. 0 ударов астероидами
3. 0 ударов камнями
4. ≥10 отжиманий
5. Все звёзды ускорения собраны

Вывод: ★★★☆☆ — строка из 5 символов. Нет личного рекорда, нет Perfect Symmetry.

Кнопки: **RESTART** → перезагрузка сцены. **NEXT LEVEL** → "Level2" (или возврат).

## **11.5 GameOver (существующая реализация)**

`PushupUIHandler.ShowGameOver()`:
- Создаёт панель с Game Over текстом
- Кнопка Restart → `SceneManager.LoadScene(активная сцена)`
- Авто-создание EventSystem
- Счётчик TotalHitsTaken не отображается

## **11.6 HUD во время игры**

Реализован через `PushupUIHandler.cs` (615 строк) с авто-сборкой Canvas (`EnsureOxygenHUD()`):

|**Элемент**|**Позиция**|**Описание**|
| :- | :- | :- |
|Счёт|anchor (0.125, 0.875), pivot (0.5, 0.5)|"Очки: N", пульсация при начислении|
|Индикатор кислорода|anchor (0.875, 0.875), pivot (0.5, 0.5)|5 сердечек, пульсация на бит|
|Счётчик отжиманий|Кастомная|Текст числа|
|Ритм-хит|Динамически|"PERFECT!" / "GOOD!" / "OK!" / "MISS!"|
|Читерство|Центр (0.5, 0.5), font 80|Жёлтый с красной обводкой|
|Шкала ускорения|anchor (0.3125-0.6875, 0.875)|Cyan заливка, показывается при boost|
**COSMO PUSH**

Game Design Document

Version 4.0 | Unity 6.3 LTS (6000.3.11f1) | Android 8.0+ / iOS 14+ | 2026

# **Правила ведения этого документа**

1. Документ описывает игру в настоящем времени.
2. Никаких маркеров новизны в тексте. Документ — готовая спецификация, не журнал правок.
3. Версия документа — только в шапке.
4. Сравнения с другими проектами — не в тексте разделов.
5. Пометки «без изменений» запрещены.
6. История изменений — отдельно в changelog или VCS.
7. **Маркеры статуса:** 🟢 = Реализовано | ⚠️ = Заглушка / Частично | 🔜 = План (не реализовано)

# **1. Обзор игры**

|**Параметр**|**Значение**|
| :- | :- |
|Название|Cosmo Push (рабочее название)|
|Жанр|Ритм-аркада / Фитнес-игра|
|Платформа|Android 8.0+ / iOS 14+|
|Движок|Unity 6.3 LTS (6000.3.11f1)|
|Ориентация|Landscape (горизонтально)|
|Трекинг|🟢 WebcamPoseDetection (BlazePose через Unity Sentis) на всех платформах 🔜 ARKit (iOS A12+) + Sentis (Android) через TrackerFactory|
|Основная точка управления|🟢 Нос (#0) — через PlayerCharacter / NoseMicroController|
|Верификатор отжимания|🟢 PushupVerifier — по среднему Y плеч (#11/#12), без верификации локтей|
|Количество дорожек|🟢 2: HIGH (laneIndex=0) и LOW (laneIndex=1) — MID линия скрыта 🔜 3: HIGH/MID/LOW|
|Смена дорожки|🟢 По порогу среднего Y плеч (>0.5 → HIGH, <0.5 → LOW) 🔜 Верификация локтей + temporal gate 150-200ms|
|Размещение телефона|Стоит на ребре в landscape, прислонён под углом 30-45° к стене/подставке лицевым экраном к игроку. 50-80 см от лица игрока.|
|Камера|Фронтальная камера телефона. Задняя камера не используется.|
|Рендер-пайплайн|Universal Render Pipeline (URP)|
|Режимы сложности|⚠️ Dropdown: Easy / Normal / Hard (DifficultySelector) 🔜 Карточки Колени (Beginner) / Носки (Standard) с иллюстрациями|
|Количество уровней|🟢 2 сцены с аудио-треками: Level 1 (CustomGameMusic, ~112s) и Level 2 (trek2, ~66.6s) 🔜 3 уровня, последовательная разблокировка|

🟢 **Реализовано:** Игрок кладёт телефон на пол в landscape, ложится в упор лёжа лицом к экрану. Камера отслеживает нос (для микросмещения персонажа) и плечи (для смены дорожки). Космонавт в скафандре летит в невесомости. Уклонение двухслойное: лёгкое смещение носом внутри дорожки, полная смена дорожки — через движение плеч.

## **1.1 Правила дизайна — неизменяемые**

**⚠ ПРАВИЛО: Единственные режимы сложности — «Колени» и «Носки». Никаких других вариантов не существует и не добавляется.**

**⚠ ПРАВИЛО: Коммуникация с игроком во время игры — только через звук и визуал. Тактильная обратная связь не используется: телефон лежит на полу и игрок его не держит.**

**⚠ ПРАВИЛО: Смена дорожки возможна исключительно через верифицированное отжимание.** — 🟢 Текущая реализация: проверка только по Y плеч, локти не верифицируются. 🔜 Добавить верификацию локтей (углы #13/#14) и temporal gate 150-200ms.

## **1.2 Лабораторное допущение — стабильность телефона**

Данный документ рассматривает лабораторный случай: телефон установлен надёжно на полу и не подвергается внешним механическим воздействиям в ходе игровой сессии.

# **2. Игровой мир и визуальная концепция**

## **2.1 Сеттинг**

🟢 Открытый космос. Тёмный фон со звёздами. Рендеринг через URP. Каждый уровень — отдельный визуальный биом: уровень 1 — тёмный пояс астероидов, уровень 2 — яркая туманность (BackgroundStoneManager создаёт парящие камни).

🔜 Parallax-фон (3-4 слоя) через GPU Resident Drawer Unity 6.

## **2.2 Персонаж**

🟢 Космонавт — Image UI элемент с RectTransform. Вид сбоку (профиль). Персонаж прикреплён к левой трети экрана (x=300f), движется по вертикали. Реализовано:
- Микросмещение носом (±60px от базовой позиции дорожки)
- Физика растяжения (stretch/squash при движении через AstroTurbine)
- Пульсация на каждый бит (PulseCoroutine, ×1.15)
- Автоматическое притяжение к цели (космическая станция) в финале уровня
- AstroSpriteController + AstroTurbine добавляются в runtime

|**Скин**|**Описание**|**Разблокировка**|
| :- | :- | :- |
|Космонавт|Киберпанк-скафандр, неоновые акценты|🟢 Бесплатно (SkinManager.ApplySkin)|
|Инопланетянин|Серый, большие глаза, антенны|⚠️ 3 000 очков — через SkinManager (заглушка)|
|Кот|Котик в гермошлеме, хвост из скафандра|⚠️ 5 000 очков — через SkinManager (заглушка)|
|Рыба|Рыбка в шарообразном шлеме-аквариуме|⚠️ 8 000 очков — через SkinManager (заглушка)|

## **2.3 Объекты в пространстве**

|**Объект**|**Внешний вид**|**Эффект**|
| :- | :- | :- |
|Препятствие (Obstacle)|🟢 Случайный спрайт из Resources/Emojis/Obstacles/, 300×300, вращается|-1 кислород при столкновении|
|Астероид (Asteroid)|🟢 Спрайт emoji_u2604 (комета), 300×300, летит по диагонали|-1 кислород, оранжевый шлейф|
|Печенье (Cookie)|🟢 Случайный спрайт из Resources/Emojis/Cookies/, 120×120|+10 очков, эффект частиц|
|Звезда-ускорение (Speedup)|🟢 Спрайт emoji_u2b50, 150×150, вращается|×1.5 ускорение музыки на 1 сек|
|Спутник-цель (Goal)|🟢 Спрайт Resources/Satellite, 800×800|Финал уровня|

🔜 **План:** Баллон кислорода (+1 жизнь, макс. 5), детализированные 3D-модели, VFX Graph для частиц.

# **3. Механика дорожек**

## **3.1 Текущая реализация — 2 дорожки (HIGH / LOW, MID скрыта)**

🟢 На данный момент игра использует **2 дорожки** (HIGH и LOW). Дорожка MID определена в коде (`LaneSystem.cs`) и принудительно скрыта. Переключение между HIGH и LOW происходит по порогу среднего Y плеч.

```csharp
// LaneSystem.cs — текущая реализация
float GetLaneY(int laneIndex) {
    if (laneIndex == 0) return HighY;  // y=135
    return LowY;                        // y=-270 (2 линии)
}

// Принудительное скрытие MID линии
void HighlightLane(int laneIndex) {
    if (MidLine != null && MidLine.gameObject.activeSelf)
        MidLine.gameObject.SetActive(false);
}
```

**Дорожки в реализации:**
- laneIndex=0 → HIGH, Y=135, цвет #00FFFF alpha 20%
- laneIndex=1 → LOW, Y=-270, цвет #00FFFF alpha 20%

**Смена дорожки (PushupVerifier):**
```csharp
float leftY = poseDetection.GetKeypointY(11);
float rightY = poseDetection.GetKeypointY(12);
float rawSY = (leftY + rightY) / 2f;
_filteredSY = Mathf.Lerp(_filteredSY, rawSY, Time.deltaTime * 15f);
int detectedLane = (_filteredSY > 0.5f) ? 0 : 1; // HIGH если >0.5
```

🔜 **План:** 3 дорожки (HIGH/MID/LOW) с верификацией локтей и temporal gate 150-200ms. Цвета HIGH=#4488FF, MID=#FFD700, LOW=#00FFCC.

## **3.2 Двухслойная система управления**

**Слой 1 — Микродвижение носом (мгновенно):**
🟢 Реализовано в `PlayerCharacter.cs`:
```csharp
float rawNoseY = 1f - activeTracker.GetNose().y;
targetNoseOffset = (rawNoseY - 0.5f) * noseMicroRange; // range=60px
_noseOffset = Mathf.SmoothDamp(_noseOffset, targetNoseOffset, ref _noseVelocity, 0.1f);
```
- Диапазон: ±60px от центра дорожки
- Вспомогательный `NoseMicroController.cs` существует, но отключается если есть PlayerCharacter

**Слой 2 — Смена дорожки телом:**
🟢 Только через порог среднего Y плеч в PushupVerifier. SmoothDamp переход позиции: `currentSmoothTime` от 0.05s (в движении) до 0.40s (на месте).

🔜 **План:** Верификация локтей (углы #13/#14), вектор носа совпадает с направлением плеч, temporal gate 150-200ms, гистерезис 15%.

|**Тип препятствия**|**Как уклониться**|**Примечание**|
| :- | :- | :- |
|Узкий астероид (центр дорожки)|🟢 Микросмещение носом — без отжимания|Реализовано|
|Широкий астероид (вся дорожка)|🟢 Смена дорожки плечами|Реализовано|
|Двойной астероид (MID + HIGH)|🔜 Единственный выход — LOW, нужно опуститься|Только с 3 дорожками|

## **3.3 Переход между дорожками**

🟢 Персонаж плавно скользит к целевой дорожке (SmoothDamp, время адаптивное 0.05-0.40s в зависимости от BodyVelocityY).

🔜 Гистерезис 15% для предотвращения дребезга при удержании позиции на границе.

## **3.4 Логика препятствий на дорожках**

🟢 Препятствия занимают одну дорожку. У игрока всегда есть одна свободная дорожка (из двух). LevelSpawner использует ритм-карту для синхронизации спавна с музыкой.

🔜 Правило — не ставить более двух одинаковых дорожек подряд, не создавать переход HIGH → LOW менее 1 сек.

# **4. Система трекинга**

## **4.1 Текущая реализация — WebcamPoseDetection (BlazePose через Unity Sentis)**

🟢 Весь трекинг реализован через **единственный класс `WebcamPoseDetection`**, работающий на всех платформах. Никакого платформенного разделения нет.

**Архитектура:**
- `WebcamPoseDetection : MonoBehaviour, IBodyTracker` — синглтон (DontDestroyOnLoad)
- IBodyTracker — интерфейс (GetNose, GetShoulderMidpoint, GetKeypointY и т.д.)
- Модели BlazePose: detector + landmarker, загружаются через ModelLoader.Load()
- Бэкенд: `Unity.InferenceEngine` (старый API Unity Sentis, требуется проверка совместимости с Sentis 2.1.1)
- Инференс на GPU: BackendType.GPUCompute
- Асинхронный цикл через async Awaitable

**Pipeline:**
1. Захват кадра с WebCamTexture (640×480)
2. Detector (224×224) — affine трансформ → поиск региона тела
3. Landmarker (256×256) — 33 keypoint, 5 значений каждый (x, y, z, visibility, extra)
4. Маппинг в нормализованные координаты (0..1)

**Компоненты, использующие трекинг:**
- PushupVerifier — плечи (#11, #12) для смены дорожки + бёдра (#23, #24) для детекта чита
- PlayerCharacter — нос (#0) для микросмещения
- NoseMicroController — альтернативное микросмещение (отключается при PlayerCharacter)

## **4.2 Платформенная архитектура (план)**

|**Условие**|**Реализация**|**Статус**|
| :- | :- | :- |
|iOS, Apple A12+|ARKitBodyTracker : IBodyTracker|🔜 План|
|Android / iOS < A12|SentisBodyTracker : IBodyTracker|⚠️ WebcamPoseDetection покрывает частично|
|Fallback (ошибка инициализации)|Возвращает нулевые данные, игра не стартует|🔜 План|

🔜 Выбор реализации происходит в TrackerFactory.Create() один раз при старте приложения.

## **4.3 iOS — AR Foundation 6 / ARKit Body Tracking**

🔜 **НЕ РЕАЛИЗОВАНО.** Build Profile для iOS не создан. Пакет AR Foundation 6 не подключен. Sentis не исключается из iOS-билда.

## **4.4 Android — Unity Sentis 2.1.1 / BlazePose Full ONNX**

🟢 WebcamPoseDetection загружает BlazePose формата .sentis. Использует старый Unity InferenceEngine API.

**Используемые индексы (MediaPipe BlazePose 33-point):**
- #0 — нос (микросмещение персонажа)
- #11 — левое плечо (смена дорожки)
- #12 — правое плечо (смена дорожки)
- #23 — левое бедро (Z-детекция читерства)
- #24 — правое бедро (Z-детекция читерства)

Остальные 28 точек модели в игровой логике не используются.

🔜 **План:** Переход на Unity Sentis 2.1.1 API (WorkerFactory → Worker, Execute → Schedule, PeekOutput<T>), NativeArray keypoints, Job System, без копирования через managed heap.

## **4.5 Чит-детекция**

🟢 Реализована в `PushupVerifier.CheckIsCheating()` через Z-координату бёдер (#23/#24) относительно плеч (#11/#12):
```csharp
zDelta = avgHipZ - avgShoulderZ
if (zDelta < 80f) → "Вы стоите\nПРИМИТЕ УПОР ЛЁЖА!"
if (zDelta > 260f) → "СЛИШКОМ ВЫСОКО!\nОТЖИМАЙТЕСЬ ОТ ПОЛА!"
```
При читерстве логика отжиманий блокируется (return в Update).

## **4.6 Проверка освещения**

⚠️ **НЕ РЕАЛИЗОВАНО.** Класс `LightCheck.cs` существует как пустая заглушка.

🔜 Анализ яркости кадра WebCamTexture через GetPixels32(), порог luminance < 0.25, предупреждение «Включи свет».

## **4.7 ReadyPlayScreen**

🔜 **НЕ РЕАЛИЗОВАНО.** Игра стартует напрямую в MainMenu → WarmUpLevel (заглушка) → Level2.

🔜 Объединённый экран: живая камера как фон, анимация «ложись», LightCheck, автостарт WarmUpLevel при confidence > 0.7 × 2 сек.

## **4.8 Фильтрация данных трекинга**

🟢 **Текущая фильтрация:**
```csharp
// PushupVerifier — фильтрация плеч
_filteredSY = Mathf.Lerp(_filteredSY, rawSY, Time.deltaTime * 15f);
BodyVelocityY = Mathf.SmoothDamp(BodyVelocityY, instantVelocity, ref _velocityFilter, 0.1f);

// PlayerCharacter — фильтрация носа
_noseOffset = Mathf.SmoothDamp(_noseOffset, targetNoseOffset, ref _noseVelocity, 0.1f);
```

🔜 **План:** KalmanFilterJob [BurstCompile] IJob для носа и плеч, Schedule в Update, Complete в LateUpdate. Low-pass фильтр 2 Hz для дыхания. Confidence-weighted adaptiveR.

## **4.9 Visibility Guard**

🟢 Реализован в `PushupVerifier.HandleVisibilityGuard()`:
- Проверка IsTracking + visibility носа (#0) > 0.5
- Первый раз: 3s таймер до паузы (для загрузки нейросети)
- После первого трекинга: 1.5s таймер до паузы
- Возврат: 3s отсчёт (пробел для сброса)
- Пауза: Time.timeScale=0, AudioListener.pause=true
- Сообщение: "ВЫ ВНЕ КАДРА! ИГРА НА ПАУЗЕ"

⚠️ Отдельный `VisibilityGuard.cs` существует, но является пустой заглушкой.

🔜 **План:** Таймер 10 сек → EndLevelIncomplete() (уровень не засчитан, очки сохраняются). Отдельный VisibilityGuard.cs.

# **5. Управление и геймплей**

## **5.1 Компоненты системы**

|**Компонент**|**Статус**|**Описание**|
| :- | :- | :- |
|PlayerCharacter|🟢|Управление персонажем: позиция, микро-смещение, пульс, турбина, стыковка|
|PushupVerifier|🟢|Смена дорожки (плечи), чит-детекция, visibility guard, счётчик отжиманий|
|PushupLaneChanger|🔜|Отдельный компонент для выдачи разрешения на смену дорожки|
|LaneSystem|🟢|2 дорожки (MID скрыта), визуальные линии, хайлайт текущей|
|LevelSpawner|🟢|Спавн объектов по ритм-карте, астероиды, печенья, звёзды, финал|
|PlayerCollision|🟢|AABB столкновения, сбор печений/звёзд, урон от препятствий|
|RhythmManager|🟢|Ритм-карты из массивов float[], события OnBeat|
|BeatManager|🔜|Синглтон на DSP-clock (AudioSettings.dspTime), OnBeat/OnHalfBeat/OnBar|
|WebcamPoseDetection|🟢|BlazePose трекинг через старый Sentis API|
|IBodyTracker|⚠️|Интерфейс есть, но не используется как абстракция (нет TrackerFactory)|
|NoseMicroController|⚠️|Отключается если есть PlayerCharacter (конфликт)|
|NoseLevelMapper|🔜|Kalman + low-pass 2 Hz фильтрация носа|
|PushupUIHandler|🟢|HUD: счётчик, очки, кислород, панель поражения/успеха|
|MoverItem|🟢|Движение объектов, хитбоксы, контуры (UI/OutlineEffect шейдер)|
|BackgroundStoneManager|🟢|Фоновые парящие камни / параллакс|
|JuiceManager|🟢|Визуальные эффекты: тряска, вспышка, частицы|
|OxygenIndicator|🟢|Отображение кислорода (5 сердечек)|
|DifficultySelector|⚠️|Dropdown Easy/Normal/Hard (вместо карточек Колени/Носки)|
|WarmUpLevel|⚠️|Заглушка: Completed=true в Awake, 0.5s → Level2|
|AsymmetrySystem|⚠️|Пустой Start() — 10 строк. Near-miss механика не реализована|
|SymmetryBonusTracker|⚠️|Пустой Start() — 10 строк. Щит + Perfect Symmetry не реализованы|
|LightCheck|⚠️|Пустой Start() — 10 строк. Проверка освещения не реализована|
|VisibilityGuard|⚠️|Пустой Start() — 10 строк (логика в PushupVerifier)|
|SkinManager|⚠️|Базовый: ApplySkin по имени, загружает спрайт из Resources/Skins/|
|SkinShop|⚠️|Заглушка: пустые кнопки Buy/Equip, нет карусели|
|SkinShopPanel|⚠️|Заглушка: пустая панель|
|SettingsPanel|⚠️|Заглушка (настройки в MainMenu через toggles)|
|StreakManager|⚠️|Заглушка: простой счётчик ++currentStreak, без сохранения|
|RatingManager|⚠️|Заглушка: запрос рейтинга через 3 сессии|
|NotificationManager|⚠️|Заглушка: push-уведомления не настроены|
|AnalyticsManager|⚠️|Заглушка: Firebase Analytics не подключен|
|CrashReporter|⚠️|Заглушка: Firebase Crashlytics не подключен|
|CameraPermissionRequest|⚠️|Не реализован. Камера запускается без явного Permission.Request()|
|CosmoNavigationSystem|🔜|Главный контроллер, объединяющий нос + верификацию|
|BeatVisualSync|🔜|Пульсация виньетки + дыхание персонажа по BPM|
|AdaptiveCalibration|🔜|Дрифт maxY/minY при усталости|
|ElbowAngleDetector|🔜|Оценка качества отжимания по углу локтей|
|PushupCounter|🟢|Встроен в PushupVerifier (LOW→HIGH→LOW с cooldown 0.5s)|
|FeedbackSystem|🔜|Flash-эффекты, текст, анимация персонажа через VFX Graph|
|TutorialCalibrator|🔜|Калибровка носа по шарам в WarmUpLevel|
|LevelData|🔜|ScriptableObject с TrackData + List<LevelEvent>|
|LevelManager|🔜|Разблокировка уровней, сохранение звёзд|
|GameManager|🔜|Синглтон. GameState enum, жизни, счёт, переходы|
|ConsentScreen|🔜|Первый запуск: медицинский дисклеймер + Privacy Policy|
|LevelCompleteScreen|🔜|Подсчёт очков, звёзды, Perfect Symmetry бонус|
|LevelEditorWindow|🔜|Editor-only. Timeline с BPM-сеткой|

## **5.2 Подсчёт отжиманий**

🟢 Конечный автомат с двумя фазами (Phase.Up / Phase.Down) и cooldown-таймером 0.5 сек:
- Phase.Up + дорожка LOW → переход в Phase.Down
- Phase.Down + дорожка HIGH → отжимание засчитано
- Счётчик отжиманий отображается в HUD через событие `OnPushupCounted`
- Ритм-оценка `RateRhythmHit`: PERFECT! (<0.2s), GOOD (<0.5s), OK!, MISS!

🔜 **План:** ElbowAngleDetector — оценка качества (Full >40°, Partial 20-40°, Cheat <20°). SymmetryBonusTracker.

# **6. Система препятствий и сбора**

🟢 **Уровень 1:** Спавн печений на каждый бит, группы по 3-6. Препятствия по таймеру (2-3 сек). Астероиды с таймером 10-18 сек.

🟢 **Уровень 2:** Строгий спавн на каждый 4-й бит. Препятствия на биты, печенья группами между ними с проверкой перекрытия.

🟢 **Столкновения AABB:** Печенья +10 очков (hitSizeFactor 1.2). Препятствия -1 кислород (hitSizeFactor 0.7). Звёзды speedup ×1.5 на 1 сек. Спутник игнорируется.

🟢 **Физика MoverItem:** Позиция = targetHitX + (speed × timeToHit). Вращение объектов. UI/OutlineEffect шейдер для контуров.

🔜 **План:** ObjectPool для объектов (сейчас new GameObject/Destroy каждый раз). Баллон кислорода (+1 жизнь, редко). VFX Graph вместо Image-частиц. Двойные препятствия.

# **7. Ритм-система**

## **7.1 Текущая реализация**

🟢 `RhythmManager.cs`:
- Два хардкоженных массива float[]: level1Map (~190 битов), level2Map (~110 битов)
- AudioSource.time для синхронизации (не DSP-clock)
- Событие `OnBeat(bool isDown)` в Update()
- Speedup через pitch multiplier
- Модульная система: useCustomMap + customMap[] для новых уровней
- Определение уровня по имени сцены

```csharp
// Текущая синхронизация
if (SongTime >= beatMap[currentBeatIndex]) {
    OnBeat?.Invoke(CurrentExpectedStateDown);
    currentBeatIndex++;
}
```

|**Уровень**|**Трек**|**Длительность**|**BPM (приблизительно)**|
| :- | :- | :- | :- |
|1 — Пояс астероидов|CustomGameMusic|~112 сек|100|
|2 — Туманность|trek2|~66.6 сек|~105|
|3 — Ледяная орбита|🔜 НЕТ ТРЕКА|НЕТ|Не создан|

🔜 **План:** DSP-clock (AudioSettings.dspTime), OnHalfBeat, OnBar, LevelData ScriptableObject с TrackData.

## **7.2 Визуальная синхронизация с ритмом**

🟢 Реализована:
1. **Пульсация персонажа** — PlayerCharacter.PulseCoroutine (×1.15 на OnBeat, 0.1 сек)
2. **Пульсация индикатора кислорода** — OxygenIndicator.PulseCoroutine (×1.2, при 1 жизни ×1.5)
3. **Ритм-рейтинг** — RateRhythmHit (PERFECT/GOOD/OK/MISS относительно ближайшего бита)

🔜 **План:** Пульсация виньетки Post Processing Volume (2-3%). OnBeat пульс дорожек VFX Graph. 3D Audio stereoPan по дорожкам. Дыхание персонажа (покачивание в ритм BPM).

# **8. Счёт, жизни и прогрессия**

## **8.1 Кислород как жизни**

|**Событие**|**Эффект**|
| :- | :- |
|Столкновение с препятствием / астероидом|🟢 -1 кислород. Красная вспышка + тряска экрана|
|0 кислорода|🟢 Game Over: панель с кнопками Restart / Quit|
|Старт уровня / рестарт|🟢 Кислород = 5|

🟢 Счётчики: AsteroidHitsTaken, RockHitsTaken, TotalHitsTaken, CookiesCollected, StarsCollected.

🔜 **План:** Подбор баллона кислорода (+1, макс. 5, редко). Кислород сохраняется между уровнями. Щит (3 симметричных отжимания).

## **8.2 Очки — печенья**

|**Параметр**|**Значение**|**Статус**|
| :- | :- | :- |
|Очки за одно печенье|10 базовых|🟢|
|Комбо-множитель|×1 → ×2 → ×3|🔜 План|
|Звёзды уровня|< 50% — 1 звезда, 50-80% — 2, >80% — 3|🔜 План|
|Итог уровня|X / MAX_COOKIES — процент собранных печений|⚠️ Подсчёт есть, UI нет|

## **8.3 Счётчик отжиманий**

🟢 LOW → HIGH → LOW цикл с cooldown 0.5 сек. Счётчик в HUD.

🔜 **План:** ElbowAngleDetector — Full (>40° зелёный), Partial (20-40° жёлтый), Cheat (<20° красный).

## **8.4 Длина сессии**

🟢 Уровень 1: ~112 сек, ~190 бит. Уровень 2: ~66.6 сек, ~110 бит.

🔜 **План:** Уровень 1: ~10 отжиманий, 2-3 мин. Уровень 2: ~11 отжиманий. Уровень 3: ~12 отжиманий.

# **9. Near-Miss и асимметрия**

⚠️ **AsymmetrySystem.cs** — пустая заглушка (10 строк). Near-miss не реализован.

🔜 **План:** Анализ разницы Y между левым и правым плечом. При разнице >6% высоты кадра — X-крен в сторону более высокого плеча. Near-miss только визуальный эффект, не отнимает жизнь. Уровни: <6% — нет, 6-10% — лёгкое смещение + жёлтая дуга + свист, >10% — заметный крен + вспышка.

# **10. Бонус за симметричные отжимания**

⚠️ **SymmetryBonusTracker.cs** — пустая заглушка (10 строк).

🔜 **План:**
- 3 подряд симметричных отжимания (<4% асимметрии) → неоновый щит вокруг скафандра
- Щит поглощает одно столкновение (кислород не теряется, щит исчезает)
- Серия сбрасывается при асимметричном отжимании
- Perfect Symmetry бонус за уровень: все отжимания симметричны → +N печений (N = кол-во отжиманий)

# **11. WarmUpLevel**

⚠️ Текущая реализация — заглушка:
```csharp
Completed = true; // Мгновенно помечается как пройденный
Invoke(nameof(LoadMainLevel), 0.5f); // Загружает Level2
```

🔜 **План:** Полноценный уровень-разогрев:
- Только печенья (без астероидов), полное игровое окружение (фон, музыка)
- Шар на LOW → "Опустись — поймай!" → фиксация minY носа
- Шар на HIGH → "Поднимись — поймай!" → фиксация maxY носа
- Первый шар всегда MID (нос игрока в покое)
- Прогресс-бар, мягкий режим PushupVerifier (пониженные пороги на 40%)
- Автоматический переход в Countdown без кнопки

# **12. Экраны и состояния игры**

|**Состояние**|**Статус**|**Описание**|
| :- | :- | :- |
|**ConsentScreen**|🔜|Медицинский дисклеймер + Privacy Policy. Только первый запуск|
|**MainMenu**|🟢|OnGUI: Start Game, Settings, Skin Shop, Music/SFX, Quit. Start Game → Level2|
|**LevelSelect**|🔜|3 уровня с иконками, звёздами, статусами|
|**DifficultySelect**|⚠️|Dropdown Easy/Normal/Hard вместо карточек Колени/Носки|
|**ReadyPlayScreen**|🔜|Камера как фон, LightCheck, автостарт|
|**WarmUpLevel**|⚠️|Заглушка: мгновенный переход в Level2|
|**Countdown**|🔜|3-2-1-GO на первые 4 бита трека|
|**Playing**|🟢|2 уровня, HUD, спавн, коллизии, ритм|
|**OutOfFrame**|🟢|Пауза при выходе из кадра (в PushupVerifier)|
|**LevelIncomplete**|🔜|Экран со статистикой без звёзд при 10 сек вне кадра|
|**Paused**|🟢|Escape → Time.timeScale=0. Resume/Restart/Menu|
|**LevelComplete**|⚠️|SuccessPanel активна, но без звёзд, Perfect Symmetry и рекордов|
|**GameOver**|🟢|Панель с кнопками Restart/Quit. Через GameOverManager|
|**SkinShop**|⚠️|Заглушка. Пустые кнопки Buy/Equip|
|**Settings**|⚠️|Только Music/SFX toggle в MainMenu. SettingsPanel — заглушка|

## **12.1 MainMenu**

🟢 `MonoBehaviour.OnGUI()`:
```csharp
// 5 кнопок: Start Game (→Level2), Settings (Debug.Log), Skin Shop (Debug.Log),
// Music toggle, SFX toggle (PlayerPrefs), Quit Game
```

🔜 Анимированное меню с логотипом, UGUI-кнопками, иконкой стрека, балансом очков.

## **12.2 LevelComplete**

⚠️ После стыковки со спутником `PushupUIHandler.ShowSuccessPanel()`:
- Показывает панель с результатами
- НЕТ звёзд, НЕТ Perfect Symmetry бонуса, НЕТ личного рекорда
- Счёт отображается как "Очки: N"

🔜 Анимация стыковки → подсчёт очков → звёзды → Perfect Symmetry бонус → личный рекорд → кнопки (Следующий уровень / Повторить).

## **12.3 HUD во время игры**

🟢 Реализован через `PushupUIHandler`:
- **Счёт** (левый верхний угол, anchor = 0.125, 0.875)
- **Индикатор кислорода** (правый верхний угол, 5 сердечек)
- **Счётчик отжиманий** (кастомная позиция)
- **Текст ритм-хита** PERFECT!/GOOD!/OK!/MISS!
- **Предупреждение о читерстве** (большой текст в центре)
- **Шкала ускорения** (cyan полоса)

🔜 **План:** Комбо-множитель. Прогресс уровня (полоска внизу). Иконка носа (симметрия/крен). Ореол щита.

# **13. JuiceManager — система визуальных эффектов**

🟢 `JuiceManager.cs` (324 строки). Единый менеджер на Canvas:

|**Эффект**|**Метод**|**Описание**|
| :- | :- | :- |
|Тряска экрана|Shake(duration, amount)|Случайные смещения Canvas|
|Вспышка|Flash(color, duration)|Оверлей с затуханием|
|Парящий текст|SpawnFloatingText(text, pos, color)|Текст вверх + исчезает|
|Летящий счёт|SpawnFlyingScore(pos, text, target)|Текст + частицы к счётчику|
|Взрыв частиц|SpawnParticleBurst(pos, color, target)|15 Image-частиц с параболой + магнит|
|Линии ускорения|SpawnSpeedLines(pos)|Голубые полосы, разлёт влево|
|Шлейф астероида|SpawnAsteroidTrail(pos)|Оранжевые кубики с затуханием|

🔜 **План:** VFX Graph вместо Image-частиц. Flash через Post Processing Volume.

# **14. Адаптивный дрифт**

🔜 **НЕ РЕАЛИЗОВАНО.** AdaptiveCalibration не реализован.

🔜 **План:** Если HIGH не достигается дольше 45 сек — maxY медленно смещается вниз (1%/мин, не более ±20% от исходной). Аналогично для LOW.

# **15. Техническая архитектура**

## **15.1 Существующие скрипты (46 шт)**

|**Скрипт**|**Строк**|**Статус**|**Назначение**|
| :- | :- | :- | :- |
|AnalyticsManager.cs|97|⚠️ Заглушка|Firebase Analytics|
|AstroSpriteController.cs|41|🟢|Анимация спрайта космонавта|
|AstroTurbine.cs|111|🟢|Визуальные эффекты турбины (хвост)|
|AsymmetrySystem.cs|10|⚠️ ПУСТОЙ|Near-miss механика|
|BGStoneMover.cs|71|🟢|Фоновый камень (параллакс)|
|BackgroundStoneManager.cs|263|🟢|Менеджер фоновых камней|
|BlazeUtils.cs|123|🟢|Утилиты Sentis (матрицы, anchors)|
|CameraPreviewHandler.cs|133|🟢|Превью камеры на экране|
|CrashReporter.cs|10|⚠️ Заглушка|Firebase Crashlytics|
|DebugGridOverlay.cs|188|🟢|Отладка сетки/дорожек|
|DifficultySelector.cs|24|⚠️ Заглушка|Dropdown Easy/Normal/Hard|
|GameInitializer.cs|27|🟢|Инициализация при старте|
|GameOverManager.cs|28|🟢|Рестарт/выход при GameOver|
|IBodyTracker.cs|20|⚠️ Интерфейс|Унифицированный интерфейс трекинга|
|JuiceManager.cs|324|🟢|Визуальные эффекты|
|LaneSystem.cs|64|🟢 (2 линии)|Система дорожек|
|LevelSpawner.cs|316|🟢|Спавн объектов по ритму|
|LightCheck.cs|10|⚠️ ПУСТОЙ|Проверка освещения|
|MainMenu.cs|89|🟢 (onGUI)|Главное меню|
|MoverItem.cs|247|🟢|Движущийся объект|
|NoseMicroController.cs|59|⚠️ Отключён|Микродвижение носом (конфликт с PlayerCharacter)|
|NotificationManager.cs|127|⚠️ Заглушка|Push-уведомления|
|OxygenIndicator.cs|82|🟢|Индикатор кислорода|
|PauseManager.cs|31|🟢|Пауза по Escape|
|PlayerCharacter.cs|243|🟢|Персонаж: движение, растяжение, пульс|
|PlayerCollision.cs|171|🟢|Коллизии AABB|
|PoseSkeletonDrawer.cs|105|🟢 Отладка|Отрисовка скелета поверх камеры|
|PushupUIHandler.cs|615|🟢|HUD, экраны успеха/поражения|
|PushupVerifier.cs|265|🟢|Верификация отжиманий + смена дорожки|
|RatingManager.cs|184|⚠️ Заглушка|Запрос рейтинга|
|RhythmManager.cs|149|🟢|Ритм-система по beatMap|
|SettingsManager.cs|57|🟢|Настройки SFX/Music|
|SettingsPanel.cs|27|⚠️ Заглушка|UI настроек|
|ShoulderVisualizer.cs|53|🟢 Отладка|Визуализация плеч|
|SkinManager.cs|50|🟢 (базово)|Экипировка скинов|
|SkinShop.cs|32|⚠️ Заглушка|Магазин скинов|
|SkinShopPanel.cs|27|⚠️ Заглушка|Панель магазина|
|StartupLoader.cs|27|🟢|Загрузка MainMenu при старте|
|StreakManager.cs|17|⚠️ Заглушка|Серия дней|
|SymmetryBonusTracker.cs|10|⚠️ ПУСТОЙ|Щит + Perfect Symmetry|
|VisibilityGuard.cs|10|⚠️ ПУСТОЙ|Выход из кадра|
|WarmUpLevel.cs|20|⚠️ Заглушка|Мгновенный переход в Level2|
|WebcamPoseDetection.cs|213|🟢|BlazePose трекинг через Sentis|
|CosmoPushSetupEditor.cs|—|🟢 Editor|Инструмент настройки сцены|
|CreateMainMenuScene.cs|—|🟢 Editor|Создание сцены меню|
|SetStartupScene.cs|—|🟢 Editor|Установка стартовой сцены|

**Итого:** 46 скриптов. 5 пустых заглушек (AsymmetrySystem, LightCheck, SymmetryBonusTracker, VisibilityGuard, CrashReporter). 38 скриптов с работающей (или частично работающей) логикой.

## **15.2 Структура проекта**

```
Assets/
├── Audio/ — аудиофайлы (CustomGameMusic, trek2)
├── Editor/ — редакторские скрипты (CosmoPushSetupEditor, CreateMainMenuScene, SetStartupScene)
├── Models/ — 3D-модели
├── Resources/
│   ├── Emojis/Cookies/ — спрайты печений
│   ├── Emojis/Obstacles/ — спрайты препятствий
│   ├── Emojis/Asteroid/ — спрайт астероида (emoji_u2604)
│   ├── Emojis/Speedup/ — спрайт звезды (emoji_u2b50)
│   └── Emojis/Hearts/ — спрайт сердечка (heart)
│   └── Satellite — спрайт цели
├── Scenes/ — сцены (Level2.unity, WarmUpLevel.unity, _Recovery/)
├── Scripts/ — 46 C# скриптов
├── Shaders/ — шейдеры (UI/OutlineEffect)
├── Sprites/ — дополнительные спрайты
└── DefaultVolumeProfile.asset — профиль пост-процессинга
```

## **15.3 Зависимости — текущие пакеты**

|**Пакет**|**Назначение**|**Статус**|
| :- | :- | :- |
|Unity Sentis (вшит)|Инференс BlazePose|🟢 (старый API)|
|Burst Compiler (встроен)|KalmanFilterJob|🔜 Не используется|
|Unity Jobs / Collections|Параллельная обработка|🔜 Не используется|
|Newtonsoft JSON|Сериализация данных|⚠️ Не проверено|
|com.coplaydev.unity-mcp|MCP для Unity (инструмент)|🟢 Только Editor|
|AR Foundation 6 + ARKit|iOS трекинг|🔜 Не подключен|
|VFX Graph|GPU-driven частицы|🔜 Не используется|
|Adaptive Performance 5.x|Тепловой менеджмент|🔜 Не подключен|
|UniTask|Асинхронность|🔜 Не подключен|
|DOTween (Free)|Анимации UI|🔜 Не подключен|
|Unity Localization|Локализация|🔜 Не подключен|
|Firebase Crashlytics|Краш-репорты|🔜 Не подключен|
|Firebase Analytics|Аналитика|🔜 Не подключен|

## **15.4 Производительность (текущая)**

- 🟢 BlazePose: GPUCompute, ~20-35ms на Snapdragon 730+
- 🟢 WebCamTexture: 640×480
- 🔜 KalmanFilterJob: НЕ РЕАЛИЗОВАН — вся фильтрация через Mathf.SmoothDamp / Mathf.Lerp
- 🔜 ObjectPool: НЕ РЕАЛИЗОВАН — new GameObject() и Destroy() каждый раз
- 🟢 Application.targetFrameRate = 60
- 🔜 Adaptive Performance: не подключен

# **16. План дальнейшей разработки**

## **🔴 Ближайший приоритет**

1. **Третья дорожка (MID):** LaneSystem.GetLaneY() → 3 линии. Пересчёт PushupVerifier для работы с 3 lane. Обновление спавнера
2. **Temporal gating в PushupVerifier:** верификация угла локтей (#13/#14), AND-условие (плечи + локти + нос), таймер 150-200ms
3. **KalmanFilterJob:** [BurstCompile] IJob для носа и плеч, Schedule в Update, Complete в LateUpdate
4. **WarmUpLevel:** калибровка minY/maxY через шары-печенья
5. **BeatManager на DSP-clock:** AudioSettings.dspTime, LevelData ScriptableObject

## **🟡 Средний приоритет**

6. **AsymmetrySystem + near-miss**
7. **SymmetryBonusTracker + щит**
8. **LevelCompleteScreen** (звёзды, Perfect Symmetry, личный рекорд)
9. **LevelSelect** (3 уровня, статусы, звёзды)
10. **DifficultySelect** (карточки Колени/Носки с иллюстрациями)
11. **SkinShop** (карусель, swipe, покупка за очки)
12. **Комбо-множитель ×1→×2→×3**
13. **LightCheck** (анализ яркости, предупреждение)

## **🔵 Дальний план**

14. **ARKitBodyTracker** (iOS A12+)
15. **TrackerFactory + Build Profiles**
16. **VisibilityGuard.EndLevelIncomplete()** (таймер 10 сек)
17. **AdaptiveCalibration** (дрифт при усталости)
18. **BeatVisualSync** (виньетка + VFX Graph)
19. **Adaptive Performance 5.x** (тепловой менеджмент)
20. **StreakManager по GDD** (PlayerPrefs, даты, награды)
21. **LevelEditorWindow** (Timeline с BPM-сеткой)
22. **CosmoNavigationSystem** (центральный контроллер)
23. **ObjectPool** для SpaceObject
24. **ConsentScreen**
25. **ReadyPlayScreen**

---

*Cosmo Push GDD — Version 4.0 — 2026*
*Документ обновлён: май 2026 — интегрировано описание текущей реализации (46 скриптов, 2 уровня, ритм-система без DSP, 2 дорожки вместо 3, визуальные эффекты, заглушки для будущих механик). Добавлены маркеры статуса 🟢⚠️🔜 из v4.1 (автор: lol).*
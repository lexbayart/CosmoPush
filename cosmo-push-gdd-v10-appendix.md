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

**Важно:** Файлы `LevelSelect.cs`, `ConsentScreen.cs`, `SettingsMenu.cs` не существуют в проекте. Соответствующие экраны не реализованы.

|**Состояние**|**Статус**|**Описание реализации**|
| :- | :- | :- |
|**ConsentScreen**|**НЕ РЕАЛИЗОВАНО**|—|
|**MainMenu**|**РЕАЛИЗОВАНО**|`MainMenu.cs` (89 строк) через `MonoBehaviour.OnGUI()` — 5 кнопок + Music/SFX toggles|
|**LevelSelect**|**НЕ РЕАЛИЗОВАНО**|Start Game ведёт сразу в "Level2"|
|**DifficultySelect**|**ЗАГЛУШКА**|`DifficultySelector.cs` (24 строки) — Dropdown Easy/Normal/Hard вместо карточек Колени/Носки|
|**ReadyPlayScreen**|**НЕ РЕАЛИЗОВАНО**|—|
|**WarmUpLevel**|**ЗАГЛУШКА**|Мгновенный переход в Level2|
|**Countdown**|**НЕ РЕАЛИЗОВАНО**|—|
|**Playing**|**РЕАЛИЗОВАНО**|2 уровня, HUD, спавн, коллизии, пауза|
|**OutOfFrame**|**РЕАЛИЗОВАНО**|Встроен в PushupVerifier, пауза вместо LevelIncomplete|
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

Вывод: ★★★☆☆ — звёздная строка из 5 символов. Нет личного рекорда уровня, нет Perfect Symmetry бонуса.

Кнопки: **RESTART** → перезагрузка сцены. **NEXT LEVEL** → "Level2" (или возврат в MainMenu с Level2).

## **11.5 GameOver (существующая реализация)**

`PushupUIHandler.ShowGameOver()`:
- Создаёт панель с Game Over текстом
- Кнопка Restart → `SceneManager.LoadScene(активная сцена)` (с разблокировкой аудио)
- Автоматическое создание `EventSystem` если его нет
- Счётчик TotalHitsTaken ведётся, но не отображается

## **11.6 HUD во время игры**

Реализован через `PushupUIHandler.cs` (615 строк) с авто-сборкой Canvas через `EnsureOxygenHUD()`:

|**Элемент**|**Позиция**|**Описание**|
| :- | :- | :- |
|Счёт|anchor (0.125, 0.875), pivot (0.5, 0.5)|"Очки: N", пульсация при начислении|
|Индикатор кислорода|anchor (0.875, 0.875), pivot (0.5, 0.5)|5 сердечек `Resources/Emojis/Hearts/heart`, пульсация на бит|
|Счётчик отжиманий|Кастомная позиция|Текст числа отжиманий|
|Ритм-хит|Динамически|"PERFECT!" / "GOOD!" / "OK!" / "MISS!" — 1 сек показа|
|Предупреждение о читерстве|Центр (0.5, 0.5), font 80, жирный жёлтый с красной обводкой|"Вы стоите..." или "СЛИШКОМ ВЫСОКО!"|
|Шкала ускорения|anchor (0.3125-0.6875, 0.875)|Cyan заливка на тёмной подложке, показывается при boost > 0|
|BoostBarRoot|Создаётся в `EnsureOxygenHUD()`|Тёмная подложка + cyan заполнение|

**НЕ РЕАЛИЗОВАНО:**
- Комбо-множитель
- Прогресс уровня (полоска внизу)
- Иконка носа (симметрия/крен)
- Ореол щита

## **11.7 Управление паузой**

`PauseManager.cs` (31 строка):
```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }
}
```

# **12. JuiceManager — система визуальных эффектов**

**🟢 Реализовано (v4.0 prototype)**

`JuiceManager.cs` (324 строки). Единый менеджер на Canvas, синглтон через `JuiceManager.Instance`:

|**Эффект**|**Метод**|**Описание**|
| :- | :- | :- |
|Тряска экрана|`Shake(duration, amount)`|Случайные смещения Canvas, `Time.unscaledDeltaTime`|
|Вспышка|`Flash(color, duration)`|Оверлей Image с затуханием альфы|
|Парящий текст|`SpawnFloatingText(text, pos, color)`|Текст вверх + альфа-исчезание (1 сек)|
|Летящий счёт|`SpawnFlyingScore(pos, text, target)`|Текст + 15 частиц, летят к target-счётчику (0.8 сек)|
|Взрыв частиц|`SpawnParticleBurst(pos, color, target)`|15 Image-частиц с параболой разлёта + магнит к цели|
|Линии ускорения|`SpawnSpeedLines(pos)`|10 голубых полос, разлёт влево с растяжением|
|Шлейф астероида|`SpawnAsteroidTrail(pos)`|Оранжевые кубики с вращением и затуханием (0.6 сек)|

# **13. Техническая архитектура**

## **13.1 Существующие скрипты (фактические)**

|**Скрипт**|**Строк**|**Статус**|**Назначение**|
| :- | :- | :- | :- |
|AstroSpriteController.cs|41|Работает|Анимация наклона спрайта космонавта (SmoothDamp)|
|AstroTurbine.cs|111|Работает|Эффект турбины: огненные квадратные частицы за спиной|
|AsymmetrySystem.cs|10|**ПУСТОЙ**|Near-miss механика (не реализована)|
|BGStoneMover.cs|71|Работает|Фоновый камень: параллакс, синхронизация с AudioSource.pitch, fadeOut|
|BackgroundStoneManager.cs|263|Работает|Процедурный менеджер: Huge камни + падающие астероиды (серийный спавн)|
|BlazeUtils.cs|123|Работает|Утилиты: матрицы 2×3, anchors, ImageTransform compute shader, argmax фильтрация|
|CameraPreviewHandler.cs|133|Работает|Превью камеры на RawImage, AspectRatioFitter, CosmicSparkle мерцающие звёзды|
|DebugGridOverlay.cs|188|Работает|Отладка сетки/дорожек с текстовыми метками|
|DifficultySelector.cs|24|Заглушка|Dropdown Easy/Normal/Hard (не влияет на геймплей)|
|GameInitializer.cs|27|Работает|Синглтон-инициализация при старте (DontDestroyOnLoad)|
|GameOverManager.cs|28|Работает|Рестарт/выход при GameOver|
|IBodyTracker.cs|20|Интерфейс|Унифицированный интерфейс трекинга (GetNose, GetShoulderMidpoint, GetKeypoint*)*|
|JuiceManager.cs|324|Работает|Единый менеджер Juice-эффектов (shake, flash, particles, flying scores)|
|LaneSystem.cs|64|Работает|Система дорожек. MID принудительно скрыта. GetLaneY(0)=HighY, GetLaneY(1)=LowY|
|LevelSpawner.cs|316|Работает|Спавн по beatMap. Level 1 и Level 2 режимы (rocksOnBeatMode). Цель в конце|
|LightCheck.cs|10|**ПУСТОЙ**|Проверка освещения (не реализована)|
|MainMenu.cs|89|Работает|Главное меню через `OnGUI()` (Immediate Mode)|
|MoverItem.cs|247|Работает|Физика объектов: прецизионное время-позиционирование, контурный шейдер (UI/OutlineEffect), конкуренция печений (ближайшая зелёная)|
|NoseMicroController.cs|59|Работает|Движение носом в пределах дорожки|
|NotificationManager.cs|127|Заглушка|Push-уведомления (не настроены)|
|OxygenIndicator.cs|82|Работает|5 кругов/serдечек, пульсация на бит (+50% при 1 жизни)|
|PauseManager.cs|31|Работает|Пауза по Escape|
|PlayerCharacter.cs|243|Работает|Y-движение, пульсация (*1.15 на OnBeat), stretch/squash, притяжение к цели|
|PlayerCollision.cs|171|Работает|AABB коллизии: печенья hitbox 1.2×, опасности 0.7×, статические счётчики урона|
|PoseSkeletonDrawer.cs|105|Отладка|Отрисовка скелета BlazePose поверх камеры|
|PushupUIHandler.cs|615|Работает|HUD, авто-сборка Canvas (EnsureOxygenHUD), SuccessPanel с звёздным рейтингом, GameOver, шкала ускорения|
|PushupVerifier.cs|265|Работает|Верификация: плечи > 0.5 → дорожка HIGH, < 0.5 → LOW. Читерство по Z бёдер. VisibilityGuard. Pushup FSM (Up/Down/Cooldown)|
|RatingManager.cs|184|Работает|Запрос рейтинга через N сессий. Открытие App Store / Google Play. 3 кнопки (Rate/Remind/Never)|
|RhythmManager.cs|149|Работает|Ритм по beatMap float[]. SongTime из AudioSource.time. OnBeat(bool). IsLevel2 по имени сцены. TriggerSpeedup(duration, pitch)|
|SettingsManager.cs|57|Работает|PlayerPrefs SFX/Music on/off. Применение к AudioListener/AudioSource|
|SettingsPanel.cs|27|Заглушка|UI настроек (пустой Start, Debug.Log в Awake)|
|ShoulderVisualizer.cs|53|Отладка|Визуализация плеч в кадре (круги в позициях 11/12)|
|SkinManager.cs|50|Работает (базово)|Load Sprite из Resources/Skins/{name}, Apply к PlayerCharacter Image|
|SkinShop.cs|32|Заглушка|Пустые Buy/Equip кнопки, coinsText = "Coins: 0"|
|SkinShopPanel.cs|27|Заглушка|Панель магазина (пустой Start)|
|StartupLoader.cs|27|Работает|Загрузка MainMenu при старте|
|StreakManager.cs|17|Заглушка|Счётчик стрика (AddStreak/ResetStreak, без сохранения)|
|SymmetryBonusTracker.cs|10|**ПУСТОЙ**|Щит + Perfect Symmetry (не реализованы)|
|VisibilityGuard.cs|10|**ПУСТОЙ**|Выход из кадра (функция в PushupVerifier)|
|WarmUpLevel.cs|20|Заглушка|Completed=true сразу → LoadScene("Level2") через 0.5с|
|WebcamPoseDetection.cs|213|Работает|BlazePose Full (33 kp): Detector (224×224, 2254 anchors) + Landmarker (256×256). GPUCompute, async Awaitable. DontDestroyOnLoad синглтон|
|CosmoPushSetupEditor.cs|—|Editor|Инструмент настройки сцены|
|CreateMainMenuScene.cs|—|Editor|Создание сцены меню|
|SetStartupScene.cs|—|Editor|Установка стартовой сцены|

**Итого:** 44 скрипта. 4 пустых заглушки (AsymmetrySystem, LightCheck, SymmetryBonusTracker, CrashReporter/не найден). 2 условно пустых (VisibilityGuard — функциональность в PushupVerifier). 5 заглушек UI/логики (DifficultySelector, WarmUpLevel, SkinShop, SkinShopPanel, SettingsPanel). Остальные — с работающей логикой.

**Файлы, не существующие в проекте (упомянутые в старом GDD v4.0):**
- `TrackerFactory.cs`, `PushupLaneChanger.cs`, `KalmanFilterJob.cs`
- `NoseLevelMapper.cs`, `AdaptiveCalibration.cs`, `BeatMarker.cs`
- `LevelSelect.cs`, `ConsentScreen.cs`, `SettingsMenu.cs`
- `DifficultySelect.cs` (отдельный файл), `ElbowAngleDetector.cs`
- `CosmoNavigationSystem.cs`, `AnalyticsTracker.cs`, `PushupCounter.cs`, `LevelComplete.cs`

## **13.2 Структура проекта**

```
Assets/
├── Audio/ — аудиофайлы (CustomGameMusic, trek2)
├── Editor/ — 3 редакторских скрипта (CosmoPushSetupEditor, CreateMainMenuScene, SetStartupScene)
├── Models/ — 3D-модели
├── Resources/
│   ├── Emojis/
│   │   ├── Cookies/ — спрайты печений
│   │   ├── Obstacles/ — спрайты препятствий
│   │   ├── Asteroid/ — спрайт астероида (emoji_u2604)
│   │   ├── Speedup/ — спрайт звезды (emoji_u2b50)
│   │   ├── Hearts/ — спрайт сердечка (heart)
│   │   └── Background/ — спрайты звёзд/искр для фона
│   ├── Satellite — спрайт цели (финиш)
│   ├── ComputeShaders/ — ImageTransform.compute (BlazePose)
│   └── Skins/ — спрайты скинов космонавта
├── Scenes/ — сцены (Level2.unity, WarmUpLevel.unity, _Recovery/)
├── Scripts/ — 44 C# скрипта (перечислены выше)
├── Shaders/ — шейдеры (UI/OutlineEffect для контура объектов)
├── Sprites/ — дополнительные спрайты
└── DefaultVolumeProfile.asset — профиль пост-процессинга
```

## **13.3 Зависимости — установленные пакеты**

|**Пакет**|**Назначение**|
| :- | :- |
|Unity Sentis (вшит в проект)|Инференс BlazePose — `Unity.InferenceEngine`, `Unity.Sentis`|
|Burst Compiler (встроен)|Не используется (нет KalmanFilterJob)|
|Unity Jobs / Collections (встроен)|Не используется|
|com.coplaydev.unity-mcp|MCP для Unity (инструмент разработки, не в билд)|

**Отсутствуют (по GDD v4.0):**
- AR Foundation 6 (для ARKit Body Tracking)
- Newtonsoft JSON (если был, не используется)
- VFX Graph (для пульсации дорожек)
- Adaptive Performance 5.x
- Unity Graphy (FPS мониторинг)

## **13.4 Производительность на мобильных (текущая)**

|**Аспект**|**Текущее состояние**|
| :- | :- |
|BlazePose инференс|GPUCompute backend, ~20-35 мс на Snapdragon 730+|
|WebCamTexture разрешение|640×480|
|Фильтрация трекинга|Mathf.SmoothDamp / Mathf.Lerp — на CPU, без Burst|
|ObjectPool|НЕ РЕАЛИЗОВАН — `new GameObject()` + `Destroy()` в спавнере|
|Target frame rate|60 (через QualitySettings)|
|Графические инструменты|Graphy / Adaptive Performance не подключены|

# **14. Что необходимо реализовать**

## **14.1 Ближайший приоритет**

1. **Третья дорожка (MID):** `LaneSystem.GetLaneY()` → 3 lanes. Пересчёт спавнера (`LevelSpawner.SpawnObject`, `GetLaneY`). MID включается, не скрывается.
2. **Temporal gating в PushupVerifier:** верификация угла локтей (#13/#14), AND-условие (плечи И локти И нос), таймер 150-200 мс.
3. **KalmanFilterJob:** `[BurstCompile] IJob` для носа (0) и плеч (11, 12). Schedule в Update → Complete в LateUpdate.
4. **WarmUpLevel:** калибровка minY/maxY через шары-печенья с обучением.
5. **RhythmManager на DSP-clock:** `AudioSettings.dspTime` вместо `AudioSource.time`, LevelData ScriptableObject для beat-карт.
6. **Low-pass фильтр 2 Hz:** для дыхательных артефактов в NoseLevelMapper.

## **14.2 Средний приоритет**

7. **AsymmetrySystem + near-miss** (анализ разницы плеч, крен по X)
8. **SymmetryBonusTracker + щит** (3 симметричных → щит, поглощающий удар)
9. **LevelCompleteScreen** (доработка: личный рекорд, Perfect Symmetry бонус)
10. **LevelSelect** (3 уровня, статусы, звёзды из PlayerPrefs)
11. **DifficultySelect** (карточки Колени/Носки с иллюстрациями, убрать Dropdown Easy/Normal/Hard)
12. **SkinShop** (карусель, swipe, покупка за очки через PlayerPrefs)
13. **Комбо-множитель ×1→×2→×3** (StreakManager + начисление очков с множителем)
14. **LightCheck** (анализ яркости кадра, предупреждение при слабом освещении)
15. **MainMenu → UI Toolkit / UGUI** (замена OnGUI на нормальный UI)

## **14.3 Дальний план**

16. **ARKitBodyTracker** (iOS A12+ через AR Foundation 6)
17. **TrackerFactory + Build Profiles** (платформенное разделение трекинга)
18. **VisibilityGuard.EndLevelIncomplete()** (таймер 10 сек на завершение уровня вместо бесконечной паузы)
19. **AdaptiveCalibration** (дрифт при усталости: калибровка порогов во время игры)
20. **BeatVisualSync** (виньетка Post Processing Volume + VFX Graph пульсация)
21. **Adaptive Performance 5.x** (тепловой менеджмент, понижение качества при перегреве)
22. **StreakManager по GDD** (PlayerPrefs, даты, ежедневные награды)
23. **LevelEditorWindow** (Timeline с BPM-сеткой, drag-drop спрайтов)
24. **CosmoNavigationSystem** (центральный контроллер состояний игры)
25. **ObjectPool** для MoverItem (уход от new GameObject/Destroy per frame)
26. **ConsentScreen** (согласие на использование камеры)
27. **ReadyPlayScreen** (экран "Ты в кадре? Начинаем!")
28. **Level 3 (Ледяная орбита):** новый трек, beatMap, визуальный биом, Level3.unity

---

*Cosmo Push GDD — Version 4.1 — 2026*
*Design Document — Integrated with v4.0 prototype code (44 scripts, 2 active levels, rhythm via float[] beatMap, 2 lanes (MID hidden), WebcamPoseDetection + Sentis BlazePose, basic visual effects + заглушки для будущих механик)*
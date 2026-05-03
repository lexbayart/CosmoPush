**COSMO PUSH** — Game Design Document

Version 5.0 | Unity 6.3 LTS (6000.3.11f1) | Android 8.0+ / iOS 14+ | 2026
*Рабочий документ — интеграция текущей реализации (v4.0 prototype) и плана развития*

---

# **1. Обзор игры**

| **Параметр** | **Значение** |
| :- | :- |
| Название | Cosmo Push (рабочее название) |
| Жанр | Ритм-аркада / Фитнес-игра |
| Платформа | Android 8.0+ / iOS 14+ |
| Движок | Unity 6.3 LTS (6000.3.11f1) |
| Ориентация | Landscape (горизонтально) |
| Трекинг | WebcamPoseDetection (BlazePose через Unity Sentis) |
| Верификатор отжимания | PushupVerifier — по среднему Y плеч (11/#12), без верификации локтей |
| Количество дорожек | 2: HIGH (laneIndex=0, Y=135) и LOW (laneIndex=1, Y=-270). MID скрыта. |
| Смена дорожки | Порог среднего Y плеч: >0.5 → HIGH, <0.5 → LOW. Сглаживание через `Mathf.Lerp` ×15 |
| Размещение телефона | На ребре в landscape, 30–45° к стене/подставке, 50–80 см от лица |
| Камера | Фронтальная |
| Рендер-пайплайн | Universal Render Pipeline (URP) |
| Режимы сложности | Dropdown Easy / Normal / Hard (заглушка — не влияет на геймплей) |
| Количество уровней | 2 аудио-трека (CustomGameMusic ~112 сек, trek2 ~66.6 сек) на одной сцене Level2 |

**🟢 Текущая реализация:** Игрок кладёт телефон на пол, ложится в упор лёжа. Камера отслеживает нос (микросмещение персонажа) и плечи (смена дорожки). Космонавт в скафандре уклоняется от астероидов, собирает печеньки и звёзды.

## 1.1 Дизайн-правила

**⚠ ПРАВИЛО:** Смена дорожки — ТОЛЬКО через верифицированное отжимание. Кивки без тела игнорируются без штрафа.
**⚠ ПРАВИЛО:** Режимы сложности — «Колени» (Beginner) и «Носки» (Standard). Никаких других вариантов.
**⚠ ПРАВИЛО:** Обратная связь — только звук и визуал. Вибрация не используется (телефон на полу).

---

# **2. Игровой мир и визуал**

## 2.1 Сеттинг

Открытый космос, киберпанк-эстетика. Тёмный фон со звёздами. Parallax через `BackgroundStoneManager.cs` (263 строки) — процедурная генерация камней. Каждый уровень — отдельный визуальный биом:

| **Уровень** | **Биом** | **Трек** | **Длительность** | **BPM** |
| :- | :- | :- | :- | :- |
| 1 | Пояс астероидов | CustomGameMusic | ~112 сек | ~100 |
| 2 | Яркая туманность (обломки станций) | trek2 | ~66.6 сек | ~105 |
| 3 | Орбита ледяной планеты | — | — | Не создан |

## 2.2 Персонаж

Космонавт в скафандре — Image UI элемент с RectTransform. Вид сбоку, зафиксирован в левой трети экрана (x=300f). Компоненты:

| **Компонент** | **Строк** | **Описание** |
| :- | :- | :- |
| PlayerCharacter.cs | 243 | Позиция, микро-смещение носом, пульс на бит, стыковка с целью |
| AstroSpriteController.cs | 41 | Анимация спрайта |
| AstroTurbine.cs | 111 | Визуальный эффект хвоста турбины |
| SkinManager.cs | 50 | (Базово) — экипировка скина |

**Скины:**

| **Скин** | **Разблокировка** | **Статус** |
| :- | :- | :- |
| Космонавт | Бесплатно | ✅ Реализован |
| Инопланетянин | 3 000 очков | ⬜ Заглушка |
| Кот | 5 000 очков | ⬜ Заглушка |
| Рыба | 8 000 очков | ⬜ Заглушка |

## 2.3 Объекты

| **Объект** | **Визуал** | **Размер** | **Эффект** |
| :- | :- | :- | :- |
| Препятствие | `Resources/Emojis/Obstacles/` | 300×300, вращается | -1 кислород |
| Астероид | `emoji_u2604` (комета) | 300×300, по диагонали | -1 кислород + оранжевый шлейф |
| Печенье | `Resources/Emojis/Cookies/` | 120×120 | +10 очков, частицы |
| Звезда (ускорение) | `emoji_u2b50` | 150×150, вращается | ×1.5 speedup на 1 сек |
| Спутник-цель | `Resources/Satellite` | 800×800 | Финал уровня |

---

# **3. Механика дорожек**

## 3.1 Текущая реализация — 2 дорожки

```csharp
// LaneSystem.cs — 64 строки
float GetLaneY(int laneIndex) {
    if (laneIndex == 0) return HighY;  // Y=135
    return LowY;                        // Y=-270
}
// MID линия принудительно скрыта:
if (MidLine != null && MidLine.gameObject.activeSelf)
    MidLine.gameObject.SetActive(false);
```

| **Дорожка** | **Y** | **Цвет** |
| :- | :- | :- |
| HIGH | 135 | #00FFFF alpha 20% |
| LOW | -270 | #00FFFF alpha 20% |
| MID | — | Скрыта |

📌 Позиции жёстко заданы в инспекторе, калибровка по телу не реализована.

## 3.2 Двухслойное управление

**Слой 1 — Микродвижение носом (SmoothDamp 0.1s):**
```csharp
float rawNoseY = 1f - activeTracker.GetNose().y;
targetNoseOffset = (rawNoseY - 0.5f) * noseMicroRange; // ±60px
_noseOffset = Mathf.SmoothDamp(_noseOffset, targetNoseOffset, ref _noseVelocity, 0.1f);
```
- Нос НЕ меняет дорожку — даже при максимальном отклонении
- `NoseMicroController.cs` (отключается при PlayerCharacter)

**Слой 2 — Смена дорожки телом (PushupVerifier):**
```csharp
float leftY = poseDetection.GetKeypointY(11);
float rightY = poseDetection.GetKeypointY(12);
float rawSY = (leftY + rightY) / 2f;
_filteredSY = Mathf.Lerp(_filteredSY, rawSY, Time.deltaTime * 15f);
int detectedLane = (_filteredSY > 0.5f) ? 0 : 1;
```
- **НЕТ** верификации локтей (#13/#14)
- **НЕТ** temporal gate (150-200мс)
- **НЕТ** гистерезиса (15%)
- SmoothDamp переход персонажа 0.05-0.40s (адаптивный по BodyVelocityY)
- Cooldown 0.5 сек между отжиманиями

**Чит-детекция (PushupVerifier):**
```csharp
zDelta = avgHipZ - avgShoulderZ; // hips (23/24) vs shoulders (11/12)
if (zDelta < 80f) → "Вы стоите. ПРИМИТЕ УПОР ЛЁЖА!"
if (zDelta > 260f) → "СЛИШКОМ ВЫСОКО! ОТЖИМАЙТЕСЬ ОТ ПОЛА!"
```

## 3.3 VisibilityGuard (встроен в PushupVerifier)

| **Параметр** | **Значение** |
| :- | :- |
| Первый раз до паузы | 3.0 сек (на загрузку нейросети) |
| После трекинга | 1.5 сек вне кадра → пауза |
| Возврат | 3.0 сек в кадре → возобновление |
| Состояние | Time.timeScale=0, AudioListener.pause=true |

📌 При выходе из кадра — ПАУЗА, не LevelIncomplete.

---

# **4. Система трекинга**

## 4.1 Текущая реализация — WebcamPoseDetection

Единственный трекер на всех платформах. Синглтон (DontDestroyOnLoad).

| **Параметр** | **Значение** |
| :- | :- |
| Модели | BlazePose Detector (224×224) + Landmarker (256×256) |
| Ключевые точки | 33 точки, 5 значений каждая (x, y, z, visibility, extra) |
| Бэкенд | `Unity.InferenceEngine` (старый Sentis API, не 2.1.1) |
| Инференс | `BackendType.GPUCompute` |
| Разрешение камеры | 640×480 |
| Производительность | ~20-35 мс/кадр на Snapdragon 730+ |
| Используемые точки | #0 (нос), #11/#12 (плечи), #23/#24 (бёдра) |

## 4.2 План (не реализовано)

| **Компонент** | **Статус** |
| :- | :- |
| ARKitBodyTracker (iOS A12+) | ⬜ Не реализовано |
| SentisBodyTracker (Android) | ⬜ Замена WebcamPoseDetection |
| TrackerFactory + IBodyTracker | ⬜ Не реализован |
| KalmanFilterJob [BurstCompile] | ⬜ Не реализован |
| Low-pass фильтр 2 Hz | ⬜ Не реализован |
| AdaptiveCalibration | ⬜ Не реализован |
| LightCheck | ⬜ Пустая заглушка (10 строк) |

---

# **5. Ритм-система**

## 5.1 RhythmManager (149 строк)

```csharp
// Хардкоженные массивы:
float[] level1Map = { 0.0f, 0.6f, 1.2f, ... }; // ~190 бит
float[] level2Map = { 0.0f, 0.57f, 1.14f, ... }; // ~110 бит

// Синхронизация: AudioSource.time (не DSP-clock)
if (SongTime >= beatMap[currentBeatIndex]) {
    OnBeat?.Invoke(CurrentExpectedStateDown);
    currentBeatIndex++;
}
```

| **Где** | **Реализовано** |
| :- | :- |
| OnBeat | ✅ Да |
| OnHalfBeat | ❌ Нет |
| OnBar | ❌ Нет |
| DSP-clock | ❌ AudioSource.time |
| ScriptableObject | ❌ float[] в коде |

## 5.2 Визуальная синхронизация

- Пульсация персонажа ×1.15 на OnBeat (0.1 сек)
- Пульсация кислорода ×1.2 (×1.5 при 1 жизни)
- Ритм-рейтинг: PERFECT (<0.2s), GOOD (<0.5s), OK (<1.0s), MISS

## 5.3 Треки

| **Трек** | **Битов** | **Длительность** |
| :- | :- | :- |
| CustomGameMusic | ~190 | ~112 сек |
| trek2 | ~110 | ~66.6 сек |

---

# **6. Счёт, жизни и прогрессия**

## 6.1 Кислород

- Старт: 5 единиц
- Урон: -1 (препятствие или астероид)
- 0 → Game Over (Restart / Quit)
- Не сохраняется между уровнями

## 6.2 Очки

| **Действие** | **Очки** |
| :- | :- |
| Печенье | +10 |
| Комбо-множитель | ❌ Не реализован |
| Звёзды уровня | ❌ Не реализованы (счётчики есть, UI нет) |

## 6.3 Счётчик отжиманий

Phase.Up + дорожка LOW → Phase.Down + дорожка HIGH → отжимание засчитано. Cooldown 0.5 сек.

---

# **7. Система препятствий (LevelSpawner — 316 строк)**

| **Тип спавна** | **Level 1** | **Level 2** |
| :- | :- | :- |
| Печеньки | Каждый бит, группы 3-6 | В паузах между препятствиями |
| Препятствия | По таймеру (2-3 сек) | Каждый 4-й бит |
| Астероиды | После прохождения препятствия | После прохождения препятствия |
| Звёзды | Каждые 4-6 сек | Каждые 4-6 сек |
| Спутник | За _currentLeadTime до конца трека | За _currentLeadTime до конца трека |

**PlayerCollision (171 строка):** AABB с hitSizeFactor = 0.7 (препятствия, щадящий) / 1.2 (печеньки, бонус).

---

# #8. UI и экраны

## 8.1 Состояния игры

| **Экран** | **Статус** | **Примечание** |
| :- | :- | :- |
| MainMenu | ✅ OnGUI 5 кнопок | Start → WarmUp → Level2 |
| DifficultySelect | ⬜ Dropdown-заглушка | Easy/Normal/Hard не влияет |
| Playing | ✅ Полноценно | HUD, коллизии, спавн |
| Paused | ✅ Escape | Time.timeScale = 0 |
| LevelComplete | ⬜ Частично | SuccessPanel без звёзд |
| GameOver | ⬜ Частично | Restart / Quit |
| WarmUpLevel | ⬜ Заглушка | Completed=true, 0.5s → Level2 |
| SkinShop | ⬜ Заглушка | Debug.Log |
| Settings | ⬜ Частично | Music/SFX toggle в MainMenu |
| ConsentScreen | ❌ Нет | — |
| ReadyPlayScreen | ❌ Нет | — |
| LevelSelect | ❌ Нет | — |

## 8.2 HUD

- Счёт (левый верхний)
- Кислород — 5 сердечек (правый верхний)
- Счётчик отжиманий
- Текст ритм-хита PERFECT!/GOOD!/OK!/MISS!
- Предупреждение о читерстве (центр)
- Шкала ускорения (cyan полоса)

---

# 9. JuiceManager — визуальные эффекты

| **Эффект** | **Метод** |
| :- | :- |
| Тряска экрана | Shake(duration, amount) |
| Вспышка | Flash(color, duration) |
| Парящий текст | SpawnFloatingText(text, pos, color) |
| Летящий счёт | SpawnFlyingScore(pos, text, target) |
| Взрыв частиц | SpawnParticleBurst(pos, color, target) |
| Линии ускорения | SpawnSpeedLines(pos) |
| Шлейф астероида | SpawnAsteroidTrail(pos) |

---

# 10. Техническая архитектура

## 10.1 Структура проекта

```
Assets/
├── Audio/ — CustomGameMusic, trek2
├── Editor/ — SetupEditor, CreateMainMenuScene, SetStartupScene
├── Models/
├── Resources/
│   ├── Emojis/Cookies/  — спрайты печений
│   ├── Emojis/Obstacles/ — спрайты препятствий
│   ├── Emojis/Asteroid/ — emoji_u2604
│   ├── Emojis/Speedup/  — emoji_u2b50
│   ├── Emojis/Hearts/   — heart
│   └── Satellite        — спрайт цели
├── Scenes/ — Level2.unity, WarmUpLevel.unity
├── Scripts/ — 43 C# скрипта
├── Shaders/ — UI/OutlineEffect
├── Sprites/
└── DefaultVolumeProfile.asset
```

## 10.2 Состояние всех скриптов (43 шт)

### ✅ Работающие (18 скриптов)

| **Скрипт** | **Строк** | **Назначение** |
| :- | :- | :- |
| PlayerCharacter.cs | 243 | Персонаж: позиция, микро-смещение, пульс, стыковка |
| PushupVerifier.cs | 265 | Верификация отжиманий, смена дорожки, чит-детекция |
| LaneSystem.cs | 64 | 2 дорожки, визуальные линии |
| LevelSpawner.cs | 316 | Спавн объектов по ритму |
| PlayerCollision.cs | 171 | AABB столкновения |
| RhythmManager.cs | 149 | Ритм-система по beatMap |
| PushupUIHandler.cs | 615 | HUD, экраны успеха/поражения |
| MoverItem.cs | 247 | Движение объектов, контуры UI/Outline |
| WebcamPoseDetection.cs | 213 | BlazePose трекинг через Sentis |
| JuiceManager.cs | 324 | Визуальные эффекты |
| BackgroundStoneManager.cs | 263 | Менеджер фоновых камней |
| AstroSpriteController.cs | 41 | Анимация спрайта |
| AstroTurbine.cs | 111 | Эффект турбины |
| BlazeUtils.cs | 123 | Утилиты Sentis |
| GameInitializer.cs | 27 | Инициализация при старте |
| GameOverManager.cs | 28 | Рестарт/выход |
| OxygenIndicator.cs | 82 | Индикатор кислорода |
| PauseManager.cs | 31 | Пауза по Escape |

### 🟡 Работает с ограничениями (7 скриптов)

| **Скрипт** | **Строк** | **Назначение** |
| :- | :- | :- |
| MainMenu.cs | 89 | OnGUI-меню |
| CameraPreviewHandler.cs | 133 | Превью камеры |
| NoseMicroController.cs | 59 | Отключается при PlayerCharacter |
| SkinManager.cs | 50 | Базовое переключение скина |
| SettingsManager.cs | 57 | PlayerPrefs Music/SFX |
| StartupLoader.cs | 27 | Загрузка MainMenu |
| BGStoneMover.cs | 71 | Параллакс камня |

### 🟡 Отладка (2 скрипта)

| **Скрипт** | **Строк** | **Назначение** |
| :- | :- | :- |
| DebugGridOverlay.cs | 188 | Отладка сетки |
| ShoulderVisualizer.cs | 53 | Визуализация плеч |
| PoseSkeletonDrawer.cs | 105 | Отрисовка скелета |

### ⬜ Заглушки / Пустые (10 скриптов)

| **Скрипт** | **Строк** | **Назначение** |
| :- | :- | :- |
| AsymmetrySystem.cs | 10 | Near-miss механика |
| SymmetryBonusTracker.cs | 10 | Щит + Perfect Symmetry |
| VisibilityGuard.cs | 10 | Выход из кадра (в PushupVerifier) |
| LightCheck.cs | 10 | Проверка освещения |
| CrashReporter.cs | 10 | Firebase (не подключен) |
| DifficultySelector.cs | 24 | Dropdown не влияет |
| WarmUpLevel.cs | 20 | Мгновенный переход |
| SkinShop.cs | 32 | Debug.Log |
| SkinShopPanel.cs | 27 | UI магазина |
| SettingsPanel.cs | 27 | UI настроек |

### 📦 Инфраструктура (6 скриптов)

| **Скрипт** | **Назначение** |
| :- | :- |
| IBodyTracker.cs | Интерфейс (20 строк) |
| AnalyticsManager.cs | Firebase (97 строк, заглушка) |
| NotificationManager.cs | Push-уведомления (127 строк, заглушка) |
| RatingManager.cs | Запрос рейтинга (184 строк) |
| StreakManager.cs | Серия дней (17 строк, заглушка) |
| ComboTimer.cs | Заглушка |
| GameEndTimer.cs | Заглушка |

📌 **Editor скрипты (3):** CosmoPushSetupEditor, CreateMainMenuScene, SetStartupScene — не входят в билд.

## 10.3 Пакеты и зависимости

| **Пакет** | **Статус** |
| :- | :- |
| Unity Sentis (InferenceEngine) | ✅ Вшит, старый API |
| URP | ✅ Установлен |
| Burst Compiler | ✅ Встроен, не используется |
| Unity Jobs / Collections | ✅ Встроены, не используются |
| MCPForUnity | 🛠 Инструмент разработки |
| Newtonsoft JSON | Возможно |
| Graphy / Adaptive Perf. | ❌ Нет |

## 10.4 Производительность

- BlazePose: ~20-35 мс на Snapdragon 730+ (GPUCompute)
- ObjectPool: ❌ — `new GameObject()` / `Destroy()`
- targetFrameRate: 60

---

# 11. Roadmap — что реализовать

## 🔴 Ближайший приоритет (v5.0)

| # | **Что** | **Зачем** |
| :- | :- | :- |
| 1 | **3-я дорожка (MID)** | LaneSystem, PushupVerifier, LevelSpawner — 3 lane |
| 2 | **Верификация локтей** | Temporal gate 150-200мс, углы #13/#14 |
| 3 | **KalmanFilterJob [BurstCompile]** | Фильтрация носа + плеч |
| 4 | **WarmUpLevel** | Калибровка minY/maxY |
| 5 | **ObjectPool** | Вместо Instantiate/Destroy |

## 🟡 Средний приоритет (v5.1+)

| # | **Что** | **Зачем** |
| :- | :- | :- |
| 6 | AsymmetrySystem + near-miss | Крен по X |
| 7 | SymmetryBonusTracker + щит | 3 симметричных → поглощение урона |
| 8 | LevelCompleteScreen | Звёзды, рекорд |
| 9 | DifficultySelect | Карточки Колени/Носки |
| 10 | SkinShop | Карусель, покупка |
| 11 | Комбо-множитель ×1→×2→×3 | Геймплей |
| 12 | LightCheck | Анализ освещения |

## 🔵 Дальний план

| # | **Что** |
| :- | :- |
| 13 | ARKitBodyTracker (iOS A12+) |
| 14 | DSP-clock синхронизация |
| 15 | BeatVisualSync (VFX Graph + виньетка) |
| 16 | Adaptive Performance 5.x |
| 17 | 3-й уровень (ледяная орбита) |
| 18 | StreakManager (награды за серию) |
| 19 | LevelEditorWindow |
| 20 | CosmoNavigationSystem |
| 21 | ConsentScreen + ReadyPlayScreen |

---

*Cosmo Push GDD — Version 5.0 — 2026-05-04*
*Интегрированный документ: текущая реализация (v4.0 prototype) + план развития*
*43 скрипта (18 работают, 10 заглушек, 7 частично, 2 отладка, 6 инфраструктура)*
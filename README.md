# 🚀 CosmoPush

**Rhythm Fitness Game with Pose Tracking**

[![Unity 6.3 LTS](https://img.shields.io/badge/Unity-6.3%20LTS-222222?logo=unity)](https://unity.com/releases/editor/archive)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![BlazePose](https://img.shields.io/badge/BlazePose-Sentis-00C853?logo=tensorflow)](https://ai.google.dev/edge/mediapipe/solutions/vision/pose_landmarker)
[![Android](https://img.shields.io/badge/Android-8.0%2B-3DDC84?logo=android)](https://developer.android.com/)
[![iOS](https://img.shields.io/badge/iOS-14%2B-000000?logo=apple)](https://developer.apple.com/ios/)

---

## 🎮 Quick Description

CosmoPush is a **rhythm arcade fitness game** where your body is the controller. Place your phone on the floor, get into pushup position, and navigate a spacesuit astronaut through an asteroid field — dodging obstacles, collecting cookies and stars — all by **doing real pushups**.

No buttons. No touch input. Just you, the floor, and the beat.

---

## 🏋️ How It Works

1. **Place your phone** face-down on the floor, propped at 30-45° against a wall or stand, 50-80cm from your face
2. **Get into pushup position** — the front-facing camera tracks your nose and shoulders using **BlazePose** via Unity Sentis
3. **Micro-movements of your nose** control fine positioning (±60px) within a lane
4. **Pushups switch lanes** — your shoulders moving up/down triggers lane changes between HIGH and LOW

It's a full-body workout disguised as a rhythm game.

---

## ✨ Features

- **🎵 2 Audio Tracks** — CustomGameMusic (~112s, ~100 BPM) and trek2 (~66.6s, ~105 BPM)
- **🛤️ 2 Lanes (HIGH / LOW)** — No MID lane. Simplified, focused gameplay. Lane switching via verified pushups.
- **🤖 BlazePose AI Tracking** — 33 keypoints via Unity Sentis (`InferenceEngine` backend, GPUCompute). Tracks nose (#0), shoulders (#11/#12), and hips (#23/#24).
- **✅ Pushup Verification** — `PushupVerifier` validates real pushups using shoulder Y-average with 15× Lerp smoothing. Anti-cheat detection warns if you're standing or too high.
- **🥇 Rhythm Rating System** — PERFECT (<0.2s), GOOD (<0.5s), OK (<1.0s), or MISS based on beat sync.
- **💨 7 Visual Effects** — Screen shake, flash, floating text, flying score, particle bursts, speed lines, asteroid trails — all via `JuiceManager`.
- **🪐 Background Parallax** — Procedurally generated stone field with parallax scrolling via `BackgroundStoneManager` (263 lines).
- **❤️ Oxygen System** — 5 lives. Obstacles and asteroids drain oxygen. Game over at 0.
- **🚀 Speed Boost** — Collect stars (⭐) for a 1.5× speedup lasting 1 second.
- **🍪 Cookie Collectibles** — +10 points each, with particle effects.
- **💀 Asteroid & Obstacle Enemies** — Rotating obstacles and diagonal comet asteroids.
- **📱 No Button Controls** — Pure body tracking. See "Controls" section below.
- **👤 Astronaut Character** — Side-view UI sprite with turbine tail effect, beat-synced pulse, and skin system (Cosmonaut, Alien, Cat, Fish).

---

## 🏗️ Tech Stack

| Layer | Technology |
|-------|-----------|
| **Engine** | Unity 6.3 LTS (6000.3.11f1) |
| **Rendering** | Universal Render Pipeline (URP) |
| **AI Pose Tracking** | BlazePose Detector (224×224) + Landmarker (256×256) via Unity Sentis |
| **Languages** | C# (43 scripts) |
| **Platforms** | Android 8.0+ / iOS 14+ |
| **Orientation** | Landscape |

---

## 🧱 Architecture Overview

The codebase consists of **43 C# scripts** (18 fully working, 7 partially working, 10 stubs, 2 debug, 6 infrastructure). The core gameplay loop flows through:

```
WebcamPoseDetection → PushupVerifier → PlayerCharacter → LevelSpawner
```

### Key Components

- **`WebcamPoseDetection.cs`** (213 lines) — Singleton blaze-pose tracker. Runs BlazePose inference on 640×480 camera feed at ~20-35ms/frame on Snapdragon 730+. Extracts nose, shoulder, and hip keypoints.

- **`PushupVerifier.cs`** (265 lines) — Verifies real pushups by tracking shoulder Y-position. Detects cheating (player standing) via hip-to-shoulder Z-distance. Manages lane switching with a 0.5s cooldown. Built-in visibility guard pauses the game when the player leaves frame.

- **`PlayerCharacter.cs`** (243 lines) — The astronaut character. Manages position, micro-offset from nose tracking, beat-synced pulse animation, and docking with the level-end satellite.

- **`LevelSpawner.cs`** (316 lines) — Spawns obstacles, cookies, asteroids, stars, and the end-of-level satellite. Two distinct spawning patterns per level.

- **`LaneSystem.cs`** (64 lines) — Defines the 2 lanes: HIGH (Y=135) and LOW (Y=-270). MID line is permanently hidden.

- **`RhythmManager.cs`** (149 lines) — Beat-map driven rhythm system. Two hardcoded beat maps (~190 beats for Level 1, ~110 for Level 2). Fires `OnBeat` events for sync.

- **`PlayerCollision.cs`** (171 lines) — AABB collision detection with hit size factor (0.7 for obstacles, 1.2 for cookies).

- **`JuiceManager.cs`** (324 lines) — Visual feedback system: screen shake, flash, floating text, flying score, particle bursts, speed lines, asteroid trails.

- **`PushupUIHandler.cs`** (615 lines) — HUD, success/defeat screens, pushup counter, rhythm hit text.

- **`BackgroundStoneManager.cs`** (263 lines) — Procedural parallax background with drifting space rocks.

### Project Structure

```
Assets/
├── Audio/                    — CustomGameMusic, trek2
├── Editor/                   — SetupEditor, CreateMainMenuScene, SetStartupScene
├── Models/                   — 3D models (if any)
├── Resources/
│   ├── Emojis/Cookies/       — Cookie sprites
│   ├── Emojis/Obstacles/     — Obstacle sprites
│   ├── Emojis/Asteroid/      — Comet sprite
│   ├── Emojis/Speedup/       — Star sprite
│   ├── Emojis/Hearts/        — Heart sprite
│   └── Satellite             — End-of-level target sprite
├── Scenes/                   — Level2.unity, WarmUpLevel.unity
├── Scripts/                  — 43 C# scripts
├── Shaders/                  — UI/OutlineEffect
├── Sprites/                  — UI sprites
└── DefaultVolumeProfile.asset
```

---

## 📲 Installation

### Prerequisites

- **Unity 6.3 LTS** (6000.3.11f1) with **Android Build Support** and/or **iOS Build Support**
- **Unity Sentis** package (InferenceEngine — included)
- **Universal Render Pipeline** package (included)

### Build for Android

1. Open the project in Unity 6.3 LTS
2. File → Build Settings → Switch Platform to Android
3. Ensure `Level2` is in the build scenes
4. Player Settings:
   - Minimum API Level: 26 (Android 8.0)
   - Graphics API: Vulkan (recommended) or OpenGL ES 3.0
   - Orientation: Landscape Left
5. Build and Run

### Build for iOS

1. Open the project in Unity 6.3 LTS
2. File → Build Settings → Switch Platform to iOS
3. Player Settings:
   - Target minimum iOS Version: 14.0
   - Orientation: Landscape Left
   - Camera Usage Description: required for pose tracking
4. Build → Open in Xcode → Archive & Deploy

---

## 🎮 Controls

**Yes, this is the twist — there are NO buttons.**

| Action | How |
|--------|-----|
| **Move up/down within lane** | Tilt your head up/down (nose Y tracking, ±60px range) |
| **Switch lanes (HIGH ↔ LOW)** | Do a pushup. Verified shoulder Y-movement triggers lane change |
| **Everything else** | Automatic — rhythm-driven obstacle spawning, no player input needed |

The game uses a **two-layer control system**:

1. **Nose Micro-Movement Layer** — SmoothDamp (0.1s) filtering. Small head tilts adjust position within the current lane. Nose NEVER changes lanes.
2. **Body Pushup Layer** — Shoulder Y-average with 15× Lerp smoothing. Threshold: >0.5 → HIGH lane, <0.5 → LOW lane. Adaptive SmoothDamp (0.05-0.40s) for character movement. 0.5s cooldown between pushups.

**Anti-Cheat:** The game detects if you're standing vs. in pushup position by comparing hip-to-shoulder Z-distance. You'll see a warning if you try to cheat.

---

## 📊 Project Status — v5.0 Prototype

This is a **vertical slice prototype** — core gameplay is functional but many features are placeholders.

### ✅ Completed Systems

- BlazePose pose tracking via Unity Sentis
- Pushup verification and lane switching
- 2-lane system (HIGH/LOW, MID hidden)
- Player character with micro-offset nose tracking
- Obstacle, cookie, asteroid, and star spawning
- AABB collision detection with hit zones
- 2 audio tracks with beat-map rhythm sync
- Background parallax with procedural stones
- 7 visual effects (JuiceManager)
- HUD with oxygen, score, pushup counter, rhythm text
- Pause/resume, game over, level complete screens
- Skin system (Cosmonaut implemented, 3 stubs)
- Anti-cheat detection
- Visibility guard (pause on face-out-of-frame)

### 🟡 Partially Working

- MainMenu (OnGUI — needs UI overhaul)
- Camera preview handler
- Settings (Music/SFX toggle working)
- WarmUp level (instant transition, no calibration yet)
- Difficulty selector (dropdown stub — doesn't affect gameplay)

### 🔴 In Progress / Planned

- **Elbow verification** — temporal gate (150-200ms) and elbow angle (#13/#14)
- **Kalman filter with BurstCompile** — better noise filtering
- **WarmUp calibration** — adaptive minY/maxY for each player
- **ObjectPool** — replace Instantiate/Destroy for performance
- **AsymmetrySystem** — near-miss side-lean mechanic
- **SymmetryBonusTracker** — shield after 3 symmetric pushups
- **LevelComplete screen** — stars rating, high scores
- **Skin Shop** — carousel UI with coin purchases
- **Combo multiplier** — ×1→×2→×3 streak system
- **ARKitBodyTracker** — iOS A12+ native body tracking
- **DSP-clock sync** — replace AudioSource.time for precise beat sync
- **Level 3** — frozen planet orbit biome

---

## 📜 License

**Proprietary / All Rights Reserved**

Copyright © 2026 Lex Bayart. All rights reserved.

This source code and associated game assets are provided for demonstration purposes only. You may view and study the code, but you may not copy, modify, distribute, or use it for any commercial or non-commercial purpose without explicit written permission.

---

## 👏 Credits & Links

- **Developer:** [Lex Bayart](https://github.com/lexbayart)
- **Repository:** [github.com/lexbayart/CosmoPush](https://github.com/lexbayart/CosmoPush)
- **Pose Tracking:** BlazePose by Google MediaPipe
- **AI Inference:** Unity Sentis
- **Engine:** Unity 6.3 LTS

---

## 📸 Screenshots

> 📸 Screenshots coming soon!

We're still in early prototype phase. Screenshots and gameplay footage will be added once the visual polish pass is complete.

---

*CosmoPush — Get fit while you play. Your living room is the arcade.* 🚀
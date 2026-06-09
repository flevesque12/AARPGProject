# Classless ARPG — Unity Project (Portfolio)

A low-poly 3D isometric action RPG inspired by **Diablo 2**, **Torchlight**, and **Path of Exile 2**.
Developed solo in Unity 6, this project serves as an in-depth technical showcase of combat systems, game feel, and event-driven code architecture.

---

## Vision & Concept

Players don't pick a class. Identity emerges freely from the **7 elemental schools** they combine: Ignis, Aqua, Terra, Ventus, Lux, Umbra, Ferrum.

Combat is modeled directly after **Path of Exile 2**: dodge i-frames, block/riposte, enemy telegraphing, and a posture/stagger system. No real-time glyph-drawing gimmick — every mechanic is built around reading the game and precise timing.

---

## Tech Stack

| Component | Choice |
|---|---|
| Engine | Unity 6000.4.8f1 |
| Render pipeline | Universal Render Pipeline (URP) |
| Input | New Input System + Legacy (mode Both) |
| Player movement | CharacterController |
| Enemy AI | NavMesh + custom state machine |
| Inter-system communication | C# events (`event Action`) |

---

## Code Architecture

```
Assets/_MainProject/Scripts/
├── Core/          — GameInput, HealthSystem, HitFeedback, StaminaSystem
├── Player/        — PlayerController, CombatController, DodgeRoll,
│                    BlockSystem, RiposteSystem, SprintController, AimIndicator
├── Enemy/         — EnemyAI, EnemySpawner, PostureSystem,
│                    EnemyTelegraph, StaggerVFX
├── Camera/        — CameraController
├── UI/            — WorldHealthBar, PlayerHUD, DamageNumber, PostureBarUI
├── Editor/        — FixHeroModelHeight (Editor tool only)
└── Skills/        — (Phase 4) SkillData, SkillCaster
```

**Conventions**: PascalCase for classes/methods, `_camelCase` for private fields, `[SerializeField]` for all Inspector-exposed values, no magic numbers.

---

## Implemented Systems

### Camera — Path of Exile 2 Style
- Perspective (FOV 38°), configurable pitch angle (`pitchAngle = 60°`)
- Position computed in code (`rotation × Vector3.back × distance`) — no manual offset
- Player movement is calculated **relative to the camera**: changing `yawAngle` automatically adapts WASD directions without any hardcoded isometric matrix

### Player Movement (PlayerController)
- `CharacterController` for direct, responsive movement
- WASD / left stick → move direction; mouse / right stick → facing direction
- Configurable acceleration/deceleration (`acceleration = 50f`, `deceleration = 40f`)
- Public API: `LockMovement`, `LockRotation`, `SetSpeedMultiplier`, `MoveByDelta`, `Teleport`, `ForceFacing`
- Events: `OnMove(direction, speed)`, `OnStopMoving()`, `OnFacingChanged(direction)`

### Stamina (StaminaSystem)
- Shared endurance resource for all combat actions
- Auto-regen with delay (`regenDelay = 1.5s`), ×3 out of combat
- Consumers: Dodge (25), Block (15/hit), Sprint (10/sec)
- Events: `OnStaminaChanged`, `OnStaminaEmpty`, `OnStaminaFull`

### Combat — Strict Priority (CombatController)
`CombatController` orchestrates 5 systems with a clear priority hierarchy:

| Priority | System |
|---|---|
| 1 (highest) | **Dodge** — interrupts everything |
| 2 | **Riposte** — if window open + attack input |
| 3 | **Block** — held |
| 4 | **Basic Attack** — 3-hit combo (+20% on 3rd hit) |
| 5 | **Sprint** — when nothing else is active |

### Dodge (DodgeRoll)
- Directional dodge toward WASD/stick input, or backward if no direction held
- **I-frames**: invulnerability from `iFrameStart` (0.05s) to `iFrameStart + iFrameDuration` (0.3s)
- Movement via `PlayerController.MoveByDelta()` + `AnimationCurve` (EaseInOut)
- Stamina cost: 25 — cooldown: 1.2s
- Dodge end opens the riposte window

### Block (BlockSystem)
- **Normal block**: hold Right Click/LT → 60% damage reduction, 120° coverage, 15 stamina/hit
- **Perfect block**: timed within 0.25s of press → 90% reduction, stamina ÷2, slowmo (`timeScale = 0.15f`, 0.2s real time)
- Block broken if stamina runs out; attacks from behind bypass the angle check

### Riposte (RiposteSystem)
- Window opens after: **perfect block** or **dodge end**
- Duration: 1.0s — damage: ×2 base damage
- Cone detection (`riposteRange = 3m`, `riposteAngle = 90°`)
- Successful riposte → slowmo (`timeScale = 0.1f`, 0.15s real time) + posture damage (50% of target's postureMax)

### Sprint (SprintController)
- Speed multiplier ×1.6 via `PlayerController.SetSpeedMultiplier()`
- Costs 10 stamina/sec, requires minimum 15 stamina to start
- `ForceStopSprint()` called by `CombatController` on dodge/block/attack

### Posture / Stagger (PostureSystem)
- Posture pool on every enemy (60 basic, 120 elite)
- `DegradePosture(float)` called by `RiposteSystem` and Ferrum skills
- Stagger → ×2.5 damage multiplier + locks enemy attacks
- Events: `OnStaggerEnter`, `OnStaggerExit` (UnityEvent, wired to StaggerVFX); `OnPostureChanged` (C# event, subscribed by PostureBarUI)

### Enemy AI (EnemyAI)
- State machine: `Idle → Chase → Attack → Dead`
- Optional patrol (random points within `patrolRadius`)
- Aggro: detection range, lost at `loseAggroRange`, instant aggro if hit while Idle
- Checks `PostureSystem.IsStaggered` before entering Attack; cancels windup coroutine if staggered mid-attack

---

## Game Feel

| Effect | Implementation |
|---|---|
| Hit stop | `Time.timeScale = 0.05f` for 60ms (`WaitForSecondsRealtime`) |
| Perfect block slowmo | `timeScale = 0.15f`, 0.2s real time |
| Riposte slowmo | `timeScale = 0.1f`, 0.15s real time |
| Stagger freeze | `timeScale = 0.05f`, 0.08s on stagger entry |
| Hit flash | `HitFeedback`: white flash + scale punch (`unscaledDeltaTime` — slowmo-safe) |
| Block flash | Blue (normal), gold (perfect ×1.35), red (broken) |
| Damage numbers | `DamageNumber.Spawn()` — TextMesh created entirely in code, no prefab needed |
| Riposte indicator | `AimIndicator` pulses gold + scales ×1.5 during the riposte window |

---

## Player HUD (PlayerHUD)
- Overlay Canvas created at runtime — no manual scene setup required
- Health bar (red, centered `current / max` text)
- Stamina bar: dynamic color green → yellow → red; auto-hides when full (`CanvasGroup`, 3s delay); red flash on insufficient stamina
- Action bar (bottom-left): live keybinding display
- Pulsing gold "RIPOSTE !" label during the riposte window

---

## TTK Targets (GDD v3.0)

| Element | Target value |
|---|---|
| Basic enemy HP | 80–120 |
| Elite enemy HP | 300–450 |
| Player base damage | 20–30 per hit |
| Riposte damage | 50–70 (×2) |
| Player HP | ~100 (survives 5–6 hits without dodging) |
| Basic enemy damage | 15–20 |
| Elite enemy damage | 25–35 |

---

## Progress

| Phase | Content | Status |
|---|---|---|
| Phase 1 | Movement, enemies, health, PoE2 camera, game feel | Done |
| Phase 2 | Common Skills — dodge, perfect block, riposte, sprint, StaminaSystem, PlayerHUD | Done |
| Phase 3 | Enemy telegraphing + Posture/Stagger system | Done |
| Phase 4 | First school — Ignis (4 active skills) + SkillData ScriptableObject | Planned |
| Phase 5 | Tissage Arcanique — 2 proc-slots, 3 triggers (OnDodge, OnKill, OnPerfectBlock) | Planned |
| Phase 6 | Basic loot (rarity, affixes, sockets) | Planned |
| Phase 7 | Second school + Layer B synergies (Condition Chains) | Planned |
| Phase 8 | First complete zone (Caldera) + Rune Gate | Planned |

---

## About

Solo personal project — iterative development with an evolving design document (GDD v3.0).
Goal: demonstrate mastery of complex combat systems, event-driven C# architecture, and game feel polish in Unity.

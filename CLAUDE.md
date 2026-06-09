# ARPG Classless — Unity Project

## Description
Isometric action-RPG in low poly 3D, inspired by Diablo 2, Torchlight, and Path of Exile 2.
Classless system: the player freely combines skills from 7 elemental schools (Ignis, Aqua,
Terra, Ventus, Lux, Umbra, Ferrum). No class selection — identity emerges from school choices.

**Design pivot (GDD v3.0):** No real-time glyph drawing mechanic. Combat follows Path of Exile 2
model (dodge i-frames, block/riposte, enemy telegraph, stagger/posture system).
The Tissage Arcanique is a passive proc-slot system configured outside combat (like Grim Dawn's
Devotion system), NOT active glyph drawing during fights.

## Architecture
- Unity 6000.4.8f1
- Universal Render Pipeline (URP)
- New Input System + Legacy (mode Both)
- NavMesh for enemies (EnemyAI); player uses **CharacterController**

## Folder Structure
- Assets/_MainProject/Scripts/Core/       — GameInput, HealthSystem, HitFeedback, StaminaSystem
- Assets/_MainProject/Scripts/Player/     — PlayerController, CombatController, DodgeRoll,
                                            BlockSystem, RiposteSystem, SprintController, AimIndicator
- Assets/_MainProject/Scripts/Enemy/      — EnemyAI, EnemySpawner, PostureSystem,
                                            EnemyTelegraph (Phase 3), StaggerVFX (Phase 3)
- Assets/_MainProject/Scripts/Camera/     — CameraController
- Assets/_MainProject/Scripts/UI/         — WorldHealthBar, PlayerHUD, DamageNumber, PostureBarUI
- Assets/_MainProject/Scripts/Editor/     — FixHeroModelHeight (Editor tool only)
- Assets/_MainProject/Scripts/Skills/     — SkillData, SkillCaster, SkillProjectile
- Assets/_MainProject/Data/Skills/        — Ignis_TraitDeBraise, Ignis_Explosion, Ignis_MurDeFeu, Ignis_Meteore
- Assets/_MainProject/Prefabs/VFX/Ignis/  — VFX prefabs (particle systems) pour les 4 skills Ignis
- Assets/_MainProject/Prefabs/            — Player, enemy, effect prefabs
- Assets/_MainProject/Models/             — HeroCharacter.glb (base), HeroCharacter_Rigged.glb
- Assets/_MainProject/Models/Animations/  — Hero_Idle.glb, Hero_Walk.glb, Hero_Attack.glb
- Assets/_MainProject/Animations/         — HeroAnimator.controller

## Code Conventions
- Naming: PascalCase for classes and methods, _camelCase for private fields
- **Comments in English**
- SerializeField for everything that must be visible in the Inspector
- C# events (event Action) for inter-system communication
- No magic numbers — all tuning values in [SerializeField] fields

## Technical Notes

### Camera — Path of Exile 2 Style
- `CameraController` placed on an empty `CameraRig` GameObject; Main Camera is a child with reset transform
- **Perspective** (not orthographic), FOV 38° default
- `pitchAngle = 60°` (tilt toward ground, adjust 55–65 to vary)
- `yawAngle = 0°` (straight north view, not diagonal)
- Zoom adjusts `distance` (not `orthographicSize`) — range 18–48 default
- Position calculated in code (`rotation * Vector3.back * distance`): do not set manual offset on Main Camera
- Inspector params: `pitchAngle`, `yawAngle`, `distance`, `minDistance`, `maxDistance`, `fieldOfView`, `followSpeed`

### Camera — Input Consistency
- `PlayerController` calculates movement direction **relative to camera** via `camera.transform.forward/right` projected on horizontal plane
- No hardcoded `isoMatrix`: direction always matches screen regardless of `yawAngle`
- Changing `yawAngle` automatically adapts movement direction

### Player Character — Scene Setup
- `Player`: **CharacterController**, PlayerController, CombatController, DodgeRoll, BlockSystem,
  RiposteSystem, SprintController, StaminaSystem, HealthSystem
- `Player/HeroModel`: rigged Meshy model (`HeroCharacter_Rigged.glb`), Animator, SkinnedMeshRenderer
- **Important**: NavMeshAgent removed from player — use `CharacterController.Move()` / `PlayerController.MoveByDelta()` for all external movement (dodge, knockback, etc.)

### Player — Movement (PlayerController)
- Uses **CharacterController**: direct, responsive movement
- WASD / left stick → movement direction
- Mouse / right stick → facing direction
- Direction calculated relative to camera (`camForward * moveInput.y + camRight * moveInput.x`)
- Configurable acceleration/deceleration (`acceleration = 50f`, `deceleration = 40f`)
- `speedMultiplier` modifiable by `SprintController`, buffs, debuffs
- Public API: `LockMovement(bool)`, `LockRotation(bool)`, `SetSpeedMultiplier(float)`,
  `MoveByDelta(Vector3)`, `Teleport(Vector3)`, `ForceFacing(Vector3)`
- Rotation: `Quaternion.Slerp` toward mouse at `rotationSpeed = 20f`, or instant snap if `instantRotation = true`
- Events: `OnMove(direction, speed)`, `OnStopMoving()`, `OnFacingChanged(direction)`

### Player — Animations
- Animations driven from `CombatController` / `PlayerController` via `Animator` (GetComponentInChildren)
- `HeroAnimator.controller`: Idle (default) / Walk / Attack states
  - Idle ↔ Walk: conditioned by Speed > 0.1 / < 0.1, transition 0.15s
  - AnyState → Attack: trigger Attack, no exit time, transition 0.05s
  - Attack → Idle: exit time at 85% of animation, transition 0.15s

### Input — GameInput
- Auto-detects keyboard/mouse vs gamepad (`lastGamepadInput` vs `lastKeyboardInput`)
- Delegates inputs to `PlayerController` (movement/aim) and `CombatController` (actions)
- Keyboard layout: WASD movement, Mouse facing, Left Click attack, **Space dodge**,
  **Right Click block**, **Left Shift sprint**
- Gamepad layout: Left Stick movement, Right Stick aim, X/Square attack,
  A/Cross dodge, LT block, LB sprint

### Stamina — StaminaSystem
- Shared endurance resource for all Common Skills actions
- Auto-regen with delay (`regenDelay = 1.5s`) after consumption
- Regen ×3 out of combat (`outOfCombatRegenMultiplier`), slowed via `SetInCombat()`
- Consumers: `DodgeRoll` (25), `BlockSystem` (15/blocked hit), `SprintController` (10/sec)
- API: `ConsumeStamina(amount)→bool`, `DrainStamina(amountPerSec)→bool`,
  `RestoreStamina(amount)`, `FillStamina()`, `SetInCombat()`
- Events: `OnStaminaChanged(current, max)`, `OnStaminaEmpty()`, `OnStaminaFull()`

### Combat — Common Skills (CombatController)
- Orchestrates combat systems with **strict priority**:
  1. **Dodge** (DodgeRoll) — interrupts everything
  2. **Riposte** (RiposteSystem) — if window open + attack input
  3. **Block** (BlockSystem) — held
  4. **Basic Attack** — 3-hit combo (+20% on 3rd hit)
  5. **Sprint** (SprintController) — when nothing else active
- Basic attack: cone detection (`attackAngle = 80°`, `attackRange = 2.5f`), cooldown 0.5s, combo window 0.8s
- Input API: `OnDodgeInput(moveDir)`, `OnBlockInput(bool)`, `OnAttackInput()`, `OnSprintInput(bool)`
- `FilterIncomingDamage(rawDamage, attackerPos)`: call from HealthSystem before applying damage
  (handles i-frames + block)

### Combat — DodgeRoll
- Directional dodge in WASD/stick direction, or backward if no direction input
- **I-frames**: invulnerability from `iFrameStart` (0.05s) to `iFrameStart + iFrameDuration` (0.3s)
- Movement via `PlayerController.MoveByDelta()` with `AnimationCurve` (EaseInOut)
- Locks PlayerController movement and rotation during dodge
- Stamina cost: 25 — cooldown: 1.2s
- Events: `OnDodgeStart()`, `OnDodgeEnd()`, `OnInvulnerabilityChanged(bool)`
- Dodge end opens riposte window (`RiposteSystem`)

### Combat — BlockSystem
- **Normal block**: hold Right Click/LT → 60% damage reduction, 120° coverage angle, 15 stamina/hit
- **Perfect Block**: timing within 0.25s of pressing button → 90% reduction, stamina ÷2,
  slowmo `timeScale = 0.15f` for 0.2s (real time)
- Block broken if insufficient stamina (full damage)
- Attacks from behind bypass block (angle check)
- Events: `OnBlockStart()`, `OnBlockEnd()`, `OnPerfectBlock()`, `OnBlockHit(residualDamage)`, `OnBlockBroken()`

### Combat — RiposteSystem
- Riposte window opens after: **Perfect Block** or **dodge end**
- Window duration: 1.0s — damage: ×2 base damage
- Cone detection (`riposteRange = 3f`, `riposteAngle = 90°`)
- Successful riposte triggers brief slowmo (`timeScale = 0.1f`, 0.15s real time)
- Riposte calls `PostureSystem.DegradePosture(postureMax * 0.5f)` on hit target (implemented)
- Events: `OnRiposteWindowOpen()`, `OnRiposteWindowClose()`, `OnRiposteHit(gameObject, damage)`

### Combat — SprintController
- Speed modifier ×1.6 via `PlayerController.SetSpeedMultiplier()`
- Costs 10 stamina/sec, requires minimum 15 stamina to start
- Auto-stops if stamina depleted or higher-priority action triggers
- `ForceStopSprint()` called by `CombatController` on dodge/block/attack

### Combat — Game Feel
- **Hit stop**: `Time.timeScale = 0.05f` for 60ms on successful hit (CombatController/RiposteSystem)
  via `WaitForSecondsRealtime`
- **Perfect Block slowmo**: `Time.timeScale = 0.15f` for 0.2s real time (BlockSystem)
- **Riposte slowmo**: `Time.timeScale = 0.1f` for 0.15s real time on hit
- **Stagger freeze**: `Time.timeScale = 0.05f` for 0.08s on enemy stagger entry (PostureSystem.StaggerCoroutine)
- `HitFeedback` uses `Time.unscaledDeltaTime` for flash lerp (compatible with all slowmo)
  - Damage hit: white flash + scale punch
  - Block hit (`OnBlockHit`): blue flash, light punch
  - Perfect block (`OnPerfectBlock`): gold flash, stronger punch (×1.35)
  - Block broken (`OnBlockBroken`): red flash
  - All block events auto-wired via `GetComponent<BlockSystem>()` in Awake — no setup needed on enemies
- `DamageNumber`: fully created by code (TextMesh), no prefab needed — called via `DamageNumber.Spawn()`

### Combat — TTK Targets (GDD v3.0)
Target values to preserve during balancing. Do NOT change without explicit instruction.
- **Act 1 basic enemy**: 80–120 HP, dies in 3–5 player hits
- **Act 1 elite enemy**: 300–450 HP, dies in 10–15 player hits
- **Player base damage**: 20–30 per hit
- **Riposte damage**: 50–70 (×2 base)
- **Player HP**: ~100 HP (survives 5–6 basic enemy hits without dodging)
- **Basic enemy damage**: 15–20 per hit
- **Elite enemy damage**: 25–35 per hit

### Aim — AimIndicator
- Displays a ground reticle at mouse aim position (reads `PlayerController.AimWorldPosition`)
- Default white semi-transparent, red when hovering an enemy
- **Riposte window**: cursor pulses gold (`riposteWindowColor`) + scales up ×`riposteSizeMultiplier` (1.5f)
  via `Time.unscaledTime` — auto-detected by polling `RiposteSystem.IsRiposteWindowOpen`
- Works without prefab: creates a projected Quad on ground if `cursorPrefab` is null
- Optional aim line (`showAimLine`) via `LineRenderer`
- `SetVisible(bool)` to hide during menus/cutscenes

### UI — PlayerHUD
- Auto-creates overlay Canvas at runtime (no manual setup) — place on same GameObject as player
- **Health bar** (top-left): configurable red color, centered `current / max` text
- **Stamina bar**: positioned below health bar (`barGap = 5px`), independent height (`staminaBarHeight = 18f`)
  - Dynamic color: green (>50%) → yellow (25–50%) → red (<25%), smooth transition
  - Red flash when stamina insufficient for an action
  - Auto-hide via `CanvasGroup` (`hideDelay = 3s`) when stamina is full
  - Smoothed via `Mathf.Lerp` each frame (`smoothSpeed = 8f`)
- **Action bar** (bottom-left): shows player keybindings, bottom to top:
  `[LMB]` Attack / `[Space]` Dodge / `[RMB]` Block / `[Shift]` Sprint
- **Riposte indicator**: gold pulsing label "RIPOSTE !" appears above the action bar when riposte window is open;
  fades out on close — subscribes to `RiposteSystem.OnRiposteWindowOpen/Close`
- ⚠ `StaminaBarUI` is superseded by `PlayerHUD` — do not use both simultaneously

### Enemy AI — EnemyAI
- State machine: `Idle → Chase → Attack → Dead`
- **Optional patrol** (`enablePatrol`): moves to random points within `patrolRadius` from spawn
- **Aggro**: detection at `detectionRange`, aggro lost at `loseAggroRange`, instant aggro if hit while Idle
- **Telegraph windup**: temporary scale-up (×1.1) during `attackWindup` before damage is applied
  → Phase 3 replaces this with EnemyTelegraph visual indicators
- **Loot drop**: `lootDropPrefabs[]` + `dropChance` (0–1), random drop on death
- **Phase 3 addition**: check `PostureSystem.IsStaggered` before entering Attack state;
  cancel attack coroutine if staggered during windup

### Enemy — PostureSystem ✓ implemented
- Attached on enemy GameObjects alongside HealthSystem
- `_postureMax`: total posture pool (basic = 60f, elite = 120f)
- `_postureRegenRate`: regen per second (basic = 10f, elite = 6f)
- `_staggerDuration`: stagger state duration (basic = 1.5f, elite = 2.5f)
- `_damageMultiplierWhenStaggered = 2.5f`: damage multiplier during stagger
- `DegradePosture(float amount)`: public method called by RiposteSystem and Ferrum skills
- `IsStaggered { get; }`: read by EnemyAI to cancel attacks
- `PosturePercent { get; }`: 0–1 ratio, read by PostureBarUI
- `PostureMax { get; }`: read by RiposteSystem to compute 50% posture damage
- Events: `OnStaggerEnter`, `OnStaggerExit` (UnityEvent, Inspector-wired to StaggerVFX);
  `OnPostureChanged(current, max)` (C# event, subscribed by PostureBarUI)
- **Riposte deals 50% of postureMax** as posture damage (2 ripostes = stagger on basic enemy)
- Posture regens only while not staggered; resets to full on stagger exit

### Enemy — EnemyTelegraph (Phase 3)
- Three telegraph types: `Circle` (0.3s windup), `Cone` (0.5s), `FullBoss` (0.8–1.2s)
- `ShowTelegraph(TelegraphType, float duration, Vector3 direction)` coroutine
- Visual: red semi-transparent indicator (Color.red, alpha 0.6f), scales 0.8→1.0 during windup
- Circle: LineRenderer ring drawn on ground centered on enemy, configurable `_telegraphRadius`
- Cone: arc LineRenderer in attack direction, `_telegraphConeAngle = 60°`, `_telegraphLength = 4m`
- `Telegraph(float duration)` shortcut uses default type
- Indicator destroyed after coroutine ends; attack applies damage only after full windup

### Enemy — PostureBarUI ✓ implemented
- Quad-based world-space bar (same pattern as WorldHealthBar), billboard in LateUpdate
- Positioned at `_yOffset = 2.05f` — just below WorldHealthBar (2.2f)
- Hidden at spawn; auto-hides when `posture == full AND Time.time - lastDegradeTime >= _hideDelay (3s)`
- Amber fill (`#D89919`), pulses amber→white when posture ≤ 30% via `Time.unscaledTime` (slowmo-safe)
- Shows "STAGGER" TextMesh for `_staggerTextDuration = 1s` on OnStaggerEnter, hides fill quad; restores after
- Subscribes to `PostureSystem.OnPostureChanged` (C# event) + `OnStaggerEnter/Exit` (UnityEvent.AddListener)

## Completed Phases
- **Phase 1**: WASD+mouse movement (CharacterController), basic enemies, health, camera PoE2 style,
  game feel (hit stop, slowmo, damage numbers, AimIndicator)
- **Phase 2**: Full Common Skills — dodge with i-frames, perfect block with slowmo, riposte,
  sprint, StaminaSystem, PlayerHUD
- **Phase 3**: Enemy Telegraph + Posture/Stagger System — PostureSystem, PostureBarUI,
  EnemyTelegraph (Circle/Cone/FullBoss), StaggerVFX, EnemyAI stagger integration
- **Phase 4**: First School Ignis — SkillData (ScriptableObject), SkillCaster, SkillProjectile,
  4 skills actifs (Trait de Braise / Explosion Ignis / Mur de Feu / Météore Ignis),
  VFX particle systems procéduraux, barre de skills HUD (slots 1–4 avec cooldown overlay),
  TTK calibration (baseDamage 25, enemyDamage 17, enemyHP 100)

## Current Phase
**Phase 5 — Tissage Arcanique**

Remaining:
- 2 proc-slots configurables hors combat
- 3 triggers : `OnDodge`, `OnKill`, `OnPerfectBlock`
- Système de devotion style (Grim Dawn) — passif, pas d'action pendant les combats

## Next Phases
- **Phase 5**: Tissage Arcanique — 2 proc-slots, 3 triggers (OnDodge, OnKill, OnPerfectBlock)
- **Phase 6**: Basic loot (rarity, affixes, sockets)
- **Phase 7**: Second school + Couche B synergies (Condition Chains)
- **Phase 8**: First complete zone (Caldera) + Rune Gate

# L'Art des Glyphes — Unity Portfolio Project

A toon-fantasy, third-person **spell-crafting** action-adventure inspired by **Mages of
Mystralia**, **Magicka 2**, **Magic and Mayhem**, and **Zelda: Tears of the Kingdom**.
Developed solo in Unity 6, this project is a technical showcase of a data-driven spell
system, event-driven C# architecture, custom shader work, and game feel.

> **Design pivot (mid-project):** this repo started as a dark, isometric Diablo/PoE2-style
> ARPG with block/riposte/posture combat. Roughly halfway through, the design pivoted to a
> colorful, third-person spell-crafting adventure — a deliberate reset once the original
> direction stopped serving the actual gameplay goal. Phase 1–4 below are that earlier
> iteration; most of their combat systems (block, riposte, posture/stagger) have since been
> retired in favor of spell-based combat. Kept here as evidence of the iteration, not as
> the current design.

---

## Vision & Concept

The player is a young mage who learns magic by **crafting custom spells** — combining a
base form, an elemental school, and modifier runes in a **Grimoire**. Magic serves combat
*and* daily life: helping villagers, solving environmental puzzles, exploring ancient
Libraries.

- **Spell crafting, not a spell menu.** Spells are built (Form + School + Runes), never
  picked from a list.
- **Combat is a creative puzzle**, not a reflex test — no block/riposte/posture-bar
  reaction combat anymore.
- **Toon fantasy, never dark.** Colorful, warm, cel-shaded — a deliberate contrast to the
  project's earlier isometric-ARPG direction.
- Identity emerges from the **7 elemental schools** — Ignis, Aqua, Terra, Ventus, Lux,
  Umbra, Ferrum — combined through spell crafting rather than picked as a class.

---

## Tech Stack

| Component | Choice |
|---|---|
| Engine | Unity 6000.4.8f1 |
| Render pipeline | Universal Render Pipeline (URP) + hand-written HLSL toon/cel shader |
| Camera | Cinemachine — fixed-angle top-down, 100% game-controlled (no player input) |
| Input | New Input System + Legacy (mode Both) |
| Player movement | CharacterController |
| Enemy AI | NavMeshAgent + custom state machine |
| Inter-system communication | C# events (`event Action`), ScriptableObjects for design data |

---

## Code Architecture

```
Assets/_MainProject/Scripts/
├── Core/          — GameInput, HealthSystem, HitFeedback, ManaSystem, StaminaSystem
├── Player/        — PlayerController, PlayerCombat, DodgeRoll, SprintController,
│                    CombatVisualFeedback
├── Enemy/         — EnemyAI, EnemySpawner, EnemyTelegraph
├── Camera/        — ThirdPersonCamera (Cinemachine wrapper)
├── UI/            — WorldHealthBar, PlayerHUD, DamageNumber
├── Skills/        — SkillData, SkillCaster, SkillProjectile (v3.1 system, Ignis school —
│                    still active in parallel while SpellCraft/ takes over, see below)
├── SpellCraft/    — v4.0 spell-crafting core (Phase 6, in progress)
│   ├── Data/      — SpellRecipe, RuneModifier (abstract), BaseFormData, SchoolData,
│   │                SpellEnums
│   └── Runtime/   — SpellFactory, ModifierProcessor, ISpellModifier, SpellContext
├── Shaders/       — ToonCel.shader (custom URP cel-shader, hand-written HLSL)
├── Editor/        — FixHeroModelHeight (Editor tool only)
└── _Archive/      — Retired v3.1 systems, kept for reference (see Progress below)
```

**Conventions**: PascalCase for classes/methods, `_camelCase` for private fields,
`[SerializeField]`/`[Header]`/`[Tooltip]` for all Inspector-exposed values, no magic
numbers, no singletons (Inspector-driven dependency injection).

---

## Implemented Systems

### Camera (ThirdPersonCamera)
- Cinemachine, top-down elevated angle, **fully game-controlled** — no mouse/stick input
  drives it at all (mouse is reserved for spell aiming instead)
- Fixed pitch (50°) in **world space** (`CinemachineFollow.TrackerSettings.BindingMode =
  WorldSpace`) — position tracks the player with damping, angle never swings around when
  the player turns
- `CinemachineRotationComposer` + `CinemachineDeoccluder` for smooth framing and obstacle
  avoidance

### Player Movement (PlayerController)
- `CharacterController`-driven, camera-relative WASD movement (`moveSpeed = 8`,
  `acceleration = 50`, `deceleration = 40`)
- Two facing modes: `Movement` (faces move direction, default) and `Aim` (faces the mouse
  raycast on the ground, driven by `PlayerCombat` during a cast)
- Public API: `LockMovement`, `LockRotation`, `SetSpeedMultiplier`, `MoveByDelta`,
  `Teleport`, `SetFacingMode`

### Mana & Stamina
- `ManaSystem` — the casting resource (`maxMana = 100`, regen with delay, ×3 out-of-combat)
- `StaminaSystem` — kept for Dodge (25) and Sprint (10/sec) only; Block no longer consumes
  it (block was removed in the pivot)

### Dodge (DodgeRoll)
- Directional dodge toward WASD/stick input, or backward if no direction held
- I-frames: `iFrameStart = 0.05s` → `iFrameStart + iFrameDuration` (0.2s, tightened from
  0.3s during the pivot)
- Movement via `PlayerController.MoveByDelta()` + `AnimationCurve` (EaseInOut)

### Sprint (SprintController)
- Speed multiplier ×1.6, 10 stamina/sec, minimum 15 stamina to start
- Wired directly from `GameInput` (the old `CombatController` middleman was retired)

### Spell Casting — v3.1 system (Skills/, Ignis school, 4 active skills)
Still the live in-game casting path while the v4.0 spell-crafting core (below) is built out.

| Skill | Type | Mana | Cooldown | Damage | Notes |
|---|---|---|---|---|---|
| Trait de Braise | Projectile | 15 | 0.8s | 35 | Speed 20, OverlapSphere hit detection |
| Explosion Ignis | AoE | 25 | 2s | 55 | Radius 2.5, 0.35s windup telegraph |
| Mur de Feu | PersistentZone | 30 | 5s | 12/tick | 4s duration, tick every 0.4s |
| Météore Ignis | DelayedAoE | 40 | 8s | 120 | Radius 3.5, 1.2s telegraph |

- `SkillCaster` (4 hotkey slots) reads `ManaSystem`, blocked during dodge via
  `PlayerCombat.CanAct`, bridges `PlayerController.SetFacingMode(Aim)` during the cast
- All VFX procedural (particle systems built entirely in code, no external assets)

### Spell Crafting Core — v4.0 (SpellCraft/, foundations in progress)
The system that will replace the table above. Currently: full data schema + instantiation
pipeline, no gameplay yet.

- `SpellRecipe` (ScriptableObject) — composes a `BaseFormData` + `SchoolData` + up to 4
  `RuneModifier`; `ManaCost`/`CooldownTime` are **computed properties**
  (`base × Π(1 + rune.multiplier)`), never stale
- `RuneModifier` — abstract ScriptableObject base implementing `ISpellModifier`; concrete
  runes (Bounce, Homing, Split, Persist, …) will be subclasses overriding `OnSpawn`
- `SpellFactory` + `ModifierProcessor` — builds the spell's root GameObject + `SpellContext`
  and fires each rune's `OnSpawn` hook
- Seed data assets: 4 base forms (Projectile/Zone/Aura/Impact), all 7 schools with their
  palette, one example recipe
- **Not yet built**: concrete rune behaviors, the 4 base forms' actual visual/physics
  behavior, the Grimoire crafting UI, environmental synergies

### Custom Cel-Shader (Shaders/ToonCel.shader)
Hand-written URP HLSL (not Shader Graph — see Progress notes) implementing the toon look:
- Banded (stepped) diffuse lighting via `smoothstep` on `NdotL × shadowAttenuation`
- Ambient floor so the shadow band never reads pure black — matches the "never dark, never
  grim" art direction
- Banded specular highlight + a warm rim light
- Full `ForwardLit` + `ShadowCaster` passes

### Enemy AI (EnemyAI)
- State machine: `Idle → Chase → Attack → Dead`, `NavMeshAgent`-driven
- Optional patrol, aggro detection/lose ranges, instant aggro on hit while Idle
- No more posture/stagger checks (that system was removed in the pivot)

---

## Game Feel

| Effect | Implementation |
|---|---|
| Hit stop | `Time.timeScale = 0.05f` for 60ms (`WaitForSecondsRealtime`) |
| Hit flash | `HitFeedback`: white flash + scale punch (`unscaledDeltaTime` — slowmo-safe) |
| Damage numbers | `DamageNumber.Spawn()` — TextMesh created entirely in code, no prefab needed |
| Dodge/Sprint visuals | `CombatVisualFeedback` — trail renderer + capsule squash/stretch/tint, scoped down to just these two after the pivot (sword-swing and shield visuals removed with the systems they represented) |

---

## Player HUD (PlayerHUD)
- Overlay Canvas created at runtime — no manual scene setup required
- HP bar (top-left) + Mana bar (single color, no tiered flash)
- 4 spell slot icons with cooldown overlay + school color tint
- No more stamina bar, riposte indicator, or keybinding action bar (retired with those
  systems)

---

## Progress

| Phase | Content | Status |
|---|---|---|
| 1 | Core movement, enemy AI, health, isometric PoE2-style camera, game feel | Done (v3.1) |
| 2 | Common combat — dodge, block/riposte, sprint, stamina, HUD | Done (v3.1) — block/riposte/posture-adjacent pieces retired in the pivot |
| 3 | Enemy telegraphing + posture/stagger | Done (v3.1) — posture/stagger system retired in the pivot |
| 4 | First school — Ignis, 4 active skills, SkillData ScriptableObject | Done — still the live casting path |
| 5 | **Design pivot**: Cinemachine 3rd-person camera, ManaSystem, PlayerCombat, retire CombatController/Block/Riposte/Posture, custom cel-shader | **Done** |
| 6 | Spell Crafting Core — SpellRecipe/RuneModifier/SpellFactory | **In progress** — data foundations + instantiation pipeline done; base-form behaviors, concrete runes, and Grimoire UI still to come |
| 7 | Village hub + first Library (dungeon) | Planned |
| 8 | Spell crafting complete — all 7 schools, 16 runes, synergies, full Grimoire | Planned |
| 9 | Expanded world — 3 more zones + Libraries | Planned |
| 10 | Full content — all zones, all Libraries, complete narrative | Planned |
| 11 | Polish & launch — final art pass, audio, balancing, accessibility | Planned |

---

## About

Solo personal project — iterative development with an evolving design document (now GDD
v4.0). The mid-project pivot from isometric ARPG to third-person spell-crafting adventure
is intentional and documented above: goal is to demonstrate not just implementation depth,
but the judgment to recognize when a design direction isn't working and rebuild on top of
what's still reusable (movement, health, enemy AI, event architecture) rather than starting
over from zero.

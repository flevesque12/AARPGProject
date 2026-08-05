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

**Third-party assets**: `Assets/3rdParty/` (currently a single character model pack used
for the player) is `.gitignore`d and not part of this repo — reimport it from its original
source after cloning. Everything under `Assets/_MainProject/` (scripts, ScriptableObject
data, materials, the custom shader) is original work.

---

## Code Architecture

```
Assets/_MainProject/Scripts/
├── Core/          — GameInput, HealthSystem, HitFeedback, HitStop, ManaSystem, StaminaSystem
├── Player/        — PlayerController, PlayerCombat, DodgeRoll, SprintController,
│                    CombatVisualFeedback
├── Enemy/         — EnemyAI, EnemySpawner, EnemyTelegraph
├── Camera/        — ThirdPersonCamera (Cinemachine wrapper)
├── UI/            — WorldHealthBar, PlayerHUD, DamageNumber
├── Grimoire/      — GrimoireUI, CraftingPanel, CraftingNode — draggable node-graph
│                    spell-crafting UI (v4.0)
├── Skills/        — SkillData, SkillCaster, SkillProjectile (v3.1 system, Ignis school —
│                    still active on hotkeys 3-4 while SpellCraft/ owns 1-2, see below)
├── SpellCraft/    — v4.0 spell-crafting core (Phase 6 — complete)
│   ├── Data/      — SpellRecipe, RuneSlot (rune + continuous intensity), RuneModifier
│   │                (abstract) + 4 concrete runes (Bounce, Split, Persist, Expand),
│   │                BaseFormData, SchoolData, SpellEnums
│   ├── Runtime/   — SpellFactory, ModifierProcessor, ISpellModifier, SpellContext,
│   │                SpellCaster, SpellImpactVFX (procedural hit-burst), the 4 base-form
│   │                behaviors (ProjectileSpell, ZoneSpell, AuraSpell, ImpactSpell),
│   │                SchoolEffectApplier + status effects (BurnStatus, SlowStatus, Knockback)
│   └── Synergies/ — EnvironmentState (terrain-patch tracking, same-type patches stack in
│                    intensity instead of duplicating)
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

### Player Model & Animation (PlayerAnimator)
- Visual: a 3rd-party humanoid model (see *Third-Party Assets* below), reskinned with the
  project's own hand-written `ToonCel` shader (its stock materials target Built-in RP and
  render incorrectly under URP)
- `Wizard.controller` — a single `Speed` float drives a 1D locomotion blend tree
  (Idle/Walk/Run); `PlayerAnimator` is the **only** script that writes to the `Animator`
- Two one-shot gestures, both wired through existing C# events rather than any direct
  Animator coupling in the gameplay scripts:
  - **Cast** — `PlayerCombat.OnSpellCast(slotIndex)` → a per-slot casting gesture (4
    distinct animations, one per hotbar slot)
  - **Dodge** — `DodgeRoll.OnDodgeStart` → a teleport-blink gesture whose *playback speed*
    is recalculated every dodge (`clipLength / DodgeRoll.DodgeDuration`) so it always
    matches the actual i-frame window, even if the dodge duration is retuned later

### Mana & Stamina
- `ManaSystem` — the casting resource (`maxMana = 100`, regen with delay, ×3 out-of-combat)
- `StaminaSystem` — kept for Dodge (25) and Sprint (10/sec) only; Block no longer consumes
  it (block was removed in the pivot)

### Dodge (DodgeRoll)
- Directional dodge toward WASD/stick input, or backward if no direction held
- I-frames: `iFrameStart = 0.05s` → `iFrameStart + iFrameDuration` (0.2s, tightened from
  0.3s during the pivot)
- Movement via `PlayerController.MoveByDelta()` + `AnimationCurve` (EaseInOut)
- Visual: a teleport-blink gesture rather than a physical roll — see *Player Model &
  Animation* above

### Sprint (SprintController)
- Speed multiplier ×1.6, 10 stamina/sec, minimum 15 stamina to start
- Wired directly from `GameInput` (the old `CombatController` middleman was retired)

### Spell Casting — v3.1 system (Skills/, Ignis school, 4 active skills)
Still live on hotkeys 3-4 (a deliberate coexistence with the v4.0 system below, not a
final keybinding — see Spell Crafting Core).

| Skill | Type | Mana | Cooldown | Damage | Notes |
|---|---|---|---|---|---|
| Trait de Braise | Projectile | 15 | 0.8s | 35 | Speed 20, OverlapSphere hit detection |
| Explosion Ignis | AoE | 25 | 2s | 55 | Radius 2.5, 0.35s windup telegraph |
| Mur de Feu | PersistentZone | 30 | 5s | 12/tick | 4s duration, tick every 0.4s |
| Météore Ignis | DelayedAoE | 40 | 8s | 120 | Radius 3.5, 1.2s telegraph |

- `SkillCaster` (4 hotkey slots) reads `ManaSystem`, blocked during dodge via
  `PlayerCombat.CanAct`, bridges `PlayerController.SetFacingMode(Aim)` during the cast
- All VFX procedural (particle systems built entirely in code, no external assets)

### Spell Crafting Core — v4.0 (SpellCraft/) — Phase 6, complete
The system that replaces the table above, now fully playable end-to-end: craft a spell in
the Grimoire, save it to a hotbar slot, cast it for real.

- `SpellRecipe` (ScriptableObject) — composes a `BaseFormData` + `SchoolData` + up to 4
  `RuneModifier`; `ManaCost`/`CooldownTime` are **computed properties**
  (`base × Π(1 + rune.multiplier)`), never stale
- **4 base forms**, each its own component built by `SpellFactory`:
  - `ProjectileSpell` — straight-line movement, `OverlapSphere` hit detection, supports
    Bounce (reflects off the hit target instead of dying) and Split (fans out extra
    projectiles) via typed accumulators on `SpellContext`
  - `ZoneSpell` — ground-anchored AoE, damage on a tick timer, leaves a matching terrain
    patch (`EnvironmentState`) for future environmental-synergy detection
  - `AuraSpell` — self-cast absorbing shield (`HealthSystem.ShieldAmount`, drained before
    HP on `TakeDamage`)
  - `ImpactSpell` — instant melee-range burst, no travel or persistence
- **3 schools with a signature combat effect** (of 7 total; the rest are currently
  color/VFX-only): Ignis → burn DoT, Aqua → slows the target's `NavMeshAgent`, Terra →
  bonus damage + a brief knockback displacement
- **4 modifier runes** (one per Trajectory/Shape/Time category, minus Interaction):
  Bounce, Split, Persist (extends duration), Expand (widens radius) — each a small
  `RuneModifier` subclass overriding a single `OnSpawn` hook
- **Continuous rune tuning** — each equipped rune carries a `RuneSlot.intensity` (0.25×–2×,
  a slider in the Grimoire) that scales both its effect and its mana/cooldown cost
  linearly, Elder-Scrolls-spell-altar style, instead of being a fixed on/off toggle. 1.0×
  reproduces the original fixed-rune values exactly, so nothing already tuned was rebalanced
- `SpellCaster` — the player-side orchestrator (mana gating, cooldowns, aim-target
  geometry per base form), live on hotkeys 1-2 alongside the legacy `SkillCaster` on 3-4
- `EnvironmentState` — a registry of terrain patches left by `ZoneSpell` (e.g. fire on the
  ground, a water puddle). Same-type overlapping patches **stack in intensity** (capped)
  instead of duplicating — re-casting a Zone spell onto ground it already marked deals
  bonus damage per stack. Full synergy *reactions* between different schools (steam, mud,
  magma…) are still a later phase; this is the tracking + same-school-stacking foundation
- **Not yet built**: the remaining 12 modifier runes, the other 4 schools' signature
  effects, cross-school environmental synergy reactions, Spellbook/Rune Encyclopedia/Journal
  Grimoire panels

### Grimoire UI — draggable node-graph spell crafting (Grimoire/)
Opens with Tab, locks player movement while open. A genuine node-graph, not a dropdown
menu — built entirely in code at runtime (no prefabs), same convention as the HUD.

- A central "recipe core" with a live Mana/Cooldown/rune-count preview, its background
  tinted toward the selected school's color instead of staying flat neutral gray
- Palette of draggable nodes: 4 Base Forms, 7 Schools, 4 Runes — drag one onto the core to
  connect it (Form/School replace any existing connection of the same kind; up to 4 runes
  stack); drag a connected node away to disconnect it. Each rune node carries its own
  intensity slider (see *Continuous rune tuning* above)
- Two "Save to Slot" buttons commit the composed recipe straight into `SpellCaster`'s
  hotbar, ready to cast immediately
- Motion: nodes ease into their slot with a scale-punch on connect, connector lines grow
  outward from the core instead of snapping to full length, and the whole panel fades/scales
  in and out on open/close instead of an instant show/hide

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
| Hit stop | `Core/HitStop.cs` — `Time.timeScale` gel (`WaitForSecondsRealtime`-based, slowmo-safe), triggered by `ProjectileSpell`/`ImpactSpell` on a real hit. Deliberately **not** triggered by `ZoneSpell`'s repeated ticks — freezing time every tick would read as a stutter, not a punch |
| Spell impact VFX | `SpellImpactVFX` — procedural, self-cleaning particle burst tinted by the school's color, generic across all 7 schools (no per-school art needed); fires on every Projectile/Impact hit and every damaging Zone tick |
| Projectile trail | `TrailRenderer` on `ProjectileSpell`, color-to-transparent gradient — the only base form that travels, so the only one with a trail |
| Hit flash | `HitFeedback`: white flash + scale punch (`unscaledDeltaTime` — slowmo-safe) |
| Damage numbers | `DamageNumber.Spawn()` — TextMesh created entirely in code, no prefab needed |
| Dodge/Sprint visuals | `CombatVisualFeedback` — trail renderer + capsule squash/stretch/tint, scoped down to just these two after the pivot (sword-swing and shield visuals removed with the systems they represented) |
| Grimoire UI motion | Node connect/disconnect ease + scale-punch, connector lines growing from the core, core panel color tint, panel open/close fade+scale — see *Grimoire UI* above |

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
| 6 | Spell Crafting Core — SpellRecipe/RuneModifier/SpellFactory, 4 base forms, 3 schools with signature effects, 4 modifier runes, terrain-effect foundation, playable `SpellCaster`, node-graph Grimoire UI | **Done** — later extended with continuous rune-intensity tuning, terrain patch stacking, and a game-feel pass (hit-stop, particle VFX, projectile trails, Grimoire UI motion) |
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

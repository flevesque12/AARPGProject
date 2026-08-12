---
name: project-history
description: Detailed history of the L'Art des Glyphes v3.1→v4.0 pivot — per-script migration narratives, Phase 5/6 implementation logs with exact Play Mode verification numbers, and root-cause writeups for fixed bugs (camera drift, projectile spawn height, aura shield cleanup, Grimoire click-through). Use when asked why a script was kept/refactored/removed, how a completed system was verified, or the root cause of a specific past bug — not needed for day-to-day feature work.
---

# Project History — v3.1→v4.0 Pivot Detail Log

This is the detailed backing material for the condensed status lines in the root
`CLAUDE.md` (Migration Status table, Completed Work, Phase 5, Phase 6). Load this
only when the condensed summary isn't enough — e.g. "why was X archived instead of
kept", "what exactly did the camera drift bug verification show", "what were the
old v3.1 tuning numbers".

## Migration Status — KEPT table narratives

**Player visual model swap (2026-07-31)**: the placeholder `Capsule` primitive under
`Player` (in `MovementGym.unity`) was replaced with an instance of the 3rd-party
`Assets/3rdParty/ATART/Character/Wizard/Prefabs/Wizard_Lit.prefab` (user request — pick
a model from `3rdParty/ATART` and use its animations). Humanoid rig, feet-at-origin
pivot, positioned at local `(0, -1, 0)` under Player so feet land at the
`CharacterController`'s capsule bottom. The stock Wizard materials use the Standard
shader (Built-in RP) or a custom `Custom/UnlitShadow` shader — both render pink in
URP — so 4 new materials were created instead (`Materials/ToonCel_WizardBody/Face/
Hair/Weapon.mat`, shader `AARPG/ToonCel`, each just wiring the matching Wizard `.tga`
into `_BaseMap`) — this doubles as applying the Phase 5 cel-shader to an imported
character model for the first time (previously only on primitive demo materials).
New `Wizard.controller` (`Animations/`) — a single `Speed` float parameter driving a
1D blend tree (`Idle1`→0, `Walk_F`→8, `Run`→13, matching `PlayerController.moveSpeed`=8
and `SprintController.speedMultiplier`=1.6) — assigned to the Animator that lives on
the nested `Wizard` FBX root (`Player/WizardModel/Wizard`, not on `WizardModel` itself).
`Animator.applyRootMotion` set to `false` — movement stays 100% owned by
`PlayerController`'s `CharacterController.Move()`, animations never displace the
transform. New `PlayerAnimator.cs` (`Scripts/Player/`) reads
`PlayerController.CurrentSpeed` each frame (smoothed via `Mathf.Lerp`) and feeds it to
the Animator — single responsibility, no other logic. `CombatVisualFeedback.
capsuleRenderer`/`capsuleTransform` and `HitFeedback.modelRenderer` were re-pointed
from the deleted Capsule to `Wizard_Body` (SkinnedMeshRenderer) and `WizardModel`
(root transform) respectively — both scripts still only tint/scale a single renderer,
so hit-flash currently only tints the body mesh, not Face/Hair/Weapon (pre-existing
single-renderer limitation, not new, just more visible now that the model has 4
separate renderers instead of 1 capsule). Verified via Unity MCP Play Mode (manually driving
`PlayerController`/`PlayerAnimator`/`SprintController`'s private `Update()` via
reflection, same frame-ticking limitation noted elsewhere in this doc): Speed param
tracked `CurrentSpeed` exactly across Idle (0) → Walk (8) → Run (12.8, sprint
multiplier applied). Zero console errors.

**Cast animation (2026-07-31, same session, follow-up)**: `Wizard.controller` gained a
`Cast` trigger + `CastSlot` int and 4 new states `Cast_Slot0..3`, each playing one of the
Wizard's `Skill1-4` clips (one-shot, `Any State` → `Cast_SlotN` on `Cast` + `CastSlot==N`,
then an exit-time transition — 0.9 through the clip — back to `Locomotion`). Slot→clip is
a plain index mapping (slot 0-1 = SpellCaster/new pipeline, 2-3 = legacy SkillCaster), not
tied to spell content — `SpellRecipe` has no visual/animation hint field, so this is the
only signal available; every recipe in a given slot plays the same gesture regardless of
its Form/School/Runes. `PlayerCombat` gained `event Action<int> OnSpellCast`, fired in
`TryCastSlot` right after a cast actually goes through (both branches). `PlayerAnimator`
subscribes to it (`OnEnable`/`OnDisable`) and sets `CastSlot`/`Cast` — `PlayerCombat` itself
has no Animator reference, keeping the existing event-driven decoupling (same pattern as
`DodgeRoll.OnDodgeStart/OnDodgeEnd` → `CombatVisualFeedback`). No movement lock added for
the cast gesture — `PlayerCombat` never locked movement during cast even before this change,
so the character can still strafe while the gesture plays; short one-shot clips (0.9-1.4s)
just finish and blend back to Locomotion. Casting_In/Wait/Out, Attack1-3, Damage_*, Death
clips are still unused — flagged as available for whenever a channeled-cast, melee, or
death/hurt-reaction pass is wanted, not started here. Verified via Play Mode (manual
`Animator.Update()` stepping, same limitation as above): `TryCastSlot(0)` correctly entered
`Cast_Slot0` and auto-returned to `Locomotion` once the clip finished; `TryCastSlot(3)`
(legacy `SkillCaster` branch) correctly entered `Cast_Slot3` — confirms `OnSpellCast` fires
from both branches. Zero console errors.

**Dodge animation (2026-07-31, same session, follow-up)**: `DodgeRoll`'s roll now plays the
Wizard's `Teleport` clip instead of no animation at all — user's choice over the pack's
`Sliding` clip (a physical ground-roll didn't fit a robed mage; a blink does, and reads
better with the existing i-frame invulnerability). `DodgeRoll` gained a public
`DodgeDuration` getter (was private-only). `Wizard.controller` gained a `Dodge` trigger, a
`DodgeSpeedMult` float, and a `Dodge` state (`Any State` → `Dodge` on the trigger, snappy
0.03s blend, then exit-time 0.95 back to `Locomotion`) whose **playback speed is bound to
`DodgeSpeedMult`** (`AnimatorState.speedParameterActive`) — the `Teleport` clip is 0.867s
but `dodgeDuration` defaults to 0.4s, so a fixed-speed clip would visibly outlast the actual
roll. `PlayerAnimator` computes `DodgeSpeedMult = dodgeClipLength / dodgeRoll.DodgeDuration`
(re-read every dodge, so retuning `dodgeDuration` in the Inspector keeps the animation in
sync automatically) and fires it from a new `DodgeRoll.OnDodgeStart` subscription — same
decoupled-event pattern as `OnSpellCast`/`CombatVisualFeedback`, `DodgeRoll` itself has no
Animator reference. `dodgeClipLength` (0.867) is a plain serialized float on
`PlayerAnimator`, not read from the clip at runtime — if the `Teleport` clip is ever swapped
for a different one, that field must be updated by hand to match. The dodge's actual
movement (`CharacterController` translation via `MoveByDelta`, smooth over `dodgeDuration`)
was **not** changed to an instant teleport-style position jump — only the visual gesture
changed, the underlying roll is still a slide-with-i-frames; a true instant-teleport-dodge
(disable collision, jump position, reappear) would be a separate, bigger design decision.
Verified via Play Mode: `DodgeSpeedMult` computed exactly `0.867/0.4 = 2.1675` on
`TryDodge`, `Dodge` state entered, and the Animator correctly auto-returned to `Locomotion`
once the sped-up clip finished. Zero console errors.

## Completed Work (v3.1 phases — superseded by Migration Status table)

### Phase 1 — Core Movement & Game Feel
- WASD + mouse movement via CharacterController
- Basic enemy AI (Idle → Chase → Attack → Dead) with NavMeshAgent
- HealthSystem (player + enemies)
- PoE2-style camera (CameraController — since replaced)
- Game feel: hit stop (Time.timeScale 0.05f for 60ms), DamageNumber.Spawn()
- AimIndicator ground reticle (since replaced)

### Phase 2 — Common Skills (Combat v3.1, since removed/refactored)
- DodgeRoll: directional, i-frames 0.05s–0.35s, AnimationCurve movement, 25 stamina, 1.2s cooldown
- BlockSystem: normal (60% reduction, 120° arc) + perfect (90%, slowmo 0.15f)
- RiposteSystem: window after perfect block or dodge end, ×2 damage, 50% posture damage
- SprintController: ×1.6 speed, 10 stamina/sec
- StaminaSystem: shared resource, 1.5s regen delay, ×3 out-of-combat regen
- PlayerHUD: HP bar, stamina bar (dynamic color), action bar, riposte indicator

### Phase 3 — Enemy Telegraph + Posture/Stagger (since removed)
- PostureSystem: posture pool (60 basic / 120 elite), regen, stagger state, ×2.5 damage during stagger
- PostureBarUI: world-space bar, amber fill, pulse at ≤30%, "STAGGER" text
- EnemyTelegraph: Circle (0.3s), Cone (0.5s), FullBoss (0.8-1.2s) red indicators
- StaggerVFX: visual effect on stagger
- EnemyAI integration: cancel attack if staggered during windup

### Phase 4 — First School Ignis (SkillData path, still active alongside SpellRecipe)
- SkillData ScriptableObject architecture
- SkillCaster: instantiation + casting logic
- SkillProjectile: physics-based projectile with VFX
- 4 active Ignis skills: Trait de Braise (basic fire projectile), Explosion Ignis
  (AoE burst), Mur de Feu (fire wall), Météore Ignis (heavy AoE)
- Procedural VFX particle systems per skill
- Skill bar HUD: slots 1-4 with cooldown overlay
- TTK calibration: baseDamage 25, enemyDamage 17, enemyHP 100

## Phase 5 — Core Pivot: full implementation log

- **Cinemachine ThirdPersonCamera**: top-down, fixed angle, 100% game-controlled
  (design changed mid-phase from the original "over-the-shoulder" plan).
- **PlayerController refactor**: `FacingMode` (Movement/Aim), no camera-rotation
  coupling since the camera takes no player input.
- **GameInput refactor**: removed block/riposte input; added spell slots 1-4,
  `OnGrimoireTogglePressed`/`OnInteractPressed` events (no consumer at the time).
- **ManaSystem**: replaces StaminaSystem for casting.
- **PlayerCombat**: spell slot casting + FacingMode.Aim bridge (did not yet own
  dodge at this point in the phase).
- **Archived removed scripts to `_Archive/`** (done 2026-07-30): CombatController/
  BlockSystem/RiposteSystem/PostureSystem/PostureBarUI/StaggerVFX moved to
  `Scripts/_Archive/*.cs.txt`, components removed from Player.prefab and
  Enemy.prefab. Rewired in the process: HealthSystem (dodge i-frames checked
  directly via DodgeRoll, no more CombatController middleman), GameInput
  (dodge/sprint wired straight to DodgeRoll/SprintController, basic melee attack
  input dropped — v4.0 has no melee combo), CombatVisualFeedback (gutted:
  sword-swing + shield visuals removed, sprint/dodge visuals kept), HitFeedback
  (block-hit/perfect/broken flashes removed), SkillCaster/SkillProjectile (posture
  stagger-damage branch removed). StaminaSystem intentionally NOT archived — still
  consumed by DodgeRoll and SprintController; removing it needs the "sprint may be
  free" open question resolved first.
- **DodgeRoll simplified**: i-frame 0.3s → 0.2s; riposte-window-on-dodge-end
  removed (that trigger actually lived in RiposteSystem's own subscription to
  `DodgeRoll.OnDodgeEnd`, not in DodgeRoll itself).
- **EnemyAI simplified**: posture checks removed. ElementalWeakness/hurt state
  intentionally deferred to Phase 8, not added here.
- **PlayerHUD refactor**: HP + Mana + 4 spell slots.
- **Basic cel-shader setup** (done 2026-07-30): hand-authored HLSL/ShaderLab
  shader (`Shaders/ToonCel.shader`, `AARPG/ToonCel`) instead of a Shader Graph
  node graph — the MCP tooling available that session could only edit `.shader`
  text, not `.shadergraph` node JSON (user confirmed hand-written HLSL over a
  blind/error-prone hand-edited node graph). Forward + ShadowCaster passes,
  banded (stepped) diffuse lighting, ambient floor so the shadow band never
  reads pure black (design pillar: never dark/grim), banded specular, and a rim
  light for the warm toon glow. Compiles clean. Demo materials
  `ToonCel_Player.mat`/`ToonCel_Enemy.mat` created and assigned — confirmed
  visually via screenshot on the Player capsule (clear light/shadow band +
  rounded specular highlight, no black shadow). Not applied to the imported
  Skeleton_Warrior/Synty models — that reskin is the Phase 11 art pass. If the
  team wants Shader Graph specifically (node-based, artist-editable) instead
  of/alongside this HLSL version, that still needs to be built by hand in the
  Unity Editor — not reachable through current MCP tools.
- **Test**: player can move in 3P, dodge, cast 1 basic spell, kill 1 enemy — done
  (2026-07-30) via Unity MCP + Play Mode, exercising specifically the code
  touched by this session's CombatController/PostureSystem archiving:
  `HealthSystem.TakeDamage` on both an entity with DodgeRoll (Player, normal
  damage confirmed) and without (Enemy, lethal hit → `IsDead=true` confirmed);
  `DodgeRoll.TryDodge` → `IsDodging=true` synchronously, correctly gating
  `PlayerCombat.CanAct`; full cast pipeline `PlayerCombat.TryCastSlot` →
  `SkillCaster.TryCastSkill` → mana 100→75 (Ignis_Explosion cost 25), cooldown
  started, FacingMode switched to Aim. Zero console errors across both Play Mode
  sessions. Still not a live human playtest — this MCP environment's Play Mode
  does not tick frames between separate tool calls (confirmed again that
  session: `Time.frameCount` stayed at 2 across multiple calls), so continuous
  input (camera damping feel, movement smoothness, precise i-frame timing) still
  needed a real keyboard/mouse session to validate.

Phase 5 was functionally complete as of 2026-07-30 — all checklist items done
except the human feel-playtest noted above, a standing limitation of this
tooling, not unfinished work.

## Phase 6 — Spell Crafting Core: full implementation log

- **SpellRecipe + RuneModifier ScriptableObjects** (done 2026-07-30).
  `SpellCraft/Data/`: `SpellEnums.cs` (`SpellBaseForm`, `SpellSchool`,
  `RuneCategory` — deliberately separate from the existing `SkillSchool`/
  `SkillType` in `Skills/SkillData.cs`, same pattern as ManaSystem staying
  decoupled from StaminaSystem, so archiving the v3.1 skill system later needs
  zero changes here), `BaseFormData` (per-form baseManaCost/baseCooldown),
  `SchoolData` (per-school display name + primary/secondary color from the
  Schools table), `RuneModifier` (abstract SO base implementing
  `ISpellModifier` — concrete runes are subclasses), `SpellRecipe` (composes
  BaseFormData + SchoolData + up to 4 RuneModifier, `ManaCost`/`CooldownTime`
  computed live as `base × Π(1 + rune.multiplier)` rather than
  cached/serialized, `OnValidate` prunes mutually-incompatible rune pairs).
  Seed data assets created under `Data/SpellCraft/`: 4 `BaseFormData`
  (Projectile/Zone/Aura/Impact, mana costs 6-9 — mid-range of the "Form only:
  5-10 Mana" bracket), all 7 `SchoolData` (colors from the Schools table), 1
  example `SpellRecipe` (`Ignis_Projectile_Base`, no runes yet since none
  existed).
- **SpellFactory + ModifierProcessor + ISpellModifier** (done 2026-07-30).
  `SpellCraft/Runtime/`: `ISpellModifier` (single `OnSpawn(SpellContext)` hook
  — deliberately minimal, `OnHit`/`OnExpire` to be added once base forms
  needed them, not speculatively), `SpellContext` (minimal MonoBehaviour data
  carrier — recipe/caster/origin/direction — added because the 4 base forms
  need something common to read from; they attach their behavior components
  onto the same GameObject `SpellFactory` creates), `ModifierProcessor`
  (iterates `recipe.ModifierRunes` and calls `rune.OnSpawn`), `SpellFactory`
  (instantiates the spell root GameObject + `SpellContext`, calls
  `ModifierProcessor` — does not touch Mana/cooldown, that stays the caller's
  job, same split as `SkillCaster`). Verified end-to-end via Unity MCP
  `execute_code` in Play Mode (Reflection.Emit-generated disposable test rune):
  baseline recipe (0 runes) → ManaCost=7/CooldownTime=1 exactly matches
  `BaseForm_Projectile`; 1 rune (×0.5 mana, ×0.2 cooldown) → 10.5/1.2 exactly
  as the formula predicts; `OnValidate` correctly nulled out an incompatible
  second rune; `SpellFactory.CreateSpell` produced a correctly-populated
  `SpellContext` and the test rune's `OnSpawn` fired through
  `ModifierProcessor`. Zero console errors.
- **4 base forms functional** (Projectile, Zone, Aura, Impact — done
  2026-07-30), all in `SpellCraft/Runtime/`. `BaseFormData` gained combat
  tuning fields (`baseDamage`, `range`, `radius`, `projectileSpeed`,
  `projectileSize`, `duration`, `tickInterval` — same flat-fields-for-all-forms
  convention as the v3.1 `SkillData`). `SpellFactory.CreateSpell` gained a
  `LayerMask hitLayer` parameter and now attaches+inits the matching base form
  behaviour after running `ModifierProcessor`. No `OnHit`/`OnExpire` modifier
  hook existed yet on any form.
  - **ProjectileSpell**: straight-line movement + manual `OverlapSphere` hit
    detection (ported from `Skills/SkillProjectile.cs`), builds its own
    primitive-sphere visual colored by `recipe.school.primaryColor`.
  - **ZoneSpell**: flat cylinder visual, ticks damage via `OverlapSphere` every
    `tickInterval` for `duration` then self-destroys (ported from
    `SkillCaster.CastZone`/`ApplyAoE`). Note: Unity runs a coroutine's body
    synchronously up to its first `yield` the moment `StartCoroutine` is
    called, so the first tick fires immediately on cast — not a bug.
  - **AuraSpell**: self-buff on the caster. Design decision (user choice,
    2026-07-30, no buff/resistance system existed yet): implements an
    absorbing shield, not a generic resistance/buff — reuses
    `BaseFormData.baseDamage` as the shield amount. Required adding shield
    support to `Core/HealthSystem.cs`: `ShieldAmount` property,
    `AddShield`/`ClearShield` methods, `OnShieldChanged` event, and
    `TakeDamage` now drains `ShieldAmount` before touching `CurrentHealth`
    (same pattern as the `DodgeRoll.IsInvulnerable` early-return check). A
    true generic buff/resistance system is still undesigned.
  - **ImpactSpell**: instant single-hit `OverlapSphere` damage at the cast
    point, no travel, no persistence, brief expanding-sphere visual (0.3s)
    then self-destroy (ported from `SkillCaster.CastAoE`/`ApplyAoE`, minus the
    `castTime` telegraph).
  Verified via Unity MCP Play Mode: Projectile moved and dealt exactly 30
  damage (100→70 HP) to a `HealthSystem` on the `Enemy` layer, correctly gated
  by `hitLayer` (an earlier `~0` "everything" test mask hit the scene's real
  Player object instead — confirms the layer filter is load-bearing); Zone
  dealt 30/tick and left an enemy at 10 HP after 3 ticks; Aura gave the caster
  a 30-point shield that absorbed a 20-dmg hit fully (shield→10) then absorbed
  the remaining 10 on a 50-dmg hit with 40 overflowing to HP (100→60); Impact
  hit an in-radius enemy (100→70) and correctly ignored one outside `radius`.
  Zero console errors/warnings across every test.
- **3 schools playable** (Ignis, Aqua, Terra — done 2026-07-30). User chose a
  signature-effect approach (vs. visual-only) matching the GDD "Combat role"
  table. New `SpellCraft/Runtime/SchoolEffectApplier.cs`: single entry point
  called by `ProjectileSpell`/`ZoneSpell`/`ImpactSpell` right before
  `TakeDamage` (not `AuraSpell`). Switches on `recipe.school.school`;
  Ventus/Lux/Umbra/Ferrum had no case yet. `SchoolData` gained per-school
  tuning fields (burn/slow/knockback).
  - **Ignis → Burn (DoT)**: new `BurnStatus.cs`, attached to the target's
    `HealthSystem` GameObject, ticks damage on a timer, re-`Init` refreshes
    duration instead of stacking a second component.
  - **Aqua → Slow**: new `SlowStatus.cs`, multiplies `NavMeshAgent.speed` for a
    duration then restores it. Only works on entities with a `NavMeshAgent`
    (v4.0 convention: NavMeshAgent for enemies only) — an Aqua spell hitting
    the player currently has no effect, since `PlayerController` has no
    external speed hook. Flagged as a gap to revisit if enemies ever cast
    spells.
  - **Terra → bonus damage + Knockback**: `damage *= 1 + bonusMultiplier`
    applied inline, plus new `Knockback.cs` which disables the target's
    `NavMeshAgent` for 0.2s while manually displacing its transform, then
    re-enables the agent (a `NavMeshAgent` ignores manual `transform.position`
    writes while enabled — standard workaround, `EnemyAI` needs no awareness
    of it).
  Verified via Unity MCP Play Mode against a real instantiated `Enemy.prefab`
  at its on-NavMesh position: Ignis Impact dealt 30 direct damage (50→20 HP)
  then 2 more Burn ticks of 5 landed (20→10, loop exits exactly at duration
  without a 3rd — matches ZoneSpell's identical tick-loop shape, not a bug).
  Aqua Impact dropped `NavMeshAgent.speed` from 5→2.5 synchronously on cast.
  Terra Impact dealt 36 damage (50→14, exactly 30×1.2) and displaced the
  enemy's position with `agent.enabled=false` observed immediately after cast.
  Zero console errors/warnings.
- **4 basic modifier runes** (Bounce, Split, Persist, Expand — done
  2026-07-30). `SpellContext` gained typed mutable accumulators
  (`BounceCount`, `ExtraProjectileCount`, `DurationMultiplier`,
  `RadiusMultiplier`) populated by `RuneModifier.OnSpawn` — deliberately NOT a
  generic key-value bag or event bus, each value maps to exactly one of these
  4 runes, kept strongly typed. New concrete `RuneModifier` subclasses:
  `BounceRune.cs` (Trajectory), `SplitRune.cs` (Shape), `PersistRune.cs`
  (Time), `ExpandRune.cs` (Shape).
  - **Bounce**: `ProjectileSpell` reads `BounceCount` at Init; on hit, if
    bounces remain, reflects `_direction` off a vector from the impact point
    to the target (`Vector3.Reflect`, using target position as a crude surface
    normal — no real collision normal available from `OverlapSphere`) and
    keeps flying instead of destroying itself.
  - **Split**: `ProjectileSpell` reads `ExtraProjectileCount` at Init and
    spawns sibling `ProjectileSpell` GameObjects in a fan (15° step), sharing
    the primary's `SpellContext`. Required caching `SpellRecipe _recipe`
    instead of holding a live `SpellContext` reference for anything read
    after Init — siblings live on separate GameObjects from the primary, so
    if the primary is destroyed first, a sibling still holding
    `_context.Recipe` would throw `MissingReferenceException`. An `isPrimary`
    flag prevents siblings from re-triggering their own split.
  - **Persist**: `ZoneSpell`/`AuraSpell` multiply `form.duration` by
    `DurationMultiplier`. No effect on Projectile/Impact.
  - **Expand**: `ZoneSpell`/`ImpactSpell` multiply `form.radius` by
    `RadiusMultiplier`. No effect on Projectile/Aura.
  Verified via Unity MCP Play Mode: Split produced exactly 3 `ProjectileSpell`
  instances and the correct `ManaCost` (7 × 1.6 = 11.2); Bounce consumed one
  charge on its first hit (`BounceCount` 2→1), redirected, then self-expired
  via max-range; Persist doubled a Zone's effective duration (3→6, radius
  unchanged at 2); Expand widened an Impact's hit radius enough to catch an
  enemy at 2.5 units (base radius 2, expanded 3). Zero console
  errors/warnings.
- **Grimoire UI — basic crafting panel** (node-graph, done 2026-07-30). User
  explicitly chose a real draggable node-graph over a simpler button-picker
  panel. Scoped to crafting only — Spellbook/RuneEncyclopedia/
  SynergyEncyclopedia/JournalPanel are Phase 8. New `Grimoire/` folder:
  - `CraftingNode.cs` — generic draggable node
    (`IBeginDragHandler`/`IDragHandler`/`IEndDragHandler`), carries a
    `NodeKind` (Form/School/Rune) + the actual asset payload, drag position
    resolved via `RectTransformUtility.ScreenPointToLocalPointInRectangle`.
    Delegates all connect/disconnect decisions to
    `CraftingPanel.HandleNodeDropped`.
  - `CraftingPanel.cs` — the graph: a central "recipe core" (live
    Mana/Cooldown/rune-count preview via a cached in-memory `SpellRecipe`
    instance, never written to disk), 6 ring-arranged slot positions (1 Form,
    1 School, 4 Runes), and a palette of draggable source nodes. Drop within
    `CoreDropRadius` (150px) connects (Form/School replace any existing
    connection of the same kind; Rune fills the first empty of 4 slots,
    rejects a 5th with on-screen feedback). Drop outside the radius on an
    already-connected node disconnects it and snaps it back to its palette
    origin. Two "Sauver → Slot 1/2" buttons call `SpellCaster.SetSlot`.
  - `GrimoireUI.cs` — hosts `CraftingPanel` inside a `ScreenSpaceOverlay`
    Canvas (sortingOrder 200, above `PlayerHUD`'s 100) built entirely in code
    at runtime. Toggled by `GameInput.OnGrimoireTogglePressed` (Tab). Calls
    `PlayerController.LockMovement` while open. Auto-creates an `EventSystem`
    + `StandaloneInputModule` if the scene doesn't have one yet.
  Added to the Player GameObject with all 4 `BaseFormData`, all 7
  `SchoolData`, and all 4 `Rune_*.asset` wired into its Inspector arrays
  (palette contents are NOT discovered via `Resources.Load`/`AssetDatabase` —
  editor-only, would break in a real build — so new future runes/schools need
  manual array wiring until Phase 8 revisits data sourcing). Verified via
  Unity MCP Play Mode exercising the real `CraftingPanel.HandleNodeDropped`
  API: opening the Grimoire built all 15 palette nodes and locked player
  movement; dropping Form=Projectile, School=Ignis, runes Bounce+Split
  produced the exact expected preview (16.8 Mana, 1.3s CD, 2 runes); saving to
  slot 1 populated `SpellCaster.GetSlot(0)`; disconnecting Split correctly
  reduced the live preview without retroactively changing the already-saved
  slot 1 recipe; casting slot 1 for real spent exactly 16.8 mana and spawned 3
  `ProjectileSpell` instances. Zero console errors/warnings.
- **Terrain effects** (fire on ground, water puddle — done 2026-07-30).
  Scoped narrow: just the ground-marking foundation, NOT synergy
  detection/reaction (that's Phase 8's "Environmental synergies"). New
  `TerrainType` enum (Fire/Water/Wind/Shadow/LooseEarth) in `SpellEnums.cs`.
  New `SpellCraft/Synergies/EnvironmentState.cs`: a static registry (not a
  MonoBehaviour singleton — project convention is "No singletons —
  dependency injection via Inspector") tracking active terrain patches
  (type/position/radius/expiry) with `RegisterPatch`/`HasPatchAt`, plus
  `TryGetTerrainType(SpellSchool)`. `ZoneSpell.Init` registers a patch
  matching its own school right when it spawns, living exactly as long as the
  zone itself. Only `ZoneSpell` leaves terrain. Verified: an Ignis Zone left a
  `Fire` patch (and correctly did NOT register as `Water`); an Aqua Zone left
  a `Water` patch, with a query 50 units away correctly returning false. Zero
  console errors/warnings. `SynergyData.cs`/`SynergyDetector.cs` do not exist
  yet — synergies don't fire in-game from this work alone.
- **2 active spell slots** (done 2026-07-30). New
  `SpellCraft/Runtime/SpellCaster.cs`: player-side orchestrator, same shape as
  `Skills/SkillCaster.cs` but driving `SpellFactory.CreateSpell`. 2 slots
  matches the GDD's starting Savoir Magique count (not yet wired to
  progression). `ComputeCastGeometry` picks origin per base form: Projectile
  spawns at the caster and fires toward the aim point; Zone/Impact are
  centered on the (range/radius-clamped) aim point; Aura originates at the
  caster. Key-binding decision (user choice, 2026-07-30): keys 1-2 route to
  `SpellCaster` (slot 0-1), keys 3-4 stay on the legacy `SkillCaster` (slot
  2-3) — a coexistence measure, not the final mapping. 2 example
  `SpellRecipe` assets: `Ignis_Projectile_Base` (key 1), `Aqua_Zone_Frost`
  (Zone + Aqua + Persist, key 2). Verified via `PlayerCombat.TryCastSlot`:
  key 1 spent 7 mana, started 1s cooldown, spawned 1 Projectile; key 2 spent
  12.6 mana (9 × 1.4 Persist), started 2.2s cooldown, spawned 1 Zone; key 3
  (legacy) still spent 30 mana on `Ignis_MurDeFeu` — no regression. Zero
  console errors/warnings.
- **Refactor SkillProjectile → ProjectileSpell with modifier support** (done
  2026-07-30). Port already done via the base-forms item above; modifier
  support: Bounce and Split both modify `ProjectileSpell`'s behavior via
  `SpellContext` accumulators. Still no generic `ISpellModifier.OnHit` event
  hook — turned out unnecessary for these two runes.

### Post-Phase-6 correction (2026-08-05)

User felt the crafting core was missing depth after comparing it against
other spell-crafting games (Magic and Mayhem, Noita, Elder Scrolls
spell-making altar, Divinity: Original Sin 2, Fable's gesture magic —
discussed but not built from, except the two picked below).

- **Continuous rune tuning** (Elder-Scrolls-slider inspired). New
  `SpellCraft/Data/RuneSlot.cs`: `{ RuneModifier rune; float intensity
  (0.25–2.0, default 1.0) }`, replacing the old bare `RuneModifier[]` on
  `SpellRecipe.modifierRunes` — intensity lives on the slot, not the shared
  rune asset. `RuneModifier.OnSpawn` gained an `intensity` parameter;
  `RuneModifier` gained `EffectiveManaCostMultiplier`/
  `EffectiveCooldownMultiplier` (`authored × intensity`). Each of the 4
  existing runes scales its own parameter: `BounceRune`/`SplitRune` scale
  their flat counts (`count × intensity`, rounded — a Bounce rune dialed to
  minimum intensity can round down to 0 bounces, intended, not a bug);
  `PersistRune`/`ExpandRune` interpolate around their multiplier's neutral
  point of 1. Intensity 1.0 reproduces the old fixed-rune behavior exactly.
  Grimoire: `CraftingNode` gained an `Intensity` property, `CraftingPanel`
  builds a hand-rolled slider as a child of each rune node so it follows the
  node during drag/connect.
- **Terrain patch stacking** (DOS2-surfaces inspired, deliberately small —
  full cross-school reactions stay Phase 8). `EnvironmentState.RegisterPatch`
  now checks for an existing overlapping patch of the same `TerrainType`
  before creating a new one; if found, increments that patch's `Intensity`
  (capped at 3), extends radius/expiry to the max of old/new. New
  `GetIntensityAt` query. `ZoneSpell.Init` reads `RegisterPatch`'s return
  value and applies +50% damage per stack beyond the first — recasting an
  Ignis Zone onto ground already marked Fire hits harder.

Verified via Unity MCP Play Mode: Bounce rune cost matched the linear formula
exactly at intensity 0.25/1.0/2.0 (7.875/10.5/14 Mana); `OnSpawn(intensity:
0.25)` on a bounceCount-2 rune correctly produced 0 bounces; four same-spot
`RegisterPatch(Fire, ...)` calls produced intensity 1→2→3→3 (capped), a
same-spot `Water` registration stayed a separate patch at intensity 1; two
same-spot Ignis Zone casts dealt 30 then 45 damage (exactly ×1.5). Zero
console errors/warnings.

### Game feel / juice pass (2026-08-05, same session, follow-up)

- **Spell juice — hit-stop + particle burst + trail.** New `Core/HitStop.cs`:
  static `HitStop.Trigger(duration, scale)` gels `Time.timeScale` briefly
  then restores it — this closes a gap in the v3.1→v4.0 migration table
  ("Hit stop on impactful spells only") that was never implemented for the
  SpellCraft forms. New `SpellCraft/Runtime/SpellImpactVFX.cs`: procedural
  one-shot particle burst, colored by `SchoolData.primaryColor`, shader
  resolved with a fallback chain (URP Particles/Unlit → URP Unlit →
  Sprites/Default) to never render pink. **Hit-stop applies only to
  genuinely discrete impacts** — `ProjectileSpell` (one trigger per hit) and
  `ImpactSpell` (one trigger for the whole AoE burst, not per target) —
  deliberately not `ZoneSpell`, whose ticks repeat every `tickInterval` for
  the whole duration; freezing time on every tick would read as a
  near-permanent freeze. Zone ticks get the particle burst without the
  hit-stop. `ProjectileSpell` also gained a `TrailRenderer`. Bug caught and
  fixed during verification: `SpellImpactVFX.Spawn` originally configured
  `ParticleSystem.main` *after* `AddComponent<ParticleSystem>()` on an
  already-active GameObject — `playOnAwake` defaults true, so the system
  started playing before configuration finished, logging a Unity warning on
  every burst. Fixed by creating the GameObject inactive, configuring the
  idle `ParticleSystem`, then activating and calling `Play()`.
- **Grimoire UI juice — motion + color.** `CraftingNode` gained
  `AnimateTo`/`AnimateToOrigin` (ease-out cubic tween + scale-punch on
  connect), replacing instant `anchoredPosition` snaps. Connector lines
  (`AnimateLineGrow`) now grow from the core outward. The core panel's
  background animates toward the selected school's color (`Color.Lerp` at
  50%), read back to neutral gray on disconnect. `GrimoireUI` gained a
  `CanvasGroup` + fade/scale open-close animation
  (`Time.unscaledDeltaTime`-driven so it isn't affected by an in-progress
  `HitStop` freeze), replacing instant `SetActive`.

Verified via Unity MCP Play Mode + screenshot: hit-stop measurably dropped
`Time.timeScale`, a real Projectile-vs-Enemy hit dealt damage and spawned a
trail while leaving `Time.timeScale` at 0.05 immediately after; a Zone tick
dealt damage but left `Time.timeScale` at 1 (confirms the exclusion is real).
Grimoire: opening reached correct final alpha/scale; connecting a Form +
School node produced no exceptions and the screenshot confirmed the core
tinted Ignis's orange with connector lines mid-grow. Zero console
errors/warnings throughout. Neither pass was a real-time playtest — same
standing MCP-environment limitation (frames don't tick between tool calls).

### Test props (2026-08-05, same session, follow-up)

User asked for gym props to test spells crafted in the Grimoire. Added 6
target dummies to `MovementGym.unity` (duplicates of `Enemy`): 3 in a line in
front of the player spawn (`TestDummy_Near`/`Mid`/`Far` at 4/10/18m) and a
3-dummy triangular cluster (`TestDummy_Cluster_A/B/C`, ~3m spacing) for
AoE/Split/Bounce/terrain-stacking testing. Each had `EnemyAI`/`EnemyTelegraph`
removed (passive) but kept `NavMeshAgent` so Aqua's slow and Terra's
knockback stay testable; `HealthSystem` retuned to 500 HP / 15 regen-per-sec /
`destroyOnDeath = false` so they're a reusable punching bag. Verified: all 6
report `onNavMesh=True`, spawn at full HP, real casts deal damage correctly.

### Bug fix — camera rotation drift breaking spell aim direction (2026-08-05)

User reported cast spells didn't travel toward the mouse cursor, asked to
"fix the camera in place". Root cause, confirmed by directly inspecting
Cinemachine's internal camera state via Unity MCP
(`CinemachineCamera.InternalUpdateCameraState`): `ThirdPersonCamera` had both
`CinemachineFollow` (Body stage, position-only, confirmed Body never writes
rotation) **and** `CinemachineRotationComposer` (Aim stage). Aim always
overrides Body's rotation output, and a Rotation Composer's entire job is to
continuously re-aim the camera to keep its LookAt target centered — the exact
opposite of "fixed angle, translate only". Since the camera's position lags
the player via damping, the target is never exactly where the Composer
expects, so it kept nudging rotation frame to frame: measured ~10° yaw drift
+ ~3° pitch drift from a single moderate player displacement (player moved to
(20, 5), camera rotation went from (47.22, 0, 0) to (44.73, 10.78, 0)). That
drift broke spell aim because `PlayerController.UpdateAimWorldPosition`'s
mouse raycast resolves a world direction from the camera's *current*
rotation — a silently-rotated camera desyncs "where the cursor looks on
screen" from "what direction that raycast actually produces". Fix: removed
`CinemachineRotationComposer` entirely and replaced its job with
`ThirdPersonCamera` writing its own `transform.rotation` directly to the
constant `Quaternion.Euler(pitchAngle, yawAngle, 0)` in `LateUpdate` every
frame — confirmed via the same internal-state test that this produces zero
rotation drift across large simulated displacements (including a (-18, 22)
teleport). Also removed the now-dead `lookAtHeightOffset`/`rotationDamping`
fields. End-to-end verification: a real cast's `ProjectileSpell` travel
direction matched `PlayerController.AimWorldPosition` exactly (`Vector3.Dot`
= 1), and `AimWorldPosition` was now bit-identical across repeated frames at
a fixed mouse/camera state (previously it could jitter as rotation drifted
even with a static cursor). Zero console errors/warnings.

### Bug fix — Projectile spells fly over the target's head (2026-08-06)

User reported projectiles visibly passing through/over targets without
dealing damage. Root cause: both `SpellCaster.ComputeCastGeometry` (v4.0) and
the legacy `Skills/SkillCaster.CastProjectile` (v3.1) spawn the projectile at
`transform.position + Vector3.up` — a full extra unit above the caster's own
pivot. But `Player`'s `CharacterController.center = (0,0,0)`, meaning
`transform.position.y` (1.062) is already the *vertical center* of the
player's own 2-unit capsule — i.e. already roughly chest height, not
feet-level like a more typical CharacterController setup. Adding another full
unit put every Projectile's spawn (flight path is flat, direction.y always
0) at y≈2.06, at or above head height. Target dummies (scale 0.8) have a
CapsuleCollider top at y≈1.84 — so every Projectile flew comfortably above
every target's collider, 100% of the time, regardless of range or framerate.
This is also why none of the Phase 6 verification passes ever caught it:
every one of those tests hand-constructed a `SpellFactory.CreateSpell` origin
already at the target's height, bypassing `ComputeCastGeometry`/
`CastProjectile` entirely. Fix: added a tunable
`[SerializeField] private float _castHeightOffset = 0.3f` to both
`SpellCaster` and `SkillCaster`, replacing the hardcoded `Vector3.up` with
`Vector3.up * _castHeightOffset` — puts the spawn at y≈1.36, comfortably
inside every current target's collider range (0.03–1.84). Verified with a
direct before/after comparison at the same target: the old `× 1.0` offset
produced a spawn at y=2.06 and a confirmed miss (500→500 HP); the new `× 0.3`
offset produced a spawn at y=1.36 and a confirmed hit (500→470, expected 30
Ignis-Projectile damage). Zero console errors/warnings.

### Bug fix — Aura shield visual never disappears (2026-08-06)

User reported the Aura's shield bubble looks always-active with no timer.
Root cause: `AuraSpell.BuildVisual()` deliberately parents its shield-bubble
sphere to `context.Caster` (the player), not to the `AuraSpell`'s own
tracking GameObject, so the bubble visually follows the player. But
`ExpireAfter`'s cleanup only ever called `Destroy(gameObject)` — destroying
the invisible tracking object, never the visual living under the player. The
shield *mechanic* was never broken (`HealthSystem.ShieldAmount` correctly
hit 0 via `ClearShield()` on schedule) — only the leftover sphere was never
cleaned up, so every cast left one more permanent orphaned bubble as a child
object (player `childCount` went 4→5 on cast and stayed at 5 after the
shield itself had already expired). Fix: `AuraSpell` now stores the visual in
a `_visual` field and calls `Destroy(_visual)` alongside `Destroy(gameObject)`
in `ExpireAfter`. Zero console errors/warnings.

### Bug fix — purple rune nodes hard to click/drag in the Grimoire (2026-08-06)

User reported the rune modifier nodes specifically (not Form/School) were
hard to click and drag. Root cause, found by comparing exact world-space
rects of every relevant UI element: `CraftingPanel.BuildFeedbackText()` runs
*last* in `BuildUI()`, so its 600px-wide, horizontally-centered "Feedback"
label renders (and raycasts) on top of every earlier sibling it overlaps.
Measured rects showed the Feedback label spanning world X=[293.5, 893.5] —
which fully contains all 4 rune sliders' X-range (378.5–808.5) and overlaps
the bottom half of every rune node's own Y-range. Since
`UnityEngine.UI.Text` defaults to `raycastTarget = true`, this invisible
label was silently swallowing clicks meant for the bottom half of every rune
node and 100% of every intensity slider underneath it. Form nodes (world
X≈173.5) and School nodes (world X≈1013.5) sit safely outside the label's
span, which is why only the purple runes were affected — this also ruled out
the newly-added intensity slider itself as the cause (its own geometry was
independently confirmed non-overlapping with its parent node). Fix:
`_feedbackText.raycastTarget = false` — it only ever displays status text.
Zero console errors/warnings.

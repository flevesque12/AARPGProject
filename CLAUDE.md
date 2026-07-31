# L'Art des Glyphes — Unity Project

## Description
Third-person action-adventure / spell-crafting RPG in toon fantasy low-poly 3D.
The player is a young mage who learns magic by crafting custom spells: combining
base forms, elemental schools, and modifier runes in a Grimoire.
Magic serves combat AND daily life — helping villagers, solving environmental puzzles,
and exploring ancient Libraries of knowledge.

**Inspirations:** Mages of Mystralia (spell crafting, 3P), Magicka 2 (elemental combos),
Magic and Mayhem (Grimoire, talismans, apprenticeship), Zelda: TotK (emergent creativity).

**Design version:** GDD v4.0 (July 2026)

> **PIVOT NOTICE (v3.1 → v4.0)**
> Major redesign from dark isometric ARPG (PoE 2 / Sekiro) to toon fantasy
> spell-crafting adventure in 3rd person. See "Migration Status" below for
> what existing code is kept, refactored, or removed.

## Design Pillars — NON-NEGOTIABLE
1. **Spell Crafting** — spells are BUILT by the player (Form + School + Runes), never picked from a menu
2. **Grimoire** — central interface: crafting workshop, spellbook, encyclopedia, journal. Must feel like a pleasure to use
3. **Living World** — village hub evolves visibly; zones unlock through learned spells
4. **Toon Fantasy** — colorful, warm, cel-shaded. NEVER dark, grim, or lugubre
5. Combat is a **creative puzzle**, NOT a reflex test. Bosses are puzzles, not DPS checks
6. Synergies are **discovered by playing**, not explained in tutorials
7. Magic serves life AND combat — never just a weapon

## Architecture
- Unity 6000.4.8f1
- Universal Render Pipeline (URP) + custom cel-shader (`Shaders/ToonCel.shader`, hand-written HLSL, done 2026-07-30 — not yet applied to imported character models, see Phase 5)
- New Input System (InputAction asset, gamepad + keyboard/mouse)
- Cinemachine for 3rd person camera (replaces CameraController)
- CharacterController for player (kept from v3.1)
- NavMeshAgent for enemies only (kept from v3.1)

## The 7 Schools
| School | Color | Combat role | World role |
|--------|-------|-------------|------------|
| Ignis | Red-orange | Direct damage, DoT, AoE | Burn, melt, heat |
| Aqua | Blue | Slow, control, freeze | Irrigate, extinguish, ice bridges |
| Terra | Brown-green | Heavy damage, walls | Repair, build, move rocks |
| Ventus | Cyan-white | Knockback, speed | Disperse fog, dry, reveal |
| Lux | Gold-white | Heals, shields, purify | Heal, illuminate, grow plants |
| Umbra | Dark purple | Stealth, confusion | Hide, spy, distract |
| Ferrum | Steel-copper | Magic weapons, armor | Forge, repair metal, magnetize |

## Spell Crafting System

### Spell architecture
```
SpellRecipe (ScriptableObject)
├── BaseForm       → enum: Projectile, Zone, Aura, Impact
├── School         → enum: Ignis, Aqua, Terra, Ventus, Lux, Umbra, Ferrum
├── ModifierRunes  → RuneModifier[] (max 4)
├── ManaCost       → float (computed: base + multipliers)
└── CooldownTime   → float (computed)

RuneModifier (ScriptableObject)
├── RuneType          → enum: Trajectory, Shape, Time, Interaction
├── ManaCostMultiplier → float
├── IncompatibleWith   → RuneModifier[]
└── BehaviorData       → type-specific parameters
```

### 4 base forms
| Form | Behavior | Typical use |
|------|----------|-------------|
| **Projectile** | Fires forward | Ranged attack, distant activation |
| **Zone** | Area effect on ground | Terrain control, traps, environmental aid |
| **Aura** | Self-buff | Shield, resistance, buff |
| **Impact** | Melee / short range | Close attack, break obstacles, push |

### 16 modifier runes (4 categories)
- **Trajectory**: Bounce, Homing, Orbit, Arc
- **Shape**: Split, Expand, Chain, Wall
- **Time**: Persist, Delay, Repeat, Instant
- **Interaction**: Absorb, Transfer, Attract, Impregnate

### Mana costs by complexity
| Complexity | Indicative cost |
|-----------|----------------|
| Form only | 5–10 Mana |
| Form + 1 mod | 12–18 Mana |
| Form + 2 mods | 20–30 Mana |
| Form + 3 mods | 35–50 Mana |
| Form + 4 mods | 55–75 Mana |

Starting Mana pool: ~100. Grows via Arcanite Fountains in Libraries.

### Environmental synergies
Spells leave terrain effects. Casting a compatible school on existing terrain triggers a Synergy:
- Ignis + water on ground = **Steam** (blind zone)
- Ignis + wind zone = **Firestorm** (spread damage)
- Aqua + wind = **Hailstorm** (AoE slow)
- Aqua + loose earth = **Mud** (trap)
- Terra + fire on ground = **Magma** (massive persistent damage)
- Lux + shadow zone = **Eclipse** (pure damage burst)
- Ferrum + fire = **Forge** (temporary weapon buff)

## Progression — Savoir Magique
No traditional XP levels. Knowledge grows through:
1. **Helping villagers** → basic runes and forms
2. **Ancient Libraries** → advanced runes, lore, modifier recipes
3. **Free experimentation** → discovering new combos in the Grimoire

Savoir thresholds unlock: extra spell slots (2→3→4), Mana pool increases,
ability to hold more modifiers per spell, new base forms, access to new zones/Libraries.

Mid-game: choose 2 main schools (reduced Mana cost, mastery runes, exclusive synergies).
Re-assignable at village — never punishes experimentation.

---

## Folder Structure — Target (v4.0)
```
Assets/_MainProject/Scripts/
├── Core/
│   ├── GameInput.cs              # Input abstraction (REFACTOR from v3.1)
│   ├── HealthSystem.cs           # HP for player + enemies (KEPT from v3.1)
│   ├── HitFeedback.cs            # Visual hit reactions (KEPT from v3.1)
│   ├── ManaSystem.cs             # Mana resource (NEW — replaces StaminaSystem for casting)
│   ├── SavoirSystem.cs           # Savoir Magique progression (NEW)
│   └── SaveManager.cs            # Save / load (NEW)
├── Player/
│   ├── PlayerController.cs       # 3P movement via CharacterController (REFACTOR — camera-relative stays)
│   ├── PlayerCombat.cs           # 4 spell slots, casting logic (NEW — replaces CombatController)
│   ├── DodgeRoll.cs              # Dodge with short i-frames ~0.2s (KEPT — simplified)
│   ├── SprintController.cs       # Sprint (KEPT — now uses light stamina or free)
│   └── InteractionController.cs  # NPC / object interaction (NEW)
├── SpellCraft/
│   ├── Data/
│   │   ├── SpellRecipe.cs        # SO — complete spell recipe (NEW)
│   │   ├── BaseFormData.cs       # SO — per-form data (NEW)
│   │   ├── SchoolData.cs         # SO — per-school data + VFX profile (NEW)
│   │   └── RuneModifier.cs       # SO — per-modifier rune data (NEW)
│   ├── Runtime/
│   │   ├── SpellCaster.cs        # Instantiates and launches spells (NEW — replaces SkillCaster)
│   │   ├── SpellFactory.cs       # Builds spell from recipe (NEW)
│   │   ├── ProjectileSpell.cs    # Projectile behavior (REFACTOR from SkillProjectile)
│   │   ├── ZoneSpell.cs          # Zone behavior (NEW)
│   │   ├── AuraSpell.cs          # Self-buff behavior (NEW)
│   │   ├── ImpactSpell.cs        # Melee/short-range behavior (NEW)
│   │   ├── ModifierProcessor.cs  # Applies rune modifiers to spell instance (NEW)
│   │   └── ISpellModifier.cs     # Interface for modifier application (NEW)
│   └── Synergies/
│       ├── SynergyData.cs        # SO — synergy definition (NEW)
│       ├── SynergyDetector.cs    # Detects synergy conditions (NEW)
│       └── EnvironmentState.cs   # Tracks active terrain effects (NEW)
├── Grimoire/
│   ├── GrimoireUI.cs             # Main Grimoire interface (NEW)
│   ├── CraftingPanel.cs          # Node-graph crafting panel (NEW)
│   ├── SpellbookPanel.cs         # Saved spells list (NEW)
│   ├── RuneEncyclopedia.cs       # Discovered runes catalog (NEW)
│   ├── SynergyEncyclopedia.cs    # Discovered synergies catalog (NEW)
│   └── JournalPanel.cs           # World journal / bestiary (NEW)
├── Enemy/
│   ├── EnemyAI.cs                # FSM: Idle, Patrol, Chase, Attack, Hurt, Death (REFACTOR — remove posture)
│   ├── EnemySpawner.cs           # Spawn management (KEPT from v3.1)
│   ├── ElementalWeakness.cs      # Elemental weakness component (NEW)
│   └── GuardianBoss.cs           # Library boss logic with phases (NEW)
├── World/
│   ├── VillageEvolution.cs       # Village visual evolution system (NEW)
│   ├── ZoneUnlocker.cs           # Zone unlock via learned spells (NEW)
│   ├── EnvironmentEffect.cs      # Spell effects on terrain (NEW)
│   ├── NPCDialogue.cs            # Dialogue system (NEW)
│   └── LibraryManager.cs         # Library dungeon: puzzles, progression (NEW)
├── Camera/
│   ├── ThirdPersonCamera.cs      # Cinemachine 3P wrapper (NEW — replaces CameraController)
│   └── CameraEffects.cs          # Camera shake, zoom (NEW)
└── UI/
    ├── PlayerHUD.cs              # HP bar, Mana bar, spell slots (REFACTOR from v3.1)
    ├── DamageNumber.cs           # Stylized damage display (KEPT from v3.1)
    ├── QuestTracker.cs           # Quest tracking UI (NEW)
    └── DialogueUI.cs             # Dialogue interface (NEW)
```

## Code Conventions
- Naming: PascalCase for classes and methods, _camelCase for private fields
- **Comments in English**
- [SerializeField] for Inspector visibility, never public unless it's API
- [Header] and [Tooltip] on all balance/tuning values
- C# events (event Action<T>) for inter-system communication
- ScriptableObjects for ALL design data
- No singletons — dependency injection via Inspector
- One script = one responsibility
- No magic numbers — all tuning values exposed in Inspector

## Player Movement — IMPORTANT
Player uses **CharacterController**, NOT NavMeshAgent.
- WASD / left stick → movement direction (relative to camera)
- Camera is **100% game-controlled** — no mouse/stick camera input at all. Fixed
  elevated top-down angle (pitch ~50°), position tracks the player with damping,
  angle never rotates when the player turns (`CinemachineFollow.TrackerSettings.
  BindingMode = WorldSpace` — NOT `LockToTargetWithWorldUp`, which would swing
  the camera around with the player like an over-the-shoulder rig)
- Mouse is reserved for spell aiming (`PlayerController.FacingMode.Aim`, raycast
  to ground) — never for camera control
- Player faces movement direction by default (`FacingMode.Movement`); switches to
  facing the mouse cursor while casting (`FacingMode.Aim`, driven by PlayerCombat)
- Dodge: DodgeRoll with ~0.2s i-frames, directional
- Sprint: SprintController with fast-regen stamina (or free movement)

### Key difference from v3.1
Camera changed from PoE2-style isometric (fixed pitch/yaw, perspective from above,
player-locked mouse-aim rotation) to Cinemachine top-down elevated (fixed pitch,
game-controlled, no player rotation input at all — mouse now drives spell aim
instead of camera or facing). PlayerController camera-relative movement code
(`camForward * moveInput.y + camRight * moveInput.x`) is preserved — it works
regardless of camera angle. The camera wrapper changes, not the movement math.

## Combat — Key Differences from v3.1
| v3.1 (REMOVED) | v4.0 (CURRENT) |
|----------------|----------------|
| BlockSystem (hold RMB, perfect block) | No blocking — dodge + positioning |
| RiposteSystem (counter-attack window) | No riposte — spell timing is the skill |
| PostureSystem (stagger on posture break) | No posture bar — enemies have elemental weaknesses |
| StaminaSystem (shared dodge/block/sprint) | ManaSystem (spell casting resource) + light sprint cost |
| CombatController (priority: dodge > riposte > block > attack > sprint) | PlayerCombat (spell slots 1-4 + dodge) |
| 3-hit combo basic attack | No melee combo — Impact form spells for close range |
| TTK calibration (3-5 hits to kill) | Varies by spell complexity and synergies |
| AimIndicator (ground reticle) | 3P aiming (crosshair or aim assist) |
| Hit stop on basic attacks | Hit stop on impactful spells only |

---

## Migration Status — v3.1 Code Inventory

### ✅ KEPT (works as-is or with minor tweaks)
| Script | Location | Notes |
|--------|----------|-------|
| HealthSystem.cs | Core/ | ✅ UPDATED (2026-07-30). `TakeDamage` no longer routes through CombatController.FilterIncomingDamage (archived) — checks `DodgeRoll.IsInvulnerable` directly for i-frames. No block/riposte damage reduction anymore (neither exists in v4.0). **Same day, later**: gained `ShieldAmount`/`AddShield`/`ClearShield`/`OnShieldChanged` for `AuraSpell` (SpellCraft Phase 6) — `TakeDamage` drains shield before HP, same early-check pattern as the `DodgeRoll` i-frame check. |
| HitFeedback.cs | Core/ | Keep white flash + scale punch. Remove block-specific colors (blue/gold/red). |
| DodgeRoll.cs | Player/ | ✅ DONE (2026-07-27). i-frame 0.3s → 0.2s. The "riposte window trigger" was removed from RiposteSystem's side (it listened to `DodgeRoll.OnDodgeEnd`), not from DodgeRoll itself. |
| SprintController.cs | Player/ | Keep as-is. Possibly make sprint free (no stamina cost). |
| DamageNumber.cs | UI/ | Keep — fully code-created via DamageNumber.Spawn(), no prefab dependency. |
| EnemySpawner.cs | Enemy/ | Keep as-is. |
| HeroCharacter models | Models/ | ⚠️ SUPERSEDED (2026-07-31) — Player now uses the 3rdParty/ATART Wizard model instead (see note below the table). HeroCharacter_Rigged.glb kept in the project but no longer wired to Player; still a candidate if the team wants a from-scratch (non-3rdParty) hero later. |
| Animations | Animations/ | HeroAnimator.controller kept in the project but unused by Player (see Wizard swap note below) — never was wired to Player either, this table entry described the Phase 5/6 plan, not something completed. |
| VFX/Ignis/ prefabs | Prefabs/VFX/ | Keep particle systems. Restyle for toon look (brighter, more stylized). |

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

### 🔄 REFACTORED (core logic preserved, interface changed)
| Script | What changes |
|--------|-------------|
| PlayerController.cs | ✅ DONE (2026-07-27). Removed iso mouse-aim-drives-rotation. Added `FacingMode` enum (Movement/Aim) — `Movement` faces move direction, `Aim` faces mouse raycast on ground (`AimWorldPosition`, switched by PlayerCombat during cast). Kept: camera-relative WASD math, CharacterController.Move(), public API (LockMovement, MoveByDelta, Teleport, etc.) |
| GameInput.cs | ✅ DONE (2026-07-27, completed 2026-07-30). Removed: block input (RMB/LT) — BlockSystem no longer receives input. Added: spell slot 1-4 → `PlayerCombat.TryCastSlot`, `OnGrimoireTogglePressed` (Tab) and `OnInteractPressed` (E) events (no consumer yet — GrimoireUI/InteractionController are Phase 6/7). Kept: WASD, dodge (Space), sprint (Shift), gamepad detection. **2026-07-30**: CombatController fully archived — dodge and sprint input now call `DodgeRoll.TryDodge`/`SprintController.SetSprintInput` directly (no more `combatController.OnDodgeInput`/`OnSprintInput`/`OnAttackInput` indirection). Basic melee attack input (left click / X-Square) dropped entirely — v4.0 has no melee combo. |
| EnemyAI.cs | ✅ PARTIAL (2026-07-27). Removed: PostureSystem.IsStaggered checks, posture-aware attack cancels, HandleStaggerEnter/Exit. Kept: FSM (Idle/Chase/Attack/Dead), NavMeshAgent, detection/aggro ranges. **Not done yet**: ElementalWeakness component reference, hurt state — deliberately deferred, these belong to Phase 8 ("ElementalWeakness on all enemies"), not Phase 5. |
| PlayerHUD.cs | ✅ DONE (2026-07-27). Removed: stamina bar, riposte "RIPOSTE!" indicator, action bar with keybinds (whole thing, not just block/riposte rows). Added: Mana bar (below HP, single color, no tiered/flash animation like old stamina), 4 spell slot icons with cooldown overlay + school color (unchanged from v3.1). Kept: HP bar (top-left), auto-create Canvas. |
| CombatVisualFeedback.cs | ✅ GUTTED (2026-07-30). Removed: sword-swing arc (attack combo, v3.1 only) + shield disc/block tint (no block in v4.0), and the CombatController/BlockSystem references driving them. Kept: sprint trail + squash/stretch/tilt, dodge trail + flatten/tint — both still part of the Socle Commun. |
| HitFeedback.cs | ✅ DONE (2026-07-30, follow-up to the 2026-07-27 color-removal note). BlockSystem reference and the block-hit/perfect-block/block-broken flash handlers removed entirely (not just the colors). Kept: damage flash + scale punch + death feedback. |
| SkillCaster.cs / SkillProjectile.cs | ✅ DONE (2026-07-30). Removed PostureSystem lookups and the stagger damage-multiplier branch in `ApplyAoE`/`Update` — v4.0 has no posture/stagger, damage is applied at face value (`skill.baseDamage` / `_damage`). |
| SkillProjectile.cs | Not started — Phase 6 (SpellRecipe system). Rename → ProjectileSpell.cs. Refactor to read from SpellRecipe instead of SkillData. Add ISpellModifier hooks for bounce/split/homing. Keep: physics movement, collision detection, VFX instantiation. |

### ❌ REMOVED (archive in _Archive/ folder, do not use)
| Script | Reason |
|--------|--------|
| CombatController.cs | ✅ ARCHIVED (2026-07-30) → `Scripts/_Archive/CombatController.cs.txt`. Replaced by PlayerCombat.cs (spell slots) + direct GameInput→DodgeRoll/SprintController wiring. Component removed from Player.prefab; GameInput.combatController field replaced by dodgeRoll/sprintController fields wired to the same GameObjects. |
| BlockSystem.cs | ✅ ARCHIVED (2026-07-30) → `Scripts/_Archive/BlockSystem.cs.txt`. No blocking mechanic in v4.0. Component removed from Player.prefab; HitFeedback/CombatVisualFeedback no longer reference it. |
| RiposteSystem.cs | ✅ ARCHIVED (2026-07-30) → `Scripts/_Archive/RiposteSystem.cs.txt`. No riposte mechanic in v4.0. Component removed from Player.prefab. |
| PostureSystem.cs | ✅ ARCHIVED (2026-07-30) → `Scripts/_Archive/PostureSystem.cs.txt`. No posture/stagger in v4.0. Component removed from Enemy.prefab; SkillCaster/SkillProjectile no longer read `IsStaggered`/`DamageMultiplierWhenStaggered`, damage applied at face value. |
| StaminaSystem.cs | **KEPT active** — not part of this archiving pass. Still consumed by DodgeRoll (dodge cost) and SprintController (drain/sec); archiving it would require deciding "sprint may be free" (open question, not yet resolved) and migrating those two systems off it first. BlockSystem removed as a consumer (doc comment updated 2026-07-30). |
| PostureBarUI.cs | ✅ ARCHIVED (2026-07-30) → `Scripts/_Archive/PostureBarUI.cs.txt`. No posture bar in v4.0. Component removed from Enemy.prefab. |
| AimIndicator.cs | ✅ ARCHIVED (2026-07-27) → `Scripts/_Archive/AimIndicator.cs.txt` (renamed `.txt`, Unity compiles `.cs` regardless of folder name). |
| CameraController.cs | ✅ ARCHIVED (2026-07-27) → `Scripts/_Archive/CameraController.cs.txt`. Component removed from Main Camera in MovementGym.unity. |
| WorldHealthBar.cs | Simplify into new version without posture bar pairing. Not started — no hard reference to PostureBarUI was found (positioning was via independent `yOffset` values, not an object link), so archiving PostureBarUI didn't break it, but the "no pairing" simplification pass itself is still pending. |
| EnemyTelegraph.cs | Already a simple generic visual indicator (Circle/Cone/FullBoss), never referenced PostureSystem — no change needed here despite the note. |
| StaggerVFX.cs | ✅ ARCHIVED (2026-07-30) → `Scripts/_Archive/StaggerVFX.cs.txt`. No stagger in v4.0. Component + its UnityEvent listeners on PostureSystem removed together from Enemy.prefab (self-contained pair, no dangling refs). |
| FixHeroModelHeight.cs | Editor-only tool — review if still relevant. Not reviewed. |

### 📦 EXISTING DATA ASSETS — Status
| Asset | Path | Status |
|-------|------|--------|
| Ignis_TraitDeBraise (SkillData SO) | Data/Skills/ | Still ACTIVE for now (Phase 5) — eventually ARCHIVE, rebuild as SpellRecipe SO in Phase 6 |
| Ignis_Explosion (SkillData SO) | Data/Skills/ | Still ACTIVE for now — same as above |
| Ignis_MurDeFeu (SkillData SO) | Data/Skills/ | Still ACTIVE for now — same as above |
| Ignis_Meteore (SkillData SO) | Data/Skills/ | Still ACTIVE for now — same as above |
| Ignis_Firewall (SkillData SO) | Data/Skills/ | Still ACTIVE for now — same as above |
| SkillData.cs | Scripts/Skills/ | Still ACTIVE (Phase 5) — **field renamed** `staminaCost` → `manaCost` (2026-07-27, `[FormerlySerializedAs]` preserved existing tuned values on the 5 assets above). Eventually ARCHIVE, replaced by SpellRecipe.cs in Phase 6 |
| SkillCaster.cs | Scripts/Skills/ | Still ACTIVE — now reads ManaSystem instead of StaminaSystem, and PlayerCombat instead of CombatController (2026-07-27). Eventually ARCHIVE, replaced by SpellCaster.cs + SpellFactory.cs in Phase 6 |
| VFX Ignis particle systems | Prefabs/VFX/Ignis/ | KEEP — reusable, restyle for toon aesthetic |
| HeroAnimator.controller | Animations/ | KEEP — add CastSpell animation states |

---

## Completed Work (v3.1 phases — code exists in project)

### Phase 1 ✅ — Core Movement & Game Feel
- WASD + mouse movement via CharacterController
- Basic enemy AI (Idle → Chase → Attack → Dead) with NavMeshAgent
- HealthSystem (player + enemies)
- PoE2-style camera (CameraController — to be replaced)
- Game feel: hit stop (Time.timeScale 0.05f for 60ms), DamageNumber.Spawn()
- AimIndicator ground reticle (to be replaced)

### Phase 2 ✅ — Common Skills (Combat v3.1)
- DodgeRoll: directional, i-frames 0.05s–0.35s, AnimationCurve movement, 25 stamina, 1.2s cooldown
- BlockSystem: normal (60% reduction, 120° arc) + perfect (90%, slowmo 0.15f)
- RiposteSystem: window after perfect block or dodge end, ×2 damage, 50% posture damage
- SprintController: ×1.6 speed, 10 stamina/sec
- StaminaSystem: shared resource, 1.5s regen delay, ×3 out-of-combat regen
- PlayerHUD: HP bar, stamina bar (dynamic color), action bar, riposte indicator

### Phase 3 ✅ — Enemy Telegraph + Posture/Stagger
- PostureSystem: posture pool (60 basic / 120 elite), regen, stagger state, ×2.5 damage during stagger
- PostureBarUI: world-space bar, amber fill, pulse at ≤30%, "STAGGER" text
- EnemyTelegraph: Circle (0.3s), Cone (0.5s), FullBoss (0.8-1.2s) red indicators
- StaggerVFX: visual effect on stagger
- EnemyAI integration: cancel attack if staggered during windup

### Phase 4 ✅ — First School Ignis
- SkillData ScriptableObject architecture
- SkillCaster: instantiation + casting logic
- SkillProjectile: physics-based projectile with VFX
- 4 active Ignis skills:
  - Trait de Braise (basic fire projectile)
  - Explosion Ignis (AoE burst)
  - Mur de Feu (fire wall)
  - Météore Ignis (heavy AoE)
- Procedural VFX particle systems per skill
- Skill bar HUD: slots 1-4 with cooldown overlay
- TTK calibration: baseDamage 25, enemyDamage 17, enemyHP 100

---

## New Roadmap (v4.0)

### Phase 5 — Core Pivot (camera + movement + cleanup)
- [x] Install Cinemachine, create ThirdPersonCamera — **top-down, fixed angle,
      100% game-controlled** (design changed mid-phase from the original
      "over-the-shoulder" plan — see Player Movement section above)
- [x] Refactor PlayerController for the new camera — `FacingMode` (Movement/Aim),
      no camera-rotation coupling since the camera takes no player input
- [x] Refactor GameInput (removed block/riposte input; added spell slots 1-4,
      `OnGrimoireTogglePressed`/`OnInteractPressed` events with no consumer yet)
- [x] Create ManaSystem (replaces StaminaSystem for casting)
- [x] Create PlayerCombat (spell slot casting + FacingMode.Aim bridge — does
      **not** yet own dodge, see note below)
- [x] Archive removed scripts to _Archive/ folder — **done (2026-07-30)**:
      CombatController/BlockSystem/RiposteSystem/PostureSystem/PostureBarUI/
      StaggerVFX moved to `Scripts/_Archive/*.cs.txt`, components removed from
      Player.prefab and Enemy.prefab. Rewired in the process: HealthSystem
      (dodge i-frames checked directly via DodgeRoll, no more
      CombatController middleman), GameInput (dodge/sprint wired straight to
      DodgeRoll/SprintController, basic melee attack input dropped —  v4.0
      has no melee combo), CombatVisualFeedback (gutted: sword-swing + shield
      visuals removed, sprint/dodge visuals kept), HitFeedback (block-hit/
      perfect/broken flashes removed), SkillCaster/SkillProjectile (posture
      stagger-damage branch removed). **StaminaSystem intentionally NOT
      archived** — still consumed by DodgeRoll and SprintController; removing
      it needs the "sprint may be free" open question resolved first.
- [x] Simplify DodgeRoll (i-frame 0.3s → 0.2s; riposte-window-on-dodge-end
      removed — that trigger actually lived in RiposteSystem's own
      subscription to `DodgeRoll.OnDodgeEnd`, not in DodgeRoll itself)
- [x] Simplify EnemyAI (remove posture checks) — ElementalWeakness/hurt state
      intentionally deferred to Phase 8, not added here
- [x] Refactor PlayerHUD (HP + Mana + 4 spell slots)
- [x] Basic cel-shader setup — **done (2026-07-30)**, hand-authored HLSL/
      ShaderLab shader (`Shaders/ToonCel.shader`, `AARPG/ToonCel`) instead of
      a Shader Graph node graph: the MCP tooling available this session can
      only edit `.shader` text, not `.shadergraph` node JSON (user confirmed
      hand-written HLSL over a blind/error-prone hand-edited node graph).
      Forward + ShadowCaster passes, banded (stepped) diffuse lighting, ambient
      floor so the shadow band never reads pure black (design pillar: never
      dark/grim), banded specular, and a rim light for the warm toon glow.
      Compiles clean (0 errors). Demo materials `ToonCel_Player.mat`/
      `ToonCel_Enemy.mat` created and assigned — confirmed visually via
      screenshot on the Player capsule (clear light/shadow band + rounded
      specular highlight, no black shadow). **Not applied to the imported
      Skeleton_Warrior/Synty models** — reskinning existing art assets is the
      Phase 11 art pass, out of scope for this foundational shader setup. If
      the team wants Shader Graph specifically (node-based, artist-editable)
      instead of/alongside this HLSL version, that still needs to be built by
      hand in the Unity Editor — not reachable through current MCP tools.
- [x] Test: player can move in 3P, dodge, cast 1 basic spell, kill 1 enemy —
      **done (2026-07-30) via Unity MCP + Play Mode**, exercising specifically
      the code touched by this session's CombatController/PostureSystem
      archiving: `HealthSystem.TakeDamage` on both an entity with DodgeRoll
      (Player, normal damage confirmed) and without (Enemy, lethal hit →
      `IsDead=true` confirmed); `DodgeRoll.TryDodge` → `IsDodging=true`
      synchronously, correctly gating `PlayerCombat.CanAct`; full cast
      pipeline `PlayerCombat.TryCastSlot` → `SkillCaster.TryCastSkill` →
      mana 100→75 (Ignis_Explosion cost 25), cooldown started, FacingMode
      switched to Aim. Zero console errors across both Play Mode sessions.
      **Still not a live human playtest** — this MCP environment's Play Mode
      does not tick frames between separate tool calls (confirmed again this
      session: `Time.frameCount` stayed at 2 across multiple calls), so
      continuous input (camera damping feel, movement smoothness, precise
      i-frame timing) still needs a real keyboard/mouse session to validate.

**Phase 5 is functionally complete** as of 2026-07-30 — all checklist items
done except the human feel-playtest noted above, which is a standing
limitation of this tooling, not unfinished work.

### Phase 6 — Spell Crafting Core
- [x] SpellRecipe + RuneModifier ScriptableObjects — **done (2026-07-30)**.
      `SpellCraft/Data/`: `SpellEnums.cs` (`SpellBaseForm`, `SpellSchool`,
      `RuneCategory` — deliberately separate from the existing `SkillSchool`/
      `SkillType` in `Skills/SkillData.cs`, same pattern as ManaSystem staying
      decoupled from StaminaSystem, so archiving the v3.1 skill system later
      needs zero changes here), `BaseFormData` (per-form baseManaCost/
      baseCooldown), `SchoolData` (per-school display name + primary/secondary
      color from the Schools table), `RuneModifier` (**abstract** SO base
      implementing `ISpellModifier` — concrete runes are subclasses, not yet
      created, that's the "4 basic modifier runes" item below), `SpellRecipe`
      (composes BaseFormData + SchoolData + up to 4 RuneModifier, `ManaCost`/
      `CooldownTime` computed live as `base × Π(1 + rune.multiplier)` rather
      than cached/serialized, `OnValidate` prunes mutually-incompatible rune
      pairs). Seed data assets created under `Data/SpellCraft/`: 4
      `BaseFormData` (Projectile/Zone/Aura/Impact, mana costs 6-9 — mid-range
      of the "Form only: 5-10 Mana" bracket), all 7 `SchoolData` (colors from
      the Schools table), 1 example `SpellRecipe`
      (`Ignis_Projectile_Base`, no runes yet since none exist).
- [x] SpellFactory + ModifierProcessor + ISpellModifier — **done (2026-07-30)**.
      `SpellCraft/Runtime/`: `ISpellModifier` (single `OnSpawn(SpellContext)`
      hook — deliberately minimal, `OnHit`/`OnExpire` will be added once the
      base forms that need them exist, not speculatively now), `SpellContext`
      (minimal MonoBehaviour data carrier — recipe/caster/origin/direction —
      **not** in CLAUDE.md's original target tree, added because the 4 base
      forms need *something* common to read from; they'll attach their
      behavior components onto the same GameObject `SpellFactory` creates
      rather than replacing this), `ModifierProcessor` (iterates
      `recipe.ModifierRunes` and calls `rune.OnSpawn`), `SpellFactory`
      (instantiates the spell root GameObject + `SpellContext`, calls
      `ModifierProcessor` — does **not** touch Mana/cooldown, that stays the
      caller's job, same split as `SkillCaster` today). Verified end-to-end
      via Unity MCP `execute_code` in Play Mode (Reflection.Emit-generated
      disposable test rune, not a real content asset): baseline recipe
      (0 runes) → ManaCost=7/CooldownTime=1 exactly matches `BaseForm_Projectile`;
      1 rune (×0.5 mana, ×0.2 cooldown) → 10.5/1.2 exactly as the formula
      predicts; `OnValidate` correctly nulled out an incompatible second rune;
      `SpellFactory.CreateSpell` produced a correctly-populated `SpellContext`
      and the test rune's `OnSpawn` fired through `ModifierProcessor`. Zero
      console errors.
- [x] 4 base forms functional (Projectile, Zone, Aura, Impact) — **done
      (2026-07-30)**, all 4 in `SpellCraft/Runtime/`. `BaseFormData` gained
      combat tuning fields (`baseDamage`, `range`, `radius`, `projectileSpeed`,
      `projectileSize`, `duration`, `tickInterval` — same flat-fields-for-all-
      forms convention as the v3.1 `SkillData`, since each `BaseFormData`
      instance is form-specific so unused fields per form are harmless).
      `SpellFactory.CreateSpell` signature gained a `LayerMask hitLayer`
      parameter and now attaches+inits the matching base form behaviour after
      running `ModifierProcessor`, switching on `recipe.baseForm.baseForm`.
      No `OnHit`/`OnExpire` modifier hook yet on any form — `ISpellModifier`
      still only exposes `OnSpawn` (see that file's comment) — so bounce/
      homing/split/etc. wait for the "4 basic modifier runes" item below.
      - **ProjectileSpell**: straight-line movement + manual `OverlapSphere`
        hit detection (ported from `Skills/SkillProjectile.cs`), builds its
        own primitive-sphere visual colored by `recipe.school.primaryColor`
        (visual creation used to live in `SkillCaster.CastProjectile`, now
        owned by the form behaviour itself since `SpellFactory` has no
        caster-side coroutine to do it).
      - **ZoneSpell**: flat cylinder visual, ticks damage via `OverlapSphere`
        every `tickInterval` for `duration` then self-destroys (ported from
        `SkillCaster.CastZone`/`ApplyAoE`). Note: Unity runs a coroutine's
        body synchronously up to its first `yield` the moment `StartCoroutine`
        is called, so the first tick fires immediately on cast, same as the
        v3.1 original (`nextTick` starts at 0) — not a bug, tripped up an
        early manual-tick test that double-counted this.
      - **AuraSpell**: self-buff on the caster. Design decision (user choice,
        2026-07-30, since no buff/resistance system existed yet): implements
        an **absorbing shield**, not a generic resistance/buff — reuses
        `BaseFormData.baseDamage` as the shield amount (no new field, same
        reuse pattern as `range` serving double duty across forms). Required
        adding shield support to `Core/HealthSystem.cs` itself:
        `ShieldAmount` property, `AddShield`/`ClearShield` methods,
        `OnShieldChanged` event, and `TakeDamage` now drains `ShieldAmount`
        before touching `CurrentHealth` (same pattern as the existing
        `DodgeRoll.IsInvulnerable` early-return check). A true generic buff/
        resistance system (damage multipliers, movement speed, etc.) is
        still undesigned — flag it for discussion before assuming Aura can
        do more than shield.
      - **ImpactSpell**: instant single-hit `OverlapSphere` damage at the
        cast point, no travel, no persistence, brief expanding-sphere visual
        (0.3s) then self-destroy (ported from `SkillCaster.CastAoE`/
        `ApplyAoE`, minus the `castTime` telegraph — telegraphing is a
        separate future concern, not in this item's scope).
      All 4 verified individually via Unity MCP Play Mode (`execute_code`,
      frame ticking between tool calls doesn't advance normally — same
      limitation noted in Phase 5/6 above — so tests invoke the relevant
      method directly instead of waiting on real frames):
      Projectile moved and dealt exactly 30 damage (100→70 HP) to a
      `HealthSystem` on the `Enemy` layer, correctly gated by `hitLayer` (an
      earlier `~0` "everything" test mask hit the scene's real Player object
      instead — confirms the layer filter is load-bearing, not decorative);
      Zone dealt 30/tick and left an enemy at 10 HP after 3 ticks; Aura gave
      the caster a 30-point shield that absorbed a 20-dmg hit fully (HP
      unchanged, shield→10) then absorbed the remaining 10 on a 50-dmg hit
      with 40 overflowing to HP (100→60); Impact hit an in-radius enemy
      (100→70) and correctly ignored one placed outside `radius`. All 4
      `BaseForm_*.asset` files tuned explicitly (not left on C# class
      defaults) via `manage_scriptable_object`. Zero console errors/warnings
      across every test.
- [x] 3 schools playable (Ignis, Aqua, Terra) — **done (2026-07-30)**. User
      chose a signature-effect approach (vs. visual-only) matching the GDD
      "Combat role" table. New `SpellCraft/Runtime/SchoolEffectApplier.cs`:
      single entry point called by `ProjectileSpell`/`ZoneSpell`/`ImpactSpell`
      right before `TakeDamage` (not `AuraSpell` — it never targets an
      enemy). Switches on `recipe.school.school`; Ventus/Lux/Umbra/Ferrum
      have no case yet (no-op) until their own Phase 8 item. `SchoolData`
      gained per-school tuning fields (burn/slow/knockback — same flat-
      fields-for-all-schools convention as elsewhere, unused fields per
      school are harmless).
      - **Ignis → Burn (DoT)**: new `BurnStatus.cs`, attached to the target's
        `HealthSystem` GameObject, ticks damage on a timer, re-`Init`
        refreshes duration instead of stacking a second component.
      - **Aqua → Slow**: new `SlowStatus.cs`, multiplies `NavMeshAgent.speed`
        for a duration then restores it. Only works on entities with a
        `NavMeshAgent` (v4.0 convention: "NavMeshAgent for enemies only") —
        an Aqua spell hitting the player currently has no effect, since
        `PlayerController` has no external speed hook. Flagged in-code as a
        gap to revisit if enemies ever cast spells.
      - **Terra → bonus damage + Knockback**: `damage *= 1 + bonusMultiplier`
        applied inline (no status component needed, it's instantaneous), plus
        new `Knockback.cs` which disables the target's `NavMeshAgent` for
        0.2s while manually displacing its transform, then re-enables the
        agent (a `NavMeshAgent` ignores manual `transform.position` writes
        while enabled — this is the standard workaround, `EnemyAI` needs no
        awareness of it since `SetDestination` just resumes once the agent
        re-enables).
      Verified via Unity MCP Play Mode against a real instantiated
      `Enemy.prefab` (not a synthetic test object, specifically to exercise
      the real `NavMeshAgent`/`HealthSystem` pairing) placed at the existing
      Enemy's on-NavMesh position (-4.32, 0.88, -9.21): Ignis Impact dealt 30
      direct damage (50→20 HP — this prefab's `maxHealth` is 50, not the 100
      used in earlier synthetic tests) then, with `BurnStatus.TickLoop`
      manually driven via reflection (`IEnumerator.MoveNext()` in a loop,
      since coroutines don't advance between tool calls — same limitation
      noted throughout Phase 5/6), 2 more ticks of 5 landed (20→10, the loop
      exits at the exact `duration` boundary without firing a 3rd — matches
      `ZoneSpell`'s identical tick-loop shape, not a bug). Aqua Impact
      dropped `NavMeshAgent.speed` from 5→2.5 synchronously on cast (exactly
      `slowMultiplier`). Terra Impact dealt 36 damage (50→14, exactly
      30×1.2) and displaced the enemy's `transform.position` by ~0.12 units
      with `agent.enabled=false` observed immediately after cast (Unity runs
      a coroutine body synchronously up to its first `yield`, so only the
      first push increment lands within the same tool call — same mechanism
      already noted for `ZoneSpell`'s first-tick-fires-immediately behavior).
      All 3 `School_*.asset` files tuned explicitly. Zero console
      errors/warnings.
- [x] 4 basic modifier runes (Bounce, Split, Persist, Expand) — **done
      (2026-07-30)**. `SpellContext` gained typed mutable accumulators
      (`BounceCount`, `ExtraProjectileCount`, `DurationMultiplier`,
      `RadiusMultiplier`) populated by `RuneModifier.OnSpawn` and read by the
      base form behaviours right after — deliberately NOT a generic
      key-value bag or event bus, each value maps to exactly one of these 4
      runes, kept strongly typed. New concrete `RuneModifier` subclasses in
      `Data/`: `BounceRune.cs` (Trajectory), `SplitRune.cs` (Shape),
      `PersistRune.cs` (Time), `ExpandRune.cs` (Shape) — each just a tuning
      field + a one-line `OnSpawn` override.
      - **Bounce**: `ProjectileSpell` reads `BounceCount` at Init; on hit, if
        bounces remain, reflects `_direction` off a vector from the impact
        point to the target (`Vector3.Reflect`, using target position as a
        crude surface normal — no real collision normal available from
        `OverlapSphere`) and keeps flying instead of destroying itself.
      - **Split**: `ProjectileSpell` reads `ExtraProjectileCount` at Init and
        spawns sibling `ProjectileSpell` GameObjects in a fan (15° step),
        sharing the primary's `SpellContext`. Required caching
        `SpellRecipe _recipe` instead of holding a live `SpellContext`
        reference for anything read after Init — siblings live on separate
        GameObjects from the primary, so if the primary is destroyed first
        (e.g. it hits something before a sibling does), a sibling still
        holding `_context.Recipe` would throw `MissingReferenceException`;
        `SpellRecipe` is a ScriptableObject asset, safe to cache independent
        of any GameObject's lifecycle. An `isPrimary` flag prevents siblings
        from re-triggering their own split (would infinite-loop otherwise,
        since they share the same context with `ExtraProjectileCount > 0`).
      - **Persist**: `ZoneSpell`/`AuraSpell` multiply `form.duration` by
        `DurationMultiplier`. No effect on Projectile/Impact (no duration to
        extend) — not a bug, same "field irrelevant for this form" pattern
        used throughout `BaseFormData`.
      - **Expand**: `ZoneSpell`/`ImpactSpell` multiply `form.radius` by
        `RadiusMultiplier`. No effect on Projectile/Aura.
      Verified via Unity MCP Play Mode: Split produced exactly 3
      `ProjectileSpell` instances (1 primary + 2 extra) and the correct
      `ManaCost` (7 × 1.6 = 11.2, base × (1+0.6) rune multiplier); Bounce
      consumed one charge on its first hit (`BounceCount` 2→1), redirected,
      then self-expired via max-range (never needed its second charge since
      the reflected path didn't hit anything else — confirms the bounce
      redirect itself, not a stall); Persist doubled a Zone's effective
      duration (3→6, radius unchanged at 2); Expand widened an Impact's hit
      radius enough to catch an enemy placed at 2.5 units (base radius 2,
      expanded 3) that a base-radius Impact would've missed. 4 `Rune_*.asset`
      files created under `Data/SpellCraft/Runes/` with explicit tuning.
      Zero console errors/warnings.
- [x] Grimoire UI — basic crafting panel (node-graph) — **done (2026-07-30)**.
      **Phase 6 is now fully complete.** User explicitly chose a real
      draggable node-graph over a simpler button-picker panel (accepting the
      extra effort/risk of tuning UI without live visual feedback in this
      MCP environment). Scoped to crafting only — `SpellbookPanel`/
      `RuneEncyclopedia`/`SynergyEncyclopedia`/`JournalPanel` are the
      Phase 8 item "Full Grimoire (craft, spellbook, rune encyclopedia,
      synergy log, journal)", not built here. New `Grimoire/` folder (first
      files in it):
      - `CraftingNode.cs` — generic draggable node (`IBeginDragHandler`/
        `IDragHandler`/`IEndDragHandler`), carries a `NodeKind` (Form/School/
        Rune) + the actual asset payload, drag position resolved via
        `RectTransformUtility.ScreenPointToLocalPointInRectangle` against its
        parent rect (avoids scale-drift issues a raw `eventData.delta`
        accumulation would have). Delegates all connect/disconnect decisions
        to `CraftingPanel.HandleNodeDropped` — the node itself holds no
        graph-state opinion.
      - `CraftingPanel.cs` — the graph: a central "recipe core" (shows a
        live Mana/Cooldown/rune-count preview, recomputed via a cached
        in-memory `SpellRecipe` instance — `ScriptableObject.CreateInstance`,
        never written to disk), 6 ring-arranged slot positions (1 Form, 1
        School, 4 Runes) at fixed angles around the core, and a palette of
        draggable source nodes (left column = 4 Forms, right column = 7
        Schools, bottom row = 4 Runes — all 4 existing `RuneModifier`
        assets). Drop within `CoreDropRadius` (150px, checked via
        `anchoredPosition.magnitude` since all nodes/core share the same
        parent rect so this needs no screen-space conversion) → connects
        (Form/School replace any existing connection of the same kind; Rune
        fills the first empty of 4 slots, rejects a 5th with on-screen
        feedback text). Drop outside the radius on an already-connected node
        → disconnects it and snaps it back to its palette origin. Two
        "Sauver → Slot 1/2" buttons call `SpellCaster.SetSlot` (new public
        method — `_slots` had no setter before, only `GetSlot`) with a
        freshly created `SpellRecipe` instance built from the current
        connections.
      - `GrimoireUI.cs` — hosts `CraftingPanel` inside a
        `ScreenSpaceOverlay` Canvas (sortingOrder 200, above `PlayerHUD`'s
        100) built entirely in code at runtime, same convention as
        `PlayerHUD`/`DamageNumber` (no prefabs). Toggled by
        `GameInput.OnGrimoireTogglePressed` (Tab) — the event has existed
        since Phase 5 with no consumer until now. Calls
        `PlayerController.LockMovement` while open (same API `DodgeRoll`
        already uses for its own i-frame gating). Auto-creates an
        `EventSystem` + `StandaloneInputModule` if the scene doesn't have
        one yet (it didn't) — required for any uGUI drag/click interaction
        to receive input at all.
      Added to the Player GameObject in `MovementGym.unity` with all 4
      `BaseFormData`, all 7 `SchoolData`, and all 4 `Rune_*.asset` wired into
      its Inspector arrays (palette contents are **not** discovered via
      `Resources.Load`/`AssetDatabase` — the latter is editor-only and would
      break in a real build — so new future runes/schools need to be added
      to this array manually until the Phase 8 "Full Grimoire" pass
      revisits data sourcing, e.g. a `Resources` folder or an addressables
      catalog).
      Verified via Unity MCP Play Mode, exercising the real public API a
      live drag would call (`CraftingPanel.HandleNodeDropped`, not a
      simulated `PointerEventData` — dragging itself is standard uGUI event
      plumbing already proven throughout the engine, not this session's new
      code, so the test targets the actually-new connect/disconnect/save
      logic instead): opening the Grimoire correctly built all 15 palette
      nodes and locked player movement; dropping Form=Projectile,
      School=Ignis, and runes Bounce+Split onto the core produced the exact
      expected preview (16.8 Mana = 7 × 1.5 × 1.6, 1.3s CD = 1 × 1.15 ×
      1.15, 2 runes); saving to slot 1 correctly populated
      `SpellCaster.GetSlot(0)`; disconnecting the Split node (dragged past
      the drop radius, dropped again) correctly reduced the **live preview**
      back to 1 rune (10.5 Mana) **without retroactively changing the
      already-saved slot 1 recipe** — confirms `SaveToSlot` snapshots
      correctly rather than holding a live reference to panel state; closing
      the Grimoire and casting slot 1 for real through
      `PlayerCombat.TryCastSlot` spent exactly the saved recipe's 16.8 mana
      and spawned 3 `ProjectileSpell` instances (1 primary + 2 from Split) —
      a fully player-crafted spell, cast through the real gameplay path,
      behaving correctly. Zero console errors/warnings.
- [x] Terrain effects (fire on ground, water puddle) — **done (2026-07-30)**.
      Scoped deliberately narrow to match this item's literal wording — just
      the ground-marking foundation, NOT synergy detection/reaction
      (Ignis+water=Steam etc.), which is explicitly the separate Phase 8 item
      "Environmental synergies (10 combinations)". New `TerrainType` enum
      (Fire/Water/Wind/Shadow/LooseEarth — the 5 schools that mark terrain
      per the "World role" column of the Schools table; Lux/Ferrum don't get
      a case) in `SpellEnums.cs`. New `SpellCraft/Synergies/
      EnvironmentState.cs` (first file in this folder — matches the
      `SpellCraft/Synergies/` location already named in the target
      architecture tree): a **static** registry (not a MonoBehaviour
      singleton — spells are instantiated dynamically by `SpellFactory` with
      no Inspector reference to a central object, and the project convention
      is "No singletons — dependency injection via Inspector," so a plain
      static class, same precedent as `SpellFactory`/`SchoolEffectApplier`,
      fits better than a scene singleton) tracking active terrain patches
      (type/position/radius/expiry) with `RegisterPatch`/`HasPatchAt`, plus a
      `TryGetTerrainType(SpellSchool)` lookup. `ZoneSpell.Init` registers a
      patch matching its own school right when it spawns, living exactly as
      long as the zone itself (no separate lingering-after-zone-expires
      layer — kept simple for this pass). Only `ZoneSpell` leaves terrain
      (Projectile/Aura/Impact don't — Zone is the one form GDD explicitly
      frames as "terrain control," see the base forms table). Verified via
      Unity MCP Play Mode: an Ignis Zone left a `Fire` patch detectable via
      `HasPatchAt` at its cast position (and correctly did NOT register as
      `Water`); an Aqua Zone (cast far enough away to avoid overlap) left a
      `Water` patch, with a query 50 units away correctly returning false —
      confirms the spatial radius check isn't a global flag. Zero console
      errors/warnings. `SynergyData.cs`/`SynergyDetector.cs` (the Phase 8
      files that will query this registry to actually trigger Steam/Mud/
      Magma/etc.) do not exist yet — don't assume synergies fire in-game
      from this work alone.
- [x] 2 active spell slots — **done (2026-07-30)**. New `SpellCraft/Runtime/
      SpellCaster.cs`: player-side orchestrator for the SpellRecipe pipeline,
      same shape as `Skills/SkillCaster.cs` (cooldown array, mana gating,
      `GetCastTarget` via `PlayerController.AimWorldPosition`) but driving
      `SpellFactory.CreateSpell` instead of the legacy `SkillProjectile`
      path. 2 slots matches the GDD's starting Savoir Magique count
      (2→3→4, not yet wired to progression — this is just a fixed-size array
      for now). `ComputeCastGeometry` picks origin per base form: Projectile
      spawns at the caster and fires toward the aim point; Zone/Impact are
      centered ON the (range/radius-clamped) aim point; Aura originates at
      the caster.
      **Key-binding decision (user choice, 2026-07-30)**: keys 1-2 now route
      to `SpellCaster` (slotIndex 0-1), keys 3-4 stay on the legacy
      `SkillCaster` (slotIndex 2-3, unchanged Ignis SkillData skills) — no
      `GameInput.cs` change needed, `PlayerCombat.TryCastSlot` does the
      routing internally by index. This is a coexistence measure, not the
      final mapping — once `SkillCaster`/`SkillData` are archived (Migration
      Status table, "EXISTING DATA ASSETS"), keys 3-4 should be freed up for
      slots 3-4 of the Savoir Magique progression instead.
      2 new example `SpellRecipe` assets under `Data/SpellCraft/Recipes/`:
      `Ignis_Projectile_Base` (already existed) on key 1, new
      `Aqua_Zone_Frost` (Zone + Aqua + Persist rune) on key 2. `SpellCaster`
      component added to the Player GameObject in `MovementGym.unity`
      (`_enemyLayer` = 128, matching `SkillCaster`'s existing value — layer
      7 "Enemy"), `PlayerCombat._spellCaster` wired to it explicitly.
      Verified via Unity MCP Play Mode calling the real
      `PlayerCombat.TryCastSlot` entry point (not a synthetic
      `SpellFactory.CreateSpell` call like earlier form/rune tests — this
      one exercises the actual player-facing path end to end): key 1 spent
      7 mana (100→93), started a 1s cooldown, spawned 1 `ProjectileSpell`;
      key 2 spent 12.6 mana (9 base × 1.4 Persist multiplier, 93→80.4),
      started a 2.2s cooldown (2 × 1.1), spawned 1 `ZoneSpell`; key 3
      (legacy, unchanged) still spent 30 mana on `Ignis_MurDeFeu` — confirms
      no regression from the `PlayerCombat` routing change. Zero console
      errors/warnings. **Not yet a real keyboard test** — same standing
      limitation as Phase 5 (this MCP environment doesn't tick Play Mode
      frames between tool calls), so continuous aiming/movement feel during
      cast is still unverified; the user can now test this live in-editor
      since real key bindings exist.
- [x] Refactor SkillProjectile → ProjectileSpell with modifier support —
      **done (2026-07-30)**. Port was already done (see "4 base forms
      functional" above); "with modifier support" is now also true: Bounce
      and Split (see "4 basic modifier runes" above) both modify
      `ProjectileSpell`'s behavior via `SpellContext` accumulators. Still no
      generic `ISpellModifier.OnHit` event hook — that pattern turned out
      unnecessary for these two runes, handled directly in
      `ProjectileSpell.Update`/`Init` instead — so a future rune needing a
      genuinely different per-hit hook (not expressible as a typed
      accumulator) may still require adding one then.

### Phase 7 — Village Hub + First Library
- [ ] Havrevent village layout, 4-5 NPCs with dialogue
- [ ] NPCDialogue system
- [ ] 3 tutorial village quests (teach basic runes)
- [ ] Emerald Forest zone — first explorable area
- [ ] Library of Dawn — 4 rooms, puzzles, Guardian boss
- [ ] VillageEvolution — first visible change
- [ ] SaveManager — save/load

### Phase 8 — Spell Crafting Complete
- [ ] All 7 schools with VFX
- [ ] All 16 modifier runes (4 categories)
- [ ] Environmental synergies (10 combinations)
- [ ] Full Grimoire (craft, spellbook, rune encyclopedia, synergy log, journal)
- [ ] 3 spell slots
- [ ] Rune incompatibilities
- [ ] ElementalWeakness on all enemies
- [ ] SavoirSystem with thresholds

### Phase 9 — Expanded World
- [ ] 3 additional zones (Twin Lakes, Ash Peaks, Zephyr Plains)
- [ ] 3 additional Libraries with Guardians
- [ ] Zone unlocking via spells
- [ ] Zone NPCs, secondary quests
- [ ] School specialization (choose 2 schools)
- [ ] 4 spell slots

### Phase 10 — Full Content
- [ ] All 7 zones + village
- [ ] All 7 Libraries with Guardians
- [ ] Complete narrative (4 acts)
- [ ] Full village evolution
- [ ] All synergies in Grimoire

### Phase 11 — Polish & Launch
- [ ] Final cel-shader + VFX pass
- [ ] Music + sound design
- [ ] Balancing pass
- [ ] Tutorial + onboarding
- [ ] Accessibility (remapping, subtitles, difficulty)
- [ ] Launch build

## Open Questions
1. **World name** — "Havrevent" is placeholder for village. World unnamed (old "Cairn" retired).
2. **Companion** — Magical familiar? Adds personality + guide role but scope.
3. **Equipment crafting** — Robes/staves modifying school affinity? Depth vs complexity.
4. **Day/night cycle** — Atmosphere boost, Lux/Umbra power changes. Extra scope.
5. **Summon form** — 5th base form for temporary creatures (Magic and Mayhem inspiration).

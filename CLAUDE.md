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
| HeroCharacter models | Models/ | ⚠️ SUPERSEDED (2026-07-31) — Player now uses the 3rdParty/ATART Wizard model instead (`Wizard_Lit.prefab`, 4 new ToonCel materials since stock ones render pink in URP; full swap details in the `project-history` skill). HeroCharacter_Rigged.glb kept in the project but no longer wired to Player; still a candidate if the team wants a from-scratch (non-3rdParty) hero later. |
| Animations | Animations/ | HeroAnimator.controller kept in the project but unused by Player — replaced by `Wizard.controller` (Speed blend tree + Cast/Dodge trigger states, see `project-history` skill) — never was wired to Player either, this table entry described the Phase 5/6 plan, not something completed. |
| VFX/Ignis/ prefabs | Prefabs/VFX/ | Keep particle systems. Restyle for toon look (brighter, more stylized). |

Wizard model swap + cast/dodge animation wiring (2026-07-31): full implementation
notes and Play Mode verification numbers are in the `project-history` skill —
load it if you need to touch `Wizard.controller`, `PlayerAnimator.cs`, or the
ToonCel Wizard materials.

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

## Completed Work (v3.1 phases — superseded by Migration Status above)

Phases 1-4 (core movement, common skills/combat, enemy telegraph+posture, first
school Ignis) shipped in v3.1 and were subsequently archived/refactored per the
Migration Status table above. Full feature lists and v3.1 tuning numbers: see the
`project-history` skill.

---

## New Roadmap (v4.0)

### Phase 5 — Core Pivot (camera + movement + cleanup) ✅ COMPLETE (2026-07-30)
- [x] Cinemachine ThirdPersonCamera — top-down, fixed angle, 100% game-controlled
- [x] PlayerController `FacingMode` (Movement/Aim), no camera-rotation coupling
- [x] GameInput refactor (spell slots 1-4, Grimoire/Interact events)
- [x] ManaSystem (replaces StaminaSystem for casting)
- [x] PlayerCombat (spell slot casting + FacingMode.Aim bridge)
- [x] Archived removed v3.1 scripts to `_Archive/` — StaminaSystem intentionally
      kept active (still consumed by DodgeRoll/SprintController)
- [x] Simplified DodgeRoll (i-frame 0.3s → 0.2s) and EnemyAI (posture checks removed)
- [x] Refactored PlayerHUD (HP + Mana + 4 spell slots)
- [x] Cel-shader (`Shaders/ToonCel.shader`, hand-written HLSL) — not yet applied
      to imported Skeleton_Warrior/Synty models (Phase 11 art pass)
- [x] Verified via Unity MCP Play Mode — not yet a live human playtest (standing
      MCP-tooling limitation: frames don't tick between tool calls)

Full implementation notes, design-decision rationale, and verification numbers
for each item: see the `project-history` skill.

### Phase 6 — Spell Crafting Core ✅ COMPLETE (2026-07-30, corrections 2026-08-05/06)
- [x] SpellRecipe + RuneModifier ScriptableObjects (`SpellCraft/Data/`)
- [x] SpellFactory + ModifierProcessor + ISpellModifier (`SpellCraft/Runtime/`)
- [x] 4 base forms functional: ProjectileSpell, ZoneSpell, AuraSpell (implements
      an absorbing shield, not a generic buff — `HealthSystem.ShieldAmount`),
      ImpactSpell
- [x] 3 schools playable: Ignis→Burn (DoT), Aqua→Slow (NavMeshAgent speed; no
      effect on the player, which has no external speed hook — enemies-only gap),
      Terra→bonus damage+Knockback. Ventus/Lux/Umbra/Ferrum are Phase 8.
- [x] 4 basic modifier runes: Bounce, Split, Persist, Expand — driven by typed
      `SpellContext` accumulators, not a generic event bus
- [x] Grimoire UI — draggable node-graph crafting panel (`Grimoire/`:
      CraftingNode, CraftingPanel, GrimoireUI), craft-only (Spellbook/
      Encyclopedia/Journal panels are Phase 8)
- [x] Terrain effects — `EnvironmentState` static registry marks Fire/Water/
      Wind/Shadow/LooseEarth patches on Zone cast (ground-marking only, no
      synergy reactions yet — that's Phase 8)
- [x] 2 active spell slots — keys 1-2 route to the new `SpellCaster`, keys 3-4
      stay on the legacy `SkillCaster` as a coexistence measure until
      SkillData is archived
- [x] SkillProjectile → ProjectileSpell refactor, with Bounce/Split modifier support

**Post-Phase-6 correction (2026-08-05)**: continuous rune tuning — each equipped
rune now carries an `intensity` (0.25–2.0, default 1.0) via `RuneSlot`, dialed
with a Grimoire slider; `EffectiveManaCostMultiplier`/`EffectiveCooldownMultiplier`
scale with it. **Bounce/Split round their flat counts (`count × intensity`) and
can round down to 0 at low intensity — intended, not a bug.** Terrain patches now
stack (`EnvironmentState`, capped at intensity 3) and a Zone cast onto its own
already-marked terrain deals +50% damage per stack beyond the first.

**Game feel / juice pass (2026-08-05)**: `Core/HitStop.cs` (`HitStop.Trigger`)
freezes `Time.timeScale` briefly on impact. **Applies only to ProjectileSpell and
ImpactSpell (one discrete hit/burst) — deliberately NOT ZoneSpell, whose ticks
repeat every `tickInterval` and would read as a near-permanent freeze.** Grimoire
node drag/connect and open/close now tween instead of snapping instantly.

**Test props (2026-08-05)**: 6 passive, high-HP (500, non-despawning) target
dummies added to `MovementGym.unity` for spell testing — 3 at 4/10/18m range, a
3-dummy AoE cluster.

**4 bug fixes (2026-08-05/06)** — root causes worth knowing before touching the
related code:
- **Camera drift**: `ThirdPersonCamera` must write `transform.rotation` directly
  in `LateUpdate` to the fixed `Quaternion.Euler(pitchAngle, yawAngle, 0)`. Do
  NOT add a `CinemachineRotationComposer` — it continuously re-aims to keep its
  LookAt target centered, which drifted the camera ~10° yaw / ~3° pitch off the
  fixed angle and desynced mouse-raycast spell aim from what was on screen.
- **Projectile spawn height**: cast origin uses `_castHeightOffset`
  (`SpellCaster`/`SkillCaster`, default 0.3, `Vector3.up * _castHeightOffset`) —
  never hardcode `Vector3.up`. `Player`'s `CharacterController.center = (0,0,0)`
  means `transform.position` is already chest-height, not feet-level, so a full
  extra unit sent every flat-trajectory projectile above every target's head.
- **Aura shield visual**: `AuraSpell` must `Destroy()` both its tracking
  GameObject AND the `_visual` shield sphere (parented separately, under the
  caster, so it visually follows the player) — destroying only one leaves an
  orphaned bubble.
- **Grimoire rune click-through**: `CraftingPanel`'s feedback label must have
  `raycastTarget = false` — being built last in `BuildUI()`, it was rendering
  (and intercepting clicks) on top of the rune nodes/sliders beneath it.

Full implementation notes, design rationale, and Play Mode verification numbers
for every item above: see the `project-history` skill.

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

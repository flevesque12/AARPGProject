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
- Universal Render Pipeline (URP) + custom cel-shader (to build)
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
| HealthSystem.cs | Core/ | No changes needed. Player + enemies. |
| HitFeedback.cs | Core/ | Keep white flash + scale punch. Remove block-specific colors (blue/gold/red). |
| DodgeRoll.cs | Player/ | ✅ DONE (2026-07-27). i-frame 0.3s → 0.2s. The "riposte window trigger" was removed from RiposteSystem's side (it listened to `DodgeRoll.OnDodgeEnd`), not from DodgeRoll itself. |
| SprintController.cs | Player/ | Keep as-is. Possibly make sprint free (no stamina cost). |
| DamageNumber.cs | UI/ | Keep — fully code-created via DamageNumber.Spawn(), no prefab dependency. |
| EnemySpawner.cs | Enemy/ | Keep as-is. |
| HeroCharacter models | Models/ | Keep 3D models (HeroCharacter_Rigged.glb). Restyle with cel-shader. |
| Animations | Animations/ | Keep Idle/Walk. Attack anim repurposed for casting gesture. |
| VFX/Ignis/ prefabs | Prefabs/VFX/ | Keep particle systems. Restyle for toon look (brighter, more stylized). |

### 🔄 REFACTORED (core logic preserved, interface changed)
| Script | What changes |
|--------|-------------|
| PlayerController.cs | ✅ DONE (2026-07-27). Removed iso mouse-aim-drives-rotation. Added `FacingMode` enum (Movement/Aim) — `Movement` faces move direction, `Aim` faces mouse raycast on ground (`AimWorldPosition`, switched by PlayerCombat during cast). Kept: camera-relative WASD math, CharacterController.Move(), public API (LockMovement, MoveByDelta, Teleport, etc.) |
| GameInput.cs | ✅ DONE (2026-07-27). Removed: block input (RMB/LT) — BlockSystem no longer receives input. Added: spell slot 1-4 → `PlayerCombat.TryCastSlot`, `OnGrimoireTogglePressed` (Tab) and `OnInteractPressed` (E) events (no consumer yet — GrimoireUI/InteractionController are Phase 6/7). Kept: WASD, dodge (Space), sprint (Shift), gamepad detection. Riposte: never had its own detection here (was internal to CombatController), nothing to remove. |
| EnemyAI.cs | ✅ PARTIAL (2026-07-27). Removed: PostureSystem.IsStaggered checks, posture-aware attack cancels, HandleStaggerEnter/Exit. Kept: FSM (Idle/Chase/Attack/Dead), NavMeshAgent, detection/aggro ranges. **Not done yet**: ElementalWeakness component reference, hurt state — deliberately deferred, these belong to Phase 8 ("ElementalWeakness on all enemies"), not Phase 5. |
| PlayerHUD.cs | ✅ DONE (2026-07-27). Removed: stamina bar, riposte "RIPOSTE!" indicator, action bar with keybinds (whole thing, not just block/riposte rows). Added: Mana bar (below HP, single color, no tiered/flash animation like old stamina), 4 spell slot icons with cooldown overlay + school color (unchanged from v3.1). Kept: HP bar (top-left), auto-create Canvas. |
| SkillProjectile.cs | Not started — Phase 6 (SpellRecipe system). Rename → ProjectileSpell.cs. Refactor to read from SpellRecipe instead of SkillData. Add ISpellModifier hooks for bounce/split/homing. Keep: physics movement, collision detection, VFX instantiation. |

### ❌ REMOVED (archive in _Archive/ folder, do not use)
| Script | Reason |
|--------|--------|
| CombatController.cs | Replaced by PlayerCombat.cs (spell slots, no melee combo). **Not archived yet** — still active in parallel (dodge/block/riposte/sprint/basic attack), PlayerCombat only owns spell casting so far. Retiring this fully is the last big item of Phase 5. |
| BlockSystem.cs | No blocking mechanic in v4.0. Input already cut (GameInput no longer calls OnBlockInput) but component/script not archived yet — inert, not deleted. |
| RiposteSystem.cs | No riposte mechanic in v4.0. Dodge no longer opens its window (2026-07-27), but Bloc Parfait still can — not archived yet. |
| PostureSystem.cs | No posture/stagger in v4.0. EnemyAI no longer reacts to it (2026-07-27) but the component itself is still active (SkillCaster/RiposteSystem still read it) — not archived yet. |
| StaminaSystem.cs | Replaced by ManaSystem for casting; sprint may be free. Still active in parallel for dodge/sprint/block until those are migrated too. |
| PostureBarUI.cs | No posture bar in v4.0. Not archived yet. |
| AimIndicator.cs | ✅ ARCHIVED (2026-07-27) → `Scripts/_Archive/AimIndicator.cs.txt` (renamed `.txt`, Unity compiles `.cs` regardless of folder name). |
| CameraController.cs | ✅ ARCHIVED (2026-07-27) → `Scripts/_Archive/CameraController.cs.txt`. Component removed from Main Camera in MovementGym.unity. |
| WorldHealthBar.cs | Simplify into new version without posture bar pairing. Not started. |
| EnemyTelegraph.cs | Already a simple generic visual indicator (Circle/Cone/FullBoss), never referenced PostureSystem — no change needed here despite the note. |
| StaggerVFX.cs | No stagger in v4.0. Not archived yet. |
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
- [ ] Archive removed scripts to _Archive/ folder — **partial**: only
      CameraController.cs + AimIndicator.cs done. BlockSystem/RiposteSystem/
      PostureSystem/PostureBarUI/StaminaSystem/StaggerVFX/CombatController
      still active in parallel (inputs cut where applicable, but components
      not archived — this is the last big item, since removing
      CombatController also means gutting/rewiring CombatVisualFeedback.cs)
- [x] Simplify DodgeRoll (i-frame 0.3s → 0.2s; riposte-window-on-dodge-end
      removed — that trigger actually lived in RiposteSystem's own
      subscription to `DodgeRoll.OnDodgeEnd`, not in DodgeRoll itself)
- [x] Simplify EnemyAI (remove posture checks) — ElementalWeakness/hurt state
      intentionally deferred to Phase 8, not added here
- [x] Refactor PlayerHUD (HP + Mana + 4 spell slots)
- [ ] Basic cel-shader setup (URP shader graph)
- [ ] Test: player can move in 3P, dodge, cast 1 basic spell, kill 1 enemy —
      core logic verified via scripted checks (mana consumption, cooldowns,
      facing mode, damage) in Play Mode, but **no live human playtest yet**.
      This MCP environment's Play Mode does not tick frames between tool
      calls, so continuous input (camera damping feel, movement smoothness)
      could not be verified this way — needs a real keyboard/mouse session.

### Phase 6 — Spell Crafting Core
- [ ] SpellRecipe + RuneModifier ScriptableObjects
- [ ] SpellFactory + ModifierProcessor + ISpellModifier
- [ ] 4 base forms functional (Projectile, Zone, Aura, Impact)
- [ ] 3 schools playable (Ignis, Aqua, Terra)
- [ ] 4 basic modifier runes (Bounce, Split, Persist, Expand)
- [ ] Grimoire UI — basic crafting panel (node-graph)
- [ ] Terrain effects (fire on ground, water puddle)
- [ ] 2 active spell slots
- [ ] Refactor SkillProjectile → ProjectileSpell with modifier support

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

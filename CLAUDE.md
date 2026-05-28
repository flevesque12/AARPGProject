# ARPG Classless — Projet Unity

## Description
Action-RPG isométrique en low poly 3D, inspiré de Diablo 2 et Torchlight.
Système classless où le joueur choisit librement ses compétences dans
des écoles ouvertes.

## Architecture
- Unity 6000.4.8f1
- Universal Render Pipeline (URP)
- New Input System + Legacy (mode Both)
- NavMesh pour le pathfinding

## Structure des dossiers
- Assets/_MainProject/Scripts/Core/ — GameInput, HealthSystem, HitFeedback
- Assets/_MainProject/Scripts/Player/ — PlayerController, PlayerCombat
- Assets/_MainProject/Scripts/Enemy/ — EnemyAI, EnemySpawner
- Assets/_MainProject/Scripts/Camera/ — CameraController
- Assets/_MainProject/Scripts/UI/ — WorldHealthBar, PlayerHUD, DamageNumber
- Assets/_MainProject/Scripts/Skills/ — (à venir) SkillData, SkillCaster
- Assets/_MainProject/Data/Skills/ — (à venir) ScriptableObject assets
- Assets/_MainProject/Prefabs/ — Prefabs joueur, ennemis, effets
- Assets/_MainProject/Models/ — HeroCharacter.glb (base), HeroCharacter_Rigged.glb (rigué)
- Assets/_MainProject/Models/Animations/ — Hero_Idle.glb, Hero_Walk.glb, Hero_Attack.glb
- Assets/_MainProject/Animations/ — HeroAnimator.controller

## Conventions de code
- Nommage : PascalCase pour les classes et méthodes, camelCase pour les variables
- Commentaires en français
- SerializeField pour tout ce qui doit être visible dans l'Inspector
- Événements C# (event Action) pour la communication entre systèmes

## Notes techniques importantes

### Personnage joueur — Setup scène
- `Player` : NavMeshAgent (`baseOffset = 1.0`), PlayerController, PlayerCombat, HealthSystem
- `Player/HeroModel` : modèle Meshy riggué (`HeroCharacter_Rigged.glb`), Animator, SkinnedMeshRenderer
  - `localPosition.y = -1.0` (compense le baseOffset du NavMeshAgent pour poser les pieds au sol)
  - Ne pas remettre à zéro sans recalculer via `FixHeroModelHeight.cs`
- Modèles dans `Assets/_MainProject/Models/` : HeroCharacter.glb (base), HeroCharacter_Rigged.glb (rigué)
- Animations dans `Assets/_MainProject/Models/Animations/` : Hero_Idle.glb, Hero_Walk.glb, Hero_Attack.glb

### Personnage joueur — Animations
- `PlayerController` pilote le paramètre `Speed` (Float) avec un `currentSpeed` local :
  - WASD : `currentSpeed = moveInput.magnitude * moveSpeed` (pas `agent.velocity` qui vaut 0 avec `agent.Move()`)
  - Click-to-move : `currentSpeed = agent.velocity.magnitude`
  - Idle : `currentSpeed = 0f`
- `PlayerCombat` déclenche le trigger `Attack` au début de `PerformAttack()`
- Les deux scripts récupèrent l'`Animator` via `GetComponentInChildren<Animator>()`
- Rotation : `Quaternion.RotateTowards` à 540°/s (Inspector), pas `Slerp`
- `HeroAnimator.controller` : états Idle (défaut) / Walk / Attack
  - Idle ↔ Walk : conditionné par Speed > 0.1 / < 0.1, transition 0.15s
  - AnyState → Attack : trigger Attack, pas d'exit time, transition 0.05s
  - Attack → Idle : exit time à 85% de l'animation, transition 0.15s

### Combat — Game Feel
- `PlayerCombat` utilise une séquence coroutine en 3 phases : anticipation (squash) → release (stretch + hit detection) → recovery
- Le **hit stop** (`Time.timeScale = 0.05f` pendant 60ms) se déclenche uniquement sur un hit réussi via `WaitForSecondsRealtime`
- La phase recovery utilise `Time.unscaledDeltaTime` pour s'exécuter normalement pendant le freeze
- `OnDisable` de `PlayerCombat` remet toujours `Time.timeScale = 1f` en sécurité
- `HitFeedback` utilise aussi `Time.unscaledDeltaTime` pour le flash lerp (compatible hit stop)
- `DamageNumber` est entièrement créé par code (TextMesh), pas de prefab requis — appelé via `DamageNumber.Spawn()`

## Phase actuelle
Phase 1 : Core loop complète — mouvement, combat, IA ennemis, game feel (hit stop, squash/stretch, chiffres de dégâts)

## Prochaines étapes
Phase 2 : Système de skills (ScriptableObjects + SkillCaster + UI)
Phase 3 : Donjon jouable
Phase 4 : Skill tree minimal (2 écoles)
Phase 5 : Équipement de base
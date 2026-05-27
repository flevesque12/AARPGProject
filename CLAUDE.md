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

## Conventions de code
- Nommage : PascalCase pour les classes et méthodes, camelCase pour les variables
- Commentaires en français
- SerializeField pour tout ce qui doit être visible dans l'Inspector
- Événements C# (event Action) pour la communication entre systèmes

## Notes techniques importantes

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
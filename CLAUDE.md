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
- Assets/_MainProject/Scripts/Editor/ — FixHeroModelHeight (outil Editor uniquement)
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

### Caméra — Style Path of Exile 2
- `CameraController` posé sur un GameObject vide `CameraRig` ; la Main Camera est enfant avec transform resetée à zéro
- **Perspective** (pas orthographic) avec FOV 38° par défaut
- `pitchAngle = 60°` (inclinaison vers le sol, ajuster entre 55–65 pour varier)
- `yawAngle = 0°` (vue droite face au nord, pas diagonale)
- Le zoom ajuste `distance` (pas `orthographicSize`) — plage 18–48 par défaut
- La position est calculée par code (`rotation * Vector3.back * distance`) : ne pas poser d'offset manuel sur la Main Camera
- Paramètres Inspector : `pitchAngle`, `yawAngle`, `distance`, `minDistance`, `maxDistance`, `fieldOfView`, `followSpeed`

### Caméra — Cohérence avec l'input joueur
- `PlayerController` contient `isoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0))` **hardcodé**
- Cette valeur doit correspondre au `yawAngle` de la caméra pour que WASD soit aligné avec l'écran
- Avec `yawAngle = 0°`, l'isoMatrix devrait être `Euler(0, 0, 0)` — sinon W déplace en diagonale plutôt que vers le haut de l'écran
- Si on change `yawAngle`, mettre à jour `isoMatrix` en conséquence

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

### Combat — Autres features joueur
- **Auto-aim manette** : `PlayerCombat` snap automatiquement vers l'ennemi le plus proche (`autoAimRange`) quand `IsUsingGamepad` est vrai
- **LookAtMouse** : en souris, le joueur se tourne vers le curseur avant chaque attaque
- **Knockback** : sur hit réussi, l'ennemi est projeté via `NavMesh.SamplePosition` + `agent.Warp` (ou `Rigidbody.AddForce` si pas de NavMeshAgent)
- **Détection en cône** : `attackAngle = 90°`, `attackRange = 2.5f` par défaut

### IA ennemie — EnemyAI
- State machine : `Idle → Chase → Attack → Dead`
- **Patrol optionnel** (`enablePatrol`) : se déplace vers des points aléatoires dans `patrolRadius` depuis le spawn
- **Aggro** : détection à `detectionRange`, perte d'aggro à `loseAggroRange`, aggro immédiat si touché en Idle
- **Windup telegraphé** : scale-up temporaire (`x1.1`) pendant `attackWindup` avant que le dégât soit appliqué
- **Loot drop** : `lootDropPrefabs[]` + `dropChance` (0–1), drop aléatoire à la mort

## Phase actuelle
Phase 1 : Core loop complète — mouvement (WASD + click-to-move), combat (cône + knockback + auto-aim), IA ennemis (state machine + patrol + loot), caméra style PoE2, game feel (hit stop, squash/stretch, chiffres de dégâts)

## Prochaines étapes
Phase 2 : Système de skills (ScriptableObjects + SkillCaster + UI)
Phase 3 : Donjon jouable
Phase 4 : Skill tree minimal (2 écoles)
Phase 5 : Équipement de base

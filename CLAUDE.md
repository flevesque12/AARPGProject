# ARPG Classless — Projet Unity

## Description
Action-RPG isométrique en low poly 3D, inspiré de Diablo 2 et Torchlight.
Système classless où le joueur choisit librement ses compétences dans
des écoles ouvertes.

## Architecture
- Unity 6000.4.8f1
- Universal Render Pipeline (URP)
- New Input System + Legacy (mode Both)
- NavMesh pour les ennemis (EnemyAI) ; joueur sur **CharacterController**

## Structure des dossiers
- Assets/_MainProject/Scripts/Core/ — GameInput, HealthSystem, HitFeedback, StaminaSystem
- Assets/_MainProject/Scripts/Player/ — PlayerController, CombatController, DodgeRoll, BlockSystem, RiposteSystem, SprintController, AimIndicator
- Assets/_MainProject/Scripts/Enemy/ — EnemyAI, EnemySpawner
- Assets/_MainProject/Scripts/Camera/ — CameraController
- Assets/_MainProject/Scripts/UI/ — WorldHealthBar, PlayerHUD, DamageNumber, StaminaBarUI
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
- `PlayerController` calcule la direction de mouvement **relativement à la caméra** via `camera.transform.forward/right` projetés sur le plan horizontal
- Plus d'`isoMatrix` hardcodé : la direction est toujours alignée avec l'écran quelle que soit la `yawAngle` de la caméra
- Si on change `yawAngle`, le mouvement s'adapte automatiquement

### Personnage joueur — Setup scène
- `Player` : **CharacterController**, PlayerController, CombatController, DodgeRoll, BlockSystem, RiposteSystem, SprintController, StaminaSystem, HealthSystem
- `Player/HeroModel` : modèle Meshy riggué (`HeroCharacter_Rigged.glb`), Animator, SkinnedMeshRenderer
- Modèles dans `Assets/_MainProject/Models/` : HeroCharacter.glb (base), HeroCharacter_Rigged.glb (rigué)
- Animations dans `Assets/_MainProject/Models/Animations/` : Hero_Idle.glb, Hero_Walk.glb, Hero_Attack.glb
- **Attention** : le `NavMeshAgent` a été retiré du joueur — utiliser `CharacterController.Move()` / `PlayerController.MoveByDelta()` pour tout déplacement externe (esquive, knockback, etc.)

### Personnage joueur — Mouvement (PlayerController)
- Utilise **CharacterController** (plus NavMeshAgent) : mouvement direct et réactif
- WASD / stick gauche → direction de mouvement
- Souris / stick droit → direction du regard (facing)
- Direction calculée relativement à la caméra (`camForward * moveInput.y + camRight * moveInput.x`)
- Accélération/décélération configurables (`acceleration = 50f`, `deceleration = 40f`)
- `speedMultiplier` modifiable par `SprintController`, buffs, debuffs
- API publique : `LockMovement(bool)`, `LockRotation(bool)`, `SetSpeedMultiplier(float)`, `MoveByDelta(Vector3)`, `Teleport(Vector3)`, `ForceFacing(Vector3)`
- Rotation : `Quaternion.Slerp` vers la souris à `rotationSpeed = 20f`, ou snap instantané si `instantRotation = true`
- Événements : `OnMove(direction, speed)`, `OnStopMoving()`, `OnFacingChanged(direction)`

### Personnage joueur — Animations
- Les animations sont pilotées depuis `CombatController` / `PlayerController` via l'`Animator` (GetComponentInChildren)
- `HeroAnimator.controller` : états Idle (défaut) / Walk / Attack
  - Idle ↔ Walk : conditionné par Speed > 0.1 / < 0.1, transition 0.15s
  - AnyState → Attack : trigger Attack, pas d'exit time, transition 0.05s
  - Attack → Idle : exit time à 85% de l'animation, transition 0.15s

### Input — GameInput
- Gère la détection clavier/souris vs manette automatiquement (`lastGamepadInput` vs `lastKeyboardInput`)
- Délègue les inputs à `PlayerController` (mouvement/visée) et `CombatController` (actions)
- Layout clavier : WASD mouvement, Souris facing, Clic gauche attaque, **Espace esquive**, **Shift gauche bloc**, **Ctrl gauche sprint**
- Layout manette : Stick G mouvement, Stick D visée, X/Square attaque, A/Cross esquive, LT bloc, LB sprint

### Stamina — StaminaSystem
- Ressource d'endurance partagée entre toutes les actions du Socle Commun
- Régénération automatique avec délai (`regenDelay = 1.5s`) après consommation
- Regen ×3 hors combat (`outOfCombatRegenMultiplier`), ralentie via `SetInCombat()`
- Consommateurs : `DodgeRoll` (25), `BlockSystem` (15/coup bloqué), `SprintController` (10/sec)
- API : `ConsumeStamina(amount)→bool`, `DrainStamina(amountPerSec)→bool`, `RestoreStamina(amount)`, `FillStamina()`, `SetInCombat()`
- Événements : `OnStaminaChanged(current, max)`, `OnStaminaEmpty()`, `OnStaminaFull()`

### Combat — Socle Commun (CombatController)
- Orchestre les systèmes de combat avec une **priorité stricte** :
  1. **Esquive** (DodgeRoll) — interrompt tout
  2. **Riposte** (RiposteSystem) — si fenêtre ouverte + input attaque
  3. **Bloc** (BlockSystem) — maintenu
  4. **Attaque de base** — combo 3 coups (+20% au 3e coup)
  5. **Sprint** (SprintController) — quand rien d'autre
- Attaque de base : détection en cône (`attackAngle = 80°`, `attackRange = 2.5f`), cooldown 0.5s, fenêtre combo 0.8s
- API d'input : `OnDodgeInput(moveDir)`, `OnBlockInput(bool)`, `OnAttackInput()`, `OnSprintInput(bool)`
- `FilterIncomingDamage(rawDamage, attackerPos)` : à appeler depuis HealthSystem avant d'appliquer les dégâts (gère i-frames + bloc)

### Combat — DodgeRoll
- Esquive directionnelle dans la direction WASD/stick, ou vers l'arrière si aucune direction
- **I-frames** : invulnérabilité de `iFrameStart` (0.05s) à `iFrameStart + iFrameDuration` (0.3s)
- Déplacement via `PlayerController.MoveByDelta()` avec `AnimationCurve` (EaseInOut)
- Verrouille mouvement et rotation du `PlayerController` pendant l'esquive
- Coût stamina : 25 — cooldown : 1.2s
- Événements : `OnDodgeStart()`, `OnDodgeEnd()`, `OnInvulnerabilityChanged(bool)`
- La fin d'esquive ouvre la fenêtre de riposte (`RiposteSystem`)

### Combat — BlockSystem
- **Bloc normal** : maintenir Shift/LT → 60% réduction de dégâts, angle de couverture 120°, coût 15 stamina/coup
- **Bloc Parfait** : timing de 0.25s après avoir APPUYÉ le bouton → 90% réduction, coût stamina ÷2, slowmo `timeScale = 0.15f` pendant 0.2s (real time)
- Bloc cassé si stamina insuffisante (dégâts complets)
- L'attaque venant de derrière n'est pas bloquée (vérification d'angle)
- Événements : `OnBlockStart()`, `OnBlockEnd()`, `OnPerfectBlock()`, `OnBlockHit(residualDamage)`, `OnBlockBroken()`

### Combat — RiposteSystem
- Fenêtre de riposte ouverte après : **Bloc Parfait** ou **fin d'esquive**
- Durée de la fenêtre : 1.0s — dégâts : ×2 les dégâts de base
- Détection en cône (`riposteRange = 3f`, `riposteAngle = 90°`)
- La riposte qui touche déclenche un bref slowmo (`timeScale = 0.1f`, 0.15s real time)
- Événements : `OnRiposteWindowOpen()`, `OnRiposteWindowClose()`, `OnRiposteHit(gameObject, damage)`

### Combat — SprintController
- Modificateur de vitesse ×1.6 via `PlayerController.SetSpeedMultiplier()`
- Consomme 10 stamina/sec, nécessite minimum 15 stamina pour démarrer
- Arrêt automatique si stamina épuisée ou si autre action prioritaire
- `ForceStopSprint()` appelé par `CombatController` sur esquive/bloc/attaque

### Combat — Game Feel
- **Hit stop** : `Time.timeScale = 0.05f` pendant 60ms sur hit réussi (dans CombatController/RiposteSystem) via `WaitForSecondsRealtime`
- **Bloc Parfait slowmo** : `Time.timeScale = 0.15f` pendant 0.2s real time (BlockSystem)
- **Riposte slowmo** : `Time.timeScale = 0.1f` pendant 0.15s real time sur touche
- `HitFeedback` utilise `Time.unscaledDeltaTime` pour le flash lerp (compatible avec tous les slowmo)
- `DamageNumber` : entièrement créé par code (TextMesh), pas de prefab requis — appelé via `DamageNumber.Spawn()`

### Visée — AimIndicator
- Affiche un réticule au sol à la position visée par la souris (lit `PlayerController.AimWorldPosition`)
- Couleur blanche semi-transparente par défaut, rouge quand la souris survole un ennemi
- Fonctionne sans prefab : crée un Quad projeté au sol si `cursorPrefab` est null
- Ligne de visée optionnelle (`showAimLine`) via `LineRenderer`
- `SetVisible(bool)` pour masquer pendant les menus/cutscenes

### UI — StaminaBarUI
- Barre d'endurance HUD, écoute les événements de `StaminaSystem`
- Couleur dynamique : vert (>50%) → jaune (25-50%) → rouge (<25%)
- Flash rouge quand stamina insuffisante pour une action
- Se masque automatiquement (`hideDelay = 3s`) quand l'endurance est pleine

### IA ennemie — EnemyAI
- State machine : `Idle → Chase → Attack → Dead`
- **Patrol optionnel** (`enablePatrol`) : se déplace vers des points aléatoires dans `patrolRadius` depuis le spawn
- **Aggro** : détection à `detectionRange`, perte d'aggro à `loseAggroRange`, aggro immédiat si touché en Idle
- **Windup telegraphé** : scale-up temporaire (`x1.1`) pendant `attackWindup` avant que le dégât soit appliqué
- **Loot drop** : `lootDropPrefabs[]` + `dropChance` (0–1), drop aléatoire à la mort

## Phase actuelle
Phase 1 : Core loop complète — mouvement WASD+souris (CharacterController), **Socle Commun** (esquive i-frames + bloc parfait + riposte + sprint + stamina), attaque combo 3 coups, IA ennemis (state machine + patrol + loot), caméra style PoE2, game feel (hit stop, slowmo, chiffres de dégâts, AimIndicator)

## Prochaines étapes
Phase 2 : Système de skills (ScriptableObjects + SkillCaster + UI)
Phase 3 : Donjon jouable
Phase 4 : Skill tree minimal (2 écoles)
Phase 5 : Équipement de base

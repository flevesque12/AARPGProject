using System;
using UnityEngine;

/// <summary>
/// Gestionnaire d'input centralisé — Lit les inputs et les distribue.
/// Supporte Legacy Input Manager ET New Input System (mode Both).
///
/// Note : la rotation de la caméra (souris / stick droit) est lue directement
/// par ThirdPersonCamera, pas par GameInput — le joueur fait face à sa direction
/// de mouvement (voir PlayerController.UpdateFacing).
///
/// v4.0 n'a pas de bloc ni d'attaque de base au corps-à-corps (voir CLAUDE.md,
/// table Combat) — esquive et sprint sont câblés directement sur DodgeRoll et
/// SprintController, sans CombatController comme intermédiaire (archivé,
/// Phase 5).
///
/// Layout clavier/souris :
///   WASD          → Mouvement
///   Souris        → Rotation caméra (géré par ThirdPersonCamera)
///   Clic gauche   → Libre (futur cast rapide / Impact form, Phase 6)
///   Clic droit    → Libre (futur skill secondaire)
///   Espace        → Esquive
///   Ctrl gauche   → Sprint (maintenir)
///   1-4           → Sorts (slots du Grimoire, via PlayerCombat)
///   Tab           → Ouvrir/fermer le Grimoire (OnGrimoireTogglePressed — futur GrimoireUI)
///   E             → Interagir (OnInteractPressed — futur InteractionController)
///
/// Layout manette :
///   Stick gauche  → Mouvement
///   Stick droit   → Rotation caméra (géré par ThirdPersonCamera)
///   A / Cross     → Esquive
///   LB            → Sprint (maintenir)
///   RT/RB         → Skills (futur)
/// </summary>
public class GameInput : MonoBehaviour
{
    [Header("Références — Glisser depuis l'Inspector")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private DodgeRoll dodgeRoll;
    [SerializeField] private SprintController sprintController;
    [SerializeField] private PlayerCombat playerCombat;

    [Header("Sensibilité")]
    [SerializeField] private float gamepadDeadzone = 0.15f;
    [SerializeField] private float gamepadAimDeadzone = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool logInputs = false;

    // Détection automatique clavier vs manette
    private bool isUsingGamepad;
    private float lastGamepadInput;
    private float lastKeyboardInput;

    // === Événements — pas encore de consommateur (GrimoireUI/InteractionController
    // arrivent en Phase 6/7), mais le contrat d'input est posé dès maintenant. ===
    public event Action OnGrimoireTogglePressed;
    public event Action OnInteractPressed;

    private void Update()
    {
        DetectInputDevice();

        HandleMovementInput();
        HandleCombatInput();
        HandleSpellInput();
        HandleUIInput();
    }

    // ========================================
    // DÉTECTION DU PÉRIPHÉRIQUE
    // ========================================

    private void DetectInputDevice()
    {
        // Détecter si le joueur utilise le gamepad ou le clavier
        // basé sur le dernier input significatif
        float gpMagnitude = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).magnitude;

        // Vérifier les axes de gamepad spécifiques
        float gpRightStick = 0f;
        try
        {
            gpRightStick = new Vector2(
                Input.GetAxisRaw("RightStickHorizontal"),
                Input.GetAxisRaw("RightStickVertical")
            ).magnitude;
        }
        catch { /* L'axe n'existe pas dans l'Input Manager, on ignore */ }

        float gpTrigger = 0f;
        try
        {
            gpTrigger = Mathf.Abs(Input.GetAxisRaw("LeftTrigger"));
        }
        catch { /* L'axe n'existe pas */ }

        if (gpRightStick > gamepadAimDeadzone || gpTrigger > 0.1f)
            lastGamepadInput = Time.unscaledTime;

        // Détecter clavier/souris
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) ||
            Mathf.Abs(Input.GetAxisRaw("Mouse X")) > 0.1f ||
            Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.1f)
        {
            // Filtrer les touches qui sont aussi mappées aux axes gamepad
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
                Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) ||
                Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift) ||
                Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) ||
                Mathf.Abs(Input.GetAxisRaw("Mouse X")) > 0.1f)
            {
                lastKeyboardInput = Time.unscaledTime;
            }
        }

        isUsingGamepad = lastGamepadInput > lastKeyboardInput;
    }

    // ========================================
    // MOUVEMENT (chaque frame)
    // ========================================

    private void HandleMovementInput()
    {
        if (playerController == null) return;

        Vector2 move;

        if (isUsingGamepad)
        {
            // Stick gauche
            move = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );

            // Appliquer la deadzone
            if (move.magnitude < gamepadDeadzone)
                move = Vector2.zero;
        }
        else
        {
            // WASD — input digital (pas de smoothing, réponse instantanée)
            move = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1f;
        }

        playerController.SetMoveInput(move);
    }

    // ========================================
    // COMBAT (esquive + sprint — v4.0 n'a pas d'attaque de base)
    // ========================================

    private void HandleCombatInput()
    {
        // === ESQUIVE ===
        bool dodgePressed = Input.GetKeyDown(KeyCode.Space);

        // Gamepad : bouton A / Cross
        try { dodgePressed |= Input.GetButtonDown("Jump"); } catch { }

        if (dodgePressed && dodgeRoll != null && dodgeRoll.CanDodge)
        {
            // Direction = là où le joueur pousse le stick/WASD
            Vector2 moveDir = Vector2.zero;
            if (isUsingGamepad)
            {
                moveDir = new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")
                );
            }
            else
            {
                if (Input.GetKey(KeyCode.W)) moveDir.y += 1f;
                if (Input.GetKey(KeyCode.S)) moveDir.y -= 1f;
                if (Input.GetKey(KeyCode.D)) moveDir.x += 1f;
                if (Input.GetKey(KeyCode.A)) moveDir.x -= 1f;
            }

            sprintController?.ForceStopSprint();

            Vector3 dodgeDir = DodgeRoll.GetDodgeDirection(moveDir, Camera.main);
            dodgeRoll.TryDodge(dodgeDir);

            if (logInputs) Debug.Log($"[Input] Esquive direction: {moveDir}");
        }

        // === SPRINT ===
        if (sprintController == null) return;

        bool sprintHeld;
        if (isUsingGamepad)
        {
            // LB (Left Bumper)
            bool lb = false;
            try { lb = Input.GetButton("LeftBumper"); } catch { }
            sprintHeld = lb;
        }
        else
        {
            sprintHeld = Input.GetKey(KeyCode.LeftShift);
        }

        sprintController.SetSprintInput(sprintHeld);
    }

    // ========================================
    // SORTS (touches 1-4) — indépendant de CombatController
    // ========================================

    private void HandleSpellInput()
    {
        if (playerCombat == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) playerCombat.TryCastSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) playerCombat.TryCastSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) playerCombat.TryCastSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) playerCombat.TryCastSlot(3);
    }

    // ========================================
    // UI (Grimoire, interaction) — pas de consommateur encore, événements seulement
    // ========================================

    private void HandleUIInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            OnGrimoireTogglePressed?.Invoke();
            if (logInputs) Debug.Log("[Input] Grimoire toggle");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            OnInteractPressed?.Invoke();
            if (logInputs) Debug.Log("[Input] Interact");
        }
    }
}

using UnityEngine;

/// <summary>
/// Gestionnaire d'input centralisé — Lit les inputs et les distribue.
/// Supporte Legacy Input Manager ET New Input System (mode Both).
/// 
/// Layout clavier/souris :
///   WASD          → Mouvement
///   Souris        → Visée / Facing
///   Clic gauche   → Attaque
///   Clic droit    → Skill secondaire (futur)
///   Espace        → Esquive
///   Shift gauche  → Bloc (maintenir)
///   Ctrl gauche   → Sprint (maintenir)
///   1-6           → Skills (futur)
///   Tab           → Skill tree (futur)
///   I             → Inventaire (futur)
///   
/// Layout manette :
///   Stick gauche  → Mouvement
///   Stick droit   → Visée
///   X / Square    → Attaque
///   A / Cross     → Esquive
///   LT            → Bloc (maintenir)
///   LB            → Sprint (maintenir)
///   RT/RB         → Skills (futur)
/// </summary>
public class GameInput : MonoBehaviour
{
    [Header("Références — Glisser depuis l'Inspector")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CombatController combatController;
    [SerializeField] private SkillCaster skillCaster;

    [Header("Sensibilité")]
    [SerializeField] private float gamepadDeadzone = 0.15f;
    [SerializeField] private float gamepadAimDeadzone = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool logInputs = false;

    // Détection automatique clavier vs manette
    private bool isUsingGamepad;
    private float lastGamepadInput;
    private float lastKeyboardInput;

    private void Update()
    {
        DetectInputDevice();

        HandleMovementInput();
        HandleAimInput();
        HandleCombatInput();
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
    // VISÉE / FACING (chaque frame)
    // ========================================

    private void HandleAimInput()
    {
        if (playerController == null) return;

        if (isUsingGamepad)
        {
            // Stick droit pour viser
            Vector2 aim = Vector2.zero;
            try
            {
                aim = new Vector2(
                    Input.GetAxisRaw("RightStickHorizontal"),
                    Input.GetAxisRaw("RightStickVertical")
                );
            }
            catch { /* Axe non configuré */ }

            if (aim.magnitude < gamepadAimDeadzone)
                aim = Vector2.zero;

            playerController.SetAimInput(aim, true);
        }
        else
        {
            // La souris contrôle le facing — géré directement par PlayerController
            // via le raycast sur le plan du sol
            playerController.SetAimInput(Vector2.zero, false);
        }
    }

    // ========================================
    // COMBAT (boutons)
    // ========================================

    private void HandleCombatInput()
    {
        if (combatController == null) return;

        // === ESQUIVE ===
        bool dodgePressed = Input.GetKeyDown(KeyCode.Space);

        // Gamepad : bouton A / Cross
        try { dodgePressed |= Input.GetButtonDown("Jump"); } catch { }

        if (dodgePressed)
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

            combatController.OnDodgeInput(moveDir);

            if (logInputs) Debug.Log($"[Input] Esquive direction: {moveDir}");
        }

        // === BLOC ===
        bool blockHeld;
        if (isUsingGamepad)
        {
            // LT (Left Trigger)
            float lt = 0f;
            try { lt = Input.GetAxisRaw("LeftTrigger"); } catch { }
            blockHeld = lt > 0.3f;
        }
        else
        {
            blockHeld = Input.GetMouseButton(1); // Right Click
        }

        combatController.OnBlockInput(blockHeld);

        // === SPRINT ===
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

        combatController.OnSprintInput(sprintHeld);

        // === ATTAQUE ===
        bool attackPressed;
        if (isUsingGamepad)
        {
            // X / Square
            bool xButton = false;
            try { xButton = Input.GetButtonDown("Fire1"); } catch { }
            attackPressed = xButton;
        }
        else
        {
            attackPressed = Input.GetMouseButtonDown(0);
        }

        if (attackPressed)
        {
            combatController.OnAttackInput();
            if (logInputs) Debug.Log("[Input] Attaque");
        }

        // === SKILLS (touches 1-4) ===
        if (skillCaster != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) skillCaster.TryCastSkill(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) skillCaster.TryCastSkill(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) skillCaster.TryCastSkill(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) skillCaster.TryCastSkill(3);
        }
    }
}

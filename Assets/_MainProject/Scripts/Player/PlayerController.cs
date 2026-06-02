using System;
using UnityEngine;

/// <summary>
/// Contrôleur du joueur — Style PoE 2 / Lost Ark.
/// WASD (ou stick gauche) contrôle le MOUVEMENT.
/// Souris (ou stick droit) contrôle le FACING (direction du regard/visée).
/// 
/// Remplace l'ancien PlayerController basé sur NavMeshAgent.
/// Utilise CharacterController pour un mouvement direct et réactif.
/// 
/// REQUIRES: CharacterController (au lieu de NavMeshAgent)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Mouvement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float acceleration = 50f;          // Vitesse d'accélération (très haute = réactif)
    [SerializeField] private float deceleration = 40f;          // Vitesse de décélération
    [SerializeField] private float gravity = -25f;              // Gravité appliquée
    [SerializeField] private float groundCheckDistance = 0.3f;

    [Header("Rotation / Facing")]
    [SerializeField] private float rotationSpeed = 20f;         // Vitesse de rotation vers la souris
    [SerializeField] private LayerMask groundLayer;             // Layer du sol pour le raycast souris
    [SerializeField] private bool instantRotation = false;      // true = snap immédiat vers la souris (style PoE2)

    [Header("Caméra")]
    [SerializeField] private Camera mainCamera;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = false;

    // === Composants ===
    private CharacterController characterController;

    // === État du mouvement ===
    private Vector3 currentVelocity;       // Vélocité actuelle (smooth)
    private Vector3 targetVelocity;        // Vélocité cible
    private float verticalVelocity;        // Gravité
    private bool isGrounded;

    // === État du facing ===
    private Vector3 aimWorldPosition;      // Position au sol visée par la souris
    private Vector3 facingDirection;        // Direction dans laquelle le joueur regarde
    private bool hasAimTarget;             // La souris a touché le sol (raycast réussi)

    // === Input (reçu de GameInput) ===
    private Vector2 moveInput;             // WASD / stick gauche
    private Vector2 aimInput;              // Stick droit (gamepad)
    private bool usingGamepad;             // true = stick droit pour viser

    // === Locks (les autres systèmes peuvent bloquer le mouvement/rotation) ===
    private bool movementLocked;
    private bool rotationLocked;
    private float speedMultiplier = 1f;

    // === Événements ===
    public event Action<Vector3, float> OnMove;           // (direction, speed)
    public event Action OnStopMoving;
    public event Action<Vector3> OnFacingChanged;         // (direction)

    // === Propriétés publiques ===
    public float MoveSpeed => moveSpeed;
    public float CurrentSpeed => new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f && !movementLocked;
    public bool IsGrounded => isGrounded;
    public Vector3 FacingDirection => facingDirection;
    public Vector3 AimWorldPosition => aimWorldPosition;
    public Vector3 MoveDirection => GetWorldMoveDirection();
    public CharacterController Controller => characterController;

    // ========================================
    // INITIALISATION
    // ========================================

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (mainCamera == null) mainCamera = Camera.main;

        facingDirection = transform.forward;
    }

    // ========================================
    // UPDATE PRINCIPAL
    // ========================================

    private void Update()
    {
        UpdateGroundCheck();
        UpdateMovement();
        UpdateFacing();
        ApplyGravity();
        ApplyMovement();
    }

    // ========================================
    // MOUVEMENT (WASD / Stick gauche)
    // ========================================

    private void UpdateMovement()
    {
        if (movementLocked)
        {
            // Décélérer vers zéro quand le mouvement est verrouillé
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * Time.deltaTime);
            return;
        }

        // Convertir l'input 2D en direction 3D relative à la caméra
        Vector3 worldDirection = GetWorldMoveDirection();

        if (worldDirection.sqrMagnitude > 0.01f)
        {
            // Calculer la vélocité cible
            float effectiveSpeed = moveSpeed * speedMultiplier;
            targetVelocity = worldDirection * effectiveSpeed;

            // Accélérer vers la cible (très rapide = réactif, pas de lag)
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);

            OnMove?.Invoke(worldDirection, CurrentSpeed);
        }
        else
        {
            // Pas d'input → décélérer
            if (currentVelocity.sqrMagnitude > 0.01f)
            {
                currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * Time.deltaTime);

                if (currentVelocity.sqrMagnitude <= 0.01f)
                {
                    currentVelocity = Vector3.zero;
                    OnStopMoving?.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// Convertit l'input WASD/stick en direction world space relative à la caméra.
    /// Prend en compte l'angle isométrique.
    /// </summary>
    private Vector3 GetWorldMoveDirection()
    {
        if (moveInput.sqrMagnitude < 0.01f)
            return Vector3.zero;

        // Direction de la caméra projetée sur le plan horizontal
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Combiner avec l'input
        Vector3 direction = (camForward * moveInput.y + camRight * moveInput.x);

        // Normaliser (éviter les diagonales plus rapides)
        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        return direction;
    }

    // ========================================
    // FACING (Souris / Stick droit)
    // ========================================

    private void UpdateFacing()
    {
        if (rotationLocked) return;

        Vector3 newFacing;

        if (usingGamepad && aimInput.sqrMagnitude > 0.1f)
        {
            // === GAMEPAD : stick droit contrôle le facing ===
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            newFacing = (camForward * aimInput.y + camRight * aimInput.x).normalized;
        }
        else
        {
            // === CLAVIER/SOURIS : souris contrôle le facing ===
            UpdateMouseAimPosition();

            if (!hasAimTarget) return;

            newFacing = (aimWorldPosition - transform.position);
            newFacing.y = 0f;

            if (newFacing.sqrMagnitude < 0.01f) return;
            newFacing.Normalize();
        }

        // Appliquer la rotation
        facingDirection = newFacing;

        if (instantRotation)
        {
            // Snap immédiat (style PoE 2 — le joueur fait toujours face à la souris)
            transform.rotation = Quaternion.LookRotation(facingDirection);
        }
        else
        {
            // Rotation smooth (plus cinématique)
            Quaternion targetRotation = Quaternion.LookRotation(facingDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        OnFacingChanged?.Invoke(facingDirection);
    }

    /// <summary>
    /// Raycast depuis la souris vers le plan du sol pour trouver le point visé.
    /// </summary>
    private void UpdateMouseAimPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Méthode 1 : Raycast sur le layer du sol
        if (groundLayer != 0 && Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayer))
        {
            aimWorldPosition = hit.point;
            hasAimTarget = true;
            return;
        }

        // Méthode 2 (fallback) : Intersection avec le plan y=0
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            aimWorldPosition = ray.GetPoint(distance);
            hasAimTarget = true;
            return;
        }

        hasAimTarget = false;
    }

    // ========================================
    // GRAVITÉ & APPLICATION
    // ========================================

    private void UpdateGroundCheck()
    {
        // CharacterController.isGrounded est fiable si on applique un petit push vers le bas
        isGrounded = characterController.isGrounded;
    }

    private void ApplyGravity()
    {
        if (isGrounded && verticalVelocity < 0f)
        {
            // Petit push vers le bas pour que isGrounded reste stable
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void ApplyMovement()
    {
        // Combiner mouvement horizontal + gravité
        Vector3 finalMove = new Vector3(currentVelocity.x, verticalVelocity, currentVelocity.z);
        characterController.Move(finalMove * Time.deltaTime);
    }

    // ========================================
    // API PUBLIQUE — Input (appelé par GameInput)
    // ========================================

    /// <summary>
    /// Input de mouvement (WASD ou stick gauche). Appelé chaque frame.
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    /// <summary>
    /// Input de visée (stick droit, gamepad uniquement). Appelé chaque frame.
    /// </summary>
    public void SetAimInput(Vector2 input, bool isGamepad)
    {
        aimInput = input;
        usingGamepad = isGamepad;
    }

    // ========================================
    // API PUBLIQUE — Locks (appelé par DodgeRoll, BlockSystem, etc.)
    // ========================================

    /// <summary>
    /// Verrouille le mouvement (pendant l'esquive, un stun, etc.)
    /// </summary>
    public void LockMovement(bool locked)
    {
        movementLocked = locked;
        if (locked)
        {
            moveInput = Vector2.zero;
            targetVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Verrouille la rotation (pendant une animation, un sort canalisé, etc.)
    /// </summary>
    public void LockRotation(bool locked)
    {
        rotationLocked = locked;
    }

    /// <summary>
    /// Modifie le multiplicateur de vitesse (sprint, slow, buff, etc.)
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    /// <summary>
    /// Retourne le multiplicateur de vitesse actuel.
    /// </summary>
    public float GetSpeedMultiplier()
    {
        return speedMultiplier;
    }

    /// <summary>
    /// Retourne la vitesse de base (pour SprintController).
    /// </summary>
    public float GetBaseSpeed()
    {
        return moveSpeed;
    }

    /// <summary>
    /// Force une direction de facing (pour la riposte, les cutscenes, etc.)
    /// </summary>
    public void ForceFacing(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) return;

        facingDirection = direction.normalized;
        transform.rotation = Quaternion.LookRotation(facingDirection);
    }

    /// <summary>
    /// Déplace le joueur d'un delta (pour DodgeRoll, knockback, etc.)
    /// Le CharacterController gère les collisions automatiquement.
    /// </summary>
    public void MoveByDelta(Vector3 delta)
    {
        characterController.Move(delta);
    }

    /// <summary>
    /// Téléporte le joueur à une position (checkpoint, portail, etc.)
    /// </summary>
    public void Teleport(Vector3 position)
    {
        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = true;
        currentVelocity = Vector3.zero;
        verticalVelocity = 0f;
    }

    // ========================================
    // DEBUG
    // ========================================

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Direction de mouvement (bleu)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, GetWorldMoveDirection() * 2f);

        // Direction de facing (rouge)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, facingDirection * 3f);

        // Point de visée souris (jaune)
        if (hasAimTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(aimWorldPosition, 0.3f);
        }
    }
}

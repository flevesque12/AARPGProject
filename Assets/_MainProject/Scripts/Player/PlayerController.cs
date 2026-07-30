using System;
using UnityEngine;

/// <summary>
/// Mode de facing du joueur : suit le mouvement, ou vise le curseur souris au sol
/// (activé par PlayerCombat pendant le cast d'un sort — système à venir).
/// </summary>
public enum FacingMode
{
    Movement,
    Aim
}

/// <summary>
/// Contrôleur du joueur — 3e personne (style Mages of Mystralia).
/// WASD (ou stick gauche) contrôle le MOUVEMENT, relatif à la caméra.
/// En FacingMode.Movement (défaut), le joueur fait face à sa direction de déplacement.
/// En FacingMode.Aim, il fait face au point visé par la souris au sol (pour le cast de sorts).
/// La caméra elle-même est entièrement pilotée par le jeu (ThirdPersonCamera) — aucun input
/// souris ne la contrôle, la souris ne sert qu'à la visée.
///
/// Utilise CharacterController pour un mouvement direct et réactif.
///
/// REQUIRES: CharacterController
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
    [SerializeField] private float rotationSpeed = 12f;         // Vitesse de rotation vers la cible de facing
    [SerializeField] private bool instantRotation = false;      // true = snap immédiat

    [Header("Visée (FacingMode.Aim)")]
    [Tooltip("Layer du sol pour le raycast souris. Vide = fallback sur le plan y=0.")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float aimRaycastMaxDistance = 200f;

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
    private FacingMode facingMode = FacingMode.Movement;
    private Vector3 facingDirection;        // Direction dans laquelle le joueur regarde
    private Vector3 aimWorldPosition;       // Point au sol visé par la souris (raycast caméra)
    private bool hasAimTarget;              // Le raycast souris a touché le sol

    // === Input (reçu de GameInput) ===
    private Vector2 moveInput;             // WASD / stick gauche

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
    public Vector3 MoveDirection => GetWorldMoveDirection();
    public CharacterController Controller => characterController;
    public FacingMode CurrentFacingMode => facingMode;
    public Vector3 AimWorldPosition => aimWorldPosition;

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
    // FACING (mouvement, ou visée souris pendant un cast)
    // ========================================

    private void UpdateFacing()
    {
        // Toujours à jour même en mode Movement : PlayerCombat en aura besoin pour
        // prévisualiser la visée avant même de basculer en FacingMode.Aim.
        UpdateAimWorldPosition();

        if (rotationLocked) return;

        Vector3 newFacing;

        if (facingMode == FacingMode.Aim)
        {
            if (!hasAimTarget) return;

            newFacing = aimWorldPosition - transform.position;
            newFacing.y = 0f;
            if (newFacing.sqrMagnitude < 0.01f) return;
            newFacing.Normalize();
        }
        else
        {
            // Pas d'input de mouvement → garder le dernier facing
            Vector3 moveDirection = GetWorldMoveDirection();
            if (moveDirection.sqrMagnitude < 0.01f) return;
            newFacing = moveDirection.normalized;
        }

        facingDirection = newFacing;

        if (instantRotation)
        {
            // Snap immédiat
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
    /// Raycast depuis la caméra à travers la position souris jusqu'au sol.
    /// </summary>
    private void UpdateAimWorldPosition()
    {
        if (mainCamera == null) { hasAimTarget = false; return; }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Méthode 1 : Raycast sur le layer du sol
        if (groundLayer != 0 && Physics.Raycast(ray, out RaycastHit hit, aimRaycastMaxDistance, groundLayer))
        {
            aimWorldPosition = hit.point;
            hasAimTarget = true;
        }
        else
        {
            // Méthode 2 (fallback) : intersection avec le plan y=0
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                aimWorldPosition = ray.GetPoint(distance);
                hasAimTarget = true;
            }
            else
            {
                hasAimTarget = false;
            }
        }

        if (showDebugGizmos && hasAimTarget)
            Debug.DrawRay(ray.origin, ray.direction * aimRaycastMaxDistance, Color.yellow);
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
    /// Change le mode de facing (Movement pendant le déplacement libre, Aim pendant
    /// le cast d'un sort). Appelé par PlayerCombat.
    /// </summary>
    public void SetFacingMode(FacingMode mode)
    {
        facingMode = mode;
    }

    // ========================================
    // API PUBLIQUE — Locks (appelé par DodgeRoll, etc.)
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
    /// Force une direction de facing (pour la visée de sort, les cutscenes, etc.)
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

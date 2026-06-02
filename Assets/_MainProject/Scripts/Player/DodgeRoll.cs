using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Roulade d'esquive directionnelle avec i-frames.
/// Version mise à jour pour CharacterController (remplace NavMeshAgent).
/// 
/// Le joueur esquive dans la direction WASD.
/// Si aucune direction → esquive vers l'arrière (opposé au facing).
/// Pendant l'esquive : i-frames (invulnérable) + mouvement verrouillé.
/// </summary>
public class DodgeRoll : MonoBehaviour
{
    [Header("Paramètres de l'esquive")]
    [SerializeField] private float dodgeDistance = 5f;
    [SerializeField] private float dodgeDuration = 0.4f;
    [SerializeField] private float iFrameStart = 0.05f;
    [SerializeField] private float iFrameDuration = 0.3f;
    [SerializeField] private float cooldown = 1.2f;
    [SerializeField] private float staminaCost = 25f;

    [Header("Animation")]
    [SerializeField] private AnimationCurve dodgeSpeedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Références")]
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private PlayerController playerController;

    // === État ===
    private bool isDodging;
    private bool isOnCooldown;
    private bool isInvulnerable;

    // === Événements ===
    public event Action OnDodgeStart;
    public event Action OnDodgeEnd;
    public event Action<bool> OnInvulnerabilityChanged;

    // === Propriétés publiques ===
    public bool IsDodging => isDodging;
    public bool IsInvulnerable => isInvulnerable;
    public bool CanDodge => !isDodging && !isOnCooldown && staminaSystem.CurrentStamina >= staminaCost;

    private void Awake()
    {
        if (staminaSystem == null) staminaSystem = GetComponent<StaminaSystem>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
    }

    /// <summary>
    /// Tente une esquive dans la direction donnée.
    /// </summary>
    public void TryDodge(Vector3 direction)
    {
        if (!CanDodge) return;
        if (!staminaSystem.ConsumeStamina(staminaCost)) return;

        // Direction par défaut : arrière du joueur (opposé au facing)
        if (direction.sqrMagnitude < 0.01f)
            direction = -transform.forward;
        else
            direction.Normalize();

        staminaSystem.SetInCombat();
        StartCoroutine(DodgeCoroutine(direction));
    }

    private IEnumerator DodgeCoroutine(Vector3 direction)
    {
        isDodging = true;
        isOnCooldown = true;
        OnDodgeStart?.Invoke();

        // Verrouiller le mouvement et la rotation du PlayerController
        playerController.LockMovement(true);
        playerController.LockRotation(true);

        // Tourner le joueur dans la direction de l'esquive
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        // Calculer la vitesse de l'esquive
        float dodgeSpeed = dodgeDistance / dodgeDuration;
        float elapsed = 0f;

        while (elapsed < dodgeDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / dodgeDuration;

            // i-frames
            bool shouldBeInvulnerable = elapsed >= iFrameStart && elapsed <= (iFrameStart + iFrameDuration);
            if (shouldBeInvulnerable != isInvulnerable)
            {
                isInvulnerable = shouldBeInvulnerable;
                OnInvulnerabilityChanged?.Invoke(isInvulnerable);
            }

            // Déplacement via CharacterController (gère les collisions automatiquement)
            float speedThisFrame = dodgeSpeed * dodgeSpeedCurve.Evaluate(normalizedTime);
            Vector3 moveThisFrame = direction * speedThisFrame * Time.deltaTime;

            // Ajouter la gravité
            moveThisFrame.y = -2f * Time.deltaTime;

            playerController.MoveByDelta(moveThisFrame);

            yield return null;
        }

        // Fin de l'esquive
        isInvulnerable = false;
        OnInvulnerabilityChanged?.Invoke(false);
        isDodging = false;

        // Déverrouiller le mouvement
        playerController.LockMovement(false);
        playerController.LockRotation(false);

        OnDodgeEnd?.Invoke();

        // Cooldown
        yield return new WaitForSeconds(cooldown);
        isOnCooldown = false;
    }

    /// <summary>
    /// Convertit l'input 2D en direction isométrique 3D.
    /// </summary>
    public static Vector3 GetDodgeDirection(Vector2 inputDirection, Camera cam)
    {
        if (inputDirection.sqrMagnitude < 0.01f)
            return Vector3.zero;

        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        return (camForward * inputDirection.y + camRight * inputDirection.x).normalized;
    }
}

using System;
using UnityEngine;

/// <summary>
/// Contrôle du sprint — version mise à jour pour CharacterController.
/// Modifie le speedMultiplier du PlayerController au lieu de la vitesse du NavMeshAgent.
/// </summary>
public class SprintController : MonoBehaviour
{
    [Header("Sprint")]
    [SerializeField] private float speedMultiplier = 1.6f;
    [SerializeField] private float staminaDrainPerSecond = 10f;
    [SerializeField] private float minStaminaToStart = 15f;

    [Header("Références")]
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private PlayerController playerController;

    // === État ===
    private bool isSprinting;
    private bool wantsToSprint;

    // === Événements ===
    public event Action<bool> OnSprintChanged;

    // === Propriétés publiques ===
    public bool IsSprinting => isSprinting;

    private void Awake()
    {
        if (staminaSystem == null) staminaSystem = GetComponent<StaminaSystem>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (wantsToSprint && !isSprinting)
        {
            if (staminaSystem.CurrentStamina >= minStaminaToStart)
                StartSprint();
        }
        else if (isSprinting)
        {
            if (!staminaSystem.DrainStamina(staminaDrainPerSecond))
            {
                StopSprint();
                return;
            }

            staminaSystem.SetInCombat();
        }
    }

    public void SetSprintInput(bool pressed)
    {
        wantsToSprint = pressed;

        if (!pressed && isSprinting)
            StopSprint();
    }

    private void StartSprint()
    {
        if (isSprinting) return;

        isSprinting = true;
        playerController.SetSpeedMultiplier(speedMultiplier);
        OnSprintChanged?.Invoke(true);
    }

    private void StopSprint()
    {
        if (!isSprinting) return;

        isSprinting = false;
        playerController.SetSpeedMultiplier(1f);
        OnSprintChanged?.Invoke(false);
    }

    public void ForceStopSprint()
    {
        wantsToSprint = false;
        StopSprint();
    }
}

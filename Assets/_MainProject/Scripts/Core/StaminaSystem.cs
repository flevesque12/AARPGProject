using System;
using UnityEngine;

/// <summary>
/// Système d'endurance (stamina) — utilisé par le Socle Commun et l'école Ferrum.
/// Fonctionne comme le HealthSystem mais avec régénération automatique.
/// Attacher au GameObject du joueur.
/// 
/// Consommateurs (v4.0 — BlockSystem retiré, voir CLAUDE.md Phase 5) :
///   - DodgeRoll      → ConsumeStamina(25)
///   - SprintController → DrainStamina(10/sec)
///   - Skills Ferrum  → ConsumeStamina(variable)
/// </summary>
public class StaminaSystem : MonoBehaviour
{
    [Header("Endurance")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float regenRate = 15f;                // Points régénérés par seconde
    [SerializeField] private float regenDelay = 1.5f;              // Délai après consommation avant regen
    [SerializeField] private float outOfCombatRegenMultiplier = 3f; // Regen ×3 hors combat

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;

    private float currentStamina;
    private float timeSinceLastUse;
    private bool isInCombat;
    private float combatTimer;
    private const float COMBAT_TIMEOUT = 5f;

    // === Événements ===
    /// <summary>Déclenché chaque fois que l'endurance change. (current, max)</summary>
    public event Action<float, float> OnStaminaChanged;

    /// <summary>Déclenché quand l'endurance tombe à 0 ou qu'une action échoue par manque.</summary>
    public event Action OnStaminaEmpty;

    /// <summary>Déclenché quand l'endurance atteint le maximum.</summary>
    public event Action OnStaminaFull;

    // === Propriétés publiques ===
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float StaminaRatio => currentStamina / maxStamina;
    public bool HasStamina => currentStamina > 0f;
    public bool IsFullStamina => currentStamina >= maxStamina;

    private void Awake()
    {
        currentStamina = maxStamina;
        timeSinceLastUse = regenDelay; // Permet la regen dès le début
    }

    private void Update()
    {
        UpdateCombatTimer();
        UpdateRegeneration();
    }

    // ========================================
    // COMBAT TIMER
    // ========================================

    private void UpdateCombatTimer()
    {
        if (isInCombat)
        {
            combatTimer -= Time.deltaTime;
            if (combatTimer <= 0f)
                isInCombat = false;
        }
    }

    // ========================================
    // RÉGÉNÉRATION
    // ========================================

    private void UpdateRegeneration()
    {
        timeSinceLastUse += Time.deltaTime;

        if (timeSinceLastUse >= regenDelay && currentStamina < maxStamina)
        {
            float regenAmount = regenRate * Time.deltaTime;

            // Regen plus rapide hors combat
            if (!isInCombat)
                regenAmount *= outOfCombatRegenMultiplier;

            currentStamina = Mathf.Min(currentStamina + regenAmount, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);

            if (currentStamina >= maxStamina)
                OnStaminaFull?.Invoke();
        }
    }

    // ========================================
    // CONSOMMATION
    // ========================================

    /// <summary>
    /// Tente de consommer de l'endurance. Retourne false si pas assez.
    /// Utilisé par : DodgeRoll (25), skills Ferrum.
    /// </summary>
    public bool ConsumeStamina(float amount)
    {
        if (amount <= 0f) return true;

        if (currentStamina < amount)
        {
            if (showDebugLog)
                Debug.Log($"[Stamina] Pas assez d'endurance : {currentStamina:F0}/{amount:F0} requis");

            OnStaminaEmpty?.Invoke();
            return false;
        }

        currentStamina -= amount;
        timeSinceLastUse = 0f; // Reset le délai de regen
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        if (showDebugLog)
            Debug.Log($"[Stamina] -{amount:F0} → {currentStamina:F0}/{maxStamina:F0}");

        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            OnStaminaEmpty?.Invoke();
        }

        return true;
    }

    /// <summary>
    /// Consomme de l'endurance par seconde (pour Sprint, Toggle skills Ferrum).
    /// Retourne false si l'endurance tombe à 0.
    /// </summary>
    public bool DrainStamina(float amountPerSecond)
    {
        return ConsumeStamina(amountPerSecond * Time.deltaTime);
    }

    // ========================================
    // RESTAURATION
    // ========================================

    /// <summary>
    /// Restaure de l'endurance (potion, kill bonus, Endurance du Guerrier passif, etc.)
    /// </summary>
    public void RestoreStamina(float amount)
    {
        if (amount <= 0f) return;

        float previous = currentStamina;
        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);

        if (currentStamina != previous)
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        if (currentStamina >= maxStamina)
            OnStaminaFull?.Invoke();
    }

    /// <summary>
    /// Remet l'endurance au maximum (respawn, changement de zone, etc.)
    /// </summary>
    public void FillStamina()
    {
        currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        OnStaminaFull?.Invoke();
    }

    // ========================================
    // ÉTAT DE COMBAT
    // ========================================

    /// <summary>
    /// Signale que le joueur est en combat (ralentit la regen).
    /// Appeler depuis DodgeRoll, SprintController quand le joueur
    /// effectue une action de combat.
    /// </summary>
    public void SetInCombat()
    {
        isInCombat = true;
        combatTimer = COMBAT_TIMEOUT;
    }

    /// <summary>
    /// Force la sortie de combat (cutscene, safe zone, etc.)
    /// </summary>
    public void ForceOutOfCombat()
    {
        isInCombat = false;
        combatTimer = 0f;
    }

    // ========================================
    // MODIFICATION DU MAX (équipement, buffs)
    // ========================================

    /// <summary>
    /// Modifie l'endurance max (équipement, buff Vigueur, Endurance du Guerrier T1 Ferrum).
    /// </summary>
    /// <param name="newMax">Nouvelle valeur maximale</param>
    /// <param name="fillToMax">Si true, remplit l'endurance au nouveau max</param>
    public void SetMaxStamina(float newMax, bool fillToMax = false)
    {
        maxStamina = Mathf.Max(1f, newMax); // Minimum 1

        if (fillToMax || currentStamina > maxStamina)
            currentStamina = maxStamina;

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    /// <summary>
    /// Modifie le taux de régénération (buff, debuff, zone spéciale).
    /// </summary>
    public void SetRegenRate(float newRate)
    {
        regenRate = Mathf.Max(0f, newRate);
    }
}
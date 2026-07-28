using System;
using UnityEngine;

/// <summary>
/// Système de Mana — ressource de cast des sorts (Grimoire).
/// Remplace StaminaSystem pour le rôle de cast ; fonctionne sur le même principe
/// (pool + régénération) mais reste un système séparé : StaminaSystem continue de
/// gérer dodge/sprint/bloc (v3.1) jusqu'à leur simplification en Phase 5/6.
/// Attacher au GameObject du joueur.
///
/// Pool de départ : ~100 (voir CLAUDE.md). Augmente via les Fontaines d'Arcanite
/// dans les Bibliothèques (SetMaxMana).
///
/// Consommateurs :
///   - SpellCaster / PlayerCombat → ConsumeMana(coût du sort, ex. 5-75 selon complexité)
/// </summary>
public class ManaSystem : MonoBehaviour
{
    [Header("Mana")]
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float regenRate = 10f;                 // Points régénérés par seconde
    [SerializeField] private float regenDelay = 1.5f;               // Délai après consommation avant regen
    [SerializeField] private float outOfCombatRegenMultiplier = 3f; // Regen ×3 hors combat

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;

    private float currentMana;
    private float timeSinceLastUse;
    private bool isInCombat;
    private float combatTimer;
    private const float COMBAT_TIMEOUT = 5f;

    // === Événements ===
    /// <summary>Déclenché chaque fois que le mana change. (current, max)</summary>
    public event Action<float, float> OnManaChanged;

    /// <summary>Déclenché quand le mana tombe à 0 ou qu'un cast échoue par manque.</summary>
    public event Action OnManaEmpty;

    /// <summary>Déclenché quand le mana atteint le maximum.</summary>
    public event Action OnManaFull;

    // === Propriétés publiques ===
    public float CurrentMana => currentMana;
    public float MaxMana => maxMana;
    public float ManaRatio => currentMana / maxMana;
    public bool HasMana => currentMana > 0f;
    public bool IsFullMana => currentMana >= maxMana;

    private void Awake()
    {
        currentMana = maxMana;
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

        if (timeSinceLastUse >= regenDelay && currentMana < maxMana)
        {
            float regenAmount = regenRate * Time.deltaTime;

            // Regen plus rapide hors combat
            if (!isInCombat)
                regenAmount *= outOfCombatRegenMultiplier;

            currentMana = Mathf.Min(currentMana + regenAmount, maxMana);
            OnManaChanged?.Invoke(currentMana, maxMana);

            if (currentMana >= maxMana)
                OnManaFull?.Invoke();
        }
    }

    // ========================================
    // CONSOMMATION
    // ========================================

    /// <summary>
    /// Vérifie si le pool contient assez de mana, sans le consommer.
    /// Utilisé par la HUD/Grimoire pour griser les sorts inabordables.
    /// </summary>
    public bool HasEnoughMana(float amount)
    {
        return currentMana >= amount;
    }

    /// <summary>
    /// Tente de consommer du mana pour un cast. Retourne false si pas assez.
    /// Utilisé par SpellCaster/PlayerCombat au moment de lancer un sort.
    /// </summary>
    public bool ConsumeMana(float amount)
    {
        if (amount <= 0f) return true;

        if (currentMana < amount)
        {
            if (showDebugLog)
                Debug.Log($"[Mana] Pas assez de mana : {currentMana:F0}/{amount:F0} requis");

            OnManaEmpty?.Invoke();
            return false;
        }

        currentMana -= amount;
        timeSinceLastUse = 0f; // Reset le délai de regen
        OnManaChanged?.Invoke(currentMana, maxMana);

        if (showDebugLog)
            Debug.Log($"[Mana] -{amount:F0} → {currentMana:F0}/{maxMana:F0}");

        if (currentMana <= 0f)
        {
            currentMana = 0f;
            OnManaEmpty?.Invoke();
        }

        return true;
    }

    // ========================================
    // RESTAURATION
    // ========================================

    /// <summary>
    /// Restaure du mana (potion, Fontaine d'Arcanite, kill bonus, etc.)
    /// </summary>
    public void RestoreMana(float amount)
    {
        if (amount <= 0f) return;

        float previous = currentMana;
        currentMana = Mathf.Min(currentMana + amount, maxMana);

        if (currentMana != previous)
            OnManaChanged?.Invoke(currentMana, maxMana);

        if (currentMana >= maxMana)
            OnManaFull?.Invoke();
    }

    /// <summary>
    /// Remet le mana au maximum (respawn, changement de zone, etc.)
    /// </summary>
    public void FillMana()
    {
        currentMana = maxMana;
        OnManaChanged?.Invoke(currentMana, maxMana);
        OnManaFull?.Invoke();
    }

    // ========================================
    // ÉTAT DE COMBAT
    // ========================================

    /// <summary>
    /// Signale que le joueur est en combat (ralentit la regen).
    /// Appeler depuis PlayerCombat/SpellCaster quand un sort est lancé.
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
    // MODIFICATION DU MAX (Fontaines d'Arcanite, Savoir Magique)
    // ========================================

    /// <summary>
    /// Modifie le mana max (Fontaine d'Arcanite, seuil de Savoir Magique).
    /// </summary>
    /// <param name="newMax">Nouvelle valeur maximale</param>
    /// <param name="fillToMax">Si true, remplit le mana au nouveau max</param>
    public void SetMaxMana(float newMax, bool fillToMax = false)
    {
        maxMana = Mathf.Max(1f, newMax); // Minimum 1

        if (fillToMax || currentMana > maxMana)
            currentMana = maxMana;

        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    /// <summary>
    /// Modifie le taux de régénération (buff, debuff, zone spéciale).
    /// </summary>
    public void SetRegenRate(float newRate)
    {
        regenRate = Mathf.Max(0f, newRate);
    }
}

using UnityEngine;
using System;

/// <summary>
/// Système de vie générique utilisé par le joueur ET les ennemis.
/// Gère les dégâts, la mort, et la régénération.
/// 
/// SETUP: Ajouter sur tout GameObject qui a de la vie (joueur, ennemis).
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float healthRegenPerSecond = 0f;
    [SerializeField] private bool destroyOnDeath = false;
    [SerializeField] private float destroyDelay = 2f;

    // --- Événements ---
    public event Action<float, float> OnHealthChanged;    // (current, max)
    public event Action<float> OnDamaged;                  // (damageAmount)
    public event Action OnDeath;
    public event Action<float> OnShieldChanged;            // (shieldAmount)

    // --- Propriétés ---
    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public float HealthPercent => CurrentHealth / maxHealth;

    /// <summary>
    /// Bouclier absorbant — consommé avant la vie normale dans TakeDamage. Alimenté par
    /// SpellCraft/Runtime/AuraSpell.cs (forme de base Aura).
    /// </summary>
    public float ShieldAmount { get; private set; }

    private DodgeRoll _dodgeRoll;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        _dodgeRoll = GetComponent<DodgeRoll>();
    }

    private void Update()
    {
        // Régénération passive
        if (!IsDead && healthRegenPerSecond > 0 && CurrentHealth < maxHealth)
        {
            Heal(healthRegenPerSecond * Time.deltaTime);
        }
    }

    /// <summary>
    /// Inflige des dégâts à cette entité.
    /// </summary>
    public void TakeDamage(float damage, GameObject attacker = null)
    {
        if (IsDead) return;
        if (damage <= 0) return;

        if (_dodgeRoll != null && _dodgeRoll.IsInvulnerable) return; // absorbed by dodge i-frames

        if (ShieldAmount > 0f)
        {
            float absorbed = Mathf.Min(ShieldAmount, damage);
            ShieldAmount -= absorbed;
            damage -= absorbed;
            OnShieldChanged?.Invoke(ShieldAmount);
            if (damage <= 0f) return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnDamaged?.Invoke(damage);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Ajoute (ou remplace, selon l'appelant) un bouclier absorbant. Voir AuraSpell.Init.
    /// </summary>
    public void AddShield(float amount)
    {
        if (amount <= 0f) return;
        ShieldAmount += amount;
        OnShieldChanged?.Invoke(ShieldAmount);
    }

    /// <summary>
    /// Retire le bouclier restant (expiration de l'Aura). Voir AuraSpell.ExpireAfter.
    /// </summary>
    public void ClearShield()
    {
        if (ShieldAmount <= 0f) return;
        ShieldAmount = 0f;
        OnShieldChanged?.Invoke(ShieldAmount);
    }

    /// <summary>
    /// Soigne cette entité.
    /// </summary>
    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        OnDeath?.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    /// <summary>
    /// Remet la vie au max (pour respawn).
    /// </summary>
    public void ResetHealth()
    {
        IsDead = false;
        CurrentHealth = maxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}

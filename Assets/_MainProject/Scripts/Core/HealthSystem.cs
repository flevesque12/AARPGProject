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

    // --- Propriétés ---
    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public float HealthPercent => CurrentHealth / maxHealth;

    private void Awake()
    {
        CurrentHealth = maxHealth;
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

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnDamaged?.Invoke(damage);

        if (CurrentHealth <= 0)
        {
            Die();
        }
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

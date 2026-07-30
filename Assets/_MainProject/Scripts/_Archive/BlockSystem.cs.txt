using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Système de blocage avec Bloc Parfait.
/// Socle Commun — disponible pour tout joueur.
/// Gère le bloc normal (maintenir) et le Bloc Parfait (timing).
/// </summary>
public class BlockSystem : MonoBehaviour
{
    [Header("Bloc Normal")]
    [SerializeField] private float blockReduction = 0.6f;          // 60% réduction de dégâts
    [SerializeField] private float blockAngle = 120f;               // Angle de couverture devant le joueur
    [SerializeField] private float staminaCostPerHit = 15f;         // Endurance consommée par coup bloqué

    [Header("Bloc Parfait")]
    [SerializeField] private float perfectBlockWindow = 0.25f;      // Fenêtre de timing (secondes)
    [SerializeField] private float perfectBlockReduction = 0.9f;    // 90% réduction
    [SerializeField] private float perfectBlockStaminaCostMult = 0.5f; // Coût endurance ÷2
    [SerializeField] private float perfectBlockSlowmoScale = 0.15f; // Time.timeScale pendant le slowmo
    [SerializeField] private float perfectBlockSlowmoDuration = 0.2f; // Durée du slowmo (real time)

    [Header("Références")]
    [SerializeField] private StaminaSystem staminaSystem;

    // === État ===
    private bool isBlocking;
    private bool isPerfectBlockWindow;
    private float blockStartTime;
    private bool isInSlowmo;
    private Coroutine slowmoCoroutine;

    // === Événements ===
    public event Action OnBlockStart;
    public event Action OnBlockEnd;
    public event Action OnPerfectBlock;                             // Déclenché sur Bloc Parfait réussi
    public event Action<float> OnBlockHit;                          // Dégâts résiduels après réduction
    public event Action OnBlockBroken;                              // Plus d'endurance → bloc cassé

    // === Propriétés publiques ===
    public bool IsBlocking => isBlocking;
    public bool IsPerfectBlockWindow => isPerfectBlockWindow;

    private void Awake()
    {
        if (staminaSystem == null) staminaSystem = GetComponent<StaminaSystem>();
    }

    /// <summary>
    /// Commence à bloquer. Appeler quand le joueur APPUIE sur le bouton de bloc.
    /// </summary>
    public void StartBlock()
    {
        if (isBlocking) return;

        isBlocking = true;
        blockStartTime = Time.time;
        isPerfectBlockWindow = true;
        OnBlockStart?.Invoke();

        // La fenêtre de Bloc Parfait se ferme après le délai
        StartCoroutine(PerfectBlockWindowCoroutine());
    }

    /// <summary>
    /// Arrête de bloquer. Appeler quand le joueur RELÂCHE le bouton.
    /// </summary>
    public void StopBlock()
    {
        if (!isBlocking) return;

        isBlocking = false;
        isPerfectBlockWindow = false;
        OnBlockEnd?.Invoke();
    }

    /// <summary>
    /// Traite un coup entrant pendant que le joueur bloque.
    /// Retourne les dégâts résiduels (après réduction).
    /// Appeler depuis HealthSystem AVANT d'appliquer les dégâts.
    /// </summary>
    /// <param name="rawDamage">Dégâts bruts de l'attaque</param>
    /// <param name="attackerPosition">Position de l'attaquant (pour vérifier l'angle)</param>
    /// <returns>Dégâts résiduels à appliquer. -1 si pas bloqué.</returns>
    public float ProcessIncomingDamage(float rawDamage, Vector3 attackerPosition)
    {
        if (!isBlocking) return -1f; // Pas en train de bloquer

        // Vérifier l'angle : le bloc ne protège que de face
        Vector3 toAttacker = (attackerPosition - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, toAttacker);

        if (angle > blockAngle * 0.5f)
            return -1f; // Attaque vient de derrière → pas bloqué

        // Déterminer si c'est un Bloc Parfait
        bool isPerfect = isPerfectBlockWindow;
        float reduction = isPerfect ? perfectBlockReduction : blockReduction;
        float staminaCost = staminaCostPerHit * (isPerfect ? perfectBlockStaminaCostMult : 1f);

        // Vérifier l'endurance
        if (!staminaSystem.ConsumeStamina(staminaCost))
        {
            // Plus d'endurance → bloc cassé, dégâts complets
            StopBlock();
            OnBlockBroken?.Invoke();
            return rawDamage;
        }

        staminaSystem.SetInCombat();

        float residualDamage = rawDamage * (1f - reduction);

        if (isPerfect)
        {
            // === BLOC PARFAIT ===
            OnPerfectBlock?.Invoke();
            slowmoCoroutine = StartCoroutine(PerfectBlockSlowmo());
        }

        OnBlockHit?.Invoke(residualDamage);
        return residualDamage;
    }

    private IEnumerator PerfectBlockWindowCoroutine()
    {
        yield return new WaitForSeconds(perfectBlockWindow);
        isPerfectBlockWindow = false;
    }

    private IEnumerator PerfectBlockSlowmo()
    {
        if (isInSlowmo) yield break;
        isInSlowmo = true;

        // Slowmo
        Time.timeScale = perfectBlockSlowmoScale;
        Time.fixedDeltaTime = 0.02f * perfectBlockSlowmoScale;

        // Attendre en temps réel (pas affecté par timeScale)
        yield return new WaitForSecondsRealtime(perfectBlockSlowmoDuration);

        // Restaurer
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        isInSlowmo = false;
    }

    /// <summary>
    /// Force l'arrêt du slowmo (si le joueur esquive ou lance un skill pendant).
    /// </summary>
    public void CancelSlowmo()
    {
        if (!isInSlowmo) return;
        if (slowmoCoroutine != null) StopCoroutine(slowmoCoroutine);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        isInSlowmo = false;
        slowmoCoroutine = null;
    }
}

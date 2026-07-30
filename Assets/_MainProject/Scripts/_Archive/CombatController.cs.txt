using System;
using UnityEngine;

/// <summary>
/// Contrôleur de combat — orchestre le Socle Commun.
/// Gère les priorités entre esquive, bloc, riposte, sprint et attaque.
/// Attacher au joueur. Lit les inputs et délègue aux systèmes.
/// 
/// PRIORITÉ DES ACTIONS :
/// 1. Esquive (interrompt tout sauf une autre esquive)
/// 2. Riposte (si fenêtre ouverte + input attaque)
/// 3. Bloc (maintenu)
/// 4. Attaque de base
/// 5. Sprint (actif quand rien d'autre)
/// </summary>
public class CombatController : MonoBehaviour
{
    [Header("Composants du Socle Commun")]
    [SerializeField] private DodgeRoll dodgeRoll;
    [SerializeField] private BlockSystem blockSystem;
    [SerializeField] private RiposteSystem riposteSystem;
    [SerializeField] private SprintController sprintController;
    [SerializeField] private StaminaSystem staminaSystem;

    [Header("Attaque de base")]
    [SerializeField] private float baseDamage = 25f;            // Dégâts de base (sera remplacé par les stats d'arme)
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackAngle = 80f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private int maxComboHits = 3;               // Combo 3 coups
    [SerializeField] private float comboWindow = 0.8f;           // Temps pour enchaîner le prochain coup
    [SerializeField] private float thirdHitBonusDamage = 0.2f;   // +20% au 3e coup
    [SerializeField] private LayerMask enemyLayer;

    [Header("Références")]
    [SerializeField] private Camera mainCamera;

    // === État du combat ===
    private int currentComboHit;
    private float lastAttackTime;
    private float comboTimer;

    // === État des inputs (mis à jour par le script d'input) ===
    private bool inputDodge;
    private bool inputBlock;
    private bool inputAttack;
    private bool inputSprint;
    private Vector2 inputMoveDirection;

    // === Événements ===
    public event Action<int> OnComboHit;               // (numéro du coup : 1, 2, 3)
    public event Action OnComboReset;
    public event Action<float> OnAttackHit;            // (dégâts infligés)

    // === Propriétés publiques ===
    // currentComboHit > 0 signifie qu'un combo est en cours (comboTimer actif)
    public bool IsPerformingAction => dodgeRoll.IsDodging || currentComboHit > 0;
    public bool CanAct => !dodgeRoll.IsDodging;
    public int CurrentComboHit => currentComboHit;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (dodgeRoll == null) dodgeRoll = GetComponent<DodgeRoll>();
        if (blockSystem == null) blockSystem = GetComponent<BlockSystem>();
        if (riposteSystem == null) riposteSystem = GetComponent<RiposteSystem>();
        if (sprintController == null) sprintController = GetComponent<SprintController>();
        if (staminaSystem == null) staminaSystem = GetComponent<StaminaSystem>();
    }

    private void Update()
    {
        UpdateComboTimer();
        ProcessInputs();
    }

    // ========================================
    // INPUT — Ces méthodes sont appelées par le script d'input (GameInput)
    // ========================================

    /// <summary>Bouton Esquive appuyé (une seule fois)</summary>
    public void OnDodgeInput(Vector2 moveDir)
    {
        inputDodge = true;
        inputMoveDirection = moveDir;
    }

    /// <summary>Bouton Bloc maintenu/relâché</summary>
    public void OnBlockInput(bool held)
    {
        inputBlock = held;
    }

    /// <summary>Bouton Attaque appuyé (une seule fois)</summary>
    public void OnAttackInput()
    {
        inputAttack = true;
    }

    /// <summary>Bouton Sprint maintenu/relâché</summary>
    public void OnSprintInput(bool held)
    {
        inputSprint = held;
    }

    // ========================================
    // LOGIQUE DE PRIORITÉ
    // ========================================

    private void ProcessInputs()
    {
        // --- PRIORITÉ 1 : ESQUIVE ---
        if (inputDodge)
        {
            inputDodge = false;

            if (dodgeRoll.CanDodge)
            {
                // Interrompre le bloc et le sprint
                blockSystem.StopBlock();
                sprintController.ForceStopSprint();
                blockSystem.CancelSlowmo();

                Vector3 dodgeDir = DodgeRoll.GetDodgeDirection(inputMoveDirection, mainCamera);
                dodgeRoll.TryDodge(dodgeDir);
                return; // L'esquive est prioritaire
            }
        }

        // Pendant l'esquive, rien d'autre ne fonctionne
        if (dodgeRoll.IsDodging) return;

        // --- PRIORITÉ 2 : RIPOSTE (si fenêtre ouverte + attaque) ---
        if (inputAttack && riposteSystem.IsRiposteWindowOpen)
        {
            inputAttack = false;
            sprintController.ForceStopSprint();

            if (riposteSystem.TryRiposte(baseDamage))
            {
                // Riposte réussie
                ResetCombo();
                return;
            }
            // Si la riposte n'a touché personne, on continue vers l'attaque normale
        }

        // --- PRIORITÉ 3 : BLOC ---
        if (inputBlock)
        {
            sprintController.ForceStopSprint();

            if (!blockSystem.IsBlocking)
                blockSystem.StartBlock();

            // Pas d'attaque pendant le bloc (sauf riposte, gérée au-dessus)
            inputAttack = false;
        }
        else
        {
            if (blockSystem.IsBlocking)
                blockSystem.StopBlock();
        }

        // --- PRIORITÉ 4 : ATTAQUE DE BASE ---
        if (inputAttack && !blockSystem.IsBlocking)
        {
            inputAttack = false;
            sprintController.ForceStopSprint();
            TryAttack();
        }

        // --- PRIORITÉ 5 : SPRINT ---
        sprintController.SetSprintInput(inputSprint && !blockSystem.IsBlocking && currentComboHit == 0);

        // Reset les inputs one-shot
        inputAttack = false;
        inputDodge = false;
    }

    // ========================================
    // ATTAQUE DE BASE — COMBO 3 COUPS
    // ========================================

    private void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        // Avancer le combo
        if (Time.time - lastAttackTime > comboWindow || currentComboHit >= maxComboHits)
            ResetCombo();

        currentComboHit++;
        lastAttackTime = Time.time;
        comboTimer = comboWindow;

        // Calculer les dégâts du coup
        float damage = baseDamage;
        if (currentComboHit == maxComboHits)
            damage *= (1f + thirdHitBonusDamage); // 3e coup = +20%

        OnComboHit?.Invoke(currentComboHit);

        // Détecter les ennemis en cône
        PerformAttackDetection(damage);
    }

    private void PerformAttackDetection(float damage)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

        foreach (Collider hit in hits)
        {
            Vector3 toEnemy = (hit.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toEnemy);

            if (angle > attackAngle * 0.5f) continue;

            HealthSystem enemyHealth = hit.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
                OnAttackHit?.Invoke(damage);
                staminaSystem.SetInCombat();

                // Tourner vers la cible
                Vector3 lookDir = hit.transform.position - transform.position;
                lookDir.y = 0f;
                if (lookDir != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }

    private void UpdateComboTimer()
    {
        if (currentComboHit > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
                ResetCombo();
        }
    }

    private void ResetCombo()
    {
        if (currentComboHit > 0)
        {
            currentComboHit = 0;
            OnComboReset?.Invoke();
        }
    }

    // ========================================
    // API PUBLIQUE pour les autres systèmes
    // ========================================

    /// <summary>
    /// Appelé par HealthSystem quand le joueur reçoit des dégâts.
    /// Vérifie le bloc et l'invulnérabilité avant d'appliquer.
    /// Retourne les dégâts résiduels à appliquer.
    /// </summary>
    public float FilterIncomingDamage(float rawDamage, Vector3 attackerPosition)
    {
        // Invulnérable (esquive) → 0 dégâts
        if (dodgeRoll.IsInvulnerable)
            return 0f;

        // En train de bloquer → dégâts réduits
        if (blockSystem.IsBlocking)
        {
            float residual = blockSystem.ProcessIncomingDamage(rawDamage, attackerPosition);
            if (residual >= 0f)
                return residual; // Bloc réussi (même partiel)
        }

        // Pas de protection → dégâts complets
        return rawDamage;
    }

    /// <summary>
    /// Met à jour les dégâts de base (quand le joueur change d'arme).
    /// </summary>
    public void SetBaseDamage(float newDamage)
    {
        baseDamage = newDamage;
    }
}

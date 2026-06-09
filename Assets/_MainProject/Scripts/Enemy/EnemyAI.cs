using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// IA d'ennemi simple avec State Machine.
/// États : Idle → Chase → Attack → Dead
/// 
/// SETUP:
///   1. Créer un GameObject ennemi (Capsule, couleur rouge)
///   2. Ajouter NavMeshAgent
///   3. Ajouter HealthSystem (destroyOnDeath = true, destroyDelay = 2)
///   4. Ajouter HitFeedback
///   5. Ajouter ce script
///   6. Layer = "Enemy"
///   7. Tag = "Enemy"
///   8. S'assurer que le NavMesh est baked sur le sol
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(HealthSystem))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Idle, Chase, Attack, Dead }

    [Header("Detection")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float loseAggroRange = 18f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackDamage = 17f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackWindup = 0.3f; // Temps avant que l'attaque touche

    [Header("Mouvement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Idle Patrol (optionnel)")]
    [SerializeField] private bool enablePatrol = false;
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float patrolWaitTime = 2f;

    [Header("Loot (optionnel)")]
    [SerializeField] private GameObject[] lootDropPrefabs;
    [SerializeField, Range(0f, 1f)] private float dropChance = 0.3f;

    // --- État ---
    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

    private NavMeshAgent agent;
    private HealthSystem health;
    private Animator animator;
    private Transform player;
    private float lastAttackTime = -999f;
    private float patrolTimer;
    private Vector3 spawnPosition;
    private bool isAttacking = false;
    private PostureSystem _posture;
    private EnemyTelegraph _telegraph;
    private Coroutine _attackCoroutine;

    // Animator parameter hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int StaggeredHash = Animator.StringToHash("Staggered");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<HealthSystem>();
        animator = GetComponentInChildren<Animator>();

        agent.speed = moveSpeed;
        agent.angularSpeed = 0;
        agent.updateRotation = false;
        agent.stoppingDistance = attackRange * 0.8f;

        spawnPosition = transform.position;
        _posture = GetComponent<PostureSystem>();
        _telegraph = GetComponent<EnemyTelegraph>();
    }

    private void OnEnable()
    {
        health.OnDeath += HandleDeath;
        health.OnDamaged += HandleDamaged;
        if (_posture != null)
        {
            _posture.OnStaggerEnter.AddListener(HandleStaggerEnter);
            _posture.OnStaggerExit.AddListener(HandleStaggerExit);
        }
    }

    private void OnDisable()
    {
        health.OnDeath -= HandleDeath;
        health.OnDamaged -= HandleDamaged;
        if (_posture != null)
        {
            _posture.OnStaggerEnter.RemoveListener(HandleStaggerEnter);
            _posture.OnStaggerExit.RemoveListener(HandleStaggerExit);
        }
    }

    private void Start()
    {
        // Trouver le joueur
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void Update()
    {
        if (CurrentState == EnemyState.Dead) return;
        if (player == null) return;
        if (_posture != null && _posture.IsStaggered) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        switch (CurrentState)
        {
            case EnemyState.Idle:
                UpdateIdle(distToPlayer);
                break;
            case EnemyState.Chase:
                UpdateChase(distToPlayer);
                break;
            case EnemyState.Attack:
                UpdateAttack(distToPlayer);
                break;
        }

        // Rotation vers la cible pendant le chase/attack
        if (CurrentState == EnemyState.Chase || CurrentState == EnemyState.Attack)
        {
            RotateTowardsPlayer();
        }

        // Update animator
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat(SpeedHash, agent.velocity.magnitude);
    }

    // === IDLE ===
    private void UpdateIdle(float distToPlayer)
    {
        // Détection du joueur
        if (distToPlayer <= detectionRange)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // Patrouille optionnelle
        if (enablePatrol)
        {
            patrolTimer -= Time.deltaTime;
            if (patrolTimer <= 0 && !agent.hasPath)
            {
                Vector3 randomPoint = spawnPosition + Random.insideUnitSphere * patrolRadius;
                randomPoint.y = spawnPosition.y;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
                patrolTimer = patrolWaitTime + Random.Range(0f, patrolWaitTime * 0.5f);
            }
        }
    }

    // === CHASE ===
    private void UpdateChase(float distToPlayer)
    {
        // Perd l'aggro si trop loin
        if (distToPlayer > loseAggroRange)
        {
            agent.SetDestination(spawnPosition);
            ChangeState(EnemyState.Idle);
            return;
        }

        // À portée d'attaque — pas d'entrée en Attack si staggerisé
        if (distToPlayer <= attackRange && (_posture == null || !_posture.IsStaggered))
        {
            agent.ResetPath();
            ChangeState(EnemyState.Attack);
            return;
        }

        // Poursuit le joueur
        agent.SetDestination(player.position);
    }

    // === ATTACK ===
    private void UpdateAttack(float distToPlayer)
    {
        // Joueur hors de portée : reprend la chasse
        if (distToPlayer > attackRange * 1.3f)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // Cooldown pas fini
        if (Time.time < lastAttackTime + attackCooldown) return;
        if (isAttacking) return;

        // Lancer l'attaque
        _attackCoroutine = StartCoroutine(PerformAttack());
    }

    private System.Collections.IEnumerator PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // Trigger attack animation
        if (animator != null)
            animator.SetTrigger(AttackHash);

        // Windup — telegraph visuel (remplace l'ancien scale-up)
        if (_telegraph != null)
            _telegraph.Telegraph(attackWindup);

        yield return new WaitForSeconds(attackWindup);

        // Annulé par stagger pendant le windup (HandleStaggerEnter) — sécurité
        if (_posture != null && _posture.IsStaggered)
        {
            isAttacking = false;
            _attackCoroutine = null;
            yield break;
        }

        if (player != null && !health.IsDead)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= attackRange * 1.2f)
            {
                HealthSystem playerHealth = player.GetComponent<HealthSystem>();
                if (playerHealth != null)
                    playerHealth.TakeDamage(attackDamage, gameObject);
            }
        }

        isAttacking = false;
        _attackCoroutine = null;
    }

    // === DEATH ===
    private void HandleDeath()
    {
        ChangeState(EnemyState.Dead);
        agent.ResetPath();
        agent.enabled = false;

        // Trigger death animation
        if (animator != null)
            animator.SetTrigger(DeadHash);

        // Désactiver le collider pour ne plus bloquer
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Drop de loot
        TryDropLoot();
    }

    private void HandleDamaged(float damage)
    {
        // Trigger hit animation
        if (animator != null)
            animator.SetTrigger(HitHash);

        // Aggro immédiat si on prend des dégâts en Idle
        if (CurrentState == EnemyState.Idle)
        {
            ChangeState(EnemyState.Chase);
        }
    }

    private void TryDropLoot()
    {
        if (lootDropPrefabs == null || lootDropPrefabs.Length == 0) return;
        if (Random.value > dropChance) return;

        GameObject lootPrefab = lootDropPrefabs[Random.Range(0, lootDropPrefabs.Length)];
        if (lootPrefab != null)
        {
            Instantiate(lootPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }
    }

    private void HandleStaggerEnter()
    {
        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
            isAttacking = false;
        }
        if (_telegraph != null) _telegraph.Cancel();
        agent.ResetPath();
        agent.isStopped = true;

        // Trigger stagger animation
        if (animator != null)
            animator.SetBool(StaggeredHash, true);
    }

    private void HandleStaggerExit()
    {
        agent.isStopped = false;

        // Exit stagger animation
        if (animator != null)
            animator.SetBool(StaggeredHash, false);
    }

    private void RotateTowardsPlayer()
    {
        if (player == null) return;

        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private void ChangeState(EnemyState newState)
    {
        CurrentState = newState;
    }

    // === DEBUG ===
    private void OnDrawGizmosSelected()
    {
        // Range de détection
        Gizmos.color = new Color(1, 1, 0, 0.15f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Range de perte d'aggro
        Gizmos.color = new Color(1, 0.5f, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, loseAggroRange);

        // Range d'attaque
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Zone de patrouille
        if (enablePatrol)
        {
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
            Gizmos.DrawWireSphere(center, patrolRadius);
        }
    }
}

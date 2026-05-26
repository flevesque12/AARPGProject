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
    [SerializeField] private float attackDamage = 10f;
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
    private Transform player;
    private float lastAttackTime = -999f;
    private float patrolTimer;
    private Vector3 spawnPosition;
    private bool isAttacking = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<HealthSystem>();

        agent.speed = moveSpeed;
        agent.angularSpeed = 0;
        agent.updateRotation = false;
        agent.stoppingDistance = attackRange * 0.8f;

        spawnPosition = transform.position;
    }

    private void OnEnable()
    {
        health.OnDeath += HandleDeath;
        health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        health.OnDeath -= HandleDeath;
        health.OnDamaged -= HandleDamaged;
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

        // À portée d'attaque
        if (distToPlayer <= attackRange)
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
        StartCoroutine(PerformAttack());
    }

    private System.Collections.IEnumerator PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // Windup (telegraph visuel pour le joueur)
        // TODO: Animation ou scale-up pour signaler l'attaque
        Vector3 originalScale = transform.localScale;
        transform.localScale = originalScale * 1.1f;

        yield return new WaitForSeconds(attackWindup);

        transform.localScale = originalScale;

        // Vérifier que le joueur est encore à portée
        if (player != null && !health.IsDead)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= attackRange * 1.2f)
            {
                HealthSystem playerHealth = player.GetComponent<HealthSystem>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage, gameObject);
                }
            }
        }

        isAttacking = false;
    }

    // === DEATH ===
    private void HandleDeath()
    {
        ChangeState(EnemyState.Dead);
        agent.ResetPath();
        agent.enabled = false;

        // Désactiver le collider pour ne plus bloquer
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Drop de loot
        TryDropLoot();
    }

    private void HandleDamaged(float damage)
    {
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

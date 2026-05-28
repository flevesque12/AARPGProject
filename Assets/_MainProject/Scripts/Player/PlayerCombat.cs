using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Système de combat du joueur.
/// Attaque en cône avec squash/stretch et hit stop sur un hit réussi.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Attaque de base")]
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackAngle = 90f;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float attackMoveLockDuration = 0.2f;

    [Header("Détection")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Feedback visuel")]
    [SerializeField] private GameObject hitParticlePrefab;
    [SerializeField] private float hitParticleLifetime = 0.5f;
    [SerializeField] private float knockbackForce = 3f;

    [Header("Auto-aim (pour manette)")]
    [SerializeField] private float autoAimRange = 5f;
    [SerializeField] private bool autoAimEnabled = true;

    [Header("Hit Stop")]
    [SerializeField] private float hitStopDuration = 0.06f;
    [SerializeField] [Range(0f, 0.2f)] private float hitStopTimeScale = 0.05f;

    private float lastAttackTime = -999f;
    private bool isAttacking = false;
    private Coroutine hitStopCoroutine;

    private PlayerController playerController;
    private HealthSystem healthSystem;
    private Animator animator;
    private Transform modelTransform;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        healthSystem = GetComponent<HealthSystem>();
    }

    private void Start()
    {
        modelTransform = transform;
        animator = GetComponentInChildren<Animator>();
    }

    private void OnDisable()
    {
        // Sécurité : restaurer le timeScale si désactivé pendant un hit stop
        Time.timeScale = 1f;
        isAttacking = false;
    }

    private void Update()
    {
        if (healthSystem != null && healthSystem.IsDead) return;

        GameInput input = GameInput.Instance;
        if (input == null) return;

        if (input.AttackPressed && !isAttacking && Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        lastAttackTime = Time.time;
        isAttacking = true;

        if (autoAimEnabled && GameInput.Instance.IsUsingGamepad)
            AutoAimTowardsEnemy();
        else if (!GameInput.Instance.IsUsingGamepad)
            LookAtMouse();

        if (playerController != null)
            playerController.LockMovement(attackMoveLockDuration);

        animator?.SetTrigger("Attack");
        StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        Vector3 originalScale = modelTransform.localScale;

        // --- Phase 1 : Anticipation (squash — le personnage s'écrase avant de frapper) ---
        Vector3 squashScale = new Vector3(
            originalScale.x * 1.25f,
            originalScale.y * 0.8f,
            originalScale.z * 0.8f
        );

        float elapsed = 0f;
        float anticipationTime = 0.07f;
        while (elapsed < anticipationTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / anticipationTime);
            modelTransform.localScale = Vector3.LerpUnclamped(originalScale, squashScale, t);
            yield return null;
        }

        // --- Phase 2 : Release (stretch — allongement dans la direction d'attaque) ---
        modelTransform.localScale = new Vector3(
            originalScale.x * 0.8f,
            originalScale.y * 1.3f,
            originalScale.z * 1.3f
        );

        // Détection des ennemis et application des dégâts
        List<HealthSystem> hitEnemies = GetEnemiesInCone();

        foreach (HealthSystem enemy in hitEnemies)
        {
            enemy.TakeDamage(attackDamage, gameObject);
            ApplyKnockback(enemy.gameObject);
            SpawnHitEffect(enemy.transform.position);
        }

        // Hit stop seulement si au moins un ennemi touché
        if (hitEnemies.Count > 0)
        {
            if (hitStopCoroutine != null) StopCoroutine(hitStopCoroutine);
            hitStopCoroutine = StartCoroutine(HitStop());
        }

        // --- Phase 3 : Recovery (retour lissé à l'échelle originale) ---
        // Utilise unscaledDeltaTime pour que l'animation ne se fige pas pendant le hit stop
        elapsed = 0f;
        float recoveryTime = 0.1f;
        Vector3 startRecovery = modelTransform.localScale;

        while (elapsed < recoveryTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / recoveryTime);
            modelTransform.localScale = Vector3.Lerp(startRecovery, originalScale, t);
            yield return null;
        }

        modelTransform.localScale = originalScale;
        isAttacking = false;

        Debug.Log($"Attaque ! {hitEnemies.Count} ennemi(s) touché(s)");
    }

    private IEnumerator HitStop()
    {
        Time.timeScale = hitStopTimeScale;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
    }

    private List<HealthSystem> GetEnemiesInCone()
    {
        List<HealthSystem> results = new List<HealthSystem>();
        Collider[] colliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

        foreach (Collider col in colliders)
        {
            Vector3 dirToEnemy = (col.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(modelTransform.forward, dirToEnemy);

            if (angle <= attackAngle * 0.5f)
            {
                HealthSystem enemyHealth = col.GetComponent<HealthSystem>();
                if (enemyHealth != null && !enemyHealth.IsDead)
                    results.Add(enemyHealth);
            }
        }
        return results;
    }

    private void AutoAimTowardsEnemy()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, autoAimRange, enemyLayer);
        float closestDist = float.MaxValue;
        Transform closestEnemy = null;

        foreach (Collider col in nearby)
        {
            HealthSystem hp = col.GetComponent<HealthSystem>();
            if (hp != null && hp.IsDead) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestEnemy = col.transform;
            }
        }

        if (closestEnemy != null)
        {
            Vector3 dir = (closestEnemy.position - transform.position).normalized;
            dir.y = 0;
            modelTransform.rotation = Quaternion.LookRotation(dir);
        }
    }

    private void LookAtMouse()
    {
        Vector3 mousePos = GameInput.Instance.MouseWorldPosition;
        Vector3 dir = (mousePos - transform.position).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
            modelTransform.rotation = Quaternion.LookRotation(dir);
    }

    private void ApplyKnockback(GameObject enemy)
    {
        Vector3 knockDir = (enemy.transform.position - transform.position).normalized;
        knockDir.y = 0;

        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            Vector3 knockTarget = enemy.transform.position + knockDir * knockbackForce;
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(knockTarget, out hit, knockbackForce, UnityEngine.AI.NavMesh.AllAreas))
                agent.Warp(hit.position);
        }
        else
        {
            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(knockDir * knockbackForce, ForceMode.Impulse);
        }
    }

    private void SpawnHitEffect(Vector3 position)
    {
        if (hitParticlePrefab != null)
        {
            GameObject fx = Instantiate(hitParticlePrefab, position + Vector3.up, Quaternion.identity);
            Destroy(fx, hitParticleLifetime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.red;
        Vector3 forward = modelTransform != null ? modelTransform.forward : transform.forward;
        Vector3 leftBound = Quaternion.Euler(0, -attackAngle * 0.5f, 0) * forward;
        Vector3 rightBound = Quaternion.Euler(0, attackAngle * 0.5f, 0) * forward;
        Gizmos.DrawRay(transform.position, leftBound * attackRange);
        Gizmos.DrawRay(transform.position, rightBound * attackRange);

        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, autoAimRange);
    }
}

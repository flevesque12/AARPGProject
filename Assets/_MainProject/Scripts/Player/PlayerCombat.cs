using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Système de combat du joueur.
/// Gère l'attaque de base et la détection des ennemis.
///
/// SETUP:
///   1. Ajouter sur le même GameObject que PlayerController
///   2. Créer un Layer "Enemy" et assigner les ennemis dessus
///   3. Optionnel: assigner un prefab de particules pour le hit
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Attaque de base")]
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackAngle = 90f;    // Cône d'attaque en degrés
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

    private float lastAttackTime = -999f;
    private PlayerController playerController;
    private Transform modelTransform;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        // Utilise le même modelTransform que le PlayerController
        // ou le transform principal si non défini
        modelTransform = transform;
    }

    private void Update()
    {
        GameInput input = GameInput.Instance;
        if (input == null) return;

        // Attaque sur pression du bouton
        if (input.AttackPressed && Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        lastAttackTime = Time.time;

        // Auto-aim : tourne vers l'ennemi le plus proche si manette
        if (autoAimEnabled && GameInput.Instance.IsUsingGamepad)
        {
            AutoAimTowardsEnemy();
        }
        else if (!GameInput.Instance.IsUsingGamepad)
        {
            // Souris : tourne vers le curseur
            LookAtMouse();
        }

        // Verrouille le mouvement brièvement pendant l'attaque
        if (playerController != null)
        {
            playerController.LockMovement(attackMoveLockDuration);
        }

        // Détecte les ennemis dans le cône d'attaque
        List<HealthSystem> hitEnemies = GetEnemiesInCone();

        foreach (HealthSystem enemy in hitEnemies)
        {
            // Inflige des dégâts
            enemy.TakeDamage(attackDamage, gameObject);

            // Knockback
            ApplyKnockback(enemy.gameObject);

            // Particules de hit
            SpawnHitEffect(enemy.transform.position);
        }

        // Feedback visuel même si on ne touche personne (swing dans le vide)
        StartCoroutine(AttackVisualFeedback());

        // Debug visuel dans l'éditeur
        Debug.Log($"Attaque ! {hitEnemies.Count} ennemi(s) touché(s)");
    }

    private List<HealthSystem> GetEnemiesInCone()
    {
        List<HealthSystem> results = new List<HealthSystem>();
        Collider[] colliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

        foreach (Collider col in colliders)
        {
            // Vérifier que l'ennemi est dans le cône d'attaque
            Vector3 dirToEnemy = (col.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(modelTransform.forward, dirToEnemy);

            if (angle <= attackAngle * 0.5f)
            {
                HealthSystem enemyHealth = col.GetComponent<HealthSystem>();
                if (enemyHealth != null && !enemyHealth.IsDead)
                {
                    results.Add(enemyHealth);
                }
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
        {
            modelTransform.rotation = Quaternion.LookRotation(dir);
        }
    }

    private void ApplyKnockback(GameObject enemy)
    {
        // Knockback simple via déplacement direct
        Vector3 knockDir = (enemy.transform.position - transform.position).normalized;
        knockDir.y = 0;

        // Si l'ennemi a un NavMeshAgent, on le pousse via Warp
        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            Vector3 knockTarget = enemy.transform.position + knockDir * knockbackForce;
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(knockTarget, out hit, knockbackForce, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
        // Sinon via Rigidbody
        else
        {
            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(knockDir * knockbackForce, ForceMode.Impulse);
            }
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

    private IEnumerator AttackVisualFeedback()
    {
        // Scale punch sur le modèle (effet de swing satisfaisant)
        Vector3 originalScale = modelTransform.localScale;
        modelTransform.localScale = originalScale * 1.15f;
        yield return new WaitForSeconds(0.05f);
        modelTransform.localScale = originalScale * 0.9f;
        yield return new WaitForSeconds(0.05f);
        modelTransform.localScale = originalScale;
    }

    // --- Debug Gizmos ---
    private void OnDrawGizmosSelected()
    {
        // Dessine le range d'attaque
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Dessine le cône d'attaque
        Gizmos.color = Color.red;
        Vector3 forward = modelTransform != null ? modelTransform.forward : transform.forward;
        Vector3 leftBound = Quaternion.Euler(0, -attackAngle * 0.5f, 0) * forward;
        Vector3 rightBound = Quaternion.Euler(0, attackAngle * 0.5f, 0) * forward;
        Gizmos.DrawRay(transform.position, leftBound * attackRange);
        Gizmos.DrawRay(transform.position, rightBound * attackRange);

        // Dessine le range d'auto-aim
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, autoAimRange);
    }
}

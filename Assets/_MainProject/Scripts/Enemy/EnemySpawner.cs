using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawner d'ennemis pour tester le prototype.
/// Place des ennemis dans un rayon autour du spawner.
/// 
/// SETUP:
///   1. Créer un GameObject vide "EnemySpawner" dans la scène
///   2. Ajouter ce script
///   3. Assigner le prefab ennemi
///   4. Le NavMesh doit être baked
///
/// ASTUCE RAPIDE POUR LE PREFAB ENNEMI:
///   Créer un Capsule rouge avec ces composants :
///   - NavMeshAgent
///   - HealthSystem (maxHealth=50, destroyOnDeath=true, destroyDelay=1.5)
///   - HitFeedback
///   - EnemyAI
///   - WorldHealthBar
///   - Layer = "Enemy", Tag = "Enemy"
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int enemyCount = 8;
    [SerializeField] private float spawnRadius = 15f;
    [SerializeField] private float minDistFromPlayer = 5f;

    [Header("Respawn")]
    [SerializeField] private bool enableRespawn = true;
    [SerializeField] private float respawnDelay = 10f;
    [SerializeField] private int maxEnemies = 12;

    private int currentEnemyCount = 0;
    private Transform player;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // Spawn initial
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy();
        }

        // Respawn loop
        if (enableRespawn)
        {
            InvokeRepeating(nameof(TryRespawn), respawnDelay, respawnDelay);
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: pas de prefab ennemi assigné !");
            return;
        }

        if (currentEnemyCount >= maxEnemies) return;

        // Trouver une position valide sur le NavMesh
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * spawnRadius;
            randomPoint.y = transform.position.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, spawnRadius, NavMesh.AllAreas))
            {
                // Vérifier la distance minimum avec le joueur
                if (player != null)
                {
                    float distToPlayer = Vector3.Distance(hit.position, player.position);
                    if (distToPlayer < minDistFromPlayer) continue;
                }

                GameObject enemy = Instantiate(enemyPrefab, hit.position, Quaternion.identity);
                enemy.transform.SetParent(transform); // Organiser dans la hiérarchie

                // Écouter la mort pour le compteur
                HealthSystem enemyHealth = enemy.GetComponent<HealthSystem>();
                if (enemyHealth != null)
                {
                    enemyHealth.OnDeath += () => currentEnemyCount--;
                }

                currentEnemyCount++;
                return;
            }
        }

        Debug.LogWarning("EnemySpawner: impossible de trouver une position valide sur le NavMesh.");
    }

    private void TryRespawn()
    {
        if (currentEnemyCount < enemyCount)
        {
            SpawnEnemy();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.15f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, minDistFromPlayer);
    }
}

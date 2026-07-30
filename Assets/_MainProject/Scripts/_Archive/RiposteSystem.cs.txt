using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Système de Riposte — contre-attaque après un Bloc Parfait.
/// Socle Commun v3.1 — v4.0 n'a pas de riposte (voir CLAUDE.md, table Combat) ;
/// ce système reste en place pour l'instant en parallèle (retrait complet =
/// item 7 de la roadmap Phase 5), mais l'esquive ne déclenche plus sa fenêtre
/// (Phase 5 item 4 : DodgeRoll simplifié, plus de couplage esquive → riposte).
/// </summary>
public class RiposteSystem : MonoBehaviour
{
    [Header("Riposte")]
    [SerializeField] private float riposteWindow = 1.0f;           // Durée de la fenêtre (secondes)
    [SerializeField] private float riposteDamageMultiplier = 2.0f;  // ×2 dégâts d'arme
    [SerializeField] private float riposteRange = 3f;               // Portée de la riposte
    [SerializeField] private float riposteAngle = 90f;              // Angle devant le joueur
    [SerializeField] private LayerMask enemyLayer;

    [Header("Feedback")]
    [SerializeField] private float riposteSlowmoOnHit = 0.1f;      // Bref slowmo quand la riposte touche
    [SerializeField] private float riposteSlowmoDuration = 0.15f;

    [Header("Références")]
    [SerializeField] private BlockSystem blockSystem;

    // === État ===
    private bool isRiposteWindowOpen;
    private Coroutine windowCoroutine;

    // === Événements ===
    public event Action OnRiposteWindowOpen;
    public event Action OnRiposteWindowClose;
    public event Action<GameObject, float> OnRiposteHit; // (cible, dégâts infligés)

    // === Propriétés publiques ===
    public bool IsRiposteWindowOpen => isRiposteWindowOpen;
    public float DamageMultiplier => riposteDamageMultiplier;

    private void Awake()
    {
        if (blockSystem == null) blockSystem = GetComponent<BlockSystem>();
    }

    private void OnEnable()
    {
        if (blockSystem != null)
            blockSystem.OnPerfectBlock += OpenRiposteWindow;
    }

    private void OnDisable()
    {
        if (blockSystem != null)
            blockSystem.OnPerfectBlock -= OpenRiposteWindow;
    }

    /// <summary>
    /// Ouvre la fenêtre de riposte.
    /// </summary>
    public void OpenRiposteWindow()
    {
        // Reset la fenêtre si elle était déjà ouverte
        if (windowCoroutine != null)
            StopCoroutine(windowCoroutine);

        isRiposteWindowOpen = true;
        OnRiposteWindowOpen?.Invoke();
        windowCoroutine = StartCoroutine(RiposteWindowCoroutine());
    }

    /// <summary>
    /// Tente d'exécuter la riposte. Appeler quand le joueur appuie sur Attaque
    /// pendant que la fenêtre est ouverte.
    /// Retourne true si la riposte a touché.
    /// </summary>
    /// <param name="baseDamage">Dégâts de base de l'arme du joueur</param>
    public bool TryRiposte(float baseDamage)
    {
        if (!isRiposteWindowOpen) return false;

        // Fermer la fenêtre immédiatement (une seule riposte par fenêtre)
        CloseRiposteWindow();

        // Détecter les ennemis en cône devant le joueur
        float riposteDamage = baseDamage * riposteDamageMultiplier;
        bool hitSomething = false;

        Collider[] hits = Physics.OverlapSphere(transform.position, riposteRange, enemyLayer);

        foreach (Collider hit in hits)
        {
            // Vérifier l'angle
            Vector3 toEnemy = (hit.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toEnemy);

            if (angle > riposteAngle * 0.5f) continue;

            // Appliquer les dégâts (la riposte ignore l'armure)
            HealthSystem enemyHealth = hit.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(riposteDamage);
                OnRiposteHit?.Invoke(hit.gameObject, riposteDamage);
                hitSomething = true;

                var posture = hit.GetComponent<PostureSystem>();
                posture?.DegradePosture(posture.PostureMax * 0.5f);

                // Tourner le joueur vers la cible
                Vector3 lookDir = hit.transform.position - transform.position;
                lookDir.y = 0f;
                if (lookDir != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        // Feedback visuel si on a touché
        if (hitSomething)
        {
            StartCoroutine(RiposteHitSlowmo());
        }

        return hitSomething;
    }

    private void CloseRiposteWindow()
    {
        if (windowCoroutine != null)
            StopCoroutine(windowCoroutine);
        isRiposteWindowOpen = false;
        OnRiposteWindowClose?.Invoke();
    }

    private IEnumerator RiposteWindowCoroutine()
    {
        yield return new WaitForSeconds(riposteWindow);
        isRiposteWindowOpen = false;
        OnRiposteWindowClose?.Invoke();
    }

    private IEnumerator RiposteHitSlowmo()
    {
        Time.timeScale = riposteSlowmoOnHit;
        Time.fixedDeltaTime = 0.02f * riposteSlowmoOnHit;
        yield return new WaitForSecondsRealtime(riposteSlowmoDuration);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}

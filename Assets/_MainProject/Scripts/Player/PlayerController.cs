using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Contrôleur du joueur - mouvement isométrique.
/// Supporte deux modes simultanés :
///   - Point-and-click (clic droit pour se déplacer)
///   - Stick/WASD (mouvement direct)
/// Le stick override automatiquement le point-and-click.
///
/// SETUP:
///   1. Créer un GameObject joueur (Capsule)
///   2. Ajouter un NavMeshAgent (pour le pathfinding click-to-move)
///   3. Ajouter un Rigidbody (IsKinematic = true)
///   4. Ajouter un Collider
///   5. Tag = "Player"
///   6. Bake le NavMesh sur votre sol (Window > AI > Navigation)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerController : MonoBehaviour
{
    [Header("Mouvement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float rotationSpeed = 540f; // degrés par seconde
    [SerializeField] private float stickDeadzone = 0.1f;

    [Header("Click-to-Move")]
    [SerializeField] private GameObject clickIndicatorPrefab; // Optionnel: indicateur au sol
    [SerializeField] private float clickStoppingDistance = 0.2f;

    [Header("Références")]
    [SerializeField] private Transform modelTransform; // Le modèle 3D enfant (pour rotation)

    private NavMeshAgent agent;
    private HealthSystem health;
    private Animator animator;
    private bool isUsingClickMove = false;
    private Vector3 clickMoveTarget;
    private float currentSpeed = 0f;

    // Isometric direction conversion:
    // En vue iso, "haut" sur le stick doit correspondre à "haut-droite" dans le monde.
    // On applique une rotation de 45° (ou selon l'angle de ta caméra).
    private readonly Matrix4x4 isoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0));

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<HealthSystem>();
        animator = GetComponentInChildren<Animator>();

        // Config du NavMeshAgent
        agent.speed = moveSpeed;
        agent.angularSpeed = 0; // On gère la rotation nous-mêmes
        agent.acceleration = 50f; // Réponse rapide
        agent.stoppingDistance = clickStoppingDistance;
        agent.updateRotation = false;

        if (modelTransform == null)
            modelTransform = transform;
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        if (health != null && health.IsDead) return;

        HandleMovement();
        animator?.SetFloat("Speed", currentSpeed);
    }

    private void HandleMovement()
    {
        GameInput input = GameInput.Instance;
        if (input == null) return;

        Vector2 moveInput = input.MoveInput;
        bool hasStickInput = moveInput.sqrMagnitude > stickDeadzone * stickDeadzone;

        // --- Mode Stick/WASD (prioritaire) ---
        if (hasStickInput)
        {
            // Annule le click-to-move si on utilise le stick
            if (isUsingClickMove)
            {
                isUsingClickMove = false;
                agent.ResetPath();
            }

            // Convertir l'input en direction isométrique
            Vector3 inputDir = new Vector3(moveInput.x, 0, moveInput.y);
            Vector3 isoDir = isoMatrix.MultiplyPoint3x4(inputDir).normalized;

            // Déplacer via NavMeshAgent (pour respecter le navmesh)
            agent.Move(isoDir * moveSpeed * Time.deltaTime);

            // Vitesse réelle basée sur l'amplitude de l'input (WASD = magnitude 0→1)
            currentSpeed = moveInput.magnitude * moveSpeed;

            // Rotation vers la direction de mouvement
            RotateTowards(isoDir);
        }
        // --- Mode Click-to-Move ---
        else if (input.MoveClickPressed)
        {
            clickMoveTarget = input.MouseWorldPosition;
            agent.SetDestination(clickMoveTarget);
            isUsingClickMove = true;
            ShowClickIndicator(clickMoveTarget);
        }

        // Rotation et vitesse pendant le click-to-move
        if (isUsingClickMove && agent.hasPath && agent.remainingDistance > clickStoppingDistance)
        {
            currentSpeed = agent.velocity.magnitude;
            RotateTowards(agent.velocity.normalized);
        }
        else if (isUsingClickMove && (!agent.hasPath || agent.remainingDistance <= clickStoppingDistance))
        {
            isUsingClickMove = false;
            currentSpeed = 0f;
        }
        else if (!hasStickInput)
        {
            currentSpeed = 0f;
        }
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        modelTransform.rotation = Quaternion.RotateTowards(
            modelTransform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void ShowClickIndicator(Vector3 position)
    {
        if (clickIndicatorPrefab != null)
        {
            GameObject indicator = Instantiate(clickIndicatorPrefab, position + Vector3.up * 0.05f, Quaternion.Euler(90, 0, 0));
            Destroy(indicator, 0.5f);
        }
    }

    private void HandleDeath()
    {
        agent.ResetPath();
        agent.enabled = false;
        // TODO: Animation de mort, écran de game over
        Debug.Log("Le joueur est mort !");
    }

    /// <summary>
    /// Empêche le mouvement temporairement (pendant une attaque, un stun, etc.)
    /// </summary>
    public void LockMovement(float duration)
    {
        agent.ResetPath();
        agent.isStopped = true;
        isUsingClickMove = false;
        Invoke(nameof(UnlockMovement), duration);
    }

    private void UnlockMovement()
    {
        if (agent.enabled)
            agent.isStopped = false;
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Couche d'abstraction pour les inputs.
/// Supporte le nouveau Input System ET le Legacy Input Manager simultanément.
/// Le joueur peut utiliser clavier/souris OU manette à tout moment.
/// 
/// SETUP: Ajouter ce script sur un GameObject "GameInput" dans la scène.
/// Requires: New Input System Package installé + Both dans Player Settings > Active Input Handling
/// </summary>
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    [Header("New Input System Actions")]
    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction pointerPositionAction;
    private InputAction pointerClickAction;

    // --- État des inputs ---
    public Vector2 MoveInput { get; private set; }
    public Vector3 MouseWorldPosition { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool MoveClickPressed { get; private set; }
    public bool IsUsingGamepad { get; private set; }

    private Camera mainCam;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Définir les actions programmatiquement (pas besoin de .inputactions asset)
        moveAction = new InputAction("Move", InputActionType.Value, null);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        attackAction = new InputAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Mouse>/leftButton");
        attackAction.AddBinding("<Keyboard>/space");
        attackAction.AddBinding("<Gamepad>/buttonWest"); // X sur Xbox, Square sur PS

        pointerPositionAction = new InputAction("PointerPos", InputActionType.Value, "<Mouse>/position");
        pointerClickAction = new InputAction("PointerClick", InputActionType.Button, "<Mouse>/rightButton");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        attackAction.Enable();
        pointerPositionAction.Enable();
        pointerClickAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        attackAction.Disable();
        pointerPositionAction.Disable();
        pointerClickAction.Disable();
    }

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        // --- Mouvement ---
        // Nouveau Input System (priorité)
        Vector2 newMove = moveAction.ReadValue<Vector2>();

        // Legacy fallback
        Vector2 legacyMove = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // Utilise celui qui a une valeur
        MoveInput = newMove.sqrMagnitude > 0.01f ? newMove : legacyMove;

        // Détecte si le joueur utilise une manette
        IsUsingGamepad = Gamepad.current != null && newMove.sqrMagnitude > 0.01f;

        // --- Attaque ---
        AttackPressed = attackAction.WasPressedThisFrame() || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);

        // --- Clic de mouvement (point-and-click avec clic droit) ---
        MoveClickPressed = pointerClickAction.WasPressedThisFrame() || Input.GetMouseButtonDown(1);

        // --- Position de la souris dans le monde ---
        UpdateMouseWorldPosition();
    }

    private void UpdateMouseWorldPosition()
    {
        if (mainCam == null) return;

        Vector2 screenPos = pointerPositionAction.ReadValue<Vector2>();
        if (screenPos == Vector2.zero)
            screenPos = Input.mousePosition;

        // Raycast depuis la caméra vers le plan du sol (Y = 0)
        Ray ray = mainCam.ScreenPointToRay(screenPos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            MouseWorldPosition = ray.GetPoint(distance);
        }
    }
}

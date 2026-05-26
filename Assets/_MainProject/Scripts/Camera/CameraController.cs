using UnityEngine;

/// <summary>
/// Caméra isométrique qui suit le joueur.
/// Vue top-down avec angle ajustable et zoom à la molette.
///
/// SETUP:
///   1. Créer un GameObject vide "CameraRig"
///   2. Mettre la Main Camera en enfant de CameraRig
///   3. Ajouter ce script sur CameraRig
///   4. Positionner la Main Camera en enfant :
///      - Position locale : (0, 15, -15) (ajuster selon le goût)
///      - Rotation locale : (45, 0, 0)
///      - La caméra doit être en mode Orthographic ou Perspective selon préférence
///   
///   Alternative rapide : ajouter ce script sur la Main Camera directement
///   et il configurera tout automatiquement.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Cible")]
    [SerializeField] private Transform target; // Le joueur

    [Header("Position")]
    [SerializeField] private Vector3 offset = new Vector3(0, 18, -18);
    [SerializeField] private float followSpeed = 8f;

    [Header("Rotation")]
    [SerializeField] private float cameraAngle = 45f; // Angle en X
    [SerializeField] private float cameraYRotation = 45f; // Rotation en Y (pour vue iso)

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private float minZoom = 8f;
    [SerializeField] private float maxZoom = 25f;
    [SerializeField] private float currentZoom = 15f;

    [Header("Configuration auto")]
    [SerializeField] private bool useOrthographic = true;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponentInChildren<Camera>();
        if (cam == null)
            cam = GetComponent<Camera>();
    }

    private void Start()
    {
        // Trouver le joueur automatiquement si non assigné
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        // Configuration de la caméra
        if (cam != null)
        {
            cam.orthographic = useOrthographic;
            if (useOrthographic)
                cam.orthographicSize = currentZoom;
        }

        // Positionner immédiatement
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.rotation = Quaternion.Euler(cameraAngle, cameraYRotation, 0);
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Suivre le joueur avec smoothing
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Rotation fixe isométrique
        transform.rotation = Quaternion.Euler(cameraAngle, cameraYRotation, 0);

        // Zoom à la molette
        HandleZoom();
    }

    private void HandleZoom()
    {
        float scrollInput = Input.mouseScrollDelta.y;

        // Support manette (optionnel : triggers)
        if (UnityEngine.InputSystem.Gamepad.current != null)
        {
            float dpad = UnityEngine.InputSystem.Gamepad.current.dpad.y.ReadValue();
            if (Mathf.Abs(dpad) > 0.1f)
                scrollInput = dpad;
        }

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            currentZoom -= scrollInput * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

            if (cam != null)
            {
                if (cam.orthographic)
                {
                    cam.orthographicSize = currentZoom;
                }
                else
                {
                    // Pour perspective, ajuster l'offset
                    offset = offset.normalized * currentZoom * 1.5f;
                }
            }
        }
    }
}

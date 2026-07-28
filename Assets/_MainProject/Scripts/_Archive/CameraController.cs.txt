using UnityEngine;

/// <summary>
/// Caméra isométrique style Path of Exile 2 — perspective, angle prononcé, vue droite.
///
/// SETUP :
///   1. Ajouter ce script sur le GameObject "CameraRig" (vide)
///   2. Mettre la Main Camera en enfant de CameraRig
///   3. Reset la transform de la Main Camera (position/rotation à zéro)
///   4. S'assurer que la Main Camera est en mode Perspective
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Cible")]
    [SerializeField] private Transform target;

    [Header("Suivi")]
    [SerializeField] private float followSpeed = 8f;

    [Header("Angle caméra — style PoE2")]
    [SerializeField] private float pitchAngle = 60f;   // Inclinaison vers le sol (55-65 pour PoE2)
    [SerializeField] private float yawAngle   = 0f;    // Rotation horizontale (0 = face au nord)

    [Header("Distance / Zoom")]
    [SerializeField] private float distance    = 28f;
    [SerializeField] private float minDistance = 18f;
    [SerializeField] private float maxDistance = 48f;
    [SerializeField] private float zoomSpeed   = 4f;

    [Header("Perspective")]
    [SerializeField] private float fieldOfView = 38f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponentInChildren<Camera>();
        if (cam == null)
            cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        if (cam != null)
        {
            cam.orthographic = false;
            cam.fieldOfView  = fieldOfView;
        }

        if (target != null)
        {
            transform.position = CalculateDesiredPosition();
            transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleZoom();

        Vector3 desired = CalculateDesiredPosition();
        transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
    }

    // Place la caméra en arrière du joueur selon le pitch/yaw et la distance.
    private Vector3 CalculateDesiredPosition()
    {
        Quaternion rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        Vector3 offset = rotation * Vector3.back * distance;
        return target.position + offset;
    }

    private void HandleZoom()
    {
        float scrollInput = Input.mouseScrollDelta.y;

        if (UnityEngine.InputSystem.Gamepad.current != null)
        {
            float dpad = UnityEngine.InputSystem.Gamepad.current.dpad.y.ReadValue();
            if (Mathf.Abs(dpad) > 0.1f)
                scrollInput = dpad;
        }

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            distance -= scrollInput * zoomSpeed;
            distance  = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }
}

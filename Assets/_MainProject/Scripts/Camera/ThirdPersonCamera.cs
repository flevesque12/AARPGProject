using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

/// <summary>
/// Elevated top-down tracking camera — Cinemachine 3.x wrapper.
/// Fully game-controlled: the player has NO camera input. Mouse is reserved for spell aiming
/// (see PlayerController's FacingMode.Aim).
/// The framing angle is FIXED in world space — the camera only translates to follow the
/// player's position, it never swings around when the player turns (classic top-down feel,
/// as opposed to an over-the-shoulder camera that stays glued behind the character).
/// Replaces CameraController (PoE2-style isometric).
///
/// SETUP:
///   1. Add a CinemachineBrain component on the Main Camera.
///   2. Create a GameObject with CinemachineCamera + CinemachineFollow +
///      CinemachineRotationComposer + CinemachineDeoccluder + this script.
///   3. Leave Target empty to auto-find the GameObject tagged "Player".
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
[RequireComponent(typeof(CinemachineFollow))]
[RequireComponent(typeof(CinemachineRotationComposer))]
[RequireComponent(typeof(CinemachineDeoccluder))]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [Tooltip("Vertical offset for the look-at point (e.g. chest height instead of feet).")]
    [SerializeField] private float lookAtHeightOffset = 1f;

    [Header("Framing angle (fixed in world space)")]
    [Tooltip("Downward pitch angle, in degrees.")]
    [SerializeField] private float pitchAngle = 50f;
    [Tooltip("World-space yaw of the camera, in degrees (0 = looking down the -Z axis).")]
    [SerializeField] private float yawAngle = 0f;
    [Tooltip("Distance from the player along the pitched/yawed direction.")]
    [SerializeField] private float followDistance = 14f;

    [Header("Lens")]
    [SerializeField] private float fieldOfView = 55f;

    [Header("Damping (smooth follow, no hard snap)")]
    [SerializeField] private float positionDamping = 0.8f;
    [SerializeField] private float rotationDamping = 0.8f;

    [Header("Collision")]
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float minimumDistanceFromTarget = 0.5f;

    private CinemachineCamera vcam;
    private CinemachineFollow follow;
    private CinemachineRotationComposer rotationComposer;
    private CinemachineDeoccluder deoccluder;

    private void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
        follow = GetComponent<CinemachineFollow>();
        rotationComposer = GetComponent<CinemachineRotationComposer>();
        deoccluder = GetComponent<CinemachineDeoccluder>();
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        if (target != null)
        {
            vcam.Follow = target;
            vcam.LookAt = target;
        }

        vcam.Lens.FieldOfView = fieldOfView;

        rotationComposer.TargetOffset = Vector3.up * lookAtHeightOffset;
        rotationComposer.Damping = new Vector2(rotationDamping, rotationDamping);

        // WorldSpace: the offset does NOT rotate with the player's own yaw, so the
        // camera keeps a constant angle and only translates to follow position —
        // it never swings around when the player turns.
        TrackerSettings tracker = follow.TrackerSettings;
        tracker.BindingMode = BindingMode.WorldSpace;
        tracker.PositionDamping = new Vector3(positionDamping, positionDamping, positionDamping);
        follow.TrackerSettings = tracker;

        ApplyFraming();

        deoccluder.CollideAgainst = obstacleLayers;
        deoccluder.MinimumDistanceFromTarget = minimumDistanceFromTarget;
    }

    private void ApplyFraming()
    {
        follow.FollowOffset = Quaternion.Euler(pitchAngle, yawAngle, 0f) * Vector3.back * followDistance;
    }

    /// <summary>
    /// Changes the default yaw framing behind the player (for future per-zone camera blending).
    /// </summary>
    public void SetYawAngle(float degrees)
    {
        yawAngle = degrees;
        ApplyFraming();
    }

    /// <summary>
    /// Changes the follow distance (for future per-zone camera blending).
    /// </summary>
    public void SetDistance(float newDistance)
    {
        followDistance = newDistance;
        ApplyFraming();
    }
}

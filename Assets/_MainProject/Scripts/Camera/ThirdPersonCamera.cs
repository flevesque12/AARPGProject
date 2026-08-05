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
/// BUG FIX (2026-08-05): this used to also have a CinemachineRotationComposer (Aim stage)
/// driving rotation. That is a dynamic "keep the target centered in frame" behavior — it
/// continuously recomputes rotation from the camera's CURRENT (damped, laggy) position toward
/// LookAt, not a fixed angle. Since CinemachineFollow's Body stage only ever writes camera
/// POSITION (confirmed via Unity MCP: disabling Aim and moving the target produced zero
/// rotation change — Body never touches rotation at all), the Aim stage was the sole source of
/// a real, measurable rotation drift (~10° yaw + ~3° pitch just from a moderate player
/// displacement) as the player moved around, proportional to how far the damped camera
/// position lagged the ideal follow offset. That drift is what caused two symptoms: the
/// camera visibly not staying "fixed in place" as advertised, AND spells firing in the wrong
/// direction relative to the mouse — PlayerController's mouse-to-ground raycast
/// (mainCamera.ScreenPointToRay) uses whatever the camera's actual current rotation is, so a
/// silently rotating camera desyncs "where the cursor looks on screen" from "what direction
/// that raycast resolves to in world space". Fix: removed the RotationComposer entirely and
/// instead set this GameObject's own transform.rotation directly to the constant
/// pitch/yaw every frame (see LateUpdate) — confirmed via the same Unity MCP test that this
/// produces zero drift regardless of player displacement, since nothing else in the pipeline
/// (Body, Deoccluder) ever writes to rotation.
///
/// SETUP:
///   1. Add a CinemachineBrain component on the Main Camera.
///   2. Create a GameObject with CinemachineCamera + CinemachineFollow +
///      CinemachineDeoccluder + this script (no Aim component needed or wanted).
///   3. Leave Target empty to auto-find the GameObject tagged "Player".
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
[RequireComponent(typeof(CinemachineFollow))]
[RequireComponent(typeof(CinemachineDeoccluder))]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

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

    [Header("Collision")]
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float minimumDistanceFromTarget = 0.5f;

    private CinemachineCamera vcam;
    private CinemachineFollow follow;
    private CinemachineDeoccluder deoccluder;

    private void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
        follow = GetComponent<CinemachineFollow>();
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
            vcam.LookAt = target; // no longer used for framing (no Aim component), kept so CinemachineDeoccluder has an occlusion target
        }

        vcam.Lens.FieldOfView = fieldOfView;

        // WorldSpace: the offset does NOT rotate with the player's own yaw, and (per the bug
        // fix note above) Body never writes rotation at all — position-only tracking.
        TrackerSettings tracker = follow.TrackerSettings;
        tracker.BindingMode = BindingMode.WorldSpace;
        tracker.PositionDamping = new Vector3(positionDamping, positionDamping, positionDamping);
        follow.TrackerSettings = tracker;

        ApplyFraming();
        ApplyFixedRotation();

        deoccluder.CollideAgainst = obstacleLayers;
        deoccluder.MinimumDistanceFromTarget = minimumDistanceFromTarget;
    }

    // Reasserted every frame rather than only once in Start(): guarantees the angle can never
    // drift (see class doc), and automatically follows SetYawAngle()/SetDistance() changes
    // without extra plumbing.
    private void LateUpdate()
    {
        ApplyFixedRotation();
    }

    private void ApplyFixedRotation()
    {
        transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
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

using System.Collections;
using UnityEngine;

/// <summary>
/// Visual feedback for the player capsule:
/// - Sprint: Trail renderer + capsule squash/stretch + tilt forward
/// - Dodge: Trail renderer + capsule flatten + tint
///
/// v4.0: sword-swing (attack combo) and shield (block) visuals removed —
/// neither mechanic exists anymore (see CLAUDE.md, Combat table). Kept
/// sprint + dodge, which are still part of the Socle Commun.
/// </summary>
public class CombatVisualFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SprintController sprintController;
    [SerializeField] private DodgeRoll dodgeRoll;
    [SerializeField] private Renderer capsuleRenderer;
    [SerializeField] private Transform capsuleTransform;

    [Header("Sprint Visual")]
    [SerializeField] private TrailRenderer sprintTrail;
    [SerializeField] private float sprintTiltAngle = 15f;
    [SerializeField] private float sprintStretchAmount = 1.15f;
    [SerializeField] private Color sprintTintColor = new Color(0.4f, 0.9f, 1f, 1f);

    [Header("Dodge Visual")]
    [SerializeField] private Color dodgeColor = new Color(0.7f, 0.7f, 1f, 0.5f);

    // Internal state
    private MaterialPropertyBlock propBlock;
    private Color originalColor;
    private Vector3 originalCapsuleScale;
    private static readonly int ColorID = Shader.PropertyToID("_BaseColor");

    private bool wasSprinting;
    private bool wasDodging;

    private void Awake()
    {
        if (sprintController == null) sprintController = GetComponent<SprintController>();
        if (dodgeRoll == null) dodgeRoll = GetComponent<DodgeRoll>();

        if (capsuleRenderer == null) capsuleRenderer = GetComponentInChildren<MeshRenderer>();
        if (capsuleTransform == null && capsuleRenderer != null) capsuleTransform = capsuleRenderer.transform;

        propBlock = new MaterialPropertyBlock();

        if (capsuleRenderer != null)
        {
            capsuleRenderer.GetPropertyBlock(propBlock);
            if (capsuleRenderer.sharedMaterial.HasProperty(ColorID))
                originalColor = capsuleRenderer.sharedMaterial.GetColor(ColorID);
            else
                originalColor = Color.white;
        }

        if (capsuleTransform != null)
        {
            originalCapsuleScale = capsuleTransform.localScale;
        }

        SetupSprintTrail();
    }

    private void OnEnable()
    {
        if (sprintController != null)
        {
            sprintController.OnSprintChanged += OnSprintChanged;
        }
        if (dodgeRoll != null)
        {
            dodgeRoll.OnDodgeStart += OnDodgeStart;
            dodgeRoll.OnDodgeEnd += OnDodgeEnd;
        }
    }

    private void OnDisable()
    {
        if (sprintController != null)
        {
            sprintController.OnSprintChanged -= OnSprintChanged;
        }
        if (dodgeRoll != null)
        {
            dodgeRoll.OnDodgeStart -= OnDodgeStart;
            dodgeRoll.OnDodgeEnd -= OnDodgeEnd;
        }
    }

    private void Update()
    {
        UpdateSprintVisual();
    }

    // ========================================
    // SETUP — Create visual elements at runtime
    // ========================================

    private void SetupSprintTrail()
    {
        if (sprintTrail == null)
        {
            GameObject trailObj = new GameObject("SprintTrail");
            trailObj.transform.SetParent(transform);
            trailObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            sprintTrail = trailObj.AddComponent<TrailRenderer>();
            sprintTrail.time = 0.3f;
            sprintTrail.startWidth = 0.6f;
            sprintTrail.endWidth = 0.0f;
            sprintTrail.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            sprintTrail.material.SetColor(ColorID, sprintTintColor);
            sprintTrail.startColor = new Color(sprintTintColor.r, sprintTintColor.g, sprintTintColor.b, 0.6f);
            sprintTrail.endColor = new Color(sprintTintColor.r, sprintTintColor.g, sprintTintColor.b, 0f);
            sprintTrail.minVertexDistance = 0.1f;
            sprintTrail.enabled = false;
        }
    }

    // ========================================
    // SPRINT FEEDBACK
    // ========================================

    private void OnSprintChanged(bool isSprinting)
    {
        if (isSprinting)
        {
            if (sprintTrail != null)
            {
                sprintTrail.Clear();
                sprintTrail.enabled = true;
            }
            wasSprinting = true;
        }
        else
        {
            wasSprinting = false;
            // Trail will fade out naturally
            StartCoroutine(DisableTrailAfterFade());
        }
    }

    private void UpdateSprintVisual()
    {
        if (capsuleTransform == null) return;

        if (wasSprinting)
        {
            // Tilt forward
            float tilt = Mathf.Lerp(capsuleTransform.localRotation.eulerAngles.x > 180 ?
                capsuleTransform.localRotation.eulerAngles.x - 360 : capsuleTransform.localRotation.eulerAngles.x,
                sprintTiltAngle, Time.deltaTime * 8f);
            capsuleTransform.localRotation = Quaternion.Euler(tilt, 0f, 0f);

            // Stretch vertically
            Vector3 targetScale = new Vector3(
                originalCapsuleScale.x * 0.9f,
                originalCapsuleScale.y * sprintStretchAmount,
                originalCapsuleScale.z * 0.9f
            );
            capsuleTransform.localScale = Vector3.Lerp(capsuleTransform.localScale, targetScale, Time.deltaTime * 8f);

            // Tint
            SetCapsuleColor(Color.Lerp(originalColor, sprintTintColor, 0.3f));
        }
        else
        {
            // Return to normal
            capsuleTransform.localRotation = Quaternion.Slerp(capsuleTransform.localRotation, Quaternion.identity, Time.deltaTime * 10f);
            capsuleTransform.localScale = Vector3.Lerp(capsuleTransform.localScale, originalCapsuleScale, Time.deltaTime * 10f);

            if (!wasDodging)
                SetCapsuleColor(Color.Lerp(GetCurrentCapsuleColor(), originalColor, Time.deltaTime * 5f));
        }
    }

    private IEnumerator DisableTrailAfterFade()
    {
        yield return new WaitForSeconds(0.4f);
        if (!wasSprinting && sprintTrail != null)
            sprintTrail.enabled = false;
    }

    // ========================================
    // DODGE FEEDBACK
    // ========================================

    private void OnDodgeStart()
    {
        wasDodging = true;
        SetCapsuleColor(dodgeColor);

        if (capsuleTransform != null)
        {
            // Flatten during dodge
            capsuleTransform.localScale = new Vector3(
                originalCapsuleScale.x * 1.3f,
                originalCapsuleScale.y * 0.5f,
                originalCapsuleScale.z * 1.3f
            );
        }

        // Enable trail during dodge
        if (sprintTrail != null)
        {
            sprintTrail.Clear();
            sprintTrail.enabled = true;
            sprintTrail.startColor = new Color(dodgeColor.r, dodgeColor.g, dodgeColor.b, 0.8f);
            sprintTrail.endColor = new Color(dodgeColor.r, dodgeColor.g, dodgeColor.b, 0f);
        }
    }

    private void OnDodgeEnd()
    {
        wasDodging = false;
        SetCapsuleColor(originalColor);

        if (capsuleTransform != null)
        {
            capsuleTransform.localScale = originalCapsuleScale;
        }

        // Reset trail colors
        if (sprintTrail != null)
        {
            sprintTrail.startColor = new Color(sprintTintColor.r, sprintTintColor.g, sprintTintColor.b, 0.6f);
            sprintTrail.endColor = new Color(sprintTintColor.r, sprintTintColor.g, sprintTintColor.b, 0f);
            StartCoroutine(DisableTrailAfterFade());
        }
    }

    // ========================================
    // UTILITY
    // ========================================

    private void SetCapsuleColor(Color color)
    {
        if (capsuleRenderer == null) return;
        propBlock.SetColor(ColorID, color);
        capsuleRenderer.SetPropertyBlock(propBlock);
    }

    private Color GetCurrentCapsuleColor()
    {
        if (capsuleRenderer == null) return originalColor;
        capsuleRenderer.GetPropertyBlock(propBlock);
        return propBlock.GetColor(ColorID);
    }
}

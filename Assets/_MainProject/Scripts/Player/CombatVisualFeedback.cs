using System.Collections;
using UnityEngine;

/// <summary>
/// Visual feedback for the player capsule:
/// - Attack: Sword swing arc (line renderer) + forward lunge + capsule flash
/// - Block: Shield disc appears in front + capsule turns blue tint
/// - Sprint: Trail renderer + capsule squash/stretch + tilt forward
/// </summary>
public class CombatVisualFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatController combatController;
    [SerializeField] private BlockSystem blockSystem;
    [SerializeField] private SprintController sprintController;
    [SerializeField] private DodgeRoll dodgeRoll;
    [SerializeField] private Renderer capsuleRenderer;
    [SerializeField] private Transform capsuleTransform;

    [Header("Sword Visual")]
    [SerializeField] private Transform swordPivot;
    [SerializeField] private LineRenderer swordTrail;
    [SerializeField] private float swordLength = 1.5f;
    [SerializeField] private float swingDuration = 0.25f;
    [SerializeField] private float swingArc = 120f;
    [SerializeField] private Color swordColor = new Color(1f, 0.9f, 0.4f, 1f);
    [SerializeField] private Color swordTrailColor = new Color(1f, 0.6f, 0.1f, 0.6f);

    [Header("Attack Feedback")]
    [SerializeField] private Color attackFlashColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private float attackLungeDistance = 0.3f;
    [SerializeField] private float attackSquashAmount = 0.85f;

    [Header("Block Visual")]
    [SerializeField] private GameObject shieldVisual;
    [SerializeField] private Color blockTintColor = new Color(0.3f, 0.6f, 1f, 1f);
    [SerializeField] private Color perfectBlockFlashColor = new Color(1f, 0.95f, 0.3f, 1f);
    [SerializeField] private float shieldPulseSpeed = 3f;

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
    private Vector3 originalCapsuleLocalPos;
    private static readonly int ColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private bool isSwinging;
    private Coroutine swingCoroutine;
    private Coroutine flashCoroutine;
    private Coroutine blockPulseCoroutine;

    private bool wasBlocking;
    private bool wasSprinting;
    private bool wasDodging;

    private void Awake()
    {
        if (combatController == null) combatController = GetComponent<CombatController>();
        if (blockSystem == null) blockSystem = GetComponent<BlockSystem>();
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
            originalCapsuleLocalPos = capsuleTransform.localPosition;
        }

        // Create visuals if not assigned
        SetupSwordVisual();
        SetupShieldVisual();
        SetupSprintTrail();
    }

    private void OnEnable()
    {
        if (combatController != null)
        {
            combatController.OnComboHit += OnAttackSwing;
            combatController.OnComboReset += OnComboReset;
        }
        if (blockSystem != null)
        {
            blockSystem.OnBlockStart += OnBlockStart;
            blockSystem.OnBlockEnd += OnBlockEnd;
            blockSystem.OnPerfectBlock += OnPerfectBlock;
            blockSystem.OnBlockBroken += OnBlockBroken;
        }
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
        if (combatController != null)
        {
            combatController.OnComboHit -= OnAttackSwing;
            combatController.OnComboReset -= OnComboReset;
        }
        if (blockSystem != null)
        {
            blockSystem.OnBlockStart -= OnBlockStart;
            blockSystem.OnBlockEnd -= OnBlockEnd;
            blockSystem.OnPerfectBlock -= OnPerfectBlock;
            blockSystem.OnBlockBroken -= OnBlockBroken;
        }
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
        UpdateBlockPulse();
        UpdateSprintVisual();
    }

    // ========================================
    // SETUP — Create visual elements at runtime
    // ========================================

    private void SetupSwordVisual()
    {
        if (swordPivot == null)
        {
            GameObject pivotObj = new GameObject("SwordPivot");
            pivotObj.transform.SetParent(transform);
            pivotObj.transform.localPosition = new Vector3(0.3f, 1.2f, 0f);
            pivotObj.transform.localRotation = Quaternion.identity;
            swordPivot = pivotObj.transform;
        }

        if (swordTrail == null)
        {
            swordTrail = swordPivot.gameObject.AddComponent<LineRenderer>();
            swordTrail.useWorldSpace = false;
            swordTrail.positionCount = 2;
            swordTrail.SetPosition(0, Vector3.zero);
            swordTrail.SetPosition(1, Vector3.forward * swordLength);
            swordTrail.startWidth = 0.08f;
            swordTrail.endWidth = 0.02f;
            swordTrail.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            swordTrail.material.SetColor(ColorID, swordColor);
            swordTrail.startColor = swordColor;
            swordTrail.endColor = new Color(swordColor.r, swordColor.g, swordColor.b, 0.2f);
            swordTrail.enabled = false;
        }
    }

    private void SetupShieldVisual()
    {
        if (shieldVisual == null)
        {
            shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shieldVisual.name = "ShieldVisual";
            shieldVisual.transform.SetParent(transform);
            shieldVisual.transform.localPosition = new Vector3(0f, 1f, 0.7f);
            shieldVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shieldVisual.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);

            // Remove collider
            Collider col = shieldVisual.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Set material
            Renderer rend = shieldVisual.GetComponent<Renderer>();
            Material shieldMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            shieldMat.SetColor(ColorID, new Color(blockTintColor.r, blockTintColor.g, blockTintColor.b, 0.4f));
            shieldMat.SetFloat("_Surface", 1); // Transparent
            shieldMat.SetFloat("_Blend", 0);
            shieldMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            shieldMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            shieldMat.SetInt("_ZWrite", 0);
            shieldMat.renderQueue = 3000;
            rend.material = shieldMat;

            shieldVisual.SetActive(false);
        }
    }

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
    // ATTACK FEEDBACK
    // ========================================

    private void OnAttackSwing(int comboHit)
    {
        if (swingCoroutine != null)
            StopCoroutine(swingCoroutine);
        swingCoroutine = StartCoroutine(SwordSwingCoroutine(comboHit));
    }

    private void OnComboReset()
    {
        if (swordTrail != null)
            swordTrail.enabled = false;
    }

    private IEnumerator SwordSwingCoroutine(int comboHit)
    {
        isSwinging = true;
        swordTrail.enabled = true;

        // Determine swing direction based on combo hit
        float startAngle, endAngle;
        switch (comboHit)
        {
            case 1:
                startAngle = -swingArc * 0.5f;
                endAngle = swingArc * 0.5f;
                break;
            case 2:
                startAngle = swingArc * 0.5f;
                endAngle = -swingArc * 0.5f;
                break;
            case 3: // Overhead slam
                startAngle = swingArc * 0.4f;
                endAngle = -swingArc * 0.6f;
                swordTrail.startWidth = 0.12f;
                break;
            default:
                startAngle = -swingArc * 0.5f;
                endAngle = swingArc * 0.5f;
                break;
        }

        // Color intensity based on combo
        Color swingColor = comboHit == 3
            ? new Color(1f, 0.4f, 0.1f, 1f) // Orange for 3rd hit
            : swordColor;
        swordTrail.startColor = swingColor;
        swordTrail.endColor = new Color(swingColor.r, swingColor.g, swingColor.b, 0.2f);

        // Attack flash on capsule
        FlashCapsule(attackFlashColor, swingDuration * 0.8f);

        // Forward lunge
        float lungeAmount = comboHit == 3 ? attackLungeDistance * 1.5f : attackLungeDistance;

        // Squash the capsule briefly
        if (capsuleTransform != null)
        {
            Vector3 squashScale = new Vector3(
                originalCapsuleScale.x * (1f / attackSquashAmount),
                originalCapsuleScale.y * attackSquashAmount,
                originalCapsuleScale.z * (1f / attackSquashAmount)
            );
            capsuleTransform.localScale = squashScale;
        }

        float elapsed = 0f;
        float duration = swingDuration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Swing the sword arc
            float currentAngle = Mathf.Lerp(startAngle, endAngle, EaseOutQuad(t));
            swordPivot.localRotation = Quaternion.Euler(0f, currentAngle, 0f);

            // Update line renderer positions
            Vector3 swordEnd = Vector3.forward * swordLength;
            swordTrail.SetPosition(0, Vector3.zero);
            swordTrail.SetPosition(1, swordEnd);

            // Lunge forward and back
            if (capsuleTransform != null)
            {
                float lungeCurve = Mathf.Sin(t * Mathf.PI); // 0 -> 1 -> 0
                capsuleTransform.localPosition = originalCapsuleLocalPos + transform.InverseTransformDirection(transform.forward) * lungeAmount * lungeCurve;

                // Return scale to normal
                capsuleTransform.localScale = Vector3.Lerp(
                    new Vector3(originalCapsuleScale.x * (1f / attackSquashAmount), originalCapsuleScale.y * attackSquashAmount, originalCapsuleScale.z * (1f / attackSquashAmount)),
                    originalCapsuleScale,
                    EaseOutQuad(t)
                );
            }

            // Fade trail alpha
            float alpha = 1f - t;
            swordTrail.startColor = new Color(swingColor.r, swingColor.g, swingColor.b, alpha);

            yield return null;
        }

        // Reset
        swordTrail.startWidth = 0.08f;
        swordTrail.enabled = false;
        if (capsuleTransform != null)
        {
            capsuleTransform.localScale = originalCapsuleScale;
            capsuleTransform.localPosition = originalCapsuleLocalPos;
        }

        isSwinging = false;
        swingCoroutine = null;
    }

    // ========================================
    // BLOCK FEEDBACK
    // ========================================

    private void OnBlockStart()
    {
        if (shieldVisual != null)
            shieldVisual.SetActive(true);

        SetCapsuleColor(blockTintColor);
        wasBlocking = true;

        if (blockPulseCoroutine != null)
            StopCoroutine(blockPulseCoroutine);
        blockPulseCoroutine = StartCoroutine(BlockPulseCoroutine());
    }

    private void OnBlockEnd()
    {
        if (shieldVisual != null)
            shieldVisual.SetActive(false);

        SetCapsuleColor(originalColor);
        wasBlocking = false;

        if (blockPulseCoroutine != null)
        {
            StopCoroutine(blockPulseCoroutine);
            blockPulseCoroutine = null;
        }
    }

    private void OnPerfectBlock()
    {
        FlashCapsule(perfectBlockFlashColor, 0.3f);

        // Flash shield bright
        if (shieldVisual != null)
        {
            StartCoroutine(ShieldFlashCoroutine(perfectBlockFlashColor, 0.3f));
        }
    }

    private void OnBlockBroken()
    {
        FlashCapsule(Color.red, 0.4f);
        OnBlockEnd();

        // Shake the capsule
        if (capsuleTransform != null)
            StartCoroutine(ShakeCoroutine(0.3f, 0.15f));
    }

    private IEnumerator BlockPulseCoroutine()
    {
        while (wasBlocking && shieldVisual != null && shieldVisual.activeSelf)
        {
            float pulse = (Mathf.Sin(Time.time * shieldPulseSpeed) + 1f) * 0.5f;
            float scale = 1.1f + pulse * 0.15f;
            shieldVisual.transform.localScale = new Vector3(scale, 0.05f, scale);
            yield return null;
        }
    }

    private IEnumerator ShieldFlashCoroutine(Color flashColor, float duration)
    {
        Renderer rend = shieldVisual.GetComponent<Renderer>();
        if (rend == null) yield break;

        Color originalShieldColor = new Color(blockTintColor.r, blockTintColor.g, blockTintColor.b, 0.4f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            Color c = Color.Lerp(flashColor, originalShieldColor, t);
            rend.material.SetColor(ColorID, c);
            yield return null;
        }

        rend.material.SetColor(ColorID, originalShieldColor);
    }

    private void UpdateBlockPulse()
    {
        // Shield always faces forward relative to player
        if (shieldVisual != null && shieldVisual.activeSelf)
        {
            shieldVisual.transform.localPosition = new Vector3(0f, 1f, 0.7f);
            shieldVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
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

        if (wasSprinting && !isSwinging)
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
        else if (!wasBlocking && !isSwinging)
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

    private void FlashCapsule(Color color, float duration)
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashCoroutine(color, duration));
    }

    private IEnumerator FlashCoroutine(Color color, float duration)
    {
        SetCapsuleColor(color);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            Color target = wasBlocking ? blockTintColor : originalColor;
            SetCapsuleColor(Color.Lerp(color, target, EaseOutQuad(t)));
            yield return null;
        }

        Color finalColor = wasBlocking ? blockTintColor : originalColor;
        SetCapsuleColor(finalColor);
        flashCoroutine = null;
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        Vector3 originalPos = capsuleTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float dampedMagnitude = magnitude * (1f - t);

            Vector3 offset = new Vector3(
                Random.Range(-1f, 1f) * dampedMagnitude,
                0f,
                Random.Range(-1f, 1f) * dampedMagnitude
            );

            capsuleTransform.localPosition = originalPos + offset;
            yield return null;
        }

        capsuleTransform.localPosition = originalPos;
    }

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

    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
}

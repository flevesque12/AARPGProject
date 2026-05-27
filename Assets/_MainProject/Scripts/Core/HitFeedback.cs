using UnityEngine;
using System.Collections;

/// <summary>
/// Feedback visuel quand l'entité prend des dégâts.
/// Flash lissé (lerp) + scale punch + chiffre de dégât flottant.
/// Utilise unscaledDeltaTime pour fonctionner correctement pendant le hit stop.
/// </summary>
public class HitFeedback : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private Renderer modelRenderer;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.2f;

    [Header("Scale Punch")]
    [SerializeField] private float punchScale = 1.2f;

    [Header("Death")]
    [SerializeField] private Color deathColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private float deathShrinkDuration = 0.5f;

    private HealthSystem health;
    private MaterialPropertyBlock propBlock;
    private Color originalColor;
    private Vector3 originalScale;
    private Coroutine flashCoroutine;
    private static readonly int ColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorIDLegacy = Shader.PropertyToID("_Color");

    private void Awake()
    {
        health = GetComponent<HealthSystem>();
        propBlock = new MaterialPropertyBlock();

        if (modelRenderer == null)
            modelRenderer = GetComponentInChildren<Renderer>();

        if (modelRenderer != null)
        {
            modelRenderer.GetPropertyBlock(propBlock);
            originalColor = modelRenderer.sharedMaterial.HasProperty(ColorID)
                ? modelRenderer.sharedMaterial.GetColor(ColorID)
                : modelRenderer.sharedMaterial.GetColor(ColorIDLegacy);
        }

        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= OnDamaged;
            health.OnDeath -= OnDeath;
        }
    }

    private void OnDamaged(float damage)
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashAndPunch());

        DamageNumber.Spawn(transform.position + Vector3.up * 1.2f, damage);
    }

    private IEnumerator FlashAndPunch()
    {
        Vector3 punchScaleVec = originalScale * punchScale;

        // --- Flash in + scale punch (rapide : 25% de la durée) ---
        float flashInTime = flashDuration * 0.25f;
        float elapsed = 0f;
        while (elapsed < flashInTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / flashInTime;
            SetColor(Color.Lerp(originalColor, flashColor, t));
            transform.localScale = Vector3.Lerp(originalScale, punchScaleVec, t);
            yield return null;
        }

        // --- Flash out + retour à l'échelle (75% de la durée) ---
        float flashOutTime = flashDuration * 0.75f;
        elapsed = 0f;
        while (elapsed < flashOutTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / flashOutTime;
            SetColor(Color.Lerp(flashColor, originalColor, t));
            transform.localScale = Vector3.Lerp(punchScaleVec, originalScale, t);
            yield return null;
        }

        SetColor(originalColor);
        transform.localScale = originalScale;
        flashCoroutine = null;
    }

    private void OnDeath()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        StartCoroutine(DeathFeedback());
    }

    private IEnumerator DeathFeedback()
    {
        SetColor(deathColor);

        float elapsed = 0f;
        while (elapsed < deathShrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / deathShrinkDuration;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        transform.localScale = Vector3.zero;
    }

    private void SetColor(Color color)
    {
        if (modelRenderer == null) return;
        propBlock.SetColor(ColorID, color);
        propBlock.SetColor(ColorIDLegacy, color);
        modelRenderer.SetPropertyBlock(propBlock);
    }

    public void ResetFeedback()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        SetColor(originalColor);
        transform.localScale = originalScale;
    }
}

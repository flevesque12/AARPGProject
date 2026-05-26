using UnityEngine;
using System.Collections;

/// <summary>
/// Feedback visuel quand l'entité prend des dégâts.
/// Flash blanc + léger scale punch.
/// 
/// SETUP:
///   1. Ajouter sur le même GameObject que HealthSystem
///   2. Assigner le Renderer du modèle 3D (MeshRenderer ou SkinnedMeshRenderer)
///   3. Le matériau DOIT utiliser un shader qui supporte "_Color" ou "_BaseColor"
/// </summary>
public class HitFeedback : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private Renderer modelRenderer;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;

    [Header("Scale Punch")]
    [SerializeField] private float punchScale = 1.2f;
    [SerializeField] private float punchDuration = 0.1f;

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

        // Trouver le renderer automatiquement si non assigné
        if (modelRenderer == null)
            modelRenderer = GetComponentInChildren<Renderer>();

        if (modelRenderer != null)
        {
            modelRenderer.GetPropertyBlock(propBlock);
            // Essayer les deux noms de propriété de couleur
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
    }

    private IEnumerator FlashAndPunch()
    {
        // Flash blanc
        SetColor(flashColor);
        transform.localScale = originalScale * punchScale;

        yield return new WaitForSeconds(flashDuration);

        // Retour à la normale
        SetColor(originalColor);
        transform.localScale = originalScale;
    }

    private void OnDeath()
    {
        StartCoroutine(DeathFeedback());
    }

    private IEnumerator DeathFeedback()
    {
        // Devenir gris et rétrécir
        SetColor(deathColor);

        float elapsed = 0;
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

    /// <summary>
    /// Reset le feedback (pour respawn).
    /// </summary>
    public void ResetFeedback()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        SetColor(originalColor);
        transform.localScale = originalScale;
    }
}

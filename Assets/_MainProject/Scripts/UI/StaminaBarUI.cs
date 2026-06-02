using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barre d'endurance visuelle dans le HUD du joueur.
/// Change de couleur selon le niveau (vert → jaune → rouge).
/// Flash quand l'endurance est insuffisante.
/// </summary>
public class StaminaBarUI : MonoBehaviour
{
    [Header("Références UI")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Couleurs")]
    [SerializeField] private Color fullColor = new Color(0.2f, 0.8f, 0.3f);    // Vert
    [SerializeField] private Color midColor = new Color(0.9f, 0.8f, 0.1f);     // Jaune
    [SerializeField] private Color lowColor = new Color(0.9f, 0.2f, 0.15f);    // Rouge
    [SerializeField] private float lowThreshold = 0.25f;
    [SerializeField] private float midThreshold = 0.5f;

    [Header("Animation")]
    [SerializeField] private float smoothSpeed = 8f;             // Vitesse de transition de la barre
    [SerializeField] private float flashDuration = 0.3f;         // Durée du flash "pas assez d'endurance"
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float hideDelay = 3f;               // Masquer la barre après X sec à plein
    [SerializeField] private float fadeSpeed = 4f;

    [Header("Références")]
    [SerializeField] private StaminaSystem staminaSystem;

    private float targetFill;
    private float currentFill;
    private float hideTimer;
    private bool shouldShow;
    private float flashTimer;

    private void Start()
    {
        if (staminaSystem == null)
            staminaSystem = FindAnyObjectByType<StaminaSystem>();

        if (staminaSystem != null)
        {
            staminaSystem.OnStaminaChanged += OnStaminaChanged;
            staminaSystem.OnStaminaEmpty += OnStaminaEmpty;
            staminaSystem.OnStaminaFull += OnStaminaFull;

            // Initialiser
            targetFill = staminaSystem.StaminaRatio;
            currentFill = targetFill;
            UpdateVisuals();
        }
    }

    private void OnDestroy()
    {
        if (staminaSystem != null)
        {
            staminaSystem.OnStaminaChanged -= OnStaminaChanged;
            staminaSystem.OnStaminaEmpty -= OnStaminaEmpty;
            staminaSystem.OnStaminaFull -= OnStaminaFull;
        }
    }

    private void Update()
    {
        // Smooth la barre
        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * smoothSpeed);
        if (fillImage != null)
            fillImage.fillAmount = currentFill;

        // Couleur dynamique
        UpdateBarColor();

        // Flash
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            float flashAlpha = Mathf.PingPong(flashTimer * 10f, 1f);
            if (fillImage != null)
                fillImage.color = Color.Lerp(GetBarColor(), flashColor, flashAlpha);
        }

        // Afficher/masquer la barre
        if (shouldShow)
        {
            hideTimer = hideDelay;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
        }
        else
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f && canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
        }
    }

    private void OnStaminaChanged(float current, float max)
    {
        targetFill = current / max;
        shouldShow = true;

        // Masquer après un moment si plein
        if (current >= max)
            shouldShow = false;
    }

    private void OnStaminaEmpty()
    {
        // Flash rouge quand vide
        flashTimer = flashDuration;
        shouldShow = true;
    }

    private void OnStaminaFull()
    {
        shouldShow = false;
    }

    private void UpdateBarColor()
    {
        if (fillImage == null || flashTimer > 0f) return;
        fillImage.color = GetBarColor();
    }

    private Color GetBarColor()
    {
        if (currentFill <= lowThreshold)
            return lowColor;
        if (currentFill <= midThreshold)
            return Color.Lerp(lowColor, midColor, (currentFill - lowThreshold) / (midThreshold - lowThreshold));
        return Color.Lerp(midColor, fullColor, (currentFill - midThreshold) / (1f - midThreshold));
    }

    private void UpdateVisuals()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = currentFill;
            fillImage.color = GetBarColor();
        }
    }
}

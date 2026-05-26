using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD du joueur - Barre de vie en overlay.
/// Se crée automatiquement au runtime (pas besoin de setup Canvas manuel).
/// 
/// SETUP: Ajouter sur le même GameObject que le joueur (avec HealthSystem).
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] private float barWidth = 300f;
    [SerializeField] private float barHeight = 25f;
    [SerializeField] private float xOffset = 20f;
    [SerializeField] private float yOffset = 20f;

    [Header("Couleurs")]
    [SerializeField] private Color healthColor = new Color(0.8f, 0.15f, 0.15f);
    [SerializeField] private Color bgColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
    [SerializeField] private Color borderColor = new Color(0.3f, 0.3f, 0.3f);

    private HealthSystem health;
    private Image healthFillImage;
    private Text healthText;

    private void Start()
    {
        health = GetComponent<HealthSystem>();
        CreateHUD();

        if (health != null)
        {
            health.OnHealthChanged += UpdateHealthBar;
            UpdateHealthBar(health.CurrentHealth, health.MaxHealth);
        }
    }

    private void CreateHUD()
    {
        // Créer le Canvas
        GameObject canvasObj = new GameObject("PlayerHUD_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Container
        GameObject container = new GameObject("HealthBarContainer");
        container.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1); // Top-left
        containerRect.anchorMax = new Vector2(0, 1);
        containerRect.pivot = new Vector2(0, 1);
        containerRect.anchoredPosition = new Vector2(xOffset, -yOffset);
        containerRect.sizeDelta = new Vector2(barWidth + 4, barHeight + 4);

        // Border
        Image borderImg = container.AddComponent<Image>();
        borderImg.color = borderColor;

        // Background
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(container.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(2, 2);
        bgRect.offsetMax = new Vector2(-2, -2);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = bgColor;

        // Health Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(bgObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.pivot = new Vector2(0, 0.5f);
        healthFillImage = fillObj.AddComponent<Image>();
        healthFillImage.color = healthColor;

        // Texte HP
        GameObject textObj = new GameObject("HPText");
        textObj.transform.SetParent(container.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        healthText = textObj.AddComponent<Text>();
        healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthText.alignment = TextAnchor.MiddleCenter;
        healthText.fontSize = 14;
        healthText.color = Color.white;
        healthText.fontStyle = FontStyle.Bold;
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (healthFillImage != null)
        {
            float percent = current / max;
            healthFillImage.fillAmount = percent;
            healthFillImage.rectTransform.anchorMax = new Vector2(percent, 1);
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnHealthChanged -= UpdateHealthBar;
    }
}

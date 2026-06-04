using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD du joueur — Barre de vie, barre d'endurance, bindings d'actions.
/// Se crée automatiquement au runtime (pas besoin de setup Canvas manuel).
///
/// SETUP: Ajouter sur le même GameObject que le joueur (HealthSystem + StaminaSystem).
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Barres — Position")]
    [SerializeField] private float barWidth = 300f;
    [SerializeField] private float barHeight = 25f;
    [SerializeField] private float staminaBarHeight = 18f;
    [SerializeField] private float xOffset = 20f;
    [SerializeField] private float yOffset = 20f;
    [SerializeField] private float barGap = 5f;

    [Header("Couleurs HP")]
    [SerializeField] private Color healthColor = new Color(0.8f, 0.15f, 0.15f);
    [SerializeField] private Color bgColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
    [SerializeField] private Color borderColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Couleurs Stamina")]
    [SerializeField] private Color staminaFullColor = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color staminaMidColor = new Color(0.9f, 0.8f, 0.1f);
    [SerializeField] private Color staminaLowColor = new Color(0.9f, 0.2f, 0.15f);
    [SerializeField] private Color staminaFlashColor = Color.red;
    [SerializeField] private float lowThreshold = 0.25f;
    [SerializeField] private float midThreshold = 0.5f;

    [Header("Animation Stamina")]
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private float hideDelay = 3f;
    [SerializeField] private float fadeSpeed = 4f;

    [Header("Boutons d'action")]
    [SerializeField] private float actionBarXOffset = 20f;
    [SerializeField] private float actionBarYOffset = 20f;
    [SerializeField] private float keyBoxWidth = 68f;
    [SerializeField] private float actionRowHeight = 26f;
    [SerializeField] private float actionRowGap = 4f;

    // (touche, nom de l'action) — du bas vers le haut
    private static readonly (string key, string action)[] ActionBindings =
    {
        ("LMB",    "Attaque"),
        ("Espace", "Esquive"),
        ("RMB",    "Bloc"),
        ("Shift",  "Sprint"),
    };

    private HealthSystem health;
    private StaminaSystem stamina;
    private RiposteSystem riposte;

    private Image healthFillImage;
    private Text healthText;

    private Image staminaFillImage;
    private CanvasGroup staminaCanvasGroup;
    private float staminaTargetFill = 1f;
    private float staminaCurrentFill = 1f;
    private float hideTimer;
    private bool staminaShouldShow;
    private float flashTimer;

    private Text riposteLabel;
    private CanvasGroup riposteCanvasGroup;

    private void Start()
    {
        health = GetComponent<HealthSystem>();
        stamina = GetComponent<StaminaSystem>();
        riposte = GetComponent<RiposteSystem>();
        CreateHUD();

        if (health != null)
        {
            health.OnHealthChanged += UpdateHealthBar;
            UpdateHealthBar(health.CurrentHealth, health.MaxHealth);
        }

        if (stamina != null)
        {
            stamina.OnStaminaChanged += OnStaminaChanged;
            stamina.OnStaminaEmpty += OnStaminaEmpty;
            stamina.OnStaminaFull += OnStaminaFull;
            staminaTargetFill = stamina.StaminaRatio;
            staminaCurrentFill = staminaTargetFill;
        }

        if (riposte != null)
        {
            riposte.OnRiposteWindowOpen += OnRiposteWindowOpen;
            riposte.OnRiposteWindowClose += OnRiposteWindowClose;
        }
    }

    private void Update()
    {
        staminaCurrentFill = Mathf.Lerp(staminaCurrentFill, staminaTargetFill, Time.deltaTime * smoothSpeed);

        if (staminaFillImage != null)
            staminaFillImage.rectTransform.anchorMax = new Vector2(staminaCurrentFill, 1f);

        UpdateStaminaColor();

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            float t = Mathf.PingPong(flashTimer * 10f, 1f);
            if (staminaFillImage != null)
                staminaFillImage.color = Color.Lerp(GetStaminaColor(), staminaFlashColor, t);
        }

        if (staminaCanvasGroup != null)
        {
            if (staminaShouldShow)
            {
                hideTimer = hideDelay;
                staminaCanvasGroup.alpha = Mathf.Lerp(staminaCanvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
            }
            else
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0f)
                    staminaCanvasGroup.alpha = Mathf.Lerp(staminaCanvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
            }
        }

        if (riposteCanvasGroup != null)
        {
            float t = Mathf.PingPong(Time.unscaledTime * 4f, 1f);
            riposteCanvasGroup.alpha = Mathf.Lerp(riposteCanvasGroup.alpha,
                riposteCanvasGroup.alpha > 0.05f ? 0.6f + t * 0.4f : 0f,
                Time.deltaTime * 12f);
        }
    }

    // ============================================================
    // CRÉATION DU HUD
    // ============================================================

    private void CreateHUD()
    {
        GameObject canvasObj = new GameObject("PlayerHUD_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        CreateHealthBar(canvasObj);
        CreateStaminaBar(canvasObj);
        CreateActionBar(canvasObj);
        CreateRiposteIndicator(canvasObj);
    }

    private void CreateHealthBar(GameObject canvasObj)
    {
        GameObject container = new GameObject("HealthBarContainer");
        container.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(0, 1);
        containerRect.pivot = new Vector2(0, 1);
        containerRect.anchoredPosition = new Vector2(xOffset, -yOffset);
        containerRect.sizeDelta = new Vector2(barWidth + 4, barHeight + 4);
        container.AddComponent<Image>().color = borderColor;

        GameObject bgObj = CreateBarBackground(container);

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

    private void CreateStaminaBar(GameObject canvasObj)
    {
        float staminaY = -yOffset - (barHeight + 4) - barGap;

        GameObject container = new GameObject("StaminaBarContainer");
        container.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(0, 1);
        containerRect.pivot = new Vector2(0, 1);
        containerRect.anchoredPosition = new Vector2(xOffset, staminaY);
        containerRect.sizeDelta = new Vector2(barWidth + 4, staminaBarHeight + 4);
        staminaCanvasGroup = container.AddComponent<CanvasGroup>();
        container.AddComponent<Image>().color = borderColor;

        GameObject bgObj = CreateBarBackground(container);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(bgObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.pivot = new Vector2(0, 0.5f);
        staminaFillImage = fillObj.AddComponent<Image>();
        staminaFillImage.color = staminaFullColor;
    }

    private void CreateActionBar(GameObject canvasObj)
    {
        for (int i = 0; i < ActionBindings.Length; i++)
        {
            float rowY = actionBarYOffset + i * (actionRowHeight + actionRowGap);
            CreateActionEntry(canvasObj, ActionBindings[i].key, ActionBindings[i].action, rowY);
        }
    }

    private void CreateActionEntry(GameObject canvasObj, string keyLabel, string actionLabel, float yFromBottom)
    {
        Color keyBgColor = new Color(0.08f, 0.08f, 0.08f, 0.88f);
        Color keyBorderCol = new Color(0.45f, 0.45f, 0.45f, 1f);

        // Boîte de la touche
        GameObject keyBox = new GameObject($"Key_{keyLabel}");
        keyBox.transform.SetParent(canvasObj.transform, false);
        RectTransform keyRect = keyBox.AddComponent<RectTransform>();
        keyRect.anchorMin = Vector2.zero;
        keyRect.anchorMax = Vector2.zero;
        keyRect.pivot = Vector2.zero;
        keyRect.anchoredPosition = new Vector2(actionBarXOffset, yFromBottom);
        keyRect.sizeDelta = new Vector2(keyBoxWidth, actionRowHeight);
        keyBox.AddComponent<Image>().color = keyBorderCol;

        // Fond de la touche
        GameObject keyBg = new GameObject("BG");
        keyBg.transform.SetParent(keyBox.transform, false);
        RectTransform keyBgRect = keyBg.AddComponent<RectTransform>();
        keyBgRect.anchorMin = Vector2.zero;
        keyBgRect.anchorMax = Vector2.one;
        keyBgRect.offsetMin = new Vector2(1, 1);
        keyBgRect.offsetMax = new Vector2(-1, -1);
        keyBg.AddComponent<Image>().color = keyBgColor;

        // Texte de la touche
        GameObject keyTextObj = new GameObject("Text");
        keyTextObj.transform.SetParent(keyBox.transform, false);
        RectTransform keyTextRect = keyTextObj.AddComponent<RectTransform>();
        keyTextRect.anchorMin = Vector2.zero;
        keyTextRect.anchorMax = Vector2.one;
        keyTextRect.offsetMin = Vector2.zero;
        keyTextRect.offsetMax = Vector2.zero;
        Text keyText = keyTextObj.AddComponent<Text>();
        keyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        keyText.text = keyLabel;
        keyText.alignment = TextAnchor.MiddleCenter;
        keyText.fontSize = 11;
        keyText.fontStyle = FontStyle.Bold;
        keyText.color = new Color(0.95f, 0.95f, 0.95f);
        keyTextObj.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);

        // Label de l'action (à droite de la boîte)
        GameObject labelObj = new GameObject($"Label_{actionLabel}");
        labelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.zero;
        labelRect.pivot = Vector2.zero;
        labelRect.anchoredPosition = new Vector2(actionBarXOffset + keyBoxWidth + 6f, yFromBottom);
        labelRect.sizeDelta = new Vector2(90f, actionRowHeight);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.text = actionLabel;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.fontSize = 13;
        labelText.color = new Color(0.9f, 0.9f, 0.9f, 0.85f);
        labelObj.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
    }

    private void CreateRiposteIndicator(GameObject canvasObj)
    {
        float totalActionHeight = ActionBindings.Length * (actionRowHeight + actionRowGap);

        GameObject container = new GameObject("RiposteIndicator");
        container.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(actionBarXOffset, actionBarYOffset + totalActionHeight + 8f);
        rect.sizeDelta = new Vector2(160f, 28f);
        container.AddComponent<Image>().color = new Color(0.8f, 0.6f, 0.05f, 0.75f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(container.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        riposteLabel = textObj.AddComponent<Text>();
        riposteLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        riposteLabel.text = "RIPOSTE !";
        riposteLabel.alignment = TextAnchor.MiddleCenter;
        riposteLabel.fontSize = 15;
        riposteLabel.fontStyle = FontStyle.Bold;
        riposteLabel.color = Color.white;
        textObj.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);

        riposteCanvasGroup = container.AddComponent<CanvasGroup>();
        riposteCanvasGroup.alpha = 0f;
        riposteCanvasGroup.interactable = false;
        riposteCanvasGroup.blocksRaycasts = false;
    }

    // ============================================================
    // UTILITAIRES
    // ============================================================

    private GameObject CreateBarBackground(GameObject parent)
    {
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(parent.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(2, 2);
        bgRect.offsetMax = new Vector2(-2, -2);
        bgObj.AddComponent<Image>().color = bgColor;
        return bgObj;
    }

    // ============================================================
    // CALLBACKS
    // ============================================================

    private void UpdateHealthBar(float current, float max)
    {
        if (healthFillImage != null)
            healthFillImage.rectTransform.anchorMax = new Vector2(current / max, 1f);

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void OnStaminaChanged(float current, float max)
    {
        staminaTargetFill = current / max;
        staminaShouldShow = current < max;
    }

    private void OnStaminaEmpty()
    {
        flashTimer = flashDuration;
        staminaShouldShow = true;
    }

    private void OnStaminaFull()
    {
        staminaShouldShow = false;
    }

    private void OnRiposteWindowOpen()
    {
        if (riposteCanvasGroup != null)
            riposteCanvasGroup.alpha = 1f;
    }

    private void OnRiposteWindowClose()
    {
        if (riposteCanvasGroup != null)
            riposteCanvasGroup.alpha = 0f;
    }

    private void UpdateStaminaColor()
    {
        if (staminaFillImage == null || flashTimer > 0f) return;
        staminaFillImage.color = GetStaminaColor();
    }

    private Color GetStaminaColor()
    {
        if (staminaCurrentFill <= lowThreshold)
            return staminaLowColor;
        if (staminaCurrentFill <= midThreshold)
            return Color.Lerp(staminaLowColor, staminaMidColor,
                (staminaCurrentFill - lowThreshold) / (midThreshold - lowThreshold));
        return Color.Lerp(staminaMidColor, staminaFullColor,
            (staminaCurrentFill - midThreshold) / (1f - midThreshold));
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnHealthChanged -= UpdateHealthBar;
        if (stamina != null)
        {
            stamina.OnStaminaChanged -= OnStaminaChanged;
            stamina.OnStaminaEmpty -= OnStaminaEmpty;
            stamina.OnStaminaFull -= OnStaminaFull;
        }
        if (riposte != null)
        {
            riposte.OnRiposteWindowOpen -= OnRiposteWindowOpen;
            riposte.OnRiposteWindowClose -= OnRiposteWindowClose;
        }
    }
}

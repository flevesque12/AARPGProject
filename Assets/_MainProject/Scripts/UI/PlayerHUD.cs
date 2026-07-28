using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD du joueur — Barre de vie, barre de mana, barre de sorts (4 slots).
/// Se crée automatiquement au runtime (pas besoin de setup Canvas manuel).
///
/// SETUP: Ajouter sur le même GameObject que le joueur (HealthSystem + ManaSystem).
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Barres — Position")]
    [SerializeField] private float barWidth = 300f;
    [SerializeField] private float barHeight = 25f;
    [SerializeField] private float manaBarHeight = 18f;
    [SerializeField] private float xOffset = 20f;
    [SerializeField] private float yOffset = 20f;
    [SerializeField] private float barGap = 5f;

    [Header("Couleurs HP")]
    [SerializeField] private Color healthColor = new Color(0.8f, 0.15f, 0.15f);
    [SerializeField] private Color bgColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
    [SerializeField] private Color borderColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Couleurs Mana")]
    [SerializeField] private Color manaColor = new Color(0.25f, 0.55f, 0.95f);

    [Header("Barre de Skills")]
    [SerializeField] private float skillSlotSize = 52f;
    [SerializeField] private float skillSlotGap = 8f;
    [SerializeField] private float skillBarYOffset = 20f;

    private HealthSystem health;
    private ManaSystem mana;
    private SkillCaster skillCaster;

    private Image[] skillCooldownOverlays = new Image[4];
    private Text[] skillNameTexts = new Text[4];

    private Image healthFillImage;
    private Text healthText;

    private Image manaFillImage;
    private Text manaText;

    private void Start()
    {
        health = GetComponent<HealthSystem>();
        mana = GetComponent<ManaSystem>();
        skillCaster = GetComponent<SkillCaster>();
        CreateHUD();

        if (health != null)
        {
            health.OnHealthChanged += UpdateHealthBar;
            UpdateHealthBar(health.CurrentHealth, health.MaxHealth);
        }

        if (mana != null)
        {
            mana.OnManaChanged += UpdateManaBar;
            UpdateManaBar(mana.CurrentMana, mana.MaxMana);
        }

        if (skillCaster != null)
            skillCaster.OnCooldownChanged += OnSkillCooldownChanged;
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
        CreateManaBar(canvasObj);
        CreateSkillBar(canvasObj);
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

    private void CreateManaBar(GameObject canvasObj)
    {
        float manaY = -yOffset - (barHeight + 4) - barGap;

        GameObject container = new GameObject("ManaBarContainer");
        container.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(0, 1);
        containerRect.pivot = new Vector2(0, 1);
        containerRect.anchoredPosition = new Vector2(xOffset, manaY);
        containerRect.sizeDelta = new Vector2(barWidth + 4, manaBarHeight + 4);
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
        manaFillImage = fillObj.AddComponent<Image>();
        manaFillImage.color = manaColor;

        GameObject textObj = new GameObject("ManaText");
        textObj.transform.SetParent(container.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        manaText = textObj.AddComponent<Text>();
        manaText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        manaText.alignment = TextAnchor.MiddleCenter;
        manaText.fontSize = 12;
        manaText.color = Color.white;
        manaText.fontStyle = FontStyle.Bold;
    }

    // ============================================================
    // BARRE DE SKILLS — bas de l'écran, centrée
    // ============================================================

    private void CreateSkillBar(GameObject canvasObj)
    {
        float totalWidth = 4 * skillSlotSize + 3 * skillSlotGap;
        float startX = -totalWidth * 0.5f;

        // Fond de la barre (derrière les slots)
        GameObject barBg = new GameObject("SkillBarBG");
        barBg.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRect = barBg.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0f);
        bgRect.anchorMax = new Vector2(0.5f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.anchoredPosition = new Vector2(0f, skillBarYOffset - 6f);
        bgRect.sizeDelta = new Vector2(totalWidth + 16f, skillSlotSize + 28f + 8f);
        Image bgImg = barBg.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.06f, 0.06f, 0.75f);

        for (int i = 0; i < 4; i++)
        {
            SkillData skill = skillCaster != null ? skillCaster.GetSlot(i) : null;
            float slotX = startX + i * (skillSlotSize + skillSlotGap) + skillSlotSize * 0.5f;
            CreateSkillSlot(canvasObj, i, slotX, skill);
        }
    }

    private void CreateSkillSlot(GameObject canvasObj, int index, float centerX, SkillData skill)
    {
        Color borderCol  = new Color(0.35f, 0.35f, 0.35f, 1f);
        Color slotBgCol  = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        Color schoolTint = skill != null ? new Color(skill.skillColor.r, skill.skillColor.g, skill.skillColor.b, 0.18f)
                                        : new Color(0.2f, 0.2f, 0.2f, 0.15f);
        Color overlayCol = new Color(0f, 0f, 0f, 0.72f);

        // Slot container (ancré au bas-centre de l'écran)
        GameObject slot = new GameObject($"SkillSlot_{index}");
        slot.transform.SetParent(canvasObj.transform, false);
        RectTransform slotRect = slot.AddComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.5f, 0f);
        slotRect.anchorMax = new Vector2(0.5f, 0f);
        slotRect.pivot = new Vector2(0.5f, 0f);
        slotRect.anchoredPosition = new Vector2(centerX, skillBarYOffset);
        slotRect.sizeDelta = new Vector2(skillSlotSize, skillSlotSize);

        // Bordure
        slot.AddComponent<Image>().color = borderCol;

        // Fond sombre
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(slot.transform, false);
        RectTransform bgR = bg.AddComponent<RectTransform>();
        bgR.anchorMin = Vector2.zero; bgR.anchorMax = Vector2.one;
        bgR.offsetMin = new Vector2(1, 1); bgR.offsetMax = new Vector2(-1, -1);
        bg.AddComponent<Image>().color = slotBgCol;

        // Tinte school (couleur subtile en fond)
        GameObject tint = new GameObject("SchoolTint");
        tint.transform.SetParent(slot.transform, false);
        RectTransform tintR = tint.AddComponent<RectTransform>();
        tintR.anchorMin = Vector2.zero; tintR.anchorMax = Vector2.one;
        tintR.offsetMin = new Vector2(1, 1); tintR.offsetMax = new Vector2(-1, -1);
        tint.AddComponent<Image>().color = schoolTint;

        // Overlay cooldown (balaye de haut en bas)
        GameObject overlay = new GameObject("CooldownOverlay");
        overlay.transform.SetParent(slot.transform, false);
        RectTransform overlayR = overlay.AddComponent<RectTransform>();
        overlayR.anchorMin = new Vector2(0f, 1f);   // ancré en haut
        overlayR.anchorMax = new Vector2(1f, 1f);
        overlayR.offsetMin = new Vector2(1, 0);
        overlayR.offsetMax = new Vector2(-1, -1);
        overlayR.sizeDelta = new Vector2(0, 0);      // hauteur 0 au départ
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = overlayCol;
        skillCooldownOverlays[index] = overlayImg;

        // Touche (coin supérieur gauche)
        GameObject keyObj = new GameObject("KeyLabel");
        keyObj.transform.SetParent(slot.transform, false);
        RectTransform keyR = keyObj.AddComponent<RectTransform>();
        keyR.anchorMin = new Vector2(0, 1); keyR.anchorMax = new Vector2(0, 1);
        keyR.pivot = new Vector2(0, 1);
        keyR.anchoredPosition = new Vector2(4, -3);
        keyR.sizeDelta = new Vector2(18, 16);
        Text keyText = keyObj.AddComponent<Text>();
        keyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        keyText.text = (index + 1).ToString();
        keyText.fontSize = 11;
        keyText.fontStyle = FontStyle.Bold;
        keyText.alignment = TextAnchor.UpperLeft;
        keyText.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        keyObj.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);

        // Nom du skill (sous le slot)
        GameObject nameObj = new GameObject("SkillName");
        nameObj.transform.SetParent(slot.transform, false);
        RectTransform nameR = nameObj.AddComponent<RectTransform>();
        nameR.anchorMin = new Vector2(0, 0); nameR.anchorMax = new Vector2(1, 0);
        nameR.pivot = new Vector2(0.5f, 1f);
        nameR.anchoredPosition = new Vector2(0, -4);
        nameR.sizeDelta = new Vector2(0, 22);
        Text nameText = nameObj.AddComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.text = skill != null ? skill.skillName : "—";
        nameText.fontSize = 10;
        nameText.alignment = TextAnchor.UpperCenter;
        nameText.color = skill != null
            ? new Color(skill.skillColor.r * 0.9f + 0.1f, skill.skillColor.g * 0.6f + 0.2f, 0.3f, 0.9f)
            : new Color(0.5f, 0.5f, 0.5f, 0.6f);
        nameObj.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
        skillNameTexts[index] = nameText;
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

    private void UpdateManaBar(float current, float max)
    {
        if (manaFillImage != null)
            manaFillImage.rectTransform.anchorMax = new Vector2(current / max, 1f);

        if (manaText != null)
            manaText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void OnSkillCooldownChanged(int slot, float remaining, float total)
    {
        if (slot < 0 || slot >= 4) return;
        Image overlay = skillCooldownOverlays[slot];
        if (overlay == null) return;

        float ratio = total > 0f ? remaining / total : 0f;
        // L'overlay couvre le haut du slot à hauteur proportionnelle au cooldown restant
        RectTransform rt = overlay.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f - ratio);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(1, 0);
        rt.offsetMax = new Vector2(-1, -1);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnHealthChanged -= UpdateHealthBar;
        if (mana != null)
            mana.OnManaChanged -= UpdateManaBar;
        if (skillCaster != null)
            skillCaster.OnCooldownChanged -= OnSkillCooldownChanged;
    }
}

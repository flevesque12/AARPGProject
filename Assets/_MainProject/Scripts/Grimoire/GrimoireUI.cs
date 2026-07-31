using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Interface principale du Grimoire — ouverture/fermeture via Tab (GameInput.
// OnGrimoireTogglePressed, posé en Phase 5 sans consommateur jusqu'ici). Pour cette passe
// "basic crafting panel", n'héberge que CraftingPanel — Spellbook/Rune Encyclopedia/Synergy
// Encyclopedia/Journal sont l'item "Full Grimoire" de la Phase 8. Se crée entièrement en code
// au runtime (Canvas + EventSystem si absent), même convention que PlayerHUD. Verrouille le
// mouvement du joueur pendant que le Grimoire est ouvert (PlayerController.LockMovement,
// déjà utilisé par DodgeRoll pour le même genre de gate).
public class GrimoireUI : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private GameInput _gameInput;
    [SerializeField] private PlayerController _player;
    [SerializeField] private SpellCaster _spellCaster;

    [Header("Palette disponible (assigné dans l'Inspector)")]
    [SerializeField] private BaseFormData[] _availableForms;
    [SerializeField] private SchoolData[] _availableSchools;
    [SerializeField] private RuneModifier[] _availableRunes;

    private GameObject _canvasObj;
    private bool _isOpen;

    private void Awake()
    {
        if (_gameInput == null) _gameInput = GetComponent<GameInput>();
        if (_player == null) _player = GetComponent<PlayerController>();
        if (_spellCaster == null) _spellCaster = GetComponent<SpellCaster>();

        BuildUI();
        _canvasObj.SetActive(false);
    }

    private void OnEnable()
    {
        if (_gameInput != null) _gameInput.OnGrimoireTogglePressed += Toggle;
    }

    private void OnDisable()
    {
        if (_gameInput != null) _gameInput.OnGrimoireTogglePressed -= Toggle;
    }

    private void Toggle()
    {
        _isOpen = !_isOpen;
        _canvasObj.SetActive(_isOpen);
        _player?.LockMovement(_isOpen);
    }

    private void BuildUI()
    {
        EnsureEventSystem();

        _canvasObj = new GameObject("GrimoireUI_Canvas");
        Canvas canvas = _canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // au-dessus du PlayerHUD (100)
        _canvasObj.AddComponent<CanvasScaler>();
        _canvasObj.AddComponent<GraphicRaycaster>();

        GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(_canvasObj.transform, false);
        RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        GameObject panelObj = new GameObject("CraftingPanel", typeof(RectTransform));
        panelObj.transform.SetParent(_canvasObj.transform, false);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1000f, 700f);
        panelRect.anchoredPosition = Vector2.zero;

        CraftingPanel craftingPanel = panelObj.AddComponent<CraftingPanel>();
        craftingPanel.Configure(_availableForms, _availableSchools, _availableRunes, _spellCaster);
        craftingPanel.BuildUI();
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }
}

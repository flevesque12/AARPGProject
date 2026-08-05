using System.Collections;
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
    private CanvasGroup _canvasGroup;
    private RectTransform _panelRect;
    private bool _isOpen;
    private Coroutine _openCloseRoutine;

    private void Awake()
    {
        if (_gameInput == null) _gameInput = GetComponent<GameInput>();
        if (_player == null) _player = GetComponent<PlayerController>();
        if (_spellCaster == null) _spellCaster = GetComponent<SpellCaster>();

        BuildUI();
        _canvasGroup.alpha = 0f; // état "fermé" initial, pour que la toute première ouverture s'anime aussi (voir AnimateOpenClose)
        _panelRect.localScale = Vector3.one * 0.85f;
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
        _player?.LockMovement(_isOpen);

        if (_isOpen) _canvasObj.SetActive(true); // activé avant l'anim d'ouverture, désactivé après celle de fermeture

        if (_openCloseRoutine != null) StopCoroutine(_openCloseRoutine);
        _openCloseRoutine = StartCoroutine(AnimateOpenClose(_isOpen));
    }

    // Fondu + léger scale-in/out au lieu d'un SetActive instantané (juice pass, voir
    // conversation "the UI is boring"). Time.unscaledDeltaTime plutôt que Time.deltaTime : le
    // Grimoire doit s'animer normalement même si un hit-stop (voir Core/HitStop.cs) a le temps
    // gelé au moment où le joueur ouvre/ferme.
    private IEnumerator AnimateOpenClose(bool opening)
    {
        const float duration = 0.18f;
        const float scaleFrom = 0.85f;

        float startAlpha = _canvasGroup.alpha;
        float startScale = _panelRect.localScale.x;
        float targetAlpha = opening ? 1f : 0f;
        float targetScale = opening ? 1f : scaleFrom;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = opening ? 1f - Mathf.Pow(1f - t, 3f) : t * t; // ease-out en ouverture, ease-in en fermeture
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
            _panelRect.localScale = Vector3.one * Mathf.Lerp(startScale, targetScale, eased);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
        _panelRect.localScale = Vector3.one * targetScale;

        if (!opening) _canvasObj.SetActive(false);
        _openCloseRoutine = null;
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
        _canvasGroup = _canvasObj.AddComponent<CanvasGroup>();

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
        _panelRect = panelRect;

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

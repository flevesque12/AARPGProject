using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Nœud déplaçable du node-graph du Grimoire (palette de Formes/Écoles/Runes). Porte juste un
// payload (kind + asset) et délègue toute la logique de connexion/déconnexion à
// CraftingPanel.HandleNodeDropped. Générique, attaché dynamiquement par
// CraftingPanel.BuildUI — pas de prefab.
public class CraftingNode : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum NodeKind { Form, School, Rune }

    public NodeKind Kind { get; private set; }
    public ScriptableObject Payload { get; private set; }
    public RectTransform NodeRect { get; private set; }

    // Intensité de la rune portée par ce nœud (voir RuneSlot.cs, "continuous tuning") — n'a de
    // sens que pour Kind==Rune, mais inoffensif à porter sur tous les nœuds. Le nœud lui-même
    // EST le slot équipé (un seul CraftingNode par rune de palette, déplacé au lieu d'être
    // recréé), donc son intensité vit ici plutôt que dans un état séparé côté CraftingPanel.
    public float Intensity { get; private set; } = 1f;

    private const float MoveDuration = 0.18f;
    private const float PunchScale = 1.18f;
    private const float PunchDuration = 0.12f;

    private Vector2 _originAnchoredPos;
    private CraftingPanel _panel;
    private RectTransform _parentRect;
    private Coroutine _activeMove;

    public void Setup(CraftingPanel panel, NodeKind kind, ScriptableObject payload)
    {
        _panel = panel;
        Kind = kind;
        Payload = payload;
        NodeRect = GetComponent<RectTransform>();
        _parentRect = NodeRect.parent as RectTransform;
        _originAnchoredPos = NodeRect.anchoredPosition;
    }

    public void SetIntensity(float value) => Intensity = Mathf.Clamp(value, RuneSlot.MinIntensity, RuneSlot.MaxIntensity);

    public void ResetToOrigin() => NodeRect.anchoredPosition = _originAnchoredPos;
    public void AnimateToOrigin() => AnimateTo(_originAnchoredPos, false);

    // Anime le nœud vers une position (slot du noyau ou retour à l'origine) au lieu d'un
    // "snap" instantané (juice pass, voir conversation "the UI is boring") : ease-out puis
    // petit scale-punch à l'arrivée si `punchOnArrival` (utilisé pour une connexion réussie,
    // pas pour un simple retour à l'origine). Une seule anim à la fois par nœud — un nouveau
    // drag pendant le tween l'interrompt proprement plutôt que de les cumuler.
    public void AnimateTo(Vector2 targetPos, bool punchOnArrival)
    {
        if (_activeMove != null) StopCoroutine(_activeMove);
        _activeMove = StartCoroutine(MoveRoutine(targetPos, punchOnArrival));
    }

    private IEnumerator MoveRoutine(Vector2 targetPos, bool punchOnArrival)
    {
        Vector2 startPos = NodeRect.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < MoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / MoveDuration), 3f); // ease-out cubic
            NodeRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        NodeRect.anchoredPosition = targetPos;

        if (punchOnArrival)
            yield return StartCoroutine(PunchRoutine());

        _activeMove = null;
    }

    private IEnumerator PunchRoutine()
    {
        float elapsed = 0f;
        while (elapsed < PunchDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / PunchDuration);
            float scale = Mathf.Lerp(PunchScale, 1f, t);
            NodeRect.localScale = Vector3.one * scale;
            yield return null;
        }
        NodeRect.localScale = Vector3.one;
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            NodeRect.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData) => _panel.HandleNodeDropped(this);
}

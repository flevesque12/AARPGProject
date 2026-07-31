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

    private Vector2 _originAnchoredPos;
    private CraftingPanel _panel;
    private RectTransform _parentRect;

    public void Setup(CraftingPanel panel, NodeKind kind, ScriptableObject payload)
    {
        _panel = panel;
        Kind = kind;
        Payload = payload;
        NodeRect = GetComponent<RectTransform>();
        _parentRect = NodeRect.parent as RectTransform;
        _originAnchoredPos = NodeRect.anchoredPosition;
    }

    public void ResetToOrigin() => NodeRect.anchoredPosition = _originAnchoredPos;

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            NodeRect.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData) => _panel.HandleNodeDropped(this);
}

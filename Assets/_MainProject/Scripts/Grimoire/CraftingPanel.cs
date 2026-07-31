using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Panneau de craft du Grimoire — vrai node-graph : palette de nœuds déplaçables (Formes,
// Écoles, Runes, sur la gauche/droite/bas) que le joueur glisse sur le "noyau" central pour
// composer un SpellRecipe. Glisser un nœud hors du noyau le déconnecte. Item roadmap Phase 6
// "Grimoire UI — basic crafting panel (node-graph)", choix utilisateur du 2026-07-30 (vrai
// node-graph plutôt qu'un panneau à boutons simples). Construit entièrement en code au
// runtime (pas de prefab), même convention que PlayerHUD. Pas de Spellbook/Encyclopédie/
// Journal ici — ces panneaux Grimoire sont l'item "Full Grimoire" de la Phase 8.
public class CraftingPanel : MonoBehaviour
{
    private const float CoreDropRadius = 150f;
    private const float RingRadius = 180f;
    private const int MaxRunes = 4;

    private BaseFormData[] _availableForms;
    private SchoolData[] _availableSchools;
    private RuneModifier[] _availableRunes;
    private SpellCaster _spellCaster;

    private RectTransform _panelRect;
    private Text _coreText;
    private Text _feedbackText;
    private Coroutine _feedbackRoutine;

    private Vector2 _formSlotPos;
    private Vector2 _schoolSlotPos;
    private Vector2[] _runeSlotPositions;

    private readonly Image[] _lines = new Image[MaxRunes + 2]; // 0=form, 1=school, 2..5=rune

    private CraftingNode _connectedFormNode;
    private CraftingNode _connectedSchoolNode;
    private readonly CraftingNode[] _connectedRuneNodes = new CraftingNode[MaxRunes];

    private BaseFormData _selectedForm;
    private SchoolData _selectedSchool;
    private readonly List<RuneModifier> _selectedRunes = new List<RuneModifier>();
    private SpellRecipe _previewRecipe;

    public void Configure(BaseFormData[] forms, SchoolData[] schools, RuneModifier[] runes, SpellCaster spellCaster)
    {
        _availableForms = forms;
        _availableSchools = schools;
        _availableRunes = runes;
        _spellCaster = spellCaster;
    }

    public void BuildUI()
    {
        _panelRect = GetComponent<RectTransform>();
        gameObject.AddComponent<Image>().color = new Color(0.12f, 0.1f, 0.18f, 0.95f);

        ComputeSlotPositions();
        BuildTitle();
        BuildLines();
        BuildCore();
        BuildFormPalette();
        BuildSchoolPalette();
        BuildRunePalette();
        BuildSaveButtons();
        BuildFeedbackText();

        RefreshPreview();
    }

    // ========================================
    // CONNEXION / DÉCONNEXION (appelé par CraftingNode.OnEndDrag)
    // ========================================

    public void HandleNodeDropped(CraftingNode node)
    {
        bool droppedOnCore = node.NodeRect.anchoredPosition.magnitude < CoreDropRadius;

        if (droppedOnCore)
        {
            switch (node.Kind)
            {
                case CraftingNode.NodeKind.Form: ConnectForm(node); break;
                case CraftingNode.NodeKind.School: ConnectSchool(node); break;
                case CraftingNode.NodeKind.Rune: ConnectRune(node); break;
            }
            return;
        }

        bool wasConnected = _connectedFormNode == node || _connectedSchoolNode == node
            || System.Array.IndexOf(_connectedRuneNodes, node) >= 0;

        if (wasConnected) DisconnectNode(node);
        else node.ResetToOrigin();
    }

    private void ConnectForm(CraftingNode node)
    {
        if (_connectedFormNode != null && _connectedFormNode != node)
            _connectedFormNode.ResetToOrigin();

        _connectedFormNode = node;
        node.NodeRect.anchoredPosition = _formSlotPos;
        _selectedForm = (BaseFormData)node.Payload;
        SetLine(0, _formSlotPos);
        RefreshPreview();
    }

    private void ConnectSchool(CraftingNode node)
    {
        if (_connectedSchoolNode != null && _connectedSchoolNode != node)
            _connectedSchoolNode.ResetToOrigin();

        _connectedSchoolNode = node;
        node.NodeRect.anchoredPosition = _schoolSlotPos;
        _selectedSchool = (SchoolData)node.Payload;
        SetLine(1, _schoolSlotPos);
        RefreshPreview();
    }

    private void ConnectRune(CraftingNode node)
    {
        for (int i = 0; i < MaxRunes; i++)
        {
            if (_connectedRuneNodes[i] != node) continue;
            node.NodeRect.anchoredPosition = _runeSlotPositions[i]; // déjà connectée, on remet juste en place
            return;
        }

        int freeSlot = System.Array.IndexOf(_connectedRuneNodes, null);
        if (freeSlot < 0)
        {
            ShowFeedback("Maximum 4 runes.");
            node.ResetToOrigin();
            return;
        }

        _connectedRuneNodes[freeSlot] = node;
        node.NodeRect.anchoredPosition = _runeSlotPositions[freeSlot];
        SetLine(2 + freeSlot, _runeSlotPositions[freeSlot]);
        RebuildSelectedRunes();
        RefreshPreview();
    }

    private void DisconnectNode(CraftingNode node)
    {
        if (_connectedFormNode == node)
        {
            _connectedFormNode = null;
            _selectedForm = null;
            HideLine(0);
        }
        else if (_connectedSchoolNode == node)
        {
            _connectedSchoolNode = null;
            _selectedSchool = null;
            HideLine(1);
        }
        else
        {
            for (int i = 0; i < MaxRunes; i++)
            {
                if (_connectedRuneNodes[i] != node) continue;
                _connectedRuneNodes[i] = null;
                HideLine(2 + i);
            }
            RebuildSelectedRunes();
        }

        node.ResetToOrigin();
        RefreshPreview();
    }

    private void RebuildSelectedRunes()
    {
        _selectedRunes.Clear();
        foreach (var node in _connectedRuneNodes)
            if (node != null) _selectedRunes.Add((RuneModifier)node.Payload);
    }

    // ========================================
    // APERÇU + SAUVEGARDE
    // ========================================

    private void RefreshPreview()
    {
        if (_selectedForm == null || _selectedSchool == null)
        {
            _coreText.text = "Glisse une\nForme + École";
            return;
        }

        if (_previewRecipe == null) _previewRecipe = ScriptableObject.CreateInstance<SpellRecipe>();
        _previewRecipe.baseForm = _selectedForm;
        _previewRecipe.school = _selectedSchool;
        _previewRecipe.modifierRunes = _selectedRunes.ToArray();

        _coreText.text = $"{_selectedSchool.displayName}\n{_selectedForm.baseForm}\n" +
                          $"{_previewRecipe.ManaCost:0.#} Mana — {_previewRecipe.CooldownTime:0.#}s CD\n" +
                          $"{_selectedRunes.Count} rune(s)";
    }

    public void SaveToSlot(int slotIndex)
    {
        if (_selectedForm == null) { ShowFeedback("Choisis une Forme."); return; }
        if (_selectedSchool == null) { ShowFeedback("Choisis une École."); return; }
        if (_spellCaster == null) { ShowFeedback("SpellCaster introuvable."); return; }

        var recipe = ScriptableObject.CreateInstance<SpellRecipe>();
        recipe.spellName = $"{_selectedSchool.displayName} {_selectedForm.baseForm}";
        recipe.baseForm = _selectedForm;
        recipe.school = _selectedSchool;
        recipe.modifierRunes = _selectedRunes.ToArray();

        _spellCaster.SetSlot(slotIndex, recipe);
        ShowFeedback($"'{recipe.spellName}' sauvegardé dans le slot {slotIndex + 1} !");
    }

    private void ShowFeedback(string message)
    {
        _feedbackText.text = message;
        if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
        _feedbackRoutine = StartCoroutine(ClearFeedbackAfter(2f));
    }

    private IEnumerator ClearFeedbackAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        _feedbackText.text = "";
    }

    // ========================================
    // GÉOMÉTRIE
    // ========================================

    private void ComputeSlotPositions()
    {
        _formSlotPos = PositionOnRing(90f);
        _schoolSlotPos = PositionOnRing(270f);
        _runeSlotPositions = new[] { PositionOnRing(150f), PositionOnRing(210f), PositionOnRing(330f), PositionOnRing(30f) };
    }

    private static Vector2 PositionOnRing(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad) * RingRadius, Mathf.Sin(rad) * RingRadius);
    }

    private void SetLine(int index, Vector2 targetPos)
    {
        Image line = _lines[index];
        line.gameObject.SetActive(true);
        RectTransform lr = line.rectTransform;
        float distance = targetPos.magnitude;
        lr.sizeDelta = new Vector2(distance, 4f);
        lr.anchoredPosition = targetPos * 0.5f;
        float angle = Mathf.Atan2(targetPos.y, targetPos.x) * Mathf.Rad2Deg;
        lr.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void HideLine(int index) => _lines[index].gameObject.SetActive(false);

    // ========================================
    // CONSTRUCTION UI
    // ========================================

    private void BuildTitle()
    {
        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(_panelRect, false);
        RectTransform tr = titleObj.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -20f);
        tr.sizeDelta = new Vector2(600f, 40f);

        Text text = titleObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = "Grimoire — Atelier de Sorts";
    }

    private void BuildLines()
    {
        for (int i = 0; i < _lines.Length; i++)
        {
            GameObject lineObj = new GameObject($"Line{i}", typeof(RectTransform), typeof(Image));
            lineObj.transform.SetParent(_panelRect, false);
            RectTransform lr = lineObj.GetComponent<RectTransform>();
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.5f);
            lr.pivot = new Vector2(0.5f, 0.5f);
            lineObj.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.5f);
            lineObj.SetActive(false);
            _lines[i] = lineObj.GetComponent<Image>();
        }
    }

    private void BuildCore()
    {
        GameObject core = new GameObject("Core", typeof(RectTransform), typeof(Image));
        core.transform.SetParent(_panelRect, false);
        RectTransform coreRect = core.GetComponent<RectTransform>();
        coreRect.anchorMin = coreRect.anchorMax = new Vector2(0.5f, 0.5f);
        coreRect.pivot = new Vector2(0.5f, 0.5f);
        coreRect.sizeDelta = new Vector2(150f, 150f);
        coreRect.anchoredPosition = Vector2.zero;
        core.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f, 1f);

        GameObject textObj = new GameObject("CoreText", typeof(RectTransform));
        textObj.transform.SetParent(coreRect, false);
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;

        _coreText = textObj.AddComponent<Text>();
        _coreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _coreText.fontSize = 13;
        _coreText.alignment = TextAnchor.MiddleCenter;
        _coreText.color = Color.white;
        _coreText.text = "?";
    }

    private void BuildFormPalette()
    {
        if (_availableForms == null) return;
        float startY = 200f;
        for (int i = 0; i < _availableForms.Length; i++)
        {
            BaseFormData form = _availableForms[i];
            Vector2 pos = new Vector2(-420f, startY - i * 60f);
            CreateNode(form.baseForm.ToString(), new Color(0.5f, 0.5f, 0.55f), pos, new Vector2(110f, 44f), CraftingNode.NodeKind.Form, form);
        }
    }

    private void BuildSchoolPalette()
    {
        if (_availableSchools == null) return;
        float startY = 260f;
        for (int i = 0; i < _availableSchools.Length; i++)
        {
            SchoolData school = _availableSchools[i];
            Vector2 pos = new Vector2(420f, startY - i * 48f);
            CreateNode(school.displayName, school.primaryColor, pos, new Vector2(110f, 40f), CraftingNode.NodeKind.School, school);
        }
    }

    private void BuildRunePalette()
    {
        if (_availableRunes == null) return;
        float startX = -165f;
        for (int i = 0; i < _availableRunes.Length; i++)
        {
            RuneModifier rune = _availableRunes[i];
            Vector2 pos = new Vector2(startX + i * 110f, -300f);
            CreateNode(rune.runeName, new Color(0.6f, 0.4f, 0.7f), pos, new Vector2(100f, 40f), CraftingNode.NodeKind.Rune, rune);
        }
    }

    private CraftingNode CreateNode(string label, Color color, Vector2 anchoredPos, Vector2 size, CraftingNode.NodeKind kind, ScriptableObject payload)
    {
        GameObject nodeObj = new GameObject($"Node_{label}", typeof(RectTransform), typeof(Image));
        nodeObj.transform.SetParent(_panelRect, false);
        RectTransform rt = nodeObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        nodeObj.GetComponent<Image>().color = color;

        GameObject textObj = new GameObject("Label", typeof(RectTransform));
        textObj.transform.SetParent(rt, false);
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 11;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;

        CraftingNode node = nodeObj.AddComponent<CraftingNode>();
        node.Setup(this, kind, payload);
        return node;
    }

    private void BuildSaveButtons()
    {
        CreateSaveButton("Sauver -> Slot 1", new Vector2(-100f, 300f), 0);
        CreateSaveButton("Sauver -> Slot 2", new Vector2(100f, 300f), 1);
    }

    private void CreateSaveButton(string label, Vector2 pos, int slotIndex)
    {
        GameObject btnObj = new GameObject($"SaveButton{slotIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(_panelRect, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(180f, 44f);
        rt.anchoredPosition = pos;
        btnObj.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f);

        GameObject textObj = new GameObject("Label", typeof(RectTransform));
        textObj.transform.SetParent(rt, false);
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 13;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;

        btnObj.GetComponent<Button>().onClick.AddListener(() => SaveToSlot(slotIndex));
    }

    private void BuildFeedbackText()
    {
        GameObject obj = new GameObject("Feedback", typeof(RectTransform));
        obj.transform.SetParent(_panelRect, false);
        RectTransform tr = obj.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0f);
        tr.pivot = new Vector2(0.5f, 0f);
        tr.anchoredPosition = new Vector2(0f, 20f);
        tr.sizeDelta = new Vector2(600f, 30f);

        _feedbackText = obj.AddComponent<Text>();
        _feedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _feedbackText.fontSize = 14;
        _feedbackText.alignment = TextAnchor.MiddleCenter;
        _feedbackText.color = new Color(1f, 0.9f, 0.4f);
        _feedbackText.text = "";
    }
}

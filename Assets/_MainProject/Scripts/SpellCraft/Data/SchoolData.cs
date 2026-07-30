using UnityEngine;

// Données par école élémentaire (voir CLAUDE.md, table "The 7 Schools"). Purement descriptif
// pour l'instant (couleurs pour l'UI/VFX) — pas de profil VFX/synergies encore (Phase 6/8).
[CreateAssetMenu(fileName = "NewSchool", menuName = "Glyphes/Spell Crafting/School")]
public class SchoolData : ScriptableObject
{
    [Header("Identité")]
    public SpellSchool school;
    public string displayName = "Ignis";
    [TextArea] public string description;

    [Header("Palette (voir CLAUDE.md, table \"The 7 Schools\")")]
    public Color primaryColor = new Color(1f, 0.4f, 0.1f);
    public Color secondaryColor = new Color(1f, 0.7f, 0.3f);
}

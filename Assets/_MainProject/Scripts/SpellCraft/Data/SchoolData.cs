using UnityEngine;

// Données par école élémentaire (voir CLAUDE.md, table "The 7 Schools"). Ignis/Aqua/Terra ont
// un effet signature appliqué par SpellCraft/Runtime/SchoolEffectApplier.cs (item roadmap
// "3 schools playable") ; les 4 autres écoles restent purement descriptives (couleur/VFX)
// jusqu'à leur propre item (Phase 8, "All 7 schools with VFX") — champs inutilisés pour elles,
// même convention à plat que BaseFormData/SkillData.
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

    [Header("Ignis — Brûlure (DoT)")]
    public float burnDamagePerTick = 5f;
    public float burnDuration = 3f;
    public float burnTickInterval = 1f;

    [Header("Aqua — Ralentissement")]
    [Tooltip("Fraction de la vitesse d'origine conservée pendant le ralentissement (0.5 = -50%).")]
    [Range(0f, 1f)] public float slowMultiplier = 0.5f;
    public float slowDuration = 2.5f;

    [Header("Terra — Dégâts majorés + Knockback")]
    [Tooltip("Bonus multiplicatif sur les dégâts (0.2 = +20%).")]
    public float damageBonusMultiplier = 0.2f;
    public float knockbackForce = 6f;
}

using UnityEngine;

// Données de tuning par forme de base (Projectile/Zone/Aura/Impact). Un même champ n'est
// pertinent que pour certaines formes (ex: projectileSpeed pour Projectile uniquement) —
// même convention que Skills/SkillData.cs (v3.1), où un seul type de données couvre
// plusieurs SkillType. Le comportement runtime associé (ProjectileSpell fait, Zone/Aura/
// Impact restent à faire — roadmap Phase 6 "4 base forms functional") lit ces champs
// depuis l'instance assignée à SpellRecipe.baseForm.
[CreateAssetMenu(fileName = "NewBaseForm", menuName = "Glyphes/Spell Crafting/Base Form")]
public class BaseFormData : ScriptableObject
{
    [Header("Identité")]
    public SpellBaseForm baseForm;
    [TextArea] public string description;

    [Header("Coût de base (voir CLAUDE.md, table \"Mana costs by complexity\")")]
    [Tooltip("Coût en Mana avant application des multiplicateurs des runes.")]
    public float baseManaCost = 8f;
    [Tooltip("Cooldown en secondes avant application des multiplicateurs des runes.")]
    public float baseCooldown = 1.5f;

    [Header("Combat (pertinence selon la forme — Projectile/Impact: dégâts direct, Zone: dégâts par tick, Aura: montant de bouclier)")]
    public float baseDamage = 30f;
    [Tooltip("Portée max (Projectile) ou rayon d'action (Zone/Impact).")]
    public float range = 8f;
    public float radius = 2f;

    [Header("Projectile uniquement")]
    public float projectileSpeed = 18f;
    public float projectileSize = 0.3f;

    [Header("Zone / Aura uniquement")]
    public float duration = 3f;
    public float tickInterval = 0.5f;
}

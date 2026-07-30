using UnityEngine;

// Données de tuning par forme de base (Projectile/Zone/Aura/Impact). Le comportement
// runtime associé (ProjectileSpell, ZoneSpell, AuraSpell, ImpactSpell) n'existe pas encore
// — c'est l'item suivant de la roadmap Phase 6 ("4 base forms functional").
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
}

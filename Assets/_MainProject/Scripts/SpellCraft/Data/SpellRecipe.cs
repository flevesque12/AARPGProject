using UnityEngine;

// Recette de sort complète — Forme de base + École + jusqu'à 4 runes modificatrices (voir
// CLAUDE.md, "Spell Crafting System"). ManaCost/CooldownTime sont calculés à la volée à
// partir des données de baseForm + des multiplicateurs de chaque rune plutôt que sérialisés,
// pour ne jamais désynchroniser la valeur affichée du reste de la recette.
[CreateAssetMenu(fileName = "NewSpellRecipe", menuName = "Glyphes/Spell Crafting/Spell Recipe")]
public class SpellRecipe : ScriptableObject
{
    private const int MaxModifierRunes = 4;

    [Header("Identité")]
    public string spellName = "New Spell";
    [TextArea] public string description;

    [Header("Composition")]
    public BaseFormData baseForm;
    public SchoolData school;
    [Tooltip("Maximum 4 runes (voir CLAUDE.md, \"16 modifier runes\").")]
    public RuneModifier[] modifierRunes = new RuneModifier[0];

    public RuneModifier[] ModifierRunes => modifierRunes;

    // Coût en Mana : base de la forme × (1 + multiplicateur) de chaque rune, en cascade.
    // Voir CLAUDE.md table "Mana costs by complexity" pour les fourchettes indicatives.
    public float ManaCost
    {
        get
        {
            float cost = baseForm != null ? baseForm.baseManaCost : 0f;
            if (modifierRunes == null) return cost;
            foreach (var rune in modifierRunes)
                if (rune != null) cost *= (1f + rune.manaCostMultiplier);
            return cost;
        }
    }

    public float CooldownTime
    {
        get
        {
            float cooldown = baseForm != null ? baseForm.baseCooldown : 0f;
            if (modifierRunes == null) return cooldown;
            foreach (var rune in modifierRunes)
                if (rune != null) cooldown *= (1f + rune.cooldownMultiplier);
            return cooldown;
        }
    }

    private void OnValidate()
    {
        if (modifierRunes == null) return;

        if (modifierRunes.Length > MaxModifierRunes)
            System.Array.Resize(ref modifierRunes, MaxModifierRunes);

        for (int i = 0; i < modifierRunes.Length; i++)
        {
            var rune = modifierRunes[i];
            if (rune == null) continue;

            for (int j = 0; j < modifierRunes.Length; j++)
            {
                if (i == j || modifierRunes[j] == null) continue;
                if (rune.IsIncompatibleWith(modifierRunes[j]))
                {
                    Debug.LogWarning($"[SpellRecipe] '{name}': '{rune.runeName}' est incompatible avec '{modifierRunes[j].runeName}' — retiré.", this);
                    modifierRunes[j] = null;
                }
            }
        }
    }
}

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
    [Tooltip("Maximum 4 runes (voir CLAUDE.md, \"16 modifier runes\"). Chaque slot porte aussi" +
        " l'intensité de sa rune (voir RuneSlot.cs, \"continuous tuning\").")]
    public RuneSlot[] modifierRunes = new RuneSlot[0];

    public RuneSlot[] ModifierRunes => modifierRunes;

    // Coût en Mana : base de la forme × (1 + multiplicateur effectif) de chaque rune, en
    // cascade. Le multiplicateur effectif est scalé par l'intensité du slot (RuneSlot,
    // "continuous tuning") — voir CLAUDE.md table "Mana costs by complexity" pour les
    // fourchettes indicatives à intensité 1.0 (valeur d'auteur).
    public float ManaCost
    {
        get
        {
            float cost = baseForm != null ? baseForm.baseManaCost : 0f;
            if (modifierRunes == null) return cost;
            foreach (var slot in modifierRunes)
                if (slot.rune != null) cost *= (1f + slot.rune.EffectiveManaCostMultiplier(slot.intensity));
            return cost;
        }
    }

    public float CooldownTime
    {
        get
        {
            float cooldown = baseForm != null ? baseForm.baseCooldown : 0f;
            if (modifierRunes == null) return cooldown;
            foreach (var slot in modifierRunes)
                if (slot.rune != null) cooldown *= (1f + slot.rune.EffectiveCooldownMultiplier(slot.intensity));
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
            modifierRunes[i].intensity = Mathf.Clamp(modifierRunes[i].intensity, RuneSlot.MinIntensity, RuneSlot.MaxIntensity);

            RuneModifier rune = modifierRunes[i].rune;
            if (rune == null) continue;

            for (int j = 0; j < modifierRunes.Length; j++)
            {
                RuneModifier other = modifierRunes[j].rune;
                if (i == j || other == null) continue;
                if (rune.IsIncompatibleWith(other))
                {
                    Debug.LogWarning($"[SpellRecipe] '{name}': '{rune.runeName}' est incompatible avec '{other.runeName}' — retiré.", this);
                    modifierRunes[j].rune = null;
                }
            }
        }
    }
}

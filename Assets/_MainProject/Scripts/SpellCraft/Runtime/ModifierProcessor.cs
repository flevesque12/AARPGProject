// Applique les hooks ISpellModifier de chaque rune d'un SpellRecipe à un SpellContext
// fraîchement instancié. Appelé par SpellFactory.CreateSpell.
public static class ModifierProcessor
{
    public static void ApplyOnSpawn(SpellContext context)
    {
        if (context == null || context.Recipe == null) return;

        var runes = context.Recipe.ModifierRunes;
        if (runes == null) return;

        foreach (var slot in runes)
            slot.rune?.OnSpawn(context, slot.intensity);
    }
}

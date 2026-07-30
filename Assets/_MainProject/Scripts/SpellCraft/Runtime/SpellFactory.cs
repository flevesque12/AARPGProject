using UnityEngine;

// Construit un sort à partir d'une SpellRecipe : instancie le GameObject racine, y attache
// SpellContext, et applique les runes modificatrices (ModifierProcessor). Le comportement
// visuel/physique par forme (Projectile/Zone/Aura/Impact) n'existe pas encore — c'est l'item
// suivant de la roadmap Phase 6 ("4 base forms functional"), qui ajoutera ses composants sur
// ce même GameObject plutôt que de remplacer SpellContext. Ne gère pas le coût en Mana ni le
// cooldown — ça reste la responsabilité de l'appelant (futur SpellCaster, qui orchestre le
// cast comme SkillCaster le fait aujourd'hui pour le système v3.1).
public static class SpellFactory
{
    public static SpellContext CreateSpell(SpellRecipe recipe, GameObject caster, Vector3 origin, Vector3 direction)
    {
        if (recipe == null)
        {
            Debug.LogError("[SpellFactory] Cannot create spell: recipe is null.");
            return null;
        }

        var spellObject = new GameObject($"Spell_{recipe.spellName}");
        spellObject.transform.position = origin;
        if (direction.sqrMagnitude > 0.0001f)
            spellObject.transform.rotation = Quaternion.LookRotation(direction);

        var context = spellObject.AddComponent<SpellContext>();
        context.Initialize(recipe, caster, origin, direction);

        ModifierProcessor.ApplyOnSpawn(context);

        return context;
    }
}

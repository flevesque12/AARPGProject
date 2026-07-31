using UnityEngine;

// Construit un sort à partir d'une SpellRecipe : instancie le GameObject racine, y attache
// SpellContext, applique les runes modificatrices (ModifierProcessor), puis attache le
// comportement de la forme de base (Projectile/Zone/Aura/Impact — les 4 formes sont
// implémentées, roadmap Phase 6 "4 base forms functional" terminé le 2026-07-30). Ne gère pas
// le coût en Mana ni le cooldown — ça reste la responsabilité de l'appelant (futur
// SpellCaster, qui orchestre le cast comme SkillCaster le fait aujourd'hui pour le système
// v3.1).
public static class SpellFactory
{
    public static SpellContext CreateSpell(SpellRecipe recipe, GameObject caster, Vector3 origin, Vector3 direction, LayerMask hitLayer)
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
        AttachBaseFormBehaviour(context, hitLayer);

        return context;
    }

    private static void AttachBaseFormBehaviour(SpellContext context, LayerMask hitLayer)
    {
        if (context.Recipe.baseForm == null)
        {
            Debug.LogWarning($"[SpellFactory] '{context.Recipe.spellName}': baseForm is null, no behaviour attached.");
            return;
        }

        switch (context.Recipe.baseForm.baseForm)
        {
            case SpellBaseForm.Projectile:
                context.gameObject.AddComponent<ProjectileSpell>().Init(context, hitLayer);
                break;
            case SpellBaseForm.Zone:
                context.gameObject.AddComponent<ZoneSpell>().Init(context, hitLayer);
                break;
            case SpellBaseForm.Aura:
                context.gameObject.AddComponent<AuraSpell>().Init(context);
                break;
            case SpellBaseForm.Impact:
                context.gameObject.AddComponent<ImpactSpell>().Init(context, hitLayer);
                break;
        }
    }
}

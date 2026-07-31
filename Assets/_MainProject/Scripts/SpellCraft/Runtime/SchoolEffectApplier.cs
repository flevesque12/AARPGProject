using UnityEngine;
using UnityEngine.AI;

// Point d'entrée unique pour les effets signature par école (voir CLAUDE.md, table "The 7
// Schools" — colonne "Combat role"). Appelé par les formes de base qui infligent des dégâts
// directs (ProjectileSpell, ZoneSpell, ImpactSpell) après chaque hit réussi, avant TakeDamage
// — Aura ne cible jamais d'ennemi donc ne l'appelle pas. Seuls Ignis/Aqua/Terra ont un effet
// pour l'instant (item roadmap "3 schools playable") ; les 4 autres écoles n'ont pas de case,
// donc no-op jusqu'à leur propre item (Phase 8, "All 7 schools with VFX").
public static class SchoolEffectApplier
{
    public static void Apply(SpellRecipe recipe, Collider targetCollider, HealthSystem targetHealth, Vector3 hitPoint, ref float damage)
    {
        if (recipe.school == null) return;

        switch (recipe.school.school)
        {
            case SpellSchool.Ignis:
                ApplyBurn(recipe.school, targetHealth);
                break;
            case SpellSchool.Aqua:
                ApplySlow(recipe.school, targetCollider);
                break;
            case SpellSchool.Terra:
                damage *= 1f + recipe.school.damageBonusMultiplier;
                ApplyKnockback(recipe.school, targetCollider, hitPoint);
                break;
        }
    }

    private static void ApplyBurn(SchoolData school, HealthSystem targetHealth)
    {
        BurnStatus burn = targetHealth.GetComponent<BurnStatus>();
        if (burn == null) burn = targetHealth.gameObject.AddComponent<BurnStatus>();
        burn.Init(targetHealth, school.burnDamagePerTick, school.burnDuration, school.burnTickInterval);
    }

    private static void ApplySlow(SchoolData school, Collider targetCollider)
    {
        NavMeshAgent agent = targetCollider.GetComponent<NavMeshAgent>();
        if (agent == null) return;

        SlowStatus slow = agent.GetComponent<SlowStatus>();
        if (slow == null) slow = agent.gameObject.AddComponent<SlowStatus>();
        slow.Init(agent, school.slowMultiplier, school.slowDuration);
    }

    private static void ApplyKnockback(SchoolData school, Collider targetCollider, Vector3 hitPoint)
    {
        NavMeshAgent agent = targetCollider.GetComponent<NavMeshAgent>();
        if (agent == null) return;

        Vector3 direction = targetCollider.transform.position - hitPoint;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) direction = targetCollider.transform.forward;
        direction.Normalize();

        Knockback knockback = agent.GetComponent<Knockback>();
        if (knockback == null) knockback = agent.gameObject.AddComponent<Knockback>();
        knockback.Apply(agent, direction * school.knockbackForce);
    }
}

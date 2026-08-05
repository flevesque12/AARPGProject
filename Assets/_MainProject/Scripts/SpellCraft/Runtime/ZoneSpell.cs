using System.Collections;
using UnityEngine;

// Comportement de la forme de base Zone : effet d'aire au sol, dégâts par tick pendant une
// durée (repris de Skills/SkillCaster.cs CastZone/ApplyAoE v3.1), en appliquant l'effet
// signature de l'école du sort à chaque tick (SchoolEffectApplier — une zone Terra repousse
// donc à répétition tant qu'une cible y reste, c'est voulu). Lit SpellContext.RadiusMultiplier
// (rune Expand) et SpellContext.DurationMultiplier (rune Persist). Enregistre aussi un patch
// de terrain (EnvironmentState — feu au sol pour Ignis, flaque pour Aqua, etc.) qui vit
// exactement la durée de la zone ; la détection de synergie qui réagira à ces patches
// (Ignis+eau=Vapeur, etc.) est l'item "Environmental synergies" de la Phase 8, pas géré ici.
// Attaché par SpellFactory sur le GameObject portant le SpellContext quand
// recipe.baseForm.baseForm == SpellBaseForm.Zone. Comme ProjectileSpell, pas de hook OnHit
// pour les runes tant qu'ISpellModifier n'expose que OnSpawn (voir ISpellModifier.cs).
public class ZoneSpell : MonoBehaviour
{
    private SpellContext _context;
    private float _damage;
    private float _radius;
    private float _duration;
    private float _tickInterval;
    private LayerMask _hitLayer;

    public void Init(SpellContext context, LayerMask hitLayer)
    {
        _context = context;
        BaseFormData form = context.Recipe.baseForm;

        _damage = form.baseDamage;
        _radius = form.radius * context.RadiusMultiplier;   // rune Expand
        _duration = form.duration * context.DurationMultiplier; // rune Persist
        _tickInterval = form.tickInterval;
        _hitLayer = hitLayer;

        if (context.Recipe.school != null && EnvironmentState.TryGetTerrainType(context.Recipe.school.school, out TerrainType terrain))
        {
            float intensity = EnvironmentState.RegisterPatch(terrain, transform.position, _radius, _duration);
            // Re-marquer un terrain déjà actif du même type l'intensifie au lieu de simplement
            // coexister (voir EnvironmentState.RegisterPatch) — +50% dégâts par stack au-delà du
            // premier, captée une fois à l'Init comme RadiusMultiplier/DurationMultiplier plutôt
            // que réinterrogée à chaque tick.
            if (intensity > 1f)
                _damage *= 1f + (intensity - 1f) * 0.5f;
        }

        BuildVisual();
        StartCoroutine(TickLoop());
    }

    private IEnumerator TickLoop()
    {
        float elapsed = 0f;
        float nextTick = 0f;
        while (elapsed < _duration)
        {
            if (elapsed >= nextTick)
            {
                nextTick += _tickInterval;
                ApplyTick();
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void ApplyTick()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _radius, _hitLayer);
        foreach (Collider hit in hits)
        {
            HealthSystem hs = hit.GetComponent<HealthSystem>();
            if (hs == null || hs.IsDead) continue;

            float damage = _damage;
            SchoolEffectApplier.Apply(_context.Recipe, hit, hs, transform.position, ref damage);
            hs.TakeDamage(damage);

            // Petit burst par cible touchée, pas de hit-stop (juice pass, voir conversation
            // "add some juice") : une Zone est un effet soutenu qui tick en continu tant qu'une
            // cible y reste, geler le temps à chaque tick serait un gel quasi permanent plutôt
            // qu'un "impact" ponctuel comme Projectile/Impact.
            SpellImpactVFX.Spawn(hit.ClosestPoint(transform.position), _context.Recipe.school != null ? _context.Recipe.school.primaryColor : Color.white);
        }
    }

    private void BuildVisual()
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = new Vector3(_radius * 2f, 0.05f, _radius * 2f);

        Collider col = visual.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Color color = _context.Recipe.school != null ? _context.Recipe.school.primaryColor : Color.white;
        color.a = 0.4f;
        Renderer r = visual.GetComponent<Renderer>();
        if (r != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color);
            r.SetPropertyBlock(mpb);
        }
    }
}

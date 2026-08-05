using System.Collections;
using UnityEngine;

// Comportement de la forme de base Aura : bouclier absorbant sur le lanceur pendant une
// durée (voir CLAUDE.md, "Shield, resistance, buff" — choix utilisateur du 2026-07-30 pour la
// version minimale : bouclier absorbant, pas de résistance/buff générique, ce système reste à
// concevoir). Réutilise BaseFormData.baseDamage comme montant de bouclier — pas de champ dédié,
// même logique de réutilisation que "range" servant à la fois de portée Projectile et de rayon
// Zone/Impact. Attaché par SpellFactory quand recipe.baseForm.baseForm == SpellBaseForm.Aura.
// Lit SpellContext.DurationMultiplier (rune Persist). Contrairement à Projectile/Zone, ne
// prend pas de hitLayer — l'effet cible uniquement le HealthSystem du lanceur
// (context.Caster), jamais une cible externe.
public class AuraSpell : MonoBehaviour
{
    private SpellContext _context;
    private HealthSystem _casterHealth;

    // BuildVisual() parents the shield bubble to the CASTER (not to this AuraSpell's own
    // GameObject) so it follows the player around while the aura is active — but that means
    // Destroy(gameObject) in ExpireAfter only destroys this tracking object, never the visual
    // living under the player. Bug fix (2026-08-06): without this reference, the bubble was
    // orphaned permanently on every cast (shield mechanic correctly expired via ClearShield,
    // but the visual never did — looked like "the aura never turns off", and stacked a new
    // leftover sphere on the player each time Aura was recast).
    private GameObject _visual;

    public void Init(SpellContext context)
    {
        _context = context;
        BaseFormData form = context.Recipe.baseForm;

        _casterHealth = context.Caster != null ? context.Caster.GetComponent<HealthSystem>() : null;
        if (_casterHealth == null)
        {
            Debug.LogWarning($"[AuraSpell] '{context.Recipe.spellName}': caster has no HealthSystem, aura has no effect.");
            Destroy(gameObject);
            return;
        }

        _casterHealth.AddShield(form.baseDamage);
        BuildVisual();
        StartCoroutine(ExpireAfter(form.duration * context.DurationMultiplier)); // rune Persist
    }

    private IEnumerator ExpireAfter(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (_casterHealth != null)
            _casterHealth.ClearShield();

        if (_visual != null)
            Destroy(_visual);

        Destroy(gameObject);
    }

    private void BuildVisual()
    {
        _visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _visual.transform.SetParent(_context.Caster.transform, false);
        _visual.transform.localPosition = Vector3.up;
        _visual.transform.localScale = Vector3.one * 1.2f;

        Collider col = _visual.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Color color = _context.Recipe.school != null ? _context.Recipe.school.primaryColor : Color.white;
        color.a = 0.25f;
        Renderer r = _visual.GetComponent<Renderer>();
        if (r != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color);
            r.SetPropertyBlock(mpb);
        }
    }
}

using System.Collections;
using UnityEngine;

// Comportement de la forme de base Impact : dégâts instantanés dans un rayon autour du point
// de cast (mêlée / courte portée, voir CLAUDE.md "Impact: Close attack, break obstacles,
// push") — un seul hit, pas de déplacement ni de persistance. Logique de dégâts reprise de
// Skills/SkillCaster.cs ApplyAoE v3.1, sans castTime/telegraph (le telegraph reste un item
// futur, hors scope "4 base forms functional"), en appliquant l'effet signature de l'école du
// sort (SchoolEffectApplier) et SpellContext.RadiusMultiplier (rune Expand). Attaché par
// SpellFactory quand recipe.baseForm.baseForm == SpellBaseForm.Impact.
public class ImpactSpell : MonoBehaviour
{
    private const float VisualLifetime = 0.3f;

    public void Init(SpellContext context, LayerMask hitLayer)
    {
        BaseFormData form = context.Recipe.baseForm;
        float radius = form.radius * context.RadiusMultiplier; // rune Expand

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, hitLayer);
        foreach (Collider hit in hits)
        {
            HealthSystem hs = hit.GetComponent<HealthSystem>();
            if (hs == null || hs.IsDead) continue;

            float damage = form.baseDamage;
            SchoolEffectApplier.Apply(context.Recipe, hit, hs, transform.position, ref damage);
            hs.TakeDamage(damage);
        }

        StartCoroutine(BuildVisualThenExpire(context, radius));
    }

    private IEnumerator BuildVisualThenExpire(SpellContext context, float radius)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = Vector3.one * radius * 2f;

        Collider col = visual.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Color color = context.Recipe.school != null ? context.Recipe.school.primaryColor : Color.white;
        color.a = 0.35f;
        Renderer r = visual.GetComponent<Renderer>();
        if (r != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color);
            r.SetPropertyBlock(mpb);
        }

        yield return new WaitForSeconds(VisualLifetime);
        Destroy(gameObject);
    }
}

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
        Color color = context.Recipe.school != null ? context.Recipe.school.primaryColor : Color.white;

        int hitCount = 0;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, hitLayer);
        foreach (Collider hit in hits)
        {
            HealthSystem hs = hit.GetComponent<HealthSystem>();
            if (hs == null || hs.IsDead) continue;

            float damage = form.baseDamage;
            SchoolEffectApplier.Apply(context.Recipe, hit, hs, transform.position, ref damage);
            hs.TakeDamage(damage);
            hitCount++;
        }

        // Un seul burst/hit-stop pour tout le groupe touché, pas un par cible — sinon un Impact
        // qui touche 3 ennemis d'un coup gèlerait le temps 3 fois d'affilée (juice pass, voir
        // conversation "add some juice").
        if (hitCount > 0)
        {
            SpellImpactVFX.Spawn(transform.position, color);
            HitStop.Trigger();
        }

        StartCoroutine(BuildVisualThenExpire(radius, color));
    }

    private IEnumerator BuildVisualThenExpire(float radius, Color color)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = Vector3.zero; // évite un flash à l'échelle par défaut (1,1,1) avant le pop

        Collider col = visual.GetComponent<Collider>();
        if (col != null) Destroy(col);

        color.a = 0.35f;
        Renderer r = visual.GetComponent<Renderer>();
        if (r != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color);
            r.SetPropertyBlock(mpb);
        }

        // Pop d'échelle (0 -> pleine taille en ~1/3 du temps de vie) au lieu d'apparaître
        // instantanément à pleine taille — juice pass.
        float fullScale = radius * 2f;
        float popDuration = VisualLifetime * 0.3f;
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            visual.transform.localScale = Vector3.one * fullScale * Mathf.Sin(t * Mathf.PI * 0.5f);
            yield return null;
        }
        visual.transform.localScale = Vector3.one * fullScale;

        yield return new WaitForSeconds(VisualLifetime - popDuration);
        Destroy(gameObject);
    }
}

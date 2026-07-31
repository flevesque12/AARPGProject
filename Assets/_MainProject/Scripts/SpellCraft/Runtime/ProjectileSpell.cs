using UnityEngine;

// Comportement de la forme de base Projectile : avance en ligne droite, inflige les dégâts
// au premier HealthSystem touché (détection OverlapSphere manuelle, reprise de
// Skills/SkillProjectile.cs v3.1 — pas de Rigidbody requis), en appliquant l'effet signature
// de l'école du sort (SchoolEffectApplier — brûlure Ignis, ralentissement Aqua, dégâts
// majorés+knockback Terra). Attaché par SpellFactory sur le GameObject portant le SpellContext
// quand recipe.baseForm.baseForm == SpellBaseForm.Projectile. Lit SpellContext.BounceCount
// (rune Bounce) et SpellContext.ExtraProjectileCount (rune Split) — pas de hook OnHit
// générique pour les runes (ISpellModifier n'a que OnSpawn, voir ISpellModifier.cs), ces deux
// effets sont gérés directement ici plutôt que via un bus d'événements, pragmatique pour deux
// cas d'usage concrets.
public class ProjectileSpell : MonoBehaviour
{
    private const float SplitAngleStep = 15f;

    // Cache de la recette plutôt qu'une référence vivante à SpellContext : un projectile
    // "Split" (sibling, voir SpawnSplitSiblings) partage le SpellContext du projectile
    // primaire, qui vit sur le GameObject primaire — si celui-ci est détruit avant le sibling
    // (ex: le primaire touche sa cible avant que le sibling n'expire), _context.Recipe
    // planterait (MissingReferenceException). SpellRecipe est un ScriptableObject persistant,
    // sûr à garder en cache indépendamment du cycle de vie du GameObject.
    private SpellRecipe _recipe;
    private Vector3 _direction;
    private float _speed;
    private float _maxRange;
    private float _damage;
    private float _hitRadius;
    private LayerMask _hitLayer;
    private Vector3 _origin;
    private int _bouncesRemaining;
    private bool _hit;

    public void Init(SpellContext context, LayerMask hitLayer) => Init(context, hitLayer, true);

    private void Init(SpellContext context, LayerMask hitLayer, bool isPrimary)
    {
        _recipe = context.Recipe;
        BaseFormData form = _recipe.baseForm;

        _direction = context.Direction.sqrMagnitude > 0.0001f ? context.Direction.normalized : transform.forward;
        _speed = form.projectileSpeed;
        _maxRange = form.range;
        _damage = form.baseDamage;
        _hitRadius = form.projectileSize * 0.55f;
        _hitLayer = hitLayer;
        _origin = transform.position;
        _bouncesRemaining = context.BounceCount;

        BuildVisual(form.projectileSize);

        if (isPrimary && context.ExtraProjectileCount > 0)
            SpawnSplitSiblings(context, hitLayer, context.ExtraProjectileCount);
    }

    private void SpawnSplitSiblings(SpellContext context, LayerMask hitLayer, int count)
    {
        float startAngle = -SplitAngleStep * (count / 2f);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + SplitAngleStep * i;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * _direction;

            var sibling = new GameObject($"Spell_{context.Recipe.spellName}_Split{i}");
            sibling.transform.position = transform.position;
            sibling.transform.rotation = Quaternion.LookRotation(dir);

            var proj = sibling.AddComponent<ProjectileSpell>();
            proj.Init(context, hitLayer, false);
            proj.OverrideDirection(dir);
        }
    }

    private void OverrideDirection(Vector3 direction)
    {
        _direction = direction.normalized;
        _origin = transform.position;
    }

    private void Update()
    {
        if (_hit) return;

        transform.position += _direction * _speed * Time.deltaTime;

        if (Vector3.Distance(_origin, transform.position) >= _maxRange)
        {
            _hit = true;
            Destroy(gameObject);
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, _hitRadius, _hitLayer);
        foreach (Collider hit in hits)
        {
            HealthSystem hs = hit.GetComponent<HealthSystem>();
            if (hs == null || hs.IsDead) continue;

            float damage = _damage;
            SchoolEffectApplier.Apply(_recipe, hit, hs, transform.position, ref damage);
            hs.TakeDamage(damage);

            if (_bouncesRemaining > 0)
            {
                _bouncesRemaining--;
                Bounce(hit);
                return;
            }

            _hit = true;
            Destroy(gameObject);
            return;
        }
    }

    private void Bounce(Collider hitCollider)
    {
        Vector3 normal = transform.position - hitCollider.transform.position;
        normal.y = 0f;
        if (normal.sqrMagnitude < 0.0001f) normal = -_direction;
        normal.Normalize();

        _direction = Vector3.Reflect(_direction, normal).normalized;
        _origin = transform.position;
    }

    private void BuildVisual(float size)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = Vector3.one * size;

        Collider col = visual.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Color color = _recipe.school != null ? _recipe.school.primaryColor : Color.white;
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

using UnityEngine;

// Spawné par SkillCaster pour les skills de type Projectile.
// Se détruit à l'impact ou quand la portée max est atteinte.
public class SkillProjectile : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private float _maxRange;
    private float _damage;
    private LayerMask _enemyLayer;
    private Vector3 _origin;
    private bool _hit;

    public void Init(Vector3 direction, float speed, float maxRange, float damage, LayerMask enemyLayer)
    {
        _direction = direction.normalized;
        _speed = speed;
        _maxRange = maxRange;
        _damage = damage;
        _enemyLayer = enemyLayer;
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

        // Détection manuelle (OverlapSphere) — pas de Rigidbody requis
        float hitRadius = transform.localScale.x * 0.55f;
        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius, _enemyLayer);
        foreach (Collider hit in hits)
        {
            HealthSystem hs = hit.GetComponent<HealthSystem>();
            if (hs == null || hs.IsDead) continue;

            hs.TakeDamage(_damage);
            _hit = true;
            Destroy(gameObject);
            return;
        }
    }
}

using System.Collections;
using UnityEngine;

// Statut Brûlure (école Ignis, voir CLAUDE.md table "The 7 Schools" — "Burn, melt, heat").
// Dégâts sur la durée, appliqué par SchoolEffectApplier après un hit direct/zone/impact d'un
// sort Ignis. Réapplication (Init appelé alors qu'un BurnStatus existe déjà sur la cible)
// rafraîchit la durée plutôt que d'empiler un second composant.
public class BurnStatus : MonoBehaviour
{
    private Coroutine _routine;

    public void Init(HealthSystem target, float damagePerTick, float duration, float tickInterval)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(TickLoop(target, damagePerTick, duration, tickInterval));
    }

    private IEnumerator TickLoop(HealthSystem target, float damagePerTick, float duration, float tickInterval)
    {
        float elapsed = 0f;
        float nextTick = tickInterval;
        while (elapsed < duration)
        {
            if (elapsed >= nextTick)
            {
                nextTick += tickInterval;
                if (target != null && !target.IsDead)
                    target.TakeDamage(damagePerTick);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(this);
    }
}

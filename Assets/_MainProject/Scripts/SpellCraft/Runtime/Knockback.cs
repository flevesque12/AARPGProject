using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Déplacement bref d'un NavMeshAgent sous l'effet d'un impact (école Terra, voir CLAUDE.md
// table "The 7 Schools" — "Heavy damage, walls"). Un NavMeshAgent actif ignore les
// déplacements manuels de son transform, donc l'agent est désactivé le temps de la poussée
// puis réactivé — EnemyAI n'a pas besoin d'être notifié, son SetDestination reprend la main
// normalement une fois l'agent réactivé.
public class Knockback : MonoBehaviour
{
    private const float PushDuration = 0.2f;

    public void Apply(NavMeshAgent agent, Vector3 impulse)
    {
        StopAllCoroutines();
        StartCoroutine(PushRoutine(agent, impulse));
    }

    private IEnumerator PushRoutine(NavMeshAgent agent, Vector3 impulse)
    {
        bool wasEnabled = agent.enabled;
        agent.enabled = false;

        float elapsed = 0f;
        while (elapsed < PushDuration)
        {
            float t = elapsed / PushDuration;
            agent.transform.position += impulse * (1f - t) * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        agent.enabled = wasEnabled;
        Destroy(this);
    }
}

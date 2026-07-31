using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Statut Ralentissement (école Aqua, voir CLAUDE.md table "The 7 Schools" — "Slow, control,
// freeze"). Réduit temporairement NavMeshAgent.speed puis restaure la vitesse d'origine. Ne
// s'applique qu'aux entités avec NavMeshAgent (voir CLAUDE.md, "NavMeshAgent for enemies
// only") — le joueur (CharacterController) n'a pas de hook de vitesse externe pour l'instant,
// donc un sort Aqua lancé sur le joueur (ex: futur ennemi Aqua) n'aurait actuellement aucun
// effet ; à revisiter si des ennemis castent des sorts un jour.
public class SlowStatus : MonoBehaviour
{
    private NavMeshAgent _agent;
    private float _originalSpeed;
    private Coroutine _routine;

    public void Init(NavMeshAgent agent, float speedMultiplier, float duration)
    {
        if (_routine == null)
        {
            _agent = agent;
            _originalSpeed = agent.speed;
        }
        else
        {
            StopCoroutine(_routine);
            agent.speed = _originalSpeed; // reset avant réapplication, évite l'empilement de ralentissements
        }

        _routine = StartCoroutine(RestoreAfter(speedMultiplier, duration));
    }

    private IEnumerator RestoreAfter(float speedMultiplier, float duration)
    {
        _agent.speed = _originalSpeed * speedMultiplier;
        yield return new WaitForSeconds(duration);

        if (_agent != null) _agent.speed = _originalSpeed;
        Destroy(this);
    }
}

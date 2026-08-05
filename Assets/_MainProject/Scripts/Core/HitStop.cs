using System.Collections;
using UnityEngine;

// Gel bref du temps à l'impact ("hit stop") — voir CLAUDE.md, table "Combat — Key Differences",
// "Hit stop on impactful spells only". Ce pattern existait en v3.1 (PostureSystem.
// StaggerCoroutine, archivé dans _Archive/) mais n'avait jamais été reconstruit côté v4.0/
// SpellCraft jusqu'à ce pass de game feel. Classe statique plutôt qu'un singleton MonoBehaviour
// (même raisonnement qu'EnvironmentState : les sorts sont instanciés dynamiquement, sans
// référence Inspector à un objet central) — un unique GameObject "runner" caché est créé à la
// volée au premier appel pour héberger la coroutine, Time.timeScale devant être restauré sur
// plusieurs frames réelles (WaitForSecondsRealtime, non affecté par le gel lui-même).
public static class HitStop
{
    private static HitStopRunner _runner;

    public static void Trigger(float duration = 0.06f, float scale = 0.05f)
    {
        if (_runner == null)
        {
            var go = new GameObject("HitStopRunner") { hideFlags = HideFlags.HideInHierarchy };
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<HitStopRunner>();
        }

        _runner.Run(duration, scale);
    }
}

// Composant interne de HitStop — jamais attaché manuellement, uniquement créé par
// HitStop.Trigger. Séparé de la classe statique parce qu'un MonoBehaviour ne peut pas être
// statique lui-même (il faut un GameObject vivant pour faire tourner la coroutine).
internal class HitStopRunner : MonoBehaviour
{
    private Coroutine _active;

    public void Run(float duration, float scale)
    {
        if (_active != null) StopCoroutine(_active);
        _active = StartCoroutine(Freeze(duration, scale));
    }

    private IEnumerator Freeze(float duration, float scale)
    {
        const float defaultFixedDelta = 0.02f;
        Time.timeScale = scale;
        Time.fixedDeltaTime = defaultFixedDelta * scale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDelta;
        _active = null;
    }
}

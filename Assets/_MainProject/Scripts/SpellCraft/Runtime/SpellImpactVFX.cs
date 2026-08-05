using UnityEngine;

// Burst de particules générique pour l'impact d'un sort — procédural (pas de prefab/texture),
// coloré par l'école du sort (SchoolData.primaryColor), même esprit que les visuels primitifs
// déjà construits en code par Projectile/Zone/Impact/AuraSpell. Volontairement générique/
// école-agnostique plutôt que de réutiliser les prefabs VFX Ignis existants (Prefabs/VFX/
// Ignis/) : ceux-ci n'existent que pour Ignis, un burst procédural uniforme couvre les 7 écoles
// dès maintenant sans attendre un pass d'art par école (Phase 8/11). Shader résolu avec repli
// (URP Particles/Unlit -> URP Unlit -> Sprites/Default) pour ne jamais rendre rose comme les
// matériaux Standard du modèle Wizard avant leur remplacement (voir migration status, "Player
// visual model swap") — Sprites/Default est garanti compatible SRP en dernier recours.
public static class SpellImpactVFX
{
    private static Shader _cachedShader;

    public static void Spawn(Vector3 position, Color color)
    {
        // Créé désactivé : ParticleSystem.playOnAwake est vrai par défaut, donc si le
        // GameObject était déjà actif, AddComponent déclencherait Play() immédiatement (via
        // OnEnable) avant que main.duration/emission/shape ci-dessous n'aient fini d'être
        // configurés — Unity logue alors une erreur "Setting the duration while system is
        // still playing is not supported" à chaque burst. Rester désactivé jusqu'à la
        // configuration complète évite ce déclenchement prématuré.
        var go = new GameObject("ImpactBurst");
        go.SetActive(false);
        go.transform.position = position;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.25f;
        main.loop = false;
        main.startLifetime = 0.35f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = color;
        main.gravityModifier = 0.3f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14, 20) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(ResolveShader());

        go.SetActive(true);
        ps.Play();
    }

    private static Shader ResolveShader()
    {
        if (_cachedShader != null) return _cachedShader;

        _cachedShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Sprites/Default");

        return _cachedShader;
    }
}

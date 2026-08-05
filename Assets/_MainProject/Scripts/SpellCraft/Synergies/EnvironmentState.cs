using System.Collections.Generic;
using UnityEngine;

// Registre statique des effets de terrain actifs (feu au sol, flaque d'eau, ...) laissés par
// les sorts Zone (voir CLAUDE.md, "Environmental synergies" et roadmap Phase 6, "Terrain
// effects (fire on ground, water puddle)"). Classe statique plutôt qu'un singleton
// MonoBehaviour — convention du projet ("No singletons — dependency injection via
// Inspector"), mais les sorts sont instanciés dynamiquement à runtime par SpellFactory et
// n'ont pas de référence Inspector à un objet central, donc un registre statique est le point
// d'accès le plus simple. Purement une fondation de données pour l'instant : la détection de
// synergie (Ignis+eau=Vapeur, Terra+feu=Magma, etc.) est l'item "Environmental synergies" de
// la Phase 8, pas géré ici — SynergyDetector.cs interrogera ce registre plus tard.
public static class EnvironmentState
{
    // Plafond du nombre de "stacks" qu'un patch peut accumuler (voir RegisterPatch) — évite un
    // empilement à l'infini si le joueur re-cast la même Zone en boucle au même endroit.
    public const float MaxIntensity = 3f;

    private class Patch
    {
        public TerrainType Type;
        public Vector3 Position;
        public float Radius;
        public float ExpiresAt;
        public float Intensity;
    }

    private static readonly List<Patch> _patches = new List<Patch>();

    // Retourne l'intensité résultante du patch (1 = nouveau patch créé ; >1 = un patch du même
    // type chevauchait déjà à cette position, intensifié au lieu de dupliqué — voir
    // conversation "terrain depth"/DOS2-style surfaces). Rayon et expiration du patch existant
    // sont étendus au passage plutôt que remplacés, pour ne jamais raccourcir un patch déjà là.
    public static float RegisterPatch(TerrainType type, Vector3 position, float radius, float duration)
    {
        PruneExpired();

        Patch existing = FindOverlapping(type, position, radius);
        if (existing != null)
        {
            existing.Intensity = Mathf.Min(MaxIntensity, existing.Intensity + 1f);
            existing.Radius = Mathf.Max(existing.Radius, radius);
            existing.ExpiresAt = Mathf.Max(existing.ExpiresAt, Time.time + duration);
            return existing.Intensity;
        }

        _patches.Add(new Patch { Type = type, Position = position, Radius = radius, ExpiresAt = Time.time + duration, Intensity = 1f });
        return 1f;
    }

    public static bool HasPatchAt(Vector3 position, TerrainType type, float queryRadius = 0.5f)
    {
        PruneExpired();
        return FindOverlapping(type, position, queryRadius) != null;
    }

    // Intensité du patch de ce type le plus proche de cette position (0 si aucun) — lu par
    // ZoneSpell pour intensifier ses dégâts quand il re-marque un terrain déjà actif.
    public static float GetIntensityAt(Vector3 position, TerrainType type, float queryRadius = 0.5f)
    {
        PruneExpired();
        Patch patch = FindOverlapping(type, position, queryRadius);
        return patch != null ? patch.Intensity : 0f;
    }

    private static Patch FindOverlapping(TerrainType type, Vector3 position, float radius)
    {
        foreach (var patch in _patches)
        {
            if (patch.Type != type) continue;
            float combinedRadius = patch.Radius + radius;
            if ((patch.Position - position).sqrMagnitude <= combinedRadius * combinedRadius)
                return patch;
        }
        return null;
    }

    // École → type de terrain laissé au sol (voir CLAUDE.md, table "The 7 Schools" — colonne
    // "World role"). Lux et Ferrum n'ont pas de case : ils ne marquent pas le terrain dans le
    // tableau de synergies actuel, `false` pour eux est donc correct, pas un oubli.
    public static bool TryGetTerrainType(SpellSchool school, out TerrainType type)
    {
        switch (school)
        {
            case SpellSchool.Ignis: type = TerrainType.Fire; return true;
            case SpellSchool.Aqua: type = TerrainType.Water; return true;
            case SpellSchool.Terra: type = TerrainType.LooseEarth; return true;
            case SpellSchool.Ventus: type = TerrainType.Wind; return true;
            case SpellSchool.Umbra: type = TerrainType.Shadow; return true;
            default: type = default; return false;
        }
    }

    private static void PruneExpired()
    {
        _patches.RemoveAll(p => p.ExpiresAt <= Time.time);
    }
}

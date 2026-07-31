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
    private class Patch
    {
        public TerrainType Type;
        public Vector3 Position;
        public float Radius;
        public float ExpiresAt;
    }

    private static readonly List<Patch> _patches = new List<Patch>();

    public static void RegisterPatch(TerrainType type, Vector3 position, float radius, float duration)
    {
        _patches.Add(new Patch { Type = type, Position = position, Radius = radius, ExpiresAt = Time.time + duration });
    }

    public static bool HasPatchAt(Vector3 position, TerrainType type, float queryRadius = 0.5f)
    {
        PruneExpired();
        foreach (var patch in _patches)
        {
            if (patch.Type != type) continue;
            float combinedRadius = patch.Radius + queryRadius;
            if ((patch.Position - position).sqrMagnitude <= combinedRadius * combinedRadius)
                return true;
        }
        return false;
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

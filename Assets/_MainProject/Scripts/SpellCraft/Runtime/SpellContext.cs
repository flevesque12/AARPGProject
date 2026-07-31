using UnityEngine;

// Conteneur de données minimal pour un sort instancié — porté par le GameObject racine créé
// par SpellFactory. Les comportements par forme (ProjectileSpell, ZoneSpell, AuraSpell,
// ImpactSpell) lisent ces données depuis ce même GameObject plutôt que de dupliquer
// origine/direction/recipe chacun de leur côté.
public class SpellContext : MonoBehaviour
{
    public SpellRecipe Recipe { get; private set; }
    public GameObject Caster { get; private set; }
    public Vector3 Origin { get; private set; }
    public Vector3 Direction { get; private set; }

    // Accumulateurs alimentés par les runes modificatrices via ISpellModifier.OnSpawn (voir
    // ModifierProcessor), lus par les formes de base juste après. Typés plutôt qu'un bag
    // générique clé-valeur — chaque valeur correspond à une rune précise de l'item roadmap
    // "4 basic modifier runes" (Bounce/Split/Persist/Expand). Multiplicateurs à 1 par défaut
    // (neutre), accumulateurs à 0 (aucun effet tant qu'aucune rune ne les alimente).
    public int BounceCount { get; private set; }
    public int ExtraProjectileCount { get; private set; }
    public float DurationMultiplier { get; private set; } = 1f;
    public float RadiusMultiplier { get; private set; } = 1f;

    public void Initialize(SpellRecipe recipe, GameObject caster, Vector3 origin, Vector3 direction)
    {
        Recipe = recipe;
        Caster = caster;
        Origin = origin;
        Direction = direction;
    }

    public void AddBounces(int count) => BounceCount += count;
    public void AddExtraProjectiles(int count) => ExtraProjectileCount += count;
    public void MultiplyDuration(float multiplier) => DurationMultiplier *= multiplier;
    public void MultiplyRadius(float multiplier) => RadiusMultiplier *= multiplier;
}

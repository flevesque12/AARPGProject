using UnityEngine;

// Conteneur de données minimal pour un sort instancié — porté par le GameObject racine créé
// par SpellFactory. Les comportements par forme (ProjectileSpell, ZoneSpell, AuraSpell,
// ImpactSpell — Phase 6, item "4 base forms functional", pas encore construits) liront ces
// données depuis ce même GameObject plutôt que de dupliquer origine/direction/recipe chacun
// de leur côté.
public class SpellContext : MonoBehaviour
{
    public SpellRecipe Recipe { get; private set; }
    public GameObject Caster { get; private set; }
    public Vector3 Origin { get; private set; }
    public Vector3 Direction { get; private set; }

    public void Initialize(SpellRecipe recipe, GameObject caster, Vector3 origin, Vector3 direction)
    {
        Recipe = recipe;
        Caster = caster;
        Origin = origin;
        Direction = direction;
    }
}

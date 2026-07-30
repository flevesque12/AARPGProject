// Contrat appliqué par une rune modificatrice sur un sort au moment de son instanciation.
// RuneModifier l'implémente directement (voir Data/RuneModifier.cs) — les runes concrètes
// (Bounce, Homing, Split, Persist, ... — pas encore créées, item "16 modifier runes" de la
// roadmap Phase 6/8) override OnSpawn pour leur comportement propre.
public interface ISpellModifier
{
    void OnSpawn(SpellContext context);
}

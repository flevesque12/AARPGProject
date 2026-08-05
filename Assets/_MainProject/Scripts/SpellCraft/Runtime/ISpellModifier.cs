// Contrat appliqué par une rune modificatrice sur un sort au moment de son instanciation.
// RuneModifier l'implémente directement (voir Data/RuneModifier.cs) — les runes concrètes
// (Bounce, Homing, Split, Persist, ... — pas encore créées, item "16 modifier runes" de la
// roadmap Phase 6/8) override OnSpawn pour leur comportement propre.
// `intensity` (voir RuneSlot.cs, "continuous tuning") : 1.0 = valeur d'auteur telle qu'écrite
// sur l'asset, en dessous/au-dessus scale linéairement l'effet — chaque rune concrète décide
// comment interpréter ce facteur pour son propre paramètre.
public interface ISpellModifier
{
    void OnSpawn(SpellContext context, float intensity);
}

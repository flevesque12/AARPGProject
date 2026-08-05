using UnityEngine;

// Rune Temps "Persist" (voir CLAUDE.md, "16 modifier runes" — Time: Persist). Prolonge la
// durée d'un effet (Zone, Aura) via SpellContext.DurationMultiplier. Sans effet sur
// Projectile/Impact, qui n'ont pas de durée (hit instantané) — pas un bug, même logique de
// champ non pertinent selon la forme que le reste de SpellCraft.
[CreateAssetMenu(fileName = "NewRune_Persist", menuName = "Glyphes/Spell Crafting/Runes/Persist (Time)")]
public class PersistRune : RuneModifier
{
    [Header("Persist")]
    [Tooltip("Multiplicateur appliqué à la durée de l'effet. Ex: 2 = durée doublée.")]
    public float durationMultiplier = 2f;

    public override void OnSpawn(SpellContext context, float intensity)
    {
        // Interpolation autour du point neutre 1 (durée inchangée) plutôt qu'un simple
        // durationMultiplier * intensity : sinon intensité 0 annulerait complètement la durée
        // au lieu de simplement "moins de bonus". Ex: durationMultiplier=2, intensity=1 -> 2
        // (valeur d'auteur inchangée), intensity=0.25 -> 1.25, intensity=2 -> 3.
        float effectiveMultiplier = 1f + (durationMultiplier - 1f) * intensity;
        context.MultiplyDuration(effectiveMultiplier);
    }
}

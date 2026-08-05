using UnityEngine;

// Rune Forme "Expand" (voir CLAUDE.md, "16 modifier runes" — Shape: Expand). Augmente le rayon
// d'effet (Zone, Impact) via SpellContext.RadiusMultiplier. Sans effet sur Projectile/Aura,
// qui n'utilisent pas de rayon d'aire — pas un bug, même logique de champ non pertinent selon
// la forme que le reste de SpellCraft.
[CreateAssetMenu(fileName = "NewRune_Expand", menuName = "Glyphes/Spell Crafting/Runes/Expand (Shape)")]
public class ExpandRune : RuneModifier
{
    [Header("Expand")]
    [Tooltip("Multiplicateur appliqué au rayon d'effet. Ex: 1.5 = rayon +50%.")]
    public float radiusMultiplier = 1.5f;

    public override void OnSpawn(SpellContext context, float intensity)
    {
        // Même interpolation autour du point neutre 1 que PersistRune — voir son commentaire.
        float effectiveMultiplier = 1f + (radiusMultiplier - 1f) * intensity;
        context.MultiplyRadius(effectiveMultiplier);
    }
}

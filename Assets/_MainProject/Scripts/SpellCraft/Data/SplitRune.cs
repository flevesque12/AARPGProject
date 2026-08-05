using UnityEngine;

// Rune Forme "Split" (voir CLAUDE.md, "16 modifier runes" — Shape: Split). Tire des
// projectiles supplémentaires en éventail au lieu d'un seul. N'a d'effet que sur
// ProjectileSpell, qui lit SpellContext.ExtraProjectileCount à l'initialisation pour faire
// naître les projectiles supplémentaires.
[CreateAssetMenu(fileName = "NewRune_Split", menuName = "Glyphes/Spell Crafting/Runes/Split (Shape)")]
public class SplitRune : RuneModifier
{
    [Header("Split")]
    [Tooltip("Nombre de projectiles supplémentaires (en plus de l'original).")]
    public int extraProjectiles = 2;

    public override void OnSpawn(SpellContext context, float intensity)
    {
        int effectiveExtra = Mathf.Max(0, Mathf.RoundToInt(extraProjectiles * intensity));
        context.AddExtraProjectiles(effectiveExtra);
    }
}

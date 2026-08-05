using UnityEngine;

// Rune Trajectoire "Bounce" (voir CLAUDE.md, "16 modifier runes" — Trajectory: Bounce). Le
// projectile rebondit sur ses cibles au lieu d'être détruit au premier impact. N'a d'effet que
// sur ProjectileSpell, qui lit SpellContext.BounceCount pour décider de rebondir ou se
// détruire (pas de hook OnHit générique — choix pragmatique pour un seul cas d'usage plutôt
// qu'un bus d'événements, voir SpellContext.cs).
[CreateAssetMenu(fileName = "NewRune_Bounce", menuName = "Glyphes/Spell Crafting/Runes/Bounce (Trajectory)")]
public class BounceRune : RuneModifier
{
    [Header("Bounce")]
    [Tooltip("Nombre de rebonds avant destruction du projectile.")]
    public int bounceCount = 2;

    public override void OnSpawn(SpellContext context, float intensity)
    {
        int effectiveBounces = Mathf.Max(0, Mathf.RoundToInt(bounceCount * intensity));
        context.AddBounces(effectiveBounces);
    }
}

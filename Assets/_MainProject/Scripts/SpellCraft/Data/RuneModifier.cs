using UnityEngine;

// Base abstraite — chaque rune concrète (Bounce, Homing, Split, Persist, ...) sera une
// sous-classe ScriptableObject qui override OnSpawn pour son comportement propre. Aucune
// rune concrète n'existe encore : c'est l'item "4 basic modifier runes" de la roadmap
// Phase 6 (puis "16 modifier runes" en Phase 8) — cette classe pose seulement la fondation
// data + le contrat de compatibilité/coût partagés par toutes les runes.
public abstract class RuneModifier : ScriptableObject, ISpellModifier
{
    [Header("Identité")]
    public string runeName = "New Rune";
    public RuneCategory category;
    [TextArea] public string description;

    [Header("Coût — multiplicateurs appliqués au coût courant du sort, à intensité 1.0")]
    [Tooltip("Ex: 0.5 = +50% du coût en Mana accumulé jusqu'ici, à intensité 1.0 (voir RuneSlot).")]
    public float manaCostMultiplier = 0.5f;
    [Tooltip("Ex: 0.15 = +15% du cooldown accumulé jusqu'ici, à intensité 1.0 (voir RuneSlot).")]
    public float cooldownMultiplier = 0.15f;

    [Header("Compatibilité")]
    [Tooltip("Runes ne pouvant pas être combinées avec celle-ci sur le même SpellRecipe.")]
    public RuneModifier[] incompatibleWith;

    public bool IsIncompatibleWith(RuneModifier other)
    {
        if (other == null || incompatibleWith == null) return false;
        foreach (var rune in incompatibleWith)
            if (rune == other) return true;
        return false;
    }

    // Coût effectif une fois scalé par l'intensité du RuneSlot (voir RuneSlot.cs) — lu par
    // SpellRecipe.ManaCost/CooldownTime. Linéaire : intensité 1.0 = valeur d'auteur inchangée
    // (rétrocompatible avec les recettes existantes), 0.25 = 25% du coût, 2.0 = 200%.
    public float EffectiveManaCostMultiplier(float intensity) => manaCostMultiplier * intensity;
    public float EffectiveCooldownMultiplier(float intensity) => cooldownMultiplier * intensity;

    // Appelé par ModifierProcessor à l'instanciation du sort (SpellFactory.CreateSpell).
    // No-op par défaut — chaque rune concrète override selon son effet, en scalant son propre
    // paramètre par `intensity` (1.0 = valeur d'auteur, voir RuneSlot.cs).
    public virtual void OnSpawn(SpellContext context, float intensity) { }
}

using UnityEngine;

// Une rune équipée sur un SpellRecipe + son intensité (voir conversation "continuous tuning",
// inspirée de la sélection de Magnitude/Duration/Area des Elder Scrolls). L'intensité vit ici,
// pas sur le RuneModifier lui-même : le même asset RuneModifier (ex. BounceRune) peut être
// équipé à des intensités différentes selon la recette. À 1.0 (valeur par défaut), l'effet et
// le coût correspondent exactement aux valeurs de l'ancien système tout-ou-rien (pas de rune
// existante à retuner) ; en dessous c'est plus faible et moins cher, au-dessus plus fort et
// plus cher.
[System.Serializable]
public struct RuneSlot
{
    public const float MinIntensity = 0.25f;
    public const float MaxIntensity = 2f;

    public RuneModifier rune;
    [Range(MinIntensity, MaxIntensity)] public float intensity;

    public RuneSlot(RuneModifier rune, float intensity = 1f)
    {
        this.rune = rune;
        this.intensity = Mathf.Clamp(intensity, MinIntensity, MaxIntensity);
    }
}

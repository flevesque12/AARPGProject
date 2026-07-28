using UnityEngine;
using UnityEngine.Serialization;

public enum SkillSchool { Ignis, Aqua, Terra, Ventus, Lux, Umbra, Ferrum }
public enum SkillType { Projectile, AoE, PersistentZone, DelayedAoE }

[CreateAssetMenu(fileName = "NewSkillData", menuName = "ARPG/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Identité")]
    public string skillName = "New Skill";
    public Sprite icon;
    [TextArea] public string description;
    public SkillSchool school;
    public SkillType skillType;

    [Header("Ressources")]
    [FormerlySerializedAs("staminaCost")]
    public float manaCost = 20f;
    public float cooldown = 2f;

    [Header("Combat")]
    public float baseDamage = 30f;
    public float range = 8f;           // Portée max du cast (ou du projectile)
    public float radius = 2f;          // Rayon AoE / zone

    [Header("Timing")]
    public float castTime = 0f;        // Windup / telegraph avant l'effet
    public float duration = 3f;        // PersistentZone : durée de la zone (s)
    public float tickInterval = 0.5f;  // PersistentZone : intervalle de dégâts (s)

    [Header("Projectile")]
    public float projectileSpeed = 18f;
    public float projectileSize = 0.3f;

    [Header("Couleur procédurale")]
    public Color skillColor = new Color(1f, 0.35f, 0f);   // Orange Ignis par défaut

    [Header("VFX (optionnel — fallback procédural si null)")]
    public GameObject projectilePrefab;
    public GameObject impactVFXPrefab;
    public GameObject zonePrefab;
}

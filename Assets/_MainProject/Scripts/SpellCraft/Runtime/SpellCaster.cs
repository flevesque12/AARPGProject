using System;
using UnityEngine;

// Composant Player — orchestre le cast des sorts SpellCraft (SpellRecipe/SpellFactory),
// équivalent v4.0 de Skills/SkillCaster.cs mais pour le nouveau pipeline de crafting plutôt
// que les 4 SkillData Ignis legacy. Démarre à 2 emplacements actifs (voir CLAUDE.md,
// "Savoir Magique" — 2→3→4 via la progression, pas encore implémentée : ce composant expose
// juste un tableau de taille fixe pour l'instant). Appelé par PlayerCombat.TryCastSlot pour
// les touches 1-2 (touches 3-4 restent routées vers l'ancien SkillCaster tant qu'il n'est pas
// archivé — choix utilisateur du 2026-07-30).
public class SpellCaster : MonoBehaviour
{
    [Header("Slots (0 = touche 1, 1 = touche 2)")]
    [SerializeField] private SpellRecipe[] _slots = new SpellRecipe[2];

    [Header("Références")]
    [SerializeField] private PlayerController _player;
    [SerializeField] private ManaSystem _mana;
    [SerializeField] private PlayerCombat _playerCombat;
    [SerializeField] private SprintController _sprint;
    [SerializeField] private LayerMask _enemyLayer;

    private float[] _cooldowns;

    // (slotIndex, cooldownRemaining, cooldownTotal) — pour l'UI future
    public event Action<int, float, float> OnCooldownChanged;

    private void Awake()
    {
        if (_player == null) _player = GetComponent<PlayerController>();
        if (_mana == null) _mana = GetComponent<ManaSystem>();
        if (_playerCombat == null) _playerCombat = GetComponent<PlayerCombat>();
        if (_sprint == null) _sprint = GetComponent<SprintController>();

        _cooldowns = new float[_slots.Length];
    }

    private void Update()
    {
        for (int i = 0; i < _cooldowns.Length; i++)
        {
            if (_cooldowns[i] <= 0f) continue;
            _cooldowns[i] -= Time.deltaTime;
            if (_cooldowns[i] < 0f) _cooldowns[i] = 0f;
            OnCooldownChanged?.Invoke(i, _cooldowns[i], _slots[i] != null ? _slots[i].CooldownTime : 0f);
        }
    }

    public void TryCastSpell(int slotIndex)
    {
        if (!CanCastSlot(slotIndex)) return;
        if (_playerCombat != null && !_playerCombat.CanAct) return; // bloqué pendant l'esquive

        SpellRecipe recipe = _slots[slotIndex];

        _sprint?.ForceStopSprint();
        _mana?.SetInCombat();
        _mana?.ConsumeMana(recipe.ManaCost);
        _cooldowns[slotIndex] = recipe.CooldownTime;
        OnCooldownChanged?.Invoke(slotIndex, recipe.CooldownTime, recipe.CooldownTime);

        var (origin, direction) = ComputeCastGeometry(recipe);
        SpellFactory.CreateSpell(recipe, gameObject, origin, direction, _enemyLayer);
    }

    public SpellRecipe GetSlot(int index) =>
        index >= 0 && index < _slots.Length ? _slots[index] : null;

    // Assigne une recette au slot — appelé par CraftingPanel.SaveToSlot (Grimoire) pour
    // enregistrer un sort composé par le joueur, en mémoire (ScriptableObject.CreateInstance,
    // pas un asset sur disque — voir CraftingPanel.cs).
    public void SetSlot(int index, SpellRecipe recipe)
    {
        if (index < 0 || index >= _slots.Length) return;
        _slots[index] = recipe;
    }

    public float GetCooldownRemaining(int slotIndex) =>
        slotIndex >= 0 && slotIndex < _cooldowns.Length ? _cooldowns[slotIndex] : 0f;

    public bool IsReady(int slotIndex) =>
        slotIndex >= 0 && slotIndex < _cooldowns.Length && _cooldowns[slotIndex] <= 0f;

    /// <summary>
    /// Vrai si le slot a une recette assignée, n'est pas en cooldown, et que le mana
    /// disponible couvre son coût. Même contrat que SkillCaster.CanCastSlot.
    /// </summary>
    public bool CanCastSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Length) return false;
        SpellRecipe recipe = _slots[slotIndex];
        if (recipe == null || recipe.baseForm == null) return false;
        if (_cooldowns[slotIndex] > 0f) return false;
        if (_mana != null && !_mana.HasEnoughMana(recipe.ManaCost)) return false;
        return true;
    }

    // Calcule le point d'origine et la direction du sort selon sa forme de base — reprend
    // GetCastTarget de SkillCaster.cs (visée via PlayerController.AimWorldPosition, clampée à
    // la portée/au rayon de la forme). Projectile part du joueur vers la cible ; Zone/Impact
    // sont centrés sur le point visé ; Aura part du joueur (son visuel suit le caster).
    private (Vector3 origin, Vector3 direction) ComputeCastGeometry(SpellRecipe recipe)
    {
        Vector3 spawnPos = transform.position + Vector3.up;
        float maxRange = recipe.baseForm.range > 0f ? recipe.baseForm.range : recipe.baseForm.radius;
        Vector3 target = GetCastTarget(maxRange);

        Vector3 direction = target - spawnPos;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
        direction.Normalize();

        return recipe.baseForm.baseForm switch
        {
            SpellBaseForm.Zone => (target, direction),
            SpellBaseForm.Impact => (target, direction),
            SpellBaseForm.Aura => (transform.position, direction),
            _ => (spawnPos, direction), // Projectile
        };
    }

    private Vector3 GetCastTarget(float maxRange)
    {
        Vector3 aim = _player != null
            ? _player.AimWorldPosition
            : transform.position + transform.forward * maxRange;

        Vector3 flatDir = aim - transform.position;
        flatDir.y = 0f;
        if (maxRange > 0f && flatDir.magnitude > maxRange)
            aim = transform.position + flatDir.normalized * maxRange;

        aim.y = transform.position.y;
        return aim;
    }
}

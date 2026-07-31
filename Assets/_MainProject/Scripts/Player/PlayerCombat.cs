using UnityEngine;

/// <summary>
/// Orchestrateur de combat v4.0 — cast des sorts (slots 1-4) et pont avec le facing
/// du joueur. CombatController (dodge/bloc/riposte/sprint/attaque de base v3.1) a
/// été archivé (Phase 5) ; esquive et sprint sont maintenant câblés directement
/// depuis GameInput vers DodgeRoll/SprintController. Cette classe ne s'occupe QUE
/// du cast. Attacher au joueur.
///
/// Touches 1-2 (slotIndex 0-1) → SpellCaster (nouveau pipeline SpellCraft/SpellRecipe).
/// Touches 3-4 (slotIndex 2-3) → SkillCaster (ancien pipeline v3.1/SkillData), tant que ce
/// dernier n'est pas archivé — choix utilisateur du 2026-07-30, conforme au GDD (le joueur
/// démarre avec 2 emplacements de sort, voir "Savoir Magique").
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    private const int NewSystemSlotCount = 2;

    [Header("Références")]
    [SerializeField] private PlayerController _player;
    [SerializeField] private DodgeRoll _dodgeRoll;
    [SerializeField] private SkillCaster _skillCaster;
    [SerializeField] private SpellCaster _spellCaster;

    [Header("Visée pendant le cast")]
    [Tooltip("Durée pendant laquelle le joueur reste tourné vers le curseur après un cast (FacingMode.Aim), avant de revenir à FacingMode.Movement.")]
    [SerializeField] private float aimHoldDuration = 0.3f;

    private float aimTimer;

    /// <summary>Le joueur peut-il agir (lancer un sort) ? Faux pendant l'esquive.</summary>
    public bool CanAct => _dodgeRoll == null || !_dodgeRoll.IsDodging;

    private void Awake()
    {
        if (_player == null) _player = GetComponent<PlayerController>();
        if (_dodgeRoll == null) _dodgeRoll = GetComponent<DodgeRoll>();
        if (_skillCaster == null) _skillCaster = GetComponent<SkillCaster>();
        if (_spellCaster == null) _spellCaster = GetComponent<SpellCaster>();
    }

    private void Update()
    {
        if (aimTimer <= 0f) return;

        aimTimer -= Time.deltaTime;
        if (aimTimer <= 0f)
            _player.SetFacingMode(FacingMode.Movement);
    }

    /// <summary>
    /// Appelé par GameInput sur l'appui des touches 1-4. Fait pivoter le joueur vers
    /// le curseur (FacingMode.Aim) pendant le cast, puis le rend au mouvement libre.
    /// Route vers SpellCaster (slotIndex 0-1, touches 1-2) ou SkillCaster (slotIndex 2-3,
    /// touches 3-4) — voir la doc de classe.
    /// </summary>
    public void TryCastSlot(int slotIndex)
    {
        if (!CanAct) return;

        if (slotIndex < NewSystemSlotCount)
        {
            if (_spellCaster == null || !_spellCaster.CanCastSlot(slotIndex)) return;

            _player.SetFacingMode(FacingMode.Aim);
            aimTimer = aimHoldDuration;

            _spellCaster.TryCastSpell(slotIndex);
            return;
        }

        if (_skillCaster == null || !_skillCaster.CanCastSlot(slotIndex)) return;

        _player.SetFacingMode(FacingMode.Aim);
        aimTimer = aimHoldDuration;

        _skillCaster.TryCastSkill(slotIndex);
    }
}

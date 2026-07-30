using UnityEngine;

/// <summary>
/// Orchestrateur de combat v4.0 — cast des sorts (slots 1-4) et pont avec le facing
/// du joueur. CombatController (dodge/bloc/riposte/sprint/attaque de base v3.1) a
/// été archivé (Phase 5) ; esquive et sprint sont maintenant câblés directement
/// depuis GameInput vers DodgeRoll/SprintController. Cette classe ne s'occupe QUE
/// du cast. Attacher au joueur.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private PlayerController _player;
    [SerializeField] private DodgeRoll _dodgeRoll;
    [SerializeField] private SkillCaster _skillCaster;

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
    /// </summary>
    public void TryCastSlot(int slotIndex)
    {
        if (!CanAct || _skillCaster == null) return;
        if (!_skillCaster.CanCastSlot(slotIndex)) return;

        _player.SetFacingMode(FacingMode.Aim);
        aimTimer = aimHoldDuration;

        _skillCaster.TryCastSkill(slotIndex);
    }
}

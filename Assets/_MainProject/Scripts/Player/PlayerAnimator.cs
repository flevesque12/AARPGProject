using UnityEngine;

/// <summary>
/// Drives the Wizard model's Animator from player state: locomotion blend
/// (Idle/Walk/Run, from PlayerController's speed), the one-shot cast gesture
/// (from PlayerCombat.OnSpellCast) and the dodge/teleport gesture (from
/// DodgeRoll.OnDodgeStart).
///
/// REQUIRES: PlayerController on the same GameObject, Animator on the model child.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator modelAnimator;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private DodgeRoll dodgeRoll;

    [Header("Tuning")]
    [Tooltip("Vitesse de lissage du paramètre Speed envoyé à l'Animator.")]
    [SerializeField] private float speedSmoothing = 10f;

    [Tooltip("Durée brute du clip Teleport (état Dodge). Sert à calculer le multiplicateur de vitesse pour que l'animation dure exactement DodgeRoll.DodgeDuration.")]
    [SerializeField] private float dodgeClipLength = 0.867f;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int CastParam = Animator.StringToHash("Cast");
    private static readonly int CastSlotParam = Animator.StringToHash("CastSlot");
    private static readonly int DodgeParam = Animator.StringToHash("Dodge");
    private static readonly int DodgeSpeedMultParam = Animator.StringToHash("DodgeSpeedMult");

    private float smoothedSpeed;

    private void Awake()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerCombat == null) playerCombat = GetComponent<PlayerCombat>();
        if (dodgeRoll == null) dodgeRoll = GetComponent<DodgeRoll>();
        if (modelAnimator == null) modelAnimator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (playerCombat != null) playerCombat.OnSpellCast += OnSpellCast;
        if (dodgeRoll != null) dodgeRoll.OnDodgeStart += OnDodgeStart;
    }

    private void OnDisable()
    {
        if (playerCombat != null) playerCombat.OnSpellCast -= OnSpellCast;
        if (dodgeRoll != null) dodgeRoll.OnDodgeStart -= OnDodgeStart;
    }

    private void Update()
    {
        if (modelAnimator == null || playerController == null) return;

        float targetSpeed = playerController.CurrentSpeed;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, targetSpeed, speedSmoothing * Time.deltaTime);
        modelAnimator.SetFloat(SpeedParam, smoothedSpeed);
    }

    /// <summary>Slot 0-3 → geste de cast dédié (Cast_Slot0..3 dans Wizard.controller).</summary>
    private void OnSpellCast(int slotIndex)
    {
        if (modelAnimator == null) return;

        modelAnimator.SetInteger(CastSlotParam, slotIndex);
        modelAnimator.SetTrigger(CastParam);
    }

    /// <summary>
    /// Geste de téléportation (Dodge) — vitesse de lecture recalée sur DodgeRoll.DodgeDuration
    /// pour que le clip Teleport dure exactement aussi longtemps que l'esquive elle-même.
    /// </summary>
    private void OnDodgeStart()
    {
        if (modelAnimator == null || dodgeRoll == null) return;

        float speedMult = dodgeClipLength / Mathf.Max(0.01f, dodgeRoll.DodgeDuration);
        modelAnimator.SetFloat(DodgeSpeedMultParam, speedMult);
        modelAnimator.SetTrigger(DodgeParam);
    }
}

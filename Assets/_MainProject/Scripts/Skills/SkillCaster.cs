using System;
using System.Collections;
using UnityEngine;

// Composant Player — gère le cast des 4 slots de skills actifs (coûts en Mana).
// Appelé par PlayerCombat.TryCastSlot (lui-même branché sur GameInput, touches 1-4).
public class SkillCaster : MonoBehaviour
{
    [Header("Slots  (0 = touche 1, 1 = touche 2, 2 = touche 3, 3 = touche 4)")]
    [SerializeField] private SkillData[] _slots = new SkillData[4];

    [Header("Références")]
    [SerializeField] private PlayerController _player;
    [SerializeField] private ManaSystem _mana;
    [SerializeField] private PlayerCombat _playerCombat;
    [SerializeField] private SprintController _sprint;
    [SerializeField] private LayerMask _enemyLayer;

    [Header("Visée")]
    [Tooltip("Décalage vertical du point d'origine du projectile au-dessus du pivot du " +
        "lanceur — voir SpellCaster._castHeightOffset (bug fix 2026-08-06), même cause : le " +
        "pivot du joueur est déjà à hauteur de poitrine, +1 unité complète envoyait le " +
        "projectile au-dessus de la tête des cibles.")]
    [SerializeField] private float _castHeightOffset = 0.3f;

    private float[] _cooldowns;

    // (slotIndex, cooldownRemaining, cooldownTotal) — pour l'UI future
    public event Action<int, float, float> OnCooldownChanged;

    private void Awake()
    {
        if (_player == null) _player = GetComponent<PlayerController>();
        if (_mana == null) _mana = GetComponent<ManaSystem>();
        if (_playerCombat == null) _playerCombat = GetComponent<PlayerCombat>();
        if (_sprint == null) _sprint = GetComponent<SprintController>();

        _cooldowns = new float[4];
    }

    private void Update()
    {
        for (int i = 0; i < 4; i++)
        {
            if (_cooldowns[i] <= 0f) continue;
            _cooldowns[i] -= Time.deltaTime;
            if (_cooldowns[i] < 0f) _cooldowns[i] = 0f;
            OnCooldownChanged?.Invoke(i, _cooldowns[i], _slots[i]?.cooldown ?? 0f);
        }
    }

    // Appelé par GameInput (touches 1-4), ou par PlayerCombat.TryCastSlot
    public void TryCastSkill(int slotIndex)
    {
        if (!CanCastSlot(slotIndex)) return;
        if (_playerCombat != null && !_playerCombat.CanAct) return; // bloqué pendant l'esquive

        SkillData skill = _slots[slotIndex];

        _sprint?.ForceStopSprint();
        _mana?.SetInCombat();
        _mana?.ConsumeMana(skill.manaCost);
        _cooldowns[slotIndex] = skill.cooldown;
        OnCooldownChanged?.Invoke(slotIndex, skill.cooldown, skill.cooldown);

        Vector3 target = GetCastTarget(skill.range);

        switch (skill.skillType)
        {
            case SkillType.Projectile:     StartCoroutine(CastProjectile(skill, target));  break;
            case SkillType.AoE:            StartCoroutine(CastAoE(skill, target));         break;
            case SkillType.PersistentZone: StartCoroutine(CastZone(skill, target));        break;
            case SkillType.DelayedAoE:     StartCoroutine(CastDelayedAoE(skill, target));  break;
        }
    }

    public SkillData GetSlot(int index) =>
        index >= 0 && index < _slots.Length ? _slots[index] : null;

    public float GetCooldownRemaining(int slotIndex) =>
        slotIndex >= 0 && slotIndex < 4 ? _cooldowns[slotIndex] : 0f;

    public bool IsReady(int slotIndex) =>
        slotIndex >= 0 && slotIndex < 4 && _cooldowns[slotIndex] <= 0f;

    /// <summary>
    /// Vrai si le slot a un sort assigné, n'est pas en cooldown, et que le mana
    /// disponible couvre son coût. Utilisé par PlayerCombat avant de lancer le cast
    /// (et par la future HUD pour griser les slots inabordables).
    /// </summary>
    public bool CanCastSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return false;
        SkillData skill = _slots[slotIndex];
        if (skill == null) return false;
        if (_cooldowns[slotIndex] > 0f) return false;
        if (_mana != null && !_mana.HasEnoughMana(skill.manaCost)) return false;
        return true;
    }

    // ========================================
    // COMPORTEMENTS PAR TYPE
    // ========================================

    private IEnumerator CastProjectile(SkillData skill, Vector3 target)
    {
        Vector3 spawnPos = transform.position + Vector3.up * _castHeightOffset;
        Vector3 dir = target - spawnPos;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
        dir.Normalize();

        GameObject proj = skill.projectilePrefab != null
            ? Instantiate(skill.projectilePrefab, spawnPos, Quaternion.identity)
            : CreatePrimitive(PrimitiveType.Sphere, spawnPos, Vector3.one * skill.projectileSize, skill.skillColor);

        // Supprimer le collider existant — SkillProjectile utilise OverlapSphere
        Collider col = proj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        SkillProjectile sp = proj.AddComponent<SkillProjectile>();
        sp.Init(dir, skill.projectileSpeed, skill.range, skill.baseDamage, _enemyLayer);
        yield break;
    }

    private IEnumerator CastAoE(SkillData skill, Vector3 target)
    {
        if (skill.castTime > 0f)
            yield return StartCoroutine(CircleIndicator(target, skill.radius, skill.skillColor, skill.castTime));

        ApplyAoE(skill, target);
        StartCoroutine(ImpactRing(target, skill.radius, skill.skillColor));
    }

    private IEnumerator CastZone(SkillData skill, Vector3 target)
    {
        GameObject zone = skill.zonePrefab != null
            ? Instantiate(skill.zonePrefab, target, Quaternion.identity)
            : CreatePrimitive(PrimitiveType.Cylinder, target,
                new Vector3(skill.radius * 2f, 0.05f, skill.radius * 2f),
                new Color(skill.skillColor.r, skill.skillColor.g, skill.skillColor.b, 0.4f));

        Collider zoneCol = zone.GetComponent<Collider>();
        if (zoneCol != null) Destroy(zoneCol);

        float elapsed = 0f;
        float nextTick = 0f;
        while (elapsed < skill.duration)
        {
            if (elapsed >= nextTick)
            {
                nextTick += skill.tickInterval;
                ApplyAoE(skill, target);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (zone != null) Destroy(zone);
    }

    private IEnumerator CastDelayedAoE(SkillData skill, Vector3 target)
    {
        yield return StartCoroutine(CircleIndicator(target, skill.radius, skill.skillColor, skill.castTime));
        ApplyAoE(skill, target);
        StartCoroutine(ImpactRing(target, skill.radius, skill.skillColor));
    }

    // ========================================
    // DÉGÂTS
    // ========================================

    private void ApplyAoE(SkillData skill, Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, skill.radius, _enemyLayer);
        foreach (Collider hit in hits)
        {
            HealthSystem hs = hit.GetComponent<HealthSystem>();
            if (hs == null || hs.IsDead) continue;

            hs.TakeDamage(skill.baseDamage);
        }
    }

    // ========================================
    // VFX PROCÉDURAUX
    // ========================================

    private IEnumerator CircleIndicator(Vector3 center, float radius, Color color, float duration)
    {
        GameObject go = new GameObject("SkillIndicator");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        SetupLR(lr, new Color(color.r, color.g, color.b, 0.75f), 0.07f);
        lr.loop = true;
        lr.positionCount = 32;

        float groundY = center.y + 0.05f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float r = radius * Mathf.Lerp(0.8f, 1f, elapsed / duration);
            DrawCirclePoints(lr, center, r, groundY);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(go);
    }

    private IEnumerator ImpactRing(Vector3 center, float radius, Color color)
    {
        GameObject go = new GameObject("SkillImpact");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        SetupLR(lr, color, 0.12f);
        lr.loop = true;
        lr.positionCount = 32;

        float groundY = center.y + 0.05f;
        float elapsed = 0f;
        const float duration = 0.4f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            lr.startColor = lr.endColor = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, t));
            DrawCirclePoints(lr, center, radius * t, groundY);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(go);
    }

    private void DrawCirclePoints(LineRenderer lr, Vector3 center, float radius, float y)
    {
        for (int i = 0; i < 32; i++)
        {
            float a = (float)i / 32 * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(center.x + Mathf.Sin(a) * radius, y, center.z + Mathf.Cos(a) * radius));
        }
    }

    private void SetupLR(LineRenderer lr, Color color, float width)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader != null) lr.material = new Material(shader);
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = width;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
    }

    private GameObject CreatePrimitive(PrimitiveType type, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.transform.position = pos;
        go.transform.localScale = scale;
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color);
            r.SetPropertyBlock(mpb);
        }
        return go;
    }

    // ========================================
    // POINT DE CAST
    // ========================================

    private Vector3 GetCastTarget(float maxRange)
    {
        Vector3 aim = _player != null
            ? _player.AimWorldPosition
            : transform.position + transform.forward * maxRange;

        Vector3 flatDir = aim - transform.position;
        flatDir.y = 0f;
        if (flatDir.magnitude > maxRange)
            aim = transform.position + flatDir.normalized * maxRange;

        aim.y = transform.position.y;
        return aim;
    }
}

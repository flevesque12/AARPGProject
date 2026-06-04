using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PostureSystem : MonoBehaviour
{
    [SerializeField] private float _postureMax = 60f;
    [SerializeField] private float _postureRegenRate = 10f;
    [SerializeField] private float _staggerDuration = 1.5f;
    [SerializeField] private float _damageMultiplierWhenStaggered = 2.5f;

    [Header("Events")]
    public UnityEvent OnStaggerEnter;
    public UnityEvent OnStaggerExit;

    public event Action<float, float> OnPostureChanged; // (current, max)

    private float _currentPosture;
    private bool _isStaggered;
    private Coroutine _staggerCoroutine;

    public bool IsStaggered => _isStaggered;
    public float PosturePercent => _currentPosture / _postureMax;
    public float PostureMax => _postureMax;
    public float DamageMultiplierWhenStaggered => _damageMultiplierWhenStaggered;

    private void Awake()
    {
        _currentPosture = _postureMax;
    }

    private void Update()
    {
        if (_isStaggered || _currentPosture >= _postureMax) return;

        _currentPosture = Mathf.Min(_postureMax, _currentPosture + _postureRegenRate * Time.deltaTime);
        OnPostureChanged?.Invoke(_currentPosture, _postureMax);
    }

    public void DegradePosture(float amount)
    {
        if (_isStaggered) return;

        _currentPosture = Mathf.Max(0f, _currentPosture - amount);
        OnPostureChanged?.Invoke(_currentPosture, _postureMax);

        if (_currentPosture <= 0f)
            EnterStagger();
    }

    private void EnterStagger()
    {
        if (_staggerCoroutine != null)
            StopCoroutine(_staggerCoroutine);

        _isStaggered = true;
        OnStaggerEnter?.Invoke();
        _staggerCoroutine = StartCoroutine(StaggerCoroutine());
    }

    private IEnumerator StaggerCoroutine()
    {
        // Brief freeze on stagger entry — slowmo-safe via WaitForSecondsRealtime
        Time.timeScale = 0.05f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        yield return new WaitForSecondsRealtime(0.08f);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        yield return new WaitForSeconds(_staggerDuration);

        ExitStagger();
    }

    private void ExitStagger()
    {
        _isStaggered = false;
        _currentPosture = _postureMax;
        OnStaggerExit?.Invoke();
        OnPostureChanged?.Invoke(_currentPosture, _postureMax);
    }
}

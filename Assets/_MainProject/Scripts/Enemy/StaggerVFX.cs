using System.Collections;
using UnityEngine;

// Attach alongside PostureSystem.
// Wire OnStaggerEnter → OnStaggerEnter() and OnStaggerExit → OnStaggerExit() in the Inspector.
public class StaggerVFX : MonoBehaviour
{
    [SerializeField] private Color _staggerTint = new Color(1f, 0.75f, 0f);   // gold
    [SerializeField] private float _punchScale = 1.15f;
    [SerializeField] private float _punchDuration = 0.12f;

    private Renderer[] _renderers;
    private Vector3 _originalScale;
    private Coroutine _punchCoroutine;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _originalScale = transform.localScale;
    }

    // Called by PostureSystem.OnStaggerEnter (UnityEvent — wire in Inspector)
    public void OnStaggerEnter()
    {
        ApplyTint();
        if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
        _punchCoroutine = StartCoroutine(ScalePunch());
    }

    // Called by PostureSystem.OnStaggerExit (UnityEvent — wire in Inspector)
    public void OnStaggerExit()
    {
        ClearTint();
        if (_punchCoroutine != null) { StopCoroutine(_punchCoroutine); _punchCoroutine = null; }
        transform.localScale = _originalScale;
    }

    private void ApplyTint()
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        foreach (var r in _renderers)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", _staggerTint);   // URP Lit/Unlit
            mpb.SetColor("_Color", _staggerTint);        // legacy fallback
            r.SetPropertyBlock(mpb);
        }
    }

    private void ClearTint()
    {
        MaterialPropertyBlock empty = new MaterialPropertyBlock();
        foreach (var r in _renderers)
            r.SetPropertyBlock(empty);
    }

    private IEnumerator ScalePunch()
    {
        float elapsed = 0f;
        while (elapsed < _punchDuration)
        {
            float t = elapsed / _punchDuration;
            transform.localScale = _originalScale * Mathf.Lerp(_punchScale, 1f, t);
            elapsed += Time.unscaledDeltaTime;  // works during slowmo
            yield return null;
        }
        transform.localScale = _originalScale;
        _punchCoroutine = null;
    }
}

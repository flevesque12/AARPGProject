using System.Collections;
using UnityEngine;

public class PostureBarUI : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] private float _yOffset = 2.05f;   // just below WorldHealthBar (2.2f)
    [SerializeField] private float _barWidth = 1.2f;
    [SerializeField] private float _barHeight = 0.08f;

    [Header("Colors")]
    [SerializeField] private Color _postureColor = new Color(0.85f, 0.6f, 0.1f);
    [SerializeField] private Color _pulseColor = Color.white;
    [SerializeField] private Color _bgColor = new Color(0.12f, 0.12f, 0.12f);
    [SerializeField] private float _pulseThreshold = 0.3f;
    [SerializeField] private float _pulseSpeed = 4f;

    [Header("Timing")]
    [SerializeField] private float _hideDelay = 3f;
    [SerializeField] private float _staggerTextDuration = 1f;

    private PostureSystem _posture;
    private Camera _mainCam;

    private Transform _barContainer;
    private Transform _fillQuad;
    private TextMesh _staggerText;
    private Material _fillMat;
    private Material _bgMat;

    private float _currentFill = 1f;
    private float _targetFill = 1f;
    private float _lastDegradeTime = -999f;
    private Coroutine _staggerTextCoroutine;

    private void Awake()
    {
        _posture = GetComponent<PostureSystem>();
        _mainCam = Camera.main;
        CreateUI();
    }

    private void OnEnable()
    {
        if (_posture == null) return;
        _posture.OnPostureChanged += OnPostureChanged;
        _posture.OnStaggerEnter.AddListener(OnStaggerEnter);
        _posture.OnStaggerExit.AddListener(OnStaggerExit);
    }

    private void OnDisable()
    {
        if (_posture == null) return;
        _posture.OnPostureChanged -= OnPostureChanged;
        _posture.OnStaggerEnter.RemoveListener(OnStaggerEnter);
        _posture.OnStaggerExit.RemoveListener(OnStaggerExit);
    }

    private void LateUpdate()
    {
        if (_barContainer == null || _mainCam == null) return;

        _barContainer.rotation = _mainCam.transform.rotation;

        _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * 10f);
        UpdateFillScale();

        UpdatePulseColor();

        // Hide when posture is full AND _hideDelay has elapsed since last degradation
        bool shouldBeVisible = _posture.PosturePercent < 1f
            || Time.time - _lastDegradeTime < _hideDelay;

        if (!shouldBeVisible && _barContainer.gameObject.activeSelf)
            _barContainer.gameObject.SetActive(false);
    }

    private void OnPostureChanged(float current, float max)
    {
        float newTarget = current / max;
        bool isDegrading = newTarget < _targetFill;
        _targetFill = newTarget;

        if (!isDegrading) return;

        _lastDegradeTime = Time.time;
        if (!_posture.IsStaggered)
            _barContainer.gameObject.SetActive(true);
    }

    private void OnStaggerEnter()
    {
        if (_staggerTextCoroutine != null)
            StopCoroutine(_staggerTextCoroutine);
        _staggerTextCoroutine = StartCoroutine(ShowStaggerText());
    }

    private void OnStaggerExit()
    {
        // OnPostureChanged fires immediately after with full posture.
        // The bar stays visible for _hideDelay via _lastDegradeTime, then auto-hides.
    }

    private IEnumerator ShowStaggerText()
    {
        _barContainer.gameObject.SetActive(true);
        if (_fillQuad != null) _fillQuad.gameObject.SetActive(false);
        if (_staggerText != null) _staggerText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(_staggerTextDuration);

        if (_staggerText != null) _staggerText.gameObject.SetActive(false);
        if (_fillQuad != null) _fillQuad.gameObject.SetActive(true);
        _staggerTextCoroutine = null;
    }

    private void UpdateFillScale()
    {
        if (_fillQuad == null) return;

        Vector3 scale = _fillQuad.localScale;
        scale.x = _barWidth * _currentFill;
        _fillQuad.localScale = scale;

        // Shift left so the bar depletes right-to-left
        Vector3 pos = _fillQuad.localPosition;
        pos.x = -(_barWidth * (1f - _currentFill)) * 0.5f;
        _fillQuad.localPosition = pos;
    }

    private void UpdatePulseColor()
    {
        if (_fillMat == null || _posture.IsStaggered) return;

        if (_targetFill > 0f && _targetFill <= _pulseThreshold)
        {
            float t = Mathf.PingPong(Time.unscaledTime * _pulseSpeed, 1f);
            _fillMat.color = Color.Lerp(_postureColor, _pulseColor, t);
        }
        else
        {
            _fillMat.color = _postureColor;
        }
    }

    private void CreateUI()
    {
        _barContainer = new GameObject("PostureBar").transform;
        _barContainer.SetParent(transform);
        _barContainer.localPosition = new Vector3(0f, _yOffset, 0f);
        _barContainer.localRotation = Quaternion.identity;

        // Background
        GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgObj.name = "BG";
        bgObj.transform.SetParent(_barContainer);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(_barWidth, _barHeight, 1f);
        Destroy(bgObj.GetComponent<Collider>());
        _bgMat = new Material(Shader.Find("Unlit/Color"));
        _bgMat.color = _bgColor;
        bgObj.GetComponent<Renderer>().material = _bgMat;

        // Posture fill
        GameObject fillObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fillObj.name = "Fill";
        fillObj.transform.SetParent(_barContainer);
        fillObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        fillObj.transform.localScale = new Vector3(_barWidth, _barHeight, 1f);
        Destroy(fillObj.GetComponent<Collider>());
        _fillMat = new Material(Shader.Find("Unlit/Color"));
        _fillMat.color = _postureColor;
        fillObj.GetComponent<Renderer>().material = _fillMat;
        _fillQuad = fillObj.transform;

        // "STAGGER" label — TextMesh, same pattern as DamageNumber
        GameObject textObj = new GameObject("StaggerText");
        textObj.transform.SetParent(_barContainer);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        textObj.transform.localScale = Vector3.one * 0.04f;
        _staggerText = textObj.AddComponent<TextMesh>();
        _staggerText.text = "STAGGER";
        _staggerText.alignment = TextAlignment.Center;
        _staggerText.anchor = TextAnchor.MiddleCenter;
        _staggerText.fontSize = 24;
        _staggerText.fontStyle = FontStyle.Bold;
        _staggerText.color = Color.white;
        textObj.SetActive(false);

        // Hidden at spawn — posture starts full
        _barContainer.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_fillMat != null) Destroy(_fillMat);
        if (_bgMat != null) Destroy(_bgMat);
    }
}

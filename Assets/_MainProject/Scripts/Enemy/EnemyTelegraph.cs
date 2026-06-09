using System.Collections;
using UnityEngine;

public class EnemyTelegraph : MonoBehaviour
{
    public enum TelegraphType { Circle, Cone, FullBoss }

    [SerializeField] private TelegraphType _defaultType = TelegraphType.Circle;
    [SerializeField] private float _telegraphRadius = 2f;
    [SerializeField] private float _telegraphConeAngle = 60f;
    [SerializeField] private float _telegraphLength = 4f;
    [SerializeField] private int _lineSegments = 32;
    [SerializeField] private float _lineWidth = 0.07f;

    private static readonly Color _color = new Color(1f, 0f, 0f, 0.6f);
    private Coroutine _activeCoroutine;
    private GameObject _activeIndicator;

    // Shortcut — uses _defaultType and current forward direction
    public void Telegraph(float duration) => StartTelegraph(_defaultType, duration, transform.forward);

    // Full control over type and direction
    public void ShowTelegraph(TelegraphType type, float duration, Vector3 direction) =>
        StartTelegraph(type, duration, direction);

    public void Cancel()
    {
        if (_activeCoroutine != null) { StopCoroutine(_activeCoroutine); _activeCoroutine = null; }
        if (_activeIndicator != null) { Destroy(_activeIndicator); _activeIndicator = null; }
    }

    private void StartTelegraph(TelegraphType type, float duration, Vector3 direction)
    {
        Cancel();
        _activeCoroutine = StartCoroutine(Run(type, duration, direction));
    }

    private IEnumerator Run(TelegraphType type, float duration, Vector3 direction)
    {
        _activeIndicator = new GameObject("TelegraphIndicator");
        LineRenderer lr = _activeIndicator.AddComponent<LineRenderer>();
        SetupLineRenderer(lr);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float scale = Mathf.Lerp(0.8f, 1f, elapsed / duration);
            DrawShape(lr, type, direction, scale);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(_activeIndicator);
        _activeIndicator = null;
        _activeCoroutine = null;
    }

    private void SetupLineRenderer(LineRenderer lr)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader != null) lr.material = new Material(shader);

        lr.startColor = _color;
        lr.endColor = _color;
        lr.startWidth = _lineWidth;
        lr.endWidth = _lineWidth;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
    }

    private void DrawShape(LineRenderer lr, TelegraphType type, Vector3 direction, float scale)
    {
        float groundY = transform.position.y + 0.05f;
        switch (type)
        {
            case TelegraphType.Circle:
                DrawCircle(lr, groundY, _telegraphRadius * scale, _lineSegments);
                break;
            case TelegraphType.Cone:
                DrawCone(lr, groundY, direction, _telegraphLength * scale);
                break;
            case TelegraphType.FullBoss:
                DrawCircle(lr, groundY, _telegraphRadius * 2f * scale, _lineSegments * 2);
                break;
        }
    }

    private void DrawCircle(LineRenderer lr, float groundY, float radius, int segments)
    {
        lr.loop = true;
        lr.positionCount = segments;
        Vector3 center = transform.position;
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(
                center.x + Mathf.Sin(angle) * radius,
                groundY,
                center.z + Mathf.Cos(angle) * radius));
        }
    }

    private void DrawCone(LineRenderer lr, float groundY, Vector3 direction, float length)
    {
        lr.loop = false;
        Vector3 dir = direction;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
        dir.Normalize();

        float baseAngle = Mathf.Atan2(dir.x, dir.z);
        float halfAngle = _telegraphConeAngle * 0.5f * Mathf.Deg2Rad;
        const int arcPoints = 16;

        // center → arc (left tip → right tip) → center
        lr.positionCount = arcPoints + 2;
        Vector3 center = new Vector3(transform.position.x, groundY, transform.position.z);
        lr.SetPosition(0, center);

        for (int i = 0; i < arcPoints; i++)
        {
            float t = (float)i / (arcPoints - 1);
            float a = baseAngle - halfAngle + t * _telegraphConeAngle * Mathf.Deg2Rad;
            lr.SetPosition(i + 1, new Vector3(
                transform.position.x + Mathf.Sin(a) * length,
                groundY,
                transform.position.z + Mathf.Cos(a) * length));
        }
        lr.SetPosition(arcPoints + 1, center);
    }
}

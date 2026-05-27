using UnityEngine;
using System.Collections;

/// <summary>
/// Chiffre de dégât flottant en world space.
/// Créé entièrement par code via DamageNumber.Spawn() — pas de prefab requis.
/// Blanc pour les coups normaux, jaune pour les gros coups.
/// </summary>
public class DamageNumber : MonoBehaviour
{
    private static readonly Color BigHitColor = new Color(1f, 0.85f, 0f);
    private const float BigHitThreshold = 20f;
    private const float Duration = 0.75f;
    private const float RiseHeight = 1.6f;
    private const float XSpread = 0.35f;

    public static void Spawn(Vector3 worldPosition, float damage)
    {
        GameObject go = new GameObject("DmgNum");
        go.transform.position = worldPosition;
        go.AddComponent<DamageNumber>().Init(damage);
    }

    private void Init(float damage)
    {
        TextMesh tm = gameObject.AddComponent<TextMesh>();
        tm.text = Mathf.RoundToInt(damage).ToString();
        tm.fontSize = 40;
        tm.characterSize = 0.1f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = damage >= BigHitThreshold ? BigHitColor : Color.white;
        tm.fontStyle = FontStyle.Bold;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 100;
        }

        StartCoroutine(Animate(tm));
    }

    private IEnumerator Animate(TextMesh tm)
    {
        Vector3 startPos = transform.position;
        float xDrift = Random.Range(-XSpread, XSpread);
        float elapsed = 0f;

        while (elapsed < Duration)
        {
            if (tm == null) yield break;

            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / Duration;

            // Montée avec ease-out cubique
            float rise = (1f - Mathf.Pow(1f - t, 3f)) * RiseHeight;
            transform.position = startPos + new Vector3(xDrift * t, rise, 0f);

            // Pop rapide au départ puis légère compression
            float scale = t < 0.15f
                ? Mathf.Lerp(0.2f, 1.3f, t / 0.15f)
                : Mathf.Lerp(1.3f, 0.85f, (t - 0.15f) / 0.85f);
            transform.localScale = Vector3.one * scale;

            // Fade dans la seconde moitié
            Color c = tm.color;
            c.a = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
            tm.color = c;

            // Toujours face à la caméra
            if (Camera.main != null)
                transform.rotation = Camera.main.transform.rotation;

            yield return null;
        }

        Destroy(gameObject);
    }
}

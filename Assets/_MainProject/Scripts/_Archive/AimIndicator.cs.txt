using UnityEngine;

/// <summary>
/// Indicateur visuel de visée au sol.
/// Affiche un réticule là où la souris pointe, pour que le joueur
/// sache toujours dans quelle direction il vise.
/// 
/// Essentiel pour le game feel WASD + souris.
/// Sans ça, le joueur ne sait pas exactement où ses projectiles iront.
/// </summary>
public class AimIndicator : MonoBehaviour
{
    [Header("Visuel")]
    [SerializeField] private GameObject cursorPrefab;            // Prefab du réticule (un simple disque projeté ou sprite)
    [SerializeField] private float cursorSize = 0.6f;
    [SerializeField] private float cursorHeightOffset = 0.05f;   // Légèrement au-dessus du sol

    [Header("Couleurs")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color enemyHoverColor = new Color(1f, 0.3f, 0.2f, 0.8f);
    [SerializeField] private Color riposteWindowColor = new Color(1f, 0.78f, 0.1f, 0.95f);
    [SerializeField] private float colorTransitionSpeed = 10f;

    [Header("Riposte")]
    [SerializeField] private float ripostePulseSpeed = 6f;
    [SerializeField] private float riposteSizeMultiplier = 1.5f;

    [Header("Ligne de visée (optionnel)")]
    [SerializeField] private bool showAimLine = false;
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private float aimLineMaxLength = 8f;

    [Header("Références")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private RiposteSystem riposteSystem;
    [SerializeField] private LayerMask enemyLayer;

    private GameObject cursorInstance;
    private Renderer cursorRenderer;
    private Color currentColor;
    private bool isOverEnemy;

    private void Start()
    {
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();
        if (riposteSystem == null)
            riposteSystem = FindAnyObjectByType<RiposteSystem>();

        // Créer le curseur
        if (cursorPrefab != null)
        {
            cursorInstance = Instantiate(cursorPrefab);
            cursorInstance.name = "AimCursor";
            cursorInstance.transform.localScale = Vector3.one * cursorSize;
            cursorRenderer = cursorInstance.GetComponentInChildren<Renderer>();
        }
        else
        {
            // Créer un curseur simple par défaut (quad projeté au sol)
            cursorInstance = GameObject.CreatePrimitive(PrimitiveType.Quad);
            cursorInstance.name = "AimCursor";
            cursorInstance.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Face vers le haut
            cursorInstance.transform.localScale = Vector3.one * cursorSize;

            // Retirer le collider
            Destroy(cursorInstance.GetComponent<Collider>());

            cursorRenderer = cursorInstance.GetComponent<Renderer>();

            // Matériau simple semi-transparent
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            Material mat = new Material(unlitShader != null ? unlitShader : Shader.Find("Hidden/InternalErrorShader"));
            mat.color = normalColor;
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);   // Alpha
            mat.renderQueue = 3001;      // Au-dessus du sol
            cursorRenderer.material = mat;
        }

        currentColor = normalColor;

        // Configurer la ligne de visée
        if (showAimLine && aimLine == null)
        {
            GameObject lineObj = new GameObject("AimLine");
            aimLine = lineObj.AddComponent<LineRenderer>();
            aimLine.positionCount = 2;
            aimLine.startWidth = 0.03f;
            aimLine.endWidth = 0.01f;
            Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit");
            aimLine.material = new Material(lineShader != null ? lineShader : Shader.Find("Hidden/InternalErrorShader"));
            aimLine.startColor = new Color(1f, 1f, 1f, 0.15f);
            aimLine.endColor = new Color(1f, 1f, 1f, 0.05f);
        }
    }

    private void Update()
    {
        if (playerController == null || cursorInstance == null) return;

        Vector3 aimPos = playerController.AimWorldPosition;
        aimPos.y += cursorHeightOffset;

        // Positionner le curseur
        cursorInstance.transform.position = aimPos;

        // Vérifier si on survole un ennemi
        isOverEnemy = false;
        if (enemyLayer != 0)
        {
            Collider[] hits = Physics.OverlapSphere(playerController.AimWorldPosition, 0.5f, enemyLayer);
            isOverEnemy = hits.Length > 0;
        }

        // Riposte window: gold pulse + scale up; overrides enemy hover
        bool riposteOpen = riposteSystem != null && riposteSystem.IsRiposteWindowOpen;

        Color targetColor;
        float targetScale;
        if (riposteOpen)
        {
            float t = Mathf.PingPong(Time.unscaledTime * ripostePulseSpeed, 1f);
            targetColor = Color.Lerp(riposteWindowColor, Color.white, t * 0.35f);
            targetScale = cursorSize * riposteSizeMultiplier;
        }
        else
        {
            targetColor = isOverEnemy ? enemyHoverColor : normalColor;
            targetScale = cursorSize;
        }

        currentColor = Color.Lerp(currentColor, targetColor, colorTransitionSpeed * Time.deltaTime);
        if (cursorRenderer != null)
            cursorRenderer.material.color = currentColor;

        float currentScale = Mathf.Lerp(cursorInstance.transform.localScale.x, targetScale, Time.deltaTime * 14f);
        cursorInstance.transform.localScale = Vector3.one * currentScale;

        // Ligne de visée
        if (showAimLine && aimLine != null)
        {
            Vector3 playerPos = playerController.transform.position + Vector3.up * 0.5f;
            Vector3 toAim = aimPos - playerPos;

            // Limiter la longueur
            if (toAim.magnitude > aimLineMaxLength)
                toAim = toAim.normalized * aimLineMaxLength;

            aimLine.SetPosition(0, playerPos);
            aimLine.SetPosition(1, playerPos + toAim);
        }
    }

    private void OnDestroy()
    {
        if (cursorInstance != null)
            Destroy(cursorInstance);
    }

    /// <summary>
    /// Masque/affiche le curseur (pendant les menus, cutscenes, etc.)
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (cursorInstance != null)
            cursorInstance.SetActive(visible);
        if (aimLine != null)
            aimLine.gameObject.SetActive(visible);
    }
}

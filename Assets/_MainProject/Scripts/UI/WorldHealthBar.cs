using UnityEngine;

/// <summary>
/// Barre de vie en World Space qui suit l'entité.
/// Utilise un simple Quad avec un shader/matériau pour la barre.
/// 
/// SETUP RAPIDE (pas besoin de Canvas !) :
///   1. Ajouter ce script sur le même GameObject que HealthSystem
///   2. Il crée automatiquement la barre de vie au-dessus de l'entité
///   3. La barre est toujours face à la caméra (billboard)
///   
/// La barre se cache quand la vie est pleine (pour le joueur, utiliser le HUD).
/// </summary>
public class WorldHealthBar : MonoBehaviour
{
    [Header("Apparence")]
    [SerializeField] private float barWidth = 1.2f;
    [SerializeField] private float barHeight = 0.12f;
    [SerializeField] private float yOffset = 2.2f;
    [SerializeField] private Color healthColor = new Color(0.2f, 0.9f, 0.2f);
    [SerializeField] private Color damageColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color backgroundColor = new Color(0.15f, 0.15f, 0.15f);

    [Header("Comportement")]
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private bool alwaysShow = false;
    [SerializeField] private float showDuration = 3f;

    private HealthSystem health;
    private Transform barContainer;
    private Transform healthFill;
    private Transform bgFill;
    private Camera mainCam;
    private float showTimer;
    private Material healthMat;
    private Material bgMat;

    private void Start()
    {
        health = GetComponent<HealthSystem>();
        mainCam = Camera.main;

        CreateHealthBar();

        if (health != null)
        {
            health.OnHealthChanged += OnHealthChanged;
            health.OnDeath += OnDeath;
        }

        // Cacher au début si vie pleine
        if (hideWhenFull && !alwaysShow)
            barContainer.gameObject.SetActive(false);
    }

    private void CreateHealthBar()
    {
        // Container
        barContainer = new GameObject("HealthBar").transform;
        barContainer.SetParent(transform);
        barContainer.localPosition = new Vector3(0, yOffset, 0);

        // Background
        GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgObj.name = "BG";
        bgObj.transform.SetParent(barContainer);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(barWidth, barHeight, 1);
        Destroy(bgObj.GetComponent<Collider>());
        bgMat = new Material(Shader.Find("Unlit/Color"));
        bgMat.color = backgroundColor;
        bgObj.GetComponent<Renderer>().material = bgMat;
        bgFill = bgObj.transform;

        // Health fill
        GameObject fillObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fillObj.name = "Fill";
        fillObj.transform.SetParent(barContainer);
        fillObj.transform.localPosition = new Vector3(0, 0, -0.01f);
        fillObj.transform.localScale = new Vector3(barWidth, barHeight, 1);
        Destroy(fillObj.GetComponent<Collider>());
        healthMat = new Material(Shader.Find("Unlit/Color"));
        healthMat.color = healthColor;
        fillObj.GetComponent<Renderer>().material = healthMat;
        healthFill = fillObj.transform;
    }

    private void LateUpdate()
    {
        if (barContainer == null || mainCam == null) return;

        // Billboard : toujours face à la caméra
        barContainer.rotation = mainCam.transform.rotation;

        // Timer pour cacher la barre
        if (!alwaysShow && hideWhenFull)
        {
            if (showTimer > 0)
            {
                showTimer -= Time.deltaTime;
                if (showTimer <= 0 && health.HealthPercent >= 1f)
                {
                    barContainer.gameObject.SetActive(false);
                }
            }
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        if (healthFill == null) return;

        float percent = current / max;

        // Scale la barre de remplissage
        Vector3 scale = healthFill.localScale;
        scale.x = barWidth * percent;
        healthFill.localScale = scale;

        // Décaler pour que la barre se vide de droite à gauche
        Vector3 pos = healthFill.localPosition;
        pos.x = -(barWidth * (1f - percent)) * 0.5f;
        healthFill.localPosition = pos;

        // Changer la couleur selon le pourcentage
        healthMat.color = Color.Lerp(damageColor, healthColor, percent);

        // Afficher la barre
        barContainer.gameObject.SetActive(true);
        showTimer = showDuration;
    }

    private void OnDeath()
    {
        if (barContainer != null)
            barContainer.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (healthMat != null) Destroy(healthMat);
        if (bgMat != null) Destroy(bgMat);
    }
}

using UnityEngine;

public class MovementDirectionArrow : MonoBehaviour
{
    [SerializeField] float animSpeed = 10f;
    [SerializeField] float showThreshold = 0.05f;

    Animator playerAnimator;
    float scale;

    void Awake()
    {
        // Cherche l'Animator dans tous les enfants du Player (HeroModel)
        playerAnimator = transform.parent.GetComponentInChildren<Animator>();
    }

    void Update()
    {
        float speed = playerAnimator != null ? playerAnimator.GetFloat("Speed") : 0f;
        float target = speed > showThreshold ? 1f : 0f;
        scale = Mathf.Lerp(scale, target, Time.deltaTime * animSpeed);
        // Anime en XZ seulement : la flèche "surgit" du sol quand on bouge
        transform.localScale = new Vector3(scale, 1f, scale);
    }
}

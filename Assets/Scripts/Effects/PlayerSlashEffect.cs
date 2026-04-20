using UnityEngine;

public class PlayerSlashEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.1f;
    [SerializeField] private float startScale = 0.8f;
    [SerializeField] private float endScale = 1.2f;
    [SerializeField] private float fadeSpeed = 1f;

    private SpriteRenderer spriteRenderer;
    private float timer;
    private Color originalColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        transform.localScale = Vector3.one * startScale;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / lifetime);

        transform.localScale = Vector3.Lerp(
            Vector3.one * startScale,
            Vector3.one * endScale,
            t
        );

        if (spriteRenderer != null)
        {
            Color c = originalColor;
            c.a = Mathf.Lerp(originalColor.a, 0f, t * fadeSpeed);
            spriteRenderer.color = c;
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }

    public void SetFacing(bool facingRight)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);
        transform.localScale = scale;
    }
}
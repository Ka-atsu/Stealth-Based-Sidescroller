using UnityEngine;

public class AirCrackVFX2D : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Timing")]
    [SerializeField] private float lifetime = 0.16f;

    [Header("Scale")]
    [SerializeField] private Vector3 startScaleMultiplier = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 endScaleMultiplier = new Vector3(1.5f, 0.6f, 1f);

    [Header("Fade")]
    [SerializeField] private float startAlpha = 0.9f;
    [SerializeField] private float endAlpha = 0f;

    private float timer;
    private Vector3 baseScale;

    private void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        baseScale = transform.localScale;
        transform.localScale = Vector3.Scale(baseScale, startScaleMultiplier);
        SetAlpha(startAlpha);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / lifetime);

        Vector3 currentMultiplier = Vector3.Lerp(startScaleMultiplier, endScaleMultiplier, t);
        transform.localScale = Vector3.Scale(baseScale, currentMultiplier);

        SetAlpha(Mathf.Lerp(startAlpha, endAlpha, t));

        if (timer >= lifetime)
            Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null)
            return;

        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;
    }
}
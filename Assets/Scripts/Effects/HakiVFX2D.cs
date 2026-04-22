using UnityEngine;

public class HakiVFX2D : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private SpriteRenderer mainRenderer;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 0.32f;

    [Header("Scale")]
    [SerializeField] private Vector3 firstPopScale = new Vector3(0.7f, 0.7f, 1f);
    [SerializeField] private Vector3 impactScale = new Vector3(2.2f, 2.2f, 1f);
    [SerializeField] private Vector3 burstScale = new Vector3(3.4f, 3.0f, 1f);
    [SerializeField] private Vector3 endScale = new Vector3(4.2f, 3.6f, 1f);

    [Header("Alpha")]
    [SerializeField] private float startAlpha = 0f;
    [SerializeField] private float impactAlpha = 1f;
    [SerializeField] private float endAlpha = 0f;

    [Header("Color")]
    [SerializeField] private Color impactColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color burstColor = new Color(0.75f, 0.78f, 0.85f, 1f);
    [SerializeField] private Color fadeColor = new Color(0.35f, 0.38f, 0.45f, 0f);

    [Header("Timing")]
    [SerializeField] private float impactPhaseNormalized = 0.12f;
    [SerializeField] private float burstPhaseNormalized = 0.32f;
    [SerializeField] private float holdPhaseNormalized = 0.48f;

    [Header("Runtime Boost")]
    [SerializeField] private float runtimeScaleMultiplier = 1f;
    [SerializeField] private float runtimeLifetimeMultiplier = 1f;

    private float timer;

    private void Reset()
    {
        if (mainRenderer == null)
            mainRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (mainRenderer == null)
            mainRenderer = GetComponentInChildren<SpriteRenderer>();

        ApplyPhase(0f);
    }

    private void Update()
    {
        float finalLifetime = Mathf.Max(0.01f, lifetime * runtimeLifetimeMultiplier);

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / finalLifetime);

        ApplyPhase(t);

        if (timer >= finalLifetime)
            Destroy(gameObject);
    }

    public void SetRuntimeMultipliers(float scaleMultiplier, float lifetimeMultiplier)
    {
        runtimeScaleMultiplier = Mathf.Max(0.05f, scaleMultiplier);
        runtimeLifetimeMultiplier = Mathf.Max(0.05f, lifetimeMultiplier);
        ApplyPhase(0f);
    }

    private void ApplyPhase(float t)
    {
        if (mainRenderer == null)
            return;

        float a = Mathf.Clamp01(impactPhaseNormalized);
        float b = Mathf.Clamp01(burstPhaseNormalized);
        float c = Mathf.Clamp01(holdPhaseNormalized);

        Vector3 scale;
        Color color;
        float alpha;

        if (t <= a)
        {
            float p = EaseOutCubic(SafeLerp01(0f, a, t));
            scale = Vector3.Lerp(firstPopScale, impactScale, p);
            color = Color.Lerp(new Color(1f, 1f, 1f, 0f), impactColor, p);
            alpha = Mathf.Lerp(startAlpha, impactAlpha, p);
        }
        else if (t <= b)
        {
            float p = EaseOutQuad(SafeLerp01(a, b, t));
            scale = Vector3.Lerp(impactScale, burstScale, p);
            color = Color.Lerp(impactColor, burstColor, p);
            alpha = Mathf.Lerp(impactAlpha, 0.92f, p);
        }
        else if (t <= c)
        {
            float p = SafeLerp01(b, c, t);
            scale = Vector3.Lerp(burstScale, burstScale * 1.05f, p);
            color = Color.Lerp(burstColor, burstColor, p);
            alpha = Mathf.Lerp(0.92f, 0.8f, p);
        }
        else
        {
            float p = EaseInQuad(SafeLerp01(c, 1f, t));
            scale = Vector3.Lerp(burstScale, endScale, p);
            color = Color.Lerp(burstColor, fadeColor, p);
            alpha = Mathf.Lerp(0.8f, endAlpha, p);
        }

        scale *= runtimeScaleMultiplier;

        mainRenderer.transform.localScale = new Vector3(scale.x, scale.y, scale.z);

        Color finalColor = color;
        finalColor.a = alpha;
        mainRenderer.color = finalColor;
    }

    private float SafeLerp01(float start, float end, float value)
    {
        if (Mathf.Approximately(start, end))
            return 0f;

        return Mathf.Clamp01((value - start) / (end - start));
    }

    private float EaseOutCubic(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    private float EaseOutQuad(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - (1f - x) * (1f - x);
    }

    private float EaseInQuad(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x;
    }
}
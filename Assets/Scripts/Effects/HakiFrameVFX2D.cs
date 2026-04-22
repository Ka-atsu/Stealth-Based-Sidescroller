using UnityEngine;

public class HakiFrameVFX2D : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private SpriteRenderer mainRenderer;

    [Header("Frames")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float perFrameDuration = 0.08f;
    [SerializeField] private bool holdOnLastFrame = true;
    [SerializeField] private float holdDuration = 0.08f;

    [Header("Fade Out")]
    [SerializeField] private bool fadeAfterFrames = true;
    [SerializeField] private float fadeDuration = 0.14f;
    [SerializeField] private float startAlpha = 1f;
    [SerializeField] private float endAlpha = 0f;

    [Header("Runtime Boost")]
    [SerializeField] private float runtimeScaleMultiplier = 1f;
    [SerializeField] private float runtimeLifetimeMultiplier = 1f;

    private float timer;
    private Vector3 originalScale;
    private bool animationFinished;
    private bool holdFinished;

    private void Reset()
    {
        if (mainRenderer == null)
            mainRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (mainRenderer == null)
            mainRenderer = GetComponentInChildren<SpriteRenderer>();

        originalScale = transform.localScale;
        ApplyScale();
        SetAlpha(startAlpha);

        if (frames != null && frames.Length > 0 && mainRenderer != null)
            mainRenderer.sprite = frames[0];
    }

    private void Update()
    {
        if (mainRenderer == null || frames == null || frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        float finalPerFrameDuration = Mathf.Max(0.01f, perFrameDuration * runtimeLifetimeMultiplier);
        float finalHoldDuration = Mathf.Max(0f, holdDuration * runtimeLifetimeMultiplier);
        float finalFadeDuration = Mathf.Max(0.01f, fadeDuration * runtimeLifetimeMultiplier);

        if (!animationFinished)
        {
            timer += Time.deltaTime;

            int frameIndex = Mathf.FloorToInt(timer / finalPerFrameDuration);
            frameIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1);

            mainRenderer.sprite = frames[frameIndex];

            bool reachedLastFrame = frameIndex >= frames.Length - 1;

            if (reachedLastFrame)
            {
                if (!holdOnLastFrame)
                {
                    animationFinished = true;
                    timer = 0f;

                    if (!fadeAfterFrames)
                        Destroy(gameObject);
                }
                else if (!holdFinished)
                {
                    float timeSpentOnLastFrame = timer - ((frames.Length - 1) * finalPerFrameDuration);

                    if (timeSpentOnLastFrame >= finalHoldDuration)
                    {
                        holdFinished = true;
                        animationFinished = true;
                        timer = 0f;

                        if (!fadeAfterFrames)
                            Destroy(gameObject);
                    }
                }
            }
        }
        else
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / finalFadeDuration);
            SetAlpha(Mathf.Lerp(startAlpha, endAlpha, t));

            if (timer >= finalFadeDuration)
                Destroy(gameObject);
        }
    }

    public void SetRuntimeMultipliers(float scaleMultiplier, float lifetimeMultiplier)
    {
        runtimeScaleMultiplier = Mathf.Max(0.05f, scaleMultiplier);
        runtimeLifetimeMultiplier = Mathf.Max(0.05f, lifetimeMultiplier);
        ApplyScale();
    }

    private void ApplyScale()
    {
        transform.localScale = originalScale * runtimeScaleMultiplier;
    }

    private void SetAlpha(float alpha)
    {
        if (mainRenderer == null)
            return;

        Color c = mainRenderer.color;
        c.a = alpha;
        mainRenderer.color = c;
    }
}
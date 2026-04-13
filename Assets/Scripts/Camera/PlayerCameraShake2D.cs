using UnityEngine;

public class PlayerCameraShake2D : MonoBehaviour
{
    [SerializeField] private float dampingSpeed = 18f;

    [Header("Debug Test")]
    [SerializeField] private float testIntensity = 1f;
    [SerializeField] private float testDuration = 0.3f;

    Vector3 originalLocalPosition;
    float currentIntensity;
    float shakeTimer;

    void Awake()
    {
        originalLocalPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;

            Vector2 offset = Random.insideUnitCircle * currentIntensity;
            transform.localPosition = originalLocalPosition + new Vector3(offset.x, offset.y, 0f);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                originalLocalPosition,
                dampingSpeed * Time.unscaledDeltaTime
            );

            if (Vector3.Distance(transform.localPosition, originalLocalPosition) < 0.001f)
                transform.localPosition = originalLocalPosition;
        }
    }

    public void Shake(float intensity, float duration)
    {
        Debug.Log($"SHAKE CALLED | intensity={intensity} duration={duration}", this);
        currentIntensity = intensity;
        shakeTimer = duration;
    }

    [ContextMenu("Test Shake")]
    private void TestShake()
    {
        Shake(testIntensity, testDuration);
    }
}
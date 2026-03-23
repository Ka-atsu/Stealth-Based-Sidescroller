using UnityEngine;

public class PlayerCameraShake2D : MonoBehaviour
{
    [SerializeField] private float dampingSpeed = 18f;

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
        currentIntensity = intensity;
        shakeTimer = duration;
    }
}
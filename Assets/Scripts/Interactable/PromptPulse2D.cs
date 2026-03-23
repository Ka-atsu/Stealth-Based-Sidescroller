using UnityEngine;

public class PromptPulse2D : MonoBehaviour
{
    [SerializeField] private Transform art;
    [SerializeField] private float bobAmplitude = 0.08f;
    [SerializeField] private float bobSpeed = 4f;
    [SerializeField] private float pulseAmount = 0.08f;
    [SerializeField] private float pulseSpeed = 6f;

    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;

    void Awake()
    {
        if (art == null)
            art = transform;

        baseLocalPosition = art.localPosition;
        baseLocalScale = art.localScale;
    }

    void Update()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        art.localPosition = baseLocalPosition + new Vector3(0f, bob, 0f);
        art.localScale = baseLocalScale * pulse;
    }
}
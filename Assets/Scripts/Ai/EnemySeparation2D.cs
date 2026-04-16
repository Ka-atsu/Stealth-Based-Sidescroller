using UnityEngine;

public class EnemySeparation2D : MonoBehaviour
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float separationRadius = 0.8f;
    [SerializeField] private float separationStrength = 2f;
    [SerializeField] private float maxPush = 1.2f;

    private Collider2D selfCollider;

    public float MaxPush => maxPush;

    void Awake()
    {
        selfCollider = GetComponent<Collider2D>();
    }

    public float GetSeparationX()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, separationRadius, enemyLayer);

        float push = 0f;

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit == selfCollider || hit.transform == transform)
                continue;

            float deltaX = transform.position.x - hit.transform.position.x;
            float absX = Mathf.Abs(deltaX);

            float dir =
                absX < 0.01f
                ? Mathf.Sign(transform.GetInstanceID() - hit.transform.GetInstanceID())
                : Mathf.Sign(deltaX);

            float weight = 1f - Mathf.Clamp01(absX / separationRadius);
            push += dir * weight;
        }

        return Mathf.Clamp(push * separationStrength, -maxPush, maxPush);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}
using UnityEngine;

public class BossTargetProjectile2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float arriveDistance = 0.05f;
    [SerializeField] private float maxLifetime = 4f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float impactRadius = 0.75f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private bool hitPlayerOnBodyContact = true;

    [Header("VFX")]
    [SerializeField] private GameObject impactEffectPrefab;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Vector2 targetPosition;
    private Vector2 attackerPosition;
    private bool initialized;
    private bool exploded;

    public void Initialize(
        Vector2 newTargetPosition,
        int newDamage,
        float newSpeed,
        float newImpactRadius,
        LayerMask newPlayerLayer,
        Vector2 newAttackerPosition)
    {
        targetPosition = newTargetPosition;
        damage = newDamage;
        speed = newSpeed;
        impactRadius = newImpactRadius;
        playerLayer = newPlayerLayer;
        attackerPosition = newAttackerPosition;
        initialized = true;

        Destroy(gameObject, maxLifetime);
    }

    private void Update()
    {
        if (!initialized || exploded)
            return;

        Vector2 currentPosition = transform.position;
        Vector2 nextPosition = Vector2.MoveTowards(currentPosition, targetPosition, speed * Time.deltaTime);
        Vector2 moveDirection = nextPosition - currentPosition;

        if (moveDirection.sqrMagnitude > 0.000001f)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        transform.position = nextPosition;

        if (Vector2.Distance(nextPosition, targetPosition) <= arriveDistance)
            Explode(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!initialized || exploded || !hitPlayerOnBodyContact)
            return;

        if (!IsInLayerMask(other.gameObject.layer, playerLayer))
            return;

        DamageFromCollider(other);
        Explode(true);
    }

    private void Explode(bool skipAreaDamage)
    {
        if (exploded)
            return;

        exploded = true;

        if (!skipAreaDamage)
            DamageAtImpactPoint();

        if (impactEffectPrefab != null)
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);

        if (debugLogs)
            Debug.Log("[BossTargetProjectile2D] Exploded at " + transform.position, this);

        Destroy(gameObject);
    }

    private void DamageAtImpactPoint()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, impactRadius, playerLayer);
        if (hit != null)
            DamageFromCollider(hit);
    }

    private void DamageFromCollider(Collider2D hit)
    {
        if (hit == null)
            return;

        PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = hit.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        playerHealth.TakeDamage(damage, attackerPosition);

        if (debugLogs)
            Debug.Log("[BossTargetProjectile2D] Damaged player", this);
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, impactRadius);
    }
}
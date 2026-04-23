using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    public int damage = 1;
    public Transform attackPoint;
    public Vector2 attackBoxSize = new Vector2(2f, 2f);
    public LayerMask playerLayer;

    [Header("Attack Timing")]
    [Tooltip("Failsafe duration so attack always ends even if animation event is missed.")]
    public float attackDuration = 0.6f;

    private Transform player;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private float attackEndTime = 0f;

    public bool IsAttacking => isAttacking;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    void Update()
    {
        // Failsafe: always end the attack after attackDuration
        if (isAttacking && Time.time >= attackEndTime)
        {
            EndAttack();
        }
    }

    public bool CanAttack()
    {
        if (player == null) return false;
        if (isAttacking) return false;
        if (Time.time < nextAttackTime) return false;

        float dist = Mathf.Abs(player.position.x - transform.position.x);
        return dist <= attackRange;
    }

    public void TryAttack()
    {
        if (!CanAttack())
            return;

        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;
        attackEndTime = Time.time + attackDuration;
    }

    // CALL THIS FROM ANIMATION EVENT at the hit frame
    public void DealDamage()
    {
        Vector2 hitPosition = attackPoint != null
            ? (Vector2)attackPoint.position
            : (Vector2)transform.position;

        Collider2D hit = Physics2D.OverlapBox(
            hitPosition,
            attackBoxSize,
            0f,
            playerLayer
        );

        if (hit == null)
            return;

        IDamageable damageable = hit.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
    }

    // CALL THIS FROM ANIMATION EVENT at the last frame
    public void EndAttack()
    {
        isAttacking = false;
        attackEndTime = 0f;
    }

    void OnDisable()
    {
        isAttacking = false;
        attackEndTime = 0f;
    }

    void OnDrawGizmosSelected()
    {
        Vector2 hitPosition = attackPoint != null
            ? (Vector2)attackPoint.position
            : (Vector2)transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(hitPosition, attackBoxSize);
    }
}
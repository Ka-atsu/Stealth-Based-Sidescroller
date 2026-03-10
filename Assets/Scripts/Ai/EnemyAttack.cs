using UnityEngine;
using System.Collections;

public class EnemyAttack : MonoBehaviour
{
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    public int damage = 1;
    public Transform attackPoint;
    public Vector2 attackBoxSize = new Vector2(2f, 2f);
    public LayerMask playerLayer;

    private Transform player;
    private float lastAttackTime = -999f;
    private bool isAttacking = false;

    public bool IsAttacking => isAttacking;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    public bool CanAttack()
    {
        if (player == null) return false;
        if (isAttacking) return false;
        if (Time.time < lastAttackTime + attackCooldown) return false;

        float dist = Mathf.Abs(player.position.x - transform.position.x);

        return dist <= attackRange;
    }

    public void TryAttack()
    {
        if (isAttacking) return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        yield return new WaitForSeconds(0.1f);

        DoSlashHit();

        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
    }

    void DoSlashHit()
    {
        if (player == null) return;

        // Face the player
        Vector2 dir = (player.position - transform.position).normalized;

        // Position hitbox in front of enemy
        Vector2 hitPosition = (Vector2)transform.position + dir * 1.2f;

        Collider2D hit = Physics2D.OverlapBox(
            hitPosition,
            attackBoxSize,
            0f,
            playerLayer
        );

        if (hit == null)
        {
            return;
        }

        IDamageable d = hit.GetComponentInParent<IDamageable>();

        if (d != null)
        {
            d.TakeDamage(damage);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Vector2 dir = (player.position - transform.position).normalized;
        Vector2 hitPosition = (Vector2)transform.position + dir * 1.2f;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(hitPosition, attackBoxSize);
    }
}
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

        float dist = Vector2.Distance(transform.position, player.position);
        return dist <= attackRange;
    }

    public void TryAttack()
    {
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        yield return new WaitForSeconds(0.1f);  // Wait before hitting

        DoSlashHit();  // Perform the actual attack

        yield return new WaitForSeconds(0.2f);  // Wait for the attack animation to finish

        isAttacking = false;  // Allow the next attack
    }

    private void DoSlashHit()
    {
        Debug.Log("Enemy attempted attack");

        Collider2D hit = Physics2D.OverlapBox(attackPoint.position, attackBoxSize, 0f, playerLayer);

        if (hit == null)
        {
            Debug.Log("Attack missed");
            return;
        }

        Debug.Log("Player detected in attack box");

        IDamageable d = hit.GetComponentInParent<IDamageable>();

        if (d != null)
        {
            d.TakeDamage(damage);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackBoxSize);
    }
}
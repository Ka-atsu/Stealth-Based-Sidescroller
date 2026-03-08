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
        if (!CanAttack())  // Check if the enemy is allowed to attack
        {
            Debug.Log("Cannot attack yet!");
            return;
        }

        Debug.Log("Attempting to attack");
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
        // Check for collision within the attack range
        Collider2D hit = Physics2D.OverlapBox(attackPoint.position, attackBoxSize, 0f, playerLayer);

        Debug.Log($"Checking attack area at {attackPoint.position}, box size {attackBoxSize}");

        if (hit == null)
        {
            Debug.Log("No hit detected.");
            return;
        }

        // Attempt to deal damage to the player
        IDamageable d = hit.GetComponentInParent<IDamageable>();  // Look for IDamageable component in the player
        if (d != null)
        {
            d.TakeDamage(damage);  // Apply damage to the player
            Debug.Log("Player hit!");
        }
        else
        {
            Debug.Log("No IDamageable found");
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackSlashSpawner : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private GameObject slashEffectPrefab;
    [SerializeField] private Transform slashSpawnPoint;
    [SerializeField] private Vector2 slashOffset = new Vector2(1f, 0.4f);

    [Header("Damage")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 attackOffset = new Vector2(1.2f, 0.5f);
    [SerializeField] private Vector2 attackBoxSize = new Vector2(1.8f, 1.4f);
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int damage = 1;

    [Header("Attack Cooldown")]
    [SerializeField] private float attackCooldown = 0.35f;
    [SerializeField] private bool debugLogs = false;

    [Header("References")]
    [SerializeField] private PlayerController2D playerController;

    private float lastAttackTime = -999f;

    public bool CanAttack => Time.time >= lastAttackTime + attackCooldown;

    private float GetFacingSign()
    {
        if (playerController == null)
            return 1f;

        return playerController.FacingSign >= 0f ? 1f : -1f;
    }

    private Vector3 GetMirroredWorldPoint(Transform point, Vector2 fallbackOffset)
    {
        float facingSign = GetFacingSign();

        if (point != null)
        {
            Vector3 local = point.localPosition;
            local.x = Mathf.Abs(local.x) * facingSign;
            return transform.TransformPoint(local);
        }

        return transform.position + new Vector3(
            Mathf.Abs(fallbackOffset.x) * facingSign,
            fallbackOffset.y,
            0f
        );
    }

    public void PerformAttack()
    {
        if (!CanAttack)
        {
            if (debugLogs)
                Debug.Log("Attack blocked by cooldown.", this);
            return;
        }

        lastAttackTime = Time.time;

        SpawnSlash();
        DoDamage();

        if (debugLogs)
            Debug.Log("Attack performed.", this);
    }

    public void SpawnSlash()
    {
        if (slashEffectPrefab == null)
        {
            Debug.LogWarning("slashEffectPrefab is NULL", this);
            return;
        }

        float facingSign = GetFacingSign();
        bool facingRight = facingSign > 0f;

        Vector3 spawnPosition = GetMirroredWorldPoint(slashSpawnPoint, slashOffset);
        spawnPosition.z = 0f;

        Quaternion rotation = facingRight
            ? Quaternion.Euler(0f, 0f, -15f)
            : Quaternion.Euler(0f, 180f, 15f);

        GameObject slash = Instantiate(slashEffectPrefab, spawnPosition, rotation);

        PlayerSlashHitEffect effect = slash.GetComponent<PlayerSlashHitEffect>();
        if (effect != null)
            effect.SetFacing(facingRight);

        if (debugLogs)
            Debug.Log($"Slash spawned at {spawnPosition} | facing={(facingRight ? "Right" : "Left")}", this);
    }

    private void DoDamage()
    {
        Vector2 center = GetMirroredWorldPoint(attackPoint, attackOffset);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f, enemyLayer);

        HashSet<BossAI2D> damagedBosses = new HashSet<BossAI2D>();

        foreach (Collider2D hit in hits)
        {
            BossAI2D boss = hit.GetComponentInParent<BossAI2D>();
            if (boss == null)
                continue;

            if (damagedBosses.Contains(boss))
                continue;

            damagedBosses.Add(boss);
            boss.TakeDamage(damage);

            if (debugLogs)
                Debug.Log($"Hit boss {boss.name} for {damage} damage.", boss);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        float facingSign = 1f;
        if (playerController != null)
            facingSign = playerController.FacingSign >= 0f ? 1f : -1f;

        Vector2 center;

        if (attackPoint != null)
        {
            Vector3 local = attackPoint.localPosition;
            local.x = Mathf.Abs(local.x) * facingSign;
            center = transform.TransformPoint(local);
        }
        else
        {
            center = (Vector2)transform.position + new Vector2(
                Mathf.Abs(attackOffset.x) * facingSign,
                attackOffset.y
            );
        }

        Gizmos.DrawWireCube(center, attackBoxSize);
    }
}
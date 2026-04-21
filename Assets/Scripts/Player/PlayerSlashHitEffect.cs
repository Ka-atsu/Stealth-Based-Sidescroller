using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerSlashHitEffect : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Impact")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Vector3 hitEffectOffset = Vector3.zero;
    [SerializeField] private bool destroySlashOnHit = false;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly HashSet<BossAI2D> hitBosses = new HashSet<BossAI2D>();
    private bool facingRight = true;

    public void SetFacing(bool isFacingRight)
    {
        facingRight = isFacingRight;
    }

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other, "Enter");
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other, "Stay");
    }

    private void TryHit(Collider2D other, string phase)
    {
        if (other == null)
            return;

        BossAI2D boss = other.GetComponentInParent<BossAI2D>();
        if (boss == null)
        {
            Log($"Touched {other.name} during {phase}, but no BossAI2D found.");
            return;
        }

        bool colliderLayerMatches = ((1 << other.gameObject.layer) & enemyLayer) != 0;
        bool bossRootLayerMatches = ((1 << boss.gameObject.layer) & enemyLayer) != 0;

        if (!colliderLayerMatches && !bossRootLayerMatches)
        {
            Log($"Touched {other.name} during {phase}, but layer check failed. Collider layer={LayerMask.LayerToName(other.gameObject.layer)}, Boss layer={LayerMask.LayerToName(boss.gameObject.layer)}");
            return;
        }

        if (hitBosses.Contains(boss))
            return;

        hitBosses.Add(boss);

        SpawnHitEffect(other);
        boss.TakeDamage(damage);

        Log($"HIT boss {boss.name} during {phase} for {damage} damage.");

        if (destroySlashOnHit)
            Destroy(gameObject);
    }

    private void SpawnHitEffect(Collider2D enemyCollider)
    {
        if (hitEffectPrefab == null)
            return;

        Vector2 closest = enemyCollider.ClosestPoint(transform.position);
        Vector3 spawnPos = new Vector3(closest.x, closest.y, 0f) + hitEffectOffset;

        Quaternion rot = facingRight
            ? Quaternion.identity
            : Quaternion.Euler(0f, 180f, 0f);

        Instantiate(hitEffectPrefab, spawnPos, rot);
    }

    private void Log(string msg)
    {
        if (debugLogs)
            Debug.Log("[PlayerSlashHitEffect] " + msg, this);
    }
}
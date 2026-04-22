using System.Collections;
using UnityEngine;

public class BossAI2D : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Chase,
        Attack,
        Cooldown,
        Hurt,
        Dead
    }

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform player;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private BossHitVFXSpawner2D hitVFXSpawner;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 20;
    [SerializeField] private float moveSpeed = 3f;

    [Header("Box Detection")]
    [SerializeField] private Vector2 detectionBoxSize = new Vector2(8f, 4f);
    [SerializeField] private Vector2 detectionBoxOffset = new Vector2(4f, 0f);
    [SerializeField] private LayerMask playerLayer;

    [Header("Attack")]
    [SerializeField] private Vector2 attackBoxSize = new Vector2(2f, 2f);
    [SerializeField] private Vector2 attackBoxOffset = new Vector2(1.5f, 0f);
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackAnimationTimeout = 1.2f;

    [Header("Hurt")]
    [SerializeField] private float hurtDuration = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private BossState currentState;
    private int currentHealth;
    private bool isFacingRight = true;
    private bool canAttack = true;
    private bool isBusy;
    private bool hasDealtDamageThisAttack;
    private bool attackAnimationFinished;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    public BossState CurrentState => currentState;
    public event System.Action<BossState> OnStateChanged;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        hitVFXSpawner = GetComponent<BossHitVFXSpawner2D>();
    }

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (hitVFXSpawner == null)
            hitVFXSpawner = GetComponent<BossHitVFXSpawner2D>();

        currentHealth = maxHealth;

        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
                player = foundPlayer.transform;
        }

        ChangeState(BossState.Idle);
    }

    private void Update()
    {
        if (currentState == BossState.Dead)
            return;

        if (player == null)
            return;

        UpdateStateLogic();
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (currentState == BossState.Dead)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (currentState == BossState.Chase)
        {
            ChasePlayer();
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void UpdateStateLogic()
    {
        if (isBusy)
            return;

        bool canSeePlayer = IsPlayerInsideDetectionBox();
        bool inAttackRange = IsPlayerInsideAttackBox();

        switch (currentState)
        {
            case BossState.Idle:
                if (canSeePlayer)
                    ChangeState(BossState.Chase);
                break;

            case BossState.Chase:
                FacePlayer();

                if (!canSeePlayer)
                {
                    ChangeState(BossState.Idle);
                }
                else if (inAttackRange && canAttack)
                {
                    StartCoroutine(AttackRoutine());
                }
                break;

            case BossState.Attack:
                break;

            case BossState.Cooldown:
                if (canSeePlayer && !inAttackRange)
                    ChangeState(BossState.Chase);
                else if (!canSeePlayer)
                    ChangeState(BossState.Idle);
                break;

            case BossState.Hurt:
                break;
        }
    }

    private void ChasePlayer()
    {
        if (player == null || rb == null)
            return;

        float direction = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    private IEnumerator AttackRoutine()
    {
        isBusy = true;
        canAttack = false;
        hasDealtDamageThisAttack = false;
        attackAnimationFinished = false;

        ChangeState(BossState.Attack);

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        FacePlayer();
        Log("Boss attack started");

        float timer = 0f;

        while (!attackAnimationFinished && currentState != BossState.Dead && timer < attackAnimationTimeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (currentState == BossState.Dead)
            yield break;

        ChangeState(BossState.Cooldown);

        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
        isBusy = false;

        if (currentState == BossState.Dead)
            yield break;

        if (IsPlayerInsideDetectionBox())
            ChangeState(BossState.Chase);
        else
            ChangeState(BossState.Idle);
    }

    public void OnAttackHitFrame()
    {
        if (currentState != BossState.Attack)
            return;

        DealDamageToPlayer();
    }

    public void OnAttackAnimationFinished()
    {
        if (currentState != BossState.Attack)
            return;

        attackAnimationFinished = true;
        Log("Attack animation finished");
    }

    public void DealDamageToPlayer()
    {
        if (hasDealtDamageThisAttack)
            return;

        if (player == null)
        {
            Debug.LogWarning("BossAI2D: player is null");
            return;
        }

        Collider2D hit = Physics2D.OverlapBox(GetAttackBoxCenter(), attackBoxSize, 0f, playerLayer);
        if (hit == null)
        {
            Log("Boss attack missed: no player in attack box");
            return;
        }

        PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("BossAI2D: PlayerHealth not found");
            return;
        }

        playerHealth.TakeDamage(contactDamage, transform.position);
        hasDealtDamageThisAttack = true;

        if (hitVFXSpawner != null)
            hitVFXSpawner.SpawnHitEffect(hit.transform.position);

        Log("Boss dealt damage to player");
    }

    public void TakeDamage(int damage)
    {
        if (currentState == BossState.Dead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Log("Boss took damage: " + damage + " | HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(HurtRoutine());
    }

    private IEnumerator HurtRoutine()
    {
        if (currentState == BossState.Dead)
            yield break;

        isBusy = true;
        ChangeState(BossState.Hurt);

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForSeconds(hurtDuration);

        isBusy = false;

        if (currentState == BossState.Dead)
            yield break;

        if (IsPlayerInsideDetectionBox())
            ChangeState(BossState.Chase);
        else
            ChangeState(BossState.Idle);
    }

    private void Die()
    {
        isBusy = true;
        ChangeState(BossState.Dead);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        Log("Boss died");
    }

    private bool IsPlayerInsideDetectionBox()
    {
        Collider2D hit = Physics2D.OverlapBox(GetDetectionBoxCenter(), detectionBoxSize, 0f, playerLayer);
        return hit != null;
    }

    private bool IsPlayerInsideAttackBox()
    {
        Collider2D hit = Physics2D.OverlapBox(GetAttackBoxCenter(), attackBoxSize, 0f, playerLayer);
        return hit != null;
    }

    private Vector2 GetDetectionBoxCenter()
    {
        float facingSign = isFacingRight ? 1f : -1f;

        return (Vector2)transform.position + new Vector2(
            detectionBoxOffset.x * facingSign,
            detectionBoxOffset.y
        );
    }

    private Vector2 GetAttackBoxCenter()
    {
        float facingSign = isFacingRight ? 1f : -1f;

        if (attackPoint != null)
            return attackPoint.position;

        return (Vector2)transform.position + new Vector2(
            attackBoxOffset.x * facingSign,
            attackBoxOffset.y
        );
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        if (player.position.x > transform.position.x && !isFacingRight)
            Flip();
        else if (player.position.x < transform.position.x && isFacingRight)
            Flip();
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    private void ChangeState(BossState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        OnStateChanged?.Invoke(currentState);

        Log("State -> " + currentState);
    }

    private void Log(string message)
    {
        if (!debugLogs)
            return;

        Debug.Log("[BossAI2D] " + message, this);
    }

    private void OnDrawGizmosSelected()
    {
        float facingSign = isFacingRight ? 1f : -1f;

        Vector2 boxCenter = (Vector2)transform.position + new Vector2(
            detectionBoxOffset.x * facingSign,
            detectionBoxOffset.y
        );

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(boxCenter, detectionBoxSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            attackPoint != null
                ? (Vector2)attackPoint.position
                : (Vector2)transform.position + new Vector2(attackBoxOffset.x * facingSign, attackBoxOffset.y),
            attackBoxSize
        );
    }
}
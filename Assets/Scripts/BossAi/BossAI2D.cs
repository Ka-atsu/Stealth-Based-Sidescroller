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

    public enum AttackMode
    {
        None,
        Melee,
        Ranged
    }

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform player;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform rangedSpawnPoint;
    [SerializeField] private BossHitVFXSpawner2D hitVFXSpawner;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 20;
    [SerializeField] private float moveSpeed = 3f;

    [Header("Detection")]
    [SerializeField] private Vector2 detectionBoxSize = new Vector2(10f, 5f);
    [SerializeField] private Vector2 detectionBoxOffset = new Vector2(5f, 0f);
    [SerializeField] private LayerMask playerLayer;

    [Header("Melee Attack")]
    [SerializeField] private Vector2 attackBoxSize = new Vector2(2f, 2f);
    [SerializeField] private Vector2 attackBoxOffset = new Vector2(1.5f, 0f);
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float meleeCooldown = 1.25f;

    [Header("Ranged Attack")]
    [SerializeField] private bool enableRangedAttack = true;
    [SerializeField] private GameObject rangedProjectilePrefab;
    [SerializeField] private float rangedMinDistance = 3f;
    [SerializeField] private float rangedMaxDistance = 9f;
    [SerializeField] private int rangedDamage = 1;
    [SerializeField] private float rangedProjectileSpeed = 9f;
    [SerializeField] private float rangedImpactRadius = 0.8f;
    [SerializeField] private float rangedCooldown = 2f;
    [SerializeField] private Vector2 rangedSpawnOffset = new Vector2(1.5f, 0.4f);

    [Header("Attack Timing")]
    [SerializeField] private float attackAnimationTimeout = 1.2f;

    [Header("Hurt")]
    [SerializeField] private float hurtDuration = 0.35f;
    [SerializeField] private float hurtKnockbackForceX = 7f;
    [SerializeField] private float hurtKnockbackForceY = 2.5f;
    [SerializeField] private float hurtVelocityDamping = 18f;
    [SerializeField] private bool interruptAttackWhenHit = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private BossState currentState;
    private AttackMode currentAttackMode = AttackMode.None;

    private int currentHealth;
    private bool isFacingRight = true;
    private bool canAttack = true;
    private bool isBusy;
    private bool attackAnimationFinished;
    private bool hasDealtDamageThisAttack;

    private Vector2 lastKnownPlayerPosition;
    private bool hasLastKnownPlayerPosition;
    private Vector2 queuedRangedTargetPosition;

    private Coroutine attackRoutine;
    private Coroutine hurtRoutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    public BossState CurrentState => currentState;
    public AttackMode CurrentAttackMode => currentAttackMode;

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

        if (player != null)
        {
            lastKnownPlayerPosition = player.position;
            hasLastKnownPlayerPosition = true;
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
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (currentState == BossState.Hurt)
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, hurtVelocityDamping * Time.fixedDeltaTime);
            rb.linearVelocity = velocity;
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

        if (canSeePlayer && player != null)
        {
            lastKnownPlayerPosition = player.position;
            hasLastKnownPlayerPosition = true;
        }

        bool inMeleeRange = IsPlayerInsideAttackBox();
        bool canUseRanged = CanUseRangedAttack(canSeePlayer, inMeleeRange);

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
                else if (inMeleeRange && canAttack)
                {
                    StartAttack(AttackMode.Melee);
                }
                else if (canUseRanged && canAttack)
                {
                    StartAttack(AttackMode.Ranged);
                }
                break;

            case BossState.Attack:
                break;

            case BossState.Cooldown:
                if (!canSeePlayer)
                    ChangeState(BossState.Idle);
                else if (!isBusy)
                    ChangeState(BossState.Chase);
                break;

            case BossState.Hurt:
                break;
        }
    }

    private void StartAttack(AttackMode mode)
    {
        if (attackRoutine != null)
            return;

        attackRoutine = StartCoroutine(AttackRoutine(mode));
    }

    private bool CanUseRangedAttack(bool canSeePlayer, bool inMeleeRange)
    {
        if (!enableRangedAttack)
            return false;

        if (rangedProjectilePrefab == null)
            return false;

        if (!canSeePlayer)
            return false;

        if (inMeleeRange)
            return false;

        if (player == null)
            return false;

        float distance = Mathf.Abs(player.position.x - transform.position.x);
        return distance >= rangedMinDistance && distance <= rangedMaxDistance;
    }

    private void ChasePlayer()
    {
        if (player == null || rb == null)
            return;

        float direction = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    private IEnumerator AttackRoutine(AttackMode mode)
    {
        isBusy = true;
        canAttack = false;
        attackAnimationFinished = false;
        hasDealtDamageThisAttack = false;
        currentAttackMode = mode;

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (mode == AttackMode.Ranged)
        {
            queuedRangedTargetPosition = hasLastKnownPlayerPosition
                ? lastKnownPlayerPosition
                : (player != null ? (Vector2)player.position : (Vector2)transform.position);

            FaceTarget(queuedRangedTargetPosition);
            Log("Attack mode = Ranged | Target = " + queuedRangedTargetPosition);
        }
        else
        {
            FacePlayer();
            Log("Attack mode = Melee");
        }

        ChangeState(BossState.Attack);

        float timer = 0f;

        while (!attackAnimationFinished && currentState != BossState.Dead && timer < attackAnimationTimeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (currentState == BossState.Dead)
        {
            attackRoutine = null;
            yield break;
        }

        ChangeState(BossState.Cooldown);

        float cooldown = mode == AttackMode.Ranged ? rangedCooldown : meleeCooldown;
        yield return new WaitForSeconds(cooldown);

        if (currentState == BossState.Dead)
        {
            attackRoutine = null;
            yield break;
        }

        currentAttackMode = AttackMode.None;
        canAttack = true;
        isBusy = false;
        attackRoutine = null;

        if (IsPlayerInsideDetectionBox())
            ChangeState(BossState.Chase);
        else
            ChangeState(BossState.Idle);
    }

    public void OnAttackHitFrame()
    {
        if (currentState != BossState.Attack)
            return;

        if (currentAttackMode == AttackMode.Melee)
        {
            DealDamageToPlayer();
        }
        else if (currentAttackMode == AttackMode.Ranged)
        {
            FireRangedProjectile();
        }
    }

    public void OnAttackAnimationFinished()
    {
        if (currentState != BossState.Attack)
            return;

        attackAnimationFinished = true;
        Log("Attack animation finished");
    }

    private void DealDamageToPlayer()
    {
        if (hasDealtDamageThisAttack)
            return;

        Collider2D hit = Physics2D.OverlapBox(GetAttackBoxCenter(), attackBoxSize, 0f, playerLayer);
        if (hit == null)
        {
            Log("Melee missed");
            return;
        }

        PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = hit.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("BossAI2D: PlayerHealth not found");
            return;
        }

        playerHealth.TakeDamage(contactDamage, transform.position);
        hasDealtDamageThisAttack = true;

        if (hitVFXSpawner != null)
            hitVFXSpawner.SpawnHitEffect(hit.transform.position);

        Log("Boss dealt melee damage");
    }

    private void FireRangedProjectile()
    {
        if (hasDealtDamageThisAttack)
            return;

        if (rangedProjectilePrefab == null)
        {
            Debug.LogWarning("BossAI2D: rangedProjectilePrefab is missing", this);
            return;
        }

        Vector2 spawnPosition = GetRangedSpawnPosition();
        GameObject projectileObject = Instantiate(rangedProjectilePrefab, spawnPosition, Quaternion.identity);

        BossTargetProjectile2D projectile = projectileObject.GetComponent<BossTargetProjectile2D>();
        if (projectile == null)
            projectile = projectileObject.GetComponentInChildren<BossTargetProjectile2D>();

        if (projectile == null)
        {
            Debug.LogWarning("BossAI2D: ranged projectile prefab needs BossTargetProjectile2D on root or child", projectileObject);
            Destroy(projectileObject);
            return;
        }

        projectile.Initialize(
            queuedRangedTargetPosition,
            rangedDamage,
            rangedProjectileSpeed,
            rangedImpactRadius,
            playerLayer,
            transform.position
        );

        hasDealtDamageThisAttack = true;
        Log("Boss fired ranged projectile");
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

        if (interruptAttackWhenHit)
            InterruptCurrentAction();

        if (hurtRoutine != null)
            StopCoroutine(hurtRoutine);

        hurtRoutine = StartCoroutine(HurtRoutine());
    }

    private IEnumerator HurtRoutine()
    {
        if (currentState == BossState.Dead)
            yield break;

        isBusy = true;
        canAttack = false;
        currentAttackMode = AttackMode.None;
        attackAnimationFinished = false;
        hasDealtDamageThisAttack = false;

        ChangeState(BossState.Hurt);

        ApplyHurtKnockback();

        yield return new WaitForSeconds(hurtDuration);

        hurtRoutine = null;
        isBusy = false;
        canAttack = true;

        if (currentState == BossState.Dead)
            yield break;

        if (IsPlayerInsideDetectionBox())
            ChangeState(BossState.Chase);
        else
            ChangeState(BossState.Idle);
    }

    private void ApplyHurtKnockback()
    {
        if (rb == null)
            return;

        if (player == null)
        {
            rb.linearVelocity = new Vector2(
                isFacingRight ? -hurtKnockbackForceX : hurtKnockbackForceX,
                hurtKnockbackForceY
            );
            return;
        }

        float directionAwayFromPlayer = transform.position.x >= player.position.x ? 1f : -1f;

        rb.linearVelocity = new Vector2(
            directionAwayFromPlayer * hurtKnockbackForceX,
            hurtKnockbackForceY
        );
    }

    private void InterruptCurrentAction()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        attackAnimationFinished = false;
        hasDealtDamageThisAttack = false;
        currentAttackMode = AttackMode.None;
        canAttack = true;
        isBusy = false;
    }

    private void Die()
    {
        InterruptCurrentAction();

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

    private Vector2 GetRangedSpawnPosition()
    {
        if (rangedSpawnPoint != null)
            return rangedSpawnPoint.position;

        float facingSign = isFacingRight ? 1f : -1f;
        return (Vector2)transform.position + new Vector2(
            rangedSpawnOffset.x * facingSign,
            rangedSpawnOffset.y
        );
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        FaceTarget(player.position);
    }

    private void FaceTarget(Vector2 targetPosition)
    {
        if (targetPosition.x > transform.position.x && !isFacingRight)
            Flip();
        else if (targetPosition.x < transform.position.x && isFacingRight)
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

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            (Vector2)transform.position + new Vector2(detectionBoxOffset.x * facingSign, detectionBoxOffset.y),
            detectionBoxSize
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            attackPoint != null
                ? (Vector2)attackPoint.position
                : (Vector2)transform.position + new Vector2(attackBoxOffset.x * facingSign, attackBoxOffset.y),
            attackBoxSize
        );

        if (enableRangedAttack)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, rangedMinDistance);
            Gizmos.DrawWireSphere(transform.position, rangedMaxDistance);

            if (Application.isPlaying)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.2f);
            }
        }
    }
}
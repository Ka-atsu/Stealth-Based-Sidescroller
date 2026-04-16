using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EnemyMovement : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugAllyBlock = true;

    [Header("Speed")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("Initial Facing")]
    [SerializeField] private bool startFacingRight = true;

    [Tooltip("ON if raw sprite art faces right when FlipX is false. OFF if raw sprite art faces left by default.")]
    [SerializeField] private bool spriteFacesRightByDefault = false;

    [Header("Ground / Wall Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.5f;

    public Transform wallCheck;
    public float wallCheckDistance = 0.5f;

    public LayerMask groundLayer;

    [Header("Ally Blocking")]
    public LayerMask enemyLayer;
    public float allyDetectForwardOffset = 0.45f;
    public float allyDetectRadius = 0.45f;
    public float allyTurnDelayMin = 0.15f;
    public float allyTurnDelayMax = 0.40f;
    public float allyTurnCooldown = 0.25f;

    bool movingRight;
    public bool MovingRight => movingRight;

    public Transform attackPoint;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Light2D light2D;

    bool waitingToTurnFromAlly;
    float allyTurnTimer;
    float allyCooldownTimer;

    Transform blockedAlly;
    bool pendingFaceRightFromAllyBlock;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        light2D = GetComponentInChildren<Light2D>();

        ApplyFacing(startFacingRight, true);
        LogDebug($"Awake | startFacingRight={startFacingRight} | movingRight={movingRight}");
    }

    void FixedUpdate()
    {
        if (allyCooldownTimer > 0f)
            allyCooldownTimer -= Time.fixedDeltaTime;
    }

    public void Patrol()
    {
        if (HandleAllyBlockDuringPatrol())
            return;

        Move(patrolSpeed);

        if (!GroundAhead() || WallAhead())
        {
            LogDebug("Patrol blocked by wall/edge -> Flip");
            CancelAllyWait();
            Flip();
        }
    }

    public void Chase(Vector2 target)
    {
        CancelAllyWait();

        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * chaseSpeed, rb.linearVelocity.y);

        if (direction.x > 0.05f)
            FaceRight();
        else if (direction.x < -0.05f)
            FaceLeft();
    }

    public void MoveTo(Vector3 targetPosition)
    {
        CancelAllyWait();

        Vector2 direction = (targetPosition - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * patrolSpeed, rb.linearVelocity.y);

        if (direction.x > 0.05f)
            FaceRight();
        else if (direction.x < -0.05f)
            FaceLeft();
    }

    void Move(float speed)
    {
        rb.linearVelocity = new Vector2((movingRight ? 1f : -1f) * speed, rb.linearVelocity.y);
    }

    public void Stop()
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    bool HandleAllyBlockDuringPatrol()
    {
        if (allyCooldownTimer > 0f)
            return false;

        Transform allyAhead = GetBlockingAlly();

        if (waitingToTurnFromAlly)
        {
            Stop();

            if (allyAhead != null)
                blockedAlly = allyAhead;

            if (blockedAlly != null)
            {
                float deltaX = transform.position.x - blockedAlly.position.x;

                if (Mathf.Abs(deltaX) < 0.01f)
                    pendingFaceRightFromAllyBlock = GetInstanceID() > blockedAlly.GetInstanceID();
                else
                    pendingFaceRightFromAllyBlock = deltaX > 0f;
            }

            allyTurnTimer -= Time.fixedDeltaTime;

            LogDebug(
                $"Waiting from ally | ally={(blockedAlly != null ? blockedAlly.name : "null")} | " +
                $"timer={allyTurnTimer:F2} | turnTo={(pendingFaceRightFromAllyBlock ? "RIGHT" : "LEFT")}"
            );

            if (allyTurnTimer <= 0f)
            {
                waitingToTurnFromAlly = false;

                LogDebug(
                    $"Turn from ally block NOW -> {(pendingFaceRightFromAllyBlock ? "RIGHT" : "LEFT")} | " +
                    $"blockedBy={(blockedAlly != null ? blockedAlly.name : "null")}"
                );

                if (pendingFaceRightFromAllyBlock)
                    FaceRight();
                else
                    FaceLeft();

                blockedAlly = null;
                allyCooldownTimer = allyTurnCooldown;
            }

            return true;
        }

        if (allyAhead == null)
            return false;

        Stop();

        blockedAlly = allyAhead;

        float firstDeltaX = transform.position.x - blockedAlly.position.x;

        if (Mathf.Abs(firstDeltaX) < 0.01f)
            pendingFaceRightFromAllyBlock = GetInstanceID() > blockedAlly.GetInstanceID();
        else
            pendingFaceRightFromAllyBlock = firstDeltaX > 0f;

        waitingToTurnFromAlly = true;
        allyTurnTimer = Random.Range(allyTurnDelayMin, allyTurnDelayMax);

        LogDebug(
            $"Ally detected ahead -> {blockedAlly.name} | myX={transform.position.x:F2} | allyX={blockedAlly.position.x:F2} | " +
            $"willTurn={(pendingFaceRightFromAllyBlock ? "RIGHT" : "LEFT")} | delay={allyTurnTimer:F2}"
        );

        return true;
    }

    void CancelAllyWait()
    {
        if (waitingToTurnFromAlly)
            LogDebug("Cancel ally wait");

        waitingToTurnFromAlly = false;
        allyTurnTimer = 0f;
        blockedAlly = null;
    }

    bool GroundAhead()
    {
        if (groundCheck == null)
            return true;

        bool hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);

        Debug.DrawRay(
            groundCheck.position,
            Vector2.down * groundCheckDistance,
            hit ? Color.green : Color.red
        );

        return hit;
    }

    bool WallAhead()
    {
        if (wallCheck == null)
            return false;

        Vector2 dir = movingRight ? Vector2.right : Vector2.left;

        bool hit = Physics2D.Raycast(
            wallCheck.position,
            dir,
            wallCheckDistance,
            groundLayer
        );

        Debug.DrawRay(
            wallCheck.position,
            dir * wallCheckDistance,
            hit ? Color.yellow : Color.gray
        );

        return hit;
    }

    Transform GetBlockingAlly()
    {
        Vector2 center = (Vector2)transform.position +
                         (movingRight ? Vector2.right : Vector2.left) * allyDetectForwardOffset;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, allyDetectRadius, enemyLayer);

        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            if (hit.transform == transform) continue;

            float deltaX = hit.transform.position.x - transform.position.x;

            // only count allies in front of current facing
            if (movingRight && deltaX < 0f) continue;
            if (!movingRight && deltaX > 0f) continue;

            float dist = Mathf.Abs(deltaX);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = hit.transform;
            }
        }

        Color c = best != null ? Color.magenta : Color.cyan;
        Debug.DrawLine(center + Vector2.left * allyDetectRadius, center + Vector2.right * allyDetectRadius, c);

        return best;
    }

    public void FaceRight()
    {
        ApplyFacing(true);
    }

    public void FaceLeft()
    {
        ApplyFacing(false);
    }

    void Flip()
    {
        ApplyFacing(!movingRight);
    }

    void ApplyFacing(bool faceRight, bool force = false)
    {
        if (!force && movingRight == faceRight)
            return;

        movingRight = faceRight;

        if (sr != null)
            sr.flipX = spriteFacesRightByDefault ? !movingRight : movingRight;

        if (wallCheck != null)
        {
            Vector3 pos = wallCheck.localPosition;
            pos.x = Mathf.Abs(pos.x) * (movingRight ? 1 : -1);
            wallCheck.localPosition = pos;
        }

        if (groundCheck != null)
        {
            Vector3 pos = groundCheck.localPosition;
            pos.x = Mathf.Abs(pos.x) * (movingRight ? 1 : -1);
            groundCheck.localPosition = pos;
        }

        if (attackPoint != null)
        {
            Vector3 pos = attackPoint.localPosition;
            pos.x = Mathf.Abs(pos.x) * (movingRight ? 1 : -1);
            attackPoint.localPosition = pos;
        }

        if (light2D != null)
        {
            light2D.transform.localRotation =
                movingRight
                ? Quaternion.Euler(0, 0, -90)
                : Quaternion.Euler(0, 0, 90);
        }

        LogDebug($"ApplyFacing -> {(movingRight ? "RIGHT" : "LEFT")} | spriteFlipX={(sr != null ? sr.flipX.ToString() : "no SR")}");
    }

    void LogDebug(string msg)
    {
        if (!debugAllyBlock) return;
        Debug.Log($"<color=magenta>[EnemyMovement]</color> {name}: {msg}", this);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position +
                         (movingRight ? Vector3.right : Vector3.left) * allyDetectForwardOffset;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, allyDetectRadius);

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Vector3 dir = movingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + dir * wallCheckDistance);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
        }

        if (blockedAlly != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, blockedAlly.position);
        }
    }
}
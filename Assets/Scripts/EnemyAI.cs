using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Alerted,
        Search,
        Return
    }

    public EnemyState currentState = EnemyState.Patrol;

    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("Detection")]
    public float detectionRange = 5f;
    public float losePlayerRange = 8f;

    [Header("Ground & Wall Detection")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.5f;
    public Transform wallCheck;
    public float wallCheckDistance = 0.5f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Transform player;

    private bool movingRight = true;

    private Vector2 lastKnownPlayerPosition;
    private float searchTimer = 3f;
    private float searchCounter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;

            case EnemyState.Alerted:
                Alerted();
                break;

            case EnemyState.Search:
                Search();
                break;

            case EnemyState.Return:
                Return();
                break;
        }
    }

    // =========================
    // PATROL (EDGE + WALL DETECTION)
    // =========================
    void Patrol()
    {
        Move(patrolSpeed);

        bool groundAhead = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        bool wallAhead = Physics2D.Raycast(
            wallCheck.position,
            movingRight ? Vector2.right : Vector2.left,
            wallCheckDistance,
            groundLayer
        );

        if (!groundAhead || wallAhead)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            Flip();
        }

        // ---- IMPROVED PLAYER DETECTION ----
        if (player != null)
        {
            Vector2 directionToPlayer = player.position - transform.position;

            // Check distance
            if (directionToPlayer.magnitude < detectionRange)
            {
                // Check if player is in front
                float dot = Vector2.Dot(directionToPlayer.normalized,
                                         movingRight ? Vector2.right : Vector2.left);

                if (dot > 0.5f) // player roughly in front
                {
                    // Check line of sight (not blocked by ground)
                    RaycastHit2D hit = Physics2D.Raycast(
                        transform.position,
                        directionToPlayer.normalized,
                        detectionRange,
                        groundLayer
                    );

                    if (hit.collider == null)
                    {
                        currentState = EnemyState.Alerted;
                    }
                }
            }
        }
    }

    // =========================
    // ALERTED (CHASE PLAYER)
    // =========================
    void Alerted()
    {
        lastKnownPlayerPosition = player.position;

        Vector2 direction = (player.position - transform.position).normalized;

        rb.linearVelocity = new Vector2(
            direction.x * chaseSpeed,
            rb.linearVelocity.y
        );

        if (direction.x > 0 && !movingRight)
            Flip();
        else if (direction.x < 0 && movingRight)
            Flip();

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > losePlayerRange)
        {
            searchCounter = 0f;
            currentState = EnemyState.Search;
        }
    }

    // =========================
    // SEARCH LAST POSITION
    // =========================
    void Search()
    {
        Vector2 direction = (lastKnownPlayerPosition - (Vector2)transform.position).normalized;

        rb.linearVelocity = new Vector2(
            direction.x * patrolSpeed,
            rb.linearVelocity.y
        );

        if (Vector2.Distance(transform.position, lastKnownPlayerPosition) < 0.2f)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            searchCounter += Time.fixedDeltaTime;

            if (searchCounter >= searchTimer)
            {
                currentState = EnemyState.Return;
            }
        }

        if (Vector2.Distance(transform.position, player.position) < detectionRange)
        {
            currentState = EnemyState.Alerted;
        }
    }

    // =========================
    // RETURN TO PATROL
    // =========================
    void Return()
    {
        Move(patrolSpeed);
        currentState = EnemyState.Patrol;
    }

    // =========================
    // BASIC MOVEMENT
    // =========================
    void Move(float speed)
    {
        rb.linearVelocity = new Vector2(
            (movingRight ? 1 : -1) * speed,
            rb.linearVelocity.y
        );
    }

    // =========================
    // FLIP ENEMY
    // =========================
    void Flip()
    {
        movingRight = !movingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // =========================
    // DEBUG VISUALS
    // =========================
    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                groundCheck.position,
                groundCheck.position + Vector3.down * groundCheckDistance
            );
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Vector3 dir = movingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(
                wallCheck.position,
                wallCheck.position + dir * wallCheckDistance
            );
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
    public float visionAngle = 30f;

    [Header("Ground & Wall Detection")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.5f;
    public Transform wallCheck;
    public float wallCheckDistance = 0.5f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Transform player;
    private PlayerMovement playerMovement;

    private SpriteRenderer sr;
    private Light2D visionLight;

    private bool movingRight = true;

    private Vector2 lastKnownPlayerPosition;
    private float searchTimer = 3f;
    private float searchCounter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        visionLight = GetComponentInChildren<Light2D>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerMovement = p.GetComponent<PlayerMovement>();
        }

        UpdateLightDirection();
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

        DetectPlayer();
    }

    void Alerted()
    {
        if (playerMovement != null && playerMovement.isHidden)
        {
            currentState = EnemyState.Search;
            return;
        }

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

        DetectPlayer();
    }

    void Return()
    {
        Move(patrolSpeed);
        currentState = EnemyState.Patrol;
    }

    void Move(float speed)
    {
        rb.linearVelocity = new Vector2(
            (movingRight ? 1 : -1) * speed,
            rb.linearVelocity.y
        );
    }

    void Flip()
    {
        movingRight = !movingRight;

        if (sr != null)
            sr.flipX = !movingRight;

        // Move wallCheck to correct side
        Vector3 wcPos = wallCheck.localPosition;
        wcPos.x = Mathf.Abs(wcPos.x) * (movingRight ? 1 : -1);
        wallCheck.localPosition = wcPos;

        UpdateLightDirection();
    }

    void UpdateLightDirection()
    {
        if (visionLight == null) return;

        visionLight.transform.localRotation = movingRight
            ? Quaternion.Euler(0, 0, -90)  // Right
            : Quaternion.Euler(0, 0, 90);  // Left
    }

    void DetectPlayer()
    {
        if (playerMovement != null && playerMovement.isHidden)
            return;

        Vector2 directionToPlayer = player.position - transform.position;

        if (directionToPlayer.magnitude > detectionRange)
            return;

        Vector2 forward = movingRight ? Vector2.right : Vector2.left;
        float angle = Vector2.Angle(forward, directionToPlayer);

        if (angle < visionAngle)
        {
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
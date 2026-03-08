using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EnemyMovement : MonoBehaviour
{
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    public Transform groundCheck;
    public float groundCheckDistance = 0.5f;

    public Transform wallCheck;
    public float wallCheckDistance = 0.5f;

    public LayerMask groundLayer;

    bool movingRight = true;
    public bool MovingRight => movingRight;

    public Transform attackPoint;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Light2D light2D;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        light2D = GetComponentInChildren<Light2D>();
    }

    public void Patrol()
    {
        Move(patrolSpeed);

        // Combined wall check and ground check
        if (!GroundAhead() || WallAhead())
            Flip();
    }

    public void Chase(Vector2 target)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;

        // Ensure we're only modifying the x component, preserving the y component for gravity or platforming
        rb.linearVelocity = new Vector2(direction.x * chaseSpeed, rb.linearVelocity.y);

        // Flip the sprite and adjust position if needed
        if (direction.x > 0 && !movingRight)
            Flip();
        else if (direction.x < 0 && movingRight)
            Flip();
    }

    // New MoveTo method to move directly to a target position (used for random or specific movement)
    public void MoveTo(Vector3 targetPosition)
    {
        Vector2 direction = (targetPosition - transform.position).normalized;

        // Move to the target position, modifying only the x component to preserve y (gravity/other forces)
        rb.linearVelocity = new Vector2(direction.x * patrolSpeed, rb.linearVelocity.y);

        // Flip the sprite if needed to face the target
        if (direction.x > 0 && !movingRight)
            Flip();
        else if (direction.x < 0 && movingRight)
            Flip();
    }

    void Move(float speed)
    {
        rb.linearVelocity = new Vector2((movingRight ? 1 : -1) * speed, rb.linearVelocity.y);  // Only modify x for movement
    }

    bool GroundAhead()
    {
        return Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
    }

    bool WallAhead()
    {
        return Physics2D.Raycast(
            wallCheck.position,
            movingRight ? Vector2.right : Vector2.left,
            wallCheckDistance,
            groundLayer
        );
    }

    void Flip()
    {
        movingRight = !movingRight;

        // Flip sprite
        if (sr != null)
            sr.flipX = !movingRight;

        // Flip wall check position
        if (wallCheck != null)
        {
            Vector3 wcPos = wallCheck.localPosition;
            wcPos.x = Mathf.Abs(wcPos.x) * (movingRight ? 1 : -1);
            wallCheck.localPosition = wcPos;
        }

        // Adjust light rotation
        if (light2D != null)
        {
            light2D.transform.localRotation =
                movingRight
                ? Quaternion.Euler(0, 0, -90)
                : Quaternion.Euler(0, 0, 90);
        }

        // Adjust attack point if needed
        if (attackPoint != null)
        {
            Vector3 atkPos = attackPoint.localPosition;
            atkPos.x = Mathf.Abs(atkPos.x) * (movingRight ? 1 : -1);
            attackPoint.localPosition = atkPos;
        }
    }
}
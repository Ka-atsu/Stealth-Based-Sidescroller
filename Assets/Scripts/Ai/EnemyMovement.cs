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

        if (!GroundAhead() || WallAhead())
            Flip();
    }

    public void Chase(Vector2 target)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;

        rb.linearVelocity = new Vector2(dir.x * chaseSpeed, rb.linearVelocity.y);

        if (dir.x > 0 && !movingRight)
            Flip();
        else if (dir.x < 0 && movingRight)
            Flip();
    }

    void Move(float speed)
    {
        rb.linearVelocity = new Vector2((movingRight ? 1 : -1) * speed, rb.linearVelocity.y);
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

        if (sr != null)
            sr.flipX = !movingRight;

        if (wallCheck != null)
        {
            Vector3 wcPos = wallCheck.localPosition;
            wcPos.x = Mathf.Abs(wcPos.x) * (movingRight ? 1 : -1);
            wallCheck.localPosition = wcPos;
        }

        if (light2D != null)
        {
            light2D.transform.localRotation =
                movingRight
                ? Quaternion.Euler(0, 0, -90)
                : Quaternion.Euler(0, 0, 90);
        }
    }
}
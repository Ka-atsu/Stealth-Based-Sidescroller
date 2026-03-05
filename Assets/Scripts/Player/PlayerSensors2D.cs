using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerSensors2D : MonoBehaviour
{
    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f;

    [Header("Wall Check")]
    public float wallCheckDistance = 0.1f;

    public bool IsGrounded { get; private set; }
    public bool WasGrounded { get; private set; }
    public bool IsTouchingWall { get; private set; }
    public int WallDirection { get; private set; } // -1 left, +1 right

    public event Action OnLanded;

    Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    public void Tick()
    {
        CheckGround();
        CheckWall();
    }

    void CheckGround()
    {
        Bounds b = col.bounds;
        Vector2 origin = new Vector2(b.center.x, b.min.y);

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        Debug.DrawRay(origin, Vector2.down * groundCheckDistance, Color.green);

        WasGrounded = IsGrounded;
        IsGrounded = hit;

        if (!WasGrounded && IsGrounded)
            OnLanded?.Invoke();
    }

    void CheckWall()
    {
        Bounds b = col.bounds;

        Vector2 leftOrigin = new Vector2(b.min.x, b.center.y);
        Vector2 rightOrigin = new Vector2(b.max.x, b.center.y);

        RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.left, wallCheckDistance, groundLayer);
        RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.right, wallCheckDistance, groundLayer);

        Debug.DrawRay(leftOrigin, Vector2.left * wallCheckDistance, Color.red);
        Debug.DrawRay(rightOrigin, Vector2.right * wallCheckDistance, Color.blue);

        if (leftHit)
        {
            IsTouchingWall = true;
            WallDirection = -1;
        }
        else if (rightHit)
        {
            IsTouchingWall = true;
            WallDirection = 1;
        }
        else
        {
            IsTouchingWall = false;
            WallDirection = 0;
        }
    }
}
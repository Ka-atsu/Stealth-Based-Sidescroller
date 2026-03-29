using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerSensors2D : MonoBehaviour
{
    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.15f;
    [SerializeField] private float groundProbeWidthPercent = 0.9f;
    [SerializeField] private float groundProbeHeight = 0.08f;
    [SerializeField] private float maxGroundAngle = 55f;

    [Header("Wall Check")]
    public float wallCheckDistance = 0.1f;
    [SerializeField] private float wallProbeWidth = 0.05f;
    [SerializeField] private float wallProbeHeightPercent = 0.7f;
    [SerializeField] private float wallMinNormalX = 0.6f;
    [SerializeField] private float wallMaxNormalY = 0.35f;

    public bool IsGrounded { get; private set; }
    public bool WasGrounded { get; private set; }
    public bool IsTouchingWall { get; private set; }
    public int WallDirection { get; private set; }

    public Vector2 GroundNormal { get; private set; } = Vector2.up;

    public event Action OnLanded;

    private Collider2D col;

    void Awake()
    {
        col = GetComponent<CapsuleCollider2D>();

        if (col == null)
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

        Vector2 castSize = new Vector2(
            b.size.x * groundProbeWidthPercent,
            groundProbeHeight
        );

        Vector2 castOrigin = new Vector2(
            b.center.x,
            b.min.y + castSize.y * 0.5f
        );

        RaycastHit2D hit = Physics2D.BoxCast(
            castOrigin,
            castSize,
            0f,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        WasGrounded = IsGrounded;

        if (hit.collider != null)
        {
            float groundAngle = Vector2.Angle(hit.normal, Vector2.up);

            if (groundAngle <= maxGroundAngle)
            {
                IsGrounded = true;
                GroundNormal = hit.normal;
            }
            else
            {
                IsGrounded = false;
                GroundNormal = Vector2.up;
            }
        }
        else
        {
            IsGrounded = false;
            GroundNormal = Vector2.up;
        }

        if (!WasGrounded && IsGrounded)
            OnLanded?.Invoke();

        Debug.DrawRay(castOrigin, Vector2.down * groundCheckDistance, IsGrounded ? Color.green : Color.yellow);
    }

    void CheckWall()
    {
        Bounds b = col.bounds;

        Vector2 castSize = new Vector2(
            wallProbeWidth,
            b.size.y * wallProbeHeightPercent
        );

        Vector2 leftOrigin = new Vector2(
            b.min.x + castSize.x * 0.5f,
            b.center.y
        );

        Vector2 rightOrigin = new Vector2(
            b.max.x - castSize.x * 0.5f,
            b.center.y
        );

        RaycastHit2D leftHit = Physics2D.BoxCast(
            leftOrigin,
            castSize,
            0f,
            Vector2.left,
            wallCheckDistance,
            groundLayer
        );

        RaycastHit2D rightHit = Physics2D.BoxCast(
            rightOrigin,
            castSize,
            0f,
            Vector2.right,
            wallCheckDistance,
            groundLayer
        );

        bool leftIsWall = IsValidWall(leftHit);
        bool rightIsWall = IsValidWall(rightHit);

        Debug.DrawRay(leftOrigin, Vector2.left * wallCheckDistance, leftIsWall ? Color.red : Color.gray);
        Debug.DrawRay(rightOrigin, Vector2.right * wallCheckDistance, rightIsWall ? Color.blue : Color.gray);

        if (leftIsWall)
        {
            IsTouchingWall = true;
            WallDirection = -1;
        }
        else if (rightIsWall)
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

    bool IsValidWall(RaycastHit2D hit)
    {
        if (hit.collider == null)
            return false;

        return Mathf.Abs(hit.normal.x) >= wallMinNormalX &&
               Mathf.Abs(hit.normal.y) <= wallMaxNormalY;
    }
}
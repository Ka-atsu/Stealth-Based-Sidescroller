using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 9f;
    public float acceleration = 12f;
    public float deceleration = 18f;

    [Header("Jump")]
    public float jumpForce = 14f;

    [Header("Gravity")]
    public float baseGravityScale = 3.5f;
    public float fallGravityMultiplier = 1.8f;
    public float lowJumpGravityMultiplier = 1.5f;
    public float maxFallSpeed = -28f;

    [Header("Apex Hang")]
    public float apexThreshold = 0.5f;
    public float apexGravityMultiplier = 0.7f;

    [Header("Coyote Time")]
    public float coyoteTime = 0.1f;
    private float coyoteCounter;

    [Header("Jump Buffer")]
    public float jumpBufferTime = 0.1f;
    private float jumpBufferCounter;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f;

    [Header("Wall Jump")]
    public float wallCheckDistance = 0.1f;
    public float wallSlideSpeed = 3f;
    public float wallJumpForce = 14f;
    public float wallJumpHorizontalForce = 11f;
    public float wallJumpLockTime = 0.15f;

    private Rigidbody2D rb;
    private Collider2D col;

    private Vector2 moveInput;

    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isJumpPressed;

    private float wallJumpLockCounter;
    private int wallDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.freezeRotation = true;
        rb.gravityScale = baseGravityScale;
    }

    private void FixedUpdate()
    {
        CheckGround();
        CheckWall();

        UpdateCoyoteTime();
        UpdateJumpBuffer();

        HandleWallSlide();

        if (wallJumpLockCounter <= 0f)
            HandleHorizontalMovement();

        HandleGravity();
        TryBufferedJump();

        wallJumpLockCounter -= Time.fixedDeltaTime;
    }

    // -----------------------
    // GROUND CHECK
    // -----------------------
    private void CheckGround()
    {
        Bounds bounds = col.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y);

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        Debug.DrawRay(origin, Vector2.down * groundCheckDistance, Color.green);

        isGrounded = hit;
    }

    // -----------------------
    // WALL CHECK
    // -----------------------
    private void CheckWall()
    {
        Bounds bounds = col.bounds;

        Vector2 leftOrigin = new Vector2(bounds.min.x, bounds.center.y);
        Vector2 rightOrigin = new Vector2(bounds.max.x, bounds.center.y);

        RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.left, wallCheckDistance, groundLayer);
        RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.right, wallCheckDistance, groundLayer);

        Debug.DrawRay(leftOrigin, Vector2.left * wallCheckDistance, Color.red);
        Debug.DrawRay(rightOrigin, Vector2.right * wallCheckDistance, Color.blue);

        if (leftHit)
        {
            isTouchingWall = true;
            wallDirection = -1;
        }
        else if (rightHit)
        {
            isTouchingWall = true;
            wallDirection = 1;
        }
        else
        {
            isTouchingWall = false;
        }
    }

    // -----------------------
    // SMOOTH MOVEMENT (LERP)
    // -----------------------
    private void HandleHorizontalMovement()
    {
        // Preserve wall jump momentum
        if (wallJumpLockCounter > 0f)
            return;

        float targetSpeed = moveInput.x * moveSpeed;

        // If airborne and no input, preserve horizontal momentum
        if (!isGrounded && Mathf.Abs(moveInput.x) < 0.1f)
            return;

        float smoothRate = Mathf.Abs(moveInput.x) > 0.1f
            ? acceleration
            : deceleration;

        float newX = Mathf.Lerp(
            rb.linearVelocity.x,
            targetSpeed,
            smoothRate * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    // -----------------------
    // WALL SLIDE
    // -----------------------
    private void HandleWallSlide()
    {
        if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0)
        {
            isWallSliding = true;
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
        else
        {
            isWallSliding = false;
        }
    }

    // -----------------------
    // GRAVITY CONTROL
    // -----------------------
    private void HandleGravity()
    {
        if (isWallSliding)
            return;

        if (!isGrounded)
        {
            float verticalSpeed = rb.linearVelocity.y;

            if (Mathf.Abs(verticalSpeed) < apexThreshold)
                rb.gravityScale = baseGravityScale * apexGravityMultiplier;
            else if (verticalSpeed < 0)
                rb.gravityScale = baseGravityScale * fallGravityMultiplier;
            else if (!isJumpPressed)
                rb.gravityScale = baseGravityScale * lowJumpGravityMultiplier;
            else
                rb.gravityScale = baseGravityScale;

            if (rb.linearVelocity.y < maxFallSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }
        else
        {
            rb.gravityScale = baseGravityScale;
        }
    }

    // -----------------------
    // JUMP SYSTEM
    // -----------------------
    private void UpdateCoyoteTime()
    {
        coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Time.fixedDeltaTime;
    }

    private void UpdateJumpBuffer()
    {
        jumpBufferCounter -= Time.fixedDeltaTime;
    }

    private void TryBufferedJump()
    {
        if (jumpBufferCounter > 0f)
        {
            if (isTouchingWall && !isGrounded)
            {
                PerformWallJump();
                jumpBufferCounter = 0f;
            }
            else if (coyoteCounter > 0f)
            {
                PerformJump();
                jumpBufferCounter = 0f;
            }
        }
    }

    private void PerformJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        coyoteCounter = 0f;
    }

    private void PerformWallJump()
    {
        wallJumpLockCounter = wallJumpLockTime;

        Vector2 jumpDir = new Vector2(-wallDirection * 0.7f, 1f).normalized;

        rb.linearVelocity = new Vector2(
            jumpDir.x * wallJumpHorizontalForce,
            wallJumpForce
        );

        isWallSliding = false;
    }

    // -----------------------
    // INPUT
    // -----------------------
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        isJumpPressed = value.isPressed;

        if (value.isPressed)
            jumpBufferCounter = jumpBufferTime;

        if (!value.isPressed && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
    }
}
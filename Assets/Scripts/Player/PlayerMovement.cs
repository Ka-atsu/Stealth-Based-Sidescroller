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
    public float wallJumpHorizontalForce = 16f;
    public float wallJumpLockTime = 0.15f;

    [Header("Sprint")]
    public float runSpeed = 14f;
    public float walkSpeed = 9f;
    private bool isRunning;

    [Header("Crouch")]
    public float crouchSpeed = 4f;
    private bool isCrouching;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    public Vector2 crouchColliderSize = new Vector2(1f, 1f);
    public Vector2 crouchColliderOffset = new Vector2(0f, -0.5f);

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.6f;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDirection;

    [Header("Celeste Dash FX")]
    public float freezeDuration = 0.05f;
    public float stretchX = 1.6f;
    public float stretchY = 0.6f;
    private Vector3 originalScale;
    private SpriteRenderer sr;
    private bool hasDashedInAir;

    private Rigidbody2D rb;
    private Collider2D col;

    private Vector2 moveInput;

    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isJumpPressed;
    private bool wasGrounded;

    private float wallJumpLockCounter;
    private int wallDirection;

    float stepTimer;
    public float stepInterval = 0.5f;

    [Header("Stealth")]
    public bool isHidden = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.freezeRotation = true;
        rb.gravityScale = baseGravityScale;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        originalColliderSize = box.size;
        originalColliderOffset = box.offset;

        originalScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
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

        // Dash handling
        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            dashTimer -= Time.fixedDeltaTime;

            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.gravityScale = baseGravityScale;

                rb.linearVelocity = Vector2.zero; // hard stop

                transform.localScale = originalScale;

                if (sr != null)
                    sr.color = Color.white;
            }

            return; // stop other movement while dashing
        }

        wallJumpLockCounter -= Time.fixedDeltaTime;
        dashCooldownTimer -= Time.fixedDeltaTime;
    }

    private System.Collections.IEnumerator FreezeFrame(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
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

        if (!wasGrounded && isGrounded)
        {
            NoiseSystem.MakeNoise(transform.position, 4f, NoiseType.JumpLanding);
        }

        if (isGrounded)
            hasDashedInAir = false;

        wasGrounded = isGrounded;
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
        if (wallJumpLockCounter > 0f)
            return;

        float currentSpeed;

        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (isRunning)
            currentSpeed = runSpeed;
        else
            currentSpeed = walkSpeed;

        float targetSpeed = moveInput.x * currentSpeed;

        // Preserve air momentum if no input
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

        // Clamp using currentSpeed
        newX = Mathf.Clamp(newX, -currentSpeed, currentSpeed);

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

        if (isGrounded && Mathf.Abs(moveInput.x) > 0.2f)
        {
            stepTimer += Time.fixedDeltaTime;

            if (stepTimer >= stepInterval)
            {
                if (isCrouching)
                    return;

                if (isRunning)
                    NoiseSystem.MakeNoise(transform.position, 4f, NoiseType.Run);
                else
                    NoiseSystem.MakeNoise(transform.position, 2f, NoiseType.Walk);

                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }
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

    private void ApplyCrouch()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();

        float heightDifference = originalColliderSize.y - crouchColliderSize.y;

        box.size = crouchColliderSize;

        // Move collider DOWN by half the removed height
        box.offset = new Vector2(
            originalColliderOffset.x,
            originalColliderOffset.y - heightDifference / 2f
        );

        isRunning = false;

        Debug.Log("Crouch Start");
    }

    private void ApplyStand()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();

        box.size = originalColliderSize;
        box.offset = originalColliderOffset;

        Debug.Log("Crouch End");
    }
    private void StartDash()
    {
        isDashing = true;

        NoiseSystem.MakeNoise(transform.position, 6f, NoiseType.Roll);

        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        // One air dash limit
        if (!isGrounded)
            hasDashedInAir = true;

        // Direction
        if (Mathf.Abs(moveInput.x) > 0.1f || Mathf.Abs(moveInput.y) > 0.1f)
            dashDirection = moveInput.normalized;
        else
            dashDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        rb.gravityScale = 0f;

        // Freeze frame
        StartCoroutine(FreezeFrame(freezeDuration));

        // Squash & stretch
        transform.localScale = new Vector3(
            originalScale.x * stretchX,
            originalScale.y * stretchY,
            originalScale.z
        );

        // Flash
        if (sr != null)
            sr.color = Color.white * 2f;
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

    public void OnSprint(InputValue value)
    {
        if (isCrouching) return; // cannot sprint while crouching

        isRunning = true;
    }

    public void OnSprintRelease(InputValue value)
    {
        isRunning = false;
    }

    public void OnCrouch(InputValue value)
    {
        isCrouching = true;
        ApplyCrouch();
    }

    public void OnCrouchRelease(InputValue value)
    {
        isCrouching = false;
        ApplyStand();
    }

    public void OnDash(InputValue value)
    {
        if (!isGrounded && hasDashedInAir)
            return;

        if (!value.isPressed)
            return;

        if (dashCooldownTimer > 0f)
            return;

        if (isDashing)
            return;

        StartDash();
    }
}
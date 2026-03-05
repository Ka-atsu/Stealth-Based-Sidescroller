using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerJump2D : MonoBehaviour
{
    [Header("Jump")]
    public float jumpForce = 14f;

    [Header("Coyote Time")]
    public float coyoteTime = 0.1f;
    float coyoteCounter;

    [Header("Jump Buffer")]
    public float jumpBufferTime = 0.1f;
    float jumpBufferCounter;

    [Header("Wall Slide / Wall Jump")]
    public float wallSlideSpeed = 3f;
    public float wallJumpForce = 14f;
    public float wallJumpHorizontalForce = 16f;
    public float wallJumpLockTime = 0.15f;

    float wallJumpLockCounter;
    bool isWallSliding;

    [Header("Gravity")]
    public float baseGravityScale = 3.5f;
    public float fallGravityMultiplier = 1.8f;
    public float lowJumpGravityMultiplier = 1.5f;
    public float maxFallSpeed = -28f;

    [Header("Apex Hang")]
    public float apexThreshold = 0.5f;
    public float apexGravityMultiplier = 0.7f;

    Rigidbody2D rb;
    PlayerNoiseEmitter2D noise;

    public bool IsMovementLocked => wallJumpLockCounter > 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        noise = GetComponent<PlayerNoiseEmitter2D>();
        
        ApplyBaseGravity();

        // Optional: landing noise via sensors event if present
        var sensors = GetComponent<PlayerSensors2D>();
        if (sensors != null)
            sensors.OnLanded += () =>
            {
                if (noise != null) noise.Emit(4f, NoiseType.JumpLanding);
            };
    }

    public void ApplyBaseGravity()
    {
        rb.gravityScale = baseGravityScale;
    }

    public void BufferJump()
    {
        jumpBufferCounter = jumpBufferTime;
    }

    public void CutJump()
    {
        if (rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
    }

    public void TickFixed(float dt, PlayerSensors2D sensors, bool jumpHeld)
    {
        UpdateCoyote(dt, sensors.IsGrounded);
        UpdateJumpBuffer(dt);

        HandleWallSlide(sensors);

        wallJumpLockCounter -= dt;

        HandleGravity(sensors.IsGrounded, jumpHeld);

        TryBufferedJump(sensors);
    }

    void UpdateCoyote(float dt, bool isGrounded)
    {
        coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - dt;
    }

    void UpdateJumpBuffer(float dt)
    {
        jumpBufferCounter -= dt;
    }

    void HandleWallSlide(PlayerSensors2D sensors)
    {
        if (sensors.IsTouchingWall && !sensors.IsGrounded && rb.linearVelocity.y < 0f)
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

    void HandleGravity(bool isGrounded, bool jumpHeld)
    {
        if (isWallSliding) return;

        if (!isGrounded)
        {
            float vy = rb.linearVelocity.y;

            if (Mathf.Abs(vy) < apexThreshold)
                rb.gravityScale = baseGravityScale * apexGravityMultiplier;
            else if (vy < 0)
                rb.gravityScale = baseGravityScale * fallGravityMultiplier;
            else if (!jumpHeld)
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

    void TryBufferedJump(PlayerSensors2D sensors)
    {
        if (jumpBufferCounter <= 0f) return;

        if (sensors.IsTouchingWall && !sensors.IsGrounded)
        {
            PerformWallJump(sensors.WallDirection);
            jumpBufferCounter = 0f;
        }
        else if (coyoteCounter > 0f)
        {
            PerformJump();
            jumpBufferCounter = 0f;
        }
    }

    void PerformJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        coyoteCounter = 0f;
    }

    void PerformWallJump(int wallDirection)
    {
        wallJumpLockCounter = wallJumpLockTime;

        Vector2 jumpDir = new Vector2(-wallDirection * 0.7f, 1f).normalized;

        rb.linearVelocity = new Vector2(
            jumpDir.x * wallJumpHorizontalForce,
            wallJumpForce
        );

        isWallSliding = false;
    }
}
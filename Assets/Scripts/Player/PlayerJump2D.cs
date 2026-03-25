using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerJump2D : MonoBehaviour
{
    #region Inspector

    [Header("Jump")]
    public float jumpForce = 14f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
    public bool resetVerticalVelocityBeforeJump = true;

    [Header("Animation Timed Jump")]
    [SerializeField] private bool useAnimationTimedGroundJump = true;

    [Header("Coyote Time")]
    public float coyoteTime = 0.1f;
    float coyoteCounter;

    [Header("Jump Buffer")]
    public float jumpBufferTime = 0.1f;
    float jumpBufferCounter;

    [Header("Wall Grace")]
    public float wallCoyoteTime = 0.1f;
    float wallCoyoteCounter;
    int lastWallDirection;

    [Header("Wall Slide / Wall Jump")]
    public float wallSlideSpeed = 3f;
    public float wallSlideEnterMinFallSpeed = 0.5f;
    public float wallJumpForce = 14f;
    public float wallJumpHorizontalForce = 16f;
    public float wallJumpLockTime = 0.15f;

    float wallJumpLockCounter;
    bool isWallSliding;
    bool wasWallSliding;

    [Header("Gravity")]
    public float baseGravityScale = 3.5f;
    public float fallGravityMultiplier = 1.8f;
    public float lowJumpGravityMultiplier = 1.5f;
    public float maxFallSpeed = -28f;

    [Header("Apex Hang")]
    public float apexThreshold = 0.5f;
    public float apexGravityMultiplier = 0.7f;
    bool apexTriggered;

    #endregion

    #region Public Hooks

    public Action<float> OnJump;
    public Action<float> OnJumpCut;
    public Action<float> OnApex;
    public Action OnWallSlideStart;
    public Action OnWallSlideEnd;
    public Action<int> OnWallJump;
    public Action OnGroundJumpQueued;

    public bool IsMovementLocked => wallJumpLockCounter > 0f;
    public bool IsWallSliding => isWallSliding;
    public bool IsGroundJumpQueued => groundJumpQueued;

    #endregion

    #region Refs

    Rigidbody2D rb;
    PlayerNoiseEmitter2D noise;

    #endregion

    #region Internal State

    bool groundJumpQueued;

    #endregion

    #region Unity

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        noise = GetComponent<PlayerNoiseEmitter2D>();

        ApplyBaseGravity();

        var sensors = GetComponent<PlayerSensors2D>();
        if (sensors != null)
        {
            sensors.OnLanded += () =>
            {
                if (noise != null)
                    noise.Emit(4f, NoiseType.JumpLanding);

                groundJumpQueued = false;
            };
        }
    }

    #endregion

    #region Public API

    public void ApplyBaseGravity()
    {
        rb.gravityScale = baseGravityScale;
    }

    public void BufferJump()
    {
        jumpBufferCounter = jumpBufferTime;
    }

    public void QueueGroundJumpFromAnimation()
    {
        if (groundJumpQueued)
            return;

        groundJumpQueued = true;
        OnGroundJumpQueued?.Invoke();
    }

    public void ReleaseGroundJumpFromAnimation()
    {
        if (!groundJumpQueued)
            return;

        PerformJump();
        groundJumpQueued = false;
    }

    public void CancelQueuedGroundJump()
    {
        groundJumpQueued = false;
    }

    public void CutJump()
    {
        if (rb.linearVelocity.y <= 0f)
            return;

        float before = rb.linearVelocity.y;
        float after = before * jumpCutMultiplier;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, after);

        float cutStrength = Mathf.InverseLerp(0f, jumpForce, before);
        OnJumpCut?.Invoke(cutStrength);
    }

    public void TickFixed(float dt, PlayerSensors2D sensors, bool jumpHeld)
    {
        if (sensors == null)
        {
            HandleGravity(false, jumpHeld);
            return;
        }

        UpdateCoyote(dt, sensors.IsGrounded);
        UpdateWallCoyote(dt, sensors);
        UpdateJumpBuffer(dt);
        UpdateWallJumpLock(dt);

        HandleWallSlide(sensors);
        HandleGravity(sensors.IsGrounded, jumpHeld);
        TryBufferedJump(sensors);
        UpdateApexEvent(sensors.IsGrounded);

        wasWallSliding = isWallSliding;
    }

    #endregion

    #region Timers

    void UpdateCoyote(float dt, bool isGrounded)
    {
        coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - dt;
    }

    void UpdateWallCoyote(float dt, PlayerSensors2D sensors)
    {
        if (sensors.IsTouchingWall && !sensors.IsGrounded)
        {
            wallCoyoteCounter = wallCoyoteTime;
            lastWallDirection = sensors.WallDirection;
        }
        else
        {
            wallCoyoteCounter -= dt;
        }
    }

    void UpdateJumpBuffer(float dt)
    {
        jumpBufferCounter -= dt;
    }

    void UpdateWallJumpLock(float dt)
    {
        wallJumpLockCounter -= dt;
    }

    #endregion

    #region Wall Slide

    void HandleWallSlide(PlayerSensors2D sensors)
    {
        bool canWallSlide =
            wallJumpLockCounter <= 0f &&
            sensors.IsTouchingWall &&
            !sensors.IsGrounded &&
            rb.linearVelocity.y < -wallSlideEnterMinFallSpeed;

        if (canWallSlide)
        {
            isWallSliding = true;
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);

            if (!wasWallSliding)
                OnWallSlideStart?.Invoke();
        }
        else
        {
            isWallSliding = false;

            if (wasWallSliding)
                OnWallSlideEnd?.Invoke();
        }
    }

    #endregion

    #region Gravity

    void HandleGravity(bool isGrounded, bool jumpHeld)
    {
        if (isWallSliding)
            return;

        if (!isGrounded)
        {
            float vy = rb.linearVelocity.y;

            if (Mathf.Abs(vy) < apexThreshold)
                rb.gravityScale = baseGravityScale * apexGravityMultiplier;
            else if (vy < 0f)
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
            apexTriggered = false;
        }
    }

    #endregion

    #region Jump Execution

    void TryBufferedJump(PlayerSensors2D sensors)
    {
        if (jumpBufferCounter <= 0f)
            return;

        if (wallCoyoteCounter > 0f && !sensors.IsGrounded)
        {
            PerformWallJump(lastWallDirection);
            return;
        }

        if (coyoteCounter > 0f)
        {
            if (useAnimationTimedGroundJump)
            {
                if (!groundJumpQueued)
                {
                    groundJumpQueued = true;
                    OnGroundJumpQueued?.Invoke();
                }
                return;
            }

            PerformJump();
        }
    }

    void PerformJump()
    {
        if (resetVerticalVelocityBeforeJump)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        rb.gravityScale = baseGravityScale;
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        apexTriggered = false;
        isWallSliding = false;
        groundJumpQueued = false;

        OnJump?.Invoke(1f);
    }

    void PerformWallJump(int wallDirection)
    {
        wallJumpLockCounter = wallJumpLockTime;
        wallCoyoteCounter = 0f;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        apexTriggered = false;
        isWallSliding = false;
        groundJumpQueued = false;

        rb.gravityScale = baseGravityScale;

        rb.linearVelocity = new Vector2(
            -wallDirection * wallJumpHorizontalForce,
            wallJumpForce
        );

        OnWallJump?.Invoke(-wallDirection);
    }

    #endregion

    #region Apex

    void UpdateApexEvent(bool isGrounded)
    {
        if (isGrounded || apexTriggered)
            return;

        float absY = Mathf.Abs(rb.linearVelocity.y);
        if (absY > apexThreshold)
            return;

        apexTriggered = true;

        float apexStrength = 1f - Mathf.InverseLerp(0f, apexThreshold, absY);
        OnApex?.Invoke(apexStrength);
    }

    #endregion
}
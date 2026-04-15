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
    public float wallCoyoteTime = 0.12f;
    float wallCoyoteCounter;
    int lastWallDirection;

    [Header("Wall Slide")]
    public float wallSlideSpeed = 2.4f;
    public float wallSlideEnterMinFallSpeed = 0.35f;
    public bool requireInputTowardWallToSlide = true;

    [Header("Wall Jump")]
    public float wallJumpForce = 15.5f;
    public float wallJumpHorizontalForce = 8f;
    public float wallJumpLockTime = 0.08f;

    [Range(0f, 1f)]
    public float wallJumpHorizontalSnap = 0.85f;

    public float wallJumpMinUpwardSpeed = 12f;

    float wallJumpLockCounter;
    bool isWallSliding;
    bool wasWallSliding;

    [Header("Gravity")]
    public float baseGravityScale = 3.8f;
    public float fallGravityMultiplier = 2.15f;
    public float lowJumpGravityMultiplier = 1.9f;
    public float maxFallSpeed = -30f;

    [Header("Apex Hang")]
    public float apexThreshold = 1.2f;
    public float apexGravityMultiplier = 0.65f;
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
    PlayerController2D controller;

    #endregion

    #region Internal State

    bool groundJumpQueued;

    #endregion

    #region Unity

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        noise = GetComponent<PlayerNoiseEmitter2D>();
        controller = GetComponent<PlayerController2D>();

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

    // moveInputX is optional so your old call still compiles.
    // Best feel: pass your actual horizontal input here.
    public void TickFixed(float dt, PlayerSensors2D sensors, bool jumpHeld, float moveInputX = 0f)
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

        HandleWallSlide(sensors, moveInputX);
        HandleGravity(sensors.IsGrounded, jumpHeld);
        TryBufferedJump(sensors);
        UpdateApexEvent(sensors.IsGrounded);

        wasWallSliding = isWallSliding;
    }

    #endregion

    #region Timers

    void UpdateCoyote(float dt, bool isGrounded)
    {
        coyoteCounter = isGrounded ? coyoteTime : Mathf.Max(coyoteCounter - dt, 0f);
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
            wallCoyoteCounter = Mathf.Max(wallCoyoteCounter - dt, 0f);
        }
    }

    void UpdateJumpBuffer(float dt)
    {
        jumpBufferCounter = Mathf.Max(jumpBufferCounter - dt, 0f);
    }

    void UpdateWallJumpLock(float dt)
    {
        wallJumpLockCounter = Mathf.Max(wallJumpLockCounter - dt, 0f);
    }

    #endregion

    #region Wall Slide

    void HandleWallSlide(PlayerSensors2D sensors, float moveInputX)
    {
        bool hasMoveInput = Mathf.Abs(moveInputX) > 0.01f;

        // If no moveInputX is passed from your controller yet, this still works.
        bool pressingTowardWall =
            !requireInputTowardWallToSlide ||
            !hasMoveInput ||
            Mathf.Sign(moveInputX) == sensors.WallDirection;

        bool canWallSlide =
            wallJumpLockCounter <= 0f &&
            sensors.IsTouchingWall &&
            !sensors.IsGrounded &&
            rb.linearVelocity.y < -wallSlideEnterMinFallSpeed &&
            pressingTowardWall;

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

        float currentX = rb.linearVelocity.x;
        float targetX = -wallDirection * wallJumpHorizontalForce;
        float launchX = Mathf.Lerp(currentX, targetX, wallJumpHorizontalSnap);

        float launchY = Mathf.Max(rb.linearVelocity.y, wallJumpMinUpwardSpeed);
        launchY = Mathf.Max(launchY, wallJumpForce);

        rb.linearVelocity = new Vector2(launchX, launchY);

        controller?.ForceFacing(-wallDirection);

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
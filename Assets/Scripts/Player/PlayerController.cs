using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Input State (read-only)")]
    public Vector2 MoveInput { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool RunHeld { get; private set; }

    public bool IsHanging { get; private set; }

    private PlayerControls playerControls;  // Reference to Input System actions

    private SmokeBombController smokeBombController;  // Reference to the SmokeBombController

    Rigidbody2D rb;
    float defaultGravity;

    PlayerSensors2D sensors;
    PlayerCrouch2D crouch;
    PlayerMotor2D motor;
    PlayerJump2D jump;
    PlayerDash2D dash;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        defaultGravity = rb.gravityScale;

        sensors = GetComponent<PlayerSensors2D>();
        crouch = GetComponent<PlayerCrouch2D>();
        motor = GetComponent<PlayerMotor2D>();
        jump = GetComponent<PlayerJump2D>();
        dash = GetComponent<PlayerDash2D>();

        // Initialize the Input System actions
        playerControls = new PlayerControls();

        // Find the SmokeBombController component (either attached to the player or a separate object)
        smokeBombController = GetComponentInChildren<SmokeBombController>();
    }

    void OnEnable()
    {
        // Enable the input actions
        playerControls.Enable();

        // Listen for Smoke Bomb input (Q button) and trigger the action
        playerControls.Player.SmokeBomb.performed += context => TriggerSmokeBomb();
    }

    void OnDisable()
    {
        // Disable the input actions
        playerControls.Disable();
    }

    void FixedUpdate()
    {
        sensors.Tick();

        if (IsHanging)
        {
            rb.linearVelocity = Vector2.zero; // Stop all movement when hanging
            return;
        }

        // When dashing: keep sensors updated, but stop other movement systems (cleaner than your current version)
        if (dash.IsDashing)
        {
            dash.TickFixed(Time.fixedDeltaTime, sensors.IsGrounded);
            return;
        }

        // Update jump system first (coyote/buffer/wall slide/gravity/jump)
        jump.TickFixed(Time.fixedDeltaTime, sensors, JumpHeld);

        // Horizontal movement (blocked by wall jump lock like your original)
        motor.TickFixed(
            Time.fixedDeltaTime,
            MoveInput,
            sensors.IsGrounded,
            RunHeld,
            crouch.IsCrouching,
            jump.IsMovementLocked
        );

        // Dash cooldown ticks only when not dashing (matches your original return behavior)
        dash.TickCooldown(Time.fixedDeltaTime, sensors.IsGrounded);
    }

    // -----------------------
    // Called by PlayerInputHandler
    // -----------------------
    public void SetMove(Vector2 v)
    {
        if (IsHanging)
        {
            MoveInput = Vector2.zero;
            return;
        }

        MoveInput = v;
    }

    public void SetJumpHeld(bool held)
    {
        JumpHeld = held;

        if (IsHanging && held)
        {
            StopHang();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 8f);
            return;
        }

        if (held)
            jump.BufferJump();
        else
            jump.CutJump();
    }

    public void SetRunHeld(bool held)
    {
        if (crouch != null && crouch.IsCrouching) return;
        RunHeld = held;
    }

    public void CancelSprint() => RunHeld = false;

    public void SetCrouch(bool crouching)
    {
        if (IsHanging && crouching)
        {
            StopHang();
            return;
        }

        if (crouching)
            CancelSprint();

        crouch.SetCrouch(crouching);
    }

    public void TryDash()
    {
        dash.TryStartDash(
            moveInput: MoveInput,
            isGrounded: sensors.IsGrounded,
            facingSign: transform.localScale.x >= 0 ? 1f : -1f
        );
    }

    public void StartHang(Vector2 hangPosition)
    {
        IsHanging = true;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        transform.position = hangPosition;
    }

    public void StopHang()
    {
        IsHanging = false;

        rb.gravityScale = defaultGravity;
        MoveInput = Vector2.zero;

        PlayerHang2D hang = GetComponent<PlayerHang2D>();

        if (hang != null)
        {
            hang.SetHangCooldown(0.25f);
            hang.SetRehangCooldown(0.35f);
        }
    }

    // -------------------------
    // smoke bomb logic
    // -------------------------
    private void TriggerSmokeBomb()
    {
        if (smokeBombController != null)
        {
            smokeBombController.TriggerSmokeBomb();
        }
    }
}
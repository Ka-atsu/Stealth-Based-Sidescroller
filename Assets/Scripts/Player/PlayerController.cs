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

    public float DefaultGravity => defaultGravity;

    private PlayerControls playerControls;
    private SmokeBombController smokeBombController;

    Rigidbody2D rb;
    float defaultGravity;

    PlayerSensors2D sensors;
    PlayerCrouch2D crouch;
    PlayerMotor2D motor;
    PlayerJump2D jump;
    PlayerDash2D dash;

    PlayerGrappleHang2D grapple;

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
        grapple = GetComponent<PlayerGrappleHang2D>();

        playerControls = new PlayerControls();

        smokeBombController = GetComponentInChildren<SmokeBombController>();
    }

    void OnEnable()
    {
        playerControls.Enable();
        playerControls.Player.SmokeBomb.performed += context => TriggerSmokeBomb();
    }

    void OnDisable()
    {
        playerControls.Disable();
    }

    void FixedUpdate()
    {
        sensors.Tick();

        if (IsHanging)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (dash.IsDashing)
        {
            dash.TickFixed(Time.fixedDeltaTime, sensors.IsGrounded);
            return;
        }

        jump.TickFixed(Time.fixedDeltaTime, sensors, JumpHeld);

        motor.TickFixed(
            Time.fixedDeltaTime,
            MoveInput,
            sensors.IsGrounded,
            RunHeld,
            crouch.IsCrouching,
            jump.IsMovementLocked
        );

        dash.TickCooldown(Time.fixedDeltaTime, sensors.IsGrounded);
    }

    // -----------------------
    // Input
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
            grapple.DropHang();
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
            grapple.DropHang();
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

    // -----------------------
    // Hang state (called by grapple system)
    // -----------------------

    public void SetHanging(bool state)
    {
        IsHanging = state;

        if (state)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }
        else
        {
            rb.gravityScale = defaultGravity;
        }
    }

    // -------------------------
    // Smoke bomb
    // -------------------------

    private void TriggerSmokeBomb()
    {
        if (smokeBombController != null)
        {
            smokeBombController.TriggerSmokeBomb();
        }
    }
}
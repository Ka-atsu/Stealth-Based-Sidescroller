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
    public bool IsGrounded => sensors != null && sensors.IsGrounded;
    public float FacingSign { get; private set; } = 1f;

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
    PlayerStealthStrike2D stealthStrike;

    // -------- AUDIO VARIABLES --------
    bool wasGrounded;
    float stepTimer;
    public float stepInterval = 0.4f;

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
        stealthStrike = GetComponent<PlayerStealthStrike2D>();

        playerControls = new PlayerControls();

        smokeBombController = GetComponentInChildren<SmokeBombController>();
    }

    void OnEnable()
    {
        playerControls.Enable();
        playerControls.Player.SmokeBomb.performed += OnSmokeBombPerformed;
    }

    void OnDisable()
    {
        playerControls.Player.SmokeBomb.performed -= OnSmokeBombPerformed;
        playerControls.Disable();
    }

    void FixedUpdate()
    {
        sensors.Tick();

        // Landing sound detection
        if (!wasGrounded && sensors.IsGrounded)
        {
            NinjaAudioManager.Instance.PlayLanding();
        }
        wasGrounded = sensors.IsGrounded;

        if (IsHanging)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        dash.TickCooldown(Time.fixedDeltaTime, sensors.IsGrounded);

        if (dash.IsDashing)
        {
            dash.TickFixed(Time.fixedDeltaTime, sensors.IsGrounded);
            return;
        }

        if (dash.IsDashRecovering)
            return;

        jump.TickFixed(Time.fixedDeltaTime, sensors, JumpHeld);

        motor.TickFixed(
            Time.fixedDeltaTime,
            MoveInput,
            sensors.IsGrounded,
            RunHeld,
            crouch.IsCrouching,
            jump.IsMovementLocked
        );

        // Footstep audio
        HandleFootsteps();
    }

    // -----------------------
    // Footstep System
    // -----------------------

    void HandleFootsteps()
    {
        if (sensors.IsGrounded && Mathf.Abs(MoveInput.x) > 0.1f)
        {
            stepTimer -= Time.fixedDeltaTime;

            if (stepTimer <= 0f)
            {
                NinjaAudioManager.Instance.PlayFootstep();
                stepTimer = RunHeld ? 0.25f : stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    // -----------------------
    // Input
    // -----------------------

    public void SetMove(Vector2 v)
    {
        if (IsHanging)
        {
            MoveInput = Vector2.zero;
            RunHeld = false;
            return;
        }

        MoveInput = v;

        if (Mathf.Abs(v.x) > 0.01f)
            FacingSign = Mathf.Sign(v.x);
        else
            RunHeld = false;
    }

    public void SetJumpHeld(bool held)
    {
        JumpHeld = held;

        if (IsHanging && held)
        {
            grapple.DropHang();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 8f);

            NinjaAudioManager.Instance.PlayJump();
            return;
        }

        if (held)
        {
            jump.BufferJump();
            NinjaAudioManager.Instance.PlayJump();
        }
        else
            jump.CutJump();
    }

    public void SetRunHeld(bool held)
    {
        if (crouch != null && crouch.IsCrouching)
            return;

        RunHeld = held;
    }

    public void CancelSprint()
    {
        RunHeld = false;
    }

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
            facingSign: FacingSign
        );

        if (dash.IsDashing)
            NinjaAudioManager.Instance.PlayDash();
    }

    public void TryStealthStrike()
    {
        if (IsHanging)
            return;

        if (dash != null && dash.IsDashing)
            return;

        if (stealthStrike == null)
            return;

        stealthStrike.TryStealthStrike(FacingSign);
    }

    // -----------------------
    // Hang state
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

    private void OnSmokeBombPerformed(InputAction.CallbackContext context)
    {
        TriggerSmokeBomb();
    }

    private void TriggerSmokeBomb()
    {
        if (smokeBombController != null)
        {
            smokeBombController.TriggerSmokeBomb();
        }
    }
}
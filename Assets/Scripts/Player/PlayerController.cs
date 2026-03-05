using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Input State (read-only)")]
    public Vector2 MoveInput { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool RunHeld { get; private set; }

    Rigidbody2D rb;

    PlayerSensors2D sensors;
    PlayerCrouch2D crouch;
    PlayerMotor2D motor;
    PlayerJump2D jump;
    PlayerDash2D dash;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        sensors = GetComponent<PlayerSensors2D>();
        crouch = GetComponent<PlayerCrouch2D>();
        motor = GetComponent<PlayerMotor2D>();
        jump = GetComponent<PlayerJump2D>();
        dash = GetComponent<PlayerDash2D>();
    }

    void FixedUpdate()
    {
        sensors.Tick();

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
    public void SetMove(Vector2 v) => MoveInput = v;

    public void SetJumpHeld(bool held)
    {
        JumpHeld = held;

        if (held)
            jump.BufferJump();
        else
            jump.CutJump(); // low jump cut like yours
    }

    public void SetRunHeld(bool held)
    {
        if (crouch != null && crouch.IsCrouching) return;
        RunHeld = held;
    }

    public void CancelSprint() => RunHeld = false;

    public void SetCrouch(bool crouching)
    {
        if (crouching)
            CancelSprint(); // matches your ApplyCrouch() behavior

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
}
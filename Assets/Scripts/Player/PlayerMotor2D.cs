using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor2D : MonoBehaviour
{
    [Header("Movement")]
    public float acceleration = 12f;
    public float deceleration = 18f;

    [Header("Sprint")]
    public float runSpeed = 14f;
    public float walkSpeed = 9f;

    [Header("Crouch")]
    public float crouchSpeed = 4f;

    [Header("Footsteps")]
    public float stepInterval = 0.5f;

    Rigidbody2D rb;
    PlayerNoiseEmitter2D noise;

    float stepTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        noise = GetComponent<PlayerNoiseEmitter2D>();
    }

    public void TickFixed(
        float dt,
        Vector2 moveInput,
        bool isGrounded,
        bool runHeld,
        bool isCrouching,
        bool movementLocked
    )
    {
        if (movementLocked) return;

        float currentSpeed =
            isCrouching ? crouchSpeed :
            runHeld ? runSpeed :
            walkSpeed;

        float targetSpeed = moveInput.x * currentSpeed;

        // Preserve air momentum if no input (your original behavior)
        if (!isGrounded && Mathf.Abs(moveInput.x) < 0.1f)
            return;

        float smoothRate = Mathf.Abs(moveInput.x) > 0.1f ? acceleration : deceleration;

        float newX = Mathf.Lerp(rb.linearVelocity.x, targetSpeed, smoothRate * dt);
        newX = Mathf.Clamp(newX, -currentSpeed, currentSpeed);

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

        HandleFootsteps(dt, moveInput, isGrounded, runHeld, isCrouching);
    }

    void HandleFootsteps(float dt, Vector2 moveInput, bool isGrounded, bool runHeld, bool isCrouching)
    {
        if (!isGrounded || Mathf.Abs(moveInput.x) <= 0.2f)
        {
            stepTimer = 0f;
            return;
        }

        if (isCrouching) return;

        stepTimer += dt;
        if (stepTimer < stepInterval) return;

        if (noise != null)
            noise.Emit(runHeld ? 4f : 2f, runHeld ? NoiseType.Run : NoiseType.Walk);

        stepTimer = 0f;
    }
}
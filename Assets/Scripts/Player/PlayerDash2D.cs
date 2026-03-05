using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash2D : MonoBehaviour
{
    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.6f;

    [Header("Celeste Dash FX")]
    public float freezeDuration = 0.05f;
    public float stretchX = 1.6f;
    public float stretchY = 0.6f;

    public bool IsDashing { get; private set; }

    Rigidbody2D rb;
    SpriteRenderer sr;
    Vector3 originalScale;

    PlayerJump2D jump;
    PlayerNoiseEmitter2D noise;

    float dashTimer;
    float dashCooldownTimer;
    Vector2 dashDirection;
    bool hasDashedInAir;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        jump = GetComponent<PlayerJump2D>();
        noise = GetComponent<PlayerNoiseEmitter2D>();
    }

    public void TryStartDash(Vector2 moveInput, bool isGrounded, float facingSign)
    {
        if (!isGrounded && hasDashedInAir) return;
        if (dashCooldownTimer > 0f) return;
        if (IsDashing) return;

        IsDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        if (!isGrounded)
            hasDashedInAir = true;

        if (Mathf.Abs(moveInput.x) > 0.1f || Mathf.Abs(moveInput.y) > 0.1f)
            dashDirection = moveInput.normalized;
        else
            dashDirection = facingSign >= 0 ? Vector2.right : Vector2.left;

        if (noise != null)
            noise.Emit(6f, NoiseType.Roll);

        rb.gravityScale = 0f;

        StartCoroutine(FreezeFrame(freezeDuration));

        transform.localScale = new Vector3(
            originalScale.x * stretchX,
            originalScale.y * stretchY,
            originalScale.z
        );

        if (sr != null)
            sr.color = Color.white * 2f;
    }

    public void TickFixed(float dt, bool isGrounded)
    {
        if (isGrounded)
            hasDashedInAir = false;

        if (!IsDashing) return;

        rb.linearVelocity = dashDirection * dashSpeed;
        dashTimer -= dt;

        if (dashTimer > 0f) return;

        IsDashing = false;

        // restore gravity via jump’s base gravity (so it stays in one place)
        rb.gravityScale = (jump != null) ? jump.baseGravityScale : 3.5f;

        rb.linearVelocity = Vector2.zero; // hard stop (your original)
        transform.localScale = originalScale;

        if (sr != null)
            sr.color = Color.white;
    }

    // cooldown ticks only when NOT dashing (to match your original return)
    public void TickCooldown(float dt, bool isGrounded)
    {
        if (isGrounded)
            hasDashedInAir = false;

        if (IsDashing) return;
        dashCooldownTimer -= dt;
    }

    IEnumerator FreezeFrame(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}
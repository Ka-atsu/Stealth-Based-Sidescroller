using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash2D : MonoBehaviour
{
    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.6f;

    [Header("Dash FX")]
    public float freezeDuration = 0.05f;

    [Header("Dash Layer")]
    [SerializeField] private string dashLayerName = "PlayerDash";

    [Header("After Image")]
    [SerializeField] private GameObject afterImagePrefab;
    [SerializeField] private float afterImageSpacing = 0.03f;
    [SerializeField] private Color afterImageColor = new Color(1f, 1f, 1f, 0.35f);

    public bool IsDashing { get; private set; }

    Rigidbody2D rb;
    SpriteRenderer sr;

    PlayerJump2D jump;
    PlayerNoiseEmitter2D noise;

    float dashTimer;
    float dashCooldownTimer;
    float afterImageTimer;
    Vector2 dashDirection;
    bool hasDashedInAir;

    int dashLayer;
    int originalLayerBeforeDash;
    float originalGravityScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        jump = GetComponent<PlayerJump2D>();
        noise = GetComponent<PlayerNoiseEmitter2D>();

        originalGravityScale = rb.gravityScale;

        dashLayer = LayerMask.NameToLayer(dashLayerName);

        if (dashLayer == -1)
            Debug.LogWarning($"Layer '{dashLayerName}' does not exist.");
    }

    public void TryStartDash(Vector2 moveInput, bool isGrounded, float facingSign)
    {
        if (!isGrounded && hasDashedInAir) return;
        if (dashCooldownTimer > 0f) return;
        if (IsDashing) return;

        IsDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        afterImageTimer = 0f;

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

        originalLayerBeforeDash = gameObject.layer;
        if (dashLayer != -1)
            gameObject.layer = dashLayer;
    }

    public void TickFixed(float dt, bool isGrounded)
    {
        if (isGrounded)
            hasDashedInAir = false;

        if (!IsDashing) return;

        rb.linearVelocity = dashDirection * dashSpeed;

        afterImageTimer -= dt;
        if (afterImagePrefab != null && afterImageTimer <= 0f)
        {
            SpawnAfterImage();
            afterImageTimer = afterImageSpacing;
        }

        dashTimer -= dt;
        if (dashTimer > 0f) return;

        EndDash();
    }

    public void TickCooldown(float dt, bool isGrounded)
    {
        if (isGrounded)
            hasDashedInAir = false;

        if (IsDashing) return;

        dashCooldownTimer -= dt;
        if (dashCooldownTimer < 0f)
            dashCooldownTimer = 0f;
    }

    void EndDash()
    {
        IsDashing = false;

        rb.gravityScale = (jump != null) ? jump.baseGravityScale : originalGravityScale;
        rb.linearVelocity *= 0.35f;

        gameObject.layer = originalLayerBeforeDash;
    }

    void SpawnAfterImage()
    {
        if (sr == null || afterImagePrefab == null || sr.sprite == null)
            return;

        GameObject obj = Instantiate(afterImagePrefab, transform.position, transform.rotation);
        SpriteRenderer ghostSR = obj.GetComponent<SpriteRenderer>();
        PlayerDashAfterImage2D ghost = obj.GetComponent<PlayerDashAfterImage2D>();

        if (ghostSR != null)
        {
            ghostSR.sortingLayerID = sr.sortingLayerID;
            ghostSR.sortingOrder = sr.sortingOrder - 1;
        }

        if (ghost != null)
        {
            ghost.Setup(
                sr.sprite,
                transform.localScale,
                sr.flipX,
                afterImageColor
            );
        }
    }

    IEnumerator FreezeFrame(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}
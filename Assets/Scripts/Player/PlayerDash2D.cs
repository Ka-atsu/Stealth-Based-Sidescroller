using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash2D : MonoBehaviour
{
    [Header("Dash")]
    public float dashSpeed = 24f;
    public float dashDuration = 0.28f;
    public float dashCooldown = 0.6f;

    [Header("Dash Recovery")]
    [SerializeField] private float dashRecoverDuration = 0.05f;
    [SerializeField] private float endMomentumMultiplier = 0.25f;

    [Header("Dash FX")]
    public float freezeDuration = 0.02f;

    [Header("Camera Shake")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [SerializeField] private Vector3 dashStartImpulseVelocity = new Vector3(3.5f, 1.5f, 0f);
    [SerializeField] private Vector3 dashEndImpulseVelocity = new Vector3(2f, 0.8f, 0f);

    [Header("Dash Layer")]
    [SerializeField] private string dashLayerName = "PlayerDash";

    [Header("After Image")]
    [SerializeField] private GameObject afterImagePrefab;
    [SerializeField] private float afterImageSpacing = 0.03f;
    [SerializeField] private Color afterImageColor = new Color(1f, 1f, 1f, 0.35f);

    public bool IsDashing { get; private set; }
    public bool IsDashRecovering => dashRecoverTimer > 0f;

    Rigidbody2D rb;
    SpriteRenderer sr;

    PlayerJump2D jump;
    PlayerNoiseEmitter2D noise;

    float dashTimer;
    float dashCooldownTimer;
    float dashRecoverTimer;
    float afterImageTimer;
    Vector2 dashDirection;
    bool hasDashedInAir;

    int dashLayer;
    int originalLayerBeforeDash;
    float originalGravityScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        jump = GetComponent<PlayerJump2D>();
        noise = GetComponent<PlayerNoiseEmitter2D>();

        originalGravityScale = rb.gravityScale;
        dashLayer = LayerMask.NameToLayer(dashLayerName);

        if (dashLayer == -1)
            Debug.LogWarning($"Layer '{dashLayerName}' does not exist.");

        if (impulseSource == null)
            impulseSource = GetComponentInChildren<CinemachineImpulseSource>(true);
    }

    public void TryStartDash(Vector2 moveInput, bool isGrounded, float facingSign)
    {
        if (!isGrounded && hasDashedInAir) return;
        if (dashCooldownTimer > 0f) return;
        if (IsDashing) return;
        if (IsDashRecovering) return;

        IsDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashRecoverTimer = 0f;
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
        rb.linearVelocity = Vector2.zero;

        StartCoroutine(FreezeFrame(freezeDuration));

        originalLayerBeforeDash = gameObject.layer;
        if (dashLayer != -1)
            gameObject.layer = dashLayer;

        DoDashImpulse(dashStartImpulseVelocity, facingSign);
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
        if (dashTimer <= 0f)
            EndDash();
    }

    public void TickCooldown(float dt, bool isGrounded)
    {
        if (isGrounded)
            hasDashedInAir = false;

        if (dashRecoverTimer > 0f)
        {
            dashRecoverTimer -= dt;
            if (dashRecoverTimer < 0f)
                dashRecoverTimer = 0f;
        }

        if (IsDashing) return;

        dashCooldownTimer -= dt;
        if (dashCooldownTimer < 0f)
            dashCooldownTimer = 0f;
    }

    void EndDash()
    {
        IsDashing = false;
        dashRecoverTimer = dashRecoverDuration;

        rb.gravityScale = (jump != null) ? jump.baseGravityScale : originalGravityScale;

        float xMomentum = dashDirection.x * dashSpeed * endMomentumMultiplier;
        rb.linearVelocity = new Vector2(xMomentum, rb.linearVelocity.y);

        gameObject.layer = originalLayerBeforeDash;

        float facingSign = dashDirection.x >= 0f ? 1f : -1f;
        DoDashImpulse(dashEndImpulseVelocity, facingSign);
    }

    void DoDashImpulse(Vector3 baseVelocity, float facingSign)
    {
        if (impulseSource == null)
        {
            Debug.LogWarning("CinemachineImpulseSource is NULL on PlayerDash2D", this);
            return;
        }

        Vector3 velocity = new Vector3(
            Mathf.Abs(baseVelocity.x) * facingSign,
            baseVelocity.y,
            baseVelocity.z
        );

        impulseSource.GenerateImpulse(velocity);
    }

    void SpawnAfterImage()
    {
        if (sr == null || afterImagePrefab == null || sr.sprite == null)
            return;

        Transform spriteT = sr.transform;

        GameObject obj = Instantiate(afterImagePrefab, spriteT.position, spriteT.rotation);
        SpriteRenderer ghostSR = obj.GetComponent<SpriteRenderer>();
        PlayerDashAfterImage2D ghost = obj.GetComponent<PlayerDashAfterImage2D>();

        if (ghostSR != null)
        {
            ghostSR.sprite = sr.sprite;
            ghostSR.sortingLayerID = sr.sortingLayerID;
            ghostSR.sortingOrder = sr.sortingOrder - 1;
            ghostSR.flipX = sr.flipX;
        }

        if (ghost != null)
        {
            ghost.Setup(
                sr.sprite,
                spriteT.lossyScale,
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
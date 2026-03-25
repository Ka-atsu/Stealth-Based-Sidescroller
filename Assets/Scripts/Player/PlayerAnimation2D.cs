using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimation2D : MonoBehaviour
{
    [Header("Thresholds")]
    [SerializeField] private float moveSpeedThreshold = 0.1f;
    [SerializeField] private float runSpeedThreshold = 11f;

    [Header("Attack Animation")]
    [SerializeField] private string stealthStrikeTriggerName = "stealthStrike";
    [SerializeField] private float stealthStrikeAnimLockDuration = 0.18f;

    [Header("Jump Animation")]
    [SerializeField] private string jumpStartTriggerName = "jumpStart";

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Visual References")]
    [SerializeField] private Transform visuals;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private PlayerController2D controller;
    private PlayerDash2D dash;
    private PlayerHealth health;
    private PlayerJump2D jump;

    private bool isStealthStriking;
    private float stealthStrikeTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponentInParent<PlayerController2D>();
        dash = GetComponentInParent<PlayerDash2D>();
        health = GetComponentInParent<PlayerHealth>();
        jump = GetComponentInParent<PlayerJump2D>();

        ResolveVisualReferences();

        if (jump != null)
        {
            jump.OnGroundJumpQueued += HandleGroundJumpQueued;
            jump.OnJump += HandleJump;
        }
        else
        {
            Debug.LogError("PlayerJump2D NOT FOUND!");
        }
    }

    void OnDestroy()
    {
        if (jump != null)
        {
            jump.OnGroundJumpQueued -= HandleGroundJumpQueued;
            jump.OnJump -= HandleJump;
        }
    }

    void Update()
    {
        if (animator == null || rb == null)
            return;

        bool isDead = health != null && health.IsDead;

        if (isDead)
        {
            animator.SetBool("isDead", true);
            animator.SetBool("isGrounded", true);
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
            animator.SetBool("isDashing", false);
            animator.SetFloat("xVelocity", 0f);
            animator.SetFloat("yVelocity", 0f);
            return;
        }

        if (isStealthStriking)
        {
            stealthStrikeTimer -= Time.deltaTime;

            animator.SetBool("isDead", false);
            animator.SetBool("isGrounded", true);
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
            animator.SetBool("isDashing", false);
            animator.SetFloat("xVelocity", 0f);
            animator.SetFloat("yVelocity", 0f);

            if (stealthStrikeTimer <= 0f)
                isStealthStriking = false;

            return;
        }

        bool realGrounded = controller != null && controller.IsGrounded;
        bool isGroundJumpQueued = jump != null && jump.IsGroundJumpQueued;
        bool isDashing = dash != null && dash.IsDashing;

        float xVelocity = Mathf.Abs(rb.linearVelocity.x);
        float yVelocity = rb.linearVelocity.y;

        bool blockGroundedForJumpStart = isGroundJumpQueued && realGrounded;
        bool animGrounded = !blockGroundedForJumpStart && realGrounded;

        bool isMoving = animGrounded && xVelocity > moveSpeedThreshold;
        bool isRunning = !isDashing && isMoving && xVelocity >= runSpeedThreshold;
        bool isWalking = !isDashing && isMoving && !isRunning;

        bool isJumping =
            !isDashing &&
            !animGrounded &&
            yVelocity > 0.01f;

        bool isFalling =
            !isDashing &&
            !animGrounded &&
            yVelocity <= 0.01f;

        animator.SetBool("isDead", false);
        animator.SetBool("isGrounded", realGrounded);
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isFalling", isFalling);
        animator.SetBool("isDashing", isDashing);

        animator.SetFloat("yVelocity", yVelocity);
        animator.SetFloat("xVelocity", xVelocity);

        if (controller != null && spriteRenderer != null)
            spriteRenderer.flipX = controller.FacingSign < 0f;

        // Optional debug
        if (enableDebugLogs && !realGrounded)
        {
            Debug.Log($"[Anim] Y={yVelocity:F2} | Jumping={isJumping} | Falling={isFalling}");
        }
    }

    public void PlayDeathAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool("isDead", true);
    }

    public void PlayStealthStrikeAnimation()
    {
        if (animator == null)
            return;

        isStealthStriking = true;
        stealthStrikeTimer = stealthStrikeAnimLockDuration;

        animator.ResetTrigger(stealthStrikeTriggerName);
        animator.SetTrigger(stealthStrikeTriggerName);
    }

    public void PlayJumpStartAnimation()
    {
        if (animator == null)
            return;

        Log("PLAY JUMP START ANIMATION");

        animator.ResetTrigger(jumpStartTriggerName);
        animator.SetTrigger(jumpStartTriggerName);
    }

    void HandleGroundJumpQueued()
    {
        Log("JUMP QUEUED -> PLAY ANIMATION");
        PlayJumpStartAnimation();
    }

    void HandleJump(float strength)
    {
        Log("JUMP ACTUALLY RELEASED");
    }

    void Log(string msg)
    {
        if (enableDebugLogs)
            Debug.Log("[PlayerAnimation] " + msg);
    }

    void ResolveVisualReferences()
    {
        if (visuals == null)
        {
            Transform found = transform.Find("Visuals");
            if (found != null)
                visuals = found;
        }

        if (animator == null)
        {
            if (visuals != null)
                animator = visuals.GetComponent<Animator>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        if (spriteRenderer == null)
        {
            if (visuals != null)
                spriteRenderer = visuals.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }
}
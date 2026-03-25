using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimation2D : MonoBehaviour
{
    [Header("Thresholds")]
    [SerializeField] private float moveSpeedThreshold = 0.1f;
    [SerializeField] private float runSpeedThreshold = 11f;
    [SerializeField] private float verticalThreshold = 0.05f;

    [Header("Visual References")]
    [SerializeField] private Transform visuals;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private PlayerController2D controller;
    private PlayerDash2D dash;
    private PlayerHealth health;

    private bool wasGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController2D>();
        dash = GetComponent<PlayerDash2D>();
        health = GetComponent<PlayerHealth>();

        ResolveVisualReferences();
    }

    void Update()
    {
        if (animator == null || rb == null)
            return;

        bool isDead = health != null && health.IsDead;
        bool isGrounded = controller != null && controller.IsGrounded;
        bool isDashing = !isDead && dash != null && dash.IsDashing;

        float xVelocity = Mathf.Abs(rb.linearVelocity.x);
        float yVelocity = rb.linearVelocity.y;

        bool isMoving = isGrounded && xVelocity > moveSpeedThreshold;
        bool isRunning = !isDashing && isMoving && xVelocity >= runSpeedThreshold;
        bool isWalking = !isDashing && isMoving && !isRunning;

        bool justLeftGround = wasGrounded && !isGrounded;

        bool isJumping = !isDead && !isDashing && !isGrounded && yVelocity > verticalThreshold && !justLeftGround;
        bool isFalling = !isDead && !isDashing && !isGrounded && !isJumping;

        if (isDead)
        {
            isWalking = false;
            isRunning = false;
            isJumping = false;
            isFalling = false;
            isDashing = false;
        }

        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isFalling", isFalling);
        animator.SetBool("isDashing", isDashing);
        animator.SetBool("isDead", isDead);

        animator.SetFloat("yVelocity", yVelocity);
        animator.SetFloat("xVelocity", xVelocity);

        if (controller != null && spriteRenderer != null)
            spriteRenderer.flipX = controller.FacingSign < 0f;

        wasGrounded = isGrounded;
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
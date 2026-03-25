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

        bool isGrounded = controller != null && controller.IsGrounded;
        bool isDashing = dash != null && dash.IsDashing;

        float xVelocity = Mathf.Abs(rb.linearVelocity.x);
        float yVelocity = rb.linearVelocity.y;

        bool isMoving = isGrounded && xVelocity > moveSpeedThreshold;
        bool isRunning = !isDashing && isMoving && xVelocity >= runSpeedThreshold;
        bool isWalking = !isDashing && isMoving && !isRunning;

        bool justLeftGround = wasGrounded && !isGrounded;

        bool isJumping = !isDashing && !isGrounded && yVelocity > verticalThreshold && !justLeftGround;
        bool isFalling = !isDashing && !isGrounded && !isJumping;

        animator.SetBool("isDead", false);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isFalling", isFalling);
        animator.SetBool("isDashing", isDashing);

        animator.SetFloat("yVelocity", yVelocity);
        animator.SetFloat("xVelocity", xVelocity);

        if (controller != null && spriteRenderer != null)
            spriteRenderer.flipX = controller.FacingSign < 0f;

        wasGrounded = isGrounded;
    }

    public void PlayDeathAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool("isDead", true);
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
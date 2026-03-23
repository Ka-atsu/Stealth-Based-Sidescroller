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

    private bool wasGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController2D>();

        ResolveVisualReferences();
    }

    void Update()
    {
        if (animator == null || rb == null)
            return;

        bool isGrounded = controller != null && controller.IsGrounded;

        float xVelocity = Mathf.Abs(rb.linearVelocity.x);
        float yVelocity = rb.linearVelocity.y;

        bool isMoving = isGrounded && xVelocity > moveSpeedThreshold;
        bool isRunning = isGrounded && xVelocity >= runSpeedThreshold;
        bool isWalking = isMoving && !isRunning;

        bool justLeftGround = wasGrounded && !isGrounded;

        bool isJumping = !isGrounded && yVelocity > verticalThreshold && !justLeftGround;
        bool isFalling = !isGrounded && !isJumping;

        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isFalling", isFalling);
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
                animator = GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            if (visuals != null)
                spriteRenderer = visuals.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimation2D : MonoBehaviour
{
    [SerializeField] private float moveSpeedThreshold = 0.1f;
    [SerializeField] private float runSpeedThreshold = 14f;
    [SerializeField] private float verticalThreshold = 0.05f;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PlayerController2D controller;

    private bool wasGrounded;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        controller = GetComponent<PlayerController2D>();
    }

    void Update()
    {
        bool isGrounded = controller != null && controller.IsGrounded;

        float xVelocity = Mathf.Abs(rb.linearVelocity.x);
        float yVelocity = rb.linearVelocity.y;

        bool isMoving = isGrounded && xVelocity > moveSpeedThreshold;
        bool isRunning = isGrounded && xVelocity >= runSpeedThreshold;
        bool isWalking = isMoving && !isRunning;

        bool justLeftGround = wasGrounded && !isGrounded;

        // Rising = jump
        bool isJumping = !isGrounded && yVelocity > verticalThreshold && !justLeftGround;

        // Neutral or downward after leaving ground = fall
        bool isFalling = !isGrounded && !isJumping;

        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isFalling", isFalling);
        animator.SetFloat("yVelocity", yVelocity);
        animator.SetFloat("xVelocity", xVelocity);

        if (controller != null)
            spriteRenderer.flipX = controller.FacingSign < 0f;

        wasGrounded = isGrounded;
    }
}
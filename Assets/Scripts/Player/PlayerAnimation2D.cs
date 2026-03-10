using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimation2D : MonoBehaviour
{
    [SerializeField] private float inputThreshold = 0.01f;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PlayerController2D controller;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        controller = GetComponent<PlayerController2D>();
    }

    void Update()
    {
        bool isWalking = controller != null && Mathf.Abs(controller.MoveInput.x) > inputThreshold;
        animator.SetBool("isWalking", isWalking);

        if (controller != null)
        {
            spriteRenderer.flipX = controller.FacingSign < 0f;
        }
    }
}
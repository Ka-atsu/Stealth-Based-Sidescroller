using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimation2D : MonoBehaviour
{
    [SerializeField] private float walkThreshold = 0.1f;

    private Animator animator;
    private Rigidbody2D rb;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        bool isWalking = Mathf.Abs(rb.linearVelocity.x) > walkThreshold;
        animator.SetBool("isWalking", isWalking);
    }
}
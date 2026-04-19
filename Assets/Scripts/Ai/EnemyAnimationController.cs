using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private EnemyAttack attack;
    [SerializeField] private EnemyStateMachine stateMachine;

    [Header("Settings")]
    [SerializeField] private float moveThreshold = 0.05f;

    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private bool lastIsAttacking;

    void Reset()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        attack = GetComponent<EnemyAttack>();
        stateMachine = GetComponent<EnemyStateMachine>();
    }

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (attack == null) attack = GetComponent<EnemyAttack>();
        if (stateMachine == null) stateMachine = GetComponent<EnemyStateMachine>();
    }

    void Update()
    {
        UpdateMovementAnimation();
        UpdateAttackAnimation();
    }

    void UpdateMovementAnimation()
    {
        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > moveThreshold;

        // While actually in attack state, force walk off
        if (stateMachine != null &&
            stateMachine.currentState == EnemyStateMachine.EnemyState.Attack)
        {
            isMoving = false;
        }

        animator.SetBool(IsMovingHash, isMoving);
    }

    void UpdateAttackAnimation()
    {
        bool isAttackingNow = attack != null && attack.IsAttacking;

        if (isAttackingNow && !lastIsAttacking)
        {
            animator.SetTrigger(AttackHash);
        }

        lastIsAttacking = isAttackingNow;
    }
}
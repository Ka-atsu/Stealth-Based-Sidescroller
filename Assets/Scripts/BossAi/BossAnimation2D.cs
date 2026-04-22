using UnityEngine;

[RequireComponent(typeof(BossAI2D))]
public class BossAnimation2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossAI2D bossAI;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;

    [Header("Animator Parameters")]
    [SerializeField] private string moveSpeedParam = "MoveSpeed";
    [SerializeField] private string isMovingParam = "IsMoving";
    [SerializeField] private string isDeadParam = "IsDead";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string rangedAttackTrigger = "RangedAttack";
    [SerializeField] private string hurtTrigger = "Hurt";
    [SerializeField] private string dieTrigger = "Die";

    [Header("Settings")]
    [SerializeField] private float moveThreshold = 0.01f;
    [SerializeField] private bool useSeparateRangedTrigger = true;
    [SerializeField] private bool debugLogs = false;

    private void Reset()
    {
        bossAI = GetComponent<BossAI2D>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (bossAI == null)
            bossAI = GetComponent<BossAI2D>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (bossAI != null)
            bossAI.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (bossAI != null)
            bossAI.OnStateChanged -= HandleStateChanged;
    }

    private void Start()
    {
        if (bossAI == null || animator == null)
        {
            Debug.LogWarning("BossAnimation2D missing references.", this);
            return;
        }

        ApplyState(bossAI.CurrentState);
    }

    private void Update()
    {
        if (bossAI == null || animator == null || rb == null)
            return;

        float moveSpeed = Mathf.Abs(rb.linearVelocity.x);

        bool isMoving =
            bossAI.CurrentState != BossAI2D.BossState.Attack &&
            bossAI.CurrentState != BossAI2D.BossState.Hurt &&
            bossAI.CurrentState != BossAI2D.BossState.Dead &&
            moveSpeed > moveThreshold;

        animator.SetFloat(moveSpeedParam, moveSpeed);
        animator.SetBool(isMovingParam, isMoving);
        animator.SetBool(isDeadParam, bossAI.CurrentState == BossAI2D.BossState.Dead);

        if (debugLogs)
        {
            Debug.Log($"[BossAnimation2D] State={bossAI.CurrentState} MoveSpeed={moveSpeed:F2} IsMoving={isMoving}", this);
        }
    }

    private void HandleStateChanged(BossAI2D.BossState newState)
    {
        ApplyState(newState);
    }

    private void ApplyState(BossAI2D.BossState state)
    {
        if (animator == null)
            return;

        animator.SetBool(isDeadParam, state == BossAI2D.BossState.Dead);

        switch (state)
        {
            case BossAI2D.BossState.Attack:
                animator.ResetTrigger(hurtTrigger);
                animator.ResetTrigger(dieTrigger);

                if (useSeparateRangedTrigger && bossAI.CurrentAttackMode == BossAI2D.AttackMode.Ranged)
                {
                    animator.ResetTrigger(attackTrigger);
                    animator.SetTrigger(rangedAttackTrigger);
                    Log("Trigger RangedAttack");
                }
                else
                {
                    animator.ResetTrigger(rangedAttackTrigger);
                    animator.SetTrigger(attackTrigger);
                    Log("Trigger Attack");
                }
                break;

            case BossAI2D.BossState.Hurt:
                animator.ResetTrigger(attackTrigger);
                animator.ResetTrigger(rangedAttackTrigger);
                animator.ResetTrigger(dieTrigger);
                animator.SetTrigger(hurtTrigger);
                Log("Trigger Hurt");
                break;

            case BossAI2D.BossState.Dead:
                animator.ResetTrigger(attackTrigger);
                animator.ResetTrigger(rangedAttackTrigger);
                animator.ResetTrigger(hurtTrigger);
                animator.SetTrigger(dieTrigger);
                Log("Trigger Die");
                break;
        }
    }

    public void Animation_AttackHit()
    {
        if (bossAI == null)
            return;

        bossAI.OnAttackHitFrame();
        Log("Animation Event -> AttackHit");
    }

    public void Animation_AttackFinished()
    {
        if (bossAI == null)
            return;

        bossAI.OnAttackAnimationFinished();
        Log("Animation Event -> AttackFinished");
    }

    private void Log(string message)
    {
        if (!debugLogs)
            return;

        Debug.Log("[BossAnimation2D] " + message, this);
    }
}
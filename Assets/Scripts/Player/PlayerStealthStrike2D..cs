using UnityEngine;

public class PlayerStealthStrike2D : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugStealthStrike = true;

    [Header("Stealth Strike")]
    [SerializeField] private float strikeRange = 1.5f;
    [SerializeField] private Vector2 strikeOffset = new Vector2(0.7f, 0.7f);
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float strikeCooldown = 0.2f;
    [SerializeField] private float strikeAnimationLockDuration = 0.25f;

    [Header("Prompt")]
    [SerializeField] private GameObject stealthPromptVisual;
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private float promptFollowSpeed = 20f;
    [SerializeField] private bool keepPromptZ = true;

    private float nextStrikeTime;
    private PlayerController2D controller;
    private PlayerAnimation2D playerAnimation;
    private EnemyAI currentTarget;
    private EnemyAI pendingStrikeTarget;
    private bool strikeInProgress;

    void Awake()
    {
        controller = GetComponent<PlayerController2D>();
        playerAnimation = GetComponent<PlayerAnimation2D>();

        if (stealthPromptVisual != null)
            stealthPromptVisual.SetActive(false);
    }

    void Update()
    {
        if (strikeInProgress)
        {
            if (stealthPromptVisual != null && stealthPromptVisual.activeSelf)
                stealthPromptVisual.SetActive(false);
            return;
        }

        float facingSign = controller != null ? controller.FacingSign : 1f;

        currentTarget = FindBestTarget(facingSign);
        UpdatePrompt();
    }

    public void TryStealthStrike(float facingSign)
    {
        if (Time.time < nextStrikeTime)
        {
            Log("Blocked: cooldown");
            return;
        }

        if (strikeInProgress)
        {
            Log("Blocked: strike already in progress");
            return;
        }

        EnemyAI target = FindBestTarget(facingSign);
        if (target == null)
        {
            Log("No stealth target found");
            return;
        }

        nextStrikeTime = Time.time + strikeCooldown;
        strikeInProgress = true;
        pendingStrikeTarget = target;

        Log($"Stealth strike started on: {target.name}");

        pendingStrikeTarget.EnterStealthStrikeVictimState(transform);

        playerAnimation?.PlayStealthStrikeAnimation();

        if (controller != null)
            controller.Stun(strikeAnimationLockDuration);
    }

    // Animation Event on the hit frame
    public void OnStealthStrikeHit()
    {
        Log("STEALTH HIT EVENT CALLED");

        if (pendingStrikeTarget == null)
        {
            Log("Hit event fired but pendingStrikeTarget is null");
            return;
        }

        Log($"Destroying target: {pendingStrikeTarget.name}");
        pendingStrikeTarget.DieFromStealthStrike();
        pendingStrikeTarget = null;
    }

    // Animation Event on the last frame
    public void OnStealthStrikeFinished()
    {
        Log("STEALTH FINISHED EVENT CALLED");

        if (pendingStrikeTarget != null)
        {
            Log($"Strike finished without hit, releasing target: {pendingStrikeTarget.name}");
            pendingStrikeTarget.ExitStealthStrikeVictimState();
            pendingStrikeTarget = null;
        }

        strikeInProgress = false;
    }

    private EnemyAI FindBestTarget(float facingSign)
    {
        Vector2 strikeCenter = GetStrikeCenter(facingSign);
        Collider2D[] hits = Physics2D.OverlapCircleAll(strikeCenter, strikeRange, enemyMask);

        EnemyAI bestTarget = null;
        float closestSqrDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy == null)
                continue;

            if (!enemy.CanBeStealthKilledFrom(transform.position))
                continue;

            float sqrDistance = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;

            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                bestTarget = enemy;
            }
        }

        return bestTarget;
    }

    private void UpdatePrompt()
    {
        if (stealthPromptVisual == null)
            return;

        if (currentTarget == null)
        {
            if (stealthPromptVisual.activeSelf)
                stealthPromptVisual.SetActive(false);

            return;
        }

        if (!stealthPromptVisual.activeSelf)
            stealthPromptVisual.SetActive(true);

        Vector3 targetPos = GetPromptPosition(currentTarget);

        if (keepPromptZ)
            targetPos.z = stealthPromptVisual.transform.position.z;

        stealthPromptVisual.transform.position = Vector3.Lerp(
            stealthPromptVisual.transform.position,
            targetPos,
            promptFollowSpeed * Time.deltaTime
        );
    }

    private Vector3 GetPromptPosition(EnemyAI enemy)
    {
        Renderer render = enemy.GetComponentInChildren<Renderer>();
        if (render != null)
        {
            Vector3 topCenter = new Vector3(
                render.bounds.center.x,
                render.bounds.max.y,
                enemy.transform.position.z
            );

            return topCenter + promptOffset;
        }

        Collider2D col = enemy.GetComponentInChildren<Collider2D>();
        if (col != null)
        {
            Vector3 topCenter = new Vector3(
                col.bounds.center.x,
                col.bounds.max.y,
                enemy.transform.position.z
            );

            return topCenter + promptOffset;
        }

        return enemy.transform.position + promptOffset;
    }

    private Vector2 GetStrikeCenter(float facingSign)
    {
        return (Vector2)transform.position +
               new Vector2(Mathf.Abs(strikeOffset.x) * facingSign, strikeOffset.y);
    }

    private void Log(string msg)
    {
        if (!debugStealthStrike)
            return;

        //Debug.Log($"[PlayerStealthStrike2D] {msg}", this);
    }

    private void OnDrawGizmos()
    {
        float facingSign = 1f;

        if (Application.isPlaying)
        {
            PlayerController2D pc = GetComponent<PlayerController2D>();
            if (pc != null)
                facingSign = pc.FacingSign;
        }

        Vector2 strikeCenter = (Vector2)transform.position +
                               new Vector2(Mathf.Abs(strikeOffset.x) * facingSign, strikeOffset.y);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(strikeCenter, strikeRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(strikeCenter, 0.05f);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, strikeCenter);
    }
}
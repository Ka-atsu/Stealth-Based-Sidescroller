using UnityEngine;

public enum GuardResponseRole
{
    None,
    Hold,
    SearchLeft,
    SearchRight
}

public class EnemyAI : MonoBehaviour
{
    [Header("Debug")]
    public bool debugLogs = true;
    public bool debugPerception = false;
    public bool debugActions = false;

    [Header("Group Coordination")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float allyAlertRadius = 6f;
    [SerializeField] private float holdMinTime = 0.4f;
    [SerializeField] private float holdMaxTime = 1.2f;

    EnemyMovement movement;
    EnemyVision vision;
    EnemyStateMachine stateMachine;
    EnemyAttack attack;
    EnemyBloodTracker bloodTracker;
    EnemyHearing hearing;
    EnemySearchBehavior searchBehavior;
    Rigidbody2D rb;

    Transform player;

    Vector3 currentSearchTarget;

    EnemyStateMachine.EnemyState lastState;

    float searchTimer = 0f;
    [SerializeField] float searchDuration = 5f;

    bool lastCanSeePlayer;
    bool lastHasBlood;
    bool lastHearingSearch;

    string lastAction = "";

    bool isStealthStrikeVictim;
    Transform stealthStrikeAttacker;

    GuardResponseRole responseRole = GuardResponseRole.None;
    float holdTimer = 0f;
    bool hasSharedThisAlert = false;

    void Start()
    {
        movement = GetComponent<EnemyMovement>();
        vision = GetComponent<EnemyVision>();
        stateMachine = GetComponent<EnemyStateMachine>();
        attack = GetComponent<EnemyAttack>();
        bloodTracker = GetComponent<EnemyBloodTracker>();
        hearing = GetComponent<EnemyHearing>();
        searchBehavior = GetComponent<EnemySearchBehavior>();
        rb = GetComponent<Rigidbody2D>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;

        if (stateMachine != null)
            lastState = stateMachine.currentState;

        Log("EnemyAI started");
    }

    void FixedUpdate()
    {
        if (isStealthStrikeVictim)
        {
            if (movement != null)
                movement.Stop();

            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            LogAction("Locked as stealth strike victim");
            return;
        }

        if (player == null)
        {
            Log("No player found");
            return;
        }

        //----------------------------------
        // PERCEPTION
        //----------------------------------

        if (vision != null)
            vision.Detect();

        if (bloodTracker != null)
            bloodTracker.DetectNearbyBlood();

        bool canSeePlayer = vision != null && vision.CanSeePlayerNow;
        bool hasBlood = bloodTracker != null && bloodTracker.HasBloodTarget();
        bool hearingSearch = hearing != null && hearing.IsInvestigating();

        Vector3 hearingTarget = hearingSearch ? hearing.lastHeardPosition : Vector3.zero;

        LogPerceptionChanges(canSeePlayer, hasBlood, hearingSearch, hearingTarget);

        // Reset shared alert only when calm again
        if (!canSeePlayer &&
            !hearingSearch &&
            !hasBlood &&
            stateMachine.currentState == EnemyStateMachine.EnemyState.Patrol)
        {
            hasSharedThisAlert = false;
            responseRole = GuardResponseRole.None;
            holdTimer = 0f;
        }

        //----------------------------------
        // STATE DECISION (PRIORITY)
        //----------------------------------

        if (attack != null && attack.IsAttacking)
        {
            LogAction("Decision -> Keep Attacking");
            stateMachine.SetState(EnemyStateMachine.EnemyState.Attack);
        }
        else if (attack != null && attack.CanAttack() && canSeePlayer)
        {
            LogAction("Decision -> Attack");
            stateMachine.SetState(EnemyStateMachine.EnemyState.Attack);
        }
        else if (canSeePlayer)
        {
            LogAction("Decision -> Alerted");
            AlertNearbyGuard(player.position);
            stateMachine.SetState(EnemyStateMachine.EnemyState.Alerted);
        }
        else if (hasBlood)
        {
            LogAction("Decision -> FollowBlood");
            stateMachine.SetState(EnemyStateMachine.EnemyState.FollowBlood);
        }
        else if (hearingSearch && stateMachine.currentState != EnemyStateMachine.EnemyState.Search)
        {
            LogAction($"Decision -> Search from hearing at {hearingTarget}");

            AlertNearbyGuard(hearingTarget);

            if (responseRole == GuardResponseRole.None)
                EnterSearch(hearingTarget);

            if (hearing != null)
                hearing.StopInvestigating();
        }
        else if (stateMachine.currentState == EnemyStateMachine.EnemyState.Search)
        {
            // let search logic run
        }
        else if (stateMachine.currentState == EnemyStateMachine.EnemyState.FollowBlood && !hasBlood)
        {
            LogAction("Lost blood target -> Search");
            EnterSearch(transform.position);
        }
        else if (stateMachine.currentState != EnemyStateMachine.EnemyState.Patrol)
        {
            LogAction("Decision -> Patrol");
            stateMachine.SetState(EnemyStateMachine.EnemyState.Patrol);
        }

        //----------------------------------
        // DEBUG STATE CHANGE
        //----------------------------------

        if (stateMachine.currentState != lastState)
        {
            Log($"STATE -> {stateMachine.currentState}");
            lastState = stateMachine.currentState;
        }

        //----------------------------------
        // STATE EXECUTION
        //----------------------------------

        switch (stateMachine.currentState)
        {
            case EnemyStateMachine.EnemyState.Patrol:
                LogAction("Patrolling");
                movement.Patrol();
                break;

            case EnemyStateMachine.EnemyState.Alerted:
                LogAction($"Chasing player at {player.position}");
                movement.Chase(player.position);
                break;

            case EnemyStateMachine.EnemyState.FollowBlood:
                if (canSeePlayer)
                {
                    LogAction("Saw player while following blood -> Alerted");
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Alerted);
                    break;
                }

                if (hasBlood)
                {
                    Vector3 bloodTarget = bloodTracker.GetBloodTargetPosition();
                    LogAction($"Following blood target at {bloodTarget}");

                    movement.Chase(bloodTarget);

                    float horizontalDist = Mathf.Abs(transform.position.x - bloodTarget.x);
                    float verticalDist = Mathf.Abs(transform.position.y - bloodTarget.y);

                    if (horizontalDist < 0.4f)
                    {
                        LogAction("Reached blood target horizontally -> Next blood point");
                        bloodTracker.MoveToNextBloodTarget();
                    }
                    else if (verticalDist > 2.5f && horizontalDist < 0.75f)
                    {
                        LogAction("Blood too high but close horizontally -> Skipping to next blood point");
                        bloodTracker.MoveToNextBloodTarget();
                    }
                }
                else
                {
                    LogAction("No more blood -> Search");
                    EnterSearch(transform.position);
                }

                break;

            case EnemyStateMachine.EnemyState.Search:
                if (canSeePlayer)
                {
                    LogAction("Saw player during search -> Alerted");
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Alerted);
                    break;
                }

                if (hasBlood)
                {
                    LogAction("Found blood during search -> FollowBlood");
                    stateMachine.SetState(EnemyStateMachine.EnemyState.FollowBlood);
                    break;
                }

                searchTimer -= Time.deltaTime;

                if (responseRole == GuardResponseRole.Hold)
                {
                    holdTimer -= Time.deltaTime;
                    movement.Stop();

                    LogAction($"Holding position | hold left: {holdTimer:F2}");

                    if (holdTimer <= 0f)
                    {
                        responseRole = Random.value > 0.5f
                            ? GuardResponseRole.SearchLeft
                            : GuardResponseRole.SearchRight;

                        if (searchBehavior != null)
                            searchBehavior.ResetSearch();

                        LogAction($"Hold finished -> {responseRole}");
                    }
                }
                else if (responseRole == GuardResponseRole.SearchLeft ||
                         responseRole == GuardResponseRole.SearchRight)
                {
                    LogAction($"Assigned search {responseRole} around {currentSearchTarget} | time left: {searchTimer:F2}");
                    searchBehavior.SearchAssigned(currentSearchTarget, responseRole);
                }
                else
                {
                    LogAction($"Searching around {currentSearchTarget} | time left: {searchTimer:F2}");
                    searchBehavior.SearchRandomly(currentSearchTarget);
                }

                if (searchTimer <= 0f)
                {
                    LogAction("Search expired -> Return");
                    responseRole = GuardResponseRole.None;
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Return);
                }

                break;

            case EnemyStateMachine.EnemyState.Attack:
                LogAction("Stopping to attack");
                movement.Stop();

                if (attack != null && !attack.IsAttacking && attack.CanAttack())
                {
                    LogAction("TryAttack()");
                    attack.TryAttack();
                }

                break;

            case EnemyStateMachine.EnemyState.Return:
                LogAction("Returning to patrol");
                responseRole = GuardResponseRole.None;
                movement.Patrol();
                stateMachine.SetState(EnemyStateMachine.EnemyState.Patrol);
                break;
        }
    }

    //----------------------------------
    // GROUP COORDINATION
    //----------------------------------

    void AlertNearbyGuard(Vector3 target)
    {
        if (hasSharedThisAlert)
            return;

        hasSharedThisAlert = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, allyAlertRadius, enemyLayer);
        EnemyAI chosenAlly = null;
        float closestDist = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.transform == transform)
                continue;

            EnemyAI ally = hit.GetComponent<EnemyAI>();
            if (ally == null) continue;
            if (ally.isStealthStrikeVictim) continue;

            float dist = Vector2.Distance(transform.position, ally.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                chosenAlly = ally;
            }
        }

        if (chosenAlly == null)
        {
            EnterSearch(target);
            return;
        }

        bool callerHolds = Random.value > 0.5f;

        if (callerHolds)
        {
            responseRole = GuardResponseRole.Hold;
            chosenAlly.ReceiveGroupOrder(
                target,
                Random.value > 0.5f ? GuardResponseRole.SearchLeft : GuardResponseRole.SearchRight
            );
        }
        else
        {
            responseRole = Random.value > 0.5f ? GuardResponseRole.SearchLeft : GuardResponseRole.SearchRight;
            chosenAlly.ReceiveGroupOrder(target, GuardResponseRole.Hold);
        }

        EnterSearch(target);
    }

    public void ReceiveGroupOrder(Vector3 target, GuardResponseRole role)
    {
        if (isStealthStrikeVictim)
            return;

        hasSharedThisAlert = true;
        responseRole = role;
        currentSearchTarget = target;
        searchTimer = searchDuration;

        if (searchBehavior != null)
            searchBehavior.ResetSearch();

        if (role == GuardResponseRole.Hold)
            holdTimer = Random.Range(holdMinTime, holdMaxTime);
        else
            holdTimer = 0f;

        Log($"Received group order -> {role} at {target}");

        stateMachine.SetState(EnemyStateMachine.EnemyState.Search);
    }

    //----------------------------------
    // STEALTH STRIKE LOCK
    //----------------------------------

    public void EnterStealthStrikeVictimState(Transform attacker)
    {
        isStealthStrikeVictim = true;
        stealthStrikeAttacker = attacker;

        if (movement != null)
            movement.Stop();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (hearing != null)
            hearing.StopInvestigating();

        if (searchBehavior != null)
            searchBehavior.ResetSearch();

        Log("Entered stealth strike victim state");
    }

    public void ExitStealthStrikeVictimState()
    {
        isStealthStrikeVictim = false;
        stealthStrikeAttacker = null;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Log("Exited stealth strike victim state");
    }

    //----------------------------------
    // ENTER SEARCH
    //----------------------------------

    void EnterSearch(Vector3 target)
    {
        currentSearchTarget = target;
        searchTimer = searchDuration;

        if (searchBehavior != null)
            searchBehavior.ResetSearch();

        Log($"ENTER SEARCH -> target: {target}, duration: {searchDuration:F2}");

        stateMachine.SetState(EnemyStateMachine.EnemyState.Search);
    }

    public bool CanBeStealthKilledFrom(Vector2 attackerPosition)
    {
        if (movement == null)
            movement = GetComponent<EnemyMovement>();

        float enemyFacing = movement.MovingRight ? 1f : -1f;
        float attackerOffsetX = attackerPosition.x - transform.position.x;

        if (Mathf.Abs(attackerOffsetX) < 0.05f)
        {
            LogAction("Stealth kill check failed: attacker too centered");
            return false;
        }

        bool attackerIsInFront = Mathf.Sign(attackerOffsetX) == enemyFacing;
        bool canKill = !attackerIsInFront;

        LogAction($"Stealth kill check -> attackerOffsetX={attackerOffsetX:F2}, enemyFacing={enemyFacing}, canKill={canKill}");

        return canKill;
    }

    public void DieFromStealthStrike()
    {
        isStealthStrikeVictim = false;
        stealthStrikeAttacker = null;

        Log("Died from stealth strike");
        Destroy(gameObject);
    }

    //----------------------------------
    // DEBUG HELPERS
    //----------------------------------

    void Log(string msg)
    {
        if (!debugLogs) return;
        Debug.Log($"<color=cyan>[EnemyAI]</color> {name}: {msg}", this);
    }

    void LogAction(string msg)
    {
        if (!debugActions) return;

        if (msg == lastAction) return;

        lastAction = msg;

        Debug.Log($"<color=yellow>[EnemyAI Action]</color> {name}: {msg}", this);
    }

    void LogPerceptionChanges(bool canSeePlayer, bool hasBlood, bool hearingSearch, Vector3 hearingTarget)
    {
        if (!debugPerception) return;

        if (canSeePlayer != lastCanSeePlayer)
        {
            Debug.Log($"<color=lime>[EnemyAI Perception]</color> {name}: CanSeePlayer -> {canSeePlayer}", this);
            lastCanSeePlayer = canSeePlayer;
        }

        if (hasBlood != lastHasBlood)
        {
            Debug.Log($"<color=red>[EnemyAI Perception]</color> {name}: HasBlood -> {hasBlood}", this);
            lastHasBlood = hasBlood;
        }

        if (hearingSearch != lastHearingSearch)
        {
            Debug.Log($"<color=orange>[EnemyAI Perception]</color> {name}: HearingSearch -> {hearingSearch} at {hearingTarget}", this);
            lastHearingSearch = hearingSearch;
        }
    }

    //----------------------------------
    // GIZMOS
    //----------------------------------

    void OnDrawGizmos()
    {
        if (stateMachine == null) return;

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.5f,
            "STATE: " + stateMachine.currentState.ToString()
        );
#endif

        if (attack != null && attack.attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attack.attackPoint.position, attack.attackRange);
        }

        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, player.position);
        }

        if (stateMachine.currentState == EnemyStateMachine.EnemyState.Search)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(currentSearchTarget, 0.35f);
            Gizmos.DrawLine(transform.position, currentSearchTarget);
        }

        if (bloodTracker != null && bloodTracker.HasBloodTarget())
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(bloodTracker.GetBloodTargetPosition(), 0.25f);
        }

        Gizmos.color = new Color(0f, 0.7f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, allyAlertRadius);
    }
}
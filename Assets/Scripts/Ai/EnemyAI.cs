using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    EnemyMovement movement;
    EnemyVision vision;
    EnemyStateMachine stateMachine;
    EnemyAttack attack;
    EnemyBloodTracker bloodTracker;
    EnemyHearing hearing;
    EnemySearchBehavior searchBehavior;

    Transform player;

    Vector3 currentSearchTarget;

    EnemyStateMachine.EnemyState lastState;

    float searchTimer = 0f;
    float searchDuration = 5f;

    void Start()
    {
        movement = GetComponent<EnemyMovement>();
        vision = GetComponent<EnemyVision>();
        stateMachine = GetComponent<EnemyStateMachine>();
        attack = GetComponent<EnemyAttack>();
        bloodTracker = GetComponent<EnemyBloodTracker>();
        hearing = GetComponent<EnemyHearing>();
        searchBehavior = GetComponent<EnemySearchBehavior>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        //----------------------------------
        // PERCEPTION
        //----------------------------------

        vision.Detect();

        if (bloodTracker != null)
            bloodTracker.DetectNearbyBlood();

        bool canSeePlayer = vision.CanSeePlayerNow;
        bool hasBlood = bloodTracker != null && bloodTracker.HasBloodTarget();
        bool hearingSearch = hearing != null && hearing.IsInvestigating();

        Vector3 hearingTarget = hearingSearch ? hearing.lastHeardPosition : Vector3.zero;

        //----------------------------------
        // STATE DECISION (PRIORITY)
        //----------------------------------

        if (attack != null && attack.CanAttack() && canSeePlayer)
        {
            stateMachine.SetState(EnemyStateMachine.EnemyState.Attack);
        }
        else if (canSeePlayer)
        {
            stateMachine.SetState(EnemyStateMachine.EnemyState.Alerted);
        }
        else if (hasBlood)
        {
            stateMachine.SetState(EnemyStateMachine.EnemyState.FollowBlood);
        }
        else if (hearingSearch && stateMachine.currentState != EnemyStateMachine.EnemyState.Search)
        {
            EnterSearch(hearingTarget);
            hearing.StopInvestigating();
        }
        else if (stateMachine.currentState == EnemyStateMachine.EnemyState.Search)
        {
            // let search logic run
        }
        else if (stateMachine.currentState == EnemyStateMachine.EnemyState.FollowBlood && !hasBlood)
        {
            EnterSearch(transform.position);
        }
        else if (stateMachine.currentState != EnemyStateMachine.EnemyState.Patrol)
        {
            stateMachine.SetState(EnemyStateMachine.EnemyState.Patrol);
        }

        //----------------------------------
        // DEBUG STATE CHANGE
        //----------------------------------

        if (stateMachine.currentState != lastState)
        {
            //Debug.Log("CURRENT AI STATE → " + stateMachine.currentState);
            lastState = stateMachine.currentState;
        }

        //----------------------------------
        // STATE EXECUTION
        //----------------------------------

        switch (stateMachine.currentState)
        {
            case EnemyStateMachine.EnemyState.Patrol:

                movement.Patrol();
                break;


            case EnemyStateMachine.EnemyState.Alerted:

                movement.Chase(player.position);
                break;


            case EnemyStateMachine.EnemyState.FollowBlood:

                if (canSeePlayer)
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Alerted);
                    break;
                }

                if (hasBlood)
                {
                    Vector3 bloodTarget = bloodTracker.GetBloodTargetPosition();
                    movement.Chase(bloodTarget);

                    float horizontalDist = Mathf.Abs(transform.position.x - bloodTarget.x);
                    float verticalDist = Mathf.Abs(transform.position.y - bloodTarget.y);

                    // Ground enemy: if we're under the blood point, count it as reached
                    if (horizontalDist < 0.4f)
                    {
                        bloodTracker.MoveToNextBloodTarget();
                    }

                    // Optional: skip blood that is too high to realistically reach
                    else if (verticalDist > 2.5f && horizontalDist < 0.75f)
                    {
                        bloodTracker.MoveToNextBloodTarget();
                    }
                }
                else
                {
                    EnterSearch(transform.position);
                }

                break;


            case EnemyStateMachine.EnemyState.Search:

                // Player spotted again
                if (canSeePlayer)
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Alerted);
                    break;
                }

                // Blood found again
                if (hasBlood)
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.FollowBlood);
                    break;
                }

                searchTimer -= Time.deltaTime;

                searchBehavior.SearchRandomly(currentSearchTarget);

                if (searchTimer <= 0f)
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Return);
                }

                break;


            case EnemyStateMachine.EnemyState.Attack:

                movement.Stop();

                if (!attack.IsAttacking)
                {
                    attack.TryAttack();
                }

                break;


            case EnemyStateMachine.EnemyState.Return:

                movement.Patrol();

                stateMachine.SetState(EnemyStateMachine.EnemyState.Patrol);

                break;
        }
    }

    //----------------------------------
    // ENTER SEARCH
    //----------------------------------

    void EnterSearch(Vector3 target)
    {
        if (stateMachine.currentState == EnemyStateMachine.EnemyState.Search)
            return;

        currentSearchTarget = target;
        searchTimer = searchDuration;

        searchBehavior.ResetSearch();

        stateMachine.SetState(EnemyStateMachine.EnemyState.Search);
    }

    public bool CanBeStealthKilledFrom(Vector2 attackerPosition)
    {
        if (movement == null)
            movement = GetComponent<EnemyMovement>();

        float enemyFacing = movement.MovingRight ? 1f : -1f;
        float attackerOffsetX = attackerPosition.x - transform.position.x;

        // If attacker is too centered, reject
        if (Mathf.Abs(attackerOffsetX) < 0.05f)
            return false;

        bool attackerIsInFront = Mathf.Sign(attackerOffsetX) == enemyFacing;

        // Can only kill if attacker is NOT in front
        return !attackerIsInFront;
    }

    public void DieFromStealthStrike()
    {
        Destroy(gameObject);
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

        if (attack != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attack.attackPoint.position, attack.attackRange);
        }

        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}
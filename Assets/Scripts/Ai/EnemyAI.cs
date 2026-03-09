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

    float searchReachDistance = 0.2f;
    float searchTimer = 10f;

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
        // STATE LOGIC
        //----------------------------------

        switch (stateMachine.currentState)
        {

            //----------------------------------
            // PATROL
            //----------------------------------

            case EnemyStateMachine.EnemyState.Patrol:

                if (canSeePlayer)
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Alerted);
                }
                else if (hasBlood)
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.FollowBlood);
                }
                else if (hearingSearch)
                {
                    EnterSearch(hearingTarget);
                }
                else
                {
                    movement.Patrol();
                }

                break;


            //----------------------------------
            // ALERTED (CHASE PLAYER)
            //----------------------------------

            case EnemyStateMachine.EnemyState.Alerted:

                if (canSeePlayer)
                {
                    if (attack != null && attack.CanAttack())
                    {
                        stateMachine.SetState(EnemyStateMachine.EnemyState.Attack);
                    }
                    else
                    {
                        movement.Chase(player.position);
                    }
                }
                else if (hasBlood)
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.FollowBlood);
                }
                else if (hearingSearch)
                {
                    EnterSearch(hearingTarget);
                }
                else
                {
                    EnterSearch(vision.LastSeenPosition);
                }

                break;


            //----------------------------------
            // FOLLOW BLOOD
            //----------------------------------

            case EnemyStateMachine.EnemyState.FollowBlood:
                {
                    if (canSeePlayer)
                    {
                        stateMachine.SetState(EnemyStateMachine.EnemyState.Alerted);
                    }
                    else if (hearingSearch)
                    {
                        EnterSearch(hearingTarget);
                    }
                    else if (hasBlood)
                    {
                        Vector3 bloodTarget = bloodTracker.GetBloodTargetPosition();

                        movement.Chase(bloodTarget);

                        float dist = Vector2.Distance(transform.position, bloodTarget);

                        if (dist <= 0.5f)
                            bloodTracker.MoveToNextBloodTarget();
                    }
                    else
                    {
                        EnterSearch(vision.LastSeenPosition);
                    }

                    break;
                }


            //----------------------------------
            // SEARCH
            //----------------------------------

            case EnemyStateMachine.EnemyState.Search:
                {
                    searchTimer -= Time.deltaTime;

                    if (searchTimer <= 0f)
                    {
                        stateMachine.SetState(EnemyStateMachine.EnemyState.Return);
                        break;
                    }

                    float dist = Vector2.Distance(transform.position, currentSearchTarget);

                    if (dist > searchReachDistance)
                    {
                        movement.Chase(currentSearchTarget);
                    }
                    else
                    {
                        searchBehavior.SearchRandomly(currentSearchTarget);
                    }

                    break;
                }


            //----------------------------------
            // ATTACK
            //----------------------------------

            case EnemyStateMachine.EnemyState.Attack:

                movement.Stop();

                if (attack != null && !attack.IsAttacking)
                {
                    attack.TryAttack();
                }

                if (!canSeePlayer)
                {
                    EnterSearch(vision.LastSeenPosition);
                }
                else if (!attack.CanAttack())
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Alerted);
                }

                break;


            //----------------------------------
            // RETURN
            //----------------------------------

            case EnemyStateMachine.EnemyState.Return:

                if (canSeePlayer)
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Alerted);
                }
                else if (hasBlood)
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.FollowBlood);
                }
                else
                {
                    movement.Patrol();
                }

                break;
        }
    }


    //----------------------------------
    // ENTER SEARCH STATE
    //----------------------------------

    void EnterSearch(Vector3 target)
    {
        currentSearchTarget = target;
        searchTimer = 10f;

        stateMachine.SetState(EnemyStateMachine.EnemyState.Search);
    }
}
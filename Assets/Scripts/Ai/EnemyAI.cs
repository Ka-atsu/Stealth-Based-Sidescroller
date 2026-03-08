using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    // References to other necessary scripts
    private EnemyMovement movement;
    private EnemyVision vision;
    private EnemyStateMachine stateMachine;
    private EnemyAttack attack;
    private EnemyBloodTracker bloodTracker;
    private EnemyHearing hearing;
    private EnemySearchBehavior searchBehavior;

    private Transform player;

    // Search-related variables
    private Vector3 currentSearchTarget;
    private float searchReachDistance = 0.2f;
    private float searchTimer = 10f;
    private EnemyStateMachine.EnemyState previousState;

    void Start()
    {
        // Get references to all necessary components
        movement = GetComponent<EnemyMovement>();
        vision = GetComponent<EnemyVision>();
        stateMachine = GetComponent<EnemyStateMachine>();
        attack = GetComponent<EnemyAttack>();
        bloodTracker = GetComponent<EnemyBloodTracker>();
        hearing = GetComponent<EnemyHearing>();
        searchBehavior = GetComponent<EnemySearchBehavior>();

        // Find the player object in the scene
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // Initialize the previous state
        previousState = stateMachine.currentState;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // Decrease the search timer during search
        if (stateMachine.currentState == EnemyStateMachine.EnemyState.Search)
        {
            searchTimer -= Time.deltaTime;
        }

        // Perform the vision detection
        vision.Detect();

        // Flags to determine the enemy's current state
        bool canSeePlayer = vision.CanSeePlayerNow;
        bool hasBlood = bloodTracker != null && bloodTracker.HasBloodTarget();
        Vector3 searchTarget = vision.LastSeenPosition;
        bool hearingSearch = false;

        // If the enemy has heard a noise, use that as the new search target
        if (hearing != null && hearing.IsInvestigating())
        {
            searchTarget = hearing.lastHeardPosition;
            hearingSearch = true;
        }

        // Check for state changes and log them
        EnemyStateMachine.EnemyState currentState = stateMachine.currentState;
        if (currentState != previousState)
        {
            Debug.Log("State changed from " + previousState.ToString() + " to " + currentState.ToString());
            previousState = currentState;  // Update previous state
        }

        // State transitions and actions
        switch (currentState)
        {
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
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Search);  // Transition to Search if hearing noise
                }
                else
                {
                    movement.Patrol();  // Continue patrolling
                }
                break;

            case EnemyStateMachine.EnemyState.Alerted:
                if (canSeePlayer)
                {
                    if (attack != null && attack.CanAttack())
                    {
                        stateMachine.SetState(EnemyStateMachine.EnemyState.Attack);  // Transition to Attack state
                    }
                    else
                    {
                        movement.Chase(player.position);  // Chase the player
                    }
                }
                else if (hasBlood)
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.FollowBlood);  // Follow blood if detected
                }
                else if (hearingSearch)
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Search);  // Transition to Search if hearing noise
                }
                else
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Search);  // Fallback to Search
                }
                break;

            case EnemyStateMachine.EnemyState.FollowBlood:
                if (hasBlood)
                {
                    Vector3 bloodTargetPosition = bloodTracker.GetBloodTargetPosition();
                    if (bloodTargetPosition != Vector3.zero)
                    {
                        movement.Chase(bloodTargetPosition);  // Chase blood target

                        float distToBlood = Vector2.Distance(transform.position, bloodTargetPosition);
                        if (distToBlood <= 0.5f)
                        {
                            bloodTracker.MoveToNextBloodTarget();  // Move to next blood target
                        }
                    }
                }
                else
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Search);  // Fallback to Search if no blood
                }
                break;

            case EnemyStateMachine.EnemyState.Search:
                if (searchTimer <= 0f)  // Check if the search time has expired
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Return);  // Return to patrol if time is up
                }
                else
                {
                    // Move towards the last known position or search target
                    float dist = Vector2.Distance(transform.position, searchTarget);

                    if (dist > searchReachDistance)
                    {
                        // If we're not close enough to the search target, keep chasing it
                        movement.Chase(searchTarget);  // Move towards the target
                    }
                    else
                    {
                        // Once close enough, start random search behavior
                        searchBehavior.SearchRandomly(searchTarget);  // Search randomly in the area
                    }
                }
                break;

            case EnemyStateMachine.EnemyState.Return:
                if (canSeePlayer)
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Alerted);  // Return to Alerted if player is seen
                }
                else if (hasBlood)
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.FollowBlood);  // Return to FollowBlood if blood is found
                }
                else
                {
                    movement.Patrol();  // Return to patrolling behavior if no action is needed
                }
                break;
        }
    }
}
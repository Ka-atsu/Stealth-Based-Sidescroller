using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    EnemyMovement movement;
    EnemyVision vision;
    EnemyStateMachine stateMachine;

    Transform player;

    void Start()
    {
        movement = GetComponent<EnemyMovement>();
        vision = GetComponent<EnemyVision>();
        stateMachine = GetComponent<EnemyStateMachine>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    void FixedUpdate()
    {
        vision.Detect();

        switch (stateMachine.currentState)
        {
            case EnemyStateMachine.EnemyState.Patrol:
                movement.Patrol();
                break;

            case EnemyStateMachine.EnemyState.Alerted:
                movement.Chase(player.position);
                break;
        }
    }
}
using UnityEngine;

public class EnemyStateMachine : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Alerted,
        Search,
        Return
    }

    public EnemyState currentState = EnemyState.Patrol;

    public void SetState(EnemyState newState)
    {
        currentState = newState;
    }
}
using UnityEngine;

public class EnemyHearing : MonoBehaviour
{
    public float hearingRange = 6f;

    EnemyStateMachine stateMachine;
    EnemyMovement movement;

    Vector2 lastHeardPosition;
    bool investigating;

    float searchTimer = 2f;
    float searchCounter = 0f;
    bool reachedNoise;
    float hearingCooldown = 1.5f;
    float hearingCooldownTimer;

    void Start()
    {
        stateMachine = GetComponent<EnemyStateMachine>();
        movement = GetComponent<EnemyMovement>();
    }

    public void HearNoise(Vector2 noisePosition, NoiseType type)
    {
        if (hearingCooldownTimer > 0f) return;

        float noiseStrength = GetNoiseStrength(type);

        if (Vector2.Distance(transform.position, noisePosition) > hearingRange * noiseStrength)
            return;

        Debug.Log($"{gameObject.name} heard {type} noise");

        lastHeardPosition = new Vector2(noisePosition.x, transform.position.y);

        investigating = true;
        reachedNoise = false;
        searchCounter = 0f;

        stateMachine.SetState(EnemyStateMachine.EnemyState.Search);
    }

    float GetNoiseStrength(NoiseType type)
    {
        switch (type)
        {
            case NoiseType.Walk: return 0.4f;
            case NoiseType.Run: return 0.7f;
            case NoiseType.JumpLanding: return 0.8f;
            case NoiseType.Roll: return 0.9f;
            case NoiseType.ThrowObject: return 1.2f;
            default: return 0.5f;
        }
    }

    void FixedUpdate()
    {
        hearingCooldownTimer -= Time.fixedDeltaTime;
        if (!investigating) return;

        float dist = Vector2.Distance(transform.position, lastHeardPosition);

        if (!reachedNoise)
        {
            movement.Chase(lastHeardPosition);

            float horizontalDist = Mathf.Abs(transform.position.x - lastHeardPosition.x);

            if (horizontalDist < 0.8f)
            {
                Debug.Log("Reached noise position");
                reachedNoise = true;
            }
        }
        else
        {
            movement.Patrol();

            searchCounter += Time.fixedDeltaTime;
            //Debug.Log("Search Counter: " + searchCounter);

            if (searchCounter >= searchTimer)
            {
                investigating = false;
                reachedNoise = false;
                searchCounter = 0f;

                hearingCooldownTimer = hearingCooldown;

                Debug.Log("Enemy finished searching and is returning to patrol.");

                stateMachine.SetState(EnemyStateMachine.EnemyState.Return);
            }
        }
    }
}
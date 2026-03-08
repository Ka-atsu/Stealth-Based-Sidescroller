using UnityEngine;

public class EnemyHearing : MonoBehaviour
{
    public float hearingRange = 6f;

    public Vector2 lastHeardPosition;
    bool investigating;
    float hearingCooldownTimer;

    public bool IsInvestigating() => investigating;

    float hearingCooldown = 1.5f;

    private EnemyStateMachine stateMachine;
    private EnemyMovement movement;

    void Start()
    {
        hearingCooldownTimer = 0f;
        stateMachine = GetComponent<EnemyStateMachine>();
        movement = GetComponent<EnemyMovement>();
    }

    public void HearNoise(Vector2 noisePosition, NoiseType type)
    {
        if (hearingCooldownTimer > 0f) return;

        // Adjust range based on noise type if needed
        float noiseStrength = GetNoiseStrength(type);

        if (Vector2.Distance(transform.position, noisePosition) > hearingRange * noiseStrength)
            return;

        lastHeardPosition = new Vector2(noisePosition.x, transform.position.y);
        investigating = true;
        hearingCooldownTimer = hearingCooldown;

        // Optionally, you could change the state of the enemy here if needed.
        // stateMachine.SetState(EnemyStateMachine.EnemyState.Search);
    }

    void Update()
    {
        hearingCooldownTimer -= Time.deltaTime;

        if (investigating)
        {
            // Move towards the last heard position
            movement.Chase(lastHeardPosition);

            // Check if the enemy has reached the investigation target
            float distToTarget = Vector2.Distance(transform.position, lastHeardPosition);
            if (distToTarget < 1f)  // You can adjust this threshold based on how close you want the enemy to be
            {
                StopInvestigating();
                // Optionally, return to patrol or search for the player
                // stateMachine.SetState(EnemyStateMachine.EnemyState.Patrol); // or any other state transition
            }
        }
    }

    public void StopInvestigating()
    {
        investigating = false;
        // You can optionally reset the last heard position if you don't want the enemy to return there.
        lastHeardPosition = Vector2.zero;
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
}
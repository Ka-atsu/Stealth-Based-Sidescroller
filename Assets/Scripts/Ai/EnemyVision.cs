using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    public float detectionRange = 5f;
    public float visionAngle = 35f;

    public float detectionBuildSpeed = 0.5f;
    public float detectionDecaySpeed = 0.6f;

    // Metal Gear style suspicion threshold
    public float suspicionThreshold = 0.35f;

    public DetectionMeterUI detectionUI;

    Transform player;
    PlayerNoiseEmitter2D playerNoise;

    float detectionMeter;

    EnemyStateMachine stateMachine;
    EnemyMovement movement;

    public bool CanSeePlayerNow { get; private set; }
    public Vector3 LastSeenPosition { get; private set; }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
        {
            player = p.transform;
            playerNoise = p.GetComponent<PlayerNoiseEmitter2D>();
            LastSeenPosition = player.position;
        }

        stateMachine = GetComponent<EnemyStateMachine>();
        movement = GetComponent<EnemyMovement>();

        if (movement == null)
        {
            Debug.LogError("EnemyMovement is not assigned or missing at runtime!");
        }
    }

    public void Detect()
    {
        if (player == null)
        {
            CanSeePlayerNow = false;
            return;
        }

        bool seesPlayer = CanSeePlayer();

        if (seesPlayer)
        {
            LastSeenPosition = player.position;
            IncreaseDetection();
        }
        else
        {
            DecreaseDetection();
        }

        // FULL detection
        if (detectionMeter >= 1f)
        {
            CanSeePlayerNow = true;
        }
        else
        {
            CanSeePlayerNow = false;

            // Suspicion stage (Metal Gear style)
            if (detectionMeter >= suspicionThreshold)
            {
                if (stateMachine.currentState == EnemyStateMachine.EnemyState.Patrol)
                {
                    stateMachine.SetState(EnemyStateMachine.EnemyState.Search);
                }
            }
        }
    }

    bool CanSeePlayer()
    {
        if (playerNoise != null && playerNoise.isHidden)
        {
            return false;
        }

        Vector2 direction = (Vector2)(player.position - transform.position);

        if (direction.magnitude > detectionRange)
        {
            return false;
        }

        Vector2 forward = movement.MovingRight ? Vector2.right : Vector2.left;

        float angle = Vector2.Angle(forward, direction.normalized);

        if (angle > visionAngle)
        {
            return false;
        }

        return true;
    }

    void IncreaseDetection()
    {
        Vector2 direction = (Vector2)(player.position - transform.position);
        Vector2 forward = movement.MovingRight ? Vector2.right : Vector2.left;

        float angle = Vector2.Angle(forward, direction.normalized);

        // Normalize how centered the player is (0 = center, 1 = edge)
        float edgeFactor = angle / visionAngle;

        // Detection is faster near center
        float speedMultiplier = Mathf.Lerp(1f, 0.3f, edgeFactor);

        detectionMeter += detectionBuildSpeed * speedMultiplier * Time.deltaTime;
        detectionMeter = Mathf.Clamp01(detectionMeter);

        if (detectionUI != null)
            detectionUI.SetValue(detectionMeter);

        if (detectionMeter >= 1f)
        {
            stateMachine.SetState(EnemyStateMachine.EnemyState.Alerted);
        }
    }

    void DecreaseDetection()
    {
        detectionMeter -= detectionDecaySpeed * Time.deltaTime;
        detectionMeter = Mathf.Clamp01(detectionMeter);

        if (detectionUI != null)
            detectionUI.SetValue(detectionMeter);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        if (movement == null)
        {
            return;
        }

        Vector3 forward = movement.MovingRight ? transform.right : -transform.right;

        Vector3 left = Quaternion.Euler(0, 0, -visionAngle / 2) * forward * detectionRange;
        Vector3 right = Quaternion.Euler(0, 0, visionAngle / 2) * forward * detectionRange;

        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
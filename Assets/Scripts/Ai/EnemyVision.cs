using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    public float detectionRange = 5f;
    public float visionAngle = 30f;

    public float detectionBuildSpeed = 0.6f;
    public float detectionDecaySpeed = 0.4f;

    public DetectionMeterUI detectionUI;

    Transform player;
    PlayerMovement playerMovement;

    float detectionMeter;

    EnemyStateMachine stateMachine;
    EnemyMovement movement;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
        {
            player = p.transform;
            playerMovement = p.GetComponent<PlayerMovement>();
        }

        stateMachine = GetComponent<EnemyStateMachine>();
        movement = GetComponent<EnemyMovement>();
    }

    public void Detect()
    {
        if (player == null) return;

        if (CanSeePlayer())
        {
            IncreaseDetection();
        }
        else
        {
            DecreaseDetection();
        }
    }

    bool CanSeePlayer()
    {
        if (playerMovement != null && playerMovement.isHidden)
            return false;

        Vector2 direction = player.position - transform.position;

        if (direction.magnitude > detectionRange)
            return false;

        Vector2 forward = movement.MovingRight ? Vector2.right : Vector2.left;

        float angle = Vector2.Angle(forward, direction);

        if (angle > visionAngle)
            return false;

        return true;
    }

    void IncreaseDetection()
    {
        detectionMeter += detectionBuildSpeed * Time.deltaTime;
        detectionMeter = Mathf.Clamp01(detectionMeter);

        detectionUI.SetValue(detectionMeter);

        if (detectionMeter >= 1f)
            stateMachine.SetState(EnemyStateMachine.EnemyState.Alerted);
    }

    void DecreaseDetection()
    {
        detectionMeter -= detectionDecaySpeed * Time.deltaTime;
        detectionMeter = Mathf.Clamp01(detectionMeter);

        detectionUI.SetValue(detectionMeter);
    }
}
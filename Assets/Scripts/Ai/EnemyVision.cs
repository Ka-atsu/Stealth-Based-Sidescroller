using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    public float detectionRange = 10f;

    // This is treated as a HALF-ANGLE
    public float visionAngle = 20f;

    public LayerMask visionMask;

    public float detectionBuildSpeed = 0.5f;
    public float detectionDecaySpeed = 0.6f;

    public float suspicionThreshold = 0.35f;

    // If player is within this distance, detection builds at full speed
    // even if they're near the edge of the cone.
    public float closeVisionFullSpeedRange = 1.5f;

    public DetectionMeterUI detectionUI;

    Transform player;
    PlayerNoiseEmitter2D playerNoise;

    float detectionMeter;

    EnemyMovement movement;

    public bool CanSeePlayerNow { get; private set; }
    public Vector3 LastSeenPosition { get; private set; }

    public float DetectionMeter => detectionMeter;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
        {
            player = p.transform;
            playerNoise = p.GetComponent<PlayerNoiseEmitter2D>();
            LastSeenPosition = player.position;
        }

        movement = GetComponent<EnemyMovement>();
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

        // Fully detected only when the meter reaches 1
        CanSeePlayerNow = detectionMeter >= 1f;
    }

    bool CanSeePlayer()
    {
        if (playerNoise != null && playerNoise.isHidden)
            return false;

        Vector2 direction = (Vector2)(player.position - transform.position);

        if (direction.magnitude > detectionRange)
            return false;

        Vector2 forward = movement != null && movement.MovingRight ? Vector2.right : Vector2.left;

        float angle = Vector2.Angle(forward, direction.normalized);

        // visionAngle is used as HALF of the cone
        if (angle > visionAngle)
            return false;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction.normalized,
            direction.magnitude,
            visionMask
        );

        if (hit.collider != null && hit.collider.CompareTag("Player"))
            return true;

        return false;
    }

    void IncreaseDetection()
    {
        if (player == null)
            return;

        Vector2 direction = (Vector2)(player.position - transform.position);
        Vector2 forward = movement != null && movement.MovingRight ? Vector2.right : Vector2.left;

        float distance = direction.magnitude;
        float angle = Vector2.Angle(forward, direction.normalized);

        float speedMultiplier = 1f;

        // Only apply edge slowdown if player is outside the close-range zone
        if (distance > closeVisionFullSpeedRange)
        {
            // 0 = center of cone, 1 = edge of cone
            float edgeFactor = Mathf.Clamp01(angle / visionAngle);

            // At center = full speed, at edge = 30% speed
            speedMultiplier = Mathf.Lerp(1f, 0.3f, edgeFactor);
        }

        detectionMeter += detectionBuildSpeed * speedMultiplier * Time.deltaTime;
        detectionMeter = Mathf.Clamp01(detectionMeter);

        if (detectionUI != null)
            detectionUI.SetValue(detectionMeter);
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

        Vector3 forward;

        if (Application.isPlaying && movement != null)
            forward = movement.MovingRight ? transform.right : -transform.right;
        else
            forward = transform.right;

        // Match actual detection logic: visionAngle is HALF-ANGLE
        Vector3 left = Quaternion.Euler(0, 0, -visionAngle) * forward * detectionRange;
        Vector3 right = Quaternion.Euler(0, 0, visionAngle) * forward * detectionRange;

        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Optional: show close full-speed zone
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, closeVisionFullSpeedRange);
    }
}
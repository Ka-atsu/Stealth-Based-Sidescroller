using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    public float detectionRange = 5f;
    public float visionAngle = 35f;

    public LayerMask visionMask;

    public float detectionBuildSpeed = 0.5f;
    public float detectionDecaySpeed = 0.6f;

    public float suspicionThreshold = 0.35f;

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

        // Only report visibility
        CanSeePlayerNow = detectionMeter >= 1f;
    }

    bool CanSeePlayer()
    {
        if (playerNoise != null && playerNoise.isHidden)
            return false;

        Vector2 direction = (Vector2)(player.position - transform.position);

        if (direction.magnitude > detectionRange)
            return false;

        Vector2 forward = movement.MovingRight ? Vector2.right : Vector2.left;

        float angle = Vector2.Angle(forward, direction.normalized);

        if (angle > visionAngle)
            return false;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction.normalized,
            detectionRange,
            visionMask
        );

        if (hit.collider != null && hit.collider.CompareTag("Player"))
            return true;

        return false;
    }

    void IncreaseDetection()
    {
        Vector2 direction = (Vector2)(player.position - transform.position);
        Vector2 forward = movement.MovingRight ? Vector2.right : Vector2.left;

        float angle = Vector2.Angle(forward, direction.normalized);

        float edgeFactor = angle / visionAngle;

        float speedMultiplier = Mathf.Lerp(1f, 0.3f, edgeFactor);

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

        if (movement == null)
            return;

        Vector3 forward = movement.MovingRight ? transform.right : -transform.right;

        Vector3 left = Quaternion.Euler(0, 0, -visionAngle / 2) * forward * detectionRange;
        Vector3 right = Quaternion.Euler(0, 0, visionAngle / 2) * forward * detectionRange;

        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
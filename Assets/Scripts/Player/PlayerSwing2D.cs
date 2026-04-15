using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(DistanceJoint2D))]
[RequireComponent(typeof(LineRenderer))]
public class PlayerSwing2D : MonoBehaviour
{
    private enum SwingState
    {
        None,
        Pulling,
        Swinging
    }

    [Header("Grapple")]
    [SerializeField] private float grappleRange = 8f;
    [SerializeField] private LayerMask grappleLayer;

    [Header("Pull")]
    [SerializeField] private float pullSpeed = 18f;
    [SerializeField] private bool pullWhenGroundedOnly = true;
    [SerializeField] private float attachRopeLength = 2.5f;
    [SerializeField] private float attachTolerance = 0.2f;

    [Header("Swing")]
    [SerializeField] private float swingForce = 22f;

    [Header("Swing Limits")]
    [SerializeField] private float maxSwingSpeed = 11f;
    [SerializeField] private bool useSoftSpeedCap = true;
    [SerializeField] private bool capTangentialSpeedOnly = true;
    [SerializeField] private float swingDamping = 0.35f;
    [SerializeField] private float normalDamping = 0f;

    [Header("Swing Control")]
    [SerializeField] private float withMomentumForceMultiplier = 1f;
    [SerializeField] private float againstMomentumForceMultiplier = 0.35f;
    [SerializeField] private float neutralMomentumThreshold = 0.2f;
    [SerializeField] private float topControlPenaltyStartY = 0.6f;
    [SerializeField] private float topControlPenaltyMax = 0.65f;

    [Header("Rope")]
    [SerializeField] private float minRopeLength = 1.5f;
    [SerializeField] private float maxRopeLength = 8f;
    [SerializeField] private float ropeAdjustSpeed = 4f;

    [Header("Rope Visuals")]
    [SerializeField] private Transform ropeStartPoint;
    [SerializeField] private float baseRopeWidth = 0.02f;
    [SerializeField] private float maxRopeWidth = 0.05f;

    [Header("Trail Juice")]
    [SerializeField] private TrailRenderer speedTrail;
    [SerializeField] private float trailShowSpeed = 7f;
    [SerializeField] private float trailMaxTime = 0.12f;
    [SerializeField] private float trailMaxWidth = 0.18f;
    [SerializeField] private float trailOffsetDistance = 0.15f;

    [Header("Camera Shake")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private Vector3 attachImpulseVelocity = new Vector3(2.5f, 1.8f, 0f);
    [SerializeField] private Vector3 releaseImpulseVelocity = new Vector3(3f, 2.2f, 0f);
    [SerializeField] private float highSpeedReleaseThreshold = 8f;
    [SerializeField] private float highSpeedReleaseMultiplier = 1.5f;

    [Header("Arc")]
    [SerializeField] private float topArcDrag = 2.5f;
    [SerializeField] private float topArcStartY = 0.75f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Rigidbody2D rb;
    private DistanceJoint2D joint;
    private LineRenderer line;
    private Camera cam;
    private PlayerController2D controller;

    private Vector2 grapplePoint;
    private Vector2 moveInput;
    private SwingState state = SwingState.None;

    public bool IsSwinging => state == SwingState.Swinging;
    public bool IsPulling => state == SwingState.Pulling;
    public bool IsBusy => state != SwingState.None;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        joint = GetComponent<DistanceJoint2D>();
        line = GetComponent<LineRenderer>();
        cam = Camera.main;
        controller = GetComponent<PlayerController2D>();

        if (impulseSource == null)
            impulseSource = GetComponentInChildren<CinemachineImpulseSource>(true);

        ConfigureJoint();
        ConfigureLine();
        ConfigureTrail();
        ResetSwingPhysics();
    }

    private void Update()
    {
        UpdateRopeVisual();
        UpdateTrailVisual();
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case SwingState.None:
                return;

            case SwingState.Pulling:
                TickPull();
                return;

            case SwingState.Swinging:
                TickSwing();
                return;
        }
    }

    private void ConfigureJoint()
    {
        joint.enabled = false;
        joint.autoConfigureDistance = false;
        joint.autoConfigureConnectedAnchor = false;
    }

    private void ConfigureLine()
    {
        line.positionCount = 2;
        line.enabled = false;
        line.startWidth = baseRopeWidth;
        line.endWidth = baseRopeWidth;
    }

    private void ConfigureTrail()
    {
        if (speedTrail == null)
            return;

        speedTrail.emitting = false;
        speedTrail.time = 0f;
        speedTrail.startWidth = 0f;
        speedTrail.endWidth = 0f;
    }

    private void TickPull()
    {
        Vector2 toPoint = grapplePoint - (Vector2)transform.position;
        float distance = toPoint.magnitude;
        float targetDistance = Mathf.Clamp(attachRopeLength, minRopeLength, maxRopeLength);

        if (distance <= targetDistance + attachTolerance)
        {
            BeginSwing();
            return;
        }

        Vector2 direction = toPoint.normalized;
        rb.linearVelocity = direction * pullSpeed;
    }

    private void TickSwing()
    {
        rb.linearDamping = swingDamping;

        Vector2 ropeDirection = GetRopeDirection();
        Vector2 tangentDirection = GetTangentDirection(ropeDirection);

        ApplySwingForce(ropeDirection, tangentDirection);
        AdjustRopeLength();
        ClampSwingMomentum(ropeDirection, tangentDirection);
        ApplyTopOfArcDrag(ropeDirection, tangentDirection);
    }

    private Vector2 GetRopeDirection()
    {
        return ((Vector2)transform.position - grapplePoint).normalized;
    }

    private Vector2 GetTangentDirection(Vector2 ropeDirection)
    {
        return new Vector2(-ropeDirection.y, ropeDirection.x);
    }

    private void ApplySwingForce(Vector2 ropeDirection, Vector2 tangentDirection)
    {
        float horizontalInput = moveInput.x;
        if (Mathf.Abs(horizontalInput) < 0.01f)
            return;

        float tangentialSpeed = Vector2.Dot(rb.linearVelocity, tangentDirection);
        float absTangentialSpeed = Mathf.Abs(tangentialSpeed);

        float relevantSpeed = capTangentialSpeedOnly
            ? absTangentialSpeed
            : rb.linearVelocity.magnitude;

        float speedCapMultiplier = GetSpeedCapMultiplier(relevantSpeed);
        if (speedCapMultiplier <= 0f)
            return;

        float inputDirection = Mathf.Sign(horizontalInput);
        float controlMultiplier = 1f;

        if (absTangentialSpeed > neutralMomentumThreshold)
        {
            float momentumDirection = Mathf.Sign(tangentialSpeed);

            controlMultiplier = inputDirection == momentumDirection
                ? withMomentumForceMultiplier
                : againstMomentumForceMultiplier;
        }

        Vector2 forceDirection = tangentDirection * inputDirection;
        float finalForce = Mathf.Abs(horizontalInput) * swingForce * controlMultiplier * speedCapMultiplier;

        float topPenaltyT = Mathf.InverseLerp(topControlPenaltyStartY, 1f, ropeDirection.y);
        float topControlMultiplier = Mathf.Lerp(1f, 1f - topControlPenaltyMax, topPenaltyT);
        finalForce *= topControlMultiplier;

        rb.AddForce(forceDirection * finalForce, ForceMode2D.Force);
    }

    private void ClampSwingMomentum(Vector2 ropeDirection, Vector2 tangentDirection)
    {
        if (maxSwingSpeed <= 0f)
            return;

        Vector2 velocity = rb.linearVelocity;

        float tangential = Vector2.Dot(velocity, tangentDirection);
        float radial = Vector2.Dot(velocity, ropeDirection);

        tangential = Mathf.Clamp(tangential, -maxSwingSpeed, maxSwingSpeed);

        rb.linearVelocity = tangentDirection * tangential + ropeDirection * radial;
    }

    private float GetSpeedCapMultiplier(float currentSpeed)
    {
        if (maxSwingSpeed <= 0f)
            return 0f;

        if (useSoftSpeedCap)
            return 1f - Mathf.Clamp01(currentSpeed / maxSwingSpeed);

        return currentSpeed >= maxSwingSpeed ? 0f : 1f;
    }

    private void AdjustRopeLength()
    {
        float newDistance = joint.distance - (moveInput.y * ropeAdjustSpeed * Time.fixedDeltaTime);
        joint.distance = Mathf.Clamp(newDistance, minRopeLength, maxRopeLength);
    }

    private void BeginSwing()
    {
        state = SwingState.Swinging;

        joint.connectedAnchor = grapplePoint;
        joint.distance = Mathf.Clamp(
            Vector2.Distance(transform.position, grapplePoint),
            minRopeLength,
            maxRopeLength
        );
        joint.enabled = true;

        rb.linearDamping = swingDamping;

        float horizontalSign = GetImpulseHorizontalSign();
        DoSwingImpulse(attachImpulseVelocity, horizontalSign);

        if (debugLogs)
            Debug.Log("Pull finished -> Swing started");
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    public void TryGrapple(Vector2 mouseScreenPosition)
    {
        if (state != SwingState.None || cam == null)
            return;

        Vector2 worldMouse = cam.ScreenToWorldPoint(mouseScreenPosition);
        Vector2 direction = (worldMouse - (Vector2)transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction,
            grappleRange,
            grappleLayer
        );

        if (!hit.collider)
            return;

        grapplePoint = hit.point;
        line.enabled = true;

        bool shouldPullFirst = !pullWhenGroundedOnly || (controller != null && controller.IsGrounded);

        if (shouldPullFirst)
        {
            state = SwingState.Pulling;

            if (debugLogs)
                Debug.Log($"Grapple hit -> Pulling to {grapplePoint}");
        }
        else
        {
            BeginSwing();

            if (debugLogs)
                Debug.Log($"Air grapple -> Swinging at {grapplePoint}");
        }
    }

    public void ReleaseSwing()
    {
        if (state == SwingState.None)
            return;

        float releaseSpeed = rb.linearVelocity.magnitude;
        float horizontalSign = GetImpulseHorizontalSign();

        state = SwingState.None;
        joint.enabled = false;
        line.enabled = false;

        ResetSwingPhysics();

        Vector3 finalReleaseImpulse = releaseImpulseVelocity;

        if (releaseSpeed >= highSpeedReleaseThreshold)
            finalReleaseImpulse *= highSpeedReleaseMultiplier;

        DoSwingImpulse(finalReleaseImpulse, horizontalSign);

        if (speedTrail != null)
            speedTrail.emitting = false;

        if (debugLogs)
            Debug.Log("Swing released");
    }

    private void ResetSwingPhysics()
    {
        rb.linearDamping = normalDamping;
    }

    private void UpdateRopeVisual()
    {
        if (state == SwingState.None || !line.enabled)
            return;

        Vector3 startPosition = ropeStartPoint != null ? ropeStartPoint.position : transform.position;
        line.SetPosition(0, startPosition);
        line.SetPosition(1, grapplePoint);

        float speed = GetCurrentSwingSpeed();
        float t = Mathf.InverseLerp(0f, maxSwingSpeed, speed);
        float width = Mathf.Lerp(baseRopeWidth, maxRopeWidth, t);

        line.startWidth = width;
        line.endWidth = width * 0.9f;
    }

    private void UpdateTrailVisual()
    {
        if (speedTrail == null)
            return;

        float speed = GetCurrentSwingSpeed();
        float t = Mathf.InverseLerp(trailShowSpeed, maxSwingSpeed, speed);

        bool shouldEmit = state == SwingState.Swinging && t > 0.05f;
        speedTrail.emitting = shouldEmit;

        speedTrail.time = Mathf.Lerp(0f, trailMaxTime, t);

        float width = Mathf.Lerp(0f, trailMaxWidth, t);
        speedTrail.startWidth = width;
        speedTrail.endWidth = 0f;

        if (rb.linearVelocity.sqrMagnitude > 0.001f)
        {
            Vector3 offset = -(Vector3)rb.linearVelocity.normalized * trailOffsetDistance;
            speedTrail.transform.position = transform.position + offset;
        }
        else
        {
            speedTrail.transform.position = transform.position;
        }
    }

    private float GetCurrentSwingSpeed()
    {
        if (state != SwingState.Swinging)
            return rb.linearVelocity.magnitude;

        Vector2 ropeDirection = GetRopeDirection();
        Vector2 tangentDirection = GetTangentDirection(ropeDirection);

        if (capTangentialSpeedOnly)
            return Mathf.Abs(Vector2.Dot(rb.linearVelocity, tangentDirection));

        return rb.linearVelocity.magnitude;
    }

    private void ApplyTopOfArcDrag(Vector2 ropeDirection, Vector2 tangentDirection)
    {
        float aboveFactor = Mathf.InverseLerp(topArcStartY, 1f, ropeDirection.y);
        if (aboveFactor <= 0f)
            return;

        float tangential = Vector2.Dot(rb.linearVelocity, tangentDirection);
        float dragAmount = topArcDrag * aboveFactor * Time.fixedDeltaTime;

        tangential = Mathf.MoveTowards(tangential, 0f, dragAmount);

        float radial = Vector2.Dot(rb.linearVelocity, ropeDirection);
        rb.linearVelocity = tangentDirection * tangential + ropeDirection * radial;
    }

    private float GetImpulseHorizontalSign()
    {
        if (Mathf.Abs(rb.linearVelocity.x) > 0.01f)
            return Mathf.Sign(rb.linearVelocity.x);

        if (controller != null)
            return controller.FacingSign;

        return 1f;
    }

    private void DoSwingImpulse(Vector3 baseVelocity, float horizontalSign)
    {
        if (impulseSource == null)
        {
            Debug.LogWarning("CinemachineImpulseSource is NULL on PlayerSwing2D", this);
            return;
        }

        Vector3 velocity = new Vector3(
            Mathf.Abs(baseVelocity.x) * Mathf.Sign(horizontalSign == 0f ? 1f : horizontalSign),
            baseVelocity.y,
            baseVelocity.z
        );

        impulseSource.GenerateImpulse(velocity);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grappleRange);

        if (state != SwingState.None)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(grapplePoint, 0.15f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, grapplePoint);
        }
    }
}
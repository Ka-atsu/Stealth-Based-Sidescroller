using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(DistanceJoint2D))]
[RequireComponent(typeof(LineRenderer))]
public class PlayerSwing2D : MonoBehaviour
{
    enum SwingState
    {
        None,
        Pulling,
        Swinging
    }

    [Header("Grapple")]
    public float grappleRange = 8f;
    public LayerMask grappleLayer;

    [Header("Pull")]
    public float pullSpeed = 18f;
    public bool pullWhenGroundedOnly = true;
    public float attachRopeLength = 2.5f;
    public float attachTolerance = 0.2f;

    [Header("Swing")]
    public float swingForce = 20f;

    [Header("Rope")]
    public float minRopeLength = 1.5f;
    public float maxRopeLength = 8f;
    public float ropeAdjustSpeed = 4f;

    [Header("Visuals")]
    [SerializeField] private Transform ropeStartPoint;

    [Header("Debug")]
    public bool debugLogs = true;

    Rigidbody2D rb;
    DistanceJoint2D joint;
    LineRenderer line;
    Camera cam;
    PlayerController2D controller;

    Vector2 grapplePoint;
    Vector2 moveInput;
    SwingState state = SwingState.None;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        joint = GetComponent<DistanceJoint2D>();
        line = GetComponent<LineRenderer>();
        cam = Camera.main;
        controller = GetComponent<PlayerController2D>();

        joint.enabled = false;
        joint.autoConfigureDistance = false;
        joint.autoConfigureConnectedAnchor = false;

        line.positionCount = 2;
        line.enabled = false;
        line.startWidth = 0.02f;
        line.endWidth = 0.02f;
    }

    void Update()
    {
        UpdateRopeVisual();
    }

    void FixedUpdate()
    {
        if (state == SwingState.None)
            return;

        if (state == SwingState.Pulling)
        {
            TickPull();
            return;
        }

        if (state == SwingState.Swinging)
        {
            TickSwing();
        }
    }

    void TickPull()
    {
        Vector2 toPoint = grapplePoint - (Vector2)transform.position;
        float distance = toPoint.magnitude;

        float targetDistance = Mathf.Clamp(attachRopeLength, minRopeLength, maxRopeLength);

        if (distance <= targetDistance + attachTolerance)
        {
            BeginSwing();
            return;
        }

        Vector2 dir = toPoint.normalized;
        rb.linearVelocity = dir * pullSpeed;
    }

    void TickSwing()
    {
        Vector2 ropeDir = ((Vector2)transform.position - grapplePoint).normalized;
        Vector2 tangent = new Vector2(-ropeDir.y, ropeDir.x);

        rb.AddForce(tangent * moveInput.x * swingForce, ForceMode2D.Force);

        float newDistance = joint.distance - (moveInput.y * ropeAdjustSpeed * Time.fixedDeltaTime);
        joint.distance = Mathf.Clamp(newDistance, minRopeLength, maxRopeLength);
    }

    void BeginSwing()
    {
        state = SwingState.Swinging;

        joint.connectedAnchor = grapplePoint;
        joint.distance = Mathf.Clamp(
            Vector2.Distance(transform.position, grapplePoint),
            minRopeLength,
            maxRopeLength
        );
        joint.enabled = true;

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
        Vector2 dir = (worldMouse - (Vector2)transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            dir,
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

        state = SwingState.None;
        joint.enabled = false;
        line.enabled = false;

        if (debugLogs)
            Debug.Log("Swing released");
    }

    void UpdateRopeVisual()
    {
        if (state == SwingState.None || !line.enabled)
            return;

        Vector3 startPos = ropeStartPoint != null ? ropeStartPoint.position : transform.position;
        line.SetPosition(0, startPos);
        line.SetPosition(1, grapplePoint);
    }

    public bool IsSwinging => state == SwingState.Swinging;
    public bool IsPulling => state == SwingState.Pulling;
    public bool IsBusy => state != SwingState.None;

    void OnDrawGizmos()
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
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerGrappleHang2D : MonoBehaviour
{
    [Header("Grapple")]
    public float grappleRange = 8f;
    public float pullSpeed = 18f;
    public LayerMask hangLayer;

    [Header("Hang")]
    [SerializeField] private Transform hangCheck;
    [SerializeField] private float hangSnapDistance = 0.2f;
    [SerializeField] private Vector2 hangOffset = new Vector2(0f, -0.5f);

    [Header("Debug")]
    public bool debugLogs = true;

    float hangLockTimer;

    Rigidbody2D rb;
    PlayerController2D controller;
    Camera cam;

    Vector2 grapplePoint;

    bool grappling;
    bool hanging;

    bool lastGrappling;
    bool lastHanging;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController2D>();
        cam = Camera.main;

        if (rb == null) Debug.LogError("Missing Rigidbody2D", this);
        if (controller == null) Debug.LogError("Missing PlayerController2D", this);
    }

    void FixedUpdate()
    {
        if (hangLockTimer > 0f)
            hangLockTimer -= Time.fixedDeltaTime;

        if (hanging)
        {
            rb.linearVelocity = Vector2.zero;
            WatchState();
            return;
        }

        if (hangLockTimer > 0f)
        {
            WatchState();
            return;
        }

        if (!grappling)
        {
            WatchState();
            return;
        }

        Vector2 dir = (grapplePoint - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * pullSpeed;

        if (hangCheck != null)
        {
            float dist = Vector2.Distance(hangCheck.position, grapplePoint);

            if (dist <= hangSnapDistance)
                StartHang(grapplePoint);
        }

        WatchState();
    }

    public void TryGrapple(Vector2 mousePos)
    {
        if (hanging || hangLockTimer > 0f || cam == null)
            return;

        Vector2 worldMouse = cam.ScreenToWorldPoint(mousePos);
        Vector2 dir = (worldMouse - (Vector2)transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            dir,
            grappleRange,
            hangLayer
        );

        if (hit.collider == null)
            return;

        // Only allow underside / ceiling surfaces
        if (hit.normal.y > -0.5f)
            return;

        grapplePoint = hit.point;
        grappling = true;
        hanging = false;

        WatchState();
    }

    void StartHang(Vector2 point)
    {
        grappling = false;
        hanging = true;

        rb.linearVelocity = Vector2.zero;
        transform.position = point + hangOffset;

        if (controller != null)
            controller.SetHanging(true);

        WatchState();
    }

    public void DropHang()
    {
        if (!hanging)
            return;

        grappling = false;
        hanging = false;

        rb.linearVelocity = new Vector2(0f, -6f);
        transform.position += Vector3.down * 0.35f;

        hangLockTimer = 0.25f;

        if (controller != null)
            controller.SetHanging(false);

        WatchState();
    }

    void WatchState()
    {
        if (!debugLogs)
            return;

        bool controllerHanging = controller != null && controller.IsHanging;

        if (lastGrappling != grappling || lastHanging != hanging)
        {
            Debug.Log($"STATE CHANGE | grappling={grappling} hanging={hanging} controller.IsHanging={controllerHanging}");
            lastGrappling = grappling;
            lastHanging = hanging;
        }
    }

    public bool IsGrappling => grappling;
    public bool IsHanging => hanging;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grappleRange);

        if (grappling)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(grapplePoint, 0.15f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, grapplePoint);
        }

        if (hangCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(hangCheck.position, hangSnapDistance);
        }
    }
}
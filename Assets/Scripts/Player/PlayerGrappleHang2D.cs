using UnityEngine;

public class PlayerGrappleHang2D : MonoBehaviour
{
    [Header("Grapple")]
    public float grappleRange = 8f;
    public float pullSpeed = 18f;
    public LayerMask hangLayer;

    [Header("Debug")]
    public bool debugLogs = true;

    float hangLockTimer;

    Rigidbody2D rb;
    PlayerController2D controller;

    Vector2 grapplePoint;

    bool grappling;
    bool hanging;

    bool lastGrappling;
    bool lastHanging;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController2D>();
    }

    void FixedUpdate()
    {
        if (hangLockTimer > 0f)
            hangLockTimer -= Time.fixedDeltaTime;

        if (hanging)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (hangLockTimer > 0f)
            return;

        if (!grappling)
            return;

        Vector2 dir = (grapplePoint - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * pullSpeed;

        if (transform.position.y >= grapplePoint.y - 0.5f)
        {
            StartHang(grapplePoint);
        }

        WatchState();
    }

    public void TryGrapple(Vector2 mousePos)
    {
        if (hanging || hangLockTimer > 0f)
        {
            //Log($"TryGrapple BLOCKED | hanging={hanging} lock={hangLockTimer:F2}");
            return;
        }

        Vector2 worldMouse = Camera.main.ScreenToWorldPoint(mousePos);
        Vector2 dir = (worldMouse - (Vector2)transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            dir,
            grappleRange,
            hangLayer
        );

        if (hit.collider == null)
        {
            //Log($"TryGrapple MISS | mouse={worldMouse}");
            return;
        }

        if (hit.normal.y > -0.5f)
        {
            //Log($"TryGrapple REJECTED WALL | normal={hit.normal} point={hit.point} object={hit.collider.name}");
            return;
        }

        grapplePoint = hit.point;
        grappling = true;
        hanging = false;

        //Log($"TryGrapple HIT | object={hit.collider.name} point={hit.point} normal={hit.normal}");
        WatchState();
    }

    void StartHang(Vector2 point)
    {
        grappling = false;
        hanging = true;

        rb.linearVelocity = Vector2.zero;
        transform.position = point + Vector2.down * 0.5f;

        //Log($"StartHang | finalPos={transform.position} point={point}");

        controller.SetHanging(true);
        WatchState();
    }

    public void DropHang()
    {
        if (!hanging)
        {
            //Log("DropHang called but hanging was already false");
            return;
        }

        grappling = false;
        hanging = false;

        rb.linearVelocity = new Vector2(0f, -6f);
        transform.position += Vector3.down * 0.35f;

        hangLockTimer = 0.25f;

        //Log($"DropHang | pos={transform.position} vel={rb.linearVelocity} lock={hangLockTimer:F2}");

        controller.SetHanging(false);
        WatchState();
    }

    void WatchState()
    {
        if (lastGrappling != grappling || lastHanging != hanging)
        {
            //Log($"STATE CHANGE | grappling={grappling} hanging={hanging} controller.IsHanging={controller.IsHanging}");
            lastGrappling = grappling;
            lastHanging = hanging;
        }
    }

    public bool IsGrappling => grappling;
    public bool IsHanging => hanging;

    void OnDrawGizmos()
    {
        // Draw grapple range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grappleRange);

        // Draw grapple point if grappling
        if (grappling)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(grapplePoint, 0.15f);

            // Line from player to grapple point
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, grapplePoint);
        }
    }
}
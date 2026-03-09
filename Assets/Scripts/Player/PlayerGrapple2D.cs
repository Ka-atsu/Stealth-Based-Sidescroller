using UnityEngine;

public class PlayerGrapple2D : MonoBehaviour
{
    public float grappleRange = 8f;
    public LayerMask hangLayer;
    public float pullSpeed = 15f;

    Rigidbody2D rb;
    PlayerController2D controller;

    Vector2 grapplePoint;
    bool grappling;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController2D>();
    }

    void Update()
    {
        if (!grappling) return;

        Vector2 dir = (grapplePoint - (Vector2)transform.position).normalized;

        rb.linearVelocity = dir * pullSpeed;

        float dist = Vector2.Distance(transform.position, grapplePoint);

        if (dist < 0.3f)
        {
            grappling = false;
            rb.linearVelocity = Vector2.zero;

            controller.StartHang(grapplePoint);
        }
    }

    public void TryGrapple(Vector2 mousePosition)
    {
        Vector2 worldMouse = Camera.main.ScreenToWorldPoint(mousePosition);

        Vector2 dir = (worldMouse - (Vector2)transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            dir,
            grappleRange,
            hangLayer
        );

        if (hit.collider == null)
            return;

        // Check surface normal (reject walls)
        if (hit.normal.y > -0.5f)
            return;

        grapplePoint = hit.point;
        grappling = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grappleRange);
    }
}
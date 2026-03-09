using UnityEngine;

public class PlayerHang2D : MonoBehaviour
{
    public Transform hangCheck;
    public float hangRadius = 0.3f;
    public LayerMask hangLayer;

    PlayerController2D controller;
    Rigidbody2D rb;

    float hangCooldown;      // cooldown after dropping
    float rehangCooldown;    // cooldown before allowed to hang again

    void Awake()
    {
        controller = GetComponent<PlayerController2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (hangCooldown > 0f)
            hangCooldown -= Time.deltaTime;

        if (rehangCooldown > 0f)
            rehangCooldown -= Time.deltaTime;

        // prevent hanging while crouching
        if (controller.MoveInput.y < -0.1f)
            return;

        if (controller.IsHanging)
            return;

        if (hangCooldown > 0f)
            return;

        if (rehangCooldown > 0f)
            return;

        TryHang();
    }

    void TryHang()
    {
        if (rb.linearVelocity.y <= 0f) return;

        Collider2D hit = Physics2D.OverlapCircle(
            hangCheck.position,
            hangRadius,
            hangLayer
        );

        if (hit == null) return;

        float ceilingY = hit.bounds.min.y;
        float playerY = transform.position.y;

        if (playerY > ceilingY - 0.2f)
            return;

        Vector2 hangPoint = hit.ClosestPoint(hangCheck.position);
        hangPoint += Vector2.down * 0.5f;

        controller.StartHang(hangPoint);

        // prevent instant rehang loops
        rehangCooldown = 0.3f;
    }

    public void SetHangCooldown(float time)
    {
        hangCooldown = time;
    }

    public void SetRehangCooldown(float time)
    {
        rehangCooldown = time;
    }

    void OnDrawGizmosSelected()
    {
        if (hangCheck == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(hangCheck.position, hangRadius);
    }
}
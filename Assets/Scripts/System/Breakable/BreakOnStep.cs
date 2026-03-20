using System.Collections;
using UnityEngine;

public class BreakOnStep : MonoBehaviour
{
    [SerializeField] private float breakDelay = 0.2f;
    [SerializeField] private bool destroyObject = true;

    [Header("Juice")]
    [SerializeField] private ParticleSystem breakParticles;
    [SerializeField] private GameObject fragmentPrefab;
    [SerializeField] private int fragmentCount = 4;
    [SerializeField] private float shakeAmount = 0.06f;
    [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0.8f);

    private bool breaking = false;
    private Collider2D col;
    private SpriteRenderer sr;
    private Vector3 originalLocalPos;
    private Color originalColor = Color.white;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        originalLocalPos = transform.localPosition;

        if (sr != null)
            originalColor = sr.color;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (breaking) return;
        if (!collision.collider.CompareTag("Player")) return;

        foreach (ContactPoint2D hit in collision.contacts)
        {
            if (hit.normal.y < -0.5f)
            {
                StartCoroutine(BreakRoutine());
                break;
            }
        }
    }

    private IEnumerator BreakRoutine()
    {
        breaking = true;

        float timer = 0f;

        while (timer < breakDelay)
        {
            timer += Time.deltaTime;

            transform.localPosition = originalLocalPos + (Vector3)Random.insideUnitCircle * shakeAmount;

            if (sr != null)
            {
                float pulse = Mathf.PingPong(timer * 20f, 1f);
                sr.color = Color.Lerp(originalColor, warningColor, pulse);
            }

            yield return null;
        }

        transform.localPosition = originalLocalPos;

        if (sr != null)
            sr.color = originalColor;

        if (breakParticles != null)
        {
            ParticleSystem ps = Instantiate(breakParticles, transform.position, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, 2f);
        }

        if (fragmentPrefab != null)
        {
            for (int i = 0; i < fragmentCount; i++)
            {
                GameObject frag = Instantiate(fragmentPrefab, transform.position, Quaternion.identity);
                Rigidbody2D rb = frag.GetComponent<Rigidbody2D>();

                if (rb != null)
                {
                    Vector2 force = new Vector2(Random.Range(-2f, 2f), Random.Range(2f, 4f));
                    rb.AddForce(force, ForceMode2D.Impulse);
                    rb.AddTorque(Random.Range(-200f, 200f));
                }

                Destroy(frag, 1.2f);
            }
        }

        if (destroyObject)
        {
            Destroy(gameObject);
        }
        else
        {
            if (col != null) col.enabled = false;
            if (sr != null) sr.enabled = false;
        }
    }
}
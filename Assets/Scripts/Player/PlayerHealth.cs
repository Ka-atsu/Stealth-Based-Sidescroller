using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float bleedDurationOnHit = 4f;

    private int currentHealth;
    private SpriteRenderer sr;
    private PlayerBleeding bleeding;
    private TrailRenderer bloodTrail;  // Reference to the TrailRenderer

    private void Awake()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        bleeding = GetComponent<PlayerBleeding>();

        // Get the TrailRenderer component on the player
        bloodTrail = GetComponent<TrailRenderer>();

        if (bloodTrail != null)
        {
            bloodTrail.enabled = false; // Make sure it's initially disabled
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Flash red on damage
        StopAllCoroutines();
        StartCoroutine(FlashRed());

        // Start the bleeding effect (if any)
        if (bleeding != null)
        {
            bleeding.StartBleeding(bleedDurationOnHit);
        }

        // Enable blood trail when taking damage
        if (bloodTrail != null)
        {
            bloodTrail.enabled = true;
            StartCoroutine(StopBloodTrailAfterDelay(bleedDurationOnHit)); // Stop the blood trail after a delay
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashRed()
    {
        if (sr != null)
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            sr.color = Color.white;
        }
    }

    private void Die()
    {
        Debug.Log("Player died");
        gameObject.SetActive(false); // Player dies, deactivate the GameObject (can be changed to trigger death animation, etc.)
    }

    private IEnumerator StopBloodTrailAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bloodTrail != null)
        {
            bloodTrail.enabled = false; // Disable the blood trail after the duration ends
        }
    }
}
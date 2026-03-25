using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float bleedDurationOnHit = 10f;

    [Header("Hit Stop")]
    [SerializeField] private float hitStopDuration = 0.06f;
    [SerializeField] private float hitStopTimeScale = 0.05f;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 0.5f;
    [SerializeField] private float flashInterval = 0.08f;

    [Header("Hit Stun")]
    [SerializeField] private float controlLockDuration = 0.15f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForceX = 7f;
    [SerializeField] private float knockbackForceY = 4f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeMagnitude = 1.2f;

    [Header("Death")]
    [SerializeField] private string deadLayerName = "DeadPlayer";

    private int currentHealth;
    private bool isInvincible;
    private bool isDead;

    public bool IsDead => isDead;

    private SpriteRenderer sr;
    private PlayerBleeding bleeding;
    private TrailRenderer bloodTrail;
    private Rigidbody2D rb;
    private PlayerController2D controller;
    private PlayerAnimation2D playerAnimation;

    private int deadLayer = -1;
    private int originalLayer;

    private Coroutine bloodTrailCoroutine;
    private Coroutine invincibilityCoroutine;
    private Coroutine hitStopCoroutine;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        currentHealth = maxHealth;

        sr = GetComponentInChildren<SpriteRenderer>();
        bleeding = GetComponent<PlayerBleeding>();
        bloodTrail = GetComponent<TrailRenderer>();
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController2D>();
        playerAnimation = GetComponent<PlayerAnimation2D>();

        originalLayer = gameObject.layer;
        deadLayer = LayerMask.NameToLayer(deadLayerName);

        if (bloodTrail != null)
            bloodTrail.enabled = false;
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(int damage, Vector2 attackerPosition)
    {
        if (isDead || isInvincible)
            return;

        currentHealth -= damage;

        if (hitStopCoroutine != null)
            StopCoroutine(hitStopCoroutine);

        float scaledHitStop = hitStopDuration * Mathf.Lerp(1f, 2f, Mathf.Clamp01(damage / 3f));
        hitStopCoroutine = StartCoroutine(HitStop(scaledHitStop));

        HitFlash.Instance?.Flash();

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRed());

        if (bleeding != null)
            bleeding.StartBleeding(bleedDurationOnHit);

        if (bloodTrail != null)
        {
            bloodTrail.enabled = true;

            if (bloodTrailCoroutine != null)
                StopCoroutine(bloodTrailCoroutine);

            bloodTrailCoroutine = StartCoroutine(StopBloodTrailAfterDelay(bleedDurationOnHit));
        }

        ApplyKnockback(attackerPosition);

        Vector2 hitDir = (transform.position - (Vector3)attackerPosition);
        hitDir.y *= 0.3f;
        hitDir.Normalize();

        float scaledShake = shakeMagnitude * Mathf.Clamp(damage, 1, 3);

        CameraImpulseSource.Instance?.Shake(hitDir, scaledShake * 1.8f);
        CameraImpulseSource.Instance?.Shake(hitDir, scaledShake);
        CameraZoomPunch.Instance?.Punch(0.2f + (damage * 0.05f), 0.12f);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (controller != null)
            StartCoroutine(DelayedStun());

        if (invincibilityCoroutine != null)
            StopCoroutine(invincibilityCoroutine);

        invincibilityCoroutine = StartCoroutine(InvincibilityFrames());
    }

    private void ApplyKnockback(Vector2 attackerPosition)
    {
        if (rb == null)
            return;

        float direction = transform.position.x < attackerPosition.x ? -1f : 1f;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.AddForce(new Vector2(direction * knockbackForceX, knockbackForceY), ForceMode2D.Impulse);
    }

    private IEnumerator HitStop(float duration)
    {
        float originalTimeScale = Time.timeScale;

        Time.timeScale = hitStopTimeScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = originalTimeScale;
    }

    private IEnumerator FlashRed()
    {
        if (sr == null)
            yield break;

        sr.color = Color.red;
        yield return new WaitForSecondsRealtime(0.12f);

        if (!isDead)
            sr.color = Color.white;

        flashCoroutine = null;
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        float elapsed = 0f;

        while (elapsed < invincibilityDuration)
        {
            if (sr != null)
                sr.enabled = !sr.enabled;

            yield return new WaitForSecondsRealtime(flashInterval);
            elapsed += flashInterval;
        }

        if (sr != null)
            sr.enabled = true;

        isInvincible = false;
        invincibilityCoroutine = null;
    }

    private IEnumerator StopBloodTrailAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (bloodTrail != null)
            bloodTrail.enabled = false;

        bloodTrailCoroutine = null;
    }

    private IEnumerator DelayedStun()
    {
        yield return new WaitForSecondsRealtime(0.02f);
        controller?.Stun(controlLockDuration);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (sr != null)
        {
            sr.enabled = true;
            sr.color = Color.white;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (invincibilityCoroutine != null)
        {
            StopCoroutine(invincibilityCoroutine);
            invincibilityCoroutine = null;
        }

        isInvincible = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (deadLayer != -1)
            gameObject.layer = deadLayer;

        playerAnimation?.PlayDeathAnimation();

        if (controller != null)
        {
            controller.DisableControl();
            controller.enabled = false;
        }
    }

    public void OnDeathAnimationFinished()
    {
        gameObject.SetActive(false);
    }
}
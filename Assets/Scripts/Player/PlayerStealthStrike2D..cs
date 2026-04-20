using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerStealthStrike2D : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugStealthStrike = true;

    [Header("Stealth Strike")]
    [SerializeField] private float strikeRange = 1.5f;
    [SerializeField] private Vector2 strikeOffset = new Vector2(0.7f, 0.7f);
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float strikeCooldown = 0.2f;
    [SerializeField] private float strikeAnimationLockDuration = 0.25f;

    [Header("Prompt")]
    [SerializeField] private GameObject stealthPromptVisual;
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private float promptFollowSpeed = 20f;
    [SerializeField] private bool keepPromptZ = true;

    [Header("Impact Juice")]
    [SerializeField] private GameObject slashVfxPrefab;
    [SerializeField] private GameObject airCrackVfxPrefab;

    [Tooltip("Base hit position relative to the player. X automatically flips with facing.")]
    [SerializeField] private Vector3 hitVfxOffset = new Vector3(0.25f, 0.2f, 0f);

    [Tooltip("Extra slash offset relative to base hit position. X automatically flips with facing.")]
    [SerializeField] private Vector3 slashVfxOffset = new Vector3(0f, 0f, 0f);

    [Tooltip("Extra crack offset relative to base hit position. X automatically flips with facing.")]
    [SerializeField] private Vector3 crackVfxOffset = new Vector3(-0.12f, 0.02f, 0f);

    [SerializeField] private float hitStopDuration = 0.045f;
    [SerializeField] private float hitStopTimeScale = 0.05f;

    [SerializeField] private float postHitKillDelay = 0.04f;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip stealthSlashSfx;

    [Header("Cinemachine Impulse")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private Vector3 impulseVelocity = new Vector3(5f, 3f, 0f);

    private float nextStrikeTime;
    private PlayerController2D controller;
    private PlayerAnimation2D playerAnimation;
    private EnemyAI currentTarget;
    private EnemyAI pendingStrikeTarget;
    private bool strikeInProgress;

    private void Awake()
    {
        controller = GetComponent<PlayerController2D>();
        playerAnimation = GetComponent<PlayerAnimation2D>();

        if (impulseSource == null)
            impulseSource = GetComponentInChildren<CinemachineImpulseSource>(true);

        if (stealthPromptVisual != null)
            stealthPromptVisual.SetActive(false);
    }

    private void Update()
    {
        if (strikeInProgress)
        {
            if (stealthPromptVisual != null && stealthPromptVisual.activeSelf)
                stealthPromptVisual.SetActive(false);
            return;
        }

        float facingSign = controller != null ? controller.FacingSign : 1f;

        currentTarget = FindBestTarget(facingSign);
        UpdatePrompt();
    }

    public void TryStealthStrike(float facingSign)
    {
        if (Time.time < nextStrikeTime)
        {
            Log("Blocked: cooldown");
            return;
        }

        if (strikeInProgress)
        {
            Log("Blocked: strike already in progress");
            return;
        }

        EnemyAI target = FindBestTarget(facingSign);
        if (target == null)
        {
            Log("No stealth target found");
            return;
        }

        nextStrikeTime = Time.time + strikeCooldown;
        strikeInProgress = true;
        pendingStrikeTarget = target;

        Log($"Stealth strike started on: {target.name}");

        pendingStrikeTarget.EnterStealthStrikeVictimState(transform);

        playerAnimation?.PlayStealthStrikeAnimation();

        if (controller != null)
            controller.Stun(strikeAnimationLockDuration);
    }

    // Animation Event on the hit frame
    public void OnStealthStrikeHit()
    {
        Log("STEALTH HIT EVENT CALLED");

        if (pendingStrikeTarget == null)
        {
            Log("Hit event fired but pendingStrikeTarget is null");
            return;
        }

        float facingSign = controller != null ? controller.FacingSign : 1f;
        Vector3 hitPos = pendingStrikeTarget.transform.position + GetFacingOffset(facingSign);

        SpawnImpactVfx(hitPos, facingSign);
        PlayImpactSfx();
        DoCameraShake();

        StartCoroutine(PerformHitSequence(pendingStrikeTarget));

        pendingStrikeTarget = null;
    }

    // Animation Event on the last frame
    public void OnStealthStrikeFinished()
    {
        Log("STEALTH FINISHED EVENT CALLED");

        if (pendingStrikeTarget != null)
        {
            Log($"Strike finished without hit, releasing target: {pendingStrikeTarget.name}");
            pendingStrikeTarget.ExitStealthStrikeVictimState();
            pendingStrikeTarget = null;
        }

        strikeInProgress = false;
    }

    private IEnumerator PerformHitSequence(EnemyAI target)
    {
        yield return StartCoroutine(HitStopRoutine());

        if (target != null && postHitKillDelay > 0f)
            yield return new WaitForSeconds(postHitKillDelay);

        if (target != null)
        {
            Log($"Destroying target: {target.name}");
            target.DieFromStealthStrike();

            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.RegisterSuccessfulStealthKill();
            }
        }
    }

    private IEnumerator HitStopRoutine()
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = hitStopTimeScale;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = originalTimeScale;
    }

    private void SpawnImpactVfx(Vector3 hitPos, float facingSign)
    {
        Quaternion rotation = facingSign >= 0f
            ? Quaternion.Euler(0f, 0f, -18f)
            : Quaternion.Euler(0f, 180f, 18f);

        Vector3 slashPos = hitPos + GetFacingAdjustedOffset(slashVfxOffset, facingSign);
        Vector3 crackPos = hitPos + GetFacingAdjustedOffset(crackVfxOffset, facingSign);

        if (slashVfxPrefab != null)
            Instantiate(slashVfxPrefab, slashPos, rotation);

        if (airCrackVfxPrefab != null)
            Instantiate(airCrackVfxPrefab, crackPos, rotation);
    }

    private void PlayImpactSfx()
    {
        if (audioSource != null && stealthSlashSfx != null)
            audioSource.PlayOneShot(stealthSlashSfx);
    }

    private void DoCameraShake()
    {
        if (impulseSource == null)
        {
            Debug.LogWarning("CinemachineImpulseSource is NULL on PlayerStealthStrike2D", this);
            return;
        }

        float facingSign = controller != null ? controller.FacingSign : 1f;

        Vector3 velocity = new Vector3(
            Mathf.Abs(impulseVelocity.x) * facingSign,
            impulseVelocity.y,
            impulseVelocity.z
        );

        impulseSource.GenerateImpulse(velocity);
    }

    private Vector3 GetFacingOffset(float facingSign)
    {
        return new Vector3(
            Mathf.Abs(hitVfxOffset.x) * facingSign,
            hitVfxOffset.y,
            hitVfxOffset.z
        );
    }

    private Vector3 GetFacingAdjustedOffset(Vector3 offset, float facingSign)
    {
        return new Vector3(
            offset.x * facingSign,
            offset.y,
            offset.z
        );
    }

    private EnemyAI FindBestTarget(float facingSign)
    {
        Vector2 strikeCenter = GetStrikeCenter(facingSign);
        Collider2D[] hits = Physics2D.OverlapCircleAll(strikeCenter, strikeRange, enemyMask);

        EnemyAI bestTarget = null;
        float closestSqrDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy == null)
                continue;

            if (!enemy.CanBeStealthKilledFrom(transform.position))
                continue;

            float sqrDistance = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;

            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                bestTarget = enemy;
            }
        }

        return bestTarget;
    }

    private void UpdatePrompt()
    {
        if (stealthPromptVisual == null)
            return;

        if (currentTarget == null)
        {
            if (stealthPromptVisual.activeSelf)
                stealthPromptVisual.SetActive(false);

            return;
        }

        if (!stealthPromptVisual.activeSelf)
            stealthPromptVisual.SetActive(true);

        Vector3 targetPos = GetPromptPosition(currentTarget);

        if (keepPromptZ)
            targetPos.z = stealthPromptVisual.transform.position.z;

        stealthPromptVisual.transform.position = Vector3.Lerp(
            stealthPromptVisual.transform.position,
            targetPos,
            promptFollowSpeed * Time.deltaTime
        );
    }

    private Vector3 GetPromptPosition(EnemyAI enemy)
    {
        Renderer render = enemy.GetComponentInChildren<Renderer>();
        if (render != null)
        {
            Vector3 topCenter = new Vector3(
                render.bounds.center.x,
                render.bounds.max.y,
                enemy.transform.position.z
            );

            return topCenter + promptOffset;
        }

        Collider2D col = enemy.GetComponentInChildren<Collider2D>();
        if (col != null)
        {
            Vector3 topCenter = new Vector3(
                col.bounds.center.x,
                col.bounds.max.y,
                enemy.transform.position.z
            );

            return topCenter + promptOffset;
        }

        return enemy.transform.position + promptOffset;
    }

    private Vector2 GetStrikeCenter(float facingSign)
    {
        return (Vector2)transform.position +
               new Vector2(Mathf.Abs(strikeOffset.x) * facingSign, strikeOffset.y);
    }

    private void Log(string msg)
    {
        if (!debugStealthStrike)
            return;

        Debug.Log($"[PlayerStealthStrike2D] {msg}", this);
    }

    private void OnDrawGizmos()
    {
        float facingSign = 1f;

        if (Application.isPlaying)
        {
            PlayerController2D pc = GetComponent<PlayerController2D>();
            if (pc != null)
                facingSign = pc.FacingSign;
        }

        Vector2 strikeCenter = (Vector2)transform.position +
                               new Vector2(Mathf.Abs(strikeOffset.x) * facingSign, strikeOffset.y);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(strikeCenter, strikeRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(strikeCenter, 0.05f);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, strikeCenter);
    }
}
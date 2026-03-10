using UnityEngine;

public class PlayerStealthStrike2D : MonoBehaviour
{
    [Header("Stealth Strike")]
    [SerializeField] private float strikeRange = 1.5f;
    [SerializeField] private Vector2 strikeOffset = new Vector2(0.65f, 0f);
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float strikeCooldown = 0.2f;

    [Header("Prompt")]
    [SerializeField] private GameObject stealthPromptVisual;
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.2f, 0f);

    private float nextStrikeTime;
    private PlayerController2D controller;
    private EnemyAI currentTarget;

    void Start()
    {
        if (stealthPromptVisual != null)
        {
            stealthPromptVisual.SetActive(true);
            stealthPromptVisual.SetActive(false);
        }
    }

    void Awake()
    {
        controller = GetComponent<PlayerController2D>();

        if (stealthPromptVisual != null)
            stealthPromptVisual.SetActive(false);
    }

    void Update()
    {
        float facingSign = controller != null ? controller.FacingSign : 1f;

        currentTarget = FindBestTarget(facingSign);
        UpdatePrompt();
    }

    public void TryStealthStrike(float facingSign)
    {
        if (Time.time < nextStrikeTime)
            return;

        nextStrikeTime = Time.time + strikeCooldown;

        EnemyAI target = FindBestTarget(facingSign);

        if (target != null)
        {
            target.DieFromStealthStrike();
        }
    }

    private EnemyAI FindBestTarget(float facingSign)
    {
        Vector2 strikeCenter = GetStrikeCenter(facingSign);
        Collider2D[] hits = Physics2D.OverlapCircleAll(strikeCenter, strikeRange, enemyMask);

        EnemyAI bestTarget = null;
        float closestSqrDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
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

        stealthPromptVisual.transform.position = currentTarget.transform.position + promptOffset;
    }

    private Vector2 GetStrikeCenter(float facingSign)
    {
        return (Vector2)transform.position +
               new Vector2(Mathf.Abs(strikeOffset.x) * facingSign, strikeOffset.y);
    }

    private void OnDrawGizmosSelected()
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
    }
}
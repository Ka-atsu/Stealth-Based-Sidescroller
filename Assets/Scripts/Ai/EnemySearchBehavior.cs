using UnityEngine;

public class EnemySearchBehavior : MonoBehaviour
{
    private EnemyMovement movement;

    private Vector3[] searchPoints;
    private int currentPointIndex;
    private bool searchInitialized;
    private bool isWaiting;
    private float waitTimer;
    private Vector3 lastSearchTarget;

    [SerializeField] private float nearSearchOffset = 1.5f;
    [SerializeField] private float farSearchOffset = 3f;
    [SerializeField] private float reachDistance = 0.4f;
    [SerializeField] private float waitAtPointTime = 0.5f;
    [SerializeField] private float targetRefreshDistance = 0.25f;

    void Start()
    {
        movement = GetComponent<EnemyMovement>();
    }

    // Keep the same function name so your EnemyAI does not need to change
    public void SearchRandomly(Vector3 searchTarget)
    {
        if (!searchInitialized || Vector2.Distance(lastSearchTarget, searchTarget) > targetRefreshDistance)
        {
            BuildSearchPattern(searchTarget);
        }

        if (searchPoints == null || searchPoints.Length == 0)
            return;

        if (isWaiting)
        {
            movement.Stop();

            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                isWaiting = false;
                currentPointIndex++;

                if (currentPointIndex >= searchPoints.Length)
                    currentPointIndex = 0;
            }

            return;
        }

        Vector3 currentTarget = searchPoints[currentPointIndex];
        movement.MoveTo(currentTarget);

        float dist = Vector2.Distance(transform.position, currentTarget);

        if (dist <= reachDistance)
        {
            movement.Stop();
            isWaiting = true;
            waitTimer = waitAtPointTime;
        }
    }

    private void BuildSearchPattern(Vector3 searchTarget)
    {
        lastSearchTarget = searchTarget;

        // Side-scroller search pattern:
        // center -> left near -> right near -> left far -> right far
        searchPoints = new Vector3[]
        {
            new Vector3(searchTarget.x, searchTarget.y, searchTarget.z),
            new Vector3(searchTarget.x - nearSearchOffset, searchTarget.y, searchTarget.z),
            new Vector3(searchTarget.x + nearSearchOffset, searchTarget.y, searchTarget.z),
            new Vector3(searchTarget.x - farSearchOffset, searchTarget.y, searchTarget.z),
            new Vector3(searchTarget.x + farSearchOffset, searchTarget.y, searchTarget.z)
        };

        currentPointIndex = 0;
        isWaiting = false;
        waitTimer = 0f;
        searchInitialized = true;
    }

    public void ResetSearch()
    {
        searchInitialized = false;
        isWaiting = false;
        waitTimer = 0f;
        currentPointIndex = 0;
    }
}
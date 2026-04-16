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
    private GuardResponseRole lastRole = GuardResponseRole.None;

    [SerializeField] private float nearSearchOffset = 1.5f;
    [SerializeField] private float farSearchOffset = 3f;
    [SerializeField] private float reachDistance = 0.4f;
    [SerializeField] private float waitAtPointTime = 0.5f;
    [SerializeField] private float targetRefreshDistance = 0.25f;

    void Start()
    {
        movement = GetComponent<EnemyMovement>();
    }

    public void SearchRandomly(Vector3 searchTarget)
    {
        if (!searchInitialized ||
            lastRole != GuardResponseRole.None ||
            Vector2.Distance(lastSearchTarget, searchTarget) > targetRefreshDistance)
        {
            BuildSearchPattern(searchTarget);
        }

        RunSearchMovement();
    }

    public void SearchAssigned(Vector3 searchTarget, GuardResponseRole role)
    {
        if (role != GuardResponseRole.SearchLeft && role != GuardResponseRole.SearchRight)
        {
            SearchRandomly(searchTarget);
            return;
        }

        if (!searchInitialized ||
            lastRole != role ||
            Vector2.Distance(lastSearchTarget, searchTarget) > targetRefreshDistance)
        {
            BuildAssignedPattern(searchTarget, role);
        }

        RunSearchMovement();
    }

    private void RunSearchMovement()
    {
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
        lastRole = GuardResponseRole.None;

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

    private void BuildAssignedPattern(Vector3 searchTarget, GuardResponseRole role)
    {
        lastSearchTarget = searchTarget;
        lastRole = role;

        if (role == GuardResponseRole.SearchLeft)
        {
            searchPoints = new Vector3[]
            {
                new Vector3(searchTarget.x - nearSearchOffset, searchTarget.y, searchTarget.z),
                new Vector3(searchTarget.x - farSearchOffset, searchTarget.y, searchTarget.z),
                new Vector3(searchTarget.x, searchTarget.y, searchTarget.z)
            };
        }
        else
        {
            searchPoints = new Vector3[]
            {
                new Vector3(searchTarget.x + nearSearchOffset, searchTarget.y, searchTarget.z),
                new Vector3(searchTarget.x + farSearchOffset, searchTarget.y, searchTarget.z),
                new Vector3(searchTarget.x, searchTarget.y, searchTarget.z)
            };
        }

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
        lastRole = GuardResponseRole.None;
    }
}
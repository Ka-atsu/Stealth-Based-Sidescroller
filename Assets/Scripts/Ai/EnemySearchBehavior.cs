using UnityEngine;

public class EnemySearchBehavior : MonoBehaviour
{
    private EnemyMovement movement;
    private Vector3 currentSearchPoint;
    private bool hasSearchPoint = false;

    [SerializeField] private float searchRadius = 2f;
    [SerializeField] private float reachDistance = 0.5f;

    void Start()
    {
        movement = GetComponent<EnemyMovement>();
    }

    public void SearchRandomly(Vector3 searchTarget)
    {
        if (!hasSearchPoint || Vector2.Distance(transform.position, currentSearchPoint) < reachDistance)
        {
            PickNewSearchPoint(searchTarget);
        }

        movement.MoveTo(currentSearchPoint);
    }

    private void PickNewSearchPoint(Vector3 searchTarget)
    {
        float randomX = Random.Range(-searchRadius, searchRadius);

        // Side-scroller: keep same Y as target
        currentSearchPoint = new Vector3(
            searchTarget.x + randomX,
            searchTarget.y,
            searchTarget.z
        );

        hasSearchPoint = true;
    }

    public void ResetSearch()
    {
        hasSearchPoint = false;
    }
}
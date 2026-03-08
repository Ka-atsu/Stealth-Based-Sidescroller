using UnityEngine;

public class EnemySearchBehavior : MonoBehaviour
{
    private EnemyMovement movement;

    void Start()
    {
        movement = GetComponent<EnemyMovement>();
    }

    // Method for random searching within a radius around a target
    public void SearchRandomly(Vector3 searchTarget)
    {
        // Add randomness to enemy movement around the target area
        float randomAngle = Random.Range(0, 360);  // Random angle to search in
        Vector3 searchOffset = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0) * 2f;  // Random offset

        Vector3 randomSearchPosition = searchTarget + searchOffset;

        // Move to this random position but do not use "Chase"
        movement.MoveTo(randomSearchPosition);  // Use a method to move to the random position (instead of chasing)
    }
}
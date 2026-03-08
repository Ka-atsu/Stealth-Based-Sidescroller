using UnityEngine;
using System.Collections.Generic;

public class BloodTrailManager : MonoBehaviour
{
    public Transform player;  // Reference to the player
    public List<Vector3> bloodTrailPositions = new List<Vector3>();  // List of blood trail positions
    public float trailUpdateInterval = 0.5f;  // How often the trail updates

    private float timeSinceLastUpdate = 0f;

    void Update()
    {
        // Only update the trail if the player is moving
        if (player != null)
        {
            timeSinceLastUpdate += Time.deltaTime;

            if (timeSinceLastUpdate >= trailUpdateInterval)
            {
                // Add current position to blood trail
                bloodTrailPositions.Add(player.position);

                // Register the blood position in the EnemyBloodTracker
                EnemyBloodTracker bloodTracker = player.GetComponent<EnemyBloodTracker>();
                if (bloodTracker != null)
                {
                    bloodTracker.RegisterBloodPosition(player.position);
                }

                timeSinceLastUpdate = 0f;
            }
        }
    }

    // Get the next blood position for the enemy to follow
    public Vector3 GetNextBloodPosition()
    {
        if (bloodTrailPositions.Count > 0)
        {
            // Return the first blood position, and remove it from the list
            Vector3 nextPosition = bloodTrailPositions[0];
            bloodTrailPositions.RemoveAt(0);  // Remove it after it has been used
            return nextPosition;
        }

        return Vector3.zero;  // No more blood, return vector3.zero
    }

    public bool HasBloodTarget()
    {
        return bloodTrailPositions.Count > 0;
    }
}
using UnityEngine;
using System.Collections.Generic;

public class EnemyBloodTracker : MonoBehaviour
{
    public float bloodDetectRadius = 4f;  // Radius within which blood is detected
    public LayerMask bloodLayer;  // Layer mask for blood objects (if needed for detection)

    private List<Vector3> allDetectedBloodPositions = new List<Vector3>();  // Track all detected blood positions
    private int currentBloodIndex = 0;  // Keeps track of which blood position the AI should follow

    // Register a blood position to follow (called by another script, such as BloodTrailManager)
    public void RegisterBloodPosition(Vector3 bloodPosition)
    {
        if (!allDetectedBloodPositions.Contains(bloodPosition))
        {
            allDetectedBloodPositions.Add(bloodPosition);
            Debug.Log("New blood position registered at: " + bloodPosition);
        }
    }

    // Get the next blood target position
    public Vector3 GetBloodTargetPosition()
    {
        if (allDetectedBloodPositions.Count > currentBloodIndex)
        {
            // Return the next blood position, and move the index forward
            Vector3 nextPosition = allDetectedBloodPositions[currentBloodIndex];
            return nextPosition;
        }
        return Vector3.zero;  // No more blood to follow
    }

    // Move to the next blood target position in the list
    public void MoveToNextBloodTarget()
    {
        if (allDetectedBloodPositions.Count > currentBloodIndex)
        {
            currentBloodIndex++;
        }
        else
        {
            Debug.Log("No more blood positions to follow.");
        }
    }

    // Check if there are any blood positions to follow
    public bool HasBloodTarget()
    {
        return allDetectedBloodPositions.Count > 0;  // Return true if there are any blood positions
    }

    // Clean up outdated or invalid blood positions (if necessary)
    public void CleanUpBloodPositions()
    {
        for (int i = allDetectedBloodPositions.Count - 1; i >= 0; i--)
        {
            if (Vector2.Distance(transform.position, allDetectedBloodPositions[i]) > bloodDetectRadius)
            {
                Debug.Log("Removing outdated blood position at: " + allDetectedBloodPositions[i]);
                allDetectedBloodPositions.RemoveAt(i);
            }
        }
    }
}
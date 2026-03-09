using UnityEngine;
using System.Collections;

public class SmokeBombController : MonoBehaviour
{
    public GameObject smokeBombPrefab;  // Reference to the smoke bomb prefab
    public Transform smokeBombSpawnPoint;  // The point where the smoke bomb spawns

    private bool isSmokeBombActive = false; // To track the smoke bomb state

    public void TriggerSmokeBomb()
    {
        if (isSmokeBombActive) return;  // Prevent triggering multiple times

        // Activate the smoke bomb logic
        isSmokeBombActive = true;

        // Spawn the smoke bomb at the spawn point
        if (smokeBombPrefab != null && smokeBombSpawnPoint != null)
        {
            Instantiate(smokeBombPrefab, smokeBombSpawnPoint.position, Quaternion.identity);
        }

        // Optionally, you can set visibility or "hidden" status here for the player (or use a separate script for that)

        // Log for debugging purposes
        Debug.Log("Smoke Bomb Triggered!");

        // Set a timer for the smoke duration
        StartCoroutine(DisableSmokeBomb());
    }

    private IEnumerator DisableSmokeBomb()
    {
        // Wait for the smoke duration (5 seconds here)
        yield return new WaitForSeconds(5f);

        // Reset smoke bomb state after the duration ends
        isSmokeBombActive = false;
        Debug.Log("Smoke Bomb Ended");
    }
}
using UnityEngine;

public class PlayerBleeding : MonoBehaviour
{
    private TrailRenderer bloodTrail;
    private float bleedDuration;
    private float timeLeftToBleed;

    void Start()
    {
        // Get the TrailRenderer attached to the player
        bloodTrail = GetComponent<TrailRenderer>();

        // Ensure the blood trail is disabled initially
        if (bloodTrail != null)
        {
            bloodTrail.enabled = false;
        }
    }

    // This function is called to start bleeding (triggered by taking damage)
    public void StartBleeding(float duration)
    {
        bleedDuration = duration;
        timeLeftToBleed = bleedDuration;

        if (bloodTrail != null)
        {
            bloodTrail.enabled = true;  // Ensure the blood trail is enabled when bleeding starts
        }
    }

    void Update()
    {
        // If bleeding is active, count down the timer
        if (timeLeftToBleed > 0)
        {
            timeLeftToBleed -= Time.deltaTime;

            // If bleeding duration has ended, stop the blood trail
            if (timeLeftToBleed <= 0)
            {
                StopBleeding();
            }
        }
    }

    // Stop the bleeding effect and hide the blood trail
    private void StopBleeding()
    {
        if (bloodTrail != null)
        {
            bloodTrail.enabled = false;  // Disable the blood trail once the bleed is over
        }
    }
}
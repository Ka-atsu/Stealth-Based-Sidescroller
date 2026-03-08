using UnityEngine;

public class PlayerBleeding : MonoBehaviour
{
    private TrailRenderer bloodTrail;
    private float bleedDuration;
    private float timeLeftToBleed;

    public bool IsBleeding => timeLeftToBleed > 0f;

    void Start()
    {
        bloodTrail = GetComponent<TrailRenderer>();

        if (bloodTrail != null)
        {
            bloodTrail.enabled = false;
        }
    }

    public void StartBleeding(float duration)
    {
        bleedDuration = duration;
        timeLeftToBleed = bleedDuration;

        if (bloodTrail != null)
        {
            bloodTrail.enabled = true;
        }
    }

    void Update()
    {
        if (timeLeftToBleed > 0)
        {
            timeLeftToBleed -= Time.deltaTime;

            if (timeLeftToBleed <= 0)
            {
                StopBleeding();
            }
        }
    }

    private void StopBleeding()
    {
        if (bloodTrail != null)
        {
            bloodTrail.enabled = false;
        }
    }
}
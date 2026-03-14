using UnityEngine;

public class NinjaAudioManager : MonoBehaviour
{
    public static NinjaAudioManager Instance;

    public AudioSource audioSource;

    public AudioClip footstepSound;
    public AudioClip jumpSound;
    public AudioClip dashSound;
    public AudioClip landingSound;

    void Awake()
    {
        // Ensure only one global instance exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayFootstep()
    {
        audioSource.PlayOneShot(footstepSound);
    }

    public void PlayJump()
    {
        audioSource.PlayOneShot(jumpSound);
    }

    public void PlayDash()
    {
        audioSource.PlayOneShot(dashSound);
    }

    public void PlayLanding()
    {
        audioSource.PlayOneShot(landingSound);
    }
}
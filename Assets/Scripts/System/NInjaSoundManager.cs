using UnityEngine;

[RequireComponent(typeof(AudioSource))]
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
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void PlayFootstep()
    {
        PlayClip(footstepSound);
    }

    public void PlayJump()
    {
        PlayClip(jumpSound);
    }

    public void PlayDash()
    {
        PlayClip(dashSound);
    }

    public void PlayLanding()
    {
        PlayClip(landingSound);
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}
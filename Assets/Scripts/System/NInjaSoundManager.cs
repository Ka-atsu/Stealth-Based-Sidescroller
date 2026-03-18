using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class NinjaAudioManager : MonoBehaviour
{
    public static NinjaAudioManager Instance;

    public AudioSource audioSource;

    public AudioClip footstepSound;
    public AudioClip jumpSound;
    public AudioClip dashSound;
    public AudioClip landingSound;

    // Sound delay adjustments (in seconds)
    public float footstepDelay = 0f;
    public float jumpDelay = 0f;
    public float dashDelay = 0f;
    public float landingDelay = 0f;

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
        PlayClipWithDelay(footstepSound, footstepDelay);
    }

    public void PlayJump()
    {
        PlayClipWithDelay(jumpSound, jumpDelay);
    }

    public void PlayDash()
    {
        PlayClipWithDelay(dashSound, dashDelay);
    }

    public void PlayLanding()
    {
        PlayClipWithDelay(landingSound, landingDelay);
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    void PlayClipWithDelay(AudioClip clip, float delay)
    {
        if (audioSource == null || clip == null)
            return;

        if (delay > 0)
        {
            StartCoroutine(PlayClipDelayed(clip, delay));
        }
        else
        {
            audioSource.PlayOneShot(clip);
        }
    }

    IEnumerator PlayClipDelayed(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.PlayOneShot(clip);
    }
}
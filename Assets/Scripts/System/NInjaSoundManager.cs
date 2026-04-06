using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class NinjaAudioManager : MonoBehaviour
{
    public static NinjaAudioManager Instance;

    public AudioSource audioSource;
    private AudioSource musicSource;

    public AudioClip footstepSound;
    public AudioClip jumpSound;
    public AudioClip dashSound;
    public AudioClip landingSound;

    [Header("Sound Volume Levels (0-1)")]
    [Range(0f, 1f)]
    public float footstepVolume = 1f;
    [Range(0f, 1f)]
    public float jumpVolume = 1f;
    [Range(0f, 1f)]
    public float dashVolume = 1f;
    [Range(0f, 1f)]
    public float landingVolume = 1f;

    [Header("Background Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)]
    public float musicVolume = 0.7f;
    public bool autoPlayMusic = true;

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
            DontDestroyOnLoad(gameObject); // Persist across scenes
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

        // Create a separate AudioSource for background music
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = false; // Will be set per track

        // Auto-play background music if enabled
        if (autoPlayMusic && backgroundMusic != null)
        {
            PlayBackgroundMusic(backgroundMusic, musicVolume, true);
        }
    }

    public void PlayFootstep()
    {
        PlayClipWithDelay(footstepSound, footstepDelay, footstepVolume);
    }

    public void PlayJump()
    {
        PlayClipWithDelay(jumpSound, jumpDelay, jumpVolume);
    }

    public void PlayDash()
    {
        PlayClipWithDelay(dashSound, dashDelay, dashVolume);
    }

    public void PlayLanding()
    {
        PlayClipWithDelay(landingSound, landingDelay, landingVolume);
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    void PlayClipWithDelay(AudioClip clip, float delay, float volume = 1f)
    {
        if (audioSource == null || clip == null)
            return;

        if (delay > 0)
        {
            StartCoroutine(PlayClipDelayed(clip, delay, volume));
        }
        else
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    IEnumerator PlayClipDelayed(AudioClip clip, float delay, float volume = 1f)
    {
        yield return new WaitForSeconds(delay);
        audioSource.PlayOneShot(clip, volume);
    }

    public void PlayBackgroundMusic(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (musicSource == null || clip == null)
            return;

        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopBackgroundMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp01(volume);
    }
}
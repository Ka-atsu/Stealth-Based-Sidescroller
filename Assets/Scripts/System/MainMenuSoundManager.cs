using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MainMenuSoundManager : MonoBehaviour
{
    public static MainMenuSoundManager Instance;

    [Header("UI One-Shot Source")]
    public AudioSource audioSource;

    [Header("Background Music Source")]
    public AudioSource musicSource;

    [Header("UI Audio Clips")]
    public AudioClip buttonClickSound;
    public AudioClip buttonHoverSound;
    public AudioClip backSound;

    [Header("UI Volume Levels (0-1)")]
    [Range(0f, 1f)]
    public float buttonClickVolume = 1f;
    [Range(0f, 1f)]
    public float buttonHoverVolume = 1f;
    [Range(0f, 1f)]
    public float backVolume = 1f;

    [Header("Background Music")]
    public AudioClip mainMenuLoopClip;
    public bool playLoopOnStart = true;

    [Header("Music Volume Level (0-1)")]
    [Range(0f, 1f)]
    public float musicVolume = 1f;

    [Header("Sound Delay (seconds)")]
    public float buttonClickDelay = 0f;
    public float buttonHoverDelay = 0f;
    public float backDelay = 0f;

    void Awake()
    {
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

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
    }

    void Start()
    {
        if (playLoopOnStart)
        {
            PlayMainMenuLoop();
        }
    }

    public void PlayButtonClick()
    {
        PlayClipWithDelay(buttonClickSound, buttonClickDelay, buttonClickVolume);
    }

    public void PlayButtonHover()
    {
        PlayClipWithDelay(buttonHoverSound, buttonHoverDelay, buttonHoverVolume);
    }

    public void PlayBack()
    {
        PlayClipWithDelay(backSound, backDelay, backVolume);
    }

    public void PlayMainMenuLoop()
    {
        if (musicSource == null || mainMenuLoopClip == null)
            return;

        if (musicSource.isPlaying && musicSource.clip == mainMenuLoopClip)
            return;

        musicSource.clip = mainMenuLoopClip;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMainMenuLoop()
    {
        if (musicSource == null)
            return;

        musicSource.Stop();
    }

    void PlayClipWithDelay(AudioClip clip, float delay, float volume = 1f)
    {
        if (audioSource == null || clip == null)
            return;

        if (delay > 0f)
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

        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
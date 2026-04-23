using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomCombatAttackZone : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private NinjaAudioManager audioManager;
    [SerializeField] private BossAI2D bossAI;

    [Header("Boss Music")]
    [SerializeField] private AudioClip bossMusic;
    [Range(0f, 1f)] [SerializeField] private float bossMusicVolume = 0.9f;
    [SerializeField] private bool bossMusicLoop = true;
    [SerializeField] private bool startBossMusicOnInteract = true;

    [Header("Normal Music (resume after boss defeated)")]
    [SerializeField] private AudioClip normalMusicOverride;
    [Range(0f, 1f)] [SerializeField] private float normalMusicVolume = 0.7f;
    [SerializeField] private bool normalMusicLoop = true;
    [SerializeField] private float resumeDelay = 0.5f;

    private bool bossDefeated;
    private Coroutine resumeRoutine;
    private bool playerInside;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;

        if (audioManager == null)
            audioManager = Object.FindFirstObjectByType<NinjaAudioManager>();

        if (bossAI == null)
            bossAI = Object.FindFirstObjectByType<BossAI2D>();
    }

    private void Awake()
    {
        if (audioManager == null)
            audioManager = Object.FindFirstObjectByType<NinjaAudioManager>();

        if (bossAI == null)
            bossAI = Object.FindFirstObjectByType<BossAI2D>();
    }

    private void OnEnable()
    {
        if (bossAI != null)
            bossAI.OnStateChanged += HandleBossStateChanged;
    }

    private void OnDisable()
    {
        if (bossAI != null)
            bossAI.OnStateChanged -= HandleBossStateChanged;

        if (resumeRoutine != null)
        {
            StopCoroutine(resumeRoutine);
            resumeRoutine = null;
        }
    }

    private void Start()
    {
        if (bossAI != null && bossAI.CurrentState == BossAI2D.BossState.Dead)
            bossDefeated = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerInputHandler inputHandler = other.GetComponentInParent<PlayerInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.SetRoomCombatAttackEnabled(true);
            inputHandler.SetCurrentInteractable(this);
            Debug.Log("Player entered combat room", this);
        }

        playerInside = true;

        if (!startBossMusicOnInteract)
            StartBossMusic();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerInputHandler inputHandler = other.GetComponentInParent<PlayerInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.SetRoomCombatAttackEnabled(false);
            inputHandler.ClearCurrentInteractable(this);
            Debug.Log("Player exited combat room", this);
        }

        playerInside = false;

        if (audioManager != null)
            audioManager.StopBackgroundMusic();

        if (bossDefeated)
            StartResumeNormalMusic();
    }

    private void HandleBossStateChanged(BossAI2D.BossState newState)
    {
        if (newState != BossAI2D.BossState.Dead)
            return;

        bossDefeated = true;

        if (audioManager != null)
            audioManager.StopBackgroundMusic();

        StartResumeNormalMusic();
    }

    private void StartResumeNormalMusic()
    {
        if (audioManager == null)
            return;

        if (resumeRoutine != null)
        {
            StopCoroutine(resumeRoutine);
            resumeRoutine = null;
        }

        resumeRoutine = StartCoroutine(ResumeNormalMusicAfterDelay());
    }

    private IEnumerator ResumeNormalMusicAfterDelay()
    {
        if (resumeDelay > 0f)
            yield return new WaitForSeconds(resumeDelay);

        AudioClip clipToPlay = normalMusicOverride != null ? normalMusicOverride : audioManager.backgroundMusic;
        if (clipToPlay != null)
            audioManager.PlayBackgroundMusic(clipToPlay, normalMusicVolume, normalMusicLoop);

        resumeRoutine = null;
    }

    public void Interact()
    {
        if (!startBossMusicOnInteract)
            return;

        if (!playerInside || bossDefeated)
            return;

        StartBossMusic();
    }

    public void StartBossMusicFromDialogue()
    {
        StartBossMusic();
    }

    private void StartBossMusic()
    {
        if (audioManager == null || bossDefeated)
            return;

        if (resumeRoutine != null)
        {
            StopCoroutine(resumeRoutine);
            resumeRoutine = null;
        }

        audioManager.StopBackgroundMusic();

        if (bossMusic != null)
            audioManager.PlayBackgroundMusic(bossMusic, bossMusicVolume, bossMusicLoop);
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossDeathSceneTransition2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossAI2D bossAI;

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "NextScene";
    [SerializeField] private float delayBeforeLoad = 3f;

    [Header("Options")]
    [SerializeField] private bool useRealtimeDelay = true;
    [SerializeField] private bool debugLogs = true;

    private bool loadingStarted;

    private void Reset()
    {
        if (bossAI == null)
            bossAI = Object.FindFirstObjectByType<BossAI2D>();
    }

    private void Awake()
    {
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
    }

    private void Start()
    {
        if (bossAI == null)
        {
            Debug.LogWarning("BossDeathSceneTransition2D: No BossAI2D assigned/found.", this);
            return;
        }

        if (bossAI.CurrentState == BossAI2D.BossState.Dead)
            StartSceneLoad();
    }

    private void HandleBossStateChanged(BossAI2D.BossState newState)
    {
        if (newState == BossAI2D.BossState.Dead)
            StartSceneLoad();
    }

    private void StartSceneLoad()
    {
        if (loadingStarted)
            return;

        loadingStarted = true;
        StartCoroutine(LoadNextSceneRoutine());
    }

    private IEnumerator LoadNextSceneRoutine()
    {
        if (debugLogs)
            Debug.Log("[BossDeathSceneTransition2D] Boss died. Loading next scene in " + delayBeforeLoad + " seconds.", this);

        if (useRealtimeDelay)
            yield return new WaitForSecondsRealtime(delayBeforeLoad);
        else
            yield return new WaitForSeconds(delayBeforeLoad);

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("BossDeathSceneTransition2D: nextSceneName is empty.", this);
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
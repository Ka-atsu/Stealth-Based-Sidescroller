using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    public string CurrentSceneName { get; private set; }
    public int CurrentSceneBuildIndex { get; private set; }
    public bool IsLoadingScene { get; private set; }

    private const string ScrollReadCountKey = "ScrollReadCount";
    private const string ScoreKey = "PlayerScore";
    private const string StealthKillCountKey = "StealthKillCount";

    private HashSet<string> readScrollIDs = new HashSet<string>();

    public int ReadScrollCount => PlayerPrefs.GetInt(ScrollReadCountKey, 0);
    public int CurrentScore => PlayerPrefs.GetInt(ScoreKey, 0);
    public int StealthKillCount { get; private set; }

    // TRUE after player reads a new scroll, consumed after a valid stealth kill reward
    public bool HasUnreadScrollRewardTrigger { get; private set; }

    [Header("Score Settings")]
    [SerializeField] private int stealthKillAfterScrollScore = 100;

    [Header("Ending Screen Multipliers")]
    [SerializeField] private int enemyKillMultiplier = 100;
    [SerializeField] private int scrollReadMultiplier = 50;

    public int EnemyKillMultiplier => enemyKillMultiplier;
    public int ScrollReadMultiplier => scrollReadMultiplier;

    private List<CollectedScrollData> collectedScrolls = new List<CollectedScrollData>();
    public IReadOnlyList<CollectedScrollData> CollectedScrolls => collectedScrolls;

    private void Awake()
    {
        //PlayerPrefs.DeleteAll();
        //PlayerPrefs.Save();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateCurrentScene(SceneManager.GetActiveScene());

        LoadProgress();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateCurrentScene(scene);
        IsLoadingScene = false;
    }

    private void UpdateCurrentScene(Scene scene)
    {
        CurrentSceneName = scene.name;
        CurrentSceneBuildIndex = scene.buildIndex;
        Debug.Log("Current Scene: " + CurrentSceneName);
    }

    public void LoadScene(string sceneName)
    {
        if (IsLoadingScene) return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Target scene name is empty.");
            return;
        }

        if (!SceneExistsInBuildSettings(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' is not in Build Settings.");
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    public void ReloadCurrentScene()
    {
        if (IsLoadingScene) return;

        if (string.IsNullOrWhiteSpace(CurrentSceneName))
        {
            Debug.LogWarning("Current scene name is empty.");
            return;
        }

        StartCoroutine(LoadSceneRoutine(CurrentSceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        IsLoadingScene = true;

        yield return null;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);

        if (loadOperation == null)
        {
            Debug.LogError($"Failed to load scene '{sceneName}'.");
            IsLoadingScene = false;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }

    private bool SceneExistsInBuildSettings(string sceneName)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;

        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneFileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (sceneFileName == sceneName)
            {
                return true;
            }
        }

        return false;
    }

    // =========================
    // Scroll Save System
    // =========================

    public bool HasReadScroll(string scrollID)
    {
        if (string.IsNullOrWhiteSpace(scrollID)) return false;
        return readScrollIDs.Contains(scrollID);
    }

    public void RegisterReadScroll(string scrollID)
    {
        if (string.IsNullOrWhiteSpace(scrollID))
        {
            Debug.LogWarning("Scroll ID is empty.");
            return;
        }

        if (readScrollIDs.Contains(scrollID))
            return;

        readScrollIDs.Add(scrollID);

        int newCount = ReadScrollCount + 1;
        PlayerPrefs.SetInt(ScrollReadCountKey, newCount);
        PlayerPrefs.SetInt(GetScrollKey(scrollID), 1);
        PlayerPrefs.Save();

        // Reading a NEW scroll arms the reward for the next successful stealth kill
        HasUnreadScrollRewardTrigger = true;

        Debug.Log($"Registered scroll: {scrollID} | Total Read: {newCount}");
    }

    public void RegisterCollectedScroll(string scrollID, string scrollTitle, string storyText)
    {
        if (string.IsNullOrWhiteSpace(scrollID))
        {
            Debug.LogWarning("Scroll ID is empty.");
            return;
        }

        bool alreadyExists = false;

        for (int i = 0; i < collectedScrolls.Count; i++)
        {
            if (collectedScrolls[i].id == scrollID)
            {
                alreadyExists = true;
                break;
            }
        }

        if (!alreadyExists)
        {
            collectedScrolls.Add(new CollectedScrollData(scrollID, scrollTitle, storyText));
            Debug.Log("Added scroll to journal: " + scrollTitle);
        }

        RegisterReadScroll(scrollID);
    }

    // =========================
    // Score / Kill System
    // =========================

    public void RegisterSuccessfulStealthKill()
    {
        StealthKillCount++;
        PlayerPrefs.SetInt(StealthKillCountKey, StealthKillCount);
        PlayerPrefs.Save();

        Debug.Log("Successful stealth kill registered. Total: " + StealthKillCount);

        if (!HasUnreadScrollRewardTrigger)
        {
            Debug.Log("No scroll reward trigger active. No score added.");
            return;
        }

        AddScore(stealthKillAfterScrollScore);
        HasUnreadScrollRewardTrigger = false;

        Debug.Log($"Stealth kill after reading scroll! +{stealthKillAfterScrollScore} score");
    }

    public void AddScore(int amount)
    {
        if (amount <= 0) return;

        int newScore = CurrentScore + amount;
        PlayerPrefs.SetInt(ScoreKey, newScore);
        PlayerPrefs.Save();

        Debug.Log("Score updated: " + newScore);
    }

    public void ResetScore()
    {
        PlayerPrefs.SetInt(ScoreKey, 0);
        StealthKillCount = 0;
        PlayerPrefs.SetInt(StealthKillCountKey, 0);
        PlayerPrefs.Save();

        HasUnreadScrollRewardTrigger = false;

        Debug.Log("Score reset.");
    }

    private void LoadProgress()
    {
        readScrollIDs.Clear();
        HasUnreadScrollRewardTrigger = false;

        StealthKillCount = PlayerPrefs.GetInt(StealthKillCountKey, 0);

        Debug.Log("Scroll progress loaded. Current count: " + ReadScrollCount);
        Debug.Log("Current score: " + CurrentScore);
        Debug.Log("Stealth kill count: " + StealthKillCount);
    }

    private string GetScrollKey(string scrollID)
    {
        return "ScrollRead_" + scrollID;
    }

    public bool GetSavedScrollState(string scrollID)
    {
        if (string.IsNullOrWhiteSpace(scrollID)) return false;

        bool isRead = PlayerPrefs.GetInt(GetScrollKey(scrollID), 0) == 1;

        if (isRead)
            readScrollIDs.Add(scrollID);

        return isRead;
    }

    public void ResetScrollProgress()
    {
        PlayerPrefs.SetInt(ScrollReadCountKey, 0);
        readScrollIDs.Clear();
        HasUnreadScrollRewardTrigger = false;
        PlayerPrefs.Save();

        Debug.Log("All scroll progress reset.");
    }

    public void ResetAllProgress()
    {
        PlayerPrefs.SetInt(ScrollReadCountKey, 0);
        PlayerPrefs.SetInt(ScoreKey, 0);
        PlayerPrefs.SetInt(StealthKillCountKey, 0);
        PlayerPrefs.Save();

        readScrollIDs.Clear();
        collectedScrolls.Clear();
        HasUnreadScrollRewardTrigger = false;
        StealthKillCount = 0;

        Debug.Log("All game progress reset.");
    }
}
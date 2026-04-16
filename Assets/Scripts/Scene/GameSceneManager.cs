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
    private HashSet<string> readScrollIDs = new HashSet<string>();

    public int ReadScrollCount => PlayerPrefs.GetInt(ScrollReadCountKey, 0);

    private List<CollectedScrollData> collectedScrolls = new List<CollectedScrollData>();

    public IReadOnlyList<CollectedScrollData> CollectedScrolls => collectedScrolls;

    private void Awake()
    {
        // Clear all PlayerPrefs for testing purposes
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateCurrentScene(SceneManager.GetActiveScene());

        LoadScrollProgress();
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

    private void LoadScrollProgress()
    {
        readScrollIDs.Clear();

        // Optional rebuild from PlayerPrefs is not necessary if we check keys directly,
        // but keeping HashSet useful for runtime fast checks.
        Debug.Log("Scroll progress loaded. Current count: " + ReadScrollCount);
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
        PlayerPrefs.Save();

        Debug.Log("All scroll progress reset.");
    }
}
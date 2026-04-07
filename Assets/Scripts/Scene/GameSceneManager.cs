using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    public string CurrentSceneName { get; private set; }
    public int CurrentSceneBuildIndex { get; private set; }
    public bool IsLoadingScene { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateCurrentScene(SceneManager.GetActiveScene());
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
}
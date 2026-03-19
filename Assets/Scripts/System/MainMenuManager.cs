using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Transition")]
    public float sceneLoadDelay = 0.15f;

    [Header("Audio Integration")]
    public MainMenuSoundManager soundManager;
    public bool autoWireButtonClickSound = true;

    bool isLoading;

    void Awake()
    {
        ResolveSoundManager();

        if (autoWireButtonClickSound)
        {
            WireAllButtonClickSounds();
            WireAllButtonHoverSounds();
        }
    }

    public void StartGame()
    {
        LoadSceneFromMenu("Intro");
    }

    public void ContinueGame()
    {
        LoadSceneFromMenu("Level1");
    }

    public void HowToPlay()
    {
        LoadSceneFromMenu("HowToPlay");
    }

    public void Settings()
    {
        LoadSceneFromMenu("Settings");
    }

    public void ExitGame()
    {
        if (isLoading)
            return;

        isLoading = true;

        if (soundManager != null)
        {
            if (!autoWireButtonClickSound)
            {
                soundManager.PlayButtonClick();
            }

            StartCoroutine(ExitWithDelay());
        }
        else
        {
            ExitNow();
        }
    }

    public void PlayHoverSound()
    {
        if (soundManager != null)
        {
            soundManager.PlayButtonHover();
        }
    }

    public void PlayClickSoundOnly()
    {
        if (soundManager != null)
        {
            soundManager.PlayButtonClick();
        }
    }

    void LoadSceneFromMenu(string sceneName)
    {
        if (isLoading)
            return;

        isLoading = true;

        if (soundManager != null)
        {
            if (!autoWireButtonClickSound)
            {
                soundManager.PlayButtonClick();
            }

            StartCoroutine(LoadSceneWithDelay(sceneName));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    void ResolveSoundManager()
    {
        if (soundManager != null)
            return;

        if (MainMenuSoundManager.Instance != null)
        {
            soundManager = MainMenuSoundManager.Instance;
            return;
        }

        soundManager = FindFirstObjectByType<MainMenuSoundManager>();
    }

    void WireAllButtonClickSounds()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            button.onClick.AddListener(PlayClickSoundOnly);
        }
    }

    void WireAllButtonHoverSounds()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = button.gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry();
            pointerEnterEntry.eventID = EventTriggerType.PointerEnter;
            pointerEnterEntry.callback.AddListener((data) => PlayHoverSound());
            eventTrigger.triggers.Add(pointerEnterEntry);
        }
    }

    IEnumerator LoadSceneWithDelay(string sceneName)
    {
        yield return new WaitForSeconds(sceneLoadDelay);
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator ExitWithDelay()
    {
        yield return new WaitForSeconds(sceneLoadDelay);
        ExitNow();
    }

    void ExitNow()
    {
        Application.Quit();
        Debug.Log("Game Closed");
    }

}
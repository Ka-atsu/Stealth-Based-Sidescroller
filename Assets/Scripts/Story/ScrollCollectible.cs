using UnityEngine;
using UnityEngine.InputSystem;

public class ScrollCollectible : MonoBehaviour
{
    [Header("Scroll ID")]
    public string scrollID = "scroll_01";

    [Header("Story Data")]
    public string scrollTitle = "Old Scroll";

    [TextArea(5, 12)]
    public string storyText = "This is the hidden story written on the scroll.";

    [Header("Settings")]
    public bool destroyAfterReading = false;

    private bool playerInRange;
    private bool hasBeenRead;

    void Start()
    {
        if (GameSceneManager.Instance != null)
        {
            hasBeenRead = GameSceneManager.Instance.GetSavedScrollState(scrollID);

            if (hasBeenRead && destroyAfterReading)
            {
                Destroy(gameObject);
            }
        }
    }

    void Update()
    {
        if (!playerInRange) return;
        if (StoryUIManager.Instance == null) return;
        if (StoryUIManager.Instance.IsOpen) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            StoryUIManager.Instance.OpenStory(scrollTitle, storyText);

            if (!hasBeenRead)
            {
                hasBeenRead = true;

                if (GameSceneManager.Instance != null)
                {
                    GameSceneManager.Instance.RegisterReadScroll(scrollID);
                }
                else
                {
                    Debug.LogWarning("GameSceneManager.Instance is NULL");
                }
            }

            if (InteractPromptUI.Instance != null)
                InteractPromptUI.Instance.Hide();

            Debug.Log("Read scroll: " + scrollTitle);

            if (destroyAfterReading)
                Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Show("Press E to read", other.transform);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Hide();
    }
}
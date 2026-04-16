using System.Collections;
using TMPro;
using UnityEngine;

public class StoryUIManager : MonoBehaviour
{
    public static StoryUIManager Instance;

    [Header("UI References")]
    public GameObject storyPanel;
    public TMP_Text titleText;
    public TMP_Text bodyText;

    [Header("Animation Settings")]
    public float openDuration = 0.25f;
    public float closeDuration = 0.2f;

    [Tooltip("Starting scale when closed. Small Y makes it feel like a rolled scroll.")]
    public Vector3 closedScale = new Vector3(1f, 0.05f, 1f);

    [Tooltip("Final scale when fully open.")]
    public Vector3 openScale = Vector3.one;

    private CanvasGroup canvasGroup;
    private RectTransform storyRect;
    private Coroutine currentRoutine;

    private bool isOpen;
    private bool isAnimating;

    public bool IsOpen => isOpen || isAnimating;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (storyPanel == null)
        {
            Debug.LogError("StoryUIManager: storyPanel is not assigned.");
            return;
        }

        canvasGroup = storyPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = storyPanel.AddComponent<CanvasGroup>();

        storyRect = storyPanel.GetComponent<RectTransform>();

        // Start hidden but active
        storyPanel.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (storyRect != null)
            storyRect.localScale = closedScale;

        isOpen = false;
        isAnimating = false;
    }

    public void OpenStory(string title, string body)
    {
        if (titleText == null || bodyText == null)
        {
            Debug.LogError("StoryUIManager: titleText or bodyText is not assigned.");
            return;
        }

        titleText.text = title;
        bodyText.text = body;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(OpenRoutine());
    }

    public void CloseStory()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(CloseRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        isAnimating = true;
        isOpen = false;

        storyPanel.SetActive(true);
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float time = 0f;

        while (time < openDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / openDuration);

            // Smooth open with a slight pop
            float eased = EaseOutBack(t);

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            if (storyRect != null)
                storyRect.localScale = Vector3.LerpUnclamped(closedScale, openScale, eased);

            yield return null;
        }

        canvasGroup.alpha = 1f;

        if (storyRect != null)
            storyRect.localScale = openScale;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        isAnimating = false;
        isOpen = true;
    }

    private IEnumerator CloseRoutine()
    {
        isAnimating = true;
        isOpen = false;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float time = 0f;
        Vector3 startScale = storyRect != null ? storyRect.localScale : openScale;

        while (time < closeDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / closeDuration);

            float eased = EaseInBack(t);

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            if (storyRect != null)
                storyRect.localScale = Vector3.LerpUnclamped(startScale, closedScale, eased);

            yield return null;
        }

        canvasGroup.alpha = 0f;

        if (storyRect != null)
            storyRect.localScale = closedScale;

        isAnimating = false;
        isOpen = false;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private float EaseInBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }
}
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScrollJournalUI : MonoBehaviour
{
    public static ScrollJournalUI Instance;

    [Header("UI")]
    public GameObject journalPanel;
    public Transform scrollListParent;
    public GameObject scrollButtonPrefab;
    public TMP_Text selectedScrollTitle;
    public TMP_Text selectedScrollBody;

    [Header("Animation")]
    public float openDuration = 0.25f;
    public float closeDuration = 0.2f;
    public Vector3 closedScale = new Vector3(0.9f, 0.9f, 1f);
    public Vector3 openScale = Vector3.one;
    public Vector2 closedOffset = new Vector2(0f, -30f);

    private CanvasGroup canvasGroup;
    private RectTransform journalRect;
    private Vector2 openAnchoredPosition;
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

        if (journalPanel == null) return;

        canvasGroup = journalPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = journalPanel.AddComponent<CanvasGroup>();

        journalRect = journalPanel.GetComponent<RectTransform>();

        if (journalRect != null)
            openAnchoredPosition = journalRect.anchoredPosition;

        journalPanel.SetActive(true);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (journalRect != null)
        {
            journalRect.localScale = closedScale;
            journalRect.anchoredPosition = openAnchoredPosition + closedOffset;
        }

        isOpen = false;
        isAnimating = false;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            ToggleJournal();
        }
    }

    public void ToggleJournal()
    {
        if (journalPanel == null) return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        if (isOpen || isAnimating)
            currentRoutine = StartCoroutine(CloseRoutine());
        else
            currentRoutine = StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        isAnimating = true;
        isOpen = false;

        RefreshList();
        ShowDefaultMessage();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float time = 0f;

        while (time < openDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / openDuration);
            float eased = EaseOutBack(t);

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            if (journalRect != null)
            {
                journalRect.localScale = Vector3.LerpUnclamped(closedScale, openScale, eased);
                journalRect.anchoredPosition = Vector2.Lerp(openAnchoredPosition + closedOffset, openAnchoredPosition, t);
            }

            yield return null;
        }

        canvasGroup.alpha = 1f;

        if (journalRect != null)
        {
            journalRect.localScale = openScale;
            journalRect.anchoredPosition = openAnchoredPosition;
        }

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
        Vector3 startScale = journalRect != null ? journalRect.localScale : openScale;
        Vector2 startPos = journalRect != null ? journalRect.anchoredPosition : openAnchoredPosition;

        while (time < closeDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / closeDuration);

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            if (journalRect != null)
            {
                journalRect.localScale = Vector3.Lerp(startScale, closedScale, t);
                journalRect.anchoredPosition = Vector2.Lerp(startPos, openAnchoredPosition + closedOffset, t);
            }

            yield return null;
        }

        canvasGroup.alpha = 0f;

        if (journalRect != null)
        {
            journalRect.localScale = closedScale;
            journalRect.anchoredPosition = openAnchoredPosition + closedOffset;
        }

        isAnimating = false;
        isOpen = false;
    }

    private void ShowDefaultMessage()
    {
        if (selectedScrollTitle != null)
            selectedScrollTitle.text = "Select a scroll";

        if (selectedScrollBody != null)
            selectedScrollBody.text = "Its story will appear here.";
    }

    public void RefreshList()
    {
        if (scrollListParent == null || scrollButtonPrefab == null) return;
        if (GameSceneManager.Instance == null) return;

        for (int i = scrollListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(scrollListParent.GetChild(i).gameObject);
        }

        var collected = GameSceneManager.Instance.CollectedScrolls;

        for (int i = 0; i < collected.Count; i++)
        {
            var data = collected[i];

            GameObject buttonObj = Instantiate(scrollButtonPrefab, scrollListParent);

            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
                buttonText.text = data.title;

            UnityEngine.UI.Button button = buttonObj.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                var capturedData = data;
                button.onClick.AddListener(() => ShowScroll(capturedData));
            }
        }
    }

    public void ShowScroll(CollectedScrollData data)
    {
        if (selectedScrollTitle != null)
            selectedScrollTitle.text = data.title;

        if (selectedScrollBody != null)
            selectedScrollBody.text = data.body;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
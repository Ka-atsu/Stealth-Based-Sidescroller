using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ScrollJournalUI : MonoBehaviour
{
    public static ScrollJournalUI Instance;

    [Header("UI")]
    public GameObject journalPanel;
    public Transform scrollListParent;
    public GameObject scrollButtonPrefab;
    public TMP_Text selectedScrollTitle;
    public TMP_Text selectedScrollBody;

    private bool isOpen;

    public bool IsOpen => isOpen;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (journalPanel != null)
            journalPanel.SetActive(false);
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

        isOpen = !isOpen;
        journalPanel.SetActive(isOpen);

        if (isOpen)
        {
            RefreshList();
            ShowDefaultMessage();
        }
    }

    void ShowDefaultMessage()
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

        IReadOnlyList<CollectedScrollData> collected = GameSceneManager.Instance.CollectedScrolls;

        for (int i = 0; i < collected.Count; i++)
        {
            CollectedScrollData data = collected[i];

            GameObject buttonObj = Instantiate(scrollButtonPrefab, scrollListParent);

            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
                buttonText.text = data.title;

            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                CollectedScrollData capturedData = data;
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
}
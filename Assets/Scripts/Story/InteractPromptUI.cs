using TMPro;
using UnityEngine;

public class InteractPromptUI : MonoBehaviour
{
    public static InteractPromptUI Instance;

    [Header("UI")]
    public GameObject promptRoot;
    public TMP_Text promptText;

    [Header("Follow Target")]
    public Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    Transform target;
    Camera cam;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        cam = Camera.main;
        Hide();
    }

    void LateUpdate()
    {
        if (promptRoot == null || target == null || cam == null) return;

        Vector3 worldPos = target.position + worldOffset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        promptRoot.transform.position = screenPos;
    }

    public void Show(string message, Transform followTarget)
    {
        target = followTarget;

        if (promptRoot != null)
            promptRoot.SetActive(true);

        if (promptText != null)
            promptText.text = message;
    }

    public void Hide()
    {
        target = null;

        if (promptRoot != null)
            promptRoot.SetActive(false);
    }
}
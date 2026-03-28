using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SceneLoadTrigger : MonoBehaviour
{
    [Header("Scene To Load")]
    [SerializeField] private string targetSceneName;

    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool disableAfterTrigger = true;

    private bool hasTriggered;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered && triggerOnce) return;
        if (!other.CompareTag(playerTag)) return;

        if (GameSceneManager.Instance == null)
        {
            Debug.LogWarning("GameSceneManager not found in scene.");
            return;
        }

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("No target scene assigned on " + gameObject.name);
            return;
        }

        hasTriggered = true;

        if (disableAfterTrigger)
            gameObject.SetActive(false);

        GameSceneManager.Instance.LoadScene(targetSceneName);
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class SceneTeleporter : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private GameObject interactUI;

    private bool playerInRange;
    private bool requestedLoad;
    private Collider2D triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        SetInteractUI(false);
    }

    private void Update()
    {
        if (requestedLoad) return;
        if (!playerInRange) return;
        if (Keyboard.current == null) return;
        if (GameSceneManager.Instance == null) return;
        if (GameSceneManager.Instance.IsLoadingScene) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            requestedLoad = true;
            playerInRange = false;

            SetInteractUI(false);

            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }

            GameSceneManager.Instance.LoadScene(sceneToLoad);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (requestedLoad) return;
        if (!other.CompareTag("Player")) return;
        if (GameSceneManager.Instance != null && GameSceneManager.Instance.IsLoadingScene) return;

        playerInRange = true;
        SetInteractUI(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (requestedLoad) return;
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        SetInteractUI(false);
    }

    private void OnDisable()
    {
        playerInRange = false;
        SetInteractUI(false);
    }

    private void SetInteractUI(bool value)
    {
        if (interactUI != null)
        {
            interactUI.SetActive(value);
        }
    }
}
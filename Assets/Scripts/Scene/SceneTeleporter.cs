using UnityEngine;
using UnityEngine.InputSystem;

public class SceneTeleporter : MonoBehaviour
{
    public string sceneToLoad;
    public GameObject interactUI;

    private bool playerInRange = false;

    void Start()
    {
        interactUI.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && Keyboard.current.eKey.wasPressedThisFrame)
        {
            GameSceneManager.Instance.LoadScene(sceneToLoad);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            interactUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            interactUI.SetActive(false);
        }
    }
}
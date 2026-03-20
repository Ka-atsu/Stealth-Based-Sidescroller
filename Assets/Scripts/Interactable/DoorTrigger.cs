using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private DoorInteract door;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerInputHandler inputHandler = other.GetComponent<PlayerInputHandler>();
        if (inputHandler != null)
            inputHandler.SetCurrentInteractable(door);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerInputHandler inputHandler = other.GetComponent<PlayerInputHandler>();
        if (inputHandler != null)
            inputHandler.ClearCurrentInteractable(door);
    }
}
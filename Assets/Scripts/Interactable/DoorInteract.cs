using UnityEngine;

public class DoorInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private Collider2D doorBlockCollider;
    [SerializeField] private SpriteRenderer doorSprite;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    private bool isOpen;

    public void Interact()
    {
        if (isOpen)
            CloseDoor();
        else
            OpenDoor();
    }

    private void OpenDoor()
    {
        isOpen = true;

        if (doorBlockCollider != null)
            doorBlockCollider.enabled = false;

        if (doorSprite != null && openSprite != null)
            doorSprite.sprite = openSprite;
    }

    private void CloseDoor()
    {
        isOpen = false;

        if (doorBlockCollider != null)
            doorBlockCollider.enabled = true;

        if (doorSprite != null && closedSprite != null)
            doorSprite.sprite = closedSprite;
    }
}
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomCombatAttackZone : MonoBehaviour
{
    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInputHandler inputHandler = other.GetComponentInParent<PlayerInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.SetRoomCombatAttackEnabled(true);
            Debug.Log("Player entered combat room", this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerInputHandler inputHandler = other.GetComponentInParent<PlayerInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.SetRoomCombatAttackEnabled(false);
            Debug.Log("Player exited combat room", this);
        }
    }
}
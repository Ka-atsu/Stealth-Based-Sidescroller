using UnityEngine;

public class BushHideZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.isHidden = true;

                // Change layer so enemy can't collide
                other.gameObject.layer = LayerMask.NameToLayer("PlayerHidden");

                Debug.Log("Player is hiding");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.isHidden = false;

                // Restore normal collision
                other.gameObject.layer = LayerMask.NameToLayer("Player");

                Debug.Log("Player left bush");
            }
        }
    }
}
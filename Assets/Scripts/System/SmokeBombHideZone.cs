using UnityEngine;

public class SmokeBombHideZone : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] private string hiddenLayerName = "PlayerHidden";
    [SerializeField] private string normalLayerName = "Player";

    private int hiddenLayer;
    private int normalLayer;

    [Header("Smoke Bomb Settings")]
    [SerializeField] private float smokeDuration = 5f; // How long the smoke lasts

    private void Awake()
    {
        hiddenLayer = LayerMask.NameToLayer(hiddenLayerName);
        normalLayer = LayerMask.NameToLayer(normalLayerName);

        if (hiddenLayer == -1) Debug.LogError($"Layer '{hiddenLayerName}' does not exist!");
        if (normalLayer == -1) Debug.LogError($"Layer '{normalLayerName}' does not exist!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // In modular setups, the collider might be on a child, so use InParent
        var noise = other.GetComponentInParent<PlayerNoiseEmitter2D>();
        var controller = other.GetComponentInParent<PlayerController2D>();

        if (controller == null) return; // not the player

        // Hide the player and change the layer
        if (noise != null)
        {
            noise.isHidden = true; // Player cannot emit noise in the smoke
        }

        if (hiddenLayer != -1)
        {
            controller.gameObject.layer = hiddenLayer; // Set hidden layer
        }

        // Start smoke timer to withdraw the effect after the duration
        StartCoroutine(SmokeBombTimer(controller, noise));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var noise = other.GetComponentInParent<PlayerNoiseEmitter2D>();
        var controller = other.GetComponentInParent<PlayerController2D>();

        if (controller == null) return; // not the player

        // Restore the player layer and visibility
        if (noise != null)
        {
            noise.isHidden = false; // Player can emit noise again
        }

        if (normalLayer != -1)
        {
            controller.gameObject.layer = normalLayer; // Restore normal layer
        }
    }

    private System.Collections.IEnumerator SmokeBombTimer(PlayerController2D controller, PlayerNoiseEmitter2D noise)
    {
        // Wait for the smoke duration before restoring the player
        yield return new WaitForSeconds(smokeDuration);

        // Restore player once the timer is done
        RestorePlayer(controller, noise);
    }

    private void RestorePlayer(PlayerController2D controller, PlayerNoiseEmitter2D noise)
    {
        // Allow player to emit noise again and restore the normal layer
        if (noise != null)
        {
            noise.isHidden = false;
        }

        if (normalLayer != -1)
        {
            controller.gameObject.layer = normalLayer; // Restore normal layer
        }
    }
}
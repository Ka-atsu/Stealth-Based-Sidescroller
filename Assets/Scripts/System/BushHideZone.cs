using UnityEngine;

public class BushHideZone : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] private string hiddenLayerName = "PlayerHidden";
    [SerializeField] private string normalLayerName = "Player";

    private int hiddenLayer;
    private int normalLayer;

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

        if (noise != null)
            noise.isHidden = true;

        if (hiddenLayer != -1)
            controller.gameObject.layer = hiddenLayer;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var noise = other.GetComponentInParent<PlayerNoiseEmitter2D>();
        var controller = other.GetComponentInParent<PlayerController2D>();

        if (controller == null) return;

        if (noise != null)
            noise.isHidden = false;

        if (normalLayer != -1)
            controller.gameObject.layer = normalLayer;
    }
}
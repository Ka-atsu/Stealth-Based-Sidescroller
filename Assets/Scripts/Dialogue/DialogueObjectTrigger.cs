using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DialogueObjectTrigger : MonoBehaviour
{
    [SerializeField] private DialogueTrigger dialogueTrigger;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce = true;

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
        if (dialogueTrigger == null) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive) return;

        hasTriggered = true;
        dialogueTrigger.TriggerDialogue();
    }
}
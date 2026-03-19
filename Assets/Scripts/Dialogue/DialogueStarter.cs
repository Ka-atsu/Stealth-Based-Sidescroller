using UnityEngine;

public class DialogueStarter : MonoBehaviour
{
    public Dialogue dialogueToStart;

    public void StartDialogueFromSignal()
    {
        if (dialogueToStart == null)
        {
            Debug.LogWarning("No dialogue assigned.");
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("No DialogueManager found in scene.");
            return;
        }

        DialogueManager.Instance.StartDialogue(dialogueToStart);
    }
}
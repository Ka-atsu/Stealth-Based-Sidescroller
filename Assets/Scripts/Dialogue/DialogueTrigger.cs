using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class DialogueCharacter
{
    public string name;
    public Sprite icon;
}

[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;
    [TextArea(3, 10)]
    public string line;
}

[System.Serializable]
public class Dialogue
{
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
}

public class DialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue;
    [Header("Optional Boss Music")]
    [SerializeField] private RoomCombatAttackZone bossMusicZone;
    [SerializeField] private bool startBossMusicAfterDialogue = true;

    private bool waitingForDialogueEnd;

    public void TriggerDialogue()
    {
        if (DialogueManager.Instance == null)
            return;

        if (startBossMusicAfterDialogue && bossMusicZone != null)
        {
            waitingForDialogueEnd = true;
            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
        }

        DialogueManager.Instance.StartDialogue(dialogue);
    }

    private void HandleDialogueEnded(Dialogue endedDialogue)
    {
        if (!waitingForDialogueEnd)
            return;

        if (endedDialogue != dialogue)
            return;

        waitingForDialogueEnd = false;

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;

        if (bossMusicZone != null)
            bossMusicZone.StartBossMusicFromDialogue();
    }
}

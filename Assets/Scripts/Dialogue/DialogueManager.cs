using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    public Image characterIcon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueArea;
    public TextMeshProUGUI buttonName;

    private Queue<DialogueLine> lines;

    public bool isDialogueActive = false;
    public float textSpeed;
    public Animator animator;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;

        lines = new Queue<DialogueLine>();
    }

    public void StartDialogue(Dialogue dialogue)
    {
        isDialogueActive = true;
        animator.SetBool("IsOpen", isDialogueActive);
        lines.Clear();

        foreach(DialogueLine dialogueLine in dialogue.dialogueLines)
        {
            lines.Enqueue(dialogueLine);
        }

        DisplayNextDialogueLine();
    }

    public void DisplayNextDialogueLine()
    {
        if(lines.Count == 0)
        {
            EndDialogue();
            return;
        }

        //Changing the name of the "continue" button to "exit"
        if(lines.Count == 1)
        {
            buttonName.text = "";
        }

        DialogueLine currentLine = lines.Dequeue();

        characterIcon.sprite = currentLine.character.icon;
        characterName.text = currentLine.character.name;

        StopAllCoroutines();

        StartCoroutine(TypeSentence(currentLine));

    }

    public void EndDialogue()
    {
        isDialogueActive = false;
        animator.SetBool("IsOpen", isDialogueActive);
    }

    IEnumerator TypeSentence(DialogueLine dialogueLine)
    {
        dialogueArea.text = "";
        foreach(char letter in dialogueLine.line.ToCharArray())
        {
            dialogueArea.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }
    }
}

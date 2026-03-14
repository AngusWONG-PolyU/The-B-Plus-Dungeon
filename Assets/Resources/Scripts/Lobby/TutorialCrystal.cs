using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialCrystal : MonoBehaviour
{
    [Header("Tutorial Settings")]
    public string crystalName = "Tutorial Crystal";

    [Header("Tutorial Pages Setup")]
    public List<TutorialPage> tutorialPages = new List<TutorialPage>
    {
        new TutorialPage 
        {
            title = "Gameplay Controls",
            content = "<color=#FFD700>Movement:</color>\nClick anywhere on the ground to move.\n\n<color=#FFD700>Skills:</color>\nPress 1, 2, 3, or 4 to select a skill and aim with your mouse.\n\n<size=80%><i>You can press Left/Right Arrow (A/D) or click the Next/Previous button to turn pages, Esc or click the X button to close.</i></size>"
        },
        new TutorialPage 
        {
            title = "Task / B+ Tree Operations",
            content = "<color=#FFD700>Move/Promote:</color> Drag and drop a Key to another Node.\n\n<color=#FFD700>Merge Node:</color> Drag and Drop a Node onto another Node.\n\n<color=#FFD700>Right-Click Options:</color>\n- Right-click a Key to <color=#FF6347>Delete</color>, <color=#32CD32>CopyUp</color>, or <color=#1E90FF>Split Node</color>.\n- Right-click an empty Node to delete it."
        },
        new TutorialPage 
        {
            title = "B+ Tree Rules (1/2)",
            content = "1. <color=#FFD700>Order (Branching Factor):</color>\nA B+ tree of Order M means a node can have at most M children and M-1 keys. It has a minimum requirement of keys (usually ceil(M/2)-1) to stay balanced.\n\n2. <color=#FFD700>Leaf Nodes:</color>\nAll actual data is stored at the bottom (Leaf) level. All leaf nodes MUST be at the exact same depth!"
        },
        new TutorialPage 
        {
            title = "B+ Tree Rules (2/2)",
            content = "3. <color=#FFD700>Redistribute vs Merge:</color>\nWhen a node has too few keys, try borrowing (Redistributing) from a sibling first! Merging nodes might cause a chain reaction (Cascade Effect) that shrinks the tree height, costing more operations."
        }
    };

    private bool isPlayerInRange = false;

    private void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (TutorialUIManager.Instance != null)
            {
                TutorialUIManager.Instance.OpenTutorial(tutorialPages);
                // Hide the prompt once the player opens the UI
                if (PlayerInstructionUI.Instance != null)
                {
                    PlayerInstructionUI.Instance.HideInstruction();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (PlayerInstructionUI.Instance != null)
            {
                PlayerInstructionUI.Instance.ShowInstruction($"Press E to read {crystalName}!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (PlayerInstructionUI.Instance != null)
            {
                PlayerInstructionUI.Instance.HideInstruction();
            }
            if (TutorialUIManager.Instance != null)
            {
                TutorialUIManager.Instance.CloseTutorial();
            }
        }
    }
}

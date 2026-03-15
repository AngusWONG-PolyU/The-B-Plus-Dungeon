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
            title = "HUD & Controls",
            content = "<color=#FFD700>UI Overview:</color>\n• <color=#FF6347>Top Left:</color> Hearts (HP)\n• <color=#1E90FF>Top Right:</color> Minimap (View B+ tree structure)\n• <color=#32CD32>Bottom Right:</color> Skills (Press 1, 2, 3, or 4 to cast)\n• <color=#FFA500>Bottom Left:</color> Settings (Task configs, Time limits, Return to Lobby)\n• <color=#FFD700>Bottom Center:</color> Your current Target!"
        },
        new TutorialPage 
        {
            title = "Dungeon Exploration",
            content = "Find your target by entering the correct <color=#1E90FF>Portals</color>, which display specific routing paths.\n\nInside, you will encounter <color=#FF6347>an Enemy</color> or discover <color=#32CD32>an Item</color>. Clear the room, then go to unlock the door!\n\nEntering the final correct portal leads you to the <color=#FF0000>Boss Room</color>, where a tougher enemy awaits!"
        },
        new TutorialPage 
        {
            title = "Task / Operations",
            content = "<color=#FFD700>Move/Promote:</color> Drag and drop a Key to another Node.\n\n<color=#FFD700>Merge Node:</color> Drag and Drop a Node onto another Node.\n\n<color=#FFD700>Right-Click Options:</color>\n- Right-click a Key to <color=#FF6347>Delete</color>, <color=#32CD32>CopyUp</color>, or <color=#1E90FF>Split Node</color>.\n- Right-click an empty Node to delete it."
        },
        new TutorialPage 
        {
            title = "B+ Tree: Fundamentals",
            content = "<color=#FFD700>Search Routing:</color> Follow the keys! Values <color=#FF6347>smaller</color> than a key go <color=#1E90FF>Left</color>, while <color=#32CD32>greater/equal</color> values go <color=#1E90FF>Right</color>.\n\n<color=#FFD700>Leaf Nodes:</color> All data is stored at the bottom leaves. They MUST all be at the <color=#FFD700>exact same depth</color> for balance!"
        },
        new TutorialPage 
        {
            title = "B+ Tree: Node Limits",
            content = "1. <color=#FFD700>Order (M):</color> A node can have at most <color=#1E90FF>M</color> children and <color=#1E90FF>M-1</color> keys.\n\n2. <color=#FFD700>Min Keys:</color> Nodes (except root) have a strict minimum key limit!\n• Min Keys = <color=#FF6347>ceil(M/2) - 1</color>"
        },
        new TutorialPage
        {
            title = "B+ Tree: Growing",
            content = "When a node exceeds its <color=#FF6347>Max Keys</color>, it must be <color=#32CD32>Split</color>!\n\n<color=#FFD700>Right-Biased Convention:</color> This game adopts a Right-Biased split as its convention, meaning during a node split with an odd number of elements, the extra element <color=#1E90FF>always goes to the right side</color>."
        },
        new TutorialPage
        {
            title = "B+ Tree: Balancing",
            content = "When a node falls below <color=#FF6347>Min Keys</color>:\n1. <color=#32CD32>Redistribute:</color> Borrow from a sibling! (<color=#FFD700>Tie-Breaking:</color> If both can lend a key, follow a convention <color=#1E90FF>such as Right-Biased</color> to borrow from the right).\n2. <color=#FF6347>Merge:</color> Merge only if siblings have no extra keys! This <color=#FFA500>may cause a Cascading Effect</color> that shrinks the tree height."
        },
        new TutorialPage
        {
            title = "B+ Tree: Task Validation",
            content = "Because B+ Tree operations can sometimes have multiple valid outcomes, this game uses <color=#32CD32>Structural Validation</color> to check your answers!\n\nThis means whenever your final tree is valid (doesn't break any rules), it is <color=#1E90FF>Correct</color>. Strict tie-breaking conventions are preferred and taught, but <color=#FFD700>not strictly enforced</color> to pass!"
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

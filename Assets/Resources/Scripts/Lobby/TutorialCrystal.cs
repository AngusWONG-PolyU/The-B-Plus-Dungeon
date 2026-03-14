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
            title = "B+ Tree Rules (1/2)",
            content = "1. <color=#FFD700>Order (M):</color> A node can have at most <color=#1E90FF>M</color> children and <color=#1E90FF>M-1</color> keys.\n\n2. <color=#FFD700>Min Keys:</color> To maintain balance, nodes have a minimum key limit!\n- Minimum Keys = <color=#FF6347>ceil(M/2) - 1</color>"
        },
        new TutorialPage 
        {
            title = "B+ Tree Rules (2/2)",
            content = "3. <color=#FFD700>Leaf Nodes:</color> All data is stored at the bottom. They MUST be at the exact same depth.\n\n4. <color=#FFD700>Balance:</color> When a node falls below the minimum keys, try borrowing (<color=#32CD32>Redistribute</color>) from a sibling first! <color=#FF6347>Merge</color> only when necessary, as it can cause a <color=#1E90FF>Cascading Effect</color> that shrinks the tree."
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

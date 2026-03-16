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
            title = "HUD & Controls (1)",
            content = "<color=#FFD700>UI Overview:</color>\n" +
                      "• <color=#FF6347>Top Left:</color> Hearts (HP)\n" +
                      "• <color=#1E90FF>Top Right:</color> Minimap (View B+ tree structure)\n" +
                      "• <color=#32CD32>Bottom Right:</color> Skills (Press 1, 2, 3, or 4 to cast)\n" +
                      "• <color=#FFA500>Bottom Left:</color> Settings (Task configs, Time limits, Return to Lobby)\n" +
                      "• <color=#FFD700>Bottom Center:</color> Your current Target!"
        },
        new TutorialPage 
        {
            title = "HUD & Controls (2)",
            content = "<color=#FFD700>Controls:</color>\n" +
                      "• <color=#1E90FF>Press E</color> to Interact (Read, Teleport, Unlock).\n" +
                      "• <color=#1E90FF>Press M</color> to toggle full map view in the dungeon.\n" +
                      "• <color=#1E90FF>Press T</color> to toggle this tutorial anywhere.\n" +
                      "• <color=#1E90FF>Press A / D</color> or <color=#1E90FF>Arrows (Left / Right)</color> to change pages.\n" +
                      "• <color=#1E90FF>Press Esc</color> to close."
        },
        new TutorialPage 
        {
            title = "Skills Overview",
            content = "Press 1-4 to cast! <color=#FFD700>(Press again to cancel aiming)</color>\n\n" +
                      "• <color=#1E90FF>[1] Crystal Shards:</color> Directional Attack (1s CD)\n" +
                      "• <color=#1E90FF>[2] Crystal Cross:</color> Positional Attack (1s CD)\n" +
                      "• <color=#32CD32>[3] Healing:</color> Heal 1 HP (Needs healing item charge, 1s CD)\n" +
                      "• <color=#FF6347>[4] Super Slash:</color> Huge AoE directional attack! 1-hits enemies (pierces shields) & breaks doors. (Needs rare item charge, 1 use per run!)"
        },
        new TutorialPage 
        {
            title = "Dungeon Exploration",
            content = "• Find your target by entering the correct <color=#1E90FF>Portals</color>, which display specific routing paths.\n\n" +
                      "• Inside, you will encounter <color=#FF6347>an Enemy</color> or discover <color=#32CD32>an Item</color>. Clear the room, then go to unlock the door!\n\n" +
                      "• Entering the final correct portal leads you to the <color=#FF0000>Boss Room</color>, where a tougher enemy awaits!"
        },
        new TutorialPage 
        {
            title = "Task / Operations",
            content = "• <color=#FFD700>Move/Promote:</color> Drag and drop a Key to another Node.\n\n" +
                      "• <color=#FFD700>Merge Node:</color> Drag and Drop a Node onto another Node.\n\n" +
                      "• <color=#FFD700>Right-Click Options:</color>\n" +
                      "  • Right-click a Key to <color=#FF6347>Delete</color>, <color=#32CD32>CopyUp</color>, or <color=#1E90FF>Split Node</color>.\n" +
                      "  • Right-click an empty Node to delete it."
        },
        new TutorialPage 
        {
            title = "B+ Tree: Fundamentals",
            content = "• <color=#FFD700>Search Routing:</color> Follow the keys! Values <color=#FF6347>smaller</color> than a key go <color=#1E90FF>Left</color>, while <color=#32CD32>greater/equal</color> values go <color=#1E90FF>Right</color>.\n\n" +
                      "• <color=#FFD700>Leaf Nodes:</color> All data is stored at the bottom leaves. They MUST all be at the <color=#FFD700>exact same depth</color> for balance!"
        },
        new TutorialPage 
        {
            title = "B+ Tree: Node Limits",
            content = "• <color=#FFD700>Order (M):</color> A node can have at most <color=#1E90FF>M</color> children and <color=#1E90FF>M-1</color> keys.\n\n" +
                      "• <color=#FFD700>Min Keys:</color> Nodes (except root) have a strict minimum key limit!\n" +
                      "  Min Keys = <color=#FF6347>ceil(M/2) - 1</color>."
        },
        new TutorialPage
        {
            title = "B+ Tree: Growing",
            content = "• When a node exceeds its <color=#FF6347>Max Keys</color>, it must be <color=#32CD32>Split</color>!\n\n" +
                      "• <color=#FFD700>Right-Biased Convention:</color> This game adopts a Right-Biased split as its convention, meaning during a node split with an odd number of elements, the extra element <color=#1E90FF>always goes to the right side</color>."
        },
        new TutorialPage
        {
            title = "B+ Tree: Balancing",
            content = "When a node falls below <color=#FF6347>Min Keys</color>:\n" +
                      "• <color=#32CD32>Redistribute:</color> Borrow from a sibling! (<color=#FFD700>Tie-Breaking:</color> If both can lend a key, follow a convention <color=#1E90FF>such as Right-Biased</color> to borrow from the right).\n" +
                      "• <color=#FF6347>Merge:</color> Merge only if siblings have no extra keys! This <color=#FFA500>may cause a Cascading Effect</color> that shrinks the tree height."
        },
        new TutorialPage
        {
            title = "B+ Tree: Task Validation",
            content = "• Because B+ Tree operations can sometimes have multiple valid outcomes, this game uses <color=#32CD32>Structural Validation</color> to check your answers!\n\n" +
                      "• This means whenever your final tree is valid (doesn't break any rules), it is <color=#1E90FF>Correct</color>. Strict tie-breaking conventions are preferred and taught, but <color=#FFD700>not strictly enforced</color> to pass."
        },
        new TutorialPage
        {
            title = "Scoring & Grades",
            content = "Aim for an <color=#FFD700>S Grade</color>!\n\n" +
                      "• <color=#32CD32>Base Score:</color> +100 per Insert, +150 per Delete.\n" +
                      "• <color=#1E90FF>Time Bonus:</color> The faster you finish a task, the more bonus points!\n" +
                      "• <color=#FFA500>Super Slash Bonus:</color> +300 per Enemy/Door bypassed!\n" +
                      "• <color=#FF6347>Mistakes Penalty:</color> -100 (Insert/Delete), -50 (Search) mistakes.\n" +
                      "• <color=#FFD700>Perfect Clear:</color> +500 points for 0 mistakes!\n\n" +
                      "<color=#FFD700>S</color> (2500+ & 0 Mistakes) | <color=#FF6347>A</color> (1500+) | <color=#1E90FF>B</color> (800+) | <color=#32CD32>C</color> (400+) | <color=#FFFFFF>D</color>"
        }
    };

    private bool isPlayerInRange = false;

    private void Update()
    {
        // Toggle tutorial anywhere with 'T'
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (TutorialUIManager.Instance != null && TutorialUIManager.Instance.tutorialPanel != null)
            {
                bool isOpen = TutorialUIManager.Instance.tutorialPanel.activeInHierarchy;
                if (isOpen)
                {
                    TutorialUIManager.Instance.CloseTutorial();
                }
                else if (Time.timeScale > 0f) // Only open if time is running (not blocked by settings/confirmation)
                {
                    TutorialUIManager.Instance.OpenTutorial(tutorialPages);
                    Time.timeScale = 0f; // Pause the game
                }
            }
        }

        // Interact to open tutorial with 'E'
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (TutorialUIManager.Instance != null && TutorialUIManager.Instance.tutorialPanel != null)
            {
                bool isOpen = TutorialUIManager.Instance.tutorialPanel.activeInHierarchy;
                if (!isOpen && Time.timeScale > 0f)
                {
                    TutorialUIManager.Instance.OpenTutorial(tutorialPages);
                    Time.timeScale = 0f; // Pause the game
                    
                    // Hide the prompt once the player opens the UI
                    if (PlayerInstructionUI.Instance != null)
                    {
                        PlayerInstructionUI.Instance.HideInstruction();
                    }
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

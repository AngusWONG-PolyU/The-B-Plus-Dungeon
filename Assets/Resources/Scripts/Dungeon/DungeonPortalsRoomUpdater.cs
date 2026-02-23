using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DungeonPortalsRoomUpdater : MonoBehaviour
{
    [Header("Portal Configuration")]
    public GameObject child1Portal;
    public GameObject child2Portal;
    public GameObject child3Portal;
    public GameObject child4Portal;
    public GameObject child5Portal;
    
    [Header("Room References")]
    public Transform nextRoomSpawn; // Where to teleport for the correct portal
    
    [Header("References")]
    public DungeonMinimap minimap;
    public DungeonManager dungeonManager; // Handles hearts, win/lose
    public DungeonUIManager dungeonUI;
    
    // Current node tracking (runtime only - not serialized)
    [System.NonSerialized]
    private BPlusTreeNode<int, int> currentNode;
    [System.NonSerialized]
    private int[] correctSearchPath;
    [System.NonSerialized]
    private int currentPathIndex = 0;
    
    void Start()
    {
        if (dungeonUI == null)
        {
            dungeonUI = FindObjectOfType<DungeonUIManager>();
        }

        // Deactivate UI initially
        SetUIVisibility(false);

        // Deactivate all portals initially
        DeactivateAllPortals();
    }
    
    // Initialize the room updater with tree structure
    public void Initialize(BPlusTreeNode<int, int> rootNode, int[] searchPath)
    {
        currentNode = rootNode;
        correctSearchPath = searchPath;
        currentPathIndex = 0;
        UpdateRoomForNode(currentNode);
    }
    
    // Helper to control UI visibility
    public void SetUIVisibility(bool visible)
    {
        if (dungeonUI != null)
        {
            dungeonUI.SetTargetVisibility(visible);
            dungeonUI.SetMinimapVisibility(visible);
        }
    }
    
    // Set target key to display in UI
    public void SetTargetKey(int targetKey)
    {
        if (dungeonUI != null)
        {
            dungeonUI.SetTargetVisibility(true);
            dungeonUI.SetTargetText($"Target: {targetKey}");
            dungeonUI.SetTargetColor(new Color(0.5f, 1f, 0.5f)); // Light green color for target
        }
    }
    
    // Deactivate all child portals
    public void DeactivateAllPortals()
    {
        // Clear player references before deactivating
        ClearAllPortalPlayerReferences();
        
        if (child1Portal != null) child1Portal.SetActive(false);
        if (child2Portal != null) child2Portal.SetActive(false);
        if (child3Portal != null) child3Portal.SetActive(false);
        if (child4Portal != null) child4Portal.SetActive(false);
        if (child5Portal != null) child5Portal.SetActive(false);
    }
    
    // Clear player references from all portals
    private void ClearAllPortalPlayerReferences()
    {
        GameObject[] allPortals = { child1Portal, child2Portal, child3Portal, child4Portal, child5Portal };
        
        foreach (GameObject portal in allPortals)
        {
            if (portal != null)
            {
                // Find Portal green
                Transform greenPortal = portal.transform.Find("Portal green");
                if (greenPortal != null)
                {
                    DungeonPortalController controller = greenPortal.GetComponent<DungeonPortalController>();
                    if (controller != null)
                    {
                        controller.ResetPortal();
                    }
                }
            }
        }
    }
    
    // Update room to show only the required portals based on the current node
    public void UpdateRoomForNode(BPlusTreeNode<int, int> node)
    {
        if (node == null)
        {
            DeactivateAllPortals();
            return;
        }
        
        // If leaf node, deactivate all portals (end of dungeon)
        if (node.IsLeaf)
        {
            DeactivateAllPortals();
            
            Debug.Log("Reached leaf node logic - waiting for player to clear Boss Room.");
            return;
        }
        
        // Update for child count
        UpdateRoomForChildren(node);
    }

    public void ShowTargetFoundUI()
    {
        // Show UI (Minimap and Target Text)
        SetUIVisibility(true);
        
        // Update Target Text to indicate target found
        if (dungeonUI != null)
        {
            dungeonUI.SetTargetText("Target Found!");
            dungeonUI.SetTargetColor(Color.yellow);
        }
        
        Debug.Log("Target Found UI Shown!");
    }
    
    // Update room to show only the required portals based on child count
    public void UpdateRoomForChildren(BPlusTreeNode<int, int> node)
    {
        int childCount = node.Children.Count;

        // First deactivate all
        DeactivateAllPortals();
        
        // Get which portals to activate
        int[] portalsToActivate = GetPortalIndices(childCount);
        
        // Activate and configure each portal
        for (int i = 0; i < portalsToActivate.Length; i++)
        {
            int portalNumber = portalsToActivate[i];
            GameObject portal = GetPortalByNumber(portalNumber);
            
            if (portal != null)
            {
                portal.SetActive(true);
                
                // Determine label text
                string labelText = "";
                if (i == 0)
                {
                    labelText = $"< {node.Keys[0]}";
                }
                else if (i == childCount - 1)
                {
                    labelText = $"≥ {node.Keys[i - 1]}";
                }
                else
                {
                    labelText = $"≥ {node.Keys[i - 1]} & < {node.Keys[i]}";
                }

                // Update the label to show the correct child number
                UpdatePortalLabel(portal, labelText);
                
                // Configure portal controller
                ConfigurePortal(portal, i);
            }
            else
            {
                Debug.LogWarning($"Portal {portalNumber} reference not assigned in DungeonPortalsRoomUpdater!");
            }
        }
    }
    
    // Configure portal's childIndex and references
    private void ConfigurePortal(GameObject portal, int childIndex)
    {
        // Find Portal green
        Transform greenPortal = portal.transform.Find("Portal green");
        if (greenPortal == null)
        {
            Debug.LogWarning($"Cannot find 'Portal green' in {portal.name}");
            return;
        }
        
        DungeonPortalController controller = greenPortal.GetComponent<DungeonPortalController>();
        if (controller == null)
        {
            Debug.LogWarning($"DungeonPortalController not found on {greenPortal.name}! Please add it in the scene.");
            return;
        }
        
        // Configure the controller
        controller.childIndex = childIndex;
        controller.dungeonPortalsRoomUpdater = this;
        
        controller.teleportDestination = nextRoomSpawn;
        
        controller.ResetPortal(); // Reset state
        
        // Make sure Portal green is active, Portal red is inactive
        Transform redPortal = portal.transform.Find("Portal red");
        if (redPortal != null)
        {
            greenPortal.gameObject.SetActive(true);
            redPortal.gameObject.SetActive(false);
            Debug.Log($"[ConfigurePortal] {portal.name}: Portal green=active, Portal red=inactive");
        }
    }
    
    // Check if we should spawn the Boss Room
    public bool ShouldSpawnBossRoom()
    {
        if (correctSearchPath == null) return false;
        
        return currentPathIndex == correctSearchPath.Length - 1;
    }

    // Called when the player enters the portals room
    public void OnEnterPortalsRoom()
    {
        // Show UI again
        SetUIVisibility(true);
    }
    
    // Called when player exits the dungeon entirely
    public void OnExitDungeon()
    {
        SetUIVisibility(false);
    }

    // Called by DungeonPortalController when player selects a portal
    public void OnPlayerSelectPortal(int childIndex, DungeonPortalController portal)
    {
        if (currentNode == null || currentNode.IsLeaf || childIndex >= currentNode.Children.Count)
        {
            Debug.LogWarning("Cannot select portal - invalid state");
            return;
        }
        
        // Check if this is the correct portal
        bool isCorrect = false;
        if (currentPathIndex < correctSearchPath.Length)
        {
            isCorrect = (childIndex == correctSearchPath[currentPathIndex]);
        }
        
        if (isCorrect)
        {
            // Hide UI when leaving the portals room
            SetUIVisibility(false);
            
            // Correct portal - teleport player
            portal.TeleportPlayer();
            
            // After teleport effect, update room (called from portal's coroutine end)
            StartCoroutine(WaitAndUpdateRoom(childIndex));
        }
        else
        {
            // Wrong portal - turn red and lose heart
            portal.TurnRed();
            
            PlayerInstructionUI.Instance?.ShowInstruction("Illusion Shattered! The portal drains your life force!\nHEART LOST!", 3f, true);
            
            if (dungeonManager != null)
            {
                dungeonManager.OnWrongPortal();
            }
        }
    }
    
    IEnumerator WaitAndUpdateRoom(int childIndex)
    {
        // Wait for teleport effect to complete
        yield return new WaitForSeconds(1.1f);
        
        // Update to next room
        currentPathIndex++;
        currentNode = currentNode.Children[childIndex];
        
        // Update minimap
        if (minimap != null)
        {
            minimap.EnterChildNode(childIndex);
            
            // Check if reached leaf
            if (minimap.isAtLeaf)
            {
                if (dungeonManager != null)
                {
                    dungeonManager.OnReachedLeaf(currentNode);
                }
            }
        }
        
        // Deactivate all portals before updating the room
        DeactivateAllPortals();
        
        // Update room portals (will reactivate and reconfigure the needed ones)
        UpdateRoomForNode(currentNode);
    }
    
    // Get portal GameObject by number
    private GameObject GetPortalByNumber(int number)
    {
        switch (number)
        {
            case 1: return child1Portal;
            case 2: return child2Portal;
            case 3: return child3Portal;
            case 4: return child4Portal;
            case 5: return child5Portal;
            default: return null;
        }
    }
    
    // Returns which portal numbers to activate based on child count
    private int[] GetPortalIndices(int childCount)
    {
        switch (childCount)
        {
            case 2: return new int[] { 2, 4 };
            case 3: return new int[] { 2, 3, 4 };
            case 4: return new int[] { 1, 2, 4, 5 };
            case 5: return new int[] { 1, 2, 3, 4, 5 };
            default:
                Debug.LogWarning($"Unexpected child count: {childCount}. Using default [2, 3, 4]");
                return new int[] { 2, 3, 4 };
        }
    }
    
    // Update portal label to show correct child number
    private void UpdatePortalLabel(GameObject portal, string labelText)
    {
        // Find child label
        string portalNum = portal.name.Replace("Child", "").Replace(" Portal", "").Trim();
        string labelName = "Child" + portalNum + " Label";
        Transform labelTransform = portal.transform.Find(labelName);
        
        if (labelTransform != null)
        {
            TMPro.TMP_Text tmpText = labelTransform.GetComponent<TMPro.TMP_Text>();
            if (tmpText != null)
            {
                tmpText.text = labelText;
            }
        }
    }
}

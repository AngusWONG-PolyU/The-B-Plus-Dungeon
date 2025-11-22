using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonRoomUpdater : MonoBehaviour
{
    [Header("Portal Configuration")]
    public GameObject child1Portal;
    public GameObject child2Portal;
    public GameObject child3Portal;
    public GameObject child4Portal;
    public GameObject child5Portal;
    
    [Header("Room References")]
    public Transform nextRoomSpawn; // Where to teleport for correct portal
    public Transform leafRoomSpawn; // Where to teleport when next room is leaf
    public GameObject dungeonStaircase; // Staircase entrance (for tutorial)
    public GameObject portalSpawn; // Portal spawn point (for normal loop)
    
    [Header("References")]
    public DungeonMinimap minimap;
    public DungeonManager dungeonManager; // Handles hearts, win/lose
    
    [Header("UI")]
    public TMPro.TMP_Text TargetText; // Text component
    
    // Current node tracking (runtime only - not serialized)
    [System.NonSerialized]
    private BPlusTreeNode<int, int> currentNode;
    [System.NonSerialized]
    private int[] correctSearchPath;
    [System.NonSerialized]
    private int currentPathIndex = 0;
    
    private bool enteredByStaircase = false; // Track if player entered via staircase
    
    void Start()
    {
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
    
    // Set target key to display in UI
    public void SetTargetKey(int targetKey)
    {
        if (TargetText != null)
        {
            TargetText.text = $"Target: {targetKey}";
            TargetText.color = new Color(0.5f, 1f, 0.5f); // Light green color for target
        }
    }
    
    // Called by DungeonStaircase when player enters via staircase (tutorial mode)
    public void SetEnteredByStaircase()
    {
        enteredByStaircase = true;
        
        // Activate staircase, deactivate portal spawn
        if (dungeonStaircase != null) dungeonStaircase.SetActive(true);
        if (portalSpawn != null) portalSpawn.SetActive(false);
        
        Debug.Log("Player entered by staircase - Tutorial mode activated");
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
            Debug.Log("Reached leaf node - no more portals");
            return;
        }
        
        // Update for child count
        int childCount = node.Children.Count;
        UpdateRoomForChildren(childCount);
    }
    
    // Update room to show only the required portals based on child count
    public void UpdateRoomForChildren(int childCount)
    {
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
                
                // Update the label to show the correct child number
                UpdatePortalLabel(portal, i + 1); // Display as Child 1, Child 2, etc.
                
                // Configure portal controller
                ConfigurePortal(portal, i);
            }
            else
            {
                Debug.LogWarning($"Portal {portalNumber} reference not assigned in DungeonRoomUpdater!");
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
        controller.dungeonRoomUpdater = this;
        
        // Check if the child node is a leaf, use leafRoomSpawn if so
        if (currentNode != null && childIndex < currentNode.Children.Count)
        {
            BPlusTreeNode<int, int> childNode = currentNode.Children[childIndex];
            controller.teleportDestination = (childNode.IsLeaf && leafRoomSpawn != null) ? leafRoomSpawn : nextRoomSpawn;
        }
        else
        {
            controller.teleportDestination = nextRoomSpawn;
        }
        
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
            // Switch from staircase to portal spawn IMMEDIATELY when the correct portal is selected
            SwitchToPortalSpawn();
            
            // Correct portal - teleport player
            portal.TeleportPlayer();
            
            // After teleport effect, update room (called from portal's coroutine end)
            StartCoroutine(WaitAndUpdateRoom(childIndex));
        }
        else
        {
            // Wrong portal - turn red and lose heart
            portal.TurnRed();
            
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
        
        // Deactivate all portals before updating room
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
    private void UpdatePortalLabel(GameObject portal, int displayNumber)
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
                tmpText.text = "Child " + displayNumber;
            }
        }
    }
    
    // Switch from staircase to portal spawn after first teleport
    private void SwitchToPortalSpawn()
    {
        if (enteredByStaircase)
        {
            enteredByStaircase = false;
            
            // Deactivate staircase, activate portal spawn
            if (dungeonStaircase != null)
            {
                dungeonStaircase.SetActive(false);
                Debug.Log("DungeonStaircase deactivated");
            }
            
            if (portalSpawn != null)
            {
                portalSpawn.SetActive(true);
                Debug.Log("Portal spawn activated");
            }
            
            Debug.Log("Switched to portal spawn mode - all future teleports use portal spawn");
        }
    }
}

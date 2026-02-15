using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TaskDragManager : MonoBehaviour
{
    public static TaskDragManager Instance { get; private set; }

    [Header("Drag Settings")]
    public float thresholdDistance = 50f;
    public bool isDragging = false;
    
    // Key Dragging
    private TaskDraggable _currentItem;
    
    // Node Dragging
    public bool IsNodeDragging { get; private set; }
    private BPlusTreeVisualNode _dragedNode;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Key Dragging
    public void StartDragging(TaskDraggable item)
    {
        _currentItem = item;
        isDragging = true;
    }

    public void StopDragging()
    {
        if (_currentItem != null) _currentItem = null;
        isDragging = false;
    }

    // Node Dragging
    public void StartNodeDragging(BPlusTreeVisualNode node)
    {
        _dragedNode = node;
        IsNodeDragging = true;
    }

    public void StopNodeDragging(PointerEventData eventData)
    {
        // Drop logic is handled by DropZone. If we are here, it means either:
        // 1. Dropped on nothing (Reset)
        // 2. Already dropped on DropZone (Success) -> Logic handled there
        
        _dragedNode = null;
        IsNodeDragging = false;
    }
    
    // Call from DropZone for Node Merge
    public bool HandleNodeDrop(BPlusTreeVisualNode targetNode)
    {
        if (_dragedNode == null || targetNode == null) return false;
        if (_dragedNode == targetNode) return false; // Can't merge with self
        
        Debug.Log($"Dropped Node (Keys: {_dragedNode.CoreNode.Keys.Count}) onto Node (Keys: {targetNode.CoreNode.Keys.Count})");

        // Logic Check: Must be Siblings to Merge directly (in standard implementation)
        if (_dragedNode.CoreNode.Parent != targetNode.CoreNode.Parent)
        {
            Debug.LogWarning("Cannot merge nodes with different parents!");
            return false;
        }

        // Merge Logic
        // 1. Move all Keys from Dragged Node to Target Node
        targetNode.CoreNode.Keys.AddRange(_dragedNode.CoreNode.Keys);
        targetNode.CoreNode.Keys.Sort();
        
        // 2. Remove Dragged Node from Parent
        if (_dragedNode.CoreNode.Parent != null)
        {
            _dragedNode.CoreNode.Parent.Children.Remove(_dragedNode.CoreNode);
            // Also need to handle Separator Key in Parent... (Simplified for prototype: just remove node)
            // Ideally: Remove the separator key that was pointing to _dragedNode
        }

        // 3. Clear Dragged Node Data
        _dragedNode.CoreNode.Keys.Clear();
        
        // 4. Update UI
        if(BPlusTreeTaskManager.Instance != null)
        {
            BPlusTreeTaskManager.Instance.RefreshTree();
            StartCoroutine(ValidateAllNodesRoutine());
        }

        return true;
    }
    
    // Deletion Logic
    
    public void DeleteKey(BPlusTreeVisualNode visualNode, int key)
    {
        if(visualNode == null) return;
        
        Debug.Log($"Attempting to delete Key {key} from Node.");
        
        // 1. Remove from Core Data
        bool removed = visualNode.CoreNode.Keys.Remove(key);
        
        if(removed)
        {
             // 2. Refresh Tree
             if(BPlusTreeTaskManager.Instance != null)
             {
                 BPlusTreeTaskManager.Instance.RefreshTree();
                 
                 // 3. Check ALL nodes for underflow AFTER refresh
                 StartCoroutine(ValidateAllNodesRoutine());
             }
        }
    }
    
    private IEnumerator ValidateAllNodesRoutine()
    {
        // Wait for frame end to ensure visuals are spawned
        yield return new WaitForEndOfFrame();
        
        BPlusTreeVisualNode[] allNodes = FindObjectsOfType<BPlusTreeVisualNode>();
        foreach(var node in allNodes)
        {
            CheckUnderflow(node);
        }
    }

    private void CheckUnderflow(BPlusTreeVisualNode node)
    {
        int order = BPlusTreeTaskManager.Instance.treeOrder;
        int minKeys = (int)Mathf.Ceil((order - 1) / 2.0f);
        
        if (node.CoreNode.Keys.Count < minKeys)
        {
            Debug.LogWarning($"Node Underflow! Keys: {node.CoreNode.Keys.Count}, Min: {minKeys}");
            
            // Highlight Logic based on Difficulty
            if (ShouldHighlightError())
            {
                node.SetHighlight(true);
            }
        }
        else
        {
            // Reset to normal if fixed
            node.SetHighlight(false); 
        }
    }

    private bool ShouldHighlightError()
    {
        DungeonGenerator dungeonGen = FindObjectOfType<DungeonGenerator>();
        if(dungeonGen != null)
        {
            return dungeonGen.difficultyMode == DungeonGenerator.DifficultyMode.Tutorial;
        }
        return true; // Default to allow highlight if no dungeon gen found (testing)
    }

    // Helper to find root (temporary)
    private BPlusTree<int, string> GetTreeRoot(BPlusTreeNode<int, string> node)
    {
        // ... traverse up ...
        return null; 
    }

    // Call from DropZone logic or OnEndDrag
    public bool HandleKeyDrop(TaskDraggable draggable, BPlusTreeVisualNode targetNode)
    {
        if (draggable == null || targetNode == null) return false;

        // Get value
        var textComp = draggable.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textComp == null || !int.TryParse(textComp.text, out int keyVal)) return false;

        // Check source
        BPlusTreeVisualNode sourceNode = draggable.originalParent.GetComponentInParent<BPlusTreeVisualNode>();
        if (sourceNode == null || sourceNode == targetNode) return false;

        Debug.Log($"Moving Key {keyVal} from Node {sourceNode.name} to {targetNode.name}");

        // Move Logic (Borrow / Redistribution)
        // 1. Remove from source
        sourceNode.CoreNode.Keys.Remove(keyVal);
        
        // 2. Add to target
        targetNode.CoreNode.Keys.Add(keyVal);
        targetNode.CoreNode.Keys.Sort(); // Keep sorted!

        // 3. Update Visuals
        if (BPlusTreeTaskManager.Instance != null)
        {
            BPlusTreeTaskManager.Instance.RefreshTree();
            StartCoroutine(ValidateAllNodesRoutine());
        }

        Destroy(draggable.gameObject); // Cleanup old visual (Refreshed anyway)
        return true;
    }
}

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
    public bool IsKeyDragging => isDragging;

    // Node Dragging
    public bool IsNodeDragging { get; private set; }
    private BPlusTreeVisualNode _dragedNode;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    #region Drag Listeners

    public void StartDragging(TaskDraggable item)
    {
        _currentItem = item;
        isDragging = true;
    }

    public void StopDragging()
    {
        _currentItem = null;
        isDragging = false;
    }

    public void StartNodeDragging(BPlusTreeVisualNode node)
    {
        _dragedNode = node;
        IsNodeDragging = true;
    }

    public void StopNodeDragging(PointerEventData eventData)
    {
        _dragedNode = null;
        IsNodeDragging = false;
    }

    #endregion

    #region Drop Logic (Key & Node)

    // Main Entry Point for Key Drops
    public bool HandleKeyDrop(TaskDraggable draggable, BPlusTreeVisualNode targetNode)
    {
        if (draggable == null || targetNode == null) return false;

        // 1. Extract Key Value
        int keyVal = GetValueFromDraggable(draggable);
        if (keyVal == -1) return false;

        // 2. Identify Source Node
        if (draggable.originalParent == null) return false;
        BPlusTreeVisualNode sourceNode = draggable.originalParent.GetComponentInParent<BPlusTreeVisualNode>();
        if (sourceNode == null) return false;
        
        // Validation calls
        if (sourceNode == targetNode) return false;

        Debug.Log($"Handling Key Drop: Key {keyVal} from {sourceNode.name} to {targetNode.name}");

        // 3. COPY UP: Leaf -> Parent
        // Standard behavior: Copy up only happens when promoting a key to the immediate parent
        if (sourceNode.CoreNode.IsLeaf && !targetNode.CoreNode.IsLeaf)
        {
            if (sourceNode.CoreNode.Parent == targetNode.CoreNode)
            {
                Debug.Log("Operation: Copy Up (Leaf -> Parent)");
                bool success = PerformCopyUp(targetNode, keyVal);
                if (success)
                {
                    Destroy(draggable.gameObject); 
                    return true; 
                }
                else
                {
                    Debug.LogWarning("Key already exists in an internal node.");
                    PlayerInstructionUI.Instance?.ShowInstruction("Key already exists in an internal node.", 2f, true);
                    return false;
                }
            }
            else
            {
                Debug.LogWarning("Cannot move or copy key from leaf to non-parent internal node.");
                PlayerInstructionUI.Instance?.ShowInstruction("Cannot move key from leaf to non-parent internal node.", 2f, true);
                return false;
            }
        }

        // 4. MOVE: Node -> Node (Leaf->Leaf or Internal->Internal)
        // Prevent moving between different types (Leaf <-> Internal)
        if (sourceNode.CoreNode.IsLeaf != targetNode.CoreNode.IsLeaf)
        {
            Debug.LogWarning($"Cannot move key between different node types. Source is Leaf: {sourceNode.CoreNode.IsLeaf}, Target is Leaf: {targetNode.CoreNode.IsLeaf}");
            PlayerInstructionUI.Instance?.ShowInstruction("Cannot move key between different node types.", 2f, true);
            return false;
        }

        Debug.Log("Operation: Move Key (Borrow/Redistribute)");
        PerformMoveKey(sourceNode, targetNode, keyVal);
        
        Destroy(draggable.gameObject);
        return true;
    }

    // Main Entry Point for Node Merging
    public bool HandleNodeDrop(BPlusTreeVisualNode targetNode)
    {
        if (_dragedNode == null || targetNode == null || _dragedNode == targetNode) return false;
        
        // Level restriction check
        int draggedDepth = BPlusTreeTaskManager.Instance.Visualizer.GetDepth(_dragedNode.CoreNode);
        int targetDepth = BPlusTreeTaskManager.Instance.Visualizer.GetDepth(targetNode.CoreNode);
        
        if (draggedDepth != targetDepth || _dragedNode.CoreNode.IsLeaf != targetNode.CoreNode.IsLeaf)
        {
            Debug.LogWarning($"Cannot merge nodes at different levels or types. Dragged depth: {draggedDepth}, Target depth: {targetDepth}");
            PlayerInstructionUI.Instance?.ShowInstruction("Cannot merge nodes at different levels or types.", 2f, true);
            return false;
        }

        Debug.Log($"Handling Node Drop: Merge {_dragedNode.name} into {targetNode.name}");

        // 1. Merge Data
        if (targetNode.CoreNode.IsLeaf)
        {
            MergeLeafNodes(_dragedNode, targetNode);
        }
        else
        {
            MergeInternalNodes(_dragedNode, targetNode);
        }

        // 2. Cleanup Old Node Structure
        RemoveNodeFromStructure(_dragedNode);

        // 3. Refresh Tree
        UpdateTreeVisuals();
        
        return true;
    }

    public void DeleteKey(BPlusTreeVisualNode visualNode, int key)
    {
        if (visualNode == null) return;

        // Restriction Check for Deletion Task
        if (BPlusTreeTaskManager.Instance != null && 
            BPlusTreeTaskManager.Instance.CurrentTaskType == BPlusTreeTaskType.Deletion)
        {
            // Only enforce strict target check on Leaf Nodes
            if (visualNode.CoreNode.IsLeaf && !BPlusTreeTaskManager.Instance.TargetKeys.Contains(key))
            {
                string keysStr = string.Join(", ", BPlusTreeTaskManager.Instance.TargetKeys);
                Debug.LogWarning($"Deletion Task: Can only delete target key(s) {keysStr} from leaf.");
                PlayerInstructionUI.Instance?.ShowInstruction($"Can only delete target key(s) {keysStr} from leaf.", 2f, true);
                return;
            }
        }

        Debug.Log($"Deleting Key {key} from {visualNode.name}");
        RemoveKeyFromNode(visualNode, key);
        UpdateTreeVisuals();
    }

    public void DeleteNode(BPlusTreeVisualNode visualNode)
    {
        if (visualNode == null || visualNode.CoreNode == null) return;
        
        // Root Protection
        if (visualNode.CoreNode == BPlusTreeTaskManager.Instance.CurrentTree.Root)
        {
            Debug.LogWarning("Cannot delete root node directly.");
            PlayerInstructionUI.Instance?.ShowInstruction("Cannot delete root node directly.", 2f, true);
            return;
        }

        Debug.Log($"Deleting Node {visualNode.name}");
        RemoveNodeFromStructure(visualNode);
        UpdateTreeVisuals();
    }

    // Public method for Context Menu to call
    public void CopyKeyToParent(BPlusTreeVisualNode node, int key)
    {
        if (node == null || node.CoreNode == null) return;

        // Validation
        if (!node.CoreNode.IsLeaf)
        {
            Debug.LogWarning("Copy Up is only valid for Leaf Nodes.");
            PlayerInstructionUI.Instance?.ShowInstruction("Copy Up is only valid for Leaf Nodes.", 2f, true);
            return;
        }

        // Handle No Parent (Root)
        if (node.CoreNode.Parent == null)
        {
            CreateNewRootFromChild(node.CoreNode);
            return;
        }
        
        BPlusTreeNode<int, string> parentNode = node.CoreNode.Parent;
        if (parentNode.Keys.Contains(key))
        {
            Debug.LogWarning("Key already exists in parent node.");
            PlayerInstructionUI.Instance?.ShowInstruction("Key already exists in parent node.", 2f, true);
            return;
        }

        if (IsKeyInAnyInternalNode(BPlusTreeTaskManager.Instance.CurrentTree.Root, key))
        {
            Debug.LogWarning("Key already exists in an internal node.");
            PlayerInstructionUI.Instance?.ShowInstruction("Key already exists in an internal node.", 2f, true);
            return;
        }

        parentNode.Keys.Add(key);
        parentNode.Keys.Sort();
        
        UpdateTreeVisuals();
    }

    public void SplitNode(BPlusTreeVisualNode node, int splitKey)
    {
        if (node == null || node.CoreNode == null) return;

        var coreNode = node.CoreNode;
        int splitIndex = coreNode.Keys.IndexOf(splitKey);

        if (splitIndex == -1)
        {
            Debug.LogWarning("Split key not found in node.");
            return;
        }

        // Create new right node
        var newRightNode = new BPlusTreeNode<int, string>(coreNode.IsLeaf);

        // Move keys and values/children to the new right node
        newRightNode.Keys.AddRange(coreNode.Keys.GetRange(splitIndex, coreNode.Keys.Count - splitIndex));
        coreNode.Keys.RemoveRange(splitIndex, coreNode.Keys.Count - splitIndex);

        if (coreNode.IsLeaf)
        {
            if (coreNode.Values != null)
            {
                int valCount = coreNode.Values.Count;
                if (splitIndex < valCount)
                {
                    newRightNode.Values.AddRange(coreNode.Values.GetRange(splitIndex, valCount - splitIndex));
                    coreNode.Values.RemoveRange(splitIndex, valCount - splitIndex);
                }
            }

            // Update linked list pointers
            newRightNode.Next = coreNode.Next;
            coreNode.Next = newRightNode;
        }
        else
        {
            if (coreNode.Children != null)
            {
                int childrenSplitIndex = splitIndex + 1;
                if (childrenSplitIndex <= coreNode.Children.Count)
                {
                    newRightNode.Children.AddRange(coreNode.Children.GetRange(childrenSplitIndex, coreNode.Children.Count - childrenSplitIndex));
                    coreNode.Children.RemoveRange(childrenSplitIndex, coreNode.Children.Count - childrenSplitIndex);

                    // Update parent pointers for moved children
                    foreach (var child in newRightNode.Children)
                    {
                        child.Parent = newRightNode;
                    }
                }
            }
        }

        // Handle parent
        if (coreNode.Parent == null)
        {
            // Create a new root
            var newRoot = new BPlusTreeNode<int, string>(false);
            newRoot.Children.Add(coreNode);
            newRoot.Children.Add(newRightNode);
            coreNode.Parent = newRoot;
            newRightNode.Parent = newRoot;

            if (BPlusTreeTaskManager.Instance != null)
            {
                BPlusTreeTaskManager.Instance.UpdateTreeRoot(newRoot);
            }
        }
        else
        {
            // Add new right node to existing parent
            var parent = coreNode.Parent;
            int childIndex = parent.Children.IndexOf(coreNode);
            if (childIndex != -1)
            {
                parent.Children.Insert(childIndex + 1, newRightNode);
                newRightNode.Parent = parent;
            }
            else
            {
                parent.Children.Add(newRightNode);
                newRightNode.Parent = parent;
            }
        }

        UpdateTreeVisuals();
    }

    #endregion

    #region Operation Helpers
    
    // Copy Up Logic
    // Used by Drag & Drop which has a target visual node
    private bool PerformCopyUp(BPlusTreeVisualNode targetInternalNode, int key)
    {
        // Check duplicate in the target node
        if (targetInternalNode.CoreNode.Keys.Contains(key)) return false;

        // Check duplicate in ALL internal nodes
        if (IsKeyInAnyInternalNode(BPlusTreeTaskManager.Instance.CurrentTree.Root, key))
        {
            return false;
        }

        // Add to internal node
        targetInternalNode.CoreNode.Keys.Add(key);
        targetInternalNode.CoreNode.Keys.Sort();
        
        UpdateTreeVisuals();
        return true;
    }

    private bool IsKeyInAnyInternalNode(BPlusTreeNode<int, string> node, int key)
    {
        if (node == null || node.IsLeaf) return false;

        if (node.Keys.Contains(key)) return true;

        foreach (var child in node.Children)
        {
            if (IsKeyInAnyInternalNode(child, key)) return true;
        }

        return false;
    }

    // Move Key Logic
    private void PerformMoveKey(BPlusTreeVisualNode source, BPlusTreeVisualNode target, int key)
    {
        // 1. Remove from source
        RemoveKeyFromNode(source, key);

        // 2. Add to the target
        AddKeyToNode(target, key);

        UpdateTreeVisuals();
    }

    // Merge Leaf Logic
    private void MergeLeafNodes(BPlusTreeVisualNode source, BPlusTreeVisualNode target)
    {
        // Combine Key-Value Pairs
        var combined = new List<KeyValuePair<int, string>>();
        ExtractPairs(target.CoreNode, combined);
        ExtractPairs(source.CoreNode, combined);

        // Sort
        combined.Sort((a, b) => a.Key.CompareTo(b.Key));

        // Reapply to Target
        target.CoreNode.Keys.Clear();
        if (target.CoreNode.Values == null) target.CoreNode.Values = new List<string>();
        target.CoreNode.Values.Clear();

        foreach(var pair in combined)
        {
            target.CoreNode.Keys.Add(pair.Key);
            target.CoreNode.Values.Add(pair.Value);
        }
        
        // Clear source for safety
        source.CoreNode.Keys.Clear();
    }

    // Merge Internal Logic
    private void MergeInternalNodes(BPlusTreeVisualNode source, BPlusTreeVisualNode target)
    {
        // Move Keys
        target.CoreNode.Keys.AddRange(source.CoreNode.Keys);
        target.CoreNode.Keys.Sort();

        // Move Children
        if (source.CoreNode.Children != null)
        {
            target.CoreNode.Children.AddRange(source.CoreNode.Children);
            foreach(var c in source.CoreNode.Children) c.Parent = target.CoreNode;
            
            // Sort Children by looking at their first key
            target.CoreNode.Children.Sort((a, b) => {
                int keyA = (a.Keys.Count > 0) ? a.Keys[0] : int.MinValue;
                int keyB = (b.Keys.Count > 0) ? b.Keys[0] : int.MinValue;
                return keyA.CompareTo(keyB);
            });
        }
        
        source.CoreNode.Keys.Clear();
        source.CoreNode.Children.Clear();
    }

    // Cleanup Logic (Linked List + Parent Ref)
    private void RemoveNodeFromStructure(BPlusTreeVisualNode node)
    {
        var coreNode = node.CoreNode;
        var parent = coreNode.Parent;

        if (parent != null)
        {
            // 1. Maintain Linked List if Leaf
            if (coreNode.IsLeaf)
            {
                UnlinkLeafNode(coreNode);
            }

            // 2. Remove reference from parent
            parent.Children.Remove(coreNode);
        }
    }

    private void UnlinkLeafNode(BPlusTreeNode<int, string> node)
    {
        var current = BPlusTreeTaskManager.Instance.CurrentTree.FirstLeaf;
        if (current == node)
        {
            BPlusTreeTaskManager.Instance.CurrentTree.FirstLeaf = current.Next;
        }
        else
        {
            while (current != null && current.Next != node)
            {
                current = current.Next;
            }
            if (current != null)
            {
                current.Next = node.Next;
            }
        }
    }

    private void CreateNewRootFromChild(BPlusTreeNode<int, string> child)
    {
        // 1. Create New Internal Node (Root)
        var newRoot = new BPlusTreeNode<int, string>(false);
        
        // 2. Link Child to New Root
        newRoot.Children.Add(child);
        child.Parent = newRoot;
        
        // 3. Update Tree Reference
        if (BPlusTreeTaskManager.Instance != null)
        {
            BPlusTreeTaskManager.Instance.UpdateTreeRoot(newRoot);
        }

        UpdateTreeVisuals();
    }

    #endregion


    #region Data Mutation Helpers

    private void RemoveKeyFromNode(BPlusTreeVisualNode visualNode, int key)
    {
        int index = visualNode.CoreNode.Keys.IndexOf(key);
        if (index != -1)
        {
            visualNode.CoreNode.Keys.RemoveAt(index);
            
            // Auto-remove value if leaf
            if (visualNode.CoreNode.IsLeaf && visualNode.CoreNode.Values != null && index < visualNode.CoreNode.Values.Count)
            {
                visualNode.CoreNode.Values.RemoveAt(index);
            }
        }
    }

    private void AddKeyToNode(BPlusTreeVisualNode visualNode, int key)
    {
        visualNode.CoreNode.Keys.Add(key);
        visualNode.CoreNode.Keys.Sort();
        
        // Auto-add dummy value if leaf
        if (visualNode.CoreNode.IsLeaf)
        {
            if (visualNode.CoreNode.Values == null) visualNode.CoreNode.Values = new List<string>();
            
            // Re-sync values based on sorted keys
            // Simple approach: Insert default val at the new index of the key
            int newIndex = visualNode.CoreNode.Keys.IndexOf(key);
            visualNode.CoreNode.Values.Insert(newIndex, "val-" + key);
        }
    }

    private void ExtractPairs(BPlusTreeNode<int, string> node, List<KeyValuePair<int, string>> results)
    {
        for(int i=0; i<node.Keys.Count; i++)
        {
            string val = (node.Values != null && i < node.Values.Count) ? node.Values[i] : "";
            results.Add(new KeyValuePair<int, string>(node.Keys[i], val));
        }
    }

    private int GetValueFromDraggable(TaskDraggable draggable)
    {
        var textComp = draggable.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textComp != null && int.TryParse(textComp.text, out int val1)) return val1;
        
        var txt = draggable.GetComponentInChildren<Text>();
        if (txt != null && int.TryParse(txt.text, out int val2)) return val2;
        
        return -1;
    }

    #endregion

    #region Visualization & Validation

    private void UpdateTreeVisuals()
    {
        if (BPlusTreeTaskManager.Instance != null)
        {
            AutoReparentChildren(BPlusTreeTaskManager.Instance.CurrentTree.Root);
            BPlusTreeTaskManager.Instance.RefreshTree();
            StartCoroutine(ValidateAllNodesRoutine());
        }
    }

    private void AutoReparentChildren(BPlusTreeNode<int, string> root)
    {
        if (root == null || root.IsLeaf) return;

        // 1. Collect all internal nodes and leaf nodes level by level
        Dictionary<int, List<BPlusTreeNode<int, string>>> nodesByLevel = new Dictionary<int, List<BPlusTreeNode<int, string>>>();
        CollectNodesByLevel(root, 0, nodesByLevel);

        // 2. Process from bottom up (excluding leaves at the very bottom)
        int maxDepth = 0;
        foreach (var depth in nodesByLevel.Keys)
        {
            if (depth > maxDepth) maxDepth = depth;
        }

        for (int depth = maxDepth - 1; depth >= 0; depth--)
        {
            if (!nodesByLevel.ContainsKey(depth) || !nodesByLevel.ContainsKey(depth + 1)) continue;

            List<BPlusTreeNode<int, string>> parents = nodesByLevel[depth];
            List<BPlusTreeNode<int, string>> children = nodesByLevel[depth + 1];

            if (parents.Count == 0 || children.Count == 0) continue;

            // Sort parents and children by their first key (or subtree min)
            parents.Sort((a, b) => GetNodeMinKey(a).CompareTo(GetNodeMinKey(b)));
            children.Sort((a, b) => GetNodeMinKey(a).CompareTo(GetNodeMinKey(b)));

            // Clear existing children links for these parents
            foreach (var p in parents)
            {
                p.Children.Clear();
            }

            // Re-assign children to parents based on keys
            int childIndex = 0;
            for (int pIndex = 0; pIndex < parents.Count; pIndex++)
            {
                var parent = parents[pIndex];
                
                // A parent can have (Keys.Count + 1) children
                int maxChildrenForThisParent = parent.Keys.Count + 1;
                
                // If it's the last parent, it takes all remaining children
                if (pIndex == parents.Count - 1)
                {
                    while (childIndex < children.Count)
                    {
                        parent.Children.Add(children[childIndex]);
                        children[childIndex].Parent = parent;
                        childIndex++;
                    }
                }
                else
                {
                    // Assign up to maxChildrenForThisParent
                    int assigned = 0;
                    while (childIndex < children.Count && assigned < maxChildrenForThisParent)
                    {
                        parent.Children.Add(children[childIndex]);
                        children[childIndex].Parent = parent;
                        childIndex++;
                        assigned++;
                    }
                }
            }
        }
    }

    private void CollectNodesByLevel(BPlusTreeNode<int, string> node, int depth, Dictionary<int, List<BPlusTreeNode<int, string>>> dict)
    {
        if (node == null) return;

        if (!dict.ContainsKey(depth))
        {
            dict[depth] = new List<BPlusTreeNode<int, string>>();
        }
        dict[depth].Add(node);

        if (!node.IsLeaf && node.Children != null)
        {
            // Create a copy of children list to iterate safely since we might modify it later
            var childrenCopy = new List<BPlusTreeNode<int, string>>(node.Children);
            foreach (var child in childrenCopy)
            {
                CollectNodesByLevel(child, depth + 1, dict);
            }
        }
    }

    private int GetNodeMinKey(BPlusTreeNode<int, string> node)
    {
        if (node.IsLeaf)
        {
            return node.Keys.Count > 0 ? node.Keys[0] : int.MaxValue;
        }
        else
        {
            return node.Children.Count > 0 ? GetNodeMinKey(node.Children[0]) : int.MaxValue;
        }
    }

    private IEnumerator ValidateAllNodesRoutine()
    {
        yield return new WaitForEndOfFrame();
        
        BPlusTreeVisualNode[] allNodes = FindObjectsOfType<BPlusTreeVisualNode>();
        foreach(var node in allNodes)
        {
            CheckUnderflow(node);
        }
    }

    private void CheckUnderflow(BPlusTreeVisualNode node)
    {
        if (node == null || node.CoreNode == null) return;
        
        int order = BPlusTreeTaskManager.Instance.treeOrder;
        int minKeys = (int)Mathf.Ceil((order - 1) / 2.0f);
        
        bool isUnderflow = node.CoreNode.Keys.Count < minKeys;
        
        // Conditional Highlight
        if (isUnderflow && ShouldHighlightError())
        {
            node.SetHighlight(true);
        }
        else
        {
            node.SetHighlight(false); 
        }
    }

    private bool ShouldHighlightError()
    {
        DungeonGenerator dungeonGen = FindObjectOfType<DungeonGenerator>();
        if (dungeonGen != null)
        {
            return dungeonGen.difficultyMode == DungeonGenerator.DifficultyMode.Tutorial;
        }
        return true; 
    }

    #endregion
}

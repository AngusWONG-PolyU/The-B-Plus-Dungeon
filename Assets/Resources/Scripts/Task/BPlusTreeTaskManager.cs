using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Random = UnityEngine.Random;

// Define the types of tasks available
public enum BPlusTreeTaskType
{
    Deletion,
    Insertion
}

// Interface for anything that triggers a task (Enemy and Door)
public interface ITaskTrigger
{
    void OnTaskComplete(bool success);
}

public class BPlusTreeTaskManager : MonoBehaviour
{
    public static BPlusTreeTaskManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject taskCanvas; 
    public GameObject bufferArea;
    public BPlusTreeVisualizer treeVisualizer;
    public BPlusTreeVisualizer Visualizer => treeVisualizer;
    public TextMeshProUGUI taskTitleText; 
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI treeOrderText;
    public TextMeshProUGUI submitButtonText;

    [Header("Confirmation UI")]
    public GameObject confirmationPanel;
    
    [Header("Game Settings")]
    public int treeOrder = 3; // Default fallback
    
    private BPlusTree<int, string> _currentTree;
    public BPlusTree<int, string> CurrentTree => _currentTree;

    private ITaskTrigger _currentTrigger; 
    private BPlusTreeTaskType _currentTaskType;
    public BPlusTreeTaskType CurrentTaskType => _currentTaskType;
    
    // Fields for validation
    private List<int> _targetKeys = new List<int>();
    public List<int> TargetKeys => _targetKeys;
    private HashSet<int> _initialKeys;

    // Expose method to update root (for manual manipulation like CopyUp triggering root split)
    public void UpdateTreeRoot(BPlusTreeNode<int, string> newRoot)
    {
        if (_currentTree != null)
        {
            _currentTree.Root = newRoot;
            RefreshTree();
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if(taskCanvas) taskCanvas.SetActive(false);
        if(bufferArea) bufferArea.SetActive(false);
        if(confirmationPanel) confirmationPanel.SetActive(false);
    }

    public void StartTask(ITaskTrigger trigger, BPlusTreeTaskType taskType)
    {
        _currentTrigger = trigger;
        _currentTaskType = taskType;
        
        if(taskCanvas) taskCanvas.SetActive(true);
        if(bufferArea) bufferArea.SetActive(taskType == BPlusTreeTaskType.Insertion);

        if(timerText) 
        {
            timerText.gameObject.SetActive(true);
            timerText.color = Color.black;
            timerText.text = "";
        }

        if(treeOrderText)
        {
            treeOrderText.gameObject.SetActive(true);
            treeOrderText.color = Color.black;
            treeOrderText.text = "";
        }
        
        if(taskTitleText)
        {
            switch(taskType)
            {
                case BPlusTreeTaskType.Deletion: taskTitleText.text = "Delete the Key to Unravel the Spell!"; break;
                case BPlusTreeTaskType.Insertion: taskTitleText.text = "Insert the Key to Unlock the Door!"; break;
            }
        }

        if(submitButtonText)
        {
            submitButtonText.text = "Finish Unraveling";
        }

        _inResultPhase = false;
        _lastResultSuccess = false;

        GenerateTaskData(taskType);
    }

    private void GenerateTaskData(BPlusTreeTaskType type)
    {
        // 1. Determine Difficulty settings from DungeonGenerator
        int keysParams = 7; // Default
        
        DungeonGenerator dungeonGen = FindObjectOfType<DungeonGenerator>();
        if(dungeonGen != null)
        {
             // Use the Generator's order to keep consistency
             treeOrder = dungeonGen.treeOrder > 0 ? dungeonGen.treeOrder : 3;
             
             // Scale keys based on difficulty
             switch(dungeonGen.difficultyMode) {
                 case DungeonGenerator.DifficultyMode.Tutorial: keysParams = 10; break;
                 case DungeonGenerator.DifficultyMode.Easy: keysParams = Random.Range(10, 16); break;
                 case DungeonGenerator.DifficultyMode.Standard: keysParams = Random.Range(15, 21); break;
                 case DungeonGenerator.DifficultyMode.Hard: keysParams = Random.Range(20, 26); break;
             }
        }

        // Update Tree Order Text
        if(treeOrderText != null)
        {
            treeOrderText.text = $"Order: {treeOrder}";
        }

        // 2. Create the tree
        _currentTree = new BPlusTree<int, string>(treeOrder);
        
        // 3. Generate Random Keys
        // Ensure no duplicates
        _initialKeys = new HashSet<int>();
        List<int> keysToInsert = new List<int>();
        
        while(keysToInsert.Count < keysParams)
        {
            int r = Random.Range(1, 100);
            if(!_initialKeys.Contains(r))
            {
                _initialKeys.Add(r);
                keysToInsert.Add(r);
                _currentTree.Insert(r, "val-" + r);
            }
        }

        // 4. Determine Target Key(s) based on Initial State
        _targetKeys.Clear();
        
        int numTargets = 1;
        if (dungeonGen != null)
        {
            if (dungeonGen.difficultyMode == DungeonGenerator.DifficultyMode.Standard) numTargets = 2;
            else if (dungeonGen.difficultyMode == DungeonGenerator.DifficultyMode.Hard) numTargets = 3;
        }

        if (type == BPlusTreeTaskType.Deletion)
        {
            // Pick existing keys to delete
            List<int> existingKeys = new List<int>(_initialKeys);
            for (int i = 0; i < numTargets && existingKeys.Count > 0; i++)
            {
                int idx = Random.Range(0, existingKeys.Count);
                _targetKeys.Add(existingKeys[idx]);
                existingKeys.RemoveAt(idx);
            }
            
            string keysStr = string.Join(", ", _targetKeys);
            if(taskTitleText) taskTitleText.text = $"Delete Key(s) {keysStr} to Unravel the Spell!";
        }
        else // Insertion
        {
            // Pick NEW keys to insert
            for (int i = 0; i < numTargets; i++)
            {
                int candidate = Random.Range(1, 100);
                while(_initialKeys.Contains(candidate) || _targetKeys.Contains(candidate))
                {
                    candidate = Random.Range(1, 100);
                }
                _targetKeys.Add(candidate);
            }
            
            string keysStr = string.Join(", ", _targetKeys);
            if(taskTitleText) taskTitleText.text = $"Insert Key(s) {keysStr} to Unlock the Door!";
            
            // Spawn keys in buffer area
            if (bufferArea != null && treeVisualizer != null && treeVisualizer.nodePrefab != null)
            {
                // Clear existing keys in buffer area
                foreach (Transform child in bufferArea.transform)
                {
                    Destroy(child.gameObject);
                }
                
                GameObject keyPrefab = treeVisualizer.nodePrefab.GetComponent<BPlusTreeVisualNode>().keyPrefab;
                if (keyPrefab != null)
                {
                    // Instantiate keys in reverse order so first-target appears on top
                    for (int i = _targetKeys.Count - 1; i >= 0; i--)
                    {
                        int key = _targetKeys[i];
                        GameObject k = Instantiate(keyPrefab, bufferArea.transform);
                        TextMeshProUGUI t = k.GetComponentInChildren<TextMeshProUGUI>();
                        if(t) t.text = key.ToString();

                        // Ensure square shape via LayoutElement if not present
                        LayoutElement le = k.GetComponent<LayoutElement>();
                        if (le == null) le = k.AddComponent<LayoutElement>();

                        if (le.preferredWidth <= 0) le.preferredWidth = 50f;
                        if (le.preferredHeight <= 0) le.preferredHeight = 50f;
                        le.minWidth = 50f;
                        le.minHeight = 50f;

                        // Highlight the key for insertion
                        Image img = k.GetComponent<Image>();
                        if (img != null)
                        {
                            img.color = new Color(0.5f, 0.8f, 0.5f, 1f); // Darker green highlight
                        }

                        Outline outline = k.GetComponent<Outline>();
                        if (outline == null) outline = k.AddComponent<Outline>();
                        outline.effectColor = new Color(0.1f, 0.4f, 0.1f, 1f); // Dark green outline
                        outline.effectDistance = new Vector2(3, -3);
                    }
                }
            }
        }

        // 5. Visualize it
        RefreshTree();
    }

    public void UpdateTaskTimer(float remainingTime, float totalDuration)
    {
        if(timerText != null) 
        {
            timerText.text = $"Time: {remainingTime:F1}s";
            if(remainingTime <= totalDuration / 2f) timerText.color = Color.red;
            else timerText.color = Color.black;
        }
    }

    public void RefreshTree()
    {
        if(treeVisualizer) treeVisualizer.RenderTree(_currentTree);
    }
    
    public bool CheckTreeStatus(BPlusTree<int, string> tree)
    {
        // 1. Construct the Expected Set of Keys
        HashSet<int> expectedKeys = new HashSet<int>(_initialKeys);

        if (_currentTaskType == BPlusTreeTaskType.Insertion)
        {
            foreach (int key in _targetKeys)
            {
                expectedKeys.Add(key);
            }
        }
        else if (_currentTaskType == BPlusTreeTaskType.Deletion)
        {
            foreach (int key in _targetKeys)
            {
                expectedKeys.Remove(key);
            }
        }

        // 2. Collect Actual Keys from the Tree
        List<int> actualKeys = new List<int>();
        CollectKeys(tree.Root, actualKeys);

        // 3. Compare Count
        if (actualKeys.Count != expectedKeys.Count)
        {
            Debug.Log($"Validation Failed: Content Mismatch. Expected {expectedKeys.Count} keys, found {actualKeys.Count}.");
            return false;
        }

        // 4. Compare Content
        foreach (int key in actualKeys)
        {
            if (!expectedKeys.Contains(key))
            {
                Debug.Log($"Validation Failed: Found unexpected key {key}.");
                return false;
            }
        }

        // 5. Structural Validation
        if (!tree.ValidateTree()) 
        {
             Debug.Log("Validation Failed: Tree internal structure is invalid (ValidateTree failed).");
             return false;
        }

        // 6. Internal Keys Consistency
        if (!CheckInternalKeysConsistency(tree.Root, out string error))
        {
            Debug.Log($"Validation Failed: Internal Node Consistency. {error}");
            return false;
        }

        // 7. Leaf Sequence Order
        if (!CheckLeafSequenceOrder(tree.FirstLeaf, out error))
        {
            Debug.Log($"Validation Failed: Leaf Sequence Order. {error}");
            return false;
        }

        return true;
    }

    public bool CheckTreeStatusAndHighlight(BPlusTree<int, string> tree)
    {
        bool overallValid = true;

        if (tree.Root == null) return false;

        // 1. Construct the Expected Set of Keys
        HashSet<int> expectedKeys = new HashSet<int>(_initialKeys);

        if (_currentTaskType == BPlusTreeTaskType.Insertion)
        {
            foreach (int key in _targetKeys) expectedKeys.Add(key);
        }
        else if (_currentTaskType == BPlusTreeTaskType.Deletion)
        {
            foreach (int key in _targetKeys) expectedKeys.Remove(key);
        }

        // 2. Validate nodes recursively
        int minKeys = (int)Math.Ceiling(tree.Order / 2.0) - 1;
        int leafLevel = -1;
        
        ValidateNodeAndHighlight(tree.Root, tree.Order, minKeys, 0, ref leafLevel, expectedKeys, ref overallValid);

        // Check if there are missing keys or extra keys overall not caught by node validation
        List<int> actualKeys = new List<int>();
        CollectKeys(tree.Root, actualKeys);
        if (actualKeys.Count != expectedKeys.Count) 
        {
            Debug.Log($"Validation Failed: Overall Content Mismatch. Expected {expectedKeys.Count}, found {actualKeys.Count}.");
            overallValid = false;
        }

        return overallValid;
    }

    private void ValidateNodeAndHighlight(BPlusTreeNode<int, string> node, int order, int minKeys, int level, ref int leafLevel, HashSet<int> expectedKeys, ref bool overallValid)
    {
        string errorMsg = "";
        bool nodeValid = true;

        // Visual node reference (if available)
        BPlusTreeVisualNode visualNode = null;
        if (treeVisualizer != null && treeVisualizer.NodeMap != null && treeVisualizer.NodeMap.ContainsKey(node))
        {
            visualNode = treeVisualizer.NodeMap[node].GetComponent<BPlusTreeVisualNode>();
        }

        // 1. Check Key Count limitations
        if (node != _currentTree.Root && node.Keys.Count < minKeys)
        {
            nodeValid = false;
            errorMsg += $"Below minimum keys (has {node.Keys.Count}, needs {minKeys}).\n";
        }
        if (node.Keys.Count > order - 1)
        {
            nodeValid = false;
            errorMsg += $"Exceeds maximum keys (has {node.Keys.Count}, max {order - 1}).\n";
        }

        // 2. Check keys sorted
        for (int i = 0; i < node.Keys.Count - 1; i++)
        {
            if (node.Keys[i].CompareTo(node.Keys[i + 1]) >= 0)
            {
                nodeValid = false;
                errorMsg += "Keys are not sorted correctly.\n";
                break;
            }
        }

        if (node.IsLeaf)
        {
            // Level check
            if (leafLevel == -1) leafLevel = level;
            else if (leafLevel != level)
            {
                nodeValid = false;
                errorMsg += "Leaf is not at the correct uniform depth.\n";
            }

            // Expected content check
            foreach (int k in node.Keys)
            {
                if (!expectedKeys.Contains(k))
                {
                    nodeValid = false;
                    errorMsg += $"Contains unexpected key: {k}.\n";
                }
            }

            // Next pointer increasing order
            if (node.Next != null && node.Keys.Count > 0 && node.Next.Keys.Count > 0)
            {
                if (node.Keys[node.Keys.Count - 1] >= node.Next.Keys[0])
                {
                    nodeValid = false;
                    errorMsg += "Keys sequence with next leaf is broken.\n";
                }
            }
        }
        else
        {
            // Internal Node Checks
            if (node.Children.Count != node.Keys.Count + 1)
            {
                nodeValid = false;
                errorMsg += $"Mismatched children count: has {node.Keys.Count} keys but {node.Children.Count} children.\n";
            }

            for (int i = 0; i < node.Keys.Count; i++)
            {
                int internalKey = node.Keys[i];
                if (i + 1 < node.Children.Count)
                {
                    int rightSubtreeMin = GetSubtreeMin(node.Children[i + 1]);
                    if (internalKey != rightSubtreeMin)
                    {
                        nodeValid = false;
                        errorMsg += $"Internal routing key {internalKey} must equal min of right subtree ({rightSubtreeMin}).\n";
                    }
                }
            }

            // Validate children
            foreach (var child in node.Children)
            {
                if (child.Parent != node)
                {
                    nodeValid = false; // We just flag overall tree valid false but difficult to highlight child-parent mismatch cleanly on parent
                    errorMsg += "A child node has an incorrect parent pointer.\n";
                }
                ValidateNodeAndHighlight(child, order, minKeys, level + 1, ref leafLevel, expectedKeys, ref overallValid);
            }
        }

        if (!nodeValid) overallValid = false;
        
        if (visualNode != null)
        {
            visualNode.SetResultHighlight(nodeValid, errorMsg.Trim());
        }
    }

    private bool CheckLeafSequenceOrder(BPlusTreeNode<int, string> firstLeaf, out string error)
    {
        error = "";
        if (firstLeaf == null) return true;

        BPlusTreeNode<int, string> current = firstLeaf;
        int lastKey = int.MinValue;

        while (current != null)
        {
            foreach (int key in current.Keys)
            {
                if (key <= lastKey)
                {
                    error = $"Leaf keys are not in strictly increasing order. Found {key} after {lastKey}.";
                    return false;
                }
                lastKey = key;
            }
            current = current.Next;
        }

        return true;
    }

    private bool CheckInternalKeysConsistency(BPlusTreeNode<int, string> node, out string error)
    {
        error = "";
        if (node.IsLeaf) return true;

        for (int i = 0; i < node.Keys.Count; i++)
        {
            int internalKey = node.Keys[i];
            
            if (i + 1 >= node.Children.Count)
            {
                error = $"Internal Node has {node.Keys.Count} keys but only {node.Children.Count} children.";
                return false;
            }

            // Get the minimum key of the right child subtree
            int rightSubtreeMin = GetSubtreeMin(node.Children[i+1]);

            // Check if the internal key matches the minimum key of the right subtree
            if (internalKey != rightSubtreeMin)
            {
                 error = $"Internal Key {internalKey} does not match Min of Right Subtree ({rightSubtreeMin}). Did you forget to update the parent?";
                 return false;
            }
        }

        foreach (var child in node.Children)
        {
            if (!CheckInternalKeysConsistency(child, out error)) return false;
        }

        return true;
    }

    public int GetSubtreeMin(BPlusTreeNode<int, string> node)
    {
        if (node.IsLeaf)
        {
            return node.Keys.Count > 0 ? node.Keys[0] : int.MaxValue; 
        }
        else
        {
            return node.Children.Count > 0 ? GetSubtreeMin(node.Children[0]) : int.MaxValue;
        }
    }

    private void CollectKeys(BPlusTreeNode<int, string> node, List<int> results)
    {
        if (node == null) return;

        // If it's a leaf, add its keys
        if (node.IsLeaf)
        {
            results.AddRange(node.Keys);
        }
        else
        {
            // If internal, traverse children
            foreach (var child in node.Children)
            {
                CollectKeys(child, results);
            }
        }
    }

    public void CloseTask(bool success)
    {
        if(taskCanvas) taskCanvas.SetActive(false);
        if(timerText) timerText.gameObject.SetActive(false);
        if(treeOrderText) treeOrderText.gameObject.SetActive(false);
        
        // Hide the context menu when closing the task canvas
        if (TaskContextMenu.Instance != null)
        {
            TaskContextMenu.Instance.HideMenu();
        }
        
        // Resume game time
        Time.timeScale = 1f;

        if(_currentTrigger != null)
        {
            _currentTrigger.OnTaskComplete(success);
        }
    }

    // Result phase state
    private bool _inResultPhase = false;
    public bool IsInResultPhase => _inResultPhase;
    private bool _lastResultSuccess = false;

    // Methods for task submission confirmation
    public void ConfirmToSubmitTask()
    {
        if (_inResultPhase)
        {
            // If already in result phase, user presses button again to close
            CloseTaskPhase();
            return;
        }

        // Pause time while confirming
        Time.timeScale = 0f;

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
        }
        else
        {
            // Fallback if no confirmation panel is set
            ConfirmSubmit();
        }
    }

    public void ConfirmSubmit()
    {
        // Hide confirmation panel
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        // Hide any open context menus so they don't linger during result phase
        if (TaskContextMenu.Instance != null)
        {
            TaskContextMenu.Instance.HideMenu();
        }

        // Perform the check
        _lastResultSuccess = CheckTreeStatusAndHighlight(_currentTree);
        
        // Enter Result Phase
        _inResultPhase = true;

        if (submitButtonText != null)
        {
            submitButtonText.text = "Complete Ritual";
        }

        if (taskTitleText != null)
        {
            taskTitleText.text = _lastResultSuccess ? "Correct! Well done!" : "Incorrect Structure! Hover over red nodes to see why.";
            taskTitleText.color = _lastResultSuccess ? new Color(0.1f, 0.6f, 0.1f) : Color.red;
        }
    }

    public void CloseTaskPhase()
    {
        // Handle specific gameplay penalties for failure right before closing
        if (!_lastResultSuccess && _currentTrigger is EnemyController enemyController)
        {
            enemyController.ForceFinishChant();
        }

        // Actually close the task and restore time
        CloseTask(_lastResultSuccess);
    }

    public void CancelSubmit()
    {
        if (confirmationPanel != null) 
            confirmationPanel.SetActive(false);
        
        Time.timeScale = 1f; // Restore time if cancelled
    }
}
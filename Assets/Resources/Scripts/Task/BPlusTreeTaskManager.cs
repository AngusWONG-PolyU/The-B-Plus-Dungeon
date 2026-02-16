using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public BPlusTreeVisualizer treeVisualizer;
    public TextMeshProUGUI taskTitleText; 

    [Header("Confirmation UI")]
    public GameObject confirmationPanel;
    
    [Header("Game Settings")]
    public int treeOrder = 3; // Default fallback
    
    private BPlusTree<int, string> _currentTree;
    private ITaskTrigger _currentTrigger; 
    private BPlusTreeTaskType _currentTaskType;
    
    // Fields for validation
    private int _targetKey;
    private HashSet<int> _initialKeys;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if(taskCanvas) taskCanvas.SetActive(false);
        if(confirmationPanel) confirmationPanel.SetActive(false);
    }

    public void StartTask(ITaskTrigger trigger, BPlusTreeTaskType taskType)
    {
        _currentTrigger = trigger;
        _currentTaskType = taskType;
        
        if(taskCanvas) taskCanvas.SetActive(true);
        
        if(taskTitleText)
        {
            switch(taskType)
            {
                case BPlusTreeTaskType.Deletion: taskTitleText.text = "Delete the Key to Break the Spell!"; break;
                case BPlusTreeTaskType.Insertion: taskTitleText.text = "Insert the Key to Unlock the Door!"; break;
            }
        }

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

        // 4. Determine Target Key based on Initial State
        if (type == BPlusTreeTaskType.Deletion)
        {
            // Pick an existing key to delete
            List<int> existingKeys = new List<int>(_initialKeys);
            _targetKey = existingKeys[Random.Range(0, existingKeys.Count)];
            
            if(taskTitleText) taskTitleText.text = $"Delete Key {_targetKey} to Break the Spell!";
        }
        else // Insertion
        {
            // Pick a NEW key to insert
            int candidate = Random.Range(1, 100);
            while(_initialKeys.Contains(candidate))
            {
                candidate = Random.Range(1, 100);
            }
            _targetKey = candidate;
            
            if(taskTitleText) taskTitleText.text = $"Insert Key {_targetKey} to Unlock the Door!";
        }

        // 5. Visualize it
        RefreshTree();
    }

    public void RefreshTree()
    {
        if(treeVisualizer) treeVisualizer.RenderTree(_currentTree);
    }
    
    public bool CheckTreeStatus(BPlusTree<int, string> tree)
    {
        /* Currently, only check for the leaf for simplification and debugging */
        // 1. Construct the Expected Set of Keys
        HashSet<int> expectedKeys = new HashSet<int>(_initialKeys);

        if (_currentTaskType == BPlusTreeTaskType.Insertion)
        {
            expectedKeys.Add(_targetKey);
        }
        else if (_currentTaskType == BPlusTreeTaskType.Deletion)
        {
            expectedKeys.Remove(_targetKey);
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

        // 5. TODO: Structural Validation can go here
        // e.g., checking if the tree is balanced, property order is correct, etc.

        return true;
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
        
        if(_currentTrigger != null)
        {
            _currentTrigger.OnTaskComplete(success);
        }
    }

    // Methods for task submission confirmation
    public void ConfirmToSubmitTask()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            // Fallback if no confirmation panel is set, directly submit
            ConfirmSubmit();
        }
    }

    public void ConfirmSubmit()
    {
        // Restore time
        Time.timeScale = 1f;
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        // Perform the check
        bool success = CheckTreeStatus(_currentTree);
        
        // Handle specific gameplay penalties for failure before closing
        if (!success)
        {
            // Check if the trigger is an enemy, if so, force finish the chant to attack
            if (_currentTrigger is EnemyController enemyController)
            {
                enemyController.ForceFinishChant();
            }
            else
            {
                // TODO: Implement penalty for the incorrect door task, maybe simply close the task and wait for a restart?
            }
        }

        // Close the task
        CloseTask(success);
    }

    public void CancelSubmit()
    {
        if (confirmationPanel != null) 
            confirmationPanel.SetActive(false);
        
        Time.timeScale = 1f;
    }
}
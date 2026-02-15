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
             // Use Generator's order to keep consistency
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
        HashSet<int> usedKeys = new HashSet<int>();
        List<int> keysToInsert = new List<int>();
        
        while(keysToInsert.Count < keysParams)
        {
            int r = Random.Range(1, 100);
            if(!usedKeys.Contains(r))
            {
                usedKeys.Add(r);
                keysToInsert.Add(r);
                _currentTree.Insert(r, "val-" + r);
            }
        }

        // 4. Visualize it
        RefreshTree();
    }

    public void RefreshTree()
    {
        if(treeVisualizer) treeVisualizer.RenderTree(_currentTree);
    }
    
    public void CheckTreeStatus(BPlusTreeVisualNode node)
    {
         // Validate Tree after operations
         // If fully correct -> Win
    }

    public void CloseTask(bool success)
    {
        if(taskCanvas) taskCanvas.SetActive(false);
        
        if(_currentTrigger != null)
        {
            _currentTrigger.OnTaskComplete(success);
        }
    }

    // Methods for task closing confirmation
    public void TryToCloseTask()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            // Fallback if no UI assigned
            CloseTask(false);
        }
    }

    public void ConfirmGiveUp()
    {
        Time.timeScale = 1f;

        // Check if the trigger is an enemy, if so, force finish the chant to attack
        if (_currentTrigger is EnemyController enemyController)
        {
            enemyController.ForceFinishChant();
        }
        else
        {
            // TODO: Implement give up for the door task, maybe simply close the task and wait for restart?
        }

        // 2. Close the task as failed
        CloseTask(false);

        // 3. Hide the confirmation panel
        if (confirmationPanel != null) 
            confirmationPanel.SetActive(false);
    }

    public void CancelGiveUp()
    {
        if (confirmationPanel != null) 
            confirmationPanel.SetActive(false);
        
        Time.timeScale = 1f;
    }
}
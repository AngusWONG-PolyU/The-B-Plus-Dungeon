using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public enum DifficultyMode
    {
        Tutorial,
        Easy,
        Standard,
        Hard
    }

    [Header("Dungeon Settings")]
    public DifficultyMode difficultyMode = DifficultyMode.Tutorial;
    public int numberOfKeys = 15; // Total number of keys to insert into the tree
    public int treeOrder = 4; // B+ Tree order
    
    [Header("References")]
    public DungeonMinimap minimap; // Reference to the minimap
    public DungeonPortalsRoomUpdater roomUpdater; // Reference to the room updater
    
    [Header("UI References")]
    public GameObject heartUI;
    public GameObject skillUI;

    [Header("Player References")]
    public PlayerSkillController playerSkillController;
    public PlayerHealth playerHealth;

    [Header("Generation Settings")]
    public bool generateOnStart = false;
    
    [System.NonSerialized]
    private BPlusTree<int, int> dungeonTree;
    private int targetRoomKey;
    private int[] correctSearchPath;
    private bool isGenerated = false; // Track if dungeon has been generated

    // Level constraints for generation
    private int minLevel;
    private int maxLevel;

    void Start()
    {
        // Find Player references if missing
        if (playerSkillController == null) playerSkillController = FindObjectOfType<PlayerSkillController>();
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();

        // Hide UI and Disable System initially
        SetDungeonActive(false);

        if (generateOnStart)
        {
            GenerateDungeon();
        }
    }

    public void SetDungeonActive(bool active)
    {
        // Update Manager State
        DungeonManager manager = FindObjectOfType<DungeonManager>();
        if (manager != null) manager.isDungeonActive = active;

        // Toggle UI
        if (heartUI != null) heartUI.SetActive(active);
        if (skillUI != null) skillUI.SetActive(active);

        // Toggle Skill System
        if (playerSkillController != null)
        {
            playerSkillController.isSystemActive = active;
        }

        // Toggle Room Updater UI
        if (roomUpdater != null)
        {
            roomUpdater.SetUIVisibility(active);
            if (!active)
            {
                roomUpdater.DeactivateAllPortals();
            }
        }

        // Reset Health and Skill Charges when entering the dungeon
        if (active && playerHealth != null)
        {
            playerHealth.ResetHealth();
            
            // Reset Skill Charges
            PlayerSkillController psc = playerHealth.GetComponent<PlayerSkillController>();
            if (psc != null) psc.ResetAllSkillCharges();
        }
        
        // Reset Movement Speed
        CharacterMovement cm = playerHealth.GetComponent<CharacterMovement>();
        if (cm != null) cm.ResetSpeed();
    }

    // Generate the entire dungeon structure
    public void GenerateDungeon()
    {
        if (isGenerated)
        {
            Debug.Log("Dungeon already generated, skipping...");
            return;
        }
        
        // Activate Dungeon Mode (UI + Skills + Health Reset)
        SetDungeonActive(true);
        
        Debug.Log("=== GenerateDungeon() called ===");
        
        DungeonRoomController dungeonRoomController = FindObjectOfType<DungeonRoomController>();
        if (dungeonRoomController != null)
        {
            dungeonRoomController.ResetBoss();
            dungeonRoomController.ResetItems(); // Reset available items for the new run
        }

        CalculateNumberOfKeys();
        
        int attempts = 0;
        int maxAttempts = 200;
        bool validTreeGenerated = false;
        List<int> generatedKeys = new List<int>();

        while (!validTreeGenerated && attempts < maxAttempts)
        {
            attempts++;
            
            // Step 1: Create B+ Tree
            dungeonTree = new BPlusTree<int, int>(treeOrder);
            
            // Step 2: Insert keys into tree
            generatedKeys = new List<int>();
            HashSet<int> usedKeys = new HashSet<int>();
            
            // Generate unique random keys
            while (generatedKeys.Count < numberOfKeys)
            {
                // Generate a random key between 1 and 100 (or adjust range based on count)
                int maxRange = Mathf.Max(100, numberOfKeys * 5);
                int randomKey = Random.Range(1, maxRange);
                
                if (!usedKeys.Contains(randomKey))
                {
                    usedKeys.Add(randomKey);
                    generatedKeys.Add(randomKey);
                    dungeonTree.Insert(randomKey, randomKey);
                }
            }

            // Check Tree Height
            int currentHeight = dungeonTree.GetHeight();
            if (currentHeight >= minLevel && currentHeight <= maxLevel)
            {
                validTreeGenerated = true;
                Debug.Log($"Valid tree generated on attempt {attempts}. Height: {currentHeight} (Target: {minLevel}-{maxLevel})");
            }
            else
            {
                Debug.LogWarning($"Attempt {attempts}: Generated tree height {currentHeight} is out of range ({minLevel}-{maxLevel}). Regenerating...");
                // Re-roll numberOfKeys within the valid range to try a different count
                CalculateNumberOfKeys();
            }
        }

        if (!validTreeGenerated)
        {
            Debug.LogError($"Failed to generate a valid dungeon tree after {maxAttempts} attempts. Using last generated tree.");
        }

        Debug.Log($"Step 1 & 2: Created B+ Tree with order {treeOrder} and {numberOfKeys} keys.");
        Debug.Log($"Keys: {string.Join(", ", generatedKeys)}");
        
        // Step 3: Choose a random target key from the generated keys
        targetRoomKey = generatedKeys[Random.Range(0, generatedKeys.Count)];
        
        // Step 4: Get the correct search path to the target
        correctSearchPath = dungeonTree.GetSearchIndices(targetRoomKey);
        
        Debug.Log($"Dungeon Generated - Target Room: {targetRoomKey}");
        Debug.Log($"Correct Path: [{string.Join(", ", correctSearchPath)}]");
        
        isGenerated = true; // Mark as generated
        
        // Step 5: Initialize minimap
        if (minimap != null)
        {
            Debug.Log("Step 5: Initializing minimap");
            minimap.InitializeDungeon(dungeonTree, targetRoomKey);
        }
        else
        {
            Debug.LogError("Step 5: Minimap is NULL! Assign it in Inspector.");
        }
        
        // Step 6: Initialize room updater with the tree structure
        if (roomUpdater != null)
        {
            Debug.Log("Step 6: Initializing room updater");
            roomUpdater.Initialize(dungeonTree.Root, correctSearchPath);
            roomUpdater.SetTargetKey(targetRoomKey);
        }
        else
        {
            Debug.LogError("Step 6: RoomUpdater is NULL! Assign it in Inspector.");
        }
    }
    
    // Reset dungeon to allow regeneration
    public void ResetDungeon()
    {
        isGenerated = false;
        dungeonTree = null;
        targetRoomKey = 0;
        correctSearchPath = null;
        
        if (minimap != null)
        {
            minimap.ClearMinimapPublic();
        }
        
        Debug.Log("Dungeon reset - will regenerate on next entry");
    }
    
    // Public getters for other systems to access dungeon data
    public BPlusTree<int, int> GetDungeonTree() => dungeonTree;
    public int GetTargetKey() => targetRoomKey;
    public int[] GetCorrectPath() => correctSearchPath;

    private void CalculateNumberOfKeys()
    {
        minLevel = 3;
        maxLevel = 3;

        switch (difficultyMode)
        {
            case DifficultyMode.Tutorial:
                treeOrder = 3;
                minLevel = 3;
                maxLevel = 3;
                break;
            case DifficultyMode.Easy:
                treeOrder = 3;
                minLevel = 3;
                maxLevel = 4;
                break;
            case DifficultyMode.Standard:
                treeOrder = 4;
                minLevel = 4;
                maxLevel = 5;
                break;
            case DifficultyMode.Hard:
                treeOrder = 5;
                minLevel = 5;
                maxLevel = 6;
                break;
        }

        // Calculate min keys to reach minLevel
        int minKeys = GetMinKeysForHeight(minLevel);

        // Calculate max keys to stay within maxLevel
        int maxKeys = GetMaxKeysForHeight(maxLevel);

        numberOfKeys = Random.Range(minKeys, maxKeys + 1);
        Debug.Log($"Difficulty: {difficultyMode}, Level Range: {minLevel}-{maxLevel}, Keys Range: {minKeys}-{maxKeys}, Selected Keys: {numberOfKeys}");
    }

    private int GetMaxKeysForHeight(int height)
    {
        if (height <= 0) return 0;
        if (height == 1) return treeOrder - 1;
        
        // Max keys = m^(h-1) * (m-1)
        return (int)(Mathf.Pow(treeOrder, height - 1) * (treeOrder - 1));
    }

    private int GetMinKeysForHeight(int height)
    {
        if (height <= 0) return 0;
        if (height == 1) return 1;

        // Min keys = 2 * ceil(m/2)^(h-2) * ceil((m-1)/2)
        int minChildren = (int)System.Math.Ceiling(treeOrder / 2.0);
        int minLeafKeys = (int)System.Math.Ceiling((treeOrder - 1) / 2.0);

        return (int)(2 * Mathf.Pow(minChildren, height - 2) * minLeafKeys);
    }

    public float GetTaskTimeMultiplier()
    {
        switch (difficultyMode)
        {
            case DifficultyMode.Tutorial:
                return 1.5f; // Slower for Tutorial
            case DifficultyMode.Easy:
            case DifficultyMode.Standard:
            case DifficultyMode.Hard:
                return 1.0f; // Normal time for all standard modes
            default:
                return 1.0f;
        }
    }
}

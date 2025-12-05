using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int numberOfKeys = 15; // Total number of keys to insert into the tree
    public int treeOrder = 4; // B+ Tree order
    
    [Header("References")]
    public DungeonMinimap minimap; // Reference to the minimap
    public DungeonRoomUpdater roomUpdater; // Reference to the room updater
    
    [Header("Generation Settings")]
    public bool generateOnStart = false;
    
    [System.NonSerialized]
    private BPlusTree<int, int> dungeonTree;
    private int targetRoomKey;
    private int[] correctSearchPath;
    private bool isGenerated = false; // Track if dungeon has been generated

    void Start()
    {
        if (generateOnStart)
        {
            GenerateDungeon();
        }
    }

    // Generate the entire dungeon structure
    public void GenerateDungeon()
    {
        if (isGenerated)
        {
            Debug.Log("Dungeon already generated, skipping...");
            return;
        }
        
        Debug.Log("=== GenerateDungeon() called ===");
        
        // Step 1: Create B+ Tree
        dungeonTree = new BPlusTree<int, int>(treeOrder);
        Debug.Log($"Step 1: Created B+ Tree with order {treeOrder}");
        
        // Step 2: Insert keys into tree
        List<int> generatedKeys = new List<int>();
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
        Debug.Log($"Step 2: Inserted {numberOfKeys} random keys into tree: {string.Join(", ", generatedKeys)}");
        
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
        Debug.Log("Dungeon reset - will regenerate on next entry");
    }
    
    // Public getters for other systems to access dungeon data
    public BPlusTree<int, int> GetDungeonTree() => dungeonTree;
    public int GetTargetKey() => targetRoomKey;
    public int[] GetCorrectPath() => correctSearchPath;
}

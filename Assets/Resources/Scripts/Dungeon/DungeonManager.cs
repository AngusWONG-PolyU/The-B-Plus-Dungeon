using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handles dungeon game logic: hearts, win/lose conditions, room progression
public class DungeonManager : MonoBehaviour
{
    [Header("References")]
    public DungeonGenerator dungeonGenerator;
    public PlayerHealth playerHealth;

    [Header("Spawn Points")]
    public Transform portalsRoomSpawn; // Spawn point in the Portals Room
    public Transform leafRoomSpawn; // Spawn point in the Leaf Room

    // Reset dungeon state
    public void ResetDungeon()
    {
        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }
        
        Debug.Log("Dungeon state reset");
    }
    
    void Start()
    {
        // Subscribe to the death event
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath.AddListener(OnPlayerDied);
        }
    }
    
    public void OnPlayerDied()
    {
        Debug.Log("Player died - Exiting Dungeon Mode");
        if (dungeonGenerator != null)
        {
            dungeonGenerator.SetDungeonActive(false);
            dungeonGenerator.ResetDungeon();
        }
    }
    
    // Called when the player chooses the wrong portal
    public void OnWrongPortal()
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(1);
        }
        else
        {
            Debug.LogWarning("PlayerHealth not assigned in DungeonManager!");
        }
    }
    
    // Called when the player reaches a leaf node
    public void OnReachedLeaf(BPlusTreeNode<int, int> leafNode)
    {
        // Fetch the current target key dynamically
        int currentTargetKey = (dungeonGenerator != null) ? dungeonGenerator.GetTargetKey() : -1;
        
        bool foundTarget = leafNode.Keys.Contains(currentTargetKey);
        if (foundTarget)
        {
            Debug.Log("SUCCESS! Target room found!");
            OnTargetFound();
        }
        else
        {
            Debug.LogWarning($"Reached leaf but no target (Target: {currentTargetKey}) - this shouldn't happen!");
        }
    }
    
    // Called when the target is found
    private void OnTargetFound()
    {
        Debug.Log("Dungeon Complete! You found the target!");
        
        // TODO: Show victory UI, rewards, etc.
    }
    
    // Called when the player runs out of hearts
    private void OnGameOver()
    {
        Debug.Log("Game Over! No hearts remaining.");
        // TODO: Show game over UI, restart option, etc.
    }
}

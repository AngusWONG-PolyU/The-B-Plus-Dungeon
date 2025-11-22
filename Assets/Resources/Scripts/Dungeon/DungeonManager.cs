using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handles dungeon game logic: hearts, win/lose conditions, room progression
public class DungeonManager : MonoBehaviour
{
    [Header("Player Stats")]
    public int playerHearts = 3;
    
    [Header("References")]
    public DungeonGenerator dungeonGenerator;
    
    private int targetKey;
    
    void Start()
    {
        // Get target key from generator after dungeon is generated
        if (dungeonGenerator != null)
        {
            targetKey = dungeonGenerator.GetTargetKey();
        }
    }
    
    // Called when player chooses wrong portal
    public void OnWrongPortal()
    {
        playerHearts--;
        Debug.Log($"Lost 1 heart. Remaining hearts: {playerHearts}");
        
        // TODO: Update UI hearts display here
        // heartUI.RemoveHeart();
        
        if (playerHearts <= 0)
        {
            OnGameOver();
        }
    }
    
    // Called when player reaches a leaf node
    public void OnReachedLeaf(BPlusTreeNode<int, int> leafNode)
    {
        bool foundTarget = leafNode.Keys.Contains(targetKey);
        if (foundTarget)
        {
            Debug.Log("SUCCESS! Target room found!");
            OnTargetFound();
        }
        else
        {
            Debug.LogWarning("Reached leaf but no target - this shouldn't happen!");
        }
    }
    
    // Called when target is found
    private void OnTargetFound()
    {
        Debug.Log("Dungeon Complete! You found the target!");
        
        // TODO: Show victory UI, rewards, etc.
    }
    
    // Called when player runs out of hearts
    private void OnGameOver()
    {
        Debug.Log("Game Over! No hearts remaining.");
        // TODO: Show game over UI, restart option, etc.
    }
}
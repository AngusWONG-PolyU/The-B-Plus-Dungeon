using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonRoomController : MonoBehaviour
{
    [Header("Room Generation")]
    [Range(0f, 1f)]
    public float enemyRoomProbability = 0.8f;
    public DungeonManager dungeonManager;

    [Header("Enemies")]
    public List<GameObject> enemies; // Enemies present in this room prefab
    public GameObject boss; // Boss present in this room prefab

    [Header("Items")]
    public List<GameObject> items;
    private List<GameObject> availableItems; // Available items for the current run

    [Header("Room Settings")]
    public bool isCleared = false;
    public bool lockDoorsOnEntry = true;

    [Header("References")]
    public List<DungeonStaircase> staircases; // Reference to all staircases in this room
    public GameObject activeEnemy; // Currently active enemy
    public List<GameObject> doors = new List<GameObject>();

    private void Start()
    {
        // Find DungeonManager if not assigned
        if (dungeonManager == null)
        {
            dungeonManager = FindObjectOfType<DungeonManager>();
        }

        // At start, disable all the enemies and items.
        DisableAllContent();
        
        // Initialize available items
        if (availableItems == null) ResetItems();
    }

    public void ResetItems()
    {
        if (items != null)
        {
            availableItems = new List<GameObject>(items);
            Debug.Log($"[DungeonRoomController] Reset items list. Count: {availableItems.Count}");
        }
    }

    public void ResetBoss()
    {
        // 1. If there is a currently assigned boss, put it back into the enemies pool
        if (boss != null)
        {
            if (enemies == null) enemies = new List<GameObject>();
            
            // Add back to pool
            enemies.Add(boss);
            
            boss = null;
        }

        // 2. Pick a new randomly selected boss from the enemies pool
        if (enemies != null && enemies.Count > 0)
        {
            int index = Random.Range(0, enemies.Count);
            boss = enemies[index];
            enemies.RemoveAt(index);
            Debug.Log($"[DungeonRoomController] Assigned {boss.name} as new Boss and removed from enemies pool.");
        }
        else
        {
            Debug.LogWarning("[DungeonRoomController] No enemies available to assign as Boss!");
        }
    }

    public void InitializeRoom(bool isBossRoom = false)
    {
        isCleared = false;
        DisableAllContent();
        
        // Ensure doors are reset
        LockDoors();
        
        ConfigureStaircase(isBossRoom);

        if (isBossRoom)
        {
            SetupBossRoom();
        }
        else
        {
            // Use a probability to control the room type
            float roll = Random.value;
            if (roll < enemyRoomProbability)
            {
                SetupEnemyRoom();
            }
            else
            {
                SetupItemRoom();
            }
        }
    }

    private void ConfigureStaircase(bool isBossRoom)
    {
        if (staircases == null || staircases.Count == 0)
        {
            staircases = new List<DungeonStaircase>(GetComponentsInChildren<DungeonStaircase>());
        }

        foreach (var staircase in staircases)
        {
            if (staircase != null && dungeonManager != null)
            {
                if (isBossRoom)
                {
                    staircase.teleportDestination = dungeonManager.leafRoomSpawn;
                    staircase.dungeonName = "Leaf Room";
                    Debug.Log("Staircase configured for Boss Room -> Leaf Room");
                }
                else
                {
                    staircase.teleportDestination = dungeonManager.portalsRoomSpawn;
                    staircase.dungeonName = "Portals Room";
                    Debug.Log("Staircase configured for Normal Room -> Portals Room");
                }
            }
        }
        
        if (staircases.Count > 0)
        {
            Debug.Log($"Configured {staircases.Count} staircases for {(isBossRoom ? "Boss Room" : "Normal Room")}");
        }
    }

    private void DisableAllContent()
    {
        // Disable all enemies
        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null) enemy.SetActive(false);
            }
        }

        // Disable boss
        if (boss != null)
        {
            boss.SetActive(false);
        }

        // Disable items
        if (items != null)
        {
            foreach (var item in items)
            {
                if (item != null) item.SetActive(false);
            }
        }

        activeEnemy = null;
    }

    private void SetupBossRoom()
    {
        if (boss != null)
        {
            boss.SetActive(true);
            
            // Reset the boss
            EnemyController ec = boss.GetComponent<EnemyController>();
            if (ec != null)
            {
                ec.isBoss = true;
                ec.maxHealth = 5;
                ec.ResetEnemy();

                if (dungeonManager != null && dungeonManager.dungeonGenerator != null)
                {
                    ec.SetChantDurationMultiplier(dungeonManager.dungeonGenerator.GetChantDurationMultiplier());
                    Debug.Log($"[DungeonRoomController] Boss chant duration set to {ec.chantDuration}");
                }
            }

            activeEnemy = boss;
            Debug.Log($"Boss Room Setup: Activated Boss {boss.name}");
        }
        else
        {
            Debug.LogWarning("No Boss assigned in DungeonRoomController! Falling back to enemy room.");
            SetupEnemyRoom();
        }
    }

    private void SetupEnemyRoom()
    {
        if (enemies == null || enemies.Count == 0)
        {
            Debug.LogWarning("No enemies available for Enemy Room!");
            SetupItemRoom();
            return;
        }

        // Pick a random enemy
        int index = Random.Range(0, enemies.Count);
        GameObject selectedEnemy = enemies[index];
        
        if (selectedEnemy != null)
        {
            selectedEnemy.SetActive(true);
            
            // Reset the enemy
            EnemyController ec = selectedEnemy.GetComponent<EnemyController>();
            if (ec != null)
            {
                ec.isBoss = false;
                ec.maxHealth = 3;
                ec.ResetEnemy();

                if (dungeonManager != null && dungeonManager.dungeonGenerator != null)
                {
                    ec.SetChantDurationMultiplier(dungeonManager.dungeonGenerator.GetChantDurationMultiplier());
                    Debug.Log($"[DungeonRoomController] Enemy chant duration set to {ec.chantDuration}");
                }
            }
            
            activeEnemy = selectedEnemy;
            Debug.Log($"Enemy Room: Activated Enemy {selectedEnemy.name}");
        }
        else
        {
            SetupItemRoom();
        }
    }

    private void SetupItemRoom()
    {
        if (availableItems != null && availableItems.Count > 0)
        {
            int index = Random.Range(0, availableItems.Count);
            GameObject selectedItem = availableItems[index];

            if (selectedItem != null)
            {
                selectedItem.SetActive(true);
                Debug.Log($"Item Room: Activated Item {selectedItem.name}");

                // Check for one-time use
                DungeonItem di = selectedItem.GetComponent<DungeonItem>();
                if (di != null && di.isOneTimeUse)
                {
                    availableItems.RemoveAt(index);
                    Debug.Log($"Item {selectedItem.name} is one-time use and has been removed from the pool for this run.");
                }
            }
        }
        else
        {
             Debug.LogWarning("Item Room: No items defined or available in the list!");
        }

        // RoomCleared();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnPlayerEnterRoom();
        }
    }

    public void OnPlayerEnterRoom()
    {
        if (!isCleared && lockDoorsOnEntry && activeEnemy != null)
        {
            LockDoors();
        }
        else if (activeEnemy == null && !isCleared)
        {
             // If no enemies, then ensure the room is marked cleared
             RoomCleared();
        }
    }

    public void EnemyDefeated()
    {
        RoomCleared();
    }

    public void ItemGot()
    {
        RoomCleared();
    }

    private void RoomCleared()
    {
        if (isCleared) return;

        isCleared = true;
        EnableDoorTasks();
        
        Debug.Log("Room Cleared!");
    }

    private void LockDoors()
    {
        foreach (var door in doors)
        {
            if (door != null)
            {
                DoorController doorCtrl = door.GetComponent<DoorController>();
                if (doorCtrl != null)
                {
                    doorCtrl.DisableTaskLock();
                    doorCtrl.Close();
                }
                else
                {
                    door.SetActive(true); // Fallback to simple activation
                }
            }
        }
    }

    public void UnlockAllDoors()
    {
        foreach (var door in doors)
        {
            if (door != null)
            {
                DoorController doorCtrl = door.GetComponent<DoorController>();
                if (doorCtrl != null)
                {
                    doorCtrl.DisableTaskLock();
                    doorCtrl.Open();
                }
                else
                {
                    door.SetActive(false); // Fallback to simple deactivation
                }
            }
        }
    }

    private void EnableDoorTasks()
    {
        foreach (var door in doors)
        {
            if (door != null)
            {
                DoorController doorCtrl = door.GetComponent<DoorController>();
                if (doorCtrl != null)
                {
                    doorCtrl.EnableTaskLock();
                }
                else
                {
                    door.SetActive(false); // Fallback to simple deactivation
                }
            }
        }
    }
}

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

    [Header("Room Settings")]
    public bool isCleared = false;
    public bool lockDoorsOnEntry = true;

    [Header("References")]
    public List<DungeonStaircase> staircases; // Reference to all staircases in this room
    public List<GameObject> activeEnemies = new List<GameObject>(); // Currently active enemies
    public List<GameObject> doors = new List<GameObject>();

    // Events
    public delegate void RoomEvent();
    public event RoomEvent OnRoomCleared;
    public event RoomEvent OnPlayerEnter;

    private void Start()
    {
        // Find DungeonManager if not assigned
        if (dungeonManager == null)
        {
            dungeonManager = FindObjectOfType<DungeonManager>();
        }

        // Auto-assign boss from enemies if missing
        if (boss == null && enemies != null && enemies.Count > 0)
        {
            int index = Random.Range(0, enemies.Count);
            boss = enemies[index];
            enemies.RemoveAt(index);
            Debug.Log($"[DungeonRoomController] Auto-assigned {boss.name} as Boss and removed from regular enemies.");
        }

        // 1. At start, disable all the enemies and items.
        DisableAllContent();
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
            // 2. Use a probability to control the room type
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

        activeEnemies.Clear();
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
                ec.ResetEnemy();
            }

            activeEnemies.Add(boss);
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
                ec.ResetEnemy();
            }
            
            activeEnemies.Add(selectedEnemy);
            Debug.Log($"Enemy Room: Activated Enemy {selectedEnemy.name}");
        }
        else
        {
            SetupItemRoom();
        }
    }

    private void SetupItemRoom()
    {
        Debug.Log("Item Room (Empty for now)");
        // TODO: Implement item room
        RoomCleared();
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
        if (OnPlayerEnter != null) OnPlayerEnter();

        if (!isCleared && lockDoorsOnEntry && activeEnemies.Count > 0)
        {
            LockDoors();
        }
        else if (activeEnemies.Count == 0 && !isCleared)
        {
             // If no enemies, then ensure the room is marked cleared
             RoomCleared();
        }
    }

    public void EnemyDefeated(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }

        if (activeEnemies.Count == 0)
        {
            RoomCleared();
        }
    }

    private void RoomCleared()
    {
        if (isCleared) return;

        isCleared = true;
        UnlockDoors();
        
        if (OnRoomCleared != null) OnRoomCleared();
        
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
                    doorCtrl.Close();
                }
                else
                {
                    door.SetActive(true); // Fallback to simple activation
                }
            }
        }
    }

    private void UnlockDoors()
    {
        foreach (var door in doors)
        {
            if (door != null)
            {
                DoorController doorCtrl = door.GetComponent<DoorController>();
                if (doorCtrl != null)
                {
                    doorCtrl.Open();
                }
                else
                {
                    door.SetActive(false); // Fallback to simple deactivation
                }
            }
        }
    }
}

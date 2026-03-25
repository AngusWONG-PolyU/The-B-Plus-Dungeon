using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DoorController : MonoBehaviour, ITaskTrigger
{
    [Header("Task Interaction")]
    public bool isTaskLocked = false;
    public SkillData counterMagicSkill;
    
    [Header("Task Settings")]
    public float unlockTimeLimit = 30f;
    
    private bool isOpen = false;
    private NavMeshObstacle obstacle;
    
    void Awake()
    {
        obstacle = GetComponent<NavMeshObstacle>();
    }
    
    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        
        if (obstacle != null) obstacle.enabled = false;

        // Hide instruction if it was showing
        if (isPlayerInCollider && PlayerInstructionUI.Instance != null)
        {
            PlayerInstructionUI.Instance.HideInstruction();
        }

        gameObject.SetActive(false);
    }
    
    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        
        if (obstacle != null) obstacle.enabled = true;

        gameObject.SetActive(true);
    }

    private bool isPlayerInCollider = false;
    private bool isTaskActive = false;
    private bool isTaskCompleted = false;
    private Coroutine taskCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInCollider = true;
            
            // Only show instruction if the player is alive
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null && ph.currentHearts <= 0) return;

            if (isTaskLocked && !isOpen && !isTaskActive)
            {
                if (PlayerInstructionUI.Instance != null)
                {
                    PlayerInstructionUI.Instance.ShowInstruction("Press E to Unlock Door!");
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInCollider = false;
            if (PlayerInstructionUI.Instance != null && isTaskLocked && !isOpen && !isTaskActive)
            {
                PlayerInstructionUI.Instance.HideInstruction();
            }
        }
    }

    private void Update()
    {
        if (isPlayerInCollider && isTaskLocked && !isOpen && !isTaskActive && Time.timeScale > 0f)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (PlayerInstructionUI.Instance != null)
                {
                    PlayerInstructionUI.Instance.HideInstruction();
                }

                if (taskCoroutine != null) StopCoroutine(taskCoroutine);
                taskCoroutine = StartCoroutine(TaskRoutine());
            }
        }
    }

    private IEnumerator TaskRoutine()
    {
        isTaskActive = true;
        isTaskCompleted = false;

        float finalTimeLimit = unlockTimeLimit;
        
        int timeLimitMode = PlayerPrefs.GetInt("TimeLimitMode", 0);
        if (timeLimitMode == 1) finalTimeLimit = 60f;
        else if (timeLimitMode == 2) finalTimeLimit = float.MaxValue;

        if (BPlusTreeTaskManager.Instance != null)
        {
            BPlusTreeTaskManager.Instance.StartTask(this, BPlusTreeTaskType.Insertion);
        }

        float timer = 0f;
        while (timer < finalTimeLimit)
        {
            if (BPlusTreeTaskManager.Instance != null)
            {
                BPlusTreeTaskManager.Instance.UpdateTaskTimer(finalTimeLimit - timer, finalTimeLimit);
            }

            if (isTaskCompleted)
            {
                // Task was completed successfully (handled in OnTaskComplete)
                yield break;
            }

            yield return null;
            timer += Time.deltaTime;
        }

        // Time's up!
        if (PlayerInstructionUI.Instance != null)
        {
            PlayerInstructionUI.Instance.ShowInstruction("Time's Up! The Door's Auto Counter-Spell has been Activated!\nBRACE FOR IMPACT!", 3f, true);
        }

        if (BPlusTreeTaskManager.Instance != null)
        {
            BPlusTreeTaskManager.Instance.CloseTask(false);
        }
        else
        {
            // Simulate failure to trigger the -1HP logic in OnTaskComplete
            OnTaskComplete(false);
        }
    }

    public void EnableTaskLock()
    {
        isTaskLocked = true;
        isTaskActive = false;
    }

    public void DisableTaskLock()
    {
        isTaskLocked = false;
        isTaskActive = false;
    }

    public void OnTaskComplete(bool success)
    {
        isTaskActive = false;
        
        if (taskCoroutine != null)
        {
            StopCoroutine(taskCoroutine);
            taskCoroutine = null;
        }

        if (success)
        {
            isTaskCompleted = true;
            Debug.Log("Door Unlocked by Task!");
            isTaskLocked = false;

            // Tell the Room Controller to unlock ALL doors in this room
            DungeonRoomController roomController = GetComponentInParent<DungeonRoomController>();
            if (roomController != null)
            {
                roomController.UnlockAllDoors();
            }
            else
            {
                // Fallback if not in a room
                Open();
            }
            
            if (PlayerInstructionUI.Instance != null)
            {
                PlayerInstructionUI.Instance.ShowInstruction("Door Unlocked!", 1f);
            }
        }
        else
        {
            Debug.Log("Door Task Failed.");
            
            if (PlayerInstructionUI.Instance != null)
            {
                PlayerInstructionUI.Instance.ShowInstruction("Unlock Failed! The Door's Counter-Spell Strikes Back!\nBRACE FOR IMPACT!", 3f, true);
            }
            
            // Cast Counter-Magic at the player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (counterMagicSkill != null && counterMagicSkill.skillPrefab != null)
                {
                    GameObject magic = Instantiate(counterMagicSkill.skillPrefab, player.transform.position, Quaternion.identity);
                    SpellProjectile proj = magic.GetComponent<SpellProjectile>();
                    if (proj != null)
                    {
                        proj.SetCaster("Enemy");
                    }
                }
                else
                {
                    PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(1);
                    }
                }
            }

            // Show instruction again if the player is still in the collider after a delay
            if (isPlayerInCollider && isTaskLocked && !isOpen)
            {
                StartCoroutine(ShowUnlockInstructionAfterDelay(3f));
            }
        }
    }

    private IEnumerator ShowUnlockInstructionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Check if the player is still alive before showing the instruction
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null && ph.currentHearts <= 0) yield break;
        }

        if (isPlayerInCollider && isTaskLocked && !isOpen && !isTaskActive)
        {
            if (PlayerInstructionUI.Instance != null)
            {
                PlayerInstructionUI.Instance.ShowInstruction("Press E to Unlock Door!");
            }
        }
    }
}
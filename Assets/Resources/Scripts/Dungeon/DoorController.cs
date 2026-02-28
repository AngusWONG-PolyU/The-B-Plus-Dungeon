using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DoorController : MonoBehaviour, ITaskTrigger
{
    public enum DoorType { Disappearing, Sliding, Rotating }
    
    [Header("Task Interaction")]
    public bool isTaskLocked = false;

    [Header("Settings")]
    public DoorType type = DoorType.Disappearing;
    public float animationDuration = 1.0f;
    
    [Header("Sliding Settings")]
    public Vector3 slideOffset = new Vector3(0, -3, 0); // Default slide down
    
    [Header("Rotating Settings")]
    public Vector3 rotateAngle = new Vector3(0, 90, 0);
    
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isOpen = false;
    private NavMeshObstacle obstacle;
    
    void Awake()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        obstacle = GetComponent<NavMeshObstacle>();
    }
    
    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        
        if (obstacle != null) obstacle.enabled = false;

        StopAllCoroutines();
        
        switch (type)
        {
            case DoorType.Disappearing:
                gameObject.SetActive(false);
                break;
                
            case DoorType.Sliding:
                StartCoroutine(MoveTo(initialPosition + slideOffset));
                break;
                
            case DoorType.Rotating:
                StartCoroutine(RotateTo(initialRotation * Quaternion.Euler(rotateAngle)));
                break;
        }
    }
    
    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        
        if (obstacle != null) obstacle.enabled = true;

        StopAllCoroutines();
        
        switch (type)
        {
            case DoorType.Disappearing:
                gameObject.SetActive(true);
                break;
                
            case DoorType.Sliding:
                gameObject.SetActive(true);
                StartCoroutine(MoveTo(initialPosition));
                break;
                
            case DoorType.Rotating:
                gameObject.SetActive(true);
                StartCoroutine(RotateTo(initialRotation));
                break;
        }
    }
    
    private IEnumerator MoveTo(Vector3 targetPos)
    {
        float elapsed = 0;
        Vector3 startPos = transform.localPosition;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            // Smooth step interpolation
            t = t * t * (3f - 2f * t);
            
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        transform.localPosition = targetPos;
    }
    
    private IEnumerator RotateTo(Quaternion targetRot)
    {
        float elapsed = 0;
        Quaternion startRot = transform.localRotation;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            t = t * t * (3f - 2f * t);
            
            transform.localRotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }
        transform.localRotation = targetRot;
    }

    private bool isPlayerInCollider = false;
    private bool isTaskActive = false;

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
        if (isPlayerInCollider && isTaskLocked && !isOpen && !isTaskActive)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (PlayerInstructionUI.Instance != null)
                {
                    PlayerInstructionUI.Instance.HideInstruction();
                }

                if (BPlusTreeTaskManager.Instance != null)
                {
                    isTaskActive = true;
                    // Trigger Insertion Task for unlocking
                    BPlusTreeTaskManager.Instance.StartTask(this, BPlusTreeTaskType.Insertion);
                }
            }
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
        if (success)
        {
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
            
            // Deduct player health
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(1);
                }
            }

            // Show failure instruction
            if (PlayerInstructionUI.Instance != null)
            {
                PlayerInstructionUI.Instance.ShowInstruction("Magic Key Insertion Failed! The Door's Defensive Counter-Spell has been Activated!\nBRACE FOR IMPACT!", 3f, true);
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
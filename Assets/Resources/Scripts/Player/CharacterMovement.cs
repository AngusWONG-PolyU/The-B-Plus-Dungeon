using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class CharacterMovement : MonoBehaviour
{
    private Camera playerCamera;
    private Quaternion baseCameraRotation; // Store the initial camera-facing rotation
    
    // Navigation components
    private NavMeshAgent navAgent;
    
    // Animation components
    private Animator animator;
    private Transform character;
    private PlayerSkillController skillController;
    
    private Vector3 targetPosition;
    
    public GameObject targetEffect;
    private ParticleSystem targetEffectSystem;
    
    private int lockCount = 0; // Counter for multiple lock sources
    public bool isLocked => lockCount > 0; // Derived property
    
    private float baseSpeed; // Store the initial speed for resetting

    void Start()
    {
        playerCamera = Camera.main;
        // Find Character child
        character = transform.Find("Wizard");
        
        // Find the target effect particle system and hide the target effects at start
        if (targetEffect != null)
        {
            targetEffectSystem = targetEffect.GetComponent<ParticleSystem>();
            targetEffect.gameObject.SetActive(false);
        }
        
        // Get the Animator component from the Character child
        if (character != null)
        {
            animator = character.GetComponent<Animator>();
        }
        
        // Get the NavMeshAgent
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
             baseSpeed = navAgent.speed;
        }
        
        // Get PlayerSkillController
        skillController = GetComponent<PlayerSkillController>();

        // Make character face camera initially and store this as base rotation
        FaceCamera();
        baseCameraRotation = character.rotation;
    }

    void Update()
    {
        // If locked, prevent movement input and stop the agent
        if (isLocked)
        {
            if (navAgent != null && navAgent.enabled) navAgent.ResetPath();
            SetRunningAnimation(false);
            return;
        }

        // Check for left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
            // Prevent movement if clicking on UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            // Create a ray from the mouse position on the screen
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Perform a raycast to detect where the ray hits in the scene
            if (Physics.Raycast(ray, out hit))
            {
                targetPosition = hit.point;

                if (targetEffect != null)
                { 
                    targetEffect.transform.position = targetPosition; 
                    StartCoroutine(ShowTargetEffect());
                }
                
                navAgent.SetDestination(targetPosition);
            }
        }
        
        // Check if the agent is close to its destination
        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                // Stop the running animation
                SetRunningAnimation(false);
            }
            else
            {
                // Start the running animation if it's moving
                SetRunningAnimation(true);
                
                FaceTarget(targetPosition);
        
                // Keep the original camera-facing rotation
                transform.rotation = baseCameraRotation;
            }
        }
    }

    // Public method to face a target position
    public void FaceTarget(Vector3 targetWorldPos)
    {
        if (character == null || playerCamera == null) return;

        // Get character's current screen position
        Vector3 characterScreenPos = playerCamera.WorldToScreenPoint(character.position);
        // Get the target's screen position
        Vector3 targetScreenPos = playerCamera.WorldToScreenPoint(targetWorldPos);

        Vector3 currentScale = character.localScale;

        if (targetScreenPos.x > characterScreenPos.x)
        {
            // Target on the right - face right (flip X scale)
            character.localScale = new Vector3(-Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
        }
        else
        {
            // Target on the left - face left (normal scale)
            character.localScale = new Vector3(Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
        }
    }

    IEnumerator ShowTargetEffect()
    {
        // Show the target effect
        if (targetEffect != null && targetEffectSystem != null)
        {
            targetEffect.gameObject.SetActive(true);
            targetEffectSystem.Play();
        }
                
        // Wait for effect
        yield return new WaitForSeconds(0.5f);
                
        // Hide the target effect
        if (targetEffect != null && targetEffectSystem != null)
        {
            targetEffect.gameObject.SetActive(false);
            targetEffectSystem.Stop();
        }
    }
    
    // Animation control method
    private void SetRunningAnimation(bool isRunning)
    {
        if (animator != null)
        {
            animator.SetBool("isRunning", isRunning);
        }
    }
    
    // Function to make the character always face the camera
    void FaceCamera()
    {
        if (playerCamera != null)
        {
            // Make the character face the camera
            Vector3 directionToCamera = playerCamera.transform.position - transform.position;
            directionToCamera.y = 0; // Keep character upright, only rotate on Y axis
            
            if (directionToCamera != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToCamera);
            }
        }
    }
    
    // Public method to stop movement
    public void StopMovement()
    {
        // Stop NavMeshAgent
        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
        }
        
        SetRunningAnimation(false);
        Debug.Log("Character movement stopped by external script");
    }

    // Force reset all locks (e.g., on respawn)
    public void ForceUnlock()
    {
        lockCount = 0;
        SetLocked(false); // Update state to unlocked
    }

    // Public method to lock/unlock player movement
    public void SetLocked(bool locked)
    {
        if (locked)
        {
            lockCount++;
        }
        else
        {
            lockCount--;
            if (lockCount < 0) lockCount = 0;
        }

        bool currentlyLocked = isLocked;
        
        // Lock/Unlock Skills
        if (skillController != null)
        {
            if (currentlyLocked)
            {
                skillController.isSystemActive = false;
            }
            else
            {
                // Only enable skills if DungeonManager says dungeon is active
                DungeonManager dm = FindObjectOfType<DungeonManager>();
                skillController.isSystemActive = (dm != null && dm.isDungeonActive);
            }
        }

        if (currentlyLocked)
        {
            // Stop immediately when locked
            if (navAgent != null && navAgent.enabled) navAgent.ResetPath();
            SetRunningAnimation(false);
            // Debug.Log($"Player movement LOCKED (Count: {lockCount})");
        }
        else
        {
            // Debug.Log($"Player movement UNLOCKED (Count: {lockCount})");
        }
    }
    
    // Public method to resume movement capability
    public void ResumeMovement()
    {
        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
        }
    }

    public void ResetSpeed()
    {
        if (navAgent != null)
        {
            navAgent.speed = baseSpeed;
            Debug.Log("CharacterMovement: Speed reset to base value.");
        }
    }
}
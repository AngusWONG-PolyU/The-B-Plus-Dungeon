using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    
    private bool isLocked = false; // Flag to track if player is locked
    
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
        
        // Get PlayerSkillController
        skillController = GetComponent<PlayerSkillController>();

        // Make character face camera initially and store this as base rotation
        FaceCamera();
        baseCameraRotation = character.rotation;
    }

    void Update()
    {
        // If locked, prevent movement input and stop agent
        if (isLocked)
        {
            if (navAgent != null && navAgent.enabled) navAgent.ResetPath();
            SetRunningAnimation(false);
            return;
        }

        // Check for left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
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

    // Public method to lock/unlock player movement
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        
        // Lock/Unlock Skills
        if (skillController != null)
        {
            if (locked)
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

        if (isLocked)
        {
            // Stop immediately when locked
            if (navAgent != null && navAgent.enabled) navAgent.ResetPath();
            SetRunningAnimation(false);
            Debug.Log("Player movement LOCKED");
        }
        else
        {
            Debug.Log("Player movement UNLOCKED");
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
}
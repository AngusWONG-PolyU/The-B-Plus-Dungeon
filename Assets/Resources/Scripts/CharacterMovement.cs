using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.1f;
    public LayerMask groundLayer = 1; // Layer 0 (Default) for ground click detection
    
    private Vector3 targetPosition;
    private bool hasTarget = false;
    private Camera playerCamera;
    private Quaternion baseCameraRotation; // Store the initial camera-facing rotation
    
    // Animation components
    private Animator animator;
    
    void Start()
    {
        // Get the main camera if none is assigned
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        // Get the Animator component
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("No Animator component found! Please add an Animator to enable animations.");
        }
            
        // Initialize target position to current position
        targetPosition = transform.position;
        
        // Make character face camera initially and store this as base rotation
        FaceCamera();
        baseCameraRotation = transform.rotation;
    }

    void Update()
    {
        HandleInput();
        MoveTowardsTarget();
    }
    
    void HandleInput()
    {
        Vector3 inputPosition = Vector3.zero;
        bool inputDetected = false;
        
        if (Input.GetMouseButtonDown(0))
        {
            // Check if mouse is over UI element
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // Mouse is over UI - don't process movement
                Debug.Log("Clicked on UI - ignoring movement");
                return;
            }
            
            inputPosition = Input.mousePosition;
            inputDetected = true;
        }
        
        if (inputDetected)
        {
            SetTargetFromScreenPosition(inputPosition);
        }
    }
    
    void SetTargetFromScreenPosition(Vector3 screenPosition)
    {
        // Convert screen position to world position
        Ray ray = playerCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        Vector3 targetPoint;
        
        // Raycast to find where the player clicked/tapped
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            // Hit the ground - use the hit point
            targetPoint = hit.point;
        }
        else
        {
            // No ground hit - project onto a plane at character's Y level
            Plane groundPlane = new Plane(Vector3.up, transform.position);
            
            if (groundPlane.Raycast(ray, out float distance))
            {
                targetPoint = ray.GetPoint(distance);
            }
            else
            {
                // Fallback: use a point in front of camera
                targetPoint = ray.GetPoint(10f);
                targetPoint.y = transform.position.y;
            }
        }
        
        SetTarget(targetPoint, screenPosition);
    }
    
    void SetTarget(Vector3 newTarget, Vector3 clickScreenPosition)
    {
        targetPosition = newTarget;
        hasTarget = true;
        
        // Get character's current screen position
        Vector3 characterScreenPos = playerCamera.WorldToScreenPoint(transform.position);
        
        // Instead of rotating, let's use scale to flip the character
        Vector3 currentScale = transform.localScale;
        
        if (clickScreenPosition.x > characterScreenPos.x)
        {
            // Clicked to the right of the character on the screen - face right (flip X scale)
            transform.localScale = new Vector3(-Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
        }
        else
        {
            // Clicked to the left of character on screen - face left (normal scale)
            transform.localScale = new Vector3(Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
        }
        
        // Keep the original camera-facing rotation
        transform.rotation = baseCameraRotation;
    }
    
    void MoveTowardsTarget()
    {
        if (!hasTarget) 
        {
            // Stop running animation when no target
            SetRunningAnimation(false);
            return;
        }
        
        // Calculate distance to target
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        // Check if the character has reached the target
        if (distanceToTarget <= stoppingDistance)
        {
            hasTarget = false;
            SetRunningAnimation(false);
            return;
        }
        
        // Start running animation
        SetRunningAnimation(true);
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        Vector3 moveVector = direction * moveSpeed * Time.deltaTime;
        transform.position += moveVector;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Only stop for vertical walls, ignore ground/floor collisions
        Vector3 collisionNormal = collision.contacts[0].normal;
        
        // If collision normal is mostly upward (ground), ignore it
        if (collisionNormal.y > 0.5f)
        {
            return; // Don't stop movement for ground
        }
        
        // This is a wall (vertical collision) - stop movement and animation
        hasTarget = false;
        SetRunningAnimation(false);
    }
    
    void OnCollisionStay(Collision collision)
    {
        if (hasTarget)
        {
            // Same check - only stop for walls, not ground
            Vector3 collisionNormal = collision.contacts[0].normal;
            
            if (collisionNormal.y > 0.5f)
            {
                return; // Don't stop for ground
            }
            
            hasTarget = false;
            SetRunningAnimation(false);
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
    
    // Function to make character always face the camera
    void FaceCamera()
    {
        if (playerCamera != null)
        {
            // Make the character face the camera (billboard effect)
            Vector3 directionToCamera = playerCamera.transform.position - transform.position;
            directionToCamera.y = 0; // Keep character upright, only rotate on Y axis
            
            if (directionToCamera != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToCamera);
            }
        }
    }
    
    // Public method to stop movement (for portals, cutscenes, etc.)
    public void StopMovement()
    {
        hasTarget = false;
        SetRunningAnimation(false);
        Debug.Log("Character movement stopped by external script");
    }
    
    // Public method to check if character is moving
    public bool IsMoving()
    {
        return hasTarget;
    }
    
    // Optional: Draw gizmos in the scene view for debugging
    void OnDrawGizmos()
    {
        if (hasTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.5f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
}
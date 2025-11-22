using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalController : MonoBehaviour
{
    [Header("Teleport Settings")]
    public string destinationName;
    public Transform teleportDestination; // Where to teleport player
    public Vector3 teleportOffset = new Vector3(1, 0, 1);
    public string playerTag = "Player";
    
    [Header("Player")]
    public GameObject player;
    private Transform character;
    private Transform playerTeleportEffect;
    private ParticleSystem playerTeleportEffectSystem;
    
    [Header("UI")]
    public TMPro.TMP_Text promptText; // "Press E to Exit"
    
    private GameObject playerInRange;
    
    void Start()
    {
        character = player.transform.Find("Wizard");
        playerTeleportEffect = player.transform.Find("TeleportEffect");
        playerTeleportEffectSystem = playerTeleportEffect.GetComponent<ParticleSystem>();
        
        // Hide teleport effects at start
        if (playerTeleportEffect != null)
        {
            playerTeleportEffect.gameObject.SetActive(false);
        }
        
        // Hide interaction prompt at start
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
        
        // Make sure this portal has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
    
    void Update()
    {
        // Check for E key press when player is in range
        if (playerInRange != null && Input.GetKeyDown(KeyCode.E))
        {
            TeleportPlayer();
        }
    }
    
    // Detect player entering portal
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = other.gameObject;
            ShowInteractionPrompt();
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = null;
            HideInteractionPrompt();
        }
    }
    
    void ShowInteractionPrompt()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = $"Press E to teleport to {destinationName}";
        }
    }
    
    void HideInteractionPrompt()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }
    
    void TeleportPlayer()
    {
        if (playerInRange != null && teleportDestination != null)
        {
            StartCoroutine(TeleportPlayerWithEffect());
        }
        else
        {
            Debug.LogWarning($"Cannot teleport: playerInRange={playerInRange}, teleportDestination={teleportDestination}");
        }
    }
    
    IEnumerator TeleportPlayerWithEffect()
    {
        if (playerInRange == null || teleportDestination == null)
        {
            yield break;
        }
        
        HideInteractionPrompt();
        
        // Stop player movement and disable NavMeshAgent
        CharacterMovement playerMovement = playerInRange.GetComponent<CharacterMovement>();
        UnityEngine.AI.NavMeshAgent navAgent = playerInRange.GetComponent<UnityEngine.AI.NavMeshAgent>();
        
        if (playerMovement != null)
        {
            playerMovement.StopMovement();
        }
        
        // Disable NavMeshAgent to prevent it from pulling the player back
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.enabled = false;
        }
        
        // Hide character and show teleport effect
        if (playerTeleportEffect != null && character != null)
        {
            character.gameObject.SetActive(false);
            playerTeleportEffect.gameObject.SetActive(true);
        }
        
        // Wait for effect
        yield return new WaitForSeconds(0.5f);
        
        // Teleport player
        Vector3 finalPosition = teleportDestination.position + teleportOffset;
        playerInRange.transform.position = finalPosition;
        
        // Instantly move the camera to the new position
        IsometricCameraSetup cameraSetup = FindObjectOfType<IsometricCameraSetup>();
        if (cameraSetup != null)
        {
            Vector3 offset = new Vector3(-cameraSetup.distance, cameraSetup.height, -cameraSetup.distance);
            Camera.main.transform.position = finalPosition + offset;
        }
        
        // Wait a frame for position to sync
        yield return null;
        
        // Re-enable NavMeshAgent
        if (navAgent != null)
        {
            navAgent.enabled = true;
            
            // Wait for NavMesh to sync
            yield return new WaitForEndOfFrame();
            
            if (navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
            }
        }
        
        // Resume movement
        if (playerMovement != null)
        {
            playerMovement.ResumeMovement();
        }

        playerTeleportEffectSystem.Stop();
        playerTeleportEffectSystem.Play();
        // Wait for effect
        yield return new WaitForSeconds(0.5f);
        
        // Show character and hide destination effect
        if (playerTeleportEffect != null && character != null)
        {
            character.gameObject.SetActive(true);
            playerTeleportEffect.gameObject.SetActive(false);
        }
        
        playerInRange = null;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonPortalController : MonoBehaviour
{
    [Header("Dungeon Integration")]
    public DungeonRoomUpdater dungeonRoomUpdater;
    public int childIndex = -1; // Set by DungeonRoomUpdater at runtime
    
    [Header("Teleport Settings")]
    [HideInInspector] public Transform teleportDestination; // Set by DungeonRoomUpdater at runtime
    public Vector3 teleportOffset = new Vector3(1, 0, 1);
    public string playerTag = "Player";
    
    [Header("Player")]
    public GameObject player;
    private Transform character;
    private Transform playerTeleportEffect;
    private ParticleSystem playerTeleportEffectSystem;
    
    [Header("UI")]
    public TMPro.TMP_Text promptText; // Text component
    
    private GameObject playerInRange;
    
    void Start()
    {
        character = player.transform.Find("Wizard");
        playerTeleportEffect = player.transform.Find("TeleportEffect");
        playerTeleportEffectSystem = playerTeleportEffect.GetComponent<ParticleSystem>();
        
        // Validate that dungeonRoomUpdater is assigned
        if (dungeonRoomUpdater == null)
        {
            Debug.LogWarning($"[DungeonPortalController] {gameObject.name}: dungeonRoomUpdater not assigned! This will be set at runtime.");
        }
        
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
            // Notify room updater that player selected this portal
            if (dungeonRoomUpdater != null && childIndex >= 0)
            {
                dungeonRoomUpdater.OnPlayerSelectPortal(childIndex, this);
            }
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
            promptText.text = $"Press E to teleport to Child {childIndex + 1}";
        }
    }
    
    void HideInteractionPrompt()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }
    
    // Called by DungeonRoomUpdater when this is the correct portal
    public void TeleportPlayer()
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
    
    // Called by DungeonRoomUpdater when this is the wrong portal
    public void TurnRed()
    {
        StartCoroutine(TurnRedAndDisable());
    }
    
    // Reset portal state for reuse in next room
    public void ResetPortal()
    {
        enabled = true;
        playerInRange = null; // Clear player reference
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
    
    IEnumerator TurnRedAndDisable()
    {
        HideInteractionPrompt();
        
        // Find parent portal GameObject
        Transform parentPortal = transform.parent;
        if (parentPortal != null)
        {
            // Find Portal green and Portal red
            Transform greenPortal = parentPortal.Find("Portal green");
            Transform redPortal = parentPortal.Find("Portal red");
            
            if (greenPortal != null && redPortal != null)
            {
                // Swap to red portal
                greenPortal.gameObject.SetActive(false);
                redPortal.gameObject.SetActive(true);
            }
        }
        
        Debug.Log("Wrong portal chosen! Portal turned red and disabled.");
        
        yield return null;
        
        // Disable this portal permanently
        enabled = false;
    }
}

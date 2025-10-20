using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PortalController : MonoBehaviour
{
    [Header("Portal Settings")]
    public string destinationName = "Tutorial Area"; // What to show in popup
    public Transform teleportDestination; // Where to teleport player
    public Vector3 teleportOffset = new Vector3(1, 0, 1); // Offset from destination (X+1, Z+1 to avoid stucking in the destination portal)
    public string playerTag = "Player";
    public GameObject teleportEffect; // The teleport effect to control
    public GameObject destinationTeleportEffect;
    public GameObject character;
    
    [Header("UI References")]
    public GameObject confirmationPanel; // UI panel for confirmation
    public Text messageText; // Text component showing the message
    public Button yesButton; // Yes button
    public Button noButton; // No button
    
    [Header("Optional Settings")]
    public bool pauseGameDuringConfirmation = true; // Pause game when showing confirmation
    public bool allowExitToCancel = true; // Cancel when player leaves portal area
    
    private GameObject playerInRange;
    private bool showingConfirmation = false;
    
    void Start()
    {
        // Validate UI references
        if (confirmationPanel == null)
        {
            Debug.LogError($"Portal {gameObject.name}: Confirmation Panel is not assigned!");
        }
        
        if (yesButton == null || noButton == null)
        {
            Debug.LogError($"Portal {gameObject.name}: Yes/No buttons are not assigned!");
        }
        
        if (messageText == null)
        {
            Debug.LogWarning($"Portal {gameObject.name}: Message Text is not assigned!");
        }
        
        if (teleportDestination == null)
        {
            Debug.LogError($"Portal {gameObject.name}: Teleport Destination is not assigned!");
        }
        
        // Hide teleport effect at start
        if (teleportEffect != null)
        {
            teleportEffect.SetActive(false);
        }
        
        // Hide destination teleport effect at start
        if (destinationTeleportEffect != null)
        {
            destinationTeleportEffect.SetActive(false);
        }
        
        // Setup UI button listeners
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnYesClicked);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(OnNoClicked);
        }
            
        // Hide confirmation panel initially
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
            
        // Make sure this portal has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
            
        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            col2D.isTrigger = true;
        }
    }
    
    void Update()
    {
        // Optional: Add ESC key to cancel (even in UI mode)
        if (showingConfirmation && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelTeleport();
        }
    }
    
    // Detect player entering portal area
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !showingConfirmation)
        {
            playerInRange = other.gameObject;
            ShowConfirmation();
        }
    }
    
    // For 2D games
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) && !showingConfirmation)
        {
            playerInRange = other.gameObject;
            ShowConfirmation();
        }
    }
    
    // Player leaves portal area
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && allowExitToCancel)
        {
            CancelTeleport();
        }
    }
    
    // For 2D games
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) && allowExitToCancel)
        {
            CancelTeleport();
        }
    }
    
    void ShowConfirmation()
    {
        if (confirmationPanel == null)
        {
            Debug.LogError("Cannot show confirmation - Confirmation Panel is not assigned!");
            return;
        }
        
        showingConfirmation = true;
        
        // Show UI confirmation panel
        confirmationPanel.SetActive(true);
        
        // Update message text
        if (messageText != null)
        {
            messageText.text = $"Do you want to teleport to {destinationName}?";
        }
            
        // Pause the game if enabled
        if (pauseGameDuringConfirmation)
        {
            Time.timeScale = 0f;
        }
        
        Debug.Log($"Portal confirmation shown for: {destinationName}");
    }
    
    void OnYesClicked()
    {
        TeleportPlayer();
    }
    
    void OnNoClicked()
    {
        CancelTeleport();
    }
    
    void TeleportPlayer()
    {
        if (playerInRange == null)
        {
            Debug.LogWarning("Cannot teleport - no player in range!");
            CancelTeleport();
            return;
        }
        
        if (teleportDestination == null)
        {
            Debug.LogError("Cannot teleport - Teleport Destination is not assigned!");
            CancelTeleport();
            return;
        }
        
        // Simple teleport with effect
        StartCoroutine(SimpleTeleport());
    }
    
    IEnumerator SimpleTeleport()
    {
        // FIRST: Close the message panel immediately so effects can be seen!
        showingConfirmation = false;
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
        
        // Resume game time so effects work properly
        if (pauseGameDuringConfirmation)
        {
            Time.timeScale = 1f;
        }
        
        // Stop player movement
        CharacterMovement playerMovement = playerInRange.GetComponent<CharacterMovement>();
        if (playerMovement != null)
        {
            playerMovement.StopMovement();
        }
        
        // Hide the character and show teleport effect for 1 second
        if (teleportEffect != null)
        {
            character.SetActive(false);
            teleportEffect.SetActive(true);
        }
        
        // Wait for effect
        yield return new WaitForSeconds(1f);
        
        // Teleport player
        Vector3 finalTeleportPosition = teleportDestination.position + teleportOffset;
        playerInRange.transform.position = finalTeleportPosition;
        
        // Hide teleport effect
        if (teleportEffect != null)
        {
            teleportEffect.SetActive(false);
        }

        // Show the destination teleport effect
        if (destinationTeleportEffect != null)
        {
            destinationTeleportEffect.SetActive(true);
        }
        
        // Wait for effect
        yield return new WaitForSeconds(1f);
        
        // Show the character and hide the destination teleport effect
        if (destinationTeleportEffect != null)
        {
            character.SetActive(true);
            destinationTeleportEffect.SetActive(false);
        }
        
        // Clear player reference
        playerInRange = null;
        
        Debug.Log($"Player teleported to {destinationName}!");
    }
    
    void CancelTeleport()
    {
        showingConfirmation = false;
        playerInRange = null;
        
        // Hide UI panel
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
        
        // Resume game if it was paused
        if (pauseGameDuringConfirmation)
        {
            Time.timeScale = 1f;
        }
        
        Debug.Log("Portal confirmation cancelled");
    }
    
    // Public methods for external control
    public void ForceShowConfirmation(GameObject player)
    {
        if (!showingConfirmation)
        {
            playerInRange = player;
            ShowConfirmation();
        }
    }
    
    public void ForceCancelTeleport()
    {
        CancelTeleport();
    }
    
    public bool IsShowingConfirmation()
    {
        return showingConfirmation;
    }
}
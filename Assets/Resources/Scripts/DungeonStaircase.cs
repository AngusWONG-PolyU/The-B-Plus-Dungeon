using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DungeonStaircase : MonoBehaviour
{
    [Header("Staircase Settings")]
    public string dungeonName = "Tutorial Dungeon"; // Name of the dungeon
    public Transform teleportDestination; // Where to teleport player
    public Vector3 teleportOffset = new Vector3(1, 1, 1); // Offset from destination
    public string playerTag = "Player";
    
    [Header("Loading Screen")]
    public GameObject loadingScreen; // Loading screen UI panel
    public Text loadingText; // Loading text
    public Slider loadingProgressBar; // Progress bar
    public float minLoadingTime = 2f; // Minimum loading screen duration
    public float maxLoadingTime = 3f; // Maximum loading screen duration
    
    private bool isTeleporting = false;
    
    void Start()
    {
        // Validate settings
        if (teleportDestination == null)
        {
            Debug.LogError($"Staircase {gameObject.name}: Teleport Destination is not assigned!");
        }
        
        if (loadingScreen == null)
        {
            Debug.LogWarning($"Staircase {gameObject.name}: Loading Screen is not assigned!");
        }
        
        // Hide loading screen initially
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
            
        // Make sure this staircase has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
    
    // Detect player entering staircase - immediately start teleport
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !isTeleporting)
        {
            StartCoroutine(TeleportToDungeon(other.gameObject));
        }
    }
    
    IEnumerator TeleportToDungeon(GameObject player)
    {
        if (teleportDestination == null)
        {
            Debug.LogError("Cannot teleport - Teleport Destination is not assigned!");
            yield break;
        }
        
        isTeleporting = true;
        
        // Stop player movement
        CharacterMovement playerMovement = player.GetComponent<CharacterMovement>();
        if (playerMovement != null)
        {
            playerMovement.StopMovement();
        }
        
        // Show loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            
            if (loadingText != null)
            {
                loadingText.text = $"Entering {dungeonName}...";
            }
            
            // Simulate loading progress
            float loadingDuration = Random.Range(minLoadingTime, maxLoadingTime);
            float elapsedTime = 0f;
            
            while (elapsedTime < loadingDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / loadingDuration;
                
                if (loadingProgressBar != null)
                {
                    loadingProgressBar.value = progress;
                }
                
                yield return null;
            }
            
            // Ensure progress bar is full
            if (loadingProgressBar != null)
            {
                loadingProgressBar.value = 1f;
            }
        }
        else
        {
            // If no loading screen, just wait a bit
            yield return new WaitForSeconds(2f);
        }
        
        // Actual teleportation
        Vector3 finalTeleportPosition = teleportDestination.position + teleportOffset;
        player.transform.position = finalTeleportPosition;
        
        // Hide loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
        
        isTeleporting = false;
        
        Debug.Log($"Player entered {dungeonName} through the staircase!");
    }
    
    // Public method to check if currently teleporting
    public bool IsTeleporting()
    {
        return isTeleporting;
    }
}
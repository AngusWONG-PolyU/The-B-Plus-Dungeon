using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DungeonItem : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemName;
    public bool isOneTimeUse = false; // If true, appears only once per run

    [Header("InteractionUI")]
    public TMPro.TMP_Text promptText;

    private GameObject playerInRange;
    
    private void Start()
    {
        InitializePrompt();
    }

    private void OnEnable()
    {
        InitializePrompt();
    }

    private void InitializePrompt()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerInRange != null && Input.GetKeyDown(KeyCode.E))
        {
            PickupItem(playerInRange);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = other.gameObject;
            if (promptText != null)
            {
                promptText.gameObject.SetActive(true);
                promptText.text = $"Press E to get {itemName}";
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = null;
            if (promptText != null)
            {
                promptText.gameObject.SetActive(false);
            }
        }
    }

    private void PickupItem(GameObject player)
    {
        ApplyEffect(player);
        
        // Hide text before disabling
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
            
        gameObject.SetActive(false);
        
        DungeonRoomController roomController = FindObjectOfType<DungeonRoomController>();
        roomController.ItemGot();
    }

    // Every item must implement this method
    protected abstract void ApplyEffect(GameObject player);
}
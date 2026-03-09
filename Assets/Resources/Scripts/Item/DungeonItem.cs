using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DungeonItem : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemName;
    public bool isOneTimeUse = false; // If true, appears only once per run

    private GameObject playerInRange;
    
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
            if (PlayerInstructionUI.Instance != null)
            {
                PlayerInstructionUI.Instance.ShowInstruction($"Press E to get {itemName}!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = null;
            if (PlayerInstructionUI.Instance != null)
            {
                PlayerInstructionUI.Instance.HideInstruction();
            }
        }
    }

    private void PickupItem(GameObject player)
    {
        ApplyEffect(player);
        
        // Hide text before disabling
        if (PlayerInstructionUI.Instance != null)
        {
            PlayerInstructionUI.Instance.HideInstruction();
        }
            
        gameObject.SetActive(false);
        
        DungeonRoomController roomController = FindObjectOfType<DungeonRoomController>();
        roomController.ItemGot();
    }

    // Every item must implement this method
    protected abstract void ApplyEffect(GameObject player);
}
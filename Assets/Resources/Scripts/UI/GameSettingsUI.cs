using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSettingsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown taskTypeDropdown;
    [SerializeField] private TMP_Dropdown timeLimitDropdown;
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private ConfirmationUI confirmationUI;

    private void Start()
    {
        // Force reset to default each time the game starts
        PlayerPrefs.SetInt("TaskMode", 0);
        PlayerPrefs.SetInt("TimeLimitMode", 0);
        PlayerPrefs.Save();

        if (taskTypeDropdown != null)
        {
            taskTypeDropdown.value = 0;
            taskTypeDropdown.onValueChanged.AddListener(OnTaskTypeChanged);
        }

        if (timeLimitDropdown != null)
        {
            timeLimitDropdown.value = 0;
            timeLimitDropdown.onValueChanged.AddListener(OnTimeLimitChanged);
        }

        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.AddListener(() => {
                if (confirmationUI != null)
                {
                    confirmationUI.ShowConfirmation(
                        "Return to Lobby",
                        "Are you sure you want to return to the lobby? All unsaved progress will be lost.",
                        () => ReturnToLobby()
                    );
                }
                else
                {
                    ReturnToLobby();
                }
            });
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ToggleSettings);
        }
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void OnTaskTypeChanged(int value)
    {
        // 0 = Mix (Default), 1 = Insertion Only, 2 = Deletion Only
        PlayerPrefs.SetInt("TaskMode", value);
        PlayerPrefs.Save();
    }

    public void OnTimeLimitChanged(int value)
    {
        // 0 = 30s, 1 = 60s, 2 = Unlimited
        PlayerPrefs.SetInt("TimeLimitMode", value);
        PlayerPrefs.Save();
    }

    public void ReturnToLobby()
    {
        ToggleSettings();
        
        // Hide any lingering instructions
        if (PlayerInstructionUI.Instance != null)
        {
            PlayerInstructionUI.Instance.HideInstruction();
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 1. Mock 'player death' event to clear active magic and reset things
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnPlayerDeath?.Invoke();
                playerHealth.ResetHealth();
            }

            // 2. Teleport back to the lobby
            ExitPortalController exitPortal = FindObjectOfType<ExitPortalController>();
            if (exitPortal != null)
            {
                exitPortal.ForceExit(player);
            }
            else
            {
                Debug.LogWarning("Cannot return to lobby: ExitPortalController not found!");
            }
        }
        else
        {
            Debug.LogWarning("Cannot return to lobby: Player not found!");
        }
    }

    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
            
            if (settingsPanel.activeSelf)
            {
                Time.timeScale = 0f; // Pause the game
                
                if (returnToLobbyButton != null)
                {
                    DungeonManager dManager = FindObjectOfType<DungeonManager>();
                    returnToLobbyButton.interactable = (dManager != null && dManager.isDungeonActive);
                }
            }
            else
            {
                Time.timeScale = 1f; // Resume the game
            }
        }
    }
}

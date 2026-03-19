using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
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

    [Header("Audio & Video Settings")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Button resetVolumeButton;
    [SerializeField] private Button resetBrightnessButton;
    
    [Header("Post Processing")]
    [SerializeField] private PostProcessVolume globalVolume;
    private ColorGrading colorGrading;

    private void Start()
    {
        // Try getting ColorGrading from the bound PostProcessVolume
        if (globalVolume != null)
        {
            globalVolume.profile.TryGetSettings(out colorGrading);
        }

        // Force reset to default each time the game starts
        PlayerPrefs.SetInt("TaskMode", 0);
        PlayerPrefs.SetInt("TimeLimitMode", 0);
        PlayerPrefs.SetFloat("GlobalVolume", 1.0f);
        PlayerPrefs.SetFloat("Brightness", -20f);
        PlayerPrefs.Save();

        // Load Audio and Lighting settings
        float savedVolume = PlayerPrefs.GetFloat("GlobalVolume");
        float savedBrightness = PlayerPrefs.GetFloat("Brightness");
        
        ApplyVolume(savedVolume);
        ApplyBrightness(savedBrightness);

        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }

        if (resetVolumeButton != null)
        {
            resetVolumeButton.onClick.AddListener(ResetVolumeToDefault);
        }

        if (resetBrightnessButton != null)
        {
            resetBrightnessButton.onClick.AddListener(ResetBrightnessToDefault);
        }

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

    public void OnVolumeChanged(float value)
    {
        ApplyVolume(value);
        PlayerPrefs.SetFloat("GlobalVolume", value);
        PlayerPrefs.Save();
    }

    private void ApplyVolume(float value)
    {
        // Controls all AudioSources in the game globally
        AudioListener.volume = value;
    }

    public void OnBrightnessChanged(float value)
    {
        ApplyBrightness(value);
        PlayerPrefs.SetFloat("Brightness", value);
        PlayerPrefs.Save();
    }
    
    private void ApplyBrightness(float value)
    {
        // Control brightness using Post Processing's ColorGrading.brightness
        if (colorGrading != null)
        {
            colorGrading.brightness.value = value;
        }
    }

    public void ResetVolumeToDefault()
    {
        float defaultVolume = 1.0f;
        if (volumeSlider != null)
        {
            volumeSlider.value = defaultVolume;
        }
        else
        {
            OnVolumeChanged(defaultVolume);
        }
    }

    public void ResetBrightnessToDefault()
    {
        float defaultBrightness = -20f;
        if (brightnessSlider != null)
        {
            brightnessSlider.value = defaultBrightness;
        }
        else
        {
            OnBrightnessChanged(defaultBrightness);
        }
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
            // Set a flag in DungeonManager so it knows this is a forced exit and doesn't show the death UI
            DungeonManager dManager = FindObjectOfType<DungeonManager>();
            if (dManager != null)
            {
                dManager.isForceExiting = true;
            }
            
            // 1. Clean up player state and mimic death logic to clear magic
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

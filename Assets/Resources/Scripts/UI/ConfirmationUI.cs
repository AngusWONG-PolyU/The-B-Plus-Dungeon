using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class ConfirmationUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private void Awake()
    {
        // Hide the panel by default on game start
        gameObject.SetActive(false);
    }

    public void ShowConfirmation(string title, string message, UnityAction onConfirm, UnityAction onCancel = null)
    {
        // Bring to the front in the local hierarchy
        transform.SetAsLastSibling();
        
        // Deselect current UI element so players can't accidentally trigger old buttons with Space/Enter
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        gameObject.SetActive(true);

        // Update the texts if the references are assigned
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;
        
        // Clear previous listeners to prevent multiple actions from triggering at once
        confirmButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
        
        // Bind the confirm button
        confirmButton.onClick.AddListener(() => {
            gameObject.SetActive(false);
            onConfirm?.Invoke();
        });
        
        // Bind the cancel button
        cancelButton.onClick.AddListener(() => {
            gameObject.SetActive(false);
            onCancel?.Invoke();
        });
    }
}
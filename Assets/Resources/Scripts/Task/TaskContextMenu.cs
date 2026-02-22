using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskContextMenu : MonoBehaviour
{
    public static TaskContextMenu Instance { get; private set; }

    [Header("UI References")]
    public GameObject menuPanel; // Assign the UI Panel
    public Button deleteButton;
    public Button copyUpButton;
    public Button closeButton;

    private TaskClickable _currentTarget; // The clickable object (Key) we opened menu for

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        HideMenu();
        
        // Setup listeners
        if (deleteButton) deleteButton.onClick.AddListener(OnDeleteClicked);
        if (copyUpButton) copyUpButton.onClick.AddListener(OnCopyUpClicked);
        if (closeButton) closeButton.onClick.AddListener(HideMenu);
    }

    public void ShowMenu(TaskClickable target, Vector2 screenPos)
    {
        _currentTarget = target;
        
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            menuPanel.transform.position = screenPos; // Place near mouse
        }

        // Logic to enable/disable Copy Up based on context
        if (copyUpButton != null)
        {
            try
            {
                BPlusTreeVisualNode node = _currentTarget.GetParentVisualNode();
                // Can only copy up if the current node is a Leaf Node
                bool canCopyUp = (node != null && node.CoreNode != null && node.CoreNode.IsLeaf);
                                  
                copyUpButton.interactable = canCopyUp;
                
                // Optional: visual feedback
                var buttonImage = copyUpButton.GetComponent<Image>();
                if(buttonImage) buttonImage.color = canCopyUp ? Color.white : Color.gray;
            }
            catch
            {
                copyUpButton.interactable = false;
            }
        }
    }

    public void HideMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        _currentTarget = null;
    }

    private void OnDeleteClicked()
    {
        if (_currentTarget != null)
        {
            // Call Manager to execute deletion
            BPlusTreeVisualNode parentNode = _currentTarget.GetParentVisualNode();
            int val = _currentTarget.GetValue();
            
            if (TaskDragManager.Instance != null && parentNode != null)
            {
                TaskDragManager.Instance.DeleteKey(parentNode, val);
            }
            
            HideMenu();
        }
    }

    private void OnCopyUpClicked()
    {
        if (_currentTarget != null)
        {
            // Call Manager to execute copy up
            BPlusTreeVisualNode parentNode = _currentTarget.GetParentVisualNode();
            int val = _currentTarget.GetValue();
            
            if (TaskDragManager.Instance != null && parentNode != null)
            {
                TaskDragManager.Instance.CopyKeyToParent(parentNode, val);
            }
            
            HideMenu();
        }
    }
}
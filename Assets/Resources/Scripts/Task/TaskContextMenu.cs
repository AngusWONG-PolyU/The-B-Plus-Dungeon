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
    public Button deleteKeyButton;
    public Button deleteNodeButton;
    public Button copyUpButton;
    public Button splitNodeButton;
    public Button closeButton;

    private TaskClickable _currentTargetKey; 
    private BPlusTreeVisualNode _currentTargetNode;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        HideMenu();
        
        // Setup listeners
        if (deleteKeyButton) deleteKeyButton.onClick.AddListener(OnDeleteKeyClicked);
        if (deleteNodeButton) deleteNodeButton.onClick.AddListener(OnDeleteNodeClicked);
        if (copyUpButton) copyUpButton.onClick.AddListener(OnCopyUpClicked);
        if (splitNodeButton) splitNodeButton.onClick.AddListener(OnSplitClicked);
        if (closeButton) closeButton.onClick.AddListener(HideMenu);
    }
    
    private void Update()
    {
        if (menuPanel != null && menuPanel.activeSelf)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // Check if the click is outside the menu panel
                RectTransform rect = menuPanel.GetComponent<RectTransform>();
                if (rect != null)
                {
                    if (!RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition))
                    {
                        HideMenu();
                    }
                }
            }
        }
    }

    public void ShowMenu(TaskClickable target, Vector2 screenPos)
    {
        _currentTargetKey = target;
        _currentTargetNode = null;
        
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            menuPanel.transform.position = screenPos; 
        }

        // Configure Buttons for Key Context
        if (deleteKeyButton) 
        {
            deleteKeyButton.gameObject.SetActive(true);
            
            // Check Deletion constraints
            bool canDelete = true;
            if (BPlusTreeTaskManager.Instance != null)
            {
                int val = target.GetValue();
                BPlusTreeVisualNode parentNode = target.GetParentVisualNode();
                
                if (parentNode == null)
                {
                    canDelete = false; // Cannot delete keys in buffer area
                }
                else
                {
                    bool isLeaf = (parentNode.CoreNode != null && parentNode.CoreNode.IsLeaf);
                    
                    // Restriction applies effectively only to Leaf nodes
                    if (isLeaf && !BPlusTreeTaskManager.Instance.TargetKeys.Contains(val))
                    {
                        canDelete = false;
                    }
                }
            }
            
            deleteKeyButton.interactable = canDelete;
        }

        if (deleteNodeButton) 
        {
            deleteNodeButton.gameObject.SetActive(false); // Hide Delete Node button in Key context
        }
        
        if (splitNodeButton)
        {
            splitNodeButton.gameObject.SetActive(true);
            BPlusTreeVisualNode node = _currentTargetKey.GetParentVisualNode();
            splitNodeButton.interactable = (node != null);
        }
        
        // Logic to enable/disable Copy Up based on context
        if (copyUpButton != null)
        {
            copyUpButton.gameObject.SetActive(true); // Ensure it is visible
            try
            {
                BPlusTreeVisualNode node = _currentTargetKey.GetParentVisualNode();
                // Can only copy up if the current node is a Leaf Node
                bool canCopyUp = (node != null && node.CoreNode != null && node.CoreNode.IsLeaf);
                                  
                copyUpButton.interactable = canCopyUp;
            }
            catch
            {
                copyUpButton.interactable = false;
            }
        }
    }

    // New ShowMenu for Nodes (e.g. Empty Node)
    public void ShowNodeMenu(BPlusTreeVisualNode node, Vector2 screenPos)
    {
        _currentTargetKey = null;
        _currentTargetNode = node;

        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            menuPanel.transform.position = screenPos; 
        }

        // Configure Buttons for Node Context
        if (deleteKeyButton) 
        {
            deleteKeyButton.gameObject.SetActive(false); // Hide Delete Key button in Node context
        }

        // Only allow Delete Node if the node is effectively empty (0 keys)
        bool isNodeEmpty = (node != null && node.CoreNode != null && node.CoreNode.Keys.Count == 0);
        if (deleteNodeButton) 
        {
            deleteNodeButton.gameObject.SetActive(true);
            deleteNodeButton.interactable = isNodeEmpty;
        }
        
        if (copyUpButton)
        {
            copyUpButton.gameObject.SetActive(false);
        }
        
        if (splitNodeButton)
        {
            splitNodeButton.gameObject.SetActive(false);
        }
    }

    public void HideMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        _currentTargetKey = null;
        _currentTargetNode = null;
    }

    private void OnDeleteKeyClicked()
    {
        if (_currentTargetKey != null)
        {
            // Call Manager to execute deletion
            BPlusTreeVisualNode parentNode = _currentTargetKey.GetParentVisualNode();
            int val = _currentTargetKey.GetValue();
            
            if (TaskDragManager.Instance != null)
            {
                if (parentNode != null)
                {
                    TaskDragManager.Instance.DeleteKey(parentNode, val);
                }
                else
                {
                    // Handle deletion from buffer area (not supported yet, or just ignore)
                    Debug.LogWarning("Cannot delete keys from buffer area.");
                }
            }
            
            HideMenu();
        }
    }
    
    private void OnDeleteNodeClicked()
    {
        // Handle deletion of the node itself
        if (_currentTargetNode != null)
        {
            if (TaskDragManager.Instance != null)
            {
                TaskDragManager.Instance.DeleteNode(_currentTargetNode);
            }
            HideMenu();
        }
    }

    private void OnCopyUpClicked()
    {
        if (_currentTargetKey != null)
        {
            // Call Manager to execute copy up
            BPlusTreeVisualNode parentNode = _currentTargetKey.GetParentVisualNode();
            int val = _currentTargetKey.GetValue();
            
            if (TaskDragManager.Instance != null && parentNode != null)
            {
                TaskDragManager.Instance.CopyKeyToParent(parentNode, val);
            }
            
            HideMenu();
        }
    }

    private void OnSplitClicked()
    {
        if (_currentTargetKey != null)
        {
            BPlusTreeVisualNode parentNode = _currentTargetKey.GetParentVisualNode();
            int val = _currentTargetKey.GetValue();
            
            if (TaskDragManager.Instance != null && parentNode != null)
            {
                TaskDragManager.Instance.SplitNode(parentNode, val);
            }
            
            HideMenu();
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BPlusTreeVisualNode : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public Transform keyContainer;
    public GameObject keyPrefab;
    
    // Data
    public BPlusTreeNode<int, string> CoreNode { get; private set; }
    public List<GameObject> SpawnedKeys { get; private set; } = new List<GameObject>();
    
    // Track highlight state for interactivity checks
    public bool IsHighlighted { get; private set; }
    
    private string _errorMessage = "";
    private bool _inResultPhase = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (TaskContextMenu.Instance != null && !_inResultPhase)
            {
                TaskContextMenu.Instance.ShowNodeMenu(this, eventData.position);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_inResultPhase && !string.IsNullOrEmpty(_errorMessage) && TaskTooltip.Instance != null)
        {
            TaskTooltip.Instance.ShowTooltip(_errorMessage);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_inResultPhase && TaskTooltip.Instance != null)
        {
            TaskTooltip.Instance.HideTooltip();
        }
    }

    public void SetResultHighlight(bool isCorrect, string errorMsg)
    {
        _inResultPhase = true;
        _errorMessage = errorMsg;
        IsHighlighted = !isCorrect;

        Image img = GetComponent<Image>();
        if (img != null)
        {
            // Green for correct, Red for error
            img.color = isCorrect ? new Color(0.5f, 1f, 0.5f, 0.8f) : new Color(1f, 0.5f, 0.5f, 0.8f);
        }

        Outline outline = GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = isCorrect ? Color.green : Color.red;
            outline.effectDistance = new Vector2(3, -3);
        }
    }

    public void Initialize(BPlusTreeNode<int, string> node)
    {
        CoreNode = node;
        RenderKeys();
    }

    public void SetHighlight(bool isError, bool isRoutingError = false)
    {
        IsHighlighted = isError;
        Image img = GetComponent<Image>();
        if(img != null)
        {
            if (isRoutingError)
            {
                img.color = new Color(1f, 0.8f, 0.2f, 0.8f); // Orange for routing error
            }
            else
            {
                // Red for error (underflow/overflow), White/Gray for normal
                img.color = isError ? new Color(1f, 0.5f, 0.5f, 0.8f) : new Color(1f, 1f, 1f, 0.5f);
            }
        }
        
        // Pulse animation or Outline color change
        Outline outline = GetComponent<Outline>();
        if(outline != null)
        {
             if (isRoutingError)
             {
                 outline.effectColor = new Color(1f, 0.5f, 0f, 1f); // Dark orange
             }
             else
             {
                 outline.effectColor = isError ? Color.red : Color.black;
             }
             outline.effectDistance = isError || isRoutingError ? new Vector2(3, -3) : new Vector2(2, -2);
        }
    }

    public void RenderKeys()
    {
        // Clear old keys
        foreach (Transform child in keyContainer)
        {
            Destroy(child.gameObject);
        }
        SpawnedKeys.Clear();

        // Spawn new keys
        foreach (var key in CoreNode.Keys)
        {
            GameObject k = Instantiate(keyPrefab, keyContainer);
            
            TextMeshProUGUI t = k.GetComponentInChildren<TextMeshProUGUI>();
            if(t) t.text = key.ToString();

            // Ensure square shape via LayoutElement if not present
            LayoutElement le = k.GetComponent<LayoutElement>();
            if (le == null) le = k.AddComponent<LayoutElement>();
            
            // Set a default square size (e.g. 50x50)
            if (le.preferredWidth <= 0) le.preferredWidth = 50f;
            if (le.preferredHeight <= 0) le.preferredHeight = 50f; // Make it square
            le.minWidth = 50f;
            le.minHeight = 50f;
            
            // Highlight the key if it's the target key for deletion
            if (BPlusTreeTaskManager.Instance != null && 
                BPlusTreeTaskManager.Instance.CurrentTaskType == BPlusTreeTaskType.Deletion && 
                BPlusTreeTaskManager.Instance.TargetKeys.Contains(key))
            {
                Image img = k.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(1f, 0.5f, 0.5f, 1f); // Reddish highlight
                }
                
                Outline outline = k.GetComponent<Outline>();
                if (outline == null) outline = k.AddComponent<Outline>();
                outline.effectColor = Color.red;
                outline.effectDistance = new Vector2(3, -3);
            }
            
            // Highlight the key if it's the target key for insertion
            if (BPlusTreeTaskManager.Instance != null && 
                BPlusTreeTaskManager.Instance.CurrentTaskType == BPlusTreeTaskType.Insertion && 
                BPlusTreeTaskManager.Instance.TargetKeys.Contains(key))
            {
                Image img = k.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.5f, 0.8f, 0.5f, 1f); // Darker green highlight
                }
                
                Outline outline = k.GetComponent<Outline>();
                if (outline == null) outline = k.AddComponent<Outline>();
                outline.effectColor = new Color(0.1f, 0.4f, 0.1f, 1f); // Dark green outline
                outline.effectDistance = new Vector2(3, -3);
            }

            SpawnedKeys.Add(k);
        }

        if (CoreNode.Keys.Count == 0)
        {
            // Add a dummy spacer to the container so ContentSizeFitter sees "content"
            GameObject spacer = new GameObject("EmptySpacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(keyContainer, false);
            LayoutElement spacerLE = spacer.GetComponent<LayoutElement>();
            spacerLE.minWidth = 80f;
            spacerLE.minHeight = 80f; // Ensure node stays at least 80x80
        }
    }
}
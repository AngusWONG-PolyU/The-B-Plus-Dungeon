using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BPlusTreeVisualNode : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    public Transform keyContainer;
    public GameObject keyPrefab;
    
    // Data
    public BPlusTreeNode<int, string> CoreNode { get; private set; }
    public List<GameObject> SpawnedKeys { get; private set; } = new List<GameObject>();
    
    // Track highlight state for interactivity checks
    public bool IsHighlighted { get; private set; }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (TaskContextMenu.Instance != null)
            {
                TaskContextMenu.Instance.ShowNodeMenu(this, eventData.position);
            }
        }
    }

    public void Initialize(BPlusTreeNode<int, string> node)
    {
        CoreNode = node;
        RenderKeys();
    }

    public void SetHighlight(bool isError)
    {
        IsHighlighted = isError;
        Image img = GetComponent<Image>();
        if(img != null)
        {
            // Red for error (underflow/overflow), White/Gray for normal
            img.color = isError ? new Color(1f, 0.5f, 0.5f, 0.8f) : new Color(1f, 1f, 1f, 0.5f);
        }
        
        // Pulse animation or Outline color change
        Outline outline = GetComponent<Outline>();
        if(outline != null)
        {
             outline.effectColor = isError ? Color.red : Color.black;
             outline.effectDistance = isError ? new Vector2(3, -3) : new Vector2(2, -2);
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
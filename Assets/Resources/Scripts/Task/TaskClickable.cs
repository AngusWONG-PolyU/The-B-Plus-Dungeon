using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TaskClickable : MonoBehaviour, IPointerClickHandler
{
    private BPlusTreeVisualNode _parentVisualNode;

    private void Awake()
    {
        _parentVisualNode = GetComponentInParent<BPlusTreeVisualNode>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right Click Detected
            if (TaskContextMenu.Instance != null)
            {
                TaskContextMenu.Instance.ShowMenu(this, eventData.position);
            }
        }
    }

    public BPlusTreeVisualNode GetParentVisualNode()
    {
        // Always try to get it dynamically in case it moved
        return GetComponentInParent<BPlusTreeVisualNode>();
    }

    public int GetValue()
    {
        // Try TMP
        var tmpro = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpro != null && int.TryParse(tmpro.text, out int val)) return val;
        
        // Try Text
        var txt = GetComponentInChildren<Text>();
        if (txt != null && int.TryParse(txt.text, out int val2)) return val2;
        
        return -1; // Parse error
    }
}
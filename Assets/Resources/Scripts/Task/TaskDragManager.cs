using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TaskDragManager : MonoBehaviour
{
    public static TaskDragManager Instance { get; private set; }

    [Header("Drag Settings")]
    public float thresholdDistance = 50f;
    public bool isDragging = false;
    
    private TaskDraggable _currentItem;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartDragging(TaskDraggable item)
    {
        _currentItem = item;
        isDragging = true;
    }

    public void StopDragging()
    {
        if (_currentItem != null)
        {
            // Reset state
            _currentItem = null;
        }
        isDragging = false;
    }

    // Call from DropZone logic or OnEndDrag
    public bool CheckDrop(Vector3 mousePos)
    {
        // Simple overlap check or custom logic
        return false;
    }
}

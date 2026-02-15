using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TaskDropZone : MonoBehaviour, IDropHandler
{
    private BPlusTreeVisualNode _visualNode;

    private void Awake()
    {
        _visualNode = GetComponent<BPlusTreeVisualNode>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_visualNode == null) return;

        // 1. Try Handle Key Drop
        if (eventData.pointerDrag != null)
        {
            TaskDraggable keyDrag = eventData.pointerDrag.GetComponent<TaskDraggable>();
            if (keyDrag != null)
            {
                bool success = TaskDragManager.Instance.HandleKeyDrop(keyDrag, _visualNode);
                keyDrag.isSuccess = success;
                return;
            }
            
            // 2. Try Handle Node Drop
            TaskNodeDraggable nodeDrag = eventData.pointerDrag.GetComponent<TaskNodeDraggable>();
            if (nodeDrag != null)
            {
                // Call Manager
                TaskDragManager.Instance.HandleNodeDrop(_visualNode);
                
                // Note: NodeDraggable handles visual reset in its own OnEndDrag
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class TaskNodeDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private Vector2 _originalPosition;
    private BPlusTreeVisualNode _visualNode;
    private bool _isDragging = false;
    
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
        _visualNode = GetComponent<BPlusTreeVisualNode>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if(TaskDragManager.Instance == null) return;

        // Prevent drag if confirmation panel is open (Time is paused)
        if (Time.timeScale == 0f) return;

        // Tutorial Restriction Check
        if (!IsDragAllowed()) 
        {
            Debug.Log("Drag restricted: In tutorial, you focus on fixing the highlighted error nodes.");
            return;
        }

        _isDragging = true;
        _originalPosition = _rectTransform.anchoredPosition;
        
        _canvasGroup.blocksRaycasts = false; // Allow dropping on other nodes
        _canvasGroup.alpha = 0.6f;
        
        // Ensure draws on top while dragging
        transform.SetAsLastSibling(); 

        TaskDragManager.Instance.StartNodeDragging(_visualNode);
    }

    private bool IsDragAllowed()
    {
        // Find difficulty settings
        DungeonGenerator gen = FindObjectOfType<DungeonGenerator>();
        if (gen != null && gen.difficultyMode == DungeonGenerator.DifficultyMode.Tutorial)
        {
            // In tutorial: specific guidance. Only allow dragging if highlighted (the problem node)
            return _visualNode.IsHighlighted; 
        }
        
        // Easy/Standard/Hard: Free freedom to mess up!
        return true; 
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _canvas == null) return;

        _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        
        _isDragging = false;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;

        // Visual Snap back (Logic happens in Manager via DropZone)
        _rectTransform.anchoredPosition = _originalPosition;
        
        TaskDragManager.Instance.StopNodeDragging(eventData);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class TaskDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    public Transform originalParent;
    public Vector3 originalPosition;
    private int _originalSiblingIndex; // Store index to restore order
    public bool isSuccess = false; // Flag to check if dropped successfully
    private bool _isDragging = false; 

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        // Find the root canvas for coordinate conversion
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (TaskDragManager.Instance == null) return;
        
        // Prevent drag if confirmation panel is open (Time is paused)
        if (Time.timeScale == 0f) return;

        _isDragging = true;
        isSuccess = false;
        originalParent = transform.parent;
        originalPosition = transform.position;
        _originalSiblingIndex = transform.GetSiblingIndex(); // Save index

        // Visual feedback
        _canvasGroup.alpha = 0.6f;
        _canvasGroup.blocksRaycasts = false; // Allow rays to pass through to find drop zones

        // Notify Manager
        TaskDragManager.Instance.StartDragging(this);

        // Move to root to draw on top of everything
        if (_canvas != null)
        {
            transform.SetParent(_canvas.transform, true);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _canvas == null) return;
        
        // Follow mouse
        _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        _isDragging = false;
        
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;

        TaskDragManager.Instance.StopDragging();

        // If not successfully dropped, return to original position
        if (!isSuccess)
        {
            ReturnToStart();
        }
    }

    public void ReturnToStart()
    {
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(_originalSiblingIndex); // Restore order
        transform.position = originalPosition;
    }
}
